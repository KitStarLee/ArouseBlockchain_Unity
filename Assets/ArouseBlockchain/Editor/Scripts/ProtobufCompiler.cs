using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityGRPC.Editor
{
    internal class ProtobufCompiler : AssetPostprocessor
    {
        private static bool _hasAnyChanges;

        /// <summary>
        /// Path to the file of all protobuf files in your Unity folder.
        /// </summary>
        private static string[] AllProtoFiles
        {
            get
            {
                string[] protoFiles = Directory.GetFiles(Application.dataPath, "*.proto", SearchOption.AllDirectories);
                return protoFiles;
            }
        }

        /// <summary>
        /// A parent folder of all protobuf files found in your Unity project collected together.
        /// This means all .proto files in Unity could import each other freely even if they are far apart.
        /// </summary>
        private static string[] IncludePaths
        {
            get
            {
                string[] protoFiles = AllProtoFiles;

                string[] includePaths = new string[protoFiles.Length];
                for (int i = 0; i < protoFiles.Length; i++)
                {
                    string protoFolder = Path.GetDirectoryName(protoFiles[i]);
                    includePaths[i] = protoFolder;
                }

                return includePaths;
            }
        }

        /// <summary>
        /// Called from Force Compilation button in the prefs.
        /// </summary>
        internal static void CompileAllInProject()
        {
            if (ProtobufEditorSetting.isLoggingStandardEnabled)
            {
                UnityEngine.Debug.Log("Protobuf Unity : Compiling all .proto files in the project...");
            }

            foreach (string s in AllProtoFiles)
            {
                if (ProtobufEditorSetting.isLoggingStandardEnabled)
                {
                    UnityEngine.Debug.Log("Protobuf Unity : Compiling " + s);
                }

                CompileProtobufSystemPath(s, IncludePaths);
            }

            UnityEngine.Debug.Log(nameof(ProtobufCompiler));
            AssetDatabase.Refresh();
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            _hasAnyChanges = false;
            if (ProtobufEditorSetting.isAutoCompilationEnabled == false)
            {
                return;
            }

            foreach (string str in importedAssets)
            {
                if (CompileProtobufAssetPath(str, IncludePaths) == true)
                {
                    _hasAnyChanges = true;
                }
            }

            if (_hasAnyChanges)
            {
                UnityEngine.Debug.Log(nameof(ProtobufCompiler));
                AssetDatabase.Refresh();
            }
        }

        private static bool CompileProtobufAssetPath(string assetPath, string[] includePaths)
        {
            string protoFileSystemPath = Directory.GetParent(Application.dataPath) +
                                         Path.DirectorySeparatorChar.ToString() + assetPath;
            return CompileProtobufSystemPath(protoFileSystemPath, includePaths);
        }

        private static bool CompileProtobufSystemPath(string protoFileSystemPath, string[] includePaths)
        {
            //Do not compile changes coming from UPM package.
            if (protoFileSystemPath.Contains("Packages/com.e7.protobuf-unity")) return false;

            if (Path.GetExtension(protoFileSystemPath) == ".proto")
            {
                string arguments = CreateArgumentsString(protoFileSystemPath, includePaths);
                CreateCompilationProcess(arguments, protoFileSystemPath);
                return true;
            }

            return false;
        }

        private static void CreateCompilationProcess(string arguments, string protoFileSystemPath)
        {
            var protocPath = ProtobufEditorSetting.isProtocRelativeEnabled
                ? Application.dataPath + ProtobufEditorSetting.excPath
                : ProtobufEditorSetting.excPath;

            ProcessStartInfo startInfo = new ProcessStartInfo() { FileName = protocPath, Arguments = arguments };
            Process proc = new Process() { StartInfo = startInfo };
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.Start();

            string output = proc.StandardOutput.ReadToEnd();
            string error = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            if (ProtobufEditorSetting.isLoggingStandardEnabled)
            {
                if (output != "")
                {
                    UnityEngine.Debug.Log("Protobuf Unity : " + output);
                }

                UnityEngine.Debug.Log("Protobuf Unity : Compiled " + Path.GetFileName(protoFileSystemPath));
            }

            if (ProtobufEditorSetting.isLoggingErrorEnabled && error != "")
            {
                UnityEngine.Debug.LogError("Protoc Exe Path " + protocPath);
                UnityEngine.Debug.LogError("Protobuf Unity : " + error);
            }
        }

        private static string CreateArgumentsString(string protoFileSystemPath, string[] includePaths)
        {
            string options = "";

            options += CreateProtoOptions(includePaths);
            options += CreateGrpcOptions();

            string outputPath = Path.GetDirectoryName(protoFileSystemPath);
            string arguments = $"\"{protoFileSystemPath}\"" + string.Format(options, outputPath);
            if (ProtobufEditorSetting.isLoggingStandardEnabled)
            {
                UnityEngine.Debug.Log("Protobuf Unity : Final arguments :\n" + arguments);
            }

            return arguments;
        }

        private static string CreateProtoOptions(string[] includePaths)
        {
            string options = " --csharp_out \"{0}\" ";
            foreach (string s in includePaths)
            {
                options += $" --proto_path \"{s}\" ";
            }

            return options;
        }

        private static string CreateGrpcOptions()
        {
            if (!ProtobufEditorSetting.isGrpcCompilationEnabled) return string.Empty;

            string options = " --grpc_out \"{0}\" ";
            options += $" --plugin=protoc-gen-grpc={ProtobufEditorSetting.excGrpcPath}";
            return options;
        }
    }
}