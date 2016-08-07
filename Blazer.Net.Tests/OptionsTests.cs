using System;
using System.IO;
using System.Linq;

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

			using (var b = new BlazerOutputStream(ms1, new BlazerDecompressionOptions { LeaveStreamOpen = true }))
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

		[Test]
		public void ControlData_Should_Process_Zero()
		{
			var data1 = new byte[12];
			var data2 = Enumerable.Range(1, 10).Select(x => (byte)x).ToArray();
			var blazerCompressionOptions = BlazerCompressionOptions.CreateStream();
			var ms1 = new MemoryStream();
			using (var b = new BlazerInputStream(ms1, blazerCompressionOptions))
			{
				b.Write(data1, 0, data1.Length);
				b.WriteControlData(new byte[0], 0, 0);
				b.Write(data2, 0, data2.Length);
			}

			var blazerDecOptions = new BlazerDecompressionOptions();
			var called = false;
			blazerDecOptions.ControlDataCallback = (bytes, offset, count) =>
				{
					Assert.That(offset, Is.EqualTo(0));
					Assert.That(count, Is.EqualTo(0));
					called = true;
				};

			var decompressed = IntegrityHelper.DecompressData(ms1.ToArray(), x => new BlazerOutputStream(x, blazerDecOptions));
			Assert.That(decompressed.Length, Is.EqualTo(data1.Length + data2.Length));
			Assert.That(decompressed[decompressed.Length - 1], Is.EqualTo(data2[data2.Length - 1]));
			Assert.That(called, Is.True);
		}

		[Test]
		public void ControlData_Should_Process_NonZero()
		{
			var data1 = new byte[12];
			var data2 = Enumerable.Range(1, 10).Select(x => (byte)x).ToArray();
			var blazerCompressionOptions = BlazerCompressionOptions.CreateStream();
			var ms1 = new MemoryStream();
			using (var b = new BlazerInputStream(ms1, blazerCompressionOptions))
			{
				b.Write(data1, 0, data1.Length);
				b.WriteControlData(new byte[12], 0, 12);
				b.Write(data2, 0, data2.Length);
			}

			var blazerDecOptions = new BlazerDecompressionOptions();
			var called = false;
			blazerDecOptions.ControlDataCallback = (bytes, offset, count) =>
			{
				Assert.That(offset, Is.EqualTo(0));
				Assert.That(count, Is.EqualTo(12));
				called = true;
			};

			var decompressed = IntegrityHelper.DecompressData(ms1.ToArray(), x => new BlazerOutputStream(x, blazerDecOptions));
			Assert.That(decompressed.Length, Is.EqualTo(data1.Length + data2.Length));
			Assert.That(decompressed[decompressed.Length - 1], Is.EqualTo(data2[data2.Length - 1]));
			Assert.That(called, Is.True);
		}

		[Test]
		public void Flush_Should_Be_Respected()
		{
			var data1 = new byte[12];
			var blazerCompressionOptions = BlazerCompressionOptions.CreateStream();
			var ms1 = new MemoryStream();
			// default flush respected
			using (var b = new BlazerInputStream(ms1, blazerCompressionOptions))
			{
				b.Write(data1, 0, data1.Length);
				Assert.That(ms1.Length, Is.EqualTo(0));
				b.Flush();
				Assert.That(ms1.Length, Is.GreaterThan(0));
			}

			// turn off flush
			blazerCompressionOptions = BlazerCompressionOptions.CreateStream();
			blazerCompressionOptions.RespectFlush = false;
			ms1 = new MemoryStream();
			// default flush respected
			using (var b = new BlazerInputStream(ms1, blazerCompressionOptions))
			{
				b.Write(data1, 0, data1.Length);
				Assert.That(ms1.Length, Is.EqualTo(0));
				b.Flush();
				Assert.That(ms1.Length, Is.EqualTo(0));
			}
		}
	}
}
