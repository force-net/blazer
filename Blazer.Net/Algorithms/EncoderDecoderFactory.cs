using System;

using Force.Blazer.Native;

namespace Force.Blazer.Algorithms
{
	/// <summary>
	/// Factory for creating default encoders and decoders for Blazer algorithmns
	/// </summary>
	public class EncoderDecoderFactory
	{
		/// <summary>
		/// Returns decoder for algorithm
		/// </summary>
		public static IDecoder GetDecoder(BlazerAlgorithm algorithm)
		{
			switch (algorithm)
			{
				case BlazerAlgorithm.NoCompress: return new NoCompressionDecoder();
				case BlazerAlgorithm.Stream: return NativeHelper.IsNativeAvailable ? new StreamDecoderNative() : new StreamDecoder();
				case BlazerAlgorithm.Block: return NativeHelper.IsNativeAvailable ? new BlockDecoderNative() : new BlockDecoder();
				default: throw new NotImplementedException("Not supported algorithm: " + algorithm);
			}
		}

		/// <summary>
		/// Returns encoder for algorithm
		/// </summary>
		public static IEncoder GetEncoder(BlazerAlgorithm algorithm)
		{
			switch (algorithm)
			{
				case BlazerAlgorithm.NoCompress: return new NoCompressionEncoder();
				case BlazerAlgorithm.Stream: return NativeHelper.IsNativeAvailable ? new StreamEncoderNative() : new StreamEncoder();
				case BlazerAlgorithm.Block: return NativeHelper.IsNativeAvailable ? new BlockEncoderNative() : new BlockEncoder();
				default: throw new NotImplementedException("Not supported algorithm: " + algorithm);
			}
		}
	}
}
