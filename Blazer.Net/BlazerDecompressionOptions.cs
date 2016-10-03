using System;

using Force.Blazer.Algorithms;

namespace Force.Blazer
{
	/// <summary>
	/// Options for decompression
	/// </summary>
	public class BlazerDecompressionOptions
	{
		/// <summary>
		/// Initialize this property for archive without header, to provide required information for decompression
		/// </summary>
		public BlazerCompressionOptions CompressionOptions { get; set; }

		/// <summary>
		/// Initialize this property for archive without header, to provide custom decoder
		/// </summary>
		public IDecoder Decoder { get; set; }

		/// <summary>
		/// Set default decoder by algorithm
		/// </summary>
		public void SetDecoderByAlgorithm(BlazerAlgorithm algorithm)
		{
			Decoder = EncoderDecoderFactory.GetDecoder(algorithm);
		}

		/// <summary>
		/// Leave inner stream open after closing blazer stream
		/// </summary>
		public bool LeaveStreamOpen { get; set; }

		/// <summary>
		/// Password for decrypting data
		/// </summary>
		public string Password { get; set; }

		/// <summary>
		/// Encrypt full flag. Fully encypted streams does not reveal any information about inner data (blazer header is also encypted)
		/// </summary>
		public bool EncyptFull { get; set; }

		/// <summary>
		/// Disable seeking for inner stream
		/// </summary>
		/// <remarks>By default, <see cref="BlazerOutputStream"/> checks is stream seekable. But with this flag this check can be disabled and seek will not be performed for any stream</remarks>
		public bool NoSeek { get; set; }

		/// <summary>
		/// Callback on control data block. If is set, will be called for every control data
		/// </summary>
		public Action<byte[], int, int> ControlDataCallback { get; set; }

		/// <summary>
		/// Callbacks on new file info
		/// </summary>
		public Action<BlazerFileInfo> FileInfoCallback { get; set; }

		/// <summary>
		/// Create default options
		/// </summary>
		public static BlazerDecompressionOptions CreateDefault()
		{
			return new BlazerDecompressionOptions();
		}

		/// <summary>
		/// Constructor for default options
		/// </summary>
		public BlazerDecompressionOptions()
		{
		}

		/// <summary>
		/// Constructor for default options with password
		/// </summary>
		public BlazerDecompressionOptions(string password)
		{
			Password = password;
		}
	}
}
