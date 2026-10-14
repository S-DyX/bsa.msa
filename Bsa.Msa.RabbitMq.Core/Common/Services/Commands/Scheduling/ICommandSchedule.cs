using System;

namespace Bsa.Msa.Common.Services.Commands.Scheduling
{
	/// <summary>
	/// Calculates run times of a command. All times are local.
	/// </summary>
	public interface ICommandSchedule
	{
		/// <summary>
		/// Returns the first run time that is not earlier
		/// than <paramref name="notBefore"/>.
		/// </summary>
		/// <param name="notBefore">Lower bound, local time.</param>
		/// <returns>
		/// Next run time, or <c>null</c> if there are no more runs.
		/// </returns>
		DateTime? GetNextRun(DateTime notBefore);
	}
}
