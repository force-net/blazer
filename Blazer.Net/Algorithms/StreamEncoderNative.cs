using System.Runtime.InteropServices;

namespace Force.Blazer.Algorithms
{
	public class StreamEncoderNative : StreamEncoder
	{
		[DllImport(@"Blazer.Native.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern int blazer_stream_compress_block(
			byte[] bufferIn, int bufferInOffset, int bufferInLength, int globalOffset, byte[] bufferOut, int bufferOutOffset, int[] hashArr);

		protected override int GetAdditionalInSize()
		{
			return 8;
		}

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
