using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityGRPC.Editor
{
    internal static class ProtobufEditorSetting
    {
        private static readonly string PrefProtocEnable = "ProtobufUnity_Enable";
        private static readonly string PrefGrpcCompilationEnabled = "ProtobufUnity_GrpcCompilationEnabled";
        private static readonly string PrefProtocExecutable = "ProtobufUnity_ProtocExecutable";
        private static readonly string PrefProtocRelative = "ProtobufRelative_Enable";
        private static readonly string PrefProtocGrpcExecutable = "ProtobufUnity_ProtocGrpcExecutable";
        private static readonly string PrefLogError = "ProtobufUnity_LogError";
        private static readonly string PrefLogStandard = "ProtobufUnity_LogStandard";

        internal static bool isAutoCompilationEnabled
        {
            get => EditorPrefs.GetBool(PrefProtocEnable, false);
            set => EditorPrefs.SetBool(PrefProtocEnable, value);
        }

        internal static bool isGrpcCompilationEnabled
        {
            get => EditorPrefs.GetBool(PrefGrpcCompilationEnabled, true);
            set => EditorPrefs.SetBool(PrefGrpcCompilationEnabled, value);
        }

        internal static bool isProtocRelativeEnabled
        {
            get => EditorPrefs.GetBool(PrefProtocRelative, true);
            set => EditorPrefs.SetBool(PrefProtocRelative, value);
        }

        internal static bool isLoggingErrorEnabled
        {
            get => EditorPrefs.GetBool(PrefLogError, true);
            set => EditorPrefs.SetBool(PrefLogError, value);
        }

        internal static bool isLoggingStandardEnabled
        {
            get => EditorPrefs.GetBool(PrefLogStandard, false);
            set => EditorPrefs.SetBool(PrefLogStandard, value);
        }

        internal static string rawExcPath
        {
            get => EditorPrefs.GetString(PrefProtocExecutable, "");
            set => EditorPrefs.SetString(PrefProtocExecutable, value);
        }

        internal static string excPath
        {
            get
            {
                string ret = EditorPrefs.GetString(PrefProtocExecutable, "");
                return ret.StartsWith("..") ? Path.Combine(Application.dataPath, ret) : ret;
            }

            set => EditorPrefs.SetString(PrefProtocExecutable, value);
        }

        internal static string rawExcGrpcPath
        {
            get => EditorPrefs.GetString(PrefProtocGrpcExecutable, "");
            set => EditorPrefs.SetString(PrefProtocGrpcExecutable, value);
        }

        internal static string excGrpcPath
        {
            get
            {
                string ret = EditorPrefs.GetString(PrefProtocGrpcExecutable, "");
                return ret.StartsWith("..") ? Path.Combine(Application.dataPath, ret) : ret;
            }
            set => EditorPrefs.SetString(PrefProtocGrpcExecutable, value);
        }


        public static void ShowProtobufPreferencesWindow()
        {
            DrawTitle();

            DrawAutoCompile();
            DrawSpace(2);

            DrawGrpcCompile();
            DrawSpace(2);

            DrawProtocPath();
            DrawSpace(2);

            DrawLogTrigger();
            DrawSpace(4);

            DrawCompileButton();
            DrawSpace(3);

            DrawCopyRight();
        }

        private static void DrawTitle()
        {
            var style = new GUIStyle
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                normal =
                {
                    textColor = new Color(0.83f, 1f, 0.86f)
                }
            };
            EditorGUILayout.LabelField("<b>--- Unity GRPC Protobuf Auto Compiler ---</b>", style);
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            HorizontalLine();
        }

        private static void DrawAutoCompile()
        {
            EditorGUI.BeginChangeCheck();
            isAutoCompilationEnabled = EditorGUILayout.Toggle(new GUIContent("Auto Compilation Protobuf", ""),
                isAutoCompilationEnabled);
        }

        private static void DrawGrpcCompile()
        {
            isGrpcCompilationEnabled =
                EditorGUILayout.Toggle(new GUIContent("Grpc Protoc Compilation", ""), isGrpcCompilationEnabled);

            EditorGUI.BeginDisabledGroup(!isGrpcCompilationEnabled);

            EditorGUILayout.LabelField("Path to Grpc plugin:");
            rawExcGrpcPath = EditorGUILayout.TextField(rawExcGrpcPath, GUILayout.ExpandWidth(true));
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();
            HorizontalLine();
        }

        private static void DrawProtocPath()
        {
            EditorGUILayout.LabelField("Path to protoc.exe:");
            isProtocRelativeEnabled =
                EditorGUILayout.Toggle(new GUIContent("Is Relative Path", ""), isProtocRelativeEnabled);
            rawExcPath = EditorGUILayout.TextField(rawExcPath, GUILayout.ExpandWidth(true));
            EditorGUILayout.Space();
            HorizontalLine();
        }

        private static void DrawCompileButton()
        {
            if (GUILayout.Button(new GUIContent("Compile .proto Files !")))
            {
                ProtobufCompiler.CompileAllInProject();
            }
        }

        private static void DrawLogTrigger()
        {
            GUILayout.BeginHorizontal();

            isLoggingErrorEnabled =
                EditorGUILayout.Toggle(new GUIContent("Show Error Log", "Log compilation errors from protoc command."),
                    isLoggingErrorEnabled);

            isLoggingStandardEnabled =
                EditorGUILayout.Toggle(new GUIContent("Show Log", "Log compilation completion messages."),
                    isLoggingStandardEnabled);

            GUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        private static void DrawCopyRight()
        {
            var style = new GUIStyle
            {
                fontSize = 8,
                alignment = TextAnchor.LowerRight,
                richText = true,
                normal =
                {
                    textColor = new Color(0.83f, 1f, 0.86f)
                }
            };
            EditorGUILayout.LabelField("Yayapipi Studio", style);
        }

        private static void DrawSpace(int count)
        {
            for (int i = 0; i < count; i++)
            {
                EditorGUILayout.Space();
            }
        }

        private static void HorizontalLine()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            rect.height = 1;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }
    }
}