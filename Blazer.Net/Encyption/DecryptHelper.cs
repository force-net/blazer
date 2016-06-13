using System;
using System.Linq;
using System.Security.Cryptography;

namespace Force.Blazer.Encyption
{
	public class NullDecryptHelper
	{
		public virtual byte[] Decrypt(byte[] data, int offset, int count)
		{
			return data;
		}

		public virtual int AdjustLength(int inLength)
		{
			return inLength;
		}

		public virtual int GetHeaderLength()
		{
			return 0;
		}

		public virtual void Init(byte[] buffer)
		{
		}
	}

	public class DecryptHelper : NullDecryptHelper
	{
		private Aes _aes;

		private string _password;

		public DecryptHelper(string password)
		{
			_password = password;
		}

		public override int GetHeaderLength()
		{
			return 32;
		}

		public override void Init(byte[] buffer)
		{
			if (buffer.Length != GetHeaderLength())
				throw new InvalidOperationException("Invalid header");

			var salt = new byte[16];
			Buffer.BlockCopy(buffer, 0, salt, 0, 16);
			var pass = new Rfc2898DeriveBytes(_password, salt);
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
				if (decoded.Where((t, i) => salt[i] != t).Any())
					throw new InvalidOperationException("Invalid password");
			}
		}

		public override byte[] Decrypt(byte[] data, int offset, int count)
		{
			using (var decryptor = _aes.CreateDecryptor())
				return decryptor.TransformFinalBlock(data, offset, count);
		}

		public override int AdjustLength(int inLength)
		{
			return ((inLength - 1) | 15) + 1;
		}
	}
}
