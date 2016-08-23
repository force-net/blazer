using System;
using System.Linq;

using Force.Blazer.Algorithms;
using Force.Blazer.Algorithms.Patterned;

using NUnit.Framework;

namespace Blazer.Net.Tests
{
	[TestFixture(1)]
	[TestFixture(2)]
	[TestFixture(3)]
	public class PatternedCompressionTests
	{
		private readonly Type _compressorType;

		private BasePatternedCompressor GetCompressor()
		{
			return (BasePatternedCompressor)Activator.CreateInstance(_compressorType);
		}

		public PatternedCompressionTests(int type)
		{
			if (type == 1) _compressorType = typeof(StreamPatternedCompressor);
			else if (type == 2) _compressorType = typeof(StreamHighPatternedCompressor);
			else if (type == 3) _compressorType = typeof(BlockPatternedCompressor);
			else throw new NotImplementedException();
		}

		[Test]
		public void DataWithPattern_Should_Be_Correctly_Encoded_Decoded()
		{
			var ssed = GetCompressor();
			var pattern = new byte[100];
			pattern[1] = 42;
			ssed.PreparePattern(pattern, 0, pattern.Length);

			var data1 = new byte[10];
			data1[0] = 42;

			var data2 = new byte[10];
			data2[0] = 12;

			var tmpOut = new byte[ssed.CalculateMaxCompressedBufferLength(10)];
			var tmpOut2 = new byte[ssed.CalculateMaxCompressedBufferLength(10)];

			var cntPatterned = ssed.EncodeWithPattern(data1, 0, data1.Length, tmpOut, 0);
			var cntUnpatterned = StreamEncoder.CompressData(data1);
			// TODO: uncomment
			Assert.That(cntPatterned, Is.LessThan(cntUnpatterned.Length));
			var cntPatternUnpacked = ssed.DecodeWithPattern(tmpOut, 0, cntPatterned, tmpOut2, 0);
			Assert.That(cntPatternUnpacked, Is.EqualTo(10));
			CollectionAssert.AreEqual(data1, tmpOut2.Take(cntPatternUnpacked));

			// checking that we can repeat without failure
			cntPatterned = ssed.EncodeWithPattern(data2, 0, data2.Length, tmpOut, 0);
			cntPatternUnpacked = ssed.DecodeWithPattern(tmpOut, 0, cntPatterned, tmpOut2, 0);
			Assert.That(cntPatternUnpacked, Is.EqualTo(10));
			CollectionAssert.AreEqual(data2, tmpOut2.Take(cntPatternUnpacked));
		}

		[Test]
		public void DataWithPattern_Should_Be_Correctly_Encoded_Decoded_With_Offsets()
		{
			var ssed = GetCompressor();
			var pattern = new byte[100];
			pattern[1] = 42;
			ssed.PreparePattern(pattern, 1, pattern.Length - 1);

			var data1 = new byte[10];
			data1[1] = 42;

			var data2 = new byte[9];
			data1[0] = 42;

			var tmpOut = new byte[ssed.CalculateMaxCompressedBufferLength(10)];
			var tmpOut2 = new byte[ssed.CalculateMaxCompressedBufferLength(10)];

			var cntPatterned1 = ssed.EncodeWithPattern(data1, 1, data1.Length - 1, tmpOut, 1);
			var cntPatterned2 = ssed.EncodeWithPattern(data2, 0, data2.Length, tmpOut2, 0);
			Assert.That(cntPatterned1, Is.EqualTo(cntPatterned2));
			var cntPatternedUnpacked = ssed.DecodeWithPattern(tmpOut, 1, cntPatterned1, tmpOut2, 1);
			Assert.That(cntPatternedUnpacked, Is.EqualTo(9));
			CollectionAssert.AreEqual(data1.Skip(1), tmpOut2.Skip(1).Take(cntPatternedUnpacked));
		}

		[Test]
		public void DataWithPattern_Should_Be_Correctly_Encoded_Decoded_With_Simple_Interface()
		{
			var ssed = GetCompressor();
			var pattern = new byte[100];
			pattern[1] = 42;
			ssed.PreparePattern(pattern, 1, pattern.Length - 1);

			var data1 = new byte[10];
			data1[1] = 42;

			var patterned = ssed.EncodeWithPattern(data1);
			var patternedUnpacked = ssed.DecodeWithPattern(patterned);

			CollectionAssert.AreEqual(data1, patternedUnpacked);
		}

		[Test]
		public void Zero_Length_Should_not_Cause_Error()
		{
			var ssed = GetCompressor();
			var pattern = new byte[100];
			pattern[1] = 42;
			ssed.PreparePattern(pattern, 1, pattern.Length - 1);

			var data1 = new byte[0];

			var patterned = ssed.EncodeWithPattern(data1);
			Assert.That(patterned.Length, Is.EqualTo(1));
			var patternUnpacked = ssed.DecodeWithPattern(patterned);

			CollectionAssert.AreEqual(data1, patternUnpacked);
		}
	}
}
