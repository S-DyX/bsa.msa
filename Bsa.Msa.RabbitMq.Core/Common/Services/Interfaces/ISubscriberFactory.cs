using Bsa.Msa.Common.Services.MessageHandling;

namespace Bsa.Msa.Common.Services.Interfaces
{
	public interface ISubscriber : IServiceUnit
	{
	}

	/// <summary>
	/// Factory for creating subscribers
	/// </summary>
	public interface ISubscriberFactory
	{
		/// <summary>
		/// Create subscriber
		/// </summary>
		/// <param name="messageHandlerSettings"><see cref="IMessageHandlerSettings"/></param>
		/// <param name="messageHandlerFactory"><see cref="IMessageHandlerFactory"/></param>
		/// <returns></returns>
		ISubscriber Create(IMessageHandlerSettings messageHandlerSettings, IMessageHandlerFactory messageHandlerFactory);

		/// <summary>
		/// Delete queue
		/// </summary>
		/// <param name="messageHandlerSettings"></param>
		void Delete(IMessageHandlerSettings messageHandlerSettings);
	}

	


}
