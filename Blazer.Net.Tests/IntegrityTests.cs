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
	}
}
