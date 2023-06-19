using UnityEditor;

namespace UnityGRPC.Editor
{
    public class ProtobufPanel : EditorWindow
    {
        [MenuItem("Window/UnityGrpc/Protobuf")]
        public static void ShowWindow()
        {
            GetWindow(typeof(ProtobufPanel));
        }

        void OnGUI()
        {
            ProtobufEditorSetting.ShowProtobufPreferencesWindow();
        }
    }
}