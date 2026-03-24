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
		/// <param name="message"><see cref="TMessage"/></param>
		void Handle(TMessage message);
	}

	/// <summary>
	/// message handler with <see cref="TResponse"/>
	/// </summary>
	/// <typeparam name="TMessage"></typeparam>
	/// <typeparam name="TResponse"></typeparam>
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
		/// <summary>
		/// Handle the message
		/// </summary>
		/// <param name="message"><see cref="TMessage"/></param>
		/// <returns></returns>
		Task HandleAsync(TMessage message);
	}

}
