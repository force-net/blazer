using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

using Force.Blazer.Algorithms;
using Force.Blazer.Algorithms.Crc32C;
using Force.Blazer.Benchmark.MessagesDto;
using Force.Blazer.Native;

using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Zip.Compression;

using LZ4;

using Snappy;

namespace Force.Blazer.Benchmark
{
    public class Program
    {
		public static void Main()
		{
			BenchPatternedCompression();
			// BenchCrc32C();
			//BenchNoCompression();
			// BenchFile("Selesia Total", @"..\..\..\TestFiles\Silesia\ztotal.tar");
			// BenchFile("Log", @"..\..\..\TestFiles\Service.2016-05-01.log");
			// BenchFile("AdventureWorks (high compressible db)", @"..\..\..\TestFiles\AdventureWorks2012_Data.mdf");
			// BenchFile("enwiki8 (big text document)", @"..\..\..\TestFiles\enwik8");
			// BenchSilesia();
			// BenchBlockSize(@"..\..\..\TestFiles\Service.2016-05-01.log");
		}

	    private static void BenchCrc32C()
		{
			var r = new Random();
			var array = new byte[(1 << 24) + 2]; // +2 for checking unaligned data
			r.NextBytes(array);
			ICrc32CCalculator hardware = new Crc32CHardware();
			ICrc32CCalculator software = new Crc32CSoftware();

			//warm-up
			Crc32C.Crc32CAlgorithm.Compute(new byte[1]);
			hardware.Calculate(0, new byte[1], 0, 1);
			software.Calculate(0, new byte[1], 0, 1);

			var sw = new Stopwatch();
			sw.Start();
			var cr = Crc32C.Crc32CAlgorithm.Compute(array);
			Console.WriteLine("Crc32C.Net: " + ((array.Length / sw.Elapsed.TotalSeconds) / (1024 * 1024)).ToString("0") + " MB/s");
			sw.Restart();
			var hr = hardware.Calculate(0, array, 0, array.Length);
			Console.WriteLine("Hardware: " + ((array.Length / sw.Elapsed.TotalSeconds) / (1024 * 1024)).ToString("0") + " MB/s");
			sw.Restart();
			var sr = software.Calculate(0, array, 0, array.Length);
			Console.WriteLine("Software: " + ((array.Length / sw.Elapsed.TotalSeconds) / (1024 * 1024)).ToString("0") + " MB/s");
			if (hr != cr)
				Console.WriteLine("Error in hardware realization");
			if (sr != cr)
				Console.WriteLine("Error in software realization");
		}

		private static void BenchNoCompression()
		{
			var r = new Random(456789);
			var array = new byte[(1 << 24)]; // +2 for checking unaligned data
			r.NextBytes(array);
			BenchData("non-compressible data", array);
		}

		private static void BenchFile(string title, string fileName)
		{
			var array = File.ReadAllBytes(fileName);
			BenchData(title, array);
		}

		private static void BenchSilesia()
		{
			const string SilesiaPAth = @"..\..\..\TestFiles\Silesia";
			if (Directory.Exists(SilesiaPAth))
			{
				foreach (var fileName in Directory.GetFiles(SilesiaPAth))
				{
					var array = File.ReadAllBytes(fileName);
					BenchData(Path.GetFileName(fileName), array);
				}
			}
		}

		private static void BenchData(string title, byte[] array)
		{
			Console.WriteLine();
			Console.WriteLine("Testing " + title);
			DoBench("NoCompr ", array, x => new BlazerInputStream(x, BlazerCompressionOptions.CreateNoCompression()), x => new BlazerOutputStream(x));
			NativeHelper.SetNativeImplementation(false);

			DoBench("Stream/S", array, x => new BlazerInputStream(x, BlazerCompressionOptions.CreateStream()), x => new BlazerOutputStream(x));
			NativeHelper.SetNativeImplementation(true);
			DoBench("Stream/SH", array, x => new BlazerInputStream(x, BlazerCompressionOptions.CreateStreamHigh()), x => new BlazerOutputStream(x));
			DoBench("Stream/N", array, x => new BlazerInputStream(x, BlazerCompressionOptions.CreateStream()), x => new BlazerOutputStream(x));

			NativeHelper.SetNativeImplementation(false);
			DoBench("Block/S ", array, x => new BlazerInputStream(x, BlazerCompressionOptions.CreateBlock()), x => new BlazerOutputStream(x));
			NativeHelper.SetNativeImplementation(true);
			DoBench("Block/N ", array, x => new BlazerInputStream(x, BlazerCompressionOptions.CreateBlock()), x => new BlazerOutputStream(x));

			DoBench("LZ4     ", array, x => new LZ4Stream(x, LZ4StreamMode.Compress), x => new LZ4Stream(x, LZ4StreamMode.Decompress));
			DoBench("LZ4/HC  ", array, x => new LZ4Stream(x, LZ4StreamMode.Compress, LZ4StreamFlags.HighCompression), x => new LZ4Stream(x, LZ4StreamMode.Decompress));
			DoBench("Snappy  ", array, x => new SnappyStream(x, CompressionMode.Compress), x => new SnappyStream(x, CompressionMode.Decompress));
			DoBench("StdGZip ", array, x => new GZipStream(x, CompressionMode.Compress), x => new GZipStream(x, CompressionMode.Decompress));
			// very slow for usual running
			// DoBench("BZip2    ", array, x => new BZip2OutputStream(x), x => new BZip2InputStream(x));
			// DoBenchQuickLZ("QuickLZ/1", 1, array);
			// DoBenchQuickLZ("QuickLZ/3", 3, array);
		}

		private static void DoBench(string title, byte[] data, Func<Stream, Stream> createCompressionStream, Func<Stream, Stream> createDecompressionStream)
		{
			var ms = new MemoryStream();
			var sw = new Stopwatch();
			sw.Start();
			using (var cs = createCompressionStream(ms))
				new MemoryStream(data).CopyTo(cs);
			var compressionTime = sw.ElapsedMilliseconds;

			var comprArray = ms.ToArray();
			sw.Restart();
			var ds = createDecompressionStream(new MemoryStream(comprArray));
			var ms2 = new MemoryStream(data.Length);
			ds.CopyTo(ms2);
			var decompressionTime = sw.ElapsedMilliseconds;
			var resArray = ms2.ToArray();
			if (!data.SequenceEqual(resArray))
				Console.WriteLine("Data Integrity failed for " + title);
			Console.WriteLine(
				"{0}\t{1,4:0} MB/s\t{2,4:0} MB/s\t{3,7:0.000}%\t{4:0.000}",
				title,
				data.Length / (compressionTime / 1000.0) / 1048576,
				data.Length / (decompressionTime / 1000.0) / 1048576,
				100.0 * comprArray.Length / data.Length,
				1.0 * data.Length / comprArray.Length);
		}

		private static void DoBenchQuickLZ(string title, int level, byte[] data)
		{
			var sw = new Stopwatch();
			sw.Start();

			var comprArray = QuickLZ.QuickLZ.compress(data, level);
			var compressionTime = sw.ElapsedMilliseconds;

			sw.Restart();
			var resArray = QuickLZ.QuickLZ.decompress(comprArray);
			var decompressionTime = sw.ElapsedMilliseconds;
			
			if (!data.SequenceEqual(resArray))
				Console.WriteLine("Data Integrity failed for " + title);
			Console.WriteLine(
				"{0}\t{1,4:0} MB/s\t{2,4:0} MB/s\t{3,7:0.000}%\t{4:0.000}",
				title,
				data.Length / (compressionTime / 1000.0) / 1048576,
				data.Length / (decompressionTime / 1000.0) / 1048576,
				100.0 * comprArray.Length / data.Length,
				1.0 * data.Length / comprArray.Length);
		}

		private static void BenchBlockSize(string fileName)
		{
			var array = File.ReadAllBytes(fileName);
			var blockSize = 16;
			while (blockSize < 1 << 21)
			{
				Console.WriteLine(blockSize);
				BlazerCompressionOptions streamOptions = BlazerCompressionOptions.CreateStream();
				// max block size for this test
				streamOptions.MaxBlockSize = 1 << 20;
				DoBenchBlock("Stream   ", blockSize, array, x => new BlazerInputStream(x, streamOptions), x => new BlazerOutputStream(x));
				DoBenchBlock("Stream/H ", blockSize, array, x => new BlazerInputStream(x, BlazerCompressionOptions.CreateStreamHigh()), x => new BlazerOutputStream(x));
				DoBenchBlock("Block    ", blockSize, array, x => new BlazerInputStream(x, BlazerCompressionOptions.CreateNoCompression()), x => new BlazerOutputStream(x));
				DoBenchBlock("LZ4      ", blockSize, array, x => new LZ4Stream(x, LZ4StreamMode.Compress), x => new LZ4Stream(x, LZ4StreamMode.Decompress));
				DoBenchBlock("LZ4/HC   ", blockSize, array, x => new LZ4Stream(x, LZ4StreamMode.Compress, LZ4StreamFlags.HighCompression), x => new LZ4Stream(x, LZ4StreamMode.Decompress));
				DoBenchBlock("Snappy   ", blockSize, array, x => new SnappyStream(x, CompressionMode.Compress), x => new SnappyStream(x, CompressionMode.Decompress));
				DoBenchBlock("SharpZip ", blockSize, array, x => new GZipOutputStream(x), x => new GZipInputStream(x));
				blockSize <<= 1;
			}
		}

		private static void DoBenchBlock(
			string title,
			int blockSize,
			byte[] data,
			Func<Stream, Stream> createCompressionStream,
			Func<Stream, Stream> createDecompressionStream)
		{
			var ms = new MemoryStream();
			var sw = new Stopwatch();
			sw.Start();
			using (var cs = createCompressionStream(ms))
			{
				for (var i = 0; i < data.Length; i += blockSize)
				{
					cs.Write(data, i, Math.Min(data.Length - i, blockSize));
					cs.Flush();
				}
			}

			var compressionTime = sw.ElapsedMilliseconds;

			var comprArray = ms.ToArray();
			sw.Restart();
			var ds = createDecompressionStream(new MemoryStream(comprArray));
			var ms2 = new MemoryStream(data.Length);
			ds.CopyTo(ms2);
			var decompressionTime = sw.ElapsedMilliseconds;
			var resArray = ms2.ToArray();
			if (!data.SequenceEqual(resArray))
				Console.WriteLine("Data Integrity failed for " + title);
			Console.WriteLine(
				"{0}\t{1,4:0} MB/s\t{2,4:0} MB/s\t{3,7:0.000}%\t{4:0.000}",
				title,
				data.Length / (compressionTime / 1000.0) / 1048576,
				data.Length / (decompressionTime / 1000.0) / 1048576,
				100.0 * comprArray.Length / data.Length,
				1.0 * data.Length / comprArray.Length);
		}

		private static void BenchPatternedCompression()
		{
			var data = LogMessage.Generate(10000);
			var totalSize = data.Sum(x => x.Length);

			var gzipSize = data.Sum(x =>
				{
					var deflater = new Deflater();
					deflater.SetInput(x);
					deflater.Finish();
					var cnt = 0;
					while (!deflater.IsNeedingInput)
						cnt += deflater.Deflate(new byte[x.Length]);
					return cnt;
				});

			var quickLzSize = data.Sum(x => QuickLZ.QuickLZ.compress(x, 1).Length);

			var blStreamIndependent = data.Sum(x => StreamEncoder.CompressData(x).Length);

			var ps = BlazerPatternedHelper.CreateStream();
			ps.PreparePattern(data[0]);

			var psbest = BlazerPatternedHelper.CreateStream();
			psbest.PreparePattern(LogMessage.GenerateBestPattern());
			// var psh = BlazerPatternedHelper.CreateStreamHigh();
			// psh.PreparePattern(data[0]);
			// var pb = BlazerPatternedHelper.CreateBlock();
			// pb.PreparePattern(data[0]);

			var blSPatterned = data.Sum(x => ps.EncodeWithPattern(x).Length);
			var blSBestPatterned = data.Sum(x => psbest.EncodeWithPattern(x).Length);
			// var blSHPatterned = data.Sum(x => psh.EncodeWithPattern(x).Length);
			// var blBPatterned = data.Sum(x => pb.EncodeWithPattern(x).Length);

			Console.WriteLine(Encoding.UTF8.GetString(data[0]));
			Console.WriteLine();
			Console.WriteLine("Total:               {0}", totalSize);
			Console.WriteLine("GZip (Deflate):      {0}\t{1:0.000}", gzipSize, 100.0 * gzipSize / totalSize);
			Console.WriteLine("QuickLZ:             {0}\t{1:0.000}", quickLzSize, 100.0 * quickLzSize / totalSize);
			Console.WriteLine("Blazer Independent:  {0}\t{1:0.000}", blStreamIndependent, 100.0 * blStreamIndependent / totalSize);
			Console.WriteLine("Blazer Pattern:      {0}\t{1:0.000}", blSPatterned, 100.0 * blSPatterned / totalSize);
			Console.WriteLine("Blazer Pattern Best: {0}\t{1:0.000}", blSBestPatterned, 100.0 * blSBestPatterned / totalSize);
			// Console.WriteLine("Blazer Pattern SH:   {0}\t{1:0.000}", blSHPatterned, 100.0 * blSHPatterned / totalSize);
			// Console.WriteLine("Blazer Pattern B:    {0}\t{1:0.000}", blBPatterned, 100.0 * blBPatterned / totalSize);
		}
    }
}
