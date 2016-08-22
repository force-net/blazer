using System;

namespace Force.Blazer.Algorithms.Sampled
{
	/// <summary>
	/// Sampled Compressor/Decompressor for Blazer Stream algorithm
	/// </summary>
	public class StreamSampledCompressor : BaseSampledCompressor
	{
		private readonly StreamEncoder _encoder;

		private readonly StreamDecoder _decoder;

		private int[] _hashArrClone;

		/// <summary>
		/// Initializes sampled compressor
		/// </summary>
		public StreamSampledCompressor()
		{
			_encoder = (StreamEncoder)EncoderDecoderFactory.GetEncoder(BlazerAlgorithm.Stream);
			_decoder = (StreamDecoder)EncoderDecoderFactory.GetDecoder(BlazerAlgorithm.Stream);
		}

		/// <summary>
		/// Calculates max compressed buffer size for specified uncompressed data length
		/// </summary>
		public override int CalculateMaxCompressedBufferLength(int uncompressedLength)
		{
			return uncompressedLength + (uncompressedLength >> 8) + 3 + _encoder.GetAdditionalInSize() + 1;
		}

		/// <summary>
		/// Initializes HashArray for Encoder 
		/// </summary>
		protected override void InitHashArray()
		{
			_hashArrClone = new int[_encoder.HashArr.Length];
			Array.Copy(_encoder.HashArr, 0, _hashArrClone, 0, _hashArrClone.Length);
		}

		/// <summary>
		/// Restores HashArray for Encoder
		/// </summary>
		protected override void RestoreHashArray()
		{
			Array.Copy(_hashArrClone, 0, _encoder.HashArr, 0, _hashArrClone.Length);
		}

		/// <summary>
		/// Returns algorithm id
		/// </summary>
		protected override byte GetAlgorithmId()
		{
			return (byte)_encoder.GetAlgorithmId();
		}

		/// <summary>
		/// Compress block of data
		/// </summary>
		protected override int CompressBlock(int countIn, byte[] bufferOut, int offsetOut)
		{
			return _encoder.CompressBlock(_innerBuffer, _sampleLength, _sampleLength + countIn, 0, bufferOut, offsetOut + 1);
		}

		/// <summary>
		/// Decompress block of data
		/// </summary>
		protected override int DecompressBlock(byte[] bufferIn, int offsetIn, int countIn)
		{
			return _decoder.DecompressBlock(bufferIn, offsetIn + 1, offsetIn + countIn, _innerBuffer, _sampleLength, _innerBuffer.Length);
		}
	}
}
