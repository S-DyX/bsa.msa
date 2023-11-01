namespace Bsa.Msa.Common.Services.MessageHandling
{
	/// <summary>
	/// Для простоты поиска
	/// </summary>
	public interface IMessageHandler
	{
	}
	/// <summary>
	/// Интерфейс для работы с сообщениями
	/// </summary>
	/// <typeparam name="TMessage"></typeparam>
	public interface IMessageHandler<in TMessage> : IMessageHandler
	{
		/// <summary>
		/// Обработать сообщение
		/// </summary>
		/// <param name="message"></param>
		void Handle(TMessage message);
	}

	public interface IMessageHandler<in TMessage, out TResponse>
	{
		TResponse Handle(TMessage message);
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
