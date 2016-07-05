using System;

namespace Force.Blazer.Algorithms
{
	public class NoCompressionEncoder : IEncoder
	{
		private int _maxInBlockSize;

		private byte[] _buffer;

		private int _bufferOutHeaderSize;

		private int _bufferInPos;

		private Action<byte[], int, byte> _onBlockPrepared;

		public void Write(byte[] buffer, int offset, int count)
		{
			while (count > 0)
			{
				var toCopy = Math.Min(count, _maxInBlockSize - _bufferInPos);
				Buffer.BlockCopy(buffer, offset, _buffer, _bufferInPos, toCopy);
				_bufferInPos += toCopy;

				if (_bufferInPos >= _maxInBlockSize)
				{
					CompressAndWrite();
				}

				count -= toCopy;
				offset += toCopy;
			}
		}

		public void CompressAndWrite()
		{
			_onBlockPrepared(_buffer, _bufferInPos, 0x00);
			_bufferInPos = _bufferOutHeaderSize;
		}

		public void Init(int maxInBlockSize, int additionalHeaderSizeForOut, Action<byte[], int, byte> onBlockPrepared)
		{
			_maxInBlockSize = maxInBlockSize + additionalHeaderSizeForOut;
			_buffer = new byte[_maxInBlockSize];
			_bufferOutHeaderSize = additionalHeaderSizeForOut;
			_bufferInPos = _bufferOutHeaderSize;
			_onBlockPrepared = onBlockPrepared;
		}

		public BlazerAlgorithm GetAlgorithmId()
		{
			return BlazerAlgorithm.NoCompress;
		}

		public void Dispose()
		{
		}
	}
}
