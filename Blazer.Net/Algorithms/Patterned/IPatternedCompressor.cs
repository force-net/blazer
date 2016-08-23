namespace Force.Blazer.Algorithms.Patterned
{
	/// <summary>
	/// Common interface for Pattened Encoder/Decoder
	/// </summary>
	public interface IPatternedCompressor
	{
		/// <summary>
		/// Calculates max compressed buffer size for specified uncompressed data length
		/// </summary>
		int CalculateMaxCompressedBufferLength(int uncompressedLength);

		/// <summary>
		/// Prepares pattern. Should be called only once for pattern
		/// </summary>
		void PreparePattern(byte[] pattern);

		/// <summary>
		/// Prepares pattern. Should be called only once for pattern
		/// </summary>
		void PreparePattern(byte[] pattern, int offset, int length);

		/// <summary>
		/// Encodes data with prepared pattern
		/// </summary>
		int EncodeWithPattern(byte[] bufferIn, int offsetIn, int countIn, byte[] bufferOut, int offsetOut);

		/// <summary>
		/// Decodes data with prepared pattern
		/// </summary>
		int DecodeWithPattern(byte[] bufferIn, int offsetIn, int countIn, byte[] bufferOut, int offsetOut);

		/// <summary>
		/// Encodes data with prepared pattern
		/// </summary>
		byte[] EncodeWithPattern(byte[] buffer, int offset, int count);

		/// <summary>
		/// Encodes data with prepared pattern
		/// </summary>
		byte[] EncodeWithPattern(byte[] buffer);

		/// <summary>
		/// Decodes data with prepared pattern
		/// </summary>
		byte[] DecodeWithPattern(byte[] buffer, int offset, int count);

		/// <summary>
		/// Decodes data with prepared pattern
		/// </summary>
		byte[] DecodeWithPattern(byte[] buffer);
	}
}
