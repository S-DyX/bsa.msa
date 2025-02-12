using System.Threading.Tasks;

namespace Bsa.Msa.Common.Services.MessageHandling
{
    /// <summary>
    /// Common interface
    /// </summary>
    public interface IMessageHandler
    {
    }
    /// <summary>
    /// Common async interface
    /// </summary>
    public interface IMessageHandlerAsync : IMessageHandler
    {
    }
    /// <summary>
    /// Message Handler
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public interface IMessageHandler<in TMessage> : IMessageHandler
    {
        /// <summary>
        /// Handle the message
        /// </summary>
        /// <param name="message"></param>
        void Handle(TMessage message);
    }

    public interface IMessageHandler<in TMessage, out TResponse>
    {
        TResponse Handle(TMessage message);
    }
    /// <summary>
    /// Async message handler
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public interface IMessageHandlerAsync<in TMessage> : IMessageHandlerAsync
    {
        Task HandleAsync(TMessage message);
    }

    //public interface IMessageItemCollection<TValue>
    //{
    //	List<TValue> Items { get; }
    //}

    //public abstract class MessageHandlerBase<TValue, TItems> : IMessageHandler<TValue> where TValue : IMessageItemCollection<TItems>
    //{
    //	public void Handle(TValue message)
    //	{
    //		if (message.Items.Count < 100)
    //		{
    //		}
    //	}
    //}
}
