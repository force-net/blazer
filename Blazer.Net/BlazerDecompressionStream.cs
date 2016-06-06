using System;
using System.IO;

using Force.Blazer.Algorithms;
using Force.Blazer.Algorithms.Crc32C;

namespace Force.Blazer
{
	public class BlazerDecompressionStream : Stream
	{
		#region Stream stub

		public override void Flush()
		{
			throw new NotSupportedException();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}

		public override bool CanRead
		{
			get
			{
				return true;
			}
		}

		public override bool CanSeek
		{
			get
			{
				return false;
			}
		}

		public override bool CanWrite
		{
			get
			{
				return false;
			}
		}

		public override long Length
		{
			get
			{
				return -1L;
			}
		}

		public override long Position
		{
			get
			{
				return -1L;
			}

			set
			{
				throw new NotSupportedException();
			}
		}

		#endregion

		private readonly Stream _innerStream;

		private IDecoder _decoder;

		private byte[] _inBuffer;

		private int _inLength;
		
		private bool _includeCrc;
		private bool _includeFooter;

		private int _maxUncompressedBlockSize;

		private byte _algorithmId;

		public BlazerDecompressionStream(Stream innerStream, IDecoder decoder, BlazerFlags flags)
		{
			_innerStream = innerStream;
			if (!_innerStream.CanRead)
				throw new InvalidOperationException("Base stream is invalid");

			if ((flags & BlazerFlags.IncludeHeader) != 0)
				throw new InvalidOperationException("Flags cannot contains IncludeHeader flags");

			_decoder = decoder;
			_maxUncompressedBlockSize = 1 << ((((int)flags) & 15) + 9);
			_decoder.Init(_maxUncompressedBlockSize, GetNextChunk);
			_algorithmId = (byte)_decoder.GetAlgorithmId();
			_inBuffer = new byte[_maxUncompressedBlockSize];
			_includeCrc = (flags & BlazerFlags.IncludeCrc) != 0;
			_includeFooter = (flags & BlazerFlags.IncludeFooter) != 0;
		}

		public BlazerDecompressionStream(Stream innerStream, BlazerAlgorithm algorithm, BlazerFlags flags)
			: this(innerStream, EncoderDecoderFactory.GetDecoder(algorithm), flags)
		{
		}

		public BlazerDecompressionStream(Stream innerStream)
		{
			_innerStream = innerStream;
			_innerStream = innerStream;
			if (!_innerStream.CanRead)
				throw new InvalidOperationException("Base stream is invalid");
			ReadAndValidateHeader();
		}

		protected override void Dispose(bool disposing)
		{
			_innerStream.Dispose();
			_decoder.Dispose();
			base.Dispose(disposing);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return _decoder.Read(buffer, offset, count);
		}

		private void ReadAndValidateHeader()
		{
			var buf = new byte[8];
			if (!EnsureRead(buf, 0, 8))
				throw new InvalidOperationException("Invalid Stream");
			if (buf[0] != 'B' || buf[1] != 'L' || buf[2] != 'Z')
				throw new InvalidOperationException("This is not blazer archive");
			if (buf[3] != 0x00)
				throw new InvalidOperationException("Stream is created in new version of archiver. Please, update library.");
			BlazerFlags flags = (BlazerFlags)(buf[4] | (buf[5] << 8) | (buf[6] << 16) | ((uint)buf[7] << 24));

			_decoder = EncoderDecoderFactory.GetDecoder((BlazerAlgorithm)((((uint)flags) >> 4) & 15));
			_maxUncompressedBlockSize = 1 << ((((int)flags) & 15) + 9);
			_decoder.Init(_maxUncompressedBlockSize, GetNextChunk);
			_algorithmId = (byte)_decoder.GetAlgorithmId();
			_inBuffer = new byte[_maxUncompressedBlockSize];
			_includeCrc = (flags & BlazerFlags.IncludeCrc) != 0;
			_includeFooter = (flags & BlazerFlags.IncludeFooter) != 0;

			if (_includeFooter && _innerStream.CanSeek)
			{
				var position = _innerStream.Seek(0, SeekOrigin.Current);
				_innerStream.Seek(-4, SeekOrigin.End);
				if (!EnsureRead(buf, 0, 4))
					throw new Exception("Footer is missing");
				ValidateFooter(buf);
				_innerStream.Seek(position, SeekOrigin.Begin);
			}
		}

		private void ValidateFooter(byte[] footer)
		{
			if (footer[0] != 0xff || footer[1] != (byte)'Z' || footer[2] != (byte)'L' || footer[3] != (byte)'B')
				throw new InvalidOperationException("Invalid footer. Possible stream was truncated");
		}

		private readonly byte[] _sizeBlock = new byte[4];

		private byte _encodingType;

		private bool GetNextChunk()
		{
			// end of stream
			if (!EnsureRead(_sizeBlock, 0, 4)) return false;
			
			// if (_sizeBlock[0] != 0x42)
			//	throw new InvalidOperationException("Invalid header");
			_encodingType = _sizeBlock[0];
			
			// empty footer
			if (_encodingType == 0xff)
			{
				ValidateFooter(_sizeBlock);
				return false;
			}

			if (_encodingType != 0 && _encodingType != _algorithmId)
				throw new InvalidOperationException("Invalid header");
			var inLength = ((_sizeBlock[1] << 0) | (_sizeBlock[2] << 8) | _sizeBlock[3] << 16) + 1;
			if (inLength > _maxUncompressedBlockSize)
				throw new InvalidOperationException("Invalid block size");

			uint passedCrc = 0;

			if (_includeCrc)
			{
				if (!EnsureRead(_sizeBlock, 0, 4))
					throw new InvalidOperationException("Missing Crc32c in stream");
				passedCrc = ((uint)_sizeBlock[0] << 0) | (uint)_sizeBlock[1] << 8 | (uint)_sizeBlock[2] << 16 | (uint)_sizeBlock[3] << 24;
			}

			if (!EnsureRead(_inBuffer, 0, inLength))
				throw new InvalidOperationException("Invalid block data");

			if (_includeCrc)
			{
				var realCrc = Crc32C.Calculate(_inBuffer, 0, inLength);
				
				if (realCrc != passedCrc)
					throw new InvalidOperationException("Invalid CRC32C data in passed block. It seems, data error is occured.");
			}

			_inLength = inLength;
			_decoder.ProcessBlock(_inBuffer, _inLength, _encodingType != 0x00);
			return true;
		}

		private bool EnsureRead(byte[] buffer, int offset, int size)
		{
			var sizeOrig = size;
			while (true)
			{
				var cnt = _innerStream.Read(buffer, offset, size);
				if (cnt == 0)
				{
					if (size == sizeOrig) return false;
					throw new InvalidOperationException("Invalid data");
				}

				size -= cnt;
				if (size == 0) return true;
				offset += cnt;
			}
		}
	}
}
