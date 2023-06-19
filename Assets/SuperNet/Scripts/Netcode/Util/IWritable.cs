namespace SuperNet.Netcode.Util {

	/// <summary>
	/// Defines a serializable network payload.
	/// </summary>
	public interface IWritable {

		/// <summary>
		/// Serialize payload into the provided writer.
		/// </summary>
		/// <param name="writer">Writer to write to.</param>
		void Write(Writer writer);

	}

}
