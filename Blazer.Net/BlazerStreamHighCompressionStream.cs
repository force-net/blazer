using System.IO;

using Force.Blazer.Algorithms;

namespace Force.Blazer
{
	/// <summary>
	/// High Compression stream with 'stream' algorithm realization. It is good for one-time data storing and multiple reading compressed data.
	/// Inner algorithm does not finalized (but it is compatible with usual stream decoder, so, in future, it can be faster and better)
	/// </summary>
	public class BlazerStreamHighCompressionStream : BlazerBaseCompressionStream
	{
		public BlazerStreamHighCompressionStream(Stream innerStream)
			: base(innerStream, new StreamEncoderHigh(), BlazerFlags.DefaultStream)
		{
		}

		public BlazerStreamHighCompressionStream(Stream innerStream, BlazerFlags flags = BlazerFlags.DefaultStream, string password = null)
			: base(innerStream, new StreamEncoderHigh(), flags, password)
		{
		}
	}
}
