using System.IO;

namespace Force.Blazer
{
	/// <summary>
	/// Compression stream with 'block' algorithm realization. It is good for compressing big files.
	/// </summary>
	public class BlazerBlockCompressionStream : BlazerBaseCompressionStream
	{
		public BlazerBlockCompressionStream(Stream innerStream)
			: base(innerStream, BlazerAlgorithm.Block, BlazerFlags.DefaultBlock)
		{
		}

		public BlazerBlockCompressionStream(Stream innerStream, BlazerFlags flags = BlazerFlags.DefaultBlock, string password = null)
			: base(innerStream, BlazerAlgorithm.Block, flags, password)
		{
		}
	}
}
