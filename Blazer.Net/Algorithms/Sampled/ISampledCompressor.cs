namespace Force.Blazer.Algorithms.Sampled
{
	/// <summary>
	/// Common interface for Sampled Encoder/Decoder
	/// </summary>
	public interface ISampledCompressor
	{
		/// <summary>
		/// Calculates max compressed buffer size for specified uncompressed data length
		/// </summary>
		int CalculateMaxCompressedBufferLength(int uncompressedLength);

		/// <summary>
		/// Prepares sample. Should be called only once for sample
		/// </summary>
		void PrepareSample(byte[] sample, int offset, int length);

		/// <summary>
		/// Encodes data with prepared sample
		/// </summary>
		int EncodeWithSample(byte[] bufferIn, int offsetIn, int countIn, byte[] bufferOut, int offsetOut);

		/// <summary>
		/// Decodes data with prepared sample
		/// </summary>
		int DecodeWithSample(byte[] bufferIn, int offsetIn, int countIn, byte[] bufferOut, int offsetOut);

		/// <summary>
		/// Encodes data with prepared sample
		/// </summary>
		byte[] EncodeWithSample(byte[] buffer, int offset, int count);

		/// <summary>
		/// Encodes data with prepared sample
		/// </summary>
		byte[] EncodeWithSample(byte[] buffer);

		/// <summary>
		/// Decodes data with prepared sample
		/// </summary>
		byte[] DecodeWithSample(byte[] buffer, int offset, int count);

		/// <summary>
		/// Decodes data with prepared sample
		/// </summary>
		byte[] DecodeWithSample(byte[] buffer);
	}
}
