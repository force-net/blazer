namespace Force.Blazer.Algorithms
{
	/// <summary>
	/// Information about data buffer
	/// </summary>
	public struct BufferInfo
	{
		/// <summary>
		/// Data buffer
		/// </summary>
		public byte[] Buffer;

		/// <summary>
		/// Buffer offset
		/// </summary>
		public int Offset;

		/// <summary>
		/// Buffer data length (Offset + Count, right position of valid data in buffer)
		/// </summary>
		public int Length;

		/// <summary>
		/// Count of valid data in buffer)
		/// </summary>
		public int Count
		{
			get
			{
				return Length - Offset;
			}
		}

		/// <summary>
		/// BufferInfo constructor
		/// </summary>
		public BufferInfo(byte[] buffer, int offset, int length)
		{
			Buffer = buffer;
			Offset = offset;
			Length = length;
		}

		public byte[] ExtractToSeparateArray()
		{
			var res = new byte[Length];
			System.Buffer.BlockCopy(Buffer, Offset, res, 0, Count);
			return res;
		}
	}
}
