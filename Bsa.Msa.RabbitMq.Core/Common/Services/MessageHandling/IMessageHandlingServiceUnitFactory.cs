using Bsa.Msa.Common.Services.Interfaces;
using Bsa.Msa.Common.Services.Settings;

namespace Bsa.Msa.Common.Services.MessageHandling
{
	public interface IMessageHandlingServiceUnitFactory
	{
		IServiceUnit Create(string type, IServiceUnitSettings settings);
	}

	//public class MessageHandlingServiceUnitFactory : IMessageHandlingServiceUnitFactory
	//{
	//	public IServiceUnit Create(IServiceUnitSettings settings, string type)
	//	{
	//		throw new NotImplementedException();
	//	}
	//}
}
