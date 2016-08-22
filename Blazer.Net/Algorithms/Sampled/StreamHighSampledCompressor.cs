using System;

namespace Force.Blazer.Algorithms.Sampled
{
	/// <summary>
	/// Sampled Compressor/Decompressor for Blazer Stream High algorithm
	/// </summary>
	/// <remarks>Method is very slow to use in normal situations. Use only when needed in your specific case</remarks>
	public class StreamHighSampledCompressor : StreamSampledCompressor
	{
		private readonly StreamEncoderHigh _encoder;

		/// <summary>
		/// Initializes sampled compressor
		/// </summary>
		public StreamHighSampledCompressor()
		{
			_encoder = new StreamEncoderHigh();
			_encoder.Init(0);
		}

		private int[][] _hashArrClone;

		private int[] _hashArrPosClone;

		/// <summary>
		/// Initializes HashArray for Encoder 
		/// </summary>
		protected override void InitHashArray()
		{
			_hashArrClone = new int[StreamEncoderHigh.HASHARR_CNT][];
			var e = _encoder.HashArr2;
			for (var i = 0; i < StreamEncoderHigh.HASHARR_CNT; i++)
			{
				_hashArrClone[i] = new int[e[i].Length];
				Array.Copy(e[i], 0, _hashArrClone[i], 0, e[i].Length);
			}

			_hashArrPosClone = new int[_encoder.HashArrPos.Length];
			Array.Copy(_encoder.HashArrPos, 0, _hashArrPosClone, 0, _hashArrPosClone.Length);
		}

		/// <summary>
		/// Restores HashArray for Encoder
		/// </summary>
		protected override void RestoreHashArray()
		{
			var e = _encoder.HashArr2;
			for (var i = 0; i < StreamEncoderHigh.HASHARR_CNT; i++)
			{
				Array.Copy(_hashArrClone[i], 0, e[i], 0, e[i].Length);
			}

			Array.Copy(_hashArrPosClone, 0, _encoder.HashArrPos, 0, _hashArrPosClone.Length);
		}

		/// <summary>
		/// Compress block of data
		/// </summary>
		protected override int CompressBlock(int countIn, byte[] bufferOut, int offsetOut)
		{
			return _encoder.CompressBlock(_innerBuffer, _sampleLength, _sampleLength + countIn, 0, bufferOut, offsetOut + 1);
		}
	}
}
