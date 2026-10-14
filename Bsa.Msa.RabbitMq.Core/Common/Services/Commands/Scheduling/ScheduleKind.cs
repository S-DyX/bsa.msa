namespace Bsa.Msa.Common.Services.Commands.Scheduling
{
	/// <summary>
	/// Kind of a command schedule (the <c>schedule</c> setting).
	/// </summary>
	public enum ScheduleKind
	{
		/// <summary>
		/// Runs every <c>period</c>, optionally only inside
		/// a daily time window (<c>from</c> - <c>to</c>).
		/// </summary>
		Interval,

		/// <summary>
		/// Runs every day at the given times of day (<c>at</c>).
		/// </summary>
		Daily,

		/// <summary>
		/// Runs on the given days of week (<c>days</c>)
		/// at the given times of day (<c>at</c>).
		/// </summary>
		Weekly,

		/// <summary>
		/// Runs on the given days of month (<c>daysOfMonth</c>)
		/// at the given times of day (<c>at</c>).
		/// </summary>
		Monthly
	}
}
