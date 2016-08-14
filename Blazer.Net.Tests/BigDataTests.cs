using System;
using System.Diagnostics;
using System.IO;

using Force.Blazer;

using NUnit.Framework;

namespace Blazer.Net.Tests
{
	[TestFixture]
	public class BigDataTests
	{
		[Test]
		[Ignore("Slow for usual run")]
		public void Large_Data_Should_Be_Compressed()
		{
			var outs = new MemoryStream();
			// stream algorithm is affected to big data (requires backref shift)
			var blazerCompressionOptions = BlazerCompressionOptions.CreateStream();
			blazerCompressionOptions.LeaveStreamOpen = true;
			var comp = new BlazerInputStream(outs, blazerCompressionOptions);
			const int BufSize = 200000;
			var buf = new byte[BufSize];
			long totalSize = 1L << 32; // 4Gb
			var iterations = totalSize / buf.Length;
			totalSize = iterations * buf.Length;
			for (var i = 0; i < iterations; i++)
			{
				var v = i % 256;
				for (var k = 0; k < buf.Length; k++) buf[k] = (byte)v;
				comp.Write(buf, 0, buf.Length);
			}

			comp.Close();

			Debug.WriteLine("Compressed. " + outs.Length);

			outs.Seek(0, SeekOrigin.Begin);
			var decomp = new BlazerOutputStream(outs);
			long totalPos = 0;
			while (true)
			{
				var readed = decomp.Read(buf, 0, buf.Length);
				if (readed == 0) break;
				for (var i = 0; i < readed; i++)
				{
					long v = (totalPos / BufSize) % 256;
					if (buf[i] != v)
						throw new ArgumentException("Invalid data at " + totalPos + ". Expected: " + v + ", but was: " + buf[i]);
					totalPos++;
				}
			}

			Assert.That(totalPos, Is.EqualTo(totalSize));
		}
	}
}
