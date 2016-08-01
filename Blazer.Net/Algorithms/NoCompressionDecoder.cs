namespace Force.Blazer.Algorithms
{
	public class NoCompressionDecoder : IDecoder
	{
		public BufferInfo Decode(byte[] buffer, int offset, int length, bool isCompressed)
		{
			return new BufferInfo(buffer, offset, length);
		}

		public void Init(int maxUncompressedBlockSize)
		{
		}

		public BlazerAlgorithm GetAlgorithmId()
		{
			return BlazerAlgorithm.NoCompress;
		}

		public void Dispose()
		{
		}
	}
}
