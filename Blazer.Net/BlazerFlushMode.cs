namespace Force.Blazer
{
	/// <summary>
	/// Flush variants for stream
	/// </summary>
	public enum BlazerFlushMode
	{
		/// <summary>
		/// Ignore all flush requests  (default variant)
		/// </summary>
		IgnoreFlush = 0,

		/// <summary>
		/// Respect flush requests
		/// </summary>
		RespectFlush = 1,

		/// <summary>
		/// Auto flush on every write
		/// </summary>
		AutoFlush = 2,

		/// <summary>
		/// Try to determine, when flush should be performed
		/// </summary>
		SmartFlush = 3
	}
}
