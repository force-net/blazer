using System;
using System.Runtime.InteropServices;

namespace Force.Blazer.Algorithms
{
	/// <summary>
	/// Native implementation of decoder of block version of Blazer algorithm
	/// </summary>
	/// <remarks>This version provides relative good and fast compression but decompression rate is same as compression</remarks>
	public class BlockDecoderNative : BlockDecoder
	{
		[DllImport(@"Blazer.Native.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern int blazer_block_decompress_block(
			byte[] bufferIn, int bufferInOffset, int bufferInLength, byte[] bufferOut, int bufferOutOffset, int bufferOutLength, int[] hashArr);

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
		/// Decompresses block of data
		/// </summary>
		protected override int DecompressBlock(
			byte[] bufferIn, int bufferInOffset, int bufferInLength, byte[] bufferOut, int idxOut, int bufferOutLength)
		{
			var res = blazer_block_decompress_block(bufferIn, bufferInOffset, bufferInLength, bufferOut, idxOut, bufferOutLength, _hashArr);
			Array.Clear(_hashArr, 0, HASH_TABLE_LEN + 1);

			if (res < 0)
				throw new InvalidOperationException("Invalid compressed data");
			return res;
		}
	}
}
