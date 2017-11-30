using System;
using System.Linq;

using Force.Blazer;
using Force.Blazer.Algorithms;
using Force.Blazer.Helpers;
using Force.Blazer.Native;

using NUnit.Framework;

namespace Blazer.Net.Tests
{
	[TestFixture]
	public class DataArrayTests
	{
		[Test]
		public void Stream_Encode_Decode_Should_Not_Resize_Array()
		{
			var bufferIn = new byte[] { 1, 2, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4 };
			var compr = StreamEncoder.CompressData(bufferIn);
			var bufferOut = new byte[bufferIn.Length];
			var cnt = StreamDecoder.DecompressBlockExternal(compr, 0, compr.Length, ref bufferOut, 0, bufferOut.Length, false);
			Assert.That(cnt, Is.EqualTo(bufferIn.Length));
			CollectionAssert.AreEqual(bufferIn, bufferOut);
		}

		[Test]
		[TestCase(typeof(BlockEncoder), typeof(BlockDecoder))]
		[TestCase(typeof(BlockEncoderNative), typeof(BlockDecoderNative))]
		public void Block_Encode_Decode_Should_Not_Resize_Array(Type encoderType, Type decoderType)
		{
			// ensuring native is inited
			NativeHelper.SetNativeImplementation(true);
			var bufferIn = new byte[] { 1, 2, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4 };
			var encoder = (BlockEncoder)Activator.CreateInstance(encoderType);
			encoder.Init(bufferIn.Length);
			var compr = new byte[bufferIn.Length];
			var comprCnt = encoder.CompressBlock(bufferIn, 0, bufferIn.Length, compr, 0, true);
			Console.WriteLine(comprCnt);

			var bufferOut = new byte[bufferIn.Length];
			var decoder = (BlockDecoder)Activator.CreateInstance(decoderType);
			decoder.Init(bufferIn.Length);
			var cnt = decoder.DecompressBlock(compr, 0, comprCnt, bufferOut, 0, bufferOut.Length, true);
			Assert.That(cnt, Is.EqualTo(bufferIn.Length));
			CollectionAssert.AreEqual(bufferIn, bufferOut);
		}

		[Test]
		[TestCase(typeof(StreamEncoder), typeof(StreamDecoder))]
		[TestCase(typeof(StreamEncoderNative), typeof(StreamDecoderNative))]
		[TestCase(typeof(StreamEncoderHigh), typeof(StreamDecoder))]
		public void Stream_Encode_Decode_Should_Not_Resize_Array(Type encoderType, Type decoderType)
		{
			// ensuring native is inited
			NativeHelper.SetNativeImplementation(true);
			var bufferIn = new byte[] { 1, 2, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4 };
			var encoder = (StreamEncoder)Activator.CreateInstance(encoderType);
			encoder.Init(bufferIn.Length);
			var compr = new byte[bufferIn.Length];
			var comprCnt = encoder.CompressBlock(bufferIn, 0, bufferIn.Length, 0, compr, 0);
			Console.WriteLine(comprCnt);

			var bufferOut = new byte[bufferIn.Length];
			var decoder = (StreamDecoder)Activator.CreateInstance(decoderType);
			decoder.Init(bufferIn.Length);
			var cnt = decoder.DecompressBlock(compr, 0, comprCnt, bufferOut, 0, bufferOut.Length);
			Assert.That(cnt, Is.EqualTo(bufferIn.Length));
			CollectionAssert.AreEqual(bufferIn, bufferOut);
		}

		[Test]
		[TestCase(BlazerAlgorithm.NoCompress)]
		[TestCase(BlazerAlgorithm.Block)]
		[TestCase(BlazerAlgorithm.Stream)]
		public void DataArrayCompressionHelper_Should_Encode_Decode(BlazerAlgorithm algorithm)
		{
			var bufferIn = new byte[] { 1, 2, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4 };
			var arr = DataArrayCompressorHelper.CompressDataToArray(bufferIn, EncoderDecoderFactory.GetEncoder(algorithm));
			var bufferOut = DataArrayCompressorHelper.DecompressDataArray(arr, EncoderDecoderFactory.GetDecoder(algorithm));
			CollectionAssert.AreEqual(bufferIn, bufferOut);
		}

		[Test]
		[TestCase(BlazerAlgorithm.NoCompress)]
		[TestCase(BlazerAlgorithm.Block)]
		[TestCase(BlazerAlgorithm.Stream)]
		public void DataArrayCompressionHelper_Should_Encode_Decode_With_Offset(BlazerAlgorithm algorithm)
		{
			var bufferIn = new byte[] { 1, 2, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4 };
			var arr = DataArrayCompressorHelper.CompressDataToArray(bufferIn, 1, bufferIn.Length - 2, EncoderDecoderFactory.GetEncoder(algorithm));
			var dupArray = new byte[arr.Length + 10];
			Buffer.BlockCopy(arr, 0, dupArray, 2, arr.Length);
			var bufferOut = DataArrayCompressorHelper.DecompressDataArray(dupArray, 2, arr.Length, EncoderDecoderFactory.GetDecoder(algorithm));
			CollectionAssert.AreEqual(bufferIn.Skip(1).Take(bufferIn.Length - 2).ToArray(), bufferOut);
		}
	}
}
