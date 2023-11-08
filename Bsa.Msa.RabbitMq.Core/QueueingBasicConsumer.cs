using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Util;

namespace Bsa.Msa.RabbitMq.Core
{
	public interface IQueueingBasicConsumer
	{
		void HandleBasicDeliver(
			string consumerTag,
			ulong deliveryTag,
			bool redelivered,
			string exchange,
			string routingKey,
			IBasicProperties properties,
			byte[] body);

		void OnCancel();

		SharedQueue<BasicDeliverEventArgs> Queue { get; }
	}
	/// <summary>
	/// A <see cref="T:RabbitMQ.Client.IBasicConsumer" /> implementation that
	/// uses a <see cref="T:RabbitMQ.Util.SharedQueue" /> to buffer incoming deliveries.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Received messages are placed in the SharedQueue as instances
	/// of <see cref="T:RabbitMQ.Client.Events.BasicDeliverEventArgs" />.
	/// </para>
	/// <para>
	/// Note that messages taken from the SharedQueue may need
	/// acknowledging with <see cref="M:RabbitMQ.Client.IModel.BasicAck(System.UInt64,System.Boolean)" />.
	/// </para>
	/// <para>
	/// When the consumer is closed, through BasicCancel or through
	/// the shutdown of the underlying <see cref="T:RabbitMQ.Client.IModel" /> or <see cref="T:RabbitMQ.Client.IConnection" />,
	///  the  <see cref="M:RabbitMQ.Util.SharedQueue`1.Close" /> method is called, which causes any
	/// Enqueue() operations, and Dequeue() operations when the queue
	/// is empty, to throw EndOfStreamException (see the comment for <see cref="M:RabbitMQ.Util.SharedQueue`1.Close" />).
	/// </para>
	/// <para>
	/// The following is a simple example of the usage of this class:
	/// </para>
	/// <example><code>
	/// IModel channel = ...;
	/// QueueingBasicConsumer consumer = new QueueingBasicConsumer(channel);
	/// channel.BasicConsume(queueName, null, consumer);
	/// 
	/// // At this point, messages will be being asynchronously delivered,
	/// // and will be queueing up in consumer.Queue.
	/// 
	/// while (true) {
	///     try {
	///         BasicDeliverEventArgs e = (BasicDeliverEventArgs) consumer.Queue.Dequeue();
	///         // ... handle the delivery ...
	///         channel.BasicAck(e.DeliveryTag, false);
	///     } catch (EndOfStreamException ex) {
	///         // The consumer was cancelled, the model closed, or the
	///         // connection went away.
	///         break;
	///     }
	/// }
	/// </code></example>
	/// </remarks> 
	public class QueueingBasicConsumer : DefaultBasicConsumer
	{

		/// <summary>
		/// Creates a fresh <see cref="T:RabbitMQ.Client.QueueingBasicConsumer" />, with <see cref="P:RabbitMQ.Client.DefaultBasicConsumer.Model" />
		///  set to the argument, and <see cref="P:RabbitMQ.Client.QueueingBasicConsumer.Queue" /> set to a fresh <see cref="T:RabbitMQ.Util.SharedQueue" />.
		/// </summary>
		public QueueingBasicConsumer(IModel model)
		  : this(model, new SharedQueue<BasicDeliverEventArgs>())
		{
		}

		/// <summary>
		/// Creates a fresh <see cref="T:RabbitMQ.Client.QueueingBasicConsumer" />,
		///  initialising the <see cref="P:RabbitMQ.Client.DefaultBasicConsumer.Model" />
		///  and <see cref="P:RabbitMQ.Client.QueueingBasicConsumer.Queue" /> properties to the given values.
		/// </summary>
		public QueueingBasicConsumer(IModel model, SharedQueue<BasicDeliverEventArgs> queue)
		  : base(model)
		{
			this.Queue = queue;
		}

		/// <summary>
		/// Retrieves the <see cref="T:RabbitMQ.Util.SharedQueue" /> that messages arrive on.
		/// </summary>
		public SharedQueue<BasicDeliverEventArgs> Queue { get; protected set; }


		///<summary>
		/// Invoked when a delivery arrives for the consumer.
		/// </summary>
		/// <remarks>
		/// Handlers must copy or fully use delivery body before returning.
		/// Accessing the body at a later point is unsafe as its memory can
		/// be already released.
		/// </remarks>
		public override void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IBasicProperties properties, ReadOnlyMemory<byte> body)
		{
			base.HandleBasicDeliver(consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body);
			this.Queue.Enqueue(new BasicDeliverEventArgs()
			{
				ConsumerTag = consumerTag,
				DeliveryTag = deliveryTag,
				Redelivered = redelivered,
				Exchange = exchange,
				RoutingKey = routingKey,
				BasicProperties = properties,
				Body = body
			});
		}


		/// <summary>
		/// Overrides <see cref="T:RabbitMQ.Client.DefaultBasicConsumer" />'s OnCancel implementation,
		///  extending it to call the Close() method of the <see cref="T:RabbitMQ.Util.SharedQueue" />.
		/// </summary>
		public void OnCancel()
		{
			base.OnCancel();
			this.Queue.Close();
		}
	}

	public class SharedQueue : SharedQueue<object>
	{
	}


	///<summary>A thread-safe shared queue implementation.</summary>
	public class SharedQueue<T> : IEnumerable<T>
	{
		///<summary>Flag holding our current status.</summary>
		protected bool m_isOpen = true;

		///<summary>The shared queue.</summary>
		///<remarks>
		///Subclasses must ensure appropriate locking discipline when
		///accessing this field. See the implementation of Enqueue,
		///Dequeue.
		///</remarks>
		protected Queue<T> m_queue = new Queue<T>();

#if NETFX_CORE || NET4
        protected Queue<TaskCompletionSource<T>> m_waiting = new Queue<TaskCompletionSource<T>>();
#endif

		///<summary>Close the queue. Causes all further Enqueue()
		///operations to throw EndOfStreamException, and all pending
		///or subsequent Dequeue() operations to throw an
		///EndOfStreamException once the queue is empty.</summary>
		public void Close()
		{
			lock (m_queue)
			{
				m_isOpen = false;
				Monitor.PulseAll(m_queue);
#if NETFX_CORE
                // let all waiting tasks know we just closed by passing them an exception
                if (m_waiting.Count > 0) 
                {
                    try 
                    {
                        this.EnsureIsOpen();
                    }
                    catch (Exception ex) 
                    {
                        foreach (var tcs in m_waiting) 
                        {
                            tcs.TrySetException(ex);
                        }
                    }
                }
#endif
			}
		}

		///<summary>Retrieve the first item from the queue, or block if none available</summary>
		///<remarks>
		///Callers of Dequeue() will block if no items are available
		///until some other thread calls Enqueue() or the queue is
		///closed. In the latter case this method will throw
		///EndOfStreamException.
		///</remarks>
		public T Dequeue()
		{
			lock (m_queue)
			{
				while (m_queue.Count == 0)
				{
					EnsureIsOpen();
					Monitor.Wait(m_queue);
				}
				return m_queue.Dequeue();
			}
		}

#if NETFX_CORE || NET4
        /// <summary>
        /// Asynchronously retrieves the first item from the queue.
        /// </summary>
        public Task<T> DequeueAsync() 
        {
            lock (m_queue) 
            {
                EnsureIsOpen();
                if (m_queue.Count > 0)
                {
                    return Task.FromResult(this.Dequeue());
                }
                else 
                {
                    var tcs = new TaskCompletionSource<T>();
                    m_waiting.Enqueue(tcs);
                    return tcs.Task;
                }
            }
        }
#endif

		///<summary>Retrieve the first item from the queue, or return
		///nothing if no items are available after the given
		///timeout</summary>
		///<remarks>
		///<para>
		/// If one or more items are present on the queue at the time
		/// the call is made, the call will return
		/// immediately. Otherwise, the calling thread blocks until
		/// either an item appears on the queue, or
		/// millisecondsTimeout milliseconds have elapsed.
		///</para>
		///<para>
		/// Returns true in the case that an item was available before
		/// the timeout, in which case the out parameter "result" is
		/// set to the item itself.
		///</para>
		///<para>
		/// If no items were available before the timeout, returns
		/// false, and sets "result" to null.
		///</para>
		///<para>
		/// A timeout of -1 (i.e. System.Threading.Timeout.Infinite)
		/// will be interpreted as a command to wait for an
		/// indefinitely long period of time for an item to become
		/// available. Usage of such a timeout is equivalent to
		/// calling Dequeue() with no arguments. See also the MSDN
		/// documentation for
		/// System.Threading.Monitor.Wait(object,int).
		///</para>
		///<para>
		/// If no items are present and the queue is in a closed
		/// state, or if at any time while waiting the queue
		/// transitions to a closed state (by a call to Close()), this
		/// method will throw EndOfStreamException.
		///</para>
		///</remarks>
		public bool Dequeue(int millisecondsTimeout, out T result)
		{
			if (millisecondsTimeout == Timeout.Infinite)
			{
				result = Dequeue();
				return true;
			}

			DateTime startTime = DateTime.Now;
			lock (m_queue)
			{
				while (m_queue.Count == 0)
				{
					EnsureIsOpen();
					var elapsedTime = (int)((DateTime.Now - startTime).TotalMilliseconds);
					int remainingTime = millisecondsTimeout - elapsedTime;
					if (remainingTime <= 0)
					{
						result = default(T);
						return false;
					}

					Monitor.Wait(m_queue, remainingTime);
				}

				result = m_queue.Dequeue();
				return true;
			}
		}

		///<summary>Retrieve the first item from the queue, or return
		///defaultValue immediately if no items are
		///available</summary>
		///<remarks>
		///<para>
		/// If one or more objects are present in the queue at the
		/// time of the call, the first item is removed from the queue
		/// and returned. Otherwise, the defaultValue that was passed
		/// in is returned immediately. This defaultValue may be null,
		/// or in cases where null is part of the range of the queue,
		/// may be some other sentinel object. The difference between
		/// DequeueNoWait() and Dequeue() is that DequeueNoWait() will
		/// not block when no items are available in the queue,
		/// whereas Dequeue() will.
		///</para>
		///<para>
		/// If at the time of call the queue is empty and in a
		/// closed state (following a call to Close()), then this
		/// method will throw EndOfStreamException.
		///</para>
		///</remarks>
		public T DequeueNoWait(T defaultValue)
		{
			lock (m_queue)
			{
				if (m_queue.Count == 0)
				{
					EnsureIsOpen();
					return defaultValue;
				}
				else
				{
					return m_queue.Dequeue();
				}
			}
		}

		///<summary>Place an item at the end of the queue.</summary>
		///<remarks>
		///If there is a thread waiting for an item to arrive, the
		///waiting thread will be woken, and the newly Enqueued item
		///will be passed to it. If the queue is closed on entry to
		///this method, EndOfStreamException will be thrown.
		///</remarks>
		public void Enqueue(T o)
		{
			lock (m_queue)
			{
				EnsureIsOpen();

#if NETFX_CORE
                while (m_waiting.Count > 0)
                {
                    var tcs = m_waiting.Dequeue();
                    if (tcs != null && tcs.TrySetResult(o)) 
                    {
                        // We successfully set a task return result, so
                        // no need to Enqueue or Monitor.Pulse
                        return;
                    }
                }
#endif

				m_queue.Enqueue(o);
				Monitor.Pulse(m_queue);
			}
		}

		///<summary>Implementation of the IEnumerable interface, for
		///permitting SharedQueue to be used in foreach
		///loops.</summary>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return new SharedQueueEnumerator<T>(this);
		}

		///<summary>Implementation of the IEnumerable interface, for
		///permitting SharedQueue to be used in foreach
		///loops.</summary>
		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return new SharedQueueEnumerator<T>(this);
		}

		///<summary>Call only when the lock on m_queue is held.</summary>
		/// <exception cref="EndOfStreamException" />
		private void EnsureIsOpen()
		{
			if (!m_isOpen)
			{
				throw new EndOfStreamException("SharedQueue closed");
			}
		}
	}


	///<summary>Implementation of the IEnumerator interface, for
	///permitting SharedQueue to be used in foreach loops.</summary>
	public struct SharedQueueEnumerator<T> : IEnumerator<T>
	{
		private readonly SharedQueue<T> m_queue;
		private T m_current;

		///<summary>Construct an enumerator for the given
		///SharedQueue.</summary>
		public SharedQueueEnumerator(SharedQueue<T> queue)
		{
			m_queue = queue;
			m_current = default(T);
		}

		object IEnumerator.Current
		{
			get
			{
				if (m_current == null)
				{
					throw new InvalidOperationException();
				}
				return m_current;
			}
		}

		T IEnumerator<T>.Current
		{
			get
			{
				if (m_current == null)
				{
					throw new InvalidOperationException();
				}
				return m_current;
			}
		}

		public void Dispose()
		{
		}

		bool IEnumerator.MoveNext()
		{
			try
			{
				m_current = m_queue.Dequeue();
				return true;
			}
			catch (EndOfStreamException)
			{
				m_current = default(T);
				return false;
			}
		}

		///<summary>Reset()ting a SharedQueue doesn't make sense, so
		///this method always throws
		///InvalidOperationException.</summary>
		void IEnumerator.Reset()
		{
			throw new InvalidOperationException("SharedQueue.Reset() does not make sense");
		}
	}
}
