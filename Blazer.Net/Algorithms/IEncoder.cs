using System;

namespace Force.Blazer.Algorithms
{
	public interface IEncoder : IDisposable
	{
		BufferInfo Encode(byte[] buffer, int offset, int length);

		void Init(int maxInBlockSize);

		BlazerAlgorithm GetAlgorithmId();
	}
}
