using System;
using System.Runtime.InteropServices;

namespace Force.Blazer.Algorithms
{
	public class BlockDecoderNative : BlockDecoder
	{
		[DllImport(@"Blazer.Native.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern int blazer_block_decompress_block(
			byte[] bufferIn, int bufferInOffset, int bufferInLength, byte[] bufferOut, int bufferOutOffset, int bufferOutLength);

		public override int DecompressBlock(
			byte[] bufferIn, int bufferInOffset, int bufferInLength, byte[] bufferOut, int idxOut, int bufferOutLength)
		{
			var res = blazer_block_decompress_block(bufferIn, 0, bufferInLength, bufferOut, idxOut, bufferOutLength);
			if (res < 0)
				throw new InvalidOperationException("Invalid compressed data");
			return res;
		}
	}
}
