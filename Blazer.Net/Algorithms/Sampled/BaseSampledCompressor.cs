using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Force.Blazer.Algorithms.Sampled
{
	/// <summary>
	/// Base implementation of Sampled Compressor/Decompressor
	/// </summary>
	public abstract class BaseSampledCompressor : ISampledCompressor
	{
		/// <summary>
		/// Calculates max compressed buffer size for specified uncompressed data length
		/// </summary>
		public abstract int CalculateMaxCompressedBufferLength(int uncompressedLength);

		/// <summary>
		/// Initializes internal hash array
		/// </summary>
		protected abstract void InitHashArray();

		/// <summary>
		/// Restores internal hash array
		/// </summary>
		protected abstract void RestoreHashArray();

		/// <summary>
		/// Returns algorithm id
		/// </summary>
		protected abstract byte GetAlgorithmId();

		/// <summary>
		/// Inner buffer
		/// </summary>
		[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Reviewed. Suppression is OK here.")]
		protected byte[] _innerBuffer;

		/// <summary>
		/// Length of sample
		/// </summary>
		[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Reviewed. Suppression is OK here.")]
		protected int _sampleLength;

		/// <summary>
		/// Compress block of data
		/// </summary>
		protected abstract int CompressBlock(int countIn, byte[] bufferOut, int offsetOut);

		/// <summary>
		/// Prepares sample. Should be called only once for sample
		/// </summary>
		public void PrepareSample(byte[] sample, int offset, int count)
		{
			if (count <= 0)
				throw new InvalidOperationException("Invalid sample length");
			var sampleLength = count;
			_innerBuffer = new byte[sampleLength * 2];
			var outerBuffer = new byte[CalculateMaxCompressedBufferLength(sampleLength) + 1];

			Buffer.BlockCopy(sample, offset, _innerBuffer, 0, sampleLength);
			CompressBlock(sampleLength, outerBuffer, 0);
			_sampleLength = sampleLength;
			InitHashArray();
		}

		private int _isWorking;

		/// <summary>
		/// Encodes data with prepared sample
		/// </summary>
		public int EncodeWithSample(byte[] bufferIn, int offsetIn, int countIn, byte[] bufferOut, int offsetOut)
		{
			if (Interlocked.CompareExchange(ref _isWorking, 1, 0) != 0)
				throw new InvalidOperationException("Method is not thread safe ");

			try
			{
				var totalInLength = countIn + _sampleLength;
				if (totalInLength > _innerBuffer.Length)
				{
					var oldInnerBuffer = _innerBuffer;
					_innerBuffer = new byte[totalInLength + 128];
					Buffer.BlockCopy(oldInnerBuffer, 0, _innerBuffer, 0, _sampleLength);
				}

				Buffer.BlockCopy(bufferIn, offsetIn, _innerBuffer, _sampleLength, countIn);

				if (bufferOut.Length - offsetOut < CalculateMaxCompressedBufferLength(countIn))
					throw new InvalidOperationException("Out buffer too small");

				var len = CompressBlock(countIn, bufferOut, offsetOut);
				var writtenCnt = len - offsetOut - 1;
				writtenCnt >>= 16;
				var lenCnt = 0;
				while (writtenCnt > 0)
				{
					lenCnt++;
					writtenCnt >>= 1;
				}

				// writing some metainfo to out buffer, to allow decode and validate data
				bufferOut[offsetOut] = (byte)(lenCnt | (GetAlgorithmId() << 4));

				RestoreHashArray();
				return len - offsetOut;
			}
			finally
			{
				Interlocked.Exchange(ref _isWorking, 0);
			}
		}

		/// <summary>
		/// Decompress block of data
		/// </summary>
		protected abstract int DecompressBlock(byte[] bufferIn, int offsetIn, int countIn);

		/// <summary>
		/// Decodes data with prepared sample
		/// </summary>
		public int DecodeWithSample(byte[] bufferIn, int offsetIn, int countIn, byte[] bufferOut, int offsetOut)
		{
			if (Interlocked.CompareExchange(ref _isWorking, 1, 0) != 0)
				throw new InvalidOperationException("Method is not thread safe ");

			try
			{
				var algFlag = bufferIn[offsetIn];
				var algorithm = algFlag >> 4;
				if (algorithm != GetAlgorithmId())
					throw new InvalidOperationException("Encoded data is not sampled data");
				var maxOutLength = ((algFlag + 1) & 0xf) << 16;

				var totalOutLength = _sampleLength + maxOutLength;
				if (totalOutLength > _innerBuffer.Length)
				{
					var oldInnerBuffer = _innerBuffer;
					_innerBuffer = new byte[totalOutLength + 128];
					Buffer.BlockCopy(oldInnerBuffer, 0, _innerBuffer, 0, _sampleLength);
				}

				var res = DecompressBlock(bufferIn, offsetIn, countIn);
				if (offsetOut + res - _sampleLength > bufferOut.Length)
					throw new InvalidOperationException("Out buffer too small");
				Buffer.BlockCopy(_innerBuffer, _sampleLength, bufferOut, offsetOut, res - _sampleLength);
				return res - _sampleLength;
			}
			finally
			{
				Interlocked.Exchange(ref _isWorking, 0);
			}
		}

		/// <summary>
		/// Encodes data with prepared sample
		/// </summary>
		public byte[] EncodeWithSample(byte[] buffer, int offset, int count)
		{
			var bufferOut = new byte[CalculateMaxCompressedBufferLength(count)];
			var cnt = EncodeWithSample(buffer, offset, count, bufferOut, 0);
			Array.Resize(ref bufferOut, cnt);
			return bufferOut;
		}

		/// <summary>
		/// Encodes data with prepared sample
		/// </summary>
		public byte[] EncodeWithSample(byte[] buffer)
		{
			return EncodeWithSample(buffer, 0, buffer.Length);
		}

		/// <summary>
		/// Decodes data with prepared sample
		/// </summary>
		public byte[] DecodeWithSample(byte[] buffer, int offset, int count)
		{
			var algFlag = buffer[offset];
			var maxOutLength = ((algFlag + 1) & 0xf) << 16;

			var bufferOut = new byte[maxOutLength];
			var cnt = DecodeWithSample(buffer, offset, count, bufferOut, 0);
			Array.Resize(ref bufferOut, cnt);
			return bufferOut;
		}

		/// <summary>
		/// Decodes data with prepared sample
		/// </summary>
		public byte[] DecodeWithSample(byte[] buffer)
		{
			return DecodeWithSample(buffer, 0, buffer.Length);
		}
	}
}
