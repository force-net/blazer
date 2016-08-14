using System;

namespace Force.Blazer.Algorithms
{
	/// <summary>
	/// Inteface for implementing encoders for Blazer algoritms
	/// </summary>
	public interface IEncoder : IDisposable
	{
		/// <summary>
		/// Encodes given buffer
		/// </summary>
		BufferInfo Encode(byte[] buffer, int offset, int length);

		/// <summary>
		/// Initializes encoder with information about maximum uncompressed block size
		/// </summary>
		void Init(int maxInBlockSize);

		/// <summary>
		/// Returns algorithm id
		/// </summary>
		BlazerAlgorithm GetAlgorithmId();
	}
}
