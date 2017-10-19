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
			encoder.Init(sourceData.Length);
			var res = encoder.Encode(sourceData, 0, sourceData.Length);
			var targetBuf = res.ExtractToSeparateArray(4);
			targetBuf[0] = (byte)sourceData.Length;
			targetBuf[1] = (byte)(sourceData.Length >> 8);
			targetBuf[2] = (byte)(sourceData.Length >> 16);
			targetBuf[3] = (byte)(sourceData.Length >> 24);
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
			encoder.Init(sourceData.Length);
			var res = encoder.Encode(sourceData, 0, sourceData.Length);
			outStream.Write(new[] { (byte)sourceData.Length, (byte)(sourceData.Length >> 8), (byte)(sourceData.Length >> 16), (byte)(sourceData.Length >> 24) }, 0, 4);
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
			var uncomprLength = comprData[0] | (comprData[1] << 8) | (comprData[2] << 16) | (comprData[3] << 24);
			decoder.Init(uncomprLength);
			var decoded = decoder.Decode(comprData, 4, comprData.Length, true);
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
			var uncomprLength = comprData[0] | (comprData[1] << 8) | (comprData[2] << 16) | (comprData[3] << 24);
			decoder.Init(uncomprLength);
			var decoded = decoder.Decode(comprData, 4, comprData.Length, true);
			return new MemoryStream(decoded.Buffer, decoded.Offset, decoded.Count);
		}
	}
}
