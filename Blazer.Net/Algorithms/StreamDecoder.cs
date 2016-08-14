using System;
using System.Diagnostics.CodeAnalysis;

namespace Force.Blazer.Algorithms
{
	/// <summary>
	/// Decoder of Stream version of Blazer algorithm
	/// </summary>
	/// <remarks>Stream version is good for 'live' streamss, slightly slower than Block, but support stream flushing without
	/// losing compression rate and has very fast decoder</remarks>
	public class StreamDecoder : IDecoder
	{
		/// <summary>
		/// Inner buffer to store data between iterations
		/// </summary>
		[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Reviewed. Suppression is OK here.")]
		protected byte[] _innerBuffer;

		private int _innerBufferMaxLen = 0;

		private int _innerBufferLen = 0;

		/// <summary>
		/// Initializes decoder with information about maximum uncompressed block size
		/// </summary>
		public virtual void Init(int maxUncompressedBlockSize)
		{
			_innerBufferMaxLen = maxUncompressedBlockSize + StreamEncoder.MAX_BACK_REF + 1;
			_innerBuffer = new byte[_innerBufferMaxLen];
		}

		/// <summary>
		/// Decodes given buffer
		/// </summary>
		public BufferInfo Decode(byte[] buffer, int offset, int length, bool isCompressed)
		{
			if (_innerBufferLen > StreamEncoder.MAX_BACK_REF)
			{
				Buffer.BlockCopy(_innerBuffer, _innerBufferLen - StreamEncoder.MAX_BACK_REF, _innerBuffer, 0, StreamEncoder.MAX_BACK_REF);
				_innerBufferLen = StreamEncoder.MAX_BACK_REF;
			}

			var outOffset = _innerBufferLen;

			if (isCompressed)
				_innerBufferLen = DecompressBlock(buffer, offset, length, _innerBuffer, _innerBufferLen, _innerBufferMaxLen);
			else
			{
				Buffer.BlockCopy(buffer, 0, _innerBuffer, _innerBufferLen, length);
				_innerBufferLen += length;
			}

			return new BufferInfo(_innerBuffer, outOffset, _innerBufferLen);
		}

		/// <summary>
		/// Returns algorithm id
		/// </summary>
		public BlazerAlgorithm GetAlgorithmId()
		{
			return BlazerAlgorithm.Stream;
		}

		/// <summary>
		/// Decompresses block of data
		/// </summary>
		protected virtual int DecompressBlock(
			byte[] bufferIn, int bufferInOffset, int bufferInLength, byte[] bufferOut, int bufferOutOffset, int bufferOutLength)
		{
			return DecompressBlockExternal(bufferIn, bufferInOffset, bufferInLength, bufferOut, bufferOutOffset, bufferOutLength);
		}

		/// <summary>
		/// Decompresses block of data, can be used independently for byte arrays
		/// </summary>
		/// <param name="bufferIn">In buffer</param>
		/// <param name="bufferInOffset">In buffer offset</param>
		/// <param name="bufferInLength">In buffer right offset (offset + count)</param>
		/// <param name="bufferOut">Out buffer, should be enough size</param>
		/// <param name="bufferOutOffset">Out buffer offset</param>
		/// <param name="bufferOutLength">Out buffer maximum right offset (offset + count)</param>
		/// <returns>Bytes count of decompressed data</returns>
		public static int DecompressBlockExternal(byte[] bufferIn, int bufferInOffset, int bufferInLength, byte[] bufferOut, int bufferOutOffset, int bufferOutLength)
		{
			var idxIn = bufferInOffset;
			var idxOut = bufferOutOffset;
			while (idxIn < bufferInLength)
			{
				var elem = bufferIn[idxIn++];
				var seqCntFirst = elem & 0xf;
				var litCntFirst = (elem >> 4) & 7;

				var litCnt = litCntFirst;
				int seqCnt;
				int backRef;

				if (elem >= 128)
				{
					backRef = (bufferIn[idxIn++] | bufferIn[idxIn++] << 8) + 257;
					seqCnt = seqCntFirst + 4;
					if (backRef == 0xffff + 257)
					{
						seqCntFirst = 0;
						seqCnt = 0;
						litCnt = elem - 128;
						litCntFirst = litCnt == 127 ? 7 : 0;
						// backRef = 0;
					}
				}
				else
				{
					backRef = bufferIn[idxIn++] + 1;
					seqCnt = seqCntFirst + 4;
				}

				if (litCntFirst == 7)
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

				if (seqCntFirst == 15)
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

				// exception wil be thrown anyway, but this check decreases decompression speed
				// if (idxOut - backRef < 0)
				//	throw new InvalidOperationException("Invalid stream structure");

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

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		/// <filterpriority>2</filterpriority>
		public virtual void Dispose()
		{
		}
	}
}
