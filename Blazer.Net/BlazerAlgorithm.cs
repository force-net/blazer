namespace Force.Blazer
{
	/// <summary>
	/// Blazer algorithm
	/// </summary>
	public enum BlazerAlgorithm : byte
	{
		/// <summary>
		/// No compression. Can be used for non-compressible data or for keeping stream stucture
		/// </summary>
		NoCompress = 0,

		/// <summary>
		/// Stream compression. Effective for 'live' streams
		/// </summary>
		Stream = 1,

		/// <summary>
		/// Block compression. Effective for compressing files
		/// </summary>
		Block = 2
	}
}
