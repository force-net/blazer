using System.IO;
using System.Net;
using System.Net.Sockets;

namespace Blazer.Net.Tests
{
	public static class TcpClientExtensions
	{
#if NETCORE
		public static TcpClient AcceptTcpClient(this TcpListener listener)
		{
			return listener.AcceptTcpClientAsync().Result;
		}
		
		public static void Connect(this TcpClient client, IPEndPoint  endpoint)
		{
			client.ConnectAsync(endpoint.Address, endpoint.Port).Wait();
		}

		public static void Close(this TcpClient client)
		{
			client.Dispose();
		}

		public static void Close(this Stream stream)
		{
			stream.Dispose();
		}
#endif
	}
}