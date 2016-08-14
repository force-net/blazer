using System;
using System.Diagnostics.CodeAnalysis;

namespace Force.Blazer.Algorithms
{
	/// <summary>
	/// Decoder of block version of Blazer algorithm
	/// </summary>
	/// <remarks>This version provides relative good and fast compression but decompression rate is same as compression</remarks>
	public class BlockEncoder : IEncoder
	{
		private const int HASH_TABLE_BITS = 16;
		private const int HASH_TABLE_LEN = (1 << HASH_TABLE_BITS) - 1;
		private const int MIN_SEQ_LEN = 4;
		// carefully selected random number
		private const uint Mul = 1527631329;

		/// <summary>
		/// Storage for output buffer
		/// </summary>
		[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Reviewed. Suppression is OK here.")]
		protected byte[] _bufferOut;

		/// <summary>
		/// Encodes given buffer
		/// </summary>
		public BufferInfo Encode(byte[] buffer, int offset, int length)
		{
			var cnt = CompressBlock(buffer, offset, length, _bufferOut, 0);
			return new BufferInfo(_bufferOut, 0, cnt);
		}

		/// <summary>
		/// Initializes encoder with information about maximum uncompressed block size
		/// </summary>
		public virtual void Init(int maxInBlockSize)
		{
			_bufferOut = new byte[maxInBlockSize + (maxInBlockSize >> 8) + 3];
		}

		/// <summary>
		/// Compresses block of data
		/// </summary>
		protected virtual int CompressBlock(
			byte[] bufferIn, int bufferInOffset, int bufferInLength, byte[] bufferOut, int bufferOutOffset)
		{
			return CompressBlockExternal(bufferIn, bufferInOffset, bufferInLength, bufferOut, bufferOutOffset);
		}

		/// <summary>
		/// Compressed block of data, can be used independently for byte arrays
		/// </summary>
		/// <param name="bufferIn">In buffer</param>
		/// <param name="bufferInOffset">In buffer offset</param>
		/// <param name="bufferInLength">In buffer right offset (offset + count)</param>
		/// <param name="bufferOut">Out buffer, should be enough size</param>
		/// <param name="bufferOutOffset">Out buffer offset</param>
		/// <returns>Bytes count of compressed data</returns>
		public static int CompressBlockExternal(byte[] bufferIn, int bufferInOffset, int bufferInLength, byte[] bufferOut, int bufferOutOffset)
		{
			var hashArr = new int[HASH_TABLE_LEN + 1];
			var idxIn = bufferInOffset;
			var lastProcessedIdxIn = 0;
			var idxOut = bufferOutOffset;

			int cntLit;

			var iterMax = bufferInLength - 4;

			uint mulEl = 0;

			if (bufferInLength > 3)
				mulEl = (uint)(bufferIn[0] << 16 | bufferIn[1] << 8 | bufferIn[2]);

			while (idxIn < iterMax)
			{
				int idxInP3 = idxIn + 3;
				byte elemP0 = bufferIn[idxInP3];

				mulEl = (mulEl << 8) | elemP0;
				var hashKey = (mulEl * Mul) >> (32 - HASH_TABLE_BITS);
				var hashVal = hashArr[hashKey];
				hashArr[hashKey] = idxInP3;
				var backRef = idxInP3 - hashVal;
				var isBig = backRef < 257 ? 0 : 1;
				if (hashVal > 0 && hashKey != 65535)
				{
					if ((isBig == 0 || bufferIn[hashVal + 1] == bufferIn[idxIn + 4])
						&& mulEl == (uint)((bufferIn[hashVal - 3] << 24) | (bufferIn[hashVal - 2] << 16) | (bufferIn[hashVal - 1] << 8) | bufferIn[hashVal]))
					{
						var origIdxIn = idxIn;
						hashVal += 4 - 3;
						idxIn += 4;

						while (true)
						{
							if (idxIn >= iterMax)
							{
								while (idxIn < bufferInLength && bufferIn[hashVal] == bufferIn[idxIn])
								{
									hashVal++;
									idxIn++;
								}

								break;
							}

							elemP0 = bufferIn[idxIn];
							mulEl = (mulEl << 8) | elemP0;
							hashArr[(mulEl * Mul) >> (32 - HASH_TABLE_BITS)] = idxIn;

							if (bufferIn[hashVal] == elemP0)
							{
								hashVal++;
								idxIn++;
							}
							else
							{
								mulEl = (mulEl << 8) | bufferIn[idxIn + 1];
								hashArr[(mulEl * Mul) >> (32 - HASH_TABLE_BITS)] = idxIn + 1;
								mulEl = (mulEl << 8) | bufferIn[idxIn + 2];
								hashArr[(mulEl * Mul) >> (32 - HASH_TABLE_BITS)] = idxIn + 2;
								break;
							}
						}

						int seqLen = idxIn - origIdxIn - MIN_SEQ_LEN/* - isBig*/;
						cntLit = origIdxIn - lastProcessedIdxIn;
						
						#region Write Back Ref
						if (backRef >= 256 + 1)
						{
							bufferOut[idxOut++] = (byte)(((Math.Min(cntLit, 7) << 4) | Math.Min(seqLen, 15)) | 128);
							bufferOut[idxOut++] = (byte)hashKey;
							bufferOut[idxOut++] = (byte)(hashKey >> 8);
						}
						else
						{
							bufferOut[idxOut++] = (byte)((Math.Min(cntLit, 7) << 4) | Math.Min(seqLen, 15));

							// 1 is always min, should not write it
							bufferOut[idxOut++] = (byte)(backRef - 1);
						}

						#endregion

						#region Write Cnt

						if (cntLit >= 7)
						{
							var c = cntLit - 7;
							if (c < 253) bufferOut[idxOut++] = (byte)c;
							else if (c < 253 + 256)
							{
								bufferOut[idxOut++] = 253;
								bufferOut[idxOut++] = (byte)(c - 253);
							}
							else if (c < 253 + (256 * 256))
							{
								bufferOut[idxOut++] = 254;
								c -= 253 + 256;
								bufferOut[idxOut++] = (byte)c;
								bufferOut[idxOut++] = (byte)(c >> 8);
							}
							else
							{
								bufferOut[idxOut++] = 255;
								c -= 253 + (256 * 256);
								bufferOut[idxOut++] = (byte)c;
								bufferOut[idxOut++] = (byte)(c >> 8);
								bufferOut[idxOut++] = (byte)(c >> 16);
								bufferOut[idxOut++] = (byte)(c >> 24);
							}
						}

						if (seqLen >= 15)
						{
							var c = seqLen - 15;
							if (c < 253) bufferOut[idxOut++] = (byte)c;
							else if (c < 253 + 256)
							{
								bufferOut[idxOut++] = 253;
								bufferOut[idxOut++] = (byte)(c - 253);
							}
							else if (c < 253 + (256 * 256))
							{
								bufferOut[idxOut++] = 254;
								c -= 253 + 256;
								bufferOut[idxOut++] = (byte)c;
								bufferOut[idxOut++] = (byte)(c >> 8);
							}
							else
							{
								bufferOut[idxOut++] = 255;
								c -= 253 + (256 * 256);
								bufferOut[idxOut++] = (byte)c;
								bufferOut[idxOut++] = (byte)(c >> 8);
								bufferOut[idxOut++] = (byte)(c >> 16);
								bufferOut[idxOut++] = (byte)(c >> 24);
							}
						}

						#endregion

						#region Copy Lit

						if (cntLit >= 8)
						{
							Buffer.BlockCopy(bufferIn, lastProcessedIdxIn, bufferOut, idxOut, cntLit);
							idxOut += cntLit;
						}
						else
						{
							while (cntLit > 0)
							{
								bufferOut[idxOut++] = bufferIn[origIdxIn - cntLit];
								cntLit--;
							}
						}

						#endregion

						lastProcessedIdxIn = idxIn;
						continue;
					}
				}

				idxIn++;
			}

			cntLit = bufferInLength - lastProcessedIdxIn;
			idxIn = bufferInLength;

			#region Write Final cntLit

			if (cntLit > 0)
			{
				bufferOut[idxOut++] = (byte)(Math.Min(127, cntLit) + 128);
				bufferOut[idxOut++] = 0xff;
				bufferOut[idxOut++] = 0xff;

				if (cntLit >= 127)
				{
					var c = cntLit - 127;
					if (c < 253) bufferOut[idxOut++] = (byte)c;
					else if (c < 253 + 256)
					{
						bufferOut[idxOut++] = 253;
						bufferOut[idxOut++] = (byte)(c - 253);
					}
					else if (c < 253 + (256 * 256))
					{
						bufferOut[idxOut++] = 254;
						c -= 253 + 256;
						bufferOut[idxOut++] = (byte)c;
						bufferOut[idxOut++] = (byte)(c >> 8);
					}
					else
					{
						bufferOut[idxOut++] = 255;
						c -= 253 + (256 * 256);
						bufferOut[idxOut++] = (byte)c;
						bufferOut[idxOut++] = (byte)(c >> 8);
						bufferOut[idxOut++] = (byte)(c >> 16);
						bufferOut[idxOut++] = (byte)(c >> 24);
					}
				}

				while (cntLit > 0)
				{
					bufferOut[idxOut++] = bufferIn[idxIn - cntLit];
					cntLit--;
				}
			}

			#endregion

			return idxOut;
		}

		/// <summary>
		/// Returns algorithm id
		/// </summary>
		public BlazerAlgorithm GetAlgorithmId()
		{
			return BlazerAlgorithm.Block;
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
