using System;

namespace Force.Blazer.Algorithms
{
	public interface IEncoder : IDisposable
	{
		void Write(byte[] buffer, int offset, int count);

		void CompressAndWrite();

		void Init(int maxInBlockSize, int additionalHeaderSizeForOut, Action<byte[], int, byte> onBlockPrepared);

		BlazerAlgorithm GetAlgorithmId();
	}
}
