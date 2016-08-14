using System;

namespace Force.Blazer.Algorithms
{
	/// <summary>
	/// Inteface for implementing decoders for Blazer algoritms
	/// </summary>
	public interface IDecoder : IDisposable
	{
		/// <summary>
		/// Decodes given buffer
		/// </summary>
		BufferInfo Decode(byte[] buffer, int offset, int length, bool isCompressed);

		/// <summary>
		/// Initializes decoder with information about maximum uncompressed block size
		/// </summary>
		void Init(int maxUncompressedBlockSize);

		/// <summary>
		/// Returns algorithm id
		/// </summary>
		BlazerAlgorithm GetAlgorithmId();
	}
}
