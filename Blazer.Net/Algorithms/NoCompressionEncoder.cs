namespace Force.Blazer.Algorithms
{
	/// <summary>
	/// Encoder of no compression version of Blazer algorithm
	/// </summary>
	/// <remarks>This is dummy decoder can be used for testing or storing data with Blazer structure</remarks>
	public class NoCompressionEncoder : IEncoder
	{
		/// <summary>
		/// Encodes given buffer
		/// </summary>
		public BufferInfo Encode(byte[] buffer, int offset, int length)
		{
			return new BufferInfo(buffer, offset, length);
		}

		/// <summary>
		/// Initializes encoder with information about maximum uncompressed block size
		/// </summary>
		public void Init(int maxInBlockSize)
		{
		}

		/// <summary>
		/// Returns algorithm id
		/// </summary>
		public BlazerAlgorithm GetAlgorithmId()
		{
			return BlazerAlgorithm.NoCompress;
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		/// <filterpriority>2</filterpriority>
		public void Dispose()
		{
		}
	}
}
