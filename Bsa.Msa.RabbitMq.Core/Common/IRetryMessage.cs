namespace Bsa.Msa.Common
{
	/// <summary>
	/// Represents a message that supports retry attempts, typically used in messaging scenarios
	/// where a message may fail processing and needs to be retried.
	/// </summary>
	public interface IRetryMessage
	{
		/// <summary>
		/// Gets or sets the number of times this message has been retried.
		/// A value of 0 typically indicates the first attempt, while higher values indicate
		/// subsequent retry attempts.
		/// </summary>
		int RetryCount { get; set; }
	}
}
