using System;
using System.IO;
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

		[Test]
		[TestCase(1, BlazerAlgorithm.NoCompress)]
		[TestCase(1, BlazerAlgorithm.Stream)]
		[TestCase(1, BlazerAlgorithm.Block)]
		[TestCase(2, BlazerAlgorithm.NoCompress)]
		[TestCase(2, BlazerAlgorithm.Stream)]
		[TestCase(2, BlazerAlgorithm.Block)]
		[TestCase(3, BlazerAlgorithm.NoCompress)]
		[TestCase(3, BlazerAlgorithm.Stream)]
		[TestCase(3, BlazerAlgorithm.Block)]
		[TestCase(4, BlazerAlgorithm.NoCompress)]
		[TestCase(4, BlazerAlgorithm.Stream)]
		[TestCase(4, BlazerAlgorithm.Block)]
		public void Small_Block_Sizes_ShouldBe_Handled_WithoutFlush(int blockSize, BlazerAlgorithm algorithm)
		{
			var data = new byte[blockSize];
			data[0] = (byte)blockSize;
			var blazerCompressionOptions = BlazerCompressionOptions.CreateStream();
			blazerCompressionOptions.SetEncoderByAlgorithm(algorithm);

			var memoryStream = new MemoryStream();
			const int Count = 10;
			using (var stream = new BlazerInputStream(memoryStream, blazerCompressionOptions))
			{
				for (var i = 0; i < Count; i++)
					stream.Write(data, 0, data.Length);
			}

			var compressed = memoryStream.ToArray();
			var decompressed = IntegrityHelper.DecompressData(compressed);
			Assert.That(decompressed.Length, Is.EqualTo(Count * data.Length));
		}

		[Test]
		[TestCase(1, BlazerAlgorithm.NoCompress)]
		[TestCase(1, BlazerAlgorithm.Stream)]
		[TestCase(1, BlazerAlgorithm.Block)]
		[TestCase(2, BlazerAlgorithm.NoCompress)]
		[TestCase(2, BlazerAlgorithm.Stream)]
		[TestCase(2, BlazerAlgorithm.Block)]
		[TestCase(3, BlazerAlgorithm.NoCompress)]
		[TestCase(3, BlazerAlgorithm.Stream)]
		[TestCase(3, BlazerAlgorithm.Block)]
		[TestCase(4, BlazerAlgorithm.NoCompress)]
		[TestCase(4, BlazerAlgorithm.Stream)]
		[TestCase(4, BlazerAlgorithm.Block)]
		public void Small_Block_Sizes_ShouldBe_Handled_WithFlush(int blockSize, BlazerAlgorithm algorithm)
		{
			var data = new byte[blockSize];
			data[0] = (byte)blockSize;
			var blazerCompressionOptions = BlazerCompressionOptions.CreateStream();
			blazerCompressionOptions.SetEncoderByAlgorithm(algorithm);
			blazerCompressionOptions.RespectFlush = true;

			var memoryStream = new MemoryStream();
			const int Count = 10;
			using (var stream = new BlazerInputStream(memoryStream, blazerCompressionOptions))
			{
				for (var i = 0; i < Count; i++)
				{
					stream.Write(data, 0, data.Length);
					stream.Flush();
				}
			}

			var compressed = memoryStream.ToArray();
			var decompressed = IntegrityHelper.DecompressData(compressed);
			Assert.That(decompressed.Length, Is.EqualTo(Count * data.Length));
			Assert.That(decompressed[blockSize], Is.EqualTo(blockSize));
		}

		[Test]
		[TestCase(BlazerAlgorithm.NoCompress)]
		[TestCase(BlazerAlgorithm.Stream)]
		[TestCase(BlazerAlgorithm.Block)]
		public void Zero_Block_Sizes_Should_Not_Cause_Error(BlazerAlgorithm algorithm)
		{
			var data = new byte[0];
			var blazerCompressionOptions = BlazerCompressionOptions.CreateStream();
			blazerCompressionOptions.SetEncoderByAlgorithm(algorithm);

			var memoryStream = new MemoryStream();
			const int Count = 10;
			using (var stream = new BlazerInputStream(memoryStream, blazerCompressionOptions))
			{
				for (var i = 0; i < Count; i++)
				{
					stream.Write(data, 0, data.Length);
					stream.Flush();
				}
			}

			var compressed = memoryStream.ToArray();
			var decompressed = IntegrityHelper.DecompressData(compressed);
			Assert.That(decompressed.Length, Is.EqualTo(0));
		}
	}
}
