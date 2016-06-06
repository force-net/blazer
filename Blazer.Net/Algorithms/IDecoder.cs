using System;

namespace Force.Blazer.Algorithms
{
	public interface IDecoder : IDisposable
	{
		int Read(byte[] buffer, int offset, int count);

		void ProcessBlock(byte[] inBuffer, int length, bool isCompressed);

		void Init(int maxUncompressedBlockSize, Func<bool> getNextBlock);

		BlazerAlgorithm GetAlgorithmId();
	}
}
