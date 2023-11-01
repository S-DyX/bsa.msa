using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Bsa.Msa.Common.Helpers
{
	//https://github.com/morelinq/MoreLINQ/blob/master/MoreLinq/Batch.cs
	//TODO need optimization
	public static class LinqExtensions
	{
		public static bool FastAny<TSource>(this IList<TSource> source)
		{
			return source?.Count > 0;
		}
		public static IEnumerable<IEnumerable<TSource>> Batch<TSource>(this IEnumerable<TSource> source, int size)
		{
			return Batch(source, size, x => x);
		}

		/// <summary>
		/// Batches the source sequence into sized buckets and applies a projection to each bucket.
		/// </summary>
		/// <typeparam name="TSource">Type of elements in <paramref name="source"/> sequence.</typeparam>
		/// <typeparam name="TResult">Type of result returned by <paramref name="resultSelector"/>.</typeparam>
		/// <param name="source">The source sequence.</param>
		/// <param name="size">Size of buckets.</param>
		/// <param name="resultSelector">The projection to apply to each bucket.</param>
		/// <returns>A sequence of projections on equally sized buckets containing elements of the source collection.</returns>
		/// <remarks>
		/// This operator uses deferred execution and streams its results (buckets and bucket content).
		/// </remarks>

		public static IEnumerable<TResult> Batch<TSource, TResult>(this IEnumerable<TSource> source, int size,
			Func<IEnumerable<TSource>, TResult> resultSelector)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
			if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
			return BatchImpl(source, size, resultSelector);
		}

		public static void Batch<TValue>(this IEnumerable<TValue> items, int size,
			Action<List<TValue>> action)
		{
			int offset = 0;
			var enumerable = items?.ToArray();
			while (enumerable?.Length > offset)
			{
				var curent = enumerable.Skip(offset).Take(size).ToList();
				action.Invoke(curent);
				offset += size;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TValue"></typeparam>
		/// <param name="items"></param>
		/// <param name="size"></param>
		/// <param name="action"></param>
		public static void Batch<TValue>(this List<TValue> items, int size, Action<List<TValue>> action)
		{
			int offset = 0;
			while (items.Count > offset)
			{
				var curent = items.Skip(offset).Take(size).ToList();
				action.Invoke(curent);
				offset += size;
			}
		}
		public static IEnumerable<TResult> BatchParallel<TInput, TResult>(this IEnumerable<TInput> inputList,
			Func<TInput, TResult> func, int bathCount)
		{
			var resultList = new ConcurrentBag<TResult>();
			Task.WaitAll(inputList.Batch(bathCount).Select(batchUrls => Task.Factory.StartNew(() =>
			{
				batchUrls.ToList().ForEach(x =>
				{
					var result = func.Invoke(x);
					if (result != null)
					{
						resultList.Add(result);
					}
				});
			})).ToArray());

			return resultList;
		}

		public static IEnumerable<TResult> BatchParallel<TInput, TResult>(this IEnumerable<TInput> inputList, Func<IEnumerable<TInput>, IEnumerable<TResult>> func, int bathCount)
		{
			var resultList = new ConcurrentBag<TResult>();
			Task.WaitAll(inputList.Batch(bathCount).Select(batchUrls => Task.Factory.StartNew(() =>
			{
				foreach (var result in func.Invoke(batchUrls.ToList()))
				{
					if (result != null)
					{
						resultList.Add(result);
					}
				}

			})).ToArray());

			return resultList;
		}




		private static IEnumerable<TResult> BatchImpl<TSource, TResult>(this IEnumerable<TSource> source, int size,
			Func<IEnumerable<TSource>, TResult> resultSelector)
		{
			Debug.Assert(source != null);
			Debug.Assert(size > 0);
			Debug.Assert(resultSelector != null);

			TSource[] bucket = null;
			var count = 0;

			foreach (var item in source)
			{
				if (bucket == null)
				{
					bucket = new TSource[size];
				}

				bucket[count++] = item;

				// The bucket is fully buffered before it's yielded
				if (count != size)
				{
					continue;
				}

				// Select is necessary so bucket contents are streamed too
				yield return resultSelector(bucket.Select(x => x));

				bucket = null;
				count = 0;
			}

			// Return the last bucket with all remaining elements
			if (bucket != null && count > 0)
			{
				yield return resultSelector(bucket.Take(count));
			}
		}
	}


}
