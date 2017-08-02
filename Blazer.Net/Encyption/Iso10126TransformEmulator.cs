using System;
using System.Security.Cryptography;

namespace Force.Blazer.Encyption
{
	/// <summary>
	/// This class emulates ISO10126 AES transform for .NET Core
	/// </summary>
	public class Iso10126TransformEmulator : ICryptoTransform
	{
		private readonly ICryptoTransform _origTransofrm;

		/// <summary>
		/// Constructor. Origignal transform should be aes with PKCS7 padding
		/// </summary>
		/// <param name="origTransofrm"></param>
		public Iso10126TransformEmulator(ICryptoTransform origTransofrm)
		{
			_origTransofrm = origTransofrm;
		}

		/// <summary>
		/// Dispose current transform
		/// </summary>
		public void Dispose()
		{
			_origTransofrm.Dispose();
		}

		int ICryptoTransform.TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
		{
			return _origTransofrm.TransformBlock(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset);
		}

		byte[] ICryptoTransform.TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
		{
			var outBuffer = new byte[_origTransofrm.OutputBlockSize + inputCount];
			var dummyBuffer = new byte[_origTransofrm.InputBlockSize + inputCount];
			Buffer.BlockCopy(inputBuffer, inputOffset, dummyBuffer, 0, inputCount);
			_origTransofrm.TransformBlock(dummyBuffer, 0, dummyBuffer.Length, outBuffer, 0);
			Array.Resize(ref outBuffer, outBuffer.Length - outBuffer[outBuffer.Length - 1]);
			return outBuffer;
		}

		int ICryptoTransform.InputBlockSize
		{
			get
			{
				return _origTransofrm.InputBlockSize;
			}
		}

		int ICryptoTransform.OutputBlockSize
		{
			get
			{
				return _origTransofrm.OutputBlockSize;
			}
		}

		bool ICryptoTransform.CanTransformMultipleBlocks
		{
			get
			{
				return _origTransofrm.CanTransformMultipleBlocks;
			}
		}

		bool ICryptoTransform.CanReuseTransform
		{
			get
			{
				return _origTransofrm.CanReuseTransform;
			}
		}
	}
}
