using System;
using System.Runtime.InteropServices;

namespace Force.Blazer.Algorithms
{
	public class StreamDecoderNative : StreamDecoder
	{
		[DllImport(@"Blazer.Native.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern int blazer_stream_decompress_block(
			byte[] bufferIn, int bufferInOffset, int bufferInLength, byte[] bufferOut, int bufferOutOffset, int bufferOutLength);

		public override void Init(int maxUncompressedBlockSize)
		{
			// +8 for better copying speed. allow dummy copy by 8 bytes 
			base.Init(maxUncompressedBlockSize + 8);
		}

		public override int DecompressBlock(
			byte[] bufferIn, int bufferInOffset, int bufferInLength, byte[] bufferOut, int idxOut, int bufferOutLength)
		{
			var cnt = blazer_stream_decompress_block(
				bufferIn, bufferInOffset, bufferInLength, bufferOut, idxOut, bufferOutLength);
			if (cnt < 0)
				throw new InvalidOperationException("Invalid compressed data");
			return cnt;
		}
	}
}
