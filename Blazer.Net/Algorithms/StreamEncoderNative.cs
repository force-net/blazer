using System;
using System.Runtime.InteropServices;

namespace Force.Blazer.Algorithms
{
	public class StreamEncoderNative : StreamEncoder
	{
		[DllImport(@"Blazer.Native.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern int blazer_v1_compress_block(
			IntPtr bufferIn, int bufferInOffset, int bufferInLength, int globalOffset, IntPtr bufferOut, int bufferOutOffset, IntPtr hashArr);

		private GCHandle _bufferInHandle;
		private GCHandle _bufferOutHandle;
		private GCHandle _hashArrHandle;

		public override void Init(int maxInBlockSize, int additionalHeaderSizeForOut, Action<byte[], int, bool> onBlockPrepared)
		{
			base.Init(maxInBlockSize, additionalHeaderSizeForOut, onBlockPrepared);
			_bufferInHandle = GCHandle.Alloc(_bufferIn, GCHandleType.Pinned);
			_bufferOutHandle = GCHandle.Alloc(_bufferOut, GCHandleType.Pinned);
			_hashArrHandle = GCHandle.Alloc(_hashArr, GCHandleType.Pinned);
		}

		public override int CompressBlock(
			byte[] bufferIn,
			int bufferInOffset,
			int bufferInLength,
			int bufferInShift,
			byte[] bufferOut,
			int bufferOutOffset,
			int[] hashArr)
		{
			return blazer_v1_compress_block(
				_bufferInHandle.AddrOfPinnedObject(),
				bufferInOffset,
				bufferInLength,
				bufferInShift,
				_bufferOutHandle.AddrOfPinnedObject(),
				bufferOutOffset,
				_hashArrHandle.AddrOfPinnedObject());
		}

		public override void Dispose()
		{
			_bufferInHandle.Free();
			_bufferOutHandle.Free();
			_hashArrHandle.Free();
			base.Dispose();
		}
	}
}
