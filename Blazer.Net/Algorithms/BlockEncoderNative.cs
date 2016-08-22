using System;
using System.Runtime.InteropServices;

namespace Force.Blazer.Algorithms
{
	/// <summary>
	/// Native implementation of encoder of block version of Blazer algorithm
	/// </summary>
	/// <remarks>This version provides relative good and fast compression but decompression rate is same as compression</remarks>
	public class BlockEncoderNative : BlockEncoder
	{
		[DllImport(@"Blazer.Native.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern int blazer_block_compress_block(
			byte[] bufferIn, int bufferInOffset, int bufferInLength, byte[] bufferOut, int bufferOutOffset, int[] hashArr);

		/// <summary>
		/// Compresses block of data
		/// </summary>
		public override int CompressBlock(
			byte[] bufferIn,
			int bufferInOffset,
			int bufferInCount,
			byte[] bufferOut,
			int bufferOutOffset,
			bool doCleanup)
		{
			var cnt = blazer_block_compress_block(
				bufferIn,
				bufferInOffset,
				bufferInCount,
				bufferOut,
				bufferOutOffset,
				_hashArr);

			if (doCleanup)
				Array.Clear(_hashArr, 0, HASH_TABLE_LEN + 1);

			return cnt;
		}
	}
}
