using System.Runtime.InteropServices;

namespace Force.Blazer.Algorithms
{
	/// <summary>
	/// Native implementation of Stream version encoder of Blazer algorithm
	/// </summary>
	/// <remarks>Stream version is good for 'live' streamss, slightly slower than Block, but support stream flushing without
	/// losing compression rate and has very fast decoder</remarks>
	public class StreamEncoderNative : StreamEncoder
	{
		[DllImport(@"Blazer.Native.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern int blazer_stream_compress_block(
			byte[] bufferIn, int bufferInOffset, int bufferInLength, int globalOffset, byte[] bufferOut, int bufferOutOffset, int[] hashArr);

		/// <summary>
		/// Returns additional size for inner buffers. Can be used to store some data or for optimiations
		/// </summary>
		/// <returns>Size in bytes</returns>
		protected override int GetAdditionalInSize()
		{
			return 8;
		}

		/// <summary>
		/// Compresses block of data. See <see cref="StreamEncoder.CompressBlockExternal"/> for details
		/// </summary>
		protected override int CompressBlock(
			byte[] bufferIn,
			int bufferInOffset,
			int bufferInLength,
			int bufferInShift,
			byte[] bufferOut,
			int bufferOutOffset)
		{
			return blazer_stream_compress_block(
				bufferIn,
				bufferInOffset,
				bufferInLength,
				bufferInShift,
				bufferOut,
				bufferOutOffset,
				_hashArr);
		}
	}
}
