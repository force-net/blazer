using Force.Blazer.Algorithms.Sampled;

namespace Force.Blazer
{
	/// <summary>
	/// Helper for sampled compression
	/// </summary>
	public static class BlazerSampledHelper
	{
		/// <summary>
		/// Creates Stream algorithm Sampled compressor. Good on compression, fast on decompression
		/// </summary>
		/// <remarks>Do not use sample data more than 64Kb. It is useless for this type</remarks>
		public static ISampledCompressor CreateStream()
		{
			return new StreamSampledCompressor();
		}

		/// <summary>
		/// Creates Stream algorithm Sampled compressor with high encoding. Slow on compression, fast on decompression
		/// </summary>
		/// <remarks>Compression is very slow. Use only when needed</remarks>
		public static ISampledCompressor CreateStreamHigh()
		{
			return new StreamHighSampledCompressor();
		}

		/// <summary>
		/// Creates Block algorithm Sampled compressor. Good on compression, Good on decompression
		/// </summary>
		/// <remarks>Use this algorithm whan sample is big (greater than 64Kb)</remarks>
		public static ISampledCompressor CreateBlock()
		{
			return new BlockSampledCompressor();
		}

		/// <summary>
		/// Creates Sampled compressor and init it with sample. Algorithm is selected by sample size
		/// </summary>
		public static ISampledCompressor CreateFromSampleAuto(byte[] sampleData)
		{
			return CreateFromSampleAuto(sampleData, 0, sampleData.Length);
		}

		/// <summary>
		/// Creates Sampled compressor and init it with sample. Algorithm is selected by sample size
		/// </summary>
		public static ISampledCompressor CreateFromSampleAuto(byte[] sampleData, int offset, int count)
		{
			ISampledCompressor c;
			if (count < 65536) c = CreateStream();
			else c = CreateBlock();

			c.PrepareSample(sampleData, offset, count);

			return c;
		}
	}
}
