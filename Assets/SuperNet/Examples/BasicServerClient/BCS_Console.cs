using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace SuperNet.Examples.BCS {

	public class BCS_Console : MonoBehaviour {

		// Constants
		public const string InputPrefix = "> ";
		public const ConsoleColor ColorInput = ConsoleColor.Green;
		public const ConsoleColor ColorWarning = ConsoleColor.Yellow;
		public const ConsoleColor ColorError = ConsoleColor.Red;
		public const ConsoleColor ColorLog = ConsoleColor.White;
		
		// Event called every time the user presses enter
		public event Action<string> OnInputText;

		// Current console input
		public string InputText { get; private set; } = "";

		// Resources
		private bool ConsoleAllocated = false;
		private bool ConsoleRedirected = false;
		private TextWriter ConsoleOutput = null;
		
		private void Awake() {

			// Persist console across scenes
			DontDestroyOnLoad(this);

			// Register log callback to be called when Debug.Log is called
			Application.logMessageReceived += OnLogMessageReceived;

		}

		private void OnDestroy() {

			// Unregister log callback
			Application.logMessageReceived -= OnLogMessageReceived;

			// Shutdown console
			Shutdown();

		}

		private void Update() {

			// Check if console exists
			if (!ConsoleRedirected) {
				return;
			}

			// Check if console key is available
			if (!Console.KeyAvailable) {
				return;
			}

			// Read the next key
			ConsoleKeyInfo key = Console.ReadKey();

			if (key.Key == ConsoleKey.Enter) {

				// Remove input, write it to console and invoke event
				string text = InputText;
				InputText = "";
				WriteLine(ColorInput, InputPrefix + text);
				OnInputText?.Invoke(text);

			} else if (key.Key == ConsoleKey.Backspace) {

				// Remove last character from input
				if (InputText.Length > 0) {
					InputText = InputText.Substring(0, InputText.Length - 1);
					RedrawInput();
				}

			} else if (key.Key == ConsoleKey.Escape) {

				// Remove input
				InputText = "";
				RedrawInput();

			} else if (key.KeyChar != '\u0000') {
				
				// Add character to input
				InputText += key.KeyChar;
				RedrawInput();

			}

		}

		private void OnLogMessageReceived(string message, string stackTrace, LogType type) {
			if (type == LogType.Warning) {
				WriteLine(ColorWarning, message);
			} else if (type == LogType.Error) {
				WriteLine(ColorError, message);
			} else {
				WriteLine(ColorLog, message);
			}
		}

		public void WriteLine(ConsoleColor color, string message) {

			// Check if console exists
			if (!ConsoleRedirected) {
				return;
			}
			
			// Rewrite input line with spaces
			Console.CursorLeft = 0;
			Console.Write(new string(' ', Console.BufferWidth));
			
			// Move back one line up
			Console.CursorTop--;
			Console.CursorLeft = 0;

			// Write the line
			Console.ForegroundColor = color;
			Console.WriteLine(message);

			// Write back the input
			Console.ForegroundColor = ColorInput;
			Console.Write(InputText);

		}

		public void RedrawInput() {

			// Check if console exists
			if (!ConsoleRedirected) {
				return;
			}

			// Rewrite input line with spaces
			Console.CursorLeft = 0;
			Console.Write(new string(' ', Console.BufferWidth));

			// Move back one line up
			Console.CursorTop--;
			Console.CursorLeft = 0;

			// Write back the input
			Console.ForegroundColor = ColorInput;
			Console.Write(InputText);

		}

		public void Initialize() {
			try {

				// Check if console already exists
				if (ConsoleRedirected) {
					return;
				}

				// Attach to parent console or allocate a new one if needed
				bool attached = AttachConsole(ATTACH_PARENT_PROCESS);
				if (attached) {
					ConsoleAllocated = false;
				} else {
					ConsoleAllocated = true;
					AllocConsole();
				}

				// Create a new console output writer
				IntPtr stdHandle = GetStdHandle(STD_OUTPUT_HANDLE);
				SafeFileHandle stdFileHandle = new SafeFileHandle(stdHandle, true);
				FileStream stdStream = new FileStream(stdFileHandle, FileAccess.Write);
				StreamWriter stdWriter = new StreamWriter(stdStream, Encoding.UTF8);
				stdWriter.AutoFlush = true;

				// Redirect console output writer
				TextWriter oldWriter = Console.Out;
				Console.OutputEncoding = Encoding.UTF8;
				Console.SetOut(stdWriter);
				ConsoleOutput = oldWriter;
				ConsoleRedirected = true;

			} catch (Exception exception) {
				Debug.LogException(exception);
			}
		}

		public void Shutdown() {
			try {

				// Check if console already shut down
				if (!ConsoleRedirected) {
					return;
				}

				// Revert output writer back to the old one
				Console.SetOut(ConsoleOutput);

				// Free console if needed
				if (ConsoleAllocated) FreeConsole();

				// Defaults
				ConsoleAllocated = false;
				ConsoleRedirected = false;
				ConsoleOutput = null;

			} catch (Exception exception) {
				Debug.LogException(exception);
			}
		}

		public void SetTitle(string title) {
			try {
				SetConsoleTitle(title);
			} catch (Exception exception) {
				Debug.LogException(exception);
			}
		}

		private const uint ATTACH_PARENT_PROCESS = uint.MaxValue;
		private const int STD_OUTPUT_HANDLE = -11;
		private const int STD_INPUT_HANDLE = -10;

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool AttachConsole(uint dwProcessId);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool AllocConsole();

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool FreeConsole();

		[DllImport("kernel32.dll", EntryPoint = "GetStdHandle", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
		private static extern IntPtr GetStdHandle(int nStdHandle);

		[DllImport("kernel32.dll")]
		private static extern bool SetConsoleTitle(string lpConsoleTitle);

	}

}
