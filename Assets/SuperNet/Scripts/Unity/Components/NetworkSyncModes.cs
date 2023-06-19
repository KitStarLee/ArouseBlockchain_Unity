namespace SuperNet.Unity.Components {

	/// <summary>
	/// Syncronization mode for Vector3 values.
	/// </summary>
	public enum NetworkSyncModeVector3 : byte {
		None = 0b00000000,
		X    = 0b00000001,
		Y    = 0b00000010,
		Z    = 0b00000100,
		XY   = 0b00000011,
		XZ   = 0b00000101,
		YZ   = 0b00000110,
		XYZ  = 0b00000111,
	}

	/// <summary>
	/// Syncronization mode for Vector2 values.
	/// </summary>
	public enum NetworkSyncModeVector2 : byte {
		None = 0b00000000,
		X    = 0b00000001,
		Y    = 0b00000010,
		XY   = 0b00000011,
	}

	/// <summary>
	/// Syncronization method to use.
	/// </summary>
	public enum NetworkSyncModeMethod : byte {
		Update = 1,
		FixedUpdate = 2,
		LateUpdate = 3,
	}

}
