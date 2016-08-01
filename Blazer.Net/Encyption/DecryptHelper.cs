using System;
using System.Linq;
using System.Security.Cryptography;

namespace Force.Blazer.Encyption
{
	public class NullDecryptHelper
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

	public class DecryptHelper : NullDecryptHelper
	{
		private Aes _aes;

		private string _password;

		private byte[] _buffer;

		public DecryptHelper(string password)
		{
			_password = password;
		}

		public override int GetHeaderLength()
		{
			return 32;
		}

		public override void Init(byte[] buffer, int maxBlockSize)
		{
			if (buffer.Length != GetHeaderLength())
				throw new InvalidOperationException("Invalid header");

			_buffer = new byte[AdjustLength(maxBlockSize)];

			var salt = new byte[8];
			Buffer.BlockCopy(buffer, 0, salt, 0, 8);
			var random = new byte[8];
			Buffer.BlockCopy(buffer, 8, random, 0, 8);
			var pass = new Rfc2898DeriveBytes(_password, salt, 4096);
			_password = null;
			_aes = Aes.Create();
			_aes.Key = pass.GetBytes(32);
			// zero. it is ok
			_aes.IV = new byte[16];
			_aes.Mode = CipherMode.CBC;
			_aes.Padding = PaddingMode.Zeros;
			using (var decryptor = _aes.CreateDecryptor())
			{
				var decoded = decryptor.TransformFinalBlock(buffer, 16, 16);
				if (decoded.Take(8).Where((t, i) => random[i] != t).Any())
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
	}
}
