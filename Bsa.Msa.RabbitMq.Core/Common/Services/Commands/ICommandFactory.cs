using System.Threading;
using Bsa.Msa.Common.Settings;

namespace Bsa.Msa.Common.Services.Commands
{
	public interface ICommandFactory
	{

		ICommand Create(string commandType, ISettings settings, CancellationToken cancellationToken);
	}

	
}
