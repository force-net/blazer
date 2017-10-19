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

		/// <summary>
		/// Extracts body to separate byte array
		/// </summary>
		/// <returns>new array</returns>
		public byte[] ExtractToSeparateArray()
		{
			return ExtractToSeparateArray(0);
		}

		/// <summary>
		/// Extracts body to separate byte array
		/// </summary>
		/// <param name="offset">additional offset for new array</param>
		/// <returns>new array</returns>
		public byte[] ExtractToSeparateArray(int offset)
		{
			var res = new byte[Count + offset];
			System.Buffer.BlockCopy(Buffer, Offset, res, offset, Count);
			return res;
		}
	}
}
