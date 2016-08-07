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
			IntegrityHelper.CheckCompressDecompress(data, blazerCompressionOptions, s => new BlazerOutputStream(s, new BlazerDecompressionOptions("123")));
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
		public void AesTest1()
		{
			var aes = Aes.Create();
			aes.IV = new byte[16];
			aes.Padding = PaddingMode.None;
			var inData = new byte[16];
			Console.WriteLine(BitConverter.ToString(aes.CreateEncryptor().TransformFinalBlock(inData, 0, 16)));
			inData[0] = 1;
			Console.WriteLine(BitConverter.ToString(aes.CreateEncryptor().TransformFinalBlock(inData, 0, 16)));
		}
	}
}
