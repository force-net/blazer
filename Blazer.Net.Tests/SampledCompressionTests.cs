using System;
using System.Linq;

using Force.Blazer.Algorithms;
using Force.Blazer.Algorithms.Sampled;

using NUnit.Framework;

namespace Blazer.Net.Tests
{
	[TestFixture(1)]
	[TestFixture(2)]
	[TestFixture(3)]
	public class SampledCompressionTests
	{
		private readonly Type _compressorType;

		private BaseSampledCompressor GetCompressor()
		{
			return (BaseSampledCompressor)Activator.CreateInstance(_compressorType);
		}

		public SampledCompressionTests(int type)
		{
			if (type == 1) _compressorType = typeof(StreamSampledCompressor);
			else if (type == 2) _compressorType = typeof(StreamHighSampledCompressor);
			else if (type == 3) _compressorType = typeof(BlockSampledCompressor);
			else throw new NotImplementedException();
		}

		[Test]
		public void DataWithSample_Should_Be_Correctly_Encoded_Decoded()
		{
			var ssed = GetCompressor();
			var sample = new byte[100];
			sample[1] = 42;
			ssed.PrepareSample(sample, 0, sample.Length);

			var data1 = new byte[10];
			data1[0] = 42;

			var data2 = new byte[10];
			data2[0] = 12;

			var tmpOut = new byte[ssed.CalculateMaxCompressedBufferLength(10)];
			var tmpOut2 = new byte[ssed.CalculateMaxCompressedBufferLength(10)];

			var cntSampled = ssed.EncodeWithSample(data1, 0, data1.Length, tmpOut, 0);
			var cntUnsampled = StreamEncoder.CompressData(data1);
			// TODO: uncomment
			Assert.That(cntSampled, Is.LessThan(cntUnsampled.Length));
			var cntSampledUnpacked = ssed.DecodeWithSample(tmpOut, 0, cntSampled, tmpOut2, 0);
			Assert.That(cntSampledUnpacked, Is.EqualTo(10));
			CollectionAssert.AreEqual(data1, tmpOut2.Take(cntSampledUnpacked));

			// checking that we can repeat without failure
			cntSampled = ssed.EncodeWithSample(data2, 0, data2.Length, tmpOut, 0);
			cntSampledUnpacked = ssed.DecodeWithSample(tmpOut, 0, cntSampled, tmpOut2, 0);
			Assert.That(cntSampledUnpacked, Is.EqualTo(10));
			CollectionAssert.AreEqual(data2, tmpOut2.Take(cntSampledUnpacked));
		}

		[Test]
		public void DataWithSample_Should_Be_Correctly_Encoded_Decoded_With_Offsets()
		{
			var ssed = GetCompressor();
			var sample = new byte[100];
			sample[1] = 42;
			ssed.PrepareSample(sample, 1, sample.Length - 1);

			var data1 = new byte[10];
			data1[1] = 42;

			var data2 = new byte[9];
			data1[0] = 42;

			var tmpOut = new byte[ssed.CalculateMaxCompressedBufferLength(10)];
			var tmpOut2 = new byte[ssed.CalculateMaxCompressedBufferLength(10)];

			var cntSampled1 = ssed.EncodeWithSample(data1, 1, data1.Length - 1, tmpOut, 1);
			var cntSampled2 = ssed.EncodeWithSample(data2, 0, data2.Length, tmpOut2, 0);
			Assert.That(cntSampled1, Is.EqualTo(cntSampled2));
			var cntSampledUnpacked = ssed.DecodeWithSample(tmpOut, 1, cntSampled1, tmpOut2, 1);
			Assert.That(cntSampledUnpacked, Is.EqualTo(9));
			CollectionAssert.AreEqual(data1.Skip(1), tmpOut2.Skip(1).Take(cntSampledUnpacked));
		}

		[Test]
		public void DataWithSample_Should_Be_Correctly_Encoded_Decoded_With_Simple_Interface()
		{
			var ssed = GetCompressor();
			var sample = new byte[100];
			sample[1] = 42;
			ssed.PrepareSample(sample, 1, sample.Length - 1);

			var data1 = new byte[10];
			data1[1] = 42;

			var sampled1 = ssed.EncodeWithSample(data1);
			var sampledUnpacked = ssed.DecodeWithSample(sampled1);

			CollectionAssert.AreEqual(data1, sampledUnpacked);
		}

		[Test]
		public void Zero_Length_Should_not_Cause_Error()
		{
			var ssed = GetCompressor();
			var sample = new byte[100];
			sample[1] = 42;
			ssed.PrepareSample(sample, 1, sample.Length - 1);

			var data1 = new byte[0];

			var sampled1 = ssed.EncodeWithSample(data1);
			Assert.That(sampled1.Length, Is.EqualTo(1));
			var sampledUnpacked = ssed.DecodeWithSample(sampled1);

			CollectionAssert.AreEqual(data1, sampledUnpacked);
		}
	}
}
