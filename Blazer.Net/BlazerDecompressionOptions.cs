using System.Diagnostics.CodeAnalysis;

using Force.Blazer.Algorithms;

namespace Force.Blazer
{
	public class BlazerDecompressionOptions
	{
		/// <summary>
		/// Initialize this property for archive without header, to provide required information for decompression
		/// </summary>
		[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:PropertySummaryDocumentationMustMatchAccessors", Justification = "Reviewed. Suppression is OK here.")]
		public BlazerCompressionOptions CompressionOptions { get; set; }

		/// <summary>
		/// Initialize this property for archive without header, to provide custom decoder
		/// </summary>
		[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:PropertySummaryDocumentationMustMatchAccessors", Justification = "Reviewed. Suppression is OK here.")]
		public IDecoder Decoder { get; set; }

		public void SetDecoderByAlgorithm(BlazerAlgorithm algorithm)
		{
			Decoder = EncoderDecoderFactory.GetDecoder(algorithm);
		}

		public bool LeaveStreamOpen { get; set; }

		public string Password { get; set; }

		public bool EncyptFull { get; set; }

		public static BlazerDecompressionOptions CreateDefault()
		{
			return new BlazerDecompressionOptions();
		}

		public BlazerDecompressionOptions()
		{
		}

		public BlazerDecompressionOptions(string password)
		{
			Password = password;
		}
	}
}
