namespace Bsa.Msa.Common.Services.Sources
{
	/// <summary>
	/// Kind of a service unit.
	/// </summary>
	public enum ServiceUnitKind
	{
		/// <summary>
		/// Message handler (queue subscriber).
		/// </summary>
		Handler,

		/// <summary>
		/// Command run by schedule.
		/// </summary>
		Command
	}
}
