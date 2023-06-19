using System;
using System.IO;
using UnityEngine;

namespace SuperNet.Examples.BCS {

	[Serializable]
	public class BCS_Config {

		// This class contains configuration values
		// That are loaded by BCS_Menu at startup
		
		public string ConsoleTitle = ""; // Console title
		public bool ConsoleEnabled = false; // True to initialize console
		public bool ServerDedicated = false; // True to automatically launch server
		public int ServerMaxConnections = 32; // Maximum allowed connections
		public int ServerPort = 0; // Listen port
		public int TargetFrameRate = 60; // Target frame rate

		public static BCS_Config Initialize() {

			// Create default configuration
			BCS_Config config = new BCS_Config();

			// Read command line arguments
			string[] commands = Environment.GetCommandLineArgs();

			try {

				// Find config file
				string file = null;
				for (int i = 0; i < commands.Length; i++) {
					if (commands[i] == "-config" && i + 1 < commands.Length) {
						file = commands[i + 1];
					}
				}

				if (file != null) {

					if (File.Exists(file)) {

						// Read configuration from disk
						string json = File.ReadAllText(file);
						config = JsonUtility.FromJson<BCS_Config>(json);

					} else {

						// Write default configuration to disk
						string json = JsonUtility.ToJson(config);
						File.WriteAllText(file, json);

					}

				}

			} catch (Exception exception) {
				Debug.LogException(exception);
			}

			// Read extra configuration
			for (int i = 0; i < commands.Length; i++) {
				if (commands[i] == "-server") {
					if (i + 1 < commands.Length) {
						if (int.TryParse(commands[i + 1], out int port)) {
							config.ServerPort = port;
						}
					}
					config.ServerDedicated = true;
				}
			}

			// Return config
			return config;

		}

	}

}
