using System;
using System.Text;

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
		/// Password for encrypting data
		/// </summary>
		public string Password
		{
			get
			{
				return PasswordRaw == null ? null : Encoding.UTF8.GetString(PasswordRaw);
			}

			set
			{
				PasswordRaw = string.IsNullOrEmpty(value) ? null : Encoding.UTF8.GetBytes(value);
			}
		}

		/// <summary>
		/// Password for decrypting data (raw binary variant)
		/// </summary>
		public byte[] PasswordRaw { get; set; }

		/// <summary>
		/// Encrypt full flag. Fully encypted streams does not reveal any information about inner data (blazer header is also encypted)
		/// </summary>
		public bool EncyptFull { get; set; }

		/// <summary>
		/// Disable seeking for inner stream
		/// </summary>
		/// <remarks>By default, <see cref="BlazerOutputStream"/> checks is stream seekable. But with this flag this check can be disabled and seek will not be performed for any stream.
		/// This also can be useful for muliple joined streams, when only part of real stream is a Blazer archive</remarks>
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
		/// Skip <see cref="FileInfoCallback"/> when archive contains only one file (can be useful, when file name is analyzed manually)
		/// </summary>
		public bool DoNotFireInfoCallbackOnOneFile { get; set; }

		/// <summary>
		/// Skip real decoding. This option can be useful for 'list' or 'test' modes, when it required to get some info (e.g. files list) without real decoding
		/// </summary>
		public bool DoNotPerformDecoding { get; set; }

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

		/// <summary>
		/// Constructor for default options with raw password
		/// </summary>
		public BlazerDecompressionOptions(byte[] passwordRaw)
		{
			PasswordRaw = passwordRaw;
		}
	}
}
