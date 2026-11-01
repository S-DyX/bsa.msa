using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Bsa.Msa.Common;

namespace Bsa.Msa.RabbitMq.Core.Common
{
	/// <summary>
	/// Append-only journal of the local queue of one subscription, used instead of a file per message.
	/// A message is a record in the current segment, its processing is an acknowledgement record.
	/// A segment is deleted when every message in it and in the older segments is acknowledged;
	/// a few messages stuck in the oldest segment are rewritten into the current one.
	/// Record: [kind:1][length:4][checksum:4][payload], little-endian. The checksum detects a torn tail.
	/// Not thread-safe: the caller holds a lock.
	/// </summary>
	internal sealed class InternalBusJournal : IDisposable
	{
		private readonly string _folder;
		private readonly long _segmentSize;
		private readonly ISerializeService _serializeService;
		private readonly ILocalLogger _logger;
		private readonly Func<string, InternalBusItem> _resolve;
		private readonly List<Segment> _segments = new List<Segment>();
		private readonly Dictionary<string, Segment> _locations = new Dictionary<string, Segment>();
		private readonly byte[] _header = new byte[HeaderSize];
		private Segment _current;
		private FileStream _stream;
		private long _nextNumber = 1;
		private bool _isRecovered;
		private int _unflushedAcks;

		private const byte MessageRecord = 1;
		private const byte AckRecord = 2;
		private const int HeaderSize = 9;
		private const int MaxRecordSize = 256 * 1024 * 1024;
		private const int MaxSegments = 8;
		private const int CompactLiveLimit = 1000;
		private const string Extension = ".qj";
		private const int AckFlushThreshold = 256;

		/// <summary>
		/// Default size of a segment file.
		/// </summary>
		internal const long DefaultSegmentSize = 16L * 1024 * 1024;

		/// <summary>
		/// Creates the journal; <see cref="Recover"/> must be called before the first write.
		/// </summary>
		/// <param name="folder">Folder of the subscription.</param>
		/// <param name="serializeService">Serializer of the records.</param>
		/// <param name="logger">Logger.</param>
		/// <param name="resolve">Returns a live message by id, used to rewrite stuck messages.</param>
		/// <param name="segmentSize">Size after which a new segment is started.</param>
		public InternalBusJournal(
			string folder,
			ISerializeService serializeService,
			ILocalLogger logger,
			Func<string, InternalBusItem> resolve,
			long segmentSize = DefaultSegmentSize)
		{
			_folder = folder;
			_serializeService = serializeService;
			_logger = logger;
			_resolve = resolve;
			_segmentSize = Math.Max(1, segmentSize);
		}

		/// <summary>
		/// Number of unacknowledged messages.
		/// </summary>
		public int Count => _locations.Count;

		/// <summary>
		/// Number of segment files.
		/// </summary>
		public int SegmentCount => _segments.Count;

		/// <summary>
		/// Reads the segments left by the previous run and returns the unacknowledged messages.
		/// </summary>
		public List<InternalBusItem> Recover()
		{
			if (_isRecovered) throw new InvalidOperationException($"Journal {_folder} is already recovered");
			_isRecovered = true;
			var messages = new Dictionary<string, (InternalBusItem Item, Segment Segment)>();
			var acked = new HashSet<string>();
			foreach (var segment in FindSegments())
			{
				_segments.Add(segment);
				_nextNumber = Math.Max(_nextNumber, segment.Number + 1);
				ReadSegment(segment.Path, (kind, payload) =>
				{
					var text = Encoding.UTF8.GetString(payload);
					if (kind == AckRecord)
					{
						acked.Add(text);
						return;
					}
					var item = _serializeService.Deserialize<InternalBusItem>(text);
					// A rewritten message appears twice, the newest copy wins.
					if (item?.Id != null) messages[item.Id] = (item, segment);
				});
			}
			var result = new List<InternalBusItem>();
			foreach (var pair in messages)
			{
				if (acked.Contains(pair.Key)) continue;
				pair.Value.Segment.Live.Add(pair.Key);
				_locations[pair.Key] = pair.Value.Segment;
				result.Add(pair.Value.Item);
			}
			DeleteCompleted();
			return result;
		}

		/// <summary>
		/// Writes a message and flushes it to the operating system before returning.
		/// </summary>
		public void Append(InternalBusItem item)
		{
			EnsureRecovered();
			EnsureSegment();
			Write(MessageRecord, Encoding.UTF8.GetBytes(_serializeService.Serialize(item)));
			_current.Live.Add(item.Id);
			_locations[item.Id] = _current;
			_stream.Flush();
			_unflushedAcks = 0;
			Compact();
		}

		/// <summary>
		/// Marks a message as processed. The record reaches the operating system with the next message,
		/// after <see cref="AckFlushThreshold"/> acknowledgements or on <see cref="Flush"/>:
		/// a lost acknowledgement only makes the message be handled again after a crash.
		/// Returns <c>false</c> for an unknown id.
		/// </summary>
		public bool Ack(string id)
		{
			EnsureRecovered();
			if (!_locations.Remove(id, out var segment)) return false;
			segment.Live.Remove(id);
			EnsureSegment();
			Write(AckRecord, Encoding.UTF8.GetBytes(id));
			if (++_unflushedAcks >= AckFlushThreshold) Flush();
			DeleteCompleted();
			return true;
		}

		/// <summary>
		/// Writes the buffered acknowledgements to the operating system.
		/// </summary>
		public void Flush()
		{
			if (_stream == null || _unflushedAcks == 0) return;
			_stream.Flush();
			_unflushedAcks = 0;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			_stream?.Dispose();
			_stream = null;
		}

		private void EnsureRecovered()
		{
			if (!_isRecovered) throw new InvalidOperationException($"Journal {_folder} must be recovered before use");
		}

		private IEnumerable<Segment> FindSegments()
		{
			if (!Directory.Exists(_folder)) return Array.Empty<Segment>();
			return Directory.GetFiles(_folder, "*" + Extension)
				.Select(path => (Path: path, Number: ParseNumber(path)))
				.Where(x => x.Number > 0)
				.OrderBy(x => x.Number)
				.Select(x => new Segment(x.Number, x.Path))
				.ToList();
		}

		private static long ParseNumber(string path)
		{
			return long.TryParse(Path.GetFileNameWithoutExtension(path), out var number) ? number : 0;
		}

		private void EnsureSegment()
		{
			if (_current != null && _current.Length < _segmentSize) return;
			_stream?.Dispose();
			_unflushedAcks = 0;
			Directory.CreateDirectory(_folder);
			var number = _nextNumber++;
			var segment = new Segment(number, Path.Combine(_folder, number.ToString("D10") + Extension));
			_stream = new FileStream(
				segment.Path,
				FileMode.CreateNew,
				FileAccess.Write,
				FileShare.Read | FileShare.Delete,
				64 * 1024);
			_current = segment;
			_segments.Add(segment);
			DeleteCompleted();
		}

		private void Write(byte kind, byte[] payload)
		{
			_header[0] = kind;
			BinaryPrimitives.WriteInt32LittleEndian(_header.AsSpan(1), payload.Length);
			BinaryPrimitives.WriteUInt32LittleEndian(_header.AsSpan(5), Checksum(payload));
			_stream.Write(_header, 0, HeaderSize);
			_stream.Write(payload, 0, payload.Length);
			_current.Length += HeaderSize + payload.Length;
		}

		/// <summary>
		/// Deletes the oldest segments while every message in them is acknowledged. Only the oldest ones:
		/// a segment also holds acknowledgements of older messages, and deleting it while those
		/// messages are still on disk would bring them back after a restart.
		/// </summary>
		private void DeleteCompleted()
		{
			while (_segments.Count > 0 && _segments[0] != _current && _segments[0].Live.Count == 0)
			{
				var segment = _segments[0];
				_segments.RemoveAt(0);
				try
				{
					File.Delete(segment.Path);
				}
				catch (Exception ex)
				{
					_logger?.Error($"Can not delete journal segment {segment.Path}: {ex.Message}", ex);
				}
			}
		}

		/// <summary>
		/// A message that is never acknowledged would keep every newer segment on disk:
		/// when there are too many segments, the live messages of the oldest one are rewritten.
		/// </summary>
		private void Compact()
		{
			if (_segments.Count <= MaxSegments) return;
			var oldest = _segments[0];
			if (oldest == _current || oldest.Live.Count > CompactLiveLimit) return;
			foreach (var id in oldest.Live.ToList())
			{
				var item = _resolve?.Invoke(id);
				if (item == null) continue;
				Write(MessageRecord, Encoding.UTF8.GetBytes(_serializeService.Serialize(item)));
				oldest.Live.Remove(id);
				_current.Live.Add(id);
				_locations[id] = _current;
			}
			_stream.Flush();
			DeleteCompleted();
		}

		private void ReadSegment(string path, Action<byte, byte[]> onRecord)
		{
			using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
			{
				var header = new byte[HeaderSize];
				while (ReadExact(stream, header))
				{
					var kind = header[0];
					var length = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(1));
					var checksum = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(5));
					if ((kind != MessageRecord && kind != AckRecord) || length < 0 || length > MaxRecordSize)
					{
						_logger?.Warn($"Journal segment {path} is damaged at {stream.Position - HeaderSize}, the rest is skipped");
						return;
					}
					var payload = new byte[length];
					if (!ReadExact(stream, payload) || Checksum(payload) != checksum)
					{
						// A record cut by a crash: it was never flushed, so it was never acknowledged to the broker.
						_logger?.Warn($"Journal segment {path} ends with an incomplete record, it is skipped");
						return;
					}
					try
					{
						onRecord(kind, payload);
					}
					catch (Exception ex)
					{
						_logger?.Error($"Journal record in {path} can not be read: {ex.Message}", ex);
					}
				}
			}
		}

		private static bool ReadExact(Stream stream, byte[] buffer)
		{
			var read = 0;
			while (read < buffer.Length)
			{
				var n = stream.Read(buffer, read, buffer.Length - read);
				if (n == 0) return false;
				read += n;
			}
			return true;
		}

		// FNV-1a: enough to detect a torn or overwritten record, not meant against tampering.
		private static uint Checksum(byte[] data)
		{
			var hash = 2166136261u;
			foreach (var b in data)
			{
				hash ^= b;
				hash *= 16777619u;
			}
			return hash;
		}

		private sealed class Segment
		{
			public Segment(long number, string path)
			{
				Number = number;
				Path = path;
			}

			public long Number { get; }

			public string Path { get; }

			public long Length { get; set; }

			public HashSet<string> Live { get; } = new HashSet<string>();
		}
	}
}
