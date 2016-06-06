using System;
using System.Diagnostics.CodeAnalysis;

namespace Force.Blazer.Algorithms
{
	public class StreamDecoder : IDecoder
	{
		[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Reviewed. Suppression is OK here.")]
		protected byte[] _innerBuffer;

		private int _innerBufferMaxLen = 0;

		private int _innerBufferPos = 0;

		private int _innerBufferLen = 0;

		private Func<bool> _needNewBlock;

		public virtual void Init(int maxUncompressedBlockSize, Func<bool> needNewBlock)
		{
			_innerBufferMaxLen = maxUncompressedBlockSize + StreamEncoder.MAX_BACK_REF + 1;
			_innerBuffer = new byte[_innerBufferMaxLen];
			_needNewBlock = needNewBlock;
		}

		public int Read(byte[] buffer, int offset, int count)
		{
			if (_innerBufferPos == _innerBufferLen) if (!_needNewBlock()) return 0;

			count = Math.Min(_innerBufferLen - _innerBufferPos, count);
			Buffer.BlockCopy(_innerBuffer, _innerBufferPos, buffer, offset, count);
			_innerBufferPos += count;
			return count;
		}

		public void ProcessBlock(byte[] inBuffer, int length, bool isCompressed)
		{
			if (_innerBufferLen > StreamEncoder.MAX_BACK_REF)
			{
				Buffer.BlockCopy(_innerBuffer, _innerBufferLen - StreamEncoder.MAX_BACK_REF, _innerBuffer, 0, StreamEncoder.MAX_BACK_REF);
				// should be same there
				_innerBufferLen = StreamEncoder.MAX_BACK_REF;
				_innerBufferPos = StreamEncoder.MAX_BACK_REF;
			}

			if (isCompressed)
				_innerBufferLen = DecompressBlock(inBuffer, length, _innerBuffer, _innerBufferLen, _innerBufferMaxLen);
			else
			{
				Buffer.BlockCopy(inBuffer, 0, _innerBuffer, _innerBufferLen, length);
				_innerBufferLen += length;
			}
		}

		public BlazerAlgorithm GetAlgorithmId()
		{
			return BlazerAlgorithm.Stream;
		}

		public virtual int DecompressBlock(byte[] bufferIn, int bufferInLength, byte[] bufferOut, int idxOut, int bufferOutLength)
		{
			var idxIn = 0;
			while (idxIn < bufferInLength)
			{
				var elem = bufferIn[idxIn];

				var litCnt = (elem >> 4) & 7;
				var litCntOrig = litCnt;
				var seqCnt = (elem & 0xf) + 4;
				int backRef;

				if (elem >= 128)
				{
					backRef = (bufferIn[idxIn + 1] | bufferIn[idxIn + 2] << 8) + 257;
					idxIn += 3;
					if (backRef >= 0xffff + 257)
					{
						seqCnt = 0;
						litCnt = elem - 128;
						litCntOrig = litCnt == 127 ? 7 : 0;
					}
				}
				else
				{
					backRef = bufferIn[idxIn + 1] + 1;
					idxIn += 2;
				}

				if (litCntOrig == 7)
				{
					var litCntR = bufferIn[idxIn++];
					if (litCntR < 253) litCnt += litCntR;
					else if (litCntR == 253)
						litCnt += 253 + bufferIn[idxIn++];
					else if (litCntR == 254)
						litCnt += 253 + 256 + (bufferIn[idxIn++] | bufferIn[idxIn++] << 8);
					else
						litCnt += 253 + (256 * 256) + (bufferIn[idxIn++] << 0) + (bufferIn[idxIn++] << 8) + (bufferIn[idxIn++] << 16) + (bufferIn[idxIn++] << 24);
				}

				if (seqCnt == 15 + 4)
				{
					var seqCntR = bufferIn[idxIn++];
					if (seqCntR < 253) seqCnt += seqCntR;
					else if (seqCntR == 253)
						seqCnt += 253 + bufferIn[idxIn++];
					else if (seqCntR == 254)
						seqCnt += 253 + 256 + (bufferIn[idxIn++] << 0 | bufferIn[idxIn++] << 8);
					else
						seqCnt += 253 + (256 * 256) + (bufferIn[idxIn++] << 0) + (bufferIn[idxIn++] << 8) + (bufferIn[idxIn++] << 16) + (bufferIn[idxIn++] << 24);
				}

				var maxOutLength = idxOut + litCnt + seqCnt;
				if (maxOutLength >= bufferOutLength)
				{
					throw new IndexOutOfRangeException("Invalid stream structure");
				}

				if (litCnt >= 8)
				{
					Buffer.BlockCopy(bufferIn, idxIn, bufferOut, idxOut, litCnt);
					idxOut += litCnt;
					idxIn += litCnt;
				}
				else
				{
					while (--litCnt >= 0) bufferOut[idxOut++] = bufferIn[idxIn++];
				}

				if (backRef >= seqCnt && seqCnt >= 8)
				{
					Buffer.BlockCopy(bufferOut, idxOut - backRef, bufferOut, idxOut, seqCnt);
					idxOut += seqCnt;
				}
				else
				{
					while (--seqCnt >= 0)
					{
						bufferOut[idxOut] = bufferOut[idxOut - backRef];
						idxOut++;
					}
				}
			}

			return idxOut;
		}

		public virtual void Dispose()
		{
		}
	}
}
