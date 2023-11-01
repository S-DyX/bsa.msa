using System;
using System.Collections.Generic;

namespace Bsa.Msa.Common.Messages
{
	/// <summary>
	/// Интерфейс описывающий работу с сообщениями
	/// </summary>
	public interface IMessagingGateway
	{
		/// <summary>
		/// Отправка сообщения
		/// </summary>
		/// <param name="publicationEndpoint"></param>
		/// <param name="message"></param>
		void Send<TMessage>(string publicationEndpoint, TMessage message);

		/// <summary>
		/// Получение сообщения
		/// </summary>
		/// <param name="subscriptionEndpoint"></param>
		/// <param name="count"></param>
		List<TMessage> Receive<TMessage>(string subscriptionEndpoint, int count);

		void Handle<TMessage>(string subscriptionEndpoint, Action<IEnumerable<TMessage>> handleAction);

	}
}
