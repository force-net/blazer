using System;
using System.Linq;
using System.Text;

using Force.Crc32;
using NUnit.Framework;

using E = Force.Blazer.Algorithms.Crc32C.Crc32C;

namespace Blazer.Net.Tests
{
	[TestFixture]
	public class Crc32Tests
	{
		[TestCase("Hello", 3)]
		[TestCase("Nazdar", 0)]
		[TestCase("Ahoj", 1)]
		[TestCase("Very long text.Very long text.Very long text.Very long text.Very long text.Very long text.Very long text", 0)]
		[TestCase("Very long text.Very long text.Very long text.Very long text.Very long text.Very long text.Very long text", 3)]
		public void ResultConsistency(string text, int offset)
		{
			var bytes = Encoding.ASCII.GetBytes(text);

			var crc1 = E.Calculate(bytes.Skip(offset).ToArray());
			var crc2 = Crc32CAlgorithm.Append(0, bytes, offset, bytes.Length - offset);
			Assert.That(crc2, Is.EqualTo(crc1));
		}

		[Test]
		public void ResultConsistencyLong()
		{
			var bytes = new byte[30000];
			new Random().NextBytes(bytes);
			var crc1 = E.Calculate(bytes, 0, bytes.Length);
			var crc2 = Crc32CAlgorithm.Append(0, bytes, 0, bytes.Length);
			Assert.That(crc2, Is.EqualTo(crc1));
		}

		[Test]
		public void ResultConsistency2()
		{
			Assert.That(E.Calculate(new byte[] { 1 }), Is.EqualTo(Crc32CAlgorithm.Compute(new byte[] { 1 })));
			Assert.That(E.Calculate(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }), Is.EqualTo(Crc32CAlgorithm.Compute(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 })));
		}

		[Test]
		public void PartIsWhole()
		{
			var bytes = new byte[30000];
			new Random().NextBytes(bytes);
			var r1 = E.Calculate(0, bytes, 0, 15000);
			var r2 = E.Calculate(r1, bytes, 15000, 15000);
			var r3 = E.Calculate(0, bytes, 0, 30000);
			Assert.That(r2, Is.EqualTo(r3));
		}
	}
}
