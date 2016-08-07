using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Force.Blazer;

using NUnit.Framework;

namespace Blazer.Net.Tests
{
	[TestFixture]
	public class IntegrityTests
	{
		[Test]
		[TestCase(BlazerAlgorithm.NoCompress)]
		[TestCase(BlazerAlgorithm.Stream)]
		[TestCase(BlazerAlgorithm.Block)]
		public void Simple_Data_Should_Be_Encoded_Decoded(BlazerAlgorithm algorithm)
		{
			var data = Encoding.UTF8.GetBytes("some compressible not very long string. some some some.");
			var blazerCompressionOptions = BlazerCompressionOptions.CreateStream();
			blazerCompressionOptions.SetEncoderByAlgorithm(algorithm);
			IntegrityHelper.CheckCompressDecompress(data, blazerCompressionOptions);
		}

		[Test]
		[TestCase(BlazerAlgorithm.NoCompress)]
		[TestCase(BlazerAlgorithm.Stream)]
		[TestCase(BlazerAlgorithm.Block)]
		public void HighCompressible_Data_Should_Be_Encoded_Decoded(BlazerAlgorithm algorithm)
		{
			var data = new byte[10 * 1048576];
			var blazerCompressionOptions = BlazerCompressionOptions.CreateStream();
			blazerCompressionOptions.SetEncoderByAlgorithm(algorithm);
			IntegrityHelper.CheckCompressDecompress(data, blazerCompressionOptions);
		}

		[Test]
		[TestCase(BlazerAlgorithm.NoCompress)]
		[TestCase(BlazerAlgorithm.Stream)]
		[TestCase(BlazerAlgorithm.Block)]
		public void NonCompressible_Data_Should_Be_Encoded_Decoded(BlazerAlgorithm algorithm)
		{
			var data = new byte[1234];
			RandomNumberGenerator.Create().GetBytes(data);
			var blazerCompressionOptions = BlazerCompressionOptions.CreateStream();
			blazerCompressionOptions.SetEncoderByAlgorithm(algorithm);
			IntegrityHelper.CheckCompressDecompress(data, blazerCompressionOptions);
		}

		[Test]
		public void Invalid_Header_Should_Throw_Errors()
		{
			var data1 = new byte[12];
			var compressed = IntegrityHelper.CompressData(data1, BlazerCompressionOptions.CreateStream());
			// not blazer archiver
			compressed[0]++;
			Assert.That(Assert.Throws<InvalidOperationException>(() => IntegrityHelper.DecompressData(compressed)).Message, Is.EqualTo("This is not Blazer archive"));
			compressed[0]--;

			// invalid version
			compressed[3]++;
			Assert.That(Assert.Throws<InvalidOperationException>(() => IntegrityHelper.DecompressData(compressed)).Message, Is.EqualTo("Stream was created in new version of Blazer library"));
			compressed[3]--;

			// invalid flags
			compressed[6]++;
			Assert.That(Assert.Throws<InvalidOperationException>(() => IntegrityHelper.DecompressData(compressed)).Message, Is.EqualTo("Invalid flag combination. Try to use newer version of Blazer"));
			compressed[6]--;
		}
	}
}
