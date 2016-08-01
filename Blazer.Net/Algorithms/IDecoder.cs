using System;

namespace Force.Blazer.Algorithms
{
	public interface IDecoder : IDisposable
	{
		BufferInfo Decode(byte[] buffer, int offset, int length, bool isCompressed);

		void Init(int maxUncompressedBlockSize);

		BlazerAlgorithm GetAlgorithmId();
	}
}
