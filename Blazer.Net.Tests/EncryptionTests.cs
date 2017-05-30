using System;
using System.Security.Cryptography;
using System.Text;

using Force.Blazer;

using NUnit.Framework;

namespace Blazer.Net.Tests
{
	[TestFixture]
	public class EncryptionTests
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
			blazerCompressionOptions.Password = "123";
			blazerCompressionOptions.FlushMode = BlazerFlushMode.AutoFlush;
			IntegrityHelper.CheckCompressDecompress(data, blazerCompressionOptions, s => new BlazerOutputStream(s, new BlazerDecompressionOptions("123")));
		}

		[Test]
		[TestCase(BlazerAlgorithm.NoCompress)]
		[TestCase(BlazerAlgorithm.Stream)]
		[TestCase(BlazerAlgorithm.Block)]
		public void Simple_Data_Should_Be_Encoded_Decoded_Small_Block(BlazerAlgorithm algorithm)
		{
			var data = Encoding.UTF8.GetBytes("some compressible not very long string. some some some.");
			var blazerCompressionOptions = BlazerCompressionOptions.CreateStream();
			blazerCompressionOptions.SetEncoderByAlgorithm(algorithm);
			blazerCompressionOptions.Password = "123";
			blazerCompressionOptions.FlushMode = BlazerFlushMode.AutoFlush;
			IntegrityHelper.CheckCompressDecompress(data, blazerCompressionOptions, s => new BlazerOutputStream(s, new BlazerDecompressionOptions("123")), 2);
		}

		[Test]
		public void Invalid_Password_Should_Throw_Error()
		{
			var data = Encoding.UTF8.GetBytes("some compressible not very long string. some some some.");
			var blazerCompressionOptions = BlazerCompressionOptions.CreateStream();
			blazerCompressionOptions.Password = "123";
			var ex = Assert.Throws<InvalidOperationException>(
				() => IntegrityHelper.CheckCompressDecompress(data, blazerCompressionOptions, s => new BlazerOutputStream(s, new BlazerDecompressionOptions("1"))));
			Assert.That(ex.Message, Is.EqualTo("Invalid password"));
		}

		[Test]
		public void No_Password_Should_Throw_Error()
		{
			var data = Encoding.UTF8.GetBytes("some compressible not very long string. some some some.");
			var blazerCompressionOptions = BlazerCompressionOptions.CreateStream();
			blazerCompressionOptions.Password = "123";
			var ex = Assert.Throws<InvalidOperationException>(
				() => IntegrityHelper.CheckCompressDecompress(data, blazerCompressionOptions, s => new BlazerOutputStream(s)));
			Assert.That(ex.Message, Is.EqualTo("Stream is encrypted, but password is not provided"));
		}

		[Test]
		public void Password_For_Not_Encrypted_Should_Throw_Error()
		{
			var data = Encoding.UTF8.GetBytes("some compressible not very long string. some some some.");
			var blazerCompressionOptions = BlazerCompressionOptions.CreateStream();
			var ex = Assert.Throws<InvalidOperationException>(
				() => IntegrityHelper.CheckCompressDecompress(data, blazerCompressionOptions, s => new BlazerOutputStream(s, new BlazerDecompressionOptions("123"))));
			Assert.That(ex.Message, Is.EqualTo("Stream is not encrypted"));
		}

		[Test]
		public void EncyptFull_Should_Work()
		{
			var data = Encoding.UTF8.GetBytes("some compressible not very long string. some some some.");
			var blazerCompressionOptions = BlazerCompressionOptions.CreateStream();
			blazerCompressionOptions.Password = "123";
			blazerCompressionOptions.EncryptFull = true;
			IntegrityHelper.CheckCompressDecompress(data, blazerCompressionOptions, s => new BlazerOutputStream(s, new BlazerDecompressionOptions("123") { EncyptFull = true }));
		}

		[Test]
		public void EncyptFull_Should_Work_Small_Buffer()
		{
			var data = Encoding.UTF8.GetBytes("some compressible not very long string. some some some.");
			var blazerCompressionOptions = BlazerCompressionOptions.CreateStream();
			blazerCompressionOptions.Password = "123";
			blazerCompressionOptions.EncryptFull = true;
			IntegrityHelper.CheckCompressDecompress(data, blazerCompressionOptions, s => new BlazerOutputStream(s, new BlazerDecompressionOptions("123") { EncyptFull = true }), 2);
		}

		[Test]
		public void Invalid_Archive_If_Missing_EncyptFull()
		{
			var data = Encoding.UTF8.GetBytes("some compressible not very long string. some some some.");
			var blazerCompressionOptions = BlazerCompressionOptions.CreateStream();
			blazerCompressionOptions.Password = "123";
			blazerCompressionOptions.EncryptFull = true;
			var ex = Assert.Throws<InvalidOperationException>(() => IntegrityHelper.CheckCompressDecompress(data, blazerCompressionOptions, s => new BlazerOutputStream(s, new BlazerDecompressionOptions("123") { EncyptFull = false })));
			Assert.That(ex.Message, Is.EqualTo("This is not Blazer archive"));
		}

		[Test]
		public void Invalid_Archive_If_Invalid_EncyptFull_Password()
		{
			var data = Encoding.UTF8.GetBytes("some compressible not very long string. some some some.");
			var blazerCompressionOptions = BlazerCompressionOptions.CreateStream();
			blazerCompressionOptions.Password = "123";
			blazerCompressionOptions.EncryptFull = true;
			var ex = Assert.Throws<InvalidOperationException>(() => IntegrityHelper.CheckCompressDecompress(data, blazerCompressionOptions, s => new BlazerOutputStream(s, new BlazerDecompressionOptions("1") { EncyptFull = true })));
			Assert.That(ex.Message, Is.EqualTo("This is not Blazer archive"));
		}
	}
}
