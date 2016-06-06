using System;

namespace Force.Blazer.Algorithms
{
	public class NoCompressionDecoder : IDecoder
	{
		private byte[] _innerBuffer;

		private int _innerBufferPos;

		private int _innerBufferLen;

		private Func<bool> _getNewBlock;

		public int Read(byte[] buffer, int offset, int count)
		{
			if (_innerBufferPos == _innerBufferLen) if (!_getNewBlock()) return 0;

			count = Math.Min(_innerBufferLen - _innerBufferPos, count);
			Buffer.BlockCopy(_innerBuffer, _innerBufferPos, buffer, offset, count);
			_innerBufferPos += count;
			return count;
		}

		public void ProcessBlock(byte[] inBuffer, int length, bool isCompressed)
		{
			_innerBufferPos = 0;
			Buffer.BlockCopy(inBuffer, 0, _innerBuffer, 0, length);
			_innerBufferLen = length;
		}

		public void Init(int maxUncompressedBlockSize, Func<bool> getNextBlock)
		{
			_innerBuffer = new byte[maxUncompressedBlockSize];
			_getNewBlock = getNextBlock;
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
