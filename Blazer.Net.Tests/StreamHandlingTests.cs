using System;
using System.IO;

using Force.Blazer;

using NUnit.Framework;

namespace Blazer.Net.Tests
{
	[TestFixture]
	public class StreamHandlingTests
	{
		[Test]
		public void Memory_Stream_With_Offset_Should_Be_Handled()
		{
			var memoryStream = new MemoryStream();
			var blzStream = new BlazerInputStream(memoryStream, BlazerCompressionOptions.CreateStream());
			var streamWriter = new StreamWriter(blzStream);
			streamWriter.Write("123");
			streamWriter.Flush();
			blzStream.Dispose();

			var dstArr = new byte[1000];
			var comprData = memoryStream.ToArray();
			Buffer.BlockCopy(comprData, 0, dstArr, 1, comprData.Length);

			// such usage of MemoryStream causes Seek and Position return invalid values. As result, decompress can cause an error
			// we're checking that we work correctly
			var r = new StreamReader(new BlazerOutputStream(new MemoryStream(dstArr, 1, comprData.Length))).ReadToEnd();
			Assert.That(r, Is.EqualTo("123"));

			Buffer.BlockCopy(comprData, 0, dstArr, 23, comprData.Length);
			r = new StreamReader(new BlazerOutputStream(new MemoryStream(dstArr, 23, comprData.Length))).ReadToEnd();
			Assert.That(r, Is.EqualTo("123"));
		}

		[Test]
		public void Two_Joined_Streams_Should_Be_Handled()
		{
			var memoryStream = new MemoryStream();
			var options = BlazerCompressionOptions.CreateStream();
			options.LeaveStreamOpen = true;
			var blzStream = new BlazerInputStream(memoryStream, options);
			var streamWriter = new StreamWriter(blzStream);
			streamWriter.Write("111111111111111111111");
			streamWriter.Flush();
			blzStream.Dispose();

			blzStream = new BlazerInputStream(memoryStream, options);
			streamWriter = new StreamWriter(blzStream);
			streamWriter.Write("22222222222222222");
			streamWriter.Flush();
			blzStream.Dispose();


			var comprData = memoryStream.ToArray();
			var readMs = new MemoryStream(comprData);

			// such usage of MemoryStream causes Seek and Position return invalid values. As result, decompress can cause an error
			// we're checking that we work correctly
			var r = new StreamReader(new BlazerOutputStream(readMs, new BlazerDecompressionOptions { LeaveStreamOpen = true })).ReadToEnd();
			Assert.That(r, Is.EqualTo("111111111111111111111"));

			r = new StreamReader(new BlazerOutputStream(readMs, new BlazerDecompressionOptions { LeaveStreamOpen = true })).ReadToEnd();
			Assert.That(r, Is.EqualTo("22222222222222222"));
		}
	}
}
