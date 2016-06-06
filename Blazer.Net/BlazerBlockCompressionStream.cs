using System.IO;

namespace Force.Blazer
{
	/// <summary>
	/// Compression stream with 'block' algorithm realization. It is good for compressing big files.
	/// </summary>
	public class BlazerBlockCompressionStream : BlazerBaseCompressionStream
	{
		public BlazerBlockCompressionStream(Stream innerStream)
			: base(innerStream, BlazerAlgorithm.Block, BlazerFlags.InBlockSize2M | BlazerFlags.IncludeCrc | BlazerFlags.IncludeFooter | BlazerFlags.IncludeHeader)
		{
		}

		public BlazerBlockCompressionStream(Stream innerStream, BlazerFlags flags)
			: base(innerStream, BlazerAlgorithm.Block, flags)
		{
		}
	}
}
