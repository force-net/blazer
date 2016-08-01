namespace Force.Blazer.Algorithms
{
	public class NoCompressionEncoder : IEncoder
	{
		public BufferInfo Encode(byte[] buffer, int offset, int length)
		{
			return new BufferInfo(buffer, offset, length);
		}

		public void Init(int maxInBlockSize)
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
