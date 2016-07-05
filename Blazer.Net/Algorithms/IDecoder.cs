using System;

namespace Force.Blazer.Algorithms
{
	public interface IDecoder : IDisposable
	{
		int Read(byte[] buffer, int offset, int count);

		void Init(int maxUncompressedBlockSize, Func<byte[], Tuple<int, byte, bool>> getNextBlock);

		BlazerAlgorithm GetAlgorithmId();
	}
}
