using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

using Force.Blazer.Algorithms;

namespace Force.Blazer.Encyption
{
	public class NullEncryptHelper
	{
		public virtual BufferInfo Encrypt(byte[] data, int offset, int length)
		{
			return new BufferInfo(data, offset, length);
		}

		public virtual byte[] AppendHeader(byte[] header)
		{
			return header;
		}
	}

	[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Reviewed. Suppression is OK here.")]
	public class EncryptHelper : NullEncryptHelper
	{
		private const int PrefixSize = 8;

		private const int PbkIterations = 20000;

		private readonly Aes _aes;

		private readonly byte[] _headerToWrite;

		private readonly RandomNumberGenerator _rng;

		private readonly Random _random;

		private readonly byte[] _buffer;

		private readonly byte[] _randomBlock8;

		private readonly byte[] _randomBlock16;

		private int AdjustLength(int inLength)
		{
			return ((inLength + PrefixSize - 1) | 15) + 1;
		}

		public EncryptHelper(string password, int maxBufferSize)
		{
			_buffer = new byte[AdjustLength(maxBufferSize)]; // additional 8 bytes for adding random data to every block and whole block is multiple by 16
			_randomBlock8 = new byte[PrefixSize];
			_randomBlock16 = new byte[16];

			// we write to header 8 byte of salt + 8 byte of random data
			// after that, we encrypt 8 bytes of random data (pad with static to 16 and write first 8)
			// this 8 bytes will be used for checking correctness on decryption
			// this is fine for fast checking "is password correct", but does not
			// give full information about is it the required password
			_rng = RandomNumberGenerator.Create();
			_random = new Random();
			var salt = new byte[8];
			_rng.GetBytes(salt);
			var pass = new Rfc2898DeriveBytes(password, salt, PbkIterations);
			_aes = Aes.Create();
			_aes.Key = pass.GetBytes(32);
			// zero. it is ok - we use data with salted random and do not need to use additional IV here
			_aes.IV = new byte[16];
			_aes.Mode = CipherMode.CBC;
			_aes.Padding = PaddingMode.None; // other padding will add additional block, we manually will add random padding
			_headerToWrite = new byte[24];

			var random = new byte[8];
			_rng.GetBytes(random);
			var toEncrypt = new byte[16];

			Buffer.BlockCopy(random, 0, toEncrypt, 0, 8);

			Buffer.BlockCopy(new[] { (byte)'B', (byte)'l', (byte)'a', (byte)'z', (byte)'e', (byte)'r', (byte)'!', (byte)'?' }, 0, toEncrypt, 8, 8);

			Buffer.BlockCopy(salt, 0, _headerToWrite, 0, 8);
			Buffer.BlockCopy(random, 0, _headerToWrite, 8, 8);

			// currently, we use salt for password, so every encryption has own key, as result we do not need to use other values for counter
			// nonce is useful when password is static
			// _counter = ((long)salt[0] << 0) | ((long)salt[1] << 8) | ((long)salt[2] << 16) | ((long)salt[3] << 24) | ((long)salt[4] << 32) | ((long)salt[5] << 40) | ((long)salt[6] << 48) | ((long)salt[7] << 56);
			_counter = 0;

			using (var encryptor = _aes.CreateEncryptor())
			{
				var encoded = encryptor.TransformFinalBlock(toEncrypt, 0, 16);
				Buffer.BlockCopy(encoded, 0, _headerToWrite, 16, 8);
			}
		}

		private long _counter;

		[SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1107:CodeMustNotContainMultipleStatementsOnOneLine", Justification = "Reviewed. Suppression is OK here.")]
		public override BufferInfo Encrypt(byte[] data, int offset, int length)
		{
			var count = length - offset;
			// we can use random iv here, but is simplier to use zero iv and write some dummy bytes
			// in block header. on decoding, we just skip it
			// this is will elimitate CBC problem with same blocks (if data is repeatable) 
			using (var encryptor = _aes.CreateEncryptor())
			{
				// currently, we're not supporting multi-threading, so, we do not need to use Interlocked operations
				var c = _counter++;
				_buffer[0] = (byte)((c >> 00) & 0xff); _buffer[1] = (byte)((c >> 08) & 0xff); _buffer[2] = (byte)((c >> 16) & 0xff); _buffer[3] = (byte)((c >> 24) & 0xff);
				_buffer[4] = (byte)((c >> 32) & 0xff); _buffer[5] = (byte)((c >> 40) & 0xff); _buffer[6] = (byte)((c >> 48) & 0xff); _buffer[7] = (byte)((c >> 56) & 0xff); 
				// _rng.GetBytes(_randomBlock8);
				// copying prefix
				// Buffer.BlockCopy(_randomBlock8, 0, _buffer, 0, PrefixSize);

				// copying real data
				Buffer.BlockCopy(data, offset, _buffer, PrefixSize, count);

				var addRandomCnt = 16 - ((count + PrefixSize) & 15);
				if (addRandomCnt < 16)
				{
					// here is no security required, but it faster
					_rng.GetBytes(_randomBlock16);
					Buffer.BlockCopy(_randomBlock16, 0,  _buffer, PrefixSize + count, addRandomCnt);
				}
				else
				{
					addRandomCnt = 0;
				}

				var encLength = PrefixSize + count + addRandomCnt;
				encryptor.TransformBlock(_buffer, 0, encLength, _buffer, 0);

				return new BufferInfo(_buffer, 0, encLength);
			}
		}

		public override byte[] AppendHeader(byte[] header)
		{
			if (header == null) header = new byte[0];
			var res = new byte[header.Length + _headerToWrite.Length];
			Buffer.BlockCopy(header, 0, res, 0, header.Length);
			Buffer.BlockCopy(_headerToWrite, 0, res, header.Length, _headerToWrite.Length);
			return res;
		}

		public static Stream ConvertStreamToEncyptionStream(Stream inner, string password)
		{
			var rng = RandomNumberGenerator.Create();
			var salt = new byte[8];
			rng.GetBytes(salt);
			var pass = new Rfc2898DeriveBytes(password, salt, 4096);
			var aes = Aes.Create();
			aes.Key = pass.GetBytes(32);
			// zero. it is ok - we use random password (due salt), so, anyway it will be different
			aes.IV = new byte[16];
			aes.Mode = CipherMode.CBC;
			aes.Padding = PaddingMode.PKCS7; // here we will use such padding
			inner.Write(salt, 0, 8);
			return new CryptoStream(inner, aes.CreateEncryptor(), CryptoStreamMode.Write);
		}
	}
}
