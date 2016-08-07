using System;
using System.IO;

using Force.Blazer;

using NUnit.Framework;

namespace Blazer.Net.Tests
{
	[TestFixture]
	public class OptionsTests
	{
		[Test]
		public void No_Footer_Should_Be_Respected()
		{
			var data = new byte[123];
			var blazerCompressionOptions = BlazerCompressionOptions.CreateStream();
			// with footer
			var compressed = IntegrityHelper.CompressData(data, blazerCompressionOptions);
			Assert.That(compressed[compressed.Length - 4], Is.EqualTo(0xff));

			blazerCompressionOptions = BlazerCompressionOptions.CreateStream();
			blazerCompressionOptions.IncludeFooter = false;
			// without footer
			var compressed2 = IntegrityHelper.CompressData(data, blazerCompressionOptions);
			Assert.That(compressed2.Length, Is.EqualTo(compressed.Length - 4));
		}

		[Test]
		public void No_Crc_Should_Be_Respected()
		{
			var data = new byte[123];
			var blazerCompressionOptions = BlazerCompressionOptions.CreateStream();
			// with footer
			var compressed = IntegrityHelper.CompressData(data, blazerCompressionOptions);

			blazerCompressionOptions = BlazerCompressionOptions.CreateStream();
			blazerCompressionOptions.IncludeCrc = false;
			// without footer
			var compressed2 = IntegrityHelper.CompressData(data, blazerCompressionOptions);
			Assert.That(compressed2.Length, Is.EqualTo(compressed.Length - 4));
		}

		[Test]
		public void Failed_Crc_Should_Throw_Error()
		{
			var data = new byte[123];
			var blazerCompressionOptions = BlazerCompressionOptions.CreateStream();
			// with footer
			var compressed = IntegrityHelper.CompressData(data, blazerCompressionOptions);

			// making data invalid
			compressed[8 + 4 + 1]++;

			var ex = Assert.Throws<InvalidOperationException>(() => IntegrityHelper.DecompressData(compressed));
			Assert.That(ex.Message, Is.EqualTo("Invalid CRC32C data in passed block. It seems, data error is occured"));
		}

		[Test]
		public void Leave_Stream_Open_Encoder_Should_Be_Respected()
		{
			var data = new byte[123];
			var blazerCompressionOptions = BlazerCompressionOptions.CreateStream();

			var compressed = IntegrityHelper.CompressData(data, blazerCompressionOptions);

			var ms1 = new MemoryStream(compressed);

			using (var b = new BlazerOutputStream(ms1))
				b.Read(new byte[1], 0, 1);
			Assert.Throws<ObjectDisposedException>(() => ms1.Read(new byte[1], 0, 1));

			ms1 = new MemoryStream(compressed);

			using (var b = new BlazerOutputStream(ms1, new BlazerDecompressionOptions() { LeaveStreamOpen = true }))
				b.Read(new byte[1], 0, 1);

			Assert.DoesNotThrow(() => ms1.Read(new byte[1], 0, 1));
		}

		[Test]
		public void Leave_Stream_Open_Decoder_Should_Be_Respected()
		{
			var data = new byte[123];
			var blazerCompressionOptions = BlazerCompressionOptions.CreateStream();
			var ms1 = new MemoryStream();
			using (var b = new BlazerInputStream(ms1, blazerCompressionOptions))
				b.Write(data, 0, data.Length);
			Assert.Throws<ObjectDisposedException>(() => ms1.Write(new byte[1], 0, 1));

			blazerCompressionOptions = BlazerCompressionOptions.CreateStream();
			blazerCompressionOptions.LeaveStreamOpen = true;
			ms1 = new MemoryStream();
			using (var b = new BlazerInputStream(ms1, blazerCompressionOptions))
				b.Write(data, 0, data.Length);
			Assert.DoesNotThrow(() => ms1.Write(new byte[1], 0, 1));
		}
	}
}
