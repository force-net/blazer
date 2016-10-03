using System;
using System.Collections.Generic;
using System.IO;

using Force.Blazer;

using NUnit.Framework;

namespace Blazer.Net.Tests
{
	[TestFixture]
	public class MultipleFilesTests
	{
		[Test]
		public void Cannot_Set_One_File_And_Multiple_Files()
		{
			var options = new BlazerCompressionOptions();
			options.FileInfo = new BlazerFileInfo();
			Assert.Throws<InvalidOperationException>(() => options.MultipleFiles = true);

			options = new BlazerCompressionOptions();
			options.MultipleFiles = true;
			Assert.Throws<InvalidOperationException>(() => options.FileInfo = new BlazerFileInfo());
		}

		[Test]
		public void EmptyFileInfo_Should_Not_Cause_An_Error()
		{
			var data = new byte[123];
			var blazerCompressionOptions = BlazerCompressionOptions.CreateStream();
			blazerCompressionOptions.FileInfo = new BlazerFileInfo();
			var compressed = IntegrityHelper.CompressData(data, blazerCompressionOptions);
			var os = new BlazerOutputStream(new MemoryStream(compressed));
			Assert.That(os.FileInfo, Is.Not.Null);
			Assert.That(os.FileInfo.FileName, Is.EqualTo(string.Empty));
		}

		[Test]
		public void FirstChunk_For_MultipleFiles_Should_Be_FileInfo()
		{
			var data = new byte[123];
			var blazerCompressionOptions = BlazerCompressionOptions.CreateStream();
			blazerCompressionOptions.MultipleFiles = true;
			var stream = new BlazerInputStream(new MemoryStream(), blazerCompressionOptions);
			Assert.Throws<InvalidOperationException>(() => stream.Write(data, 0, data.Length));

			// does not throw
			stream = new BlazerInputStream(new MemoryStream(), blazerCompressionOptions);
			stream.WriteFileInfo(new BlazerFileInfo());
			stream.Write(data, 0, data.Length);
		}

		[Test]
		public void Two_Files_Should_Be_Compressed()
		{
			var data = new byte[123];
			var blazerCompressionOptions = BlazerCompressionOptions.CreateStream();
			blazerCompressionOptions.MultipleFiles = true;
			var memoryStream = new MemoryStream();
			var stream = new BlazerInputStream(memoryStream, blazerCompressionOptions);
			stream.WriteFileInfo(new BlazerFileInfo { FileName = "t1" });
			stream.Write(data, 0, data.Length);
			stream.WriteFileInfo(new BlazerFileInfo { FileName = "t2" });
			stream.Write(data, 0, data.Length);
			stream.Close();
			var res = memoryStream.ToArray();
			var decOptions = new BlazerDecompressionOptions();
			var q = new Queue<string>(new[] { "t1", "t2" });
			decOptions.FileInfoCallback = f => Assert.That(f.FileName, Is.EqualTo(q.Dequeue()));
			new BlazerOutputStream(new MemoryStream(res), decOptions).CopyTo(new MemoryStream());
			Assert.That(q.Count, Is.EqualTo(0));
		}
	}
}
