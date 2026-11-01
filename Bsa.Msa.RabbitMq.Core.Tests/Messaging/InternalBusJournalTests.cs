using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bsa.Msa.RabbitMq.Core.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bsa.Msa.RabbitMq.Core.Tests.Messaging
{
	/// <summary>
	/// Tests of <see cref="InternalBusJournal"/>: recovery, deletion of segments, a torn tail and compaction.
	/// </summary>
	[TestClass]
	public sealed class InternalBusJournalTests
	{
		private readonly Dictionary<string, InternalBusItem> _live = new Dictionary<string, InternalBusItem>();

		private string _folder;

		[TestInitialize]
		public void Initialize()
		{
			_folder = Path.Combine(Path.GetTempPath(), "bsa-msa-journal-" + Guid.NewGuid().ToString("N"));
		}

		[TestCleanup]
		public void Cleanup()
		{
			if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
		}

		[TestMethod]
		public void Recover_AfterRestart_ReturnsOnlyUnacknowledged()
		{
			using (var journal = Open())
			{
				journal.Append(Item("1", "{\"Id\":1}"));
				journal.Append(Item("2", "{\"Id\":2}"));
				journal.Append(Item("3", "{\"Id\":3}"));
				journal.Ack("2");
			}

			using (var restarted = new InternalBusJournal(_folder, new SerializeService(), null, Resolve))
			{
				var items = restarted.Recover().OrderBy(x => x.Id).ToList();

				CollectionAssert.AreEqual(new[] { "1", "3" }, items.Select(x => x.Id).ToArray());
				Assert.AreEqual("{\"Id\":3}", items[1].Body);
				Assert.AreEqual("orders", items[1].Queue);
				Assert.AreEqual(2, restarted.Count);
			}
		}

		[TestMethod]
		public void Ack_AllMessagesOfOldSegments_DeletesThem()
		{
			// Every record starts a new segment.
			using (var journal = Open(segmentSize: 1))
			{
				for (var i = 0; i < 5; i++) journal.Append(Item(i.ToString(), "{}"));
				for (var i = 0; i < 5; i++) journal.Ack(i.ToString());

				Assert.AreEqual(0, journal.Count);
				Assert.AreEqual(1, journal.SegmentCount);
				Assert.AreEqual(1, Directory.GetFiles(_folder, "*.qj").Length);
			}
		}

		[TestMethod]
		public void Ack_OldestSegmentStillLive_KeepsNewerSegments()
		{
			using (var journal = Open(segmentSize: 1))
			{
				journal.Append(Item("old", "{}"));
				journal.Append(Item("a", "{}"));
				journal.Append(Item("b", "{}"));
				journal.Ack("a");
				journal.Ack("b");

				// The acknowledgements of "a" and "b" must survive until "old" is handled.
				Assert.IsTrue(journal.SegmentCount >= 3);
			}

			using (var restarted = new InternalBusJournal(_folder, new SerializeService(), null, Resolve))
			{
				CollectionAssert.AreEqual(new[] { "old" }, restarted.Recover().Select(x => x.Id).ToArray());
			}
		}

		[TestMethod]
		public void Recover_TornTail_IsSkipped()
		{
			using (var journal = Open())
			{
				journal.Append(Item("1", "{}"));
				journal.Append(Item("2", "{}"));
			}
			var segment = Directory.GetFiles(_folder, "*.qj").Single();
			using (var stream = new FileStream(segment, FileMode.Append))
			{
				// Half of a header: the process died in the middle of a write.
				stream.Write(new byte[] { 1, 200, 0, 0 }, 0, 4);
			}

			using (var restarted = new InternalBusJournal(_folder, new SerializeService(), null, Resolve))
			{
				Assert.AreEqual(2, restarted.Recover().Count);
			}
		}

		[TestMethod]
		public void Recover_CorruptedRecord_SkipsTheRestOfSegment()
		{
			using (var journal = Open())
			{
				journal.Append(Item("1", "{}"));
			}
			var segment = Directory.GetFiles(_folder, "*.qj").Single();
			var bytes = File.ReadAllBytes(segment);
			bytes[bytes.Length - 2] ^= 0xFF;
			File.WriteAllBytes(segment, bytes);

			using (var restarted = new InternalBusJournal(_folder, new SerializeService(), null, Resolve))
			{
				Assert.AreEqual(0, restarted.Recover().Count);
			}
		}

		[TestMethod]
		public void Append_ManySegmentsWithStuckMessage_RewritesIt()
		{
			using (var journal = Open(segmentSize: 1))
			{
				journal.Append(Item("stuck", "{\"Id\":42}"));
				for (var i = 0; i < 20; i++)
				{
					journal.Append(Item(i.ToString(), "{}"));
					journal.Ack(i.ToString());
				}

				Assert.IsTrue(journal.SegmentCount <= 10, $"segments: {journal.SegmentCount}");
			}

			using (var restarted = new InternalBusJournal(_folder, new SerializeService(), null, Resolve))
			{
				var items = restarted.Recover();
				Assert.AreEqual(1, items.Count);
				Assert.AreEqual("stuck", items[0].Id);
				Assert.AreEqual("{\"Id\":42}", items[0].Body);
			}
		}

		[TestMethod]
		public void Ack_IsWrittenOnFlush()
		{
			using (var journal = Open())
			{
				journal.Append(Item("1", "{}"));
				journal.Ack("1");

				// Until the flush the acknowledgement is buffered: a crash would handle the message again.
				Assert.AreEqual(1, RecoverCopy().Count);

				journal.Flush();

				Assert.AreEqual(0, RecoverCopy().Count);
			}
		}

		[TestMethod]
		public void Append_WithoutRecover_Throws()
		{
			using (var journal = new InternalBusJournal(_folder, new SerializeService(), null, Resolve))
			{
				Assert.ThrowsException<InvalidOperationException>(() => journal.Append(Item("1", "{}")));
			}
		}

		private InternalBusJournal Open(long segmentSize = InternalBusJournal.DefaultSegmentSize)
		{
			var journal = new InternalBusJournal(_folder, new SerializeService(), null, Resolve, segmentSize);
			journal.Recover();
			return journal;
		}

		private List<InternalBusItem> RecoverCopy()
		{
			// Recovery of a copy: the original segments stay open for writing.
			var copy = _folder + "-copy";
			if (Directory.Exists(copy)) Directory.Delete(copy, true);
			Directory.CreateDirectory(copy);
			foreach (var file in Directory.GetFiles(_folder))
			{
				using (var source = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
				using (var target = File.Create(Path.Combine(copy, Path.GetFileName(file))))
				{
					source.CopyTo(target);
				}
			}
			try
			{
				using (var journal = new InternalBusJournal(copy, new SerializeService(), null, Resolve))
				{
					return journal.Recover();
				}
			}
			finally
			{
				Directory.Delete(copy, true);
			}
		}

		private InternalBusItem Item(string id, string body)
		{
			var item = new InternalBusItem
			{
				Id = id,
				Queue = "orders",
				Body = body,
				Headers = new Dictionary<string, object>()
			};
			_live[id] = item;
			return item;
		}

		private InternalBusItem Resolve(string id)
		{
			return _live.TryGetValue(id, out var item) ? item : null;
		}
	}
}
