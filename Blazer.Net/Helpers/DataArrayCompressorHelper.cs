using System.IO;

using Force.Blazer.Algorithms;

namespace Force.Blazer.Helpers
{
	/// <summary>
	/// Helper class for creating compressed arrays with data with length prefix
	/// Can be useful for resources preparation
	/// No algorithm information is stored
	/// </summary>
	public static class DataArrayCompressorHelper
	{
		/// <summary>
		/// Compresses source data with specified encoder
		/// </summary>
		/// <param name="sourceData">array of source data</param>
		/// <param name="encoder">selected encoder</param>
		/// <returns>array of compressed data</returns>
		public static byte[] CompressDataToArray(byte[] sourceData, IEncoder encoder)
		{
			return CompressDataToArray(sourceData, 0, sourceData.Length, encoder);
		}

		/// <summary>
		/// Compresses source data with specified encoder
		/// </summary>
		/// <param name="sourceData">array of source data</param>
		/// <param name="offset">offset of array of source data</param>
		/// <param name="count">count data to read from array of source data</param>
		/// <param name="encoder">selected encoder</param>
		/// <returns>array of compressed data</returns>
		public static byte[] CompressDataToArray(byte[] sourceData, int offset, int count, IEncoder encoder)
		{
			encoder.Init(sourceData.Length);
			var res = encoder.Encode(sourceData, offset, offset + count);
			var targetBuf = res.ExtractToSeparateArray(4);
			targetBuf[0] = (byte)count;
			targetBuf[1] = (byte)(count >> 8);
			targetBuf[2] = (byte)(count >> 16);
			targetBuf[3] = (byte)(count >> 24);
			return targetBuf;
		}

		/// <summary>
		/// Compresses source data with specified encoder and writes to stream
		/// </summary>
		/// <param name="sourceData">array of source data</param>
		/// <param name="encoder">selected encoder</param>
		/// <param name="outStream">stream to write data</param>
		public static void CompressDataToArrayAndWriteToStream(byte[] sourceData, IEncoder encoder, Stream outStream)
		{
			CompressDataToArrayAndWriteToStream(sourceData, 0, sourceData.Length, encoder, outStream);
		}

		/// <summary>
		/// Compresses source data with specified encoder and writes to stream
		/// </summary>
		/// <param name="sourceData">array of source data</param>
		/// <param name="offset">offset of array of source data</param>
		/// <param name="count">count data to read from array of source data</param>
		/// <param name="encoder">selected encoder</param>
		/// <param name="outStream">stream to write data</param>
		public static void CompressDataToArrayAndWriteToStream(byte[] sourceData, int offset, int count, IEncoder encoder, Stream outStream)
		{
			encoder.Init(sourceData.Length);
			var res = encoder.Encode(sourceData, offset, offset + count);
			outStream.Write(new[] { (byte)count, (byte)(count >> 8), (byte)(count >> 16), (byte)(count >> 24) }, 0, 4);
			outStream.Write(res.Buffer, res.Offset, res.Count);
		}

		/// <summary>
		/// Decompresses data with specified decoder
		/// </summary>
		/// <param name="comprData">array of compressed data with length prefix</param>
		/// <param name="decoder">selected decoder</param>
		/// <returns>array of compressed data</returns>
		public static byte[] DecompressDataArray(byte[] comprData, IDecoder decoder)
		{
			return DecompressDataArray(comprData, 0, comprData.Length, decoder);
		}

		/// <summary>
		/// Decompresses data with specified decoder
		/// </summary>
		/// <param name="comprData">array of compressed data with length prefix</param>
		/// <param name="offset">offset of array of source data</param>
		/// <param name="count">count data to read from array of source data</param>
		/// <param name="decoder">selected decoder</param>
		/// <returns>array of compressed data</returns>
		public static byte[] DecompressDataArray(byte[] comprData, int offset, int count, IDecoder decoder)
		{
			var uncomprLength = comprData[offset] | (comprData[offset + 1] << 8) | (comprData[offset + 2] << 16) | (comprData[offset + 3] << 24);
			decoder.Init(uncomprLength);
			var decoded = decoder.Decode(comprData, offset + 4, offset + count, true);
			return decoded.ExtractToSeparateArray();
		}

		/// <summary>
		/// Decompresses data with specified decoder into stream
		/// </summary>
		/// <param name="comprData">array of compressed data with length prefix</param>
		/// <param name="decoder">selected decoder</param>
		/// <returns>Stream with uncompressed data</returns>
		public static Stream DecompressDataArrayToReadableStream(byte[] comprData, IDecoder decoder)
		{
			return DecompressDataArrayToReadableStream(comprData, 0, comprData.Length, decoder);
		}

		/// <summary>
		/// Decompresses data with specified decoder into stream
		/// </summary>
		/// <param name="comprData">array of compressed data with length prefix</param>
		/// <param name="offset">offset of array of source data</param>
		/// <param name="count">count data to read from array of source data</param>
		/// <param name="decoder">selected decoder</param>
		/// <returns>Stream with uncompressed data</returns>
		public static Stream DecompressDataArrayToReadableStream(byte[] comprData, int offset, int count, IDecoder decoder)
		{
			var uncomprLength = comprData[offset] | (comprData[offset + 1] << 8) | (comprData[offset + 2] << 16) | (comprData[offset + 3] << 24);
			decoder.Init(uncomprLength);
			var decoded = decoder.Decode(comprData, offset + 4, offset + count, true);
			return new MemoryStream(decoded.Buffer, decoded.Offset, decoded.Count);
		}
	}
}
