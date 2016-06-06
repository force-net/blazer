using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Force.Blazer.Algorithms.Crc32C;
using Force.Blazer.Native;

namespace Force.Blazer.Benchmark
{
    public class Program
    {
		public static void Main()
		{
			BenchCrc32C();
			BenchNoCompression();
			BenchFile("AdventureWorks (high compressible db)", @"..\..\..\TestFiles\AdventureWorks2012_Data.mdf");
			BenchFile("enwiki8 (big text document)", @"..\..\..\TestFiles\enwik8");
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
			hardware.Calculate(new byte[1], 0, 1);
			software.Calculate(new byte[1], 0, 1);

			var sw = new Stopwatch();
			sw.Start();
			var cr = Crc32C.Crc32CAlgorithm.Compute(array);
			Console.WriteLine("Crc32C.Net: " + ((array.Length / sw.Elapsed.TotalSeconds) / (1024 * 1024)).ToString("0") + " MB/s");
			sw.Restart();
			var hr = hardware.Calculate(array, 0, array.Length);
			Console.WriteLine("Hardware: " + ((array.Length / sw.Elapsed.TotalSeconds) / (1024 * 1024)).ToString("0") + " MB/s");
			sw.Restart();
			var sr = software.Calculate(array, 0, array.Length);
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

		private static void BenchData(string title, byte[] array)
		{
			Console.WriteLine();
			Console.WriteLine("Testing " + title);
			DoBench("NoCompr ", array, x => new BlazerNoCompressionStream(x), x => new BlazerDecompressionStream(x));
			NativeHelper.SetNativeImplementation(false);
			
			DoBench("Stream/S", array,  x => new BlazerStreamCompressionStream(x), x => new BlazerDecompressionStream(x));
			NativeHelper.SetNativeImplementation(true);
			DoBench("Stream/N", array, x => new BlazerStreamCompressionStream(x), x => new BlazerDecompressionStream(x));

			NativeHelper.SetNativeImplementation(false);
			DoBench("Block/S ", array, x => new BlazerBlockCompressionStream(x), x => new BlazerDecompressionStream(x));
			NativeHelper.SetNativeImplementation(true);
			DoBench("Block/N ", array, x => new BlazerBlockCompressionStream(x), x => new BlazerDecompressionStream(x));
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
				"{0}\t{1:0.000} MB/s\t{2:0.000} MB/s\t{3:0.000}%",
				title,
				data.Length / (compressionTime / 1000.0) / 1048576,
				comprArray.Length / (decompressionTime / 1000.0) / 1048576,
				100.0 * comprArray.Length / data.Length);
		}
    }
}
