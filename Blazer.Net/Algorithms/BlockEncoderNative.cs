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

		private int[] _hashArr;

		/// <summary>
		/// Initializes encoder with information about maximum uncompressed block size
		/// </summary>
		public override void Init(int maxInBlockSize)
		{
			base.Init(maxInBlockSize);
			_hashArr = new int[HASH_TABLE_LEN + 1];
		}

		/// <summary>
		/// Compresses block of data
		/// </summary>
		protected override int CompressBlock(
			byte[] bufferIn,
			int bufferInOffset,
			int bufferInCount,
			byte[] bufferOut,
			int bufferOutOffset)
		{
			var cnt = blazer_block_compress_block(
				bufferIn,
				bufferInOffset,
				bufferInCount,
				_bufferOut,
				bufferOutOffset,
				_hashArr);

			Array.Clear(_hashArr, 0, HASH_TABLE_LEN + 1);
			return cnt;
		}
	}
}
