namespace Bsa.Msa.Common.Services.MessageHandling
{
	/// <summary>
	/// How the local queue stores messages that are acknowledged to the broker before they are handled
	/// (handlers with degreeOfParallelism &gt; 1).
	/// </summary>
	public enum InternalQueueFormat
	{
		/// <summary>
		/// A file per message (<c>InternalQueue/&lt;md5&gt;/&lt;id&gt;.q</c>), the format of earlier versions.
		/// </summary>
		Files = 0,

		/// <summary>
		/// Append-only journal segments (<c>InternalQueue/&lt;md5&gt;/*.qj</c>): one write per message
		/// instead of creating and deleting a file. Files of the old format are still read and handled.
		/// </summary>
		Journal = 1
	}
}
