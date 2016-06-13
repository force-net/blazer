using System;
using System.Security.Cryptography;

namespace Force.Blazer.Encyption
{
	public class NullEncryptHelper
	{
		public virtual byte[] Encrypt(byte[] data, int offset, int count)
		{
			return data;
		}

		public virtual byte[] AppendHeader(byte[] header)
		{
			return header;
		}
	}

	public class EncryptHelper : NullEncryptHelper
	{
		private readonly Aes _aes;

		private readonly byte[] _headerToWrite;

		public EncryptHelper(string password)
		{
			var salt = new byte[16];
			RandomNumberGenerator.Create().GetBytes(salt);
			var pass = new Rfc2898DeriveBytes(password, salt);
			_aes = Aes.Create();
			_aes.Key = pass.GetBytes(32);
			// zero. it is ok
			_aes.IV = new byte[16];
			_aes.Mode = CipherMode.CBC;
			_aes.Padding = PaddingMode.Zeros;
			_headerToWrite = new byte[32];
			Buffer.BlockCopy(salt, 0, _headerToWrite, 0, 16);

			using (var encryptor = _aes.CreateEncryptor())
			{
				var encoded = encryptor.TransformFinalBlock(salt, 0, 16);
				Buffer.BlockCopy(encoded, 0, _headerToWrite, 16, 16);
			}
		}

		public override byte[] Encrypt(byte[] data, int offset, int count)
		{
			using (var encryptor = _aes.CreateEncryptor())
				return encryptor.TransformFinalBlock(data, offset, count);
		}

		public override byte[] AppendHeader(byte[] header)
		{
			if (header == null) header = new byte[0];
			var res = new byte[header.Length + _headerToWrite.Length];
			Buffer.BlockCopy(header, 0, res, 0, header.Length);
			Buffer.BlockCopy(_headerToWrite, 0, res, header.Length, _headerToWrite.Length);
			return res;
		}
	}
}
