using System;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace Blazer.Net.Tests
{
	[TestFixture]
	public class StreamEncoderTests
	{
		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
#if NETCORE
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
		}

		[Test]
		public void Test_AAAAAAAA()
		{
			var buf = new byte[] { 1, 1, 1, 1, 1, 1, 1, 1 };
			var len = IntegrityHelper.StreamEncoderCheckCompressDecompress(buf);
			// 1 times 1, then 7 times ref -1
			Assert.That(len, Is.EqualTo(3));
		}

		[Test]
		public void Test_AAAAAAAABBBBBBBB()
		{
			var buf = new byte[] { 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2 };
			var len = IntegrityHelper.StreamEncoderCheckCompressDecompress(buf);
			Assert.That(len, Is.LessThanOrEqualTo(16));
		}

		[Test]
		public void Test_1000A()
		{
			// byte 0: short backref, 1 lit, 15 repetitions
			// byte 1: backref 0 + 1
			// byte 2: 254 repetitions + 2 byte
			// byte 3-4: 1d7 repetitions. 471 + 256 + 253 + 15 + 4 = 999
			// 1F-00-FE-01-D7-01 (6)
			var buf = Enumerable.Range(0, 1000).Select(x => (byte)1).ToArray();
			var len = IntegrityHelper.StreamEncoderCheckCompressDecompress(buf);
			// 1 times 1, then 7 times ref -1
			Assert.That(len, Is.EqualTo(6));
		}

		[Test]
		public void Test_ByteBorder_Repetitions()
		{
			for (var i = 250; i < 300; i++)
			{
				var buf = Enumerable.Range(0, i).Select(x => (byte)1).ToArray();
				IntegrityHelper.StreamEncoderCheckCompressDecompress(buf);
			}

			for (var i = 65700; i < 65800; i++)
			{
				var buf = Enumerable.Range(0, i).Select(x => (byte)1).ToArray();
				IntegrityHelper.StreamEncoderCheckCompressDecompress(buf);
			}
		}

		[Test]
		public void Test_HabaxHabax()
		{
			var buf = new byte[] { 2, 1, 3, 1, 4, 2, 1, 3, 1, 4 };
			var len = IntegrityHelper.StreamEncoderCheckCompressDecompress(buf);
			Assert.That(len, Is.LessThan(8));
		}

		[Test]
		public void Test_Cycle_20000_SemiRandom()
		{
			var r = new Random(12346);
			var buf = Enumerable.Range(0, 20000).Select(x => (byte)r.Next(2)).ToArray();
			var len = IntegrityHelper.StreamEncoderCheckCompressDecompress(buf);
			// 1 times 1, then 7 times ref -1
			Assert.That(len, Is.LessThan(20000));
		}

		[Test]
		public void Test_Cycle_120000_SemiRandom()
		{
			var r = new Random(12346);
			var buf = Enumerable.Range(0, 120000).Select(x => (byte)r.Next(2)).ToArray();
			var len = IntegrityHelper.StreamEncoderCheckCompressDecompress(buf);
			// 1 times 1, then 7 times ref -1
			Assert.That(len, Is.LessThan(120000));
		}

		[Test]
		public void Test_Text()
		{
			var buf = Encoding.GetEncoding(1251).GetBytes(@"
Когда то давно Московское метро замышлялось как гигантское бомбоубежище, способное спасти десятки тысяч жизней. Мир стоял на пороге гибели, но тогда ее удалось отсрочить. Дорога, по которой идет человечество, вьется, как спираль, и однажды оно снова окажется на краю пропасти. Когда мир будет рушиться, метро окажется последним пристанищем человека перед тем, как он канет в ничто.
— Кто это там? Эй, Артем! Глянь ка!
Артем нехотя поднялся со своего места у костра и, перетягивая со спины на грудь автомат, двинулся во тьму. Стоя на самом краю освещенного пространства, он демонстративно, как можно громче и внушительней, щелкнул затвором и хрипло крикнул: — Стоять! Пароль!");
			IntegrityHelper.StreamEncoderCheckCompressDecompress(buf);
		}

		[Test]
		public void Test_Zero_Bytes()
		{
			var len = IntegrityHelper.StreamEncoderCheckCompressDecompress(new byte[0]);
			Assert.That(len, Is.EqualTo(0));
		}
	}
}
