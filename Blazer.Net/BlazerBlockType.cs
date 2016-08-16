namespace Force.Blazer
{
	/// <summary>
	/// Block type of Blazer archive
	/// </summary>
	public enum BlazerBlockType : byte
	{
		/// <summary>
		/// Empty control block
		/// </summary>
		ControlDataEmpty = 0xf0,

		/// <summary>
		/// Control block
		/// </summary>
		ControlData = 0xf1,

		/// <summary>
		/// File info block
		/// </summary>
		Comment = 0xf9,

		// FileInfoIndex = 0xfc

		/// <summary>
		/// File info block
		/// </summary>
		FileInfo = 0xfd,

		// FileInfoRef = 0xfe

		/// <summary>
		/// Footer block
		/// </summary>
		Footer = 0xff
	}
}
