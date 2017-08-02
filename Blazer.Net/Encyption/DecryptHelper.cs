using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

using Force.Blazer.Algorithms;

namespace Force.Blazer.Encyption
{
	internal class NullDecryptHelper
	{
		public virtual BufferInfo Decrypt(byte[] data, int offset, int length)
		{
			return new BufferInfo(data, offset, length);
		}

		public virtual int AdjustLength(int inLength)
		{
			return inLength;
		}

		public virtual int GetHeaderLength()
		{
			return 0;
		}

		public virtual void Init(byte[] header, int maxBlockSize)
		{
		}
	}

	[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Reviewed. Suppression is OK here.")]
	internal class DecryptHelper : NullDecryptHelper
	{
		private const int PbkIterations = 20000;

		private Aes _aes;

		private string _password;

		private byte[] _buffer;

		public DecryptHelper(string password)
		{
			_password = password;
		}

		public override int GetHeaderLength()
		{
			return 24;
		}

		public override void Init(byte[] buffer, int maxBlockSize)
		{
			if (buffer.Length != GetHeaderLength())
				throw new InvalidOperationException("Invalid header");

			_buffer = new byte[AdjustLength(maxBlockSize)];

			var salt = new byte[8];
			Buffer.BlockCopy(buffer, 0, salt, 0, 8);
			var pass = new Rfc2898DeriveBytes(_password, salt, PbkIterations);
			_password = null;
			_aes = Aes.Create();
			_aes.Key = pass.GetBytes(32);
			// zero. it is ok
			_aes.IV = new byte[16];
			_aes.Mode = CipherMode.CBC;
			_aes.Padding = PaddingMode.Zeros;

			using (var encryptor = _aes.CreateEncryptor())
			{
				var toEncrypt = new byte[16];
				Buffer.BlockCopy(buffer, 8, toEncrypt, 0, 8);
				Buffer.BlockCopy(new[] { (byte)'B', (byte)'l', (byte)'a', (byte)'z', (byte)'e', (byte)'r', (byte)'!', (byte)'!' }, 0, toEncrypt, 8, 8);
				var encoded = encryptor.TransformFinalBlock(toEncrypt, 0, 16);
				if (encoded.Take(8).Where((t, i) => buffer[i + 16] != t).Any())
					throw new InvalidOperationException("Invalid password");
			}
		}

		public override BufferInfo Decrypt(byte[] data, int offset, int length)
		{
			using (var decryptor = _aes.CreateDecryptor())
			{
				var cnt = decryptor.TransformBlock(data, offset, length - offset, _buffer, 0);
				// dummy data in header (8)
				return new BufferInfo(_buffer, 8, cnt);
			}
		}

		public override int AdjustLength(int inLength)
		{
			return ((inLength - 1 + 8) | 15) + 1;
		}

		public static Stream ConvertStreamToDecyptionStream(Stream inner, string password)
		{
			var salt = new byte[8];
			// ensure read 8 bytes
			var cnt = 8;
			while (cnt > 0)
			{
				var readed = inner.Read(salt, 8 - cnt, cnt);
				if (readed == 0 && cnt > 0)
					throw new InvalidOperationException("Invalid input stream");
				cnt -= readed;
			}
			
			var pass = new Rfc2898DeriveBytes(password, salt, 4096);
			var aes = Aes.Create();
			aes.Key = pass.GetBytes(32);
			// zero. it is ok - we use random password (due salt), so, anyway it will be different
			aes.IV = new byte[16];
			aes.Mode = CipherMode.CBC;
			var cryptoTransform = aes.CreateDecryptor();
#if NETCORE
			aes.Padding = PaddingMode.PKCS7; // here we will use such padding
			cryptoTransform = new Iso10126TransformEmulator(cryptoTransform);
#else
			aes.Padding = PaddingMode.ISO10126; // here we will use such padding
#endif
			aes.Padding = PaddingMode.PKCS7; // here we will use such padding
			return new CryptoStream(inner, cryptoTransform, CryptoStreamMode.Read);
		}
	}
}
