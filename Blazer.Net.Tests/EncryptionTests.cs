using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Force.Blazer;
using Force.Blazer.Encyption;

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

		[Test]
		public void Different_Paddings_Should_Not_Cause_Errors()
		{
			var data = Enumerable.Range(0, 100).Select(x => (byte)x).ToArray();
			var pass = new Rfc2898DeriveBytes("test", 8, 4096);
			var key = pass.GetBytes(32);
			var aes = Aes.Create();
			aes.Key = key;
			aes.IV = new byte[16];
			aes.Mode = CipherMode.CBC;
#if NETCORE
			aes.Padding = PaddingMode.PKCS7;
#else
			aes.Padding = PaddingMode.ISO10126;
#endif
			var memoryStream = new MemoryStream();
			var cs = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
			cs.Write(data, 0, data.Length);
			cs.Flush();
			cs.Close();

			var aes2 = Aes.Create();
			aes2.Key = key;
			aes2.IV = new byte[16];
			aes2.Mode = CipherMode.CBC;
			aes2.Padding = PaddingMode.PKCS7;
			var cs2 = new CryptoStream(new MemoryStream(memoryStream.ToArray()), new Iso10126TransformEmulator(aes2.CreateDecryptor()), CryptoStreamMode.Read);
			var data2 = new byte[128];
			var len = cs2.Read(data2, 0, data2.Length);
			Assert.That(len, Is.EqualTo(data.Length));
			CollectionAssert.AreEqual(data, data2.Take(data.Length));
		}
	}
}
