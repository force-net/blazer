namespace Force.Blazer.Algorithms
{
	/// <summary>
	/// Decoder of no compression version of Blazer algorithm
	/// </summary>
	/// <remarks>This is dummy decoder can be used for testing or storing data with Blazer structure</remarks>
	public class NoCompressionDecoder : IDecoder
	{
		/// <summary>
		/// Decodes given buffer
		/// </summary>
		public BufferInfo Decode(byte[] buffer, int offset, int length, bool isCompressed)
		{
			return new BufferInfo(buffer, offset, length);
		}

		/// <summary>
		/// Initializes decoder with information about maximum uncompressed block size
		/// </summary>
		public void Init(int maxUncompressedBlockSize)
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
