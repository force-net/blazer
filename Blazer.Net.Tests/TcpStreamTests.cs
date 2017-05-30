using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

using Force.Blazer;

using NUnit.Framework;

namespace Blazer.Net.Tests
{
	// in these test we ensure that all data is correctly processed with live network streams
	[TestFixture]
	public class TcpStreamTests
	{
		[Test]
		public void EnsureUsualStreamWriteFull()
		{
			var l = new TcpListener(new IPEndPoint(IPAddress.Loopback, 9990));
			l.Start();
			try
			{
				Task.Factory.StartNew(
					() =>
						{
							var cl = l.AcceptTcpClient();
							cl.GetStream().Write(new byte[] { 0x1, 0x2, 0x3 }, 0, 3);
							cl.Close();
						});

				var c = new TcpClient();
				c.Connect(new IPEndPoint(IPAddress.Loopback, 9990));
				var stream = c.GetStream();
				var buf = new byte[100];
				Assert.That(stream.Read(buf, 0, buf.Length), Is.EqualTo(3));
				Assert.That(buf[1], Is.EqualTo(2));
			}
			finally 
			{
				l.Stop();
			}
		}

		[Test]
		public void EnsureBlazerStreamWriteFull()
		{
			var l = new TcpListener(new IPEndPoint(IPAddress.Loopback, 9990));
			l.Start();
			try
			{
				Task.Factory.StartNew(
					() =>
					{
						var cl = l.AcceptTcpClient();
						var networkStream = new BlazerInputStream(cl.GetStream(), BlazerCompressionOptions.CreateStream());
						networkStream.Write(new byte[] { 0x1, 0x2, 0x3 }, 0, 3);
						networkStream.Close();
						cl.Close();
					});

				var c = new TcpClient();
				c.Connect(new IPEndPoint(IPAddress.Loopback, 9990));
				var stream = new BlazerOutputStream(c.GetStream());
				var buf = new byte[100];
				Assert.That(stream.Read(buf, 0, buf.Length), Is.EqualTo(3));
				Assert.That(buf[1], Is.EqualTo(2));
			}
			finally 
			{
				l.Stop();
			}
		}

		[Test]
		public void EnsureBlazerStreamWriteFull_ControlBlock()
		{
			var l = new TcpListener(new IPEndPoint(IPAddress.Loopback, 9990));
			l.Start();
			try
			{
				Task.Factory.StartNew(
					() =>
					{
						var cl = l.AcceptTcpClient();
						var str = cl.GetStream();
						var networkStream = new BlazerInputStream(str, BlazerCompressionOptions.CreateStream());
						var b = new byte[80];
						b[1] = 0x2;
						networkStream.WriteControlData(b, 0, b.Length);
						networkStream.Flush();
						networkStream.Close();
						cl.Close();
					});

				var c = new TcpClient();
				c.Connect(new IPEndPoint(IPAddress.Loopback, 9990));
				var readedCnt = 0;
				var stream = new BlazerOutputStream(c.GetStream(), new BlazerDecompressionOptions() { ControlDataCallback = (bytes, i, arg3) => readedCnt = arg3 });
				var buf = new byte[100];
				Assert.That(stream.Read(buf, 0, buf.Length), Is.EqualTo(0));
				Assert.That(readedCnt, Is.EqualTo(80));
			}
			finally 
			{
				l.Stop();
			}
		}

		[Test]
		public void EnsureBlazerStreamWriteFull_EncryptFull()
		{
			var l = new TcpListener(new IPEndPoint(IPAddress.Loopback, 9990));
			l.Start();
			try
			{
				Task.Factory.StartNew(
					() =>
					{
						var cl = l.AcceptTcpClient();
						var opt = BlazerCompressionOptions.CreateStream();
						opt.Comment = "test";
						var networkStream = new BlazerInputStream(cl.GetStream(), opt);
						networkStream.WriteControlData(new byte[0], 0, 0);
						networkStream.Close();
						cl.Close();
					});

				var c = new TcpClient();
				c.Connect(new IPEndPoint(IPAddress.Loopback, 9990));
				var stream = new BlazerOutputStream(c.GetStream(), new BlazerDecompressionOptions());
				var buf = new byte[100];
				Assert.That(stream.Read(buf, 0, buf.Length), Is.EqualTo(0));
				Assert.That(stream.Comment, Is.EqualTo("test"));
			}
			finally
			{
				l.Stop();
			}
		}
	}
}
