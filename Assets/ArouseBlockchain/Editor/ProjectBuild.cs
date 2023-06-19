using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ArouseBlockchain.Common
{
    public class ProjectBuild
    {

        public enum BuildPlatformType {

            ios,
            apk,
            exe,
            pkg,
            server
        }

        public ProjectBuild()
        {
        }

        public static void AutomationBuild()
        {
            BuildPlatformType bpt;

            //if(!Enum.TryParse(GetCommands("Platform"), out bpt)) {

            //    Debug.Log("错误指令");
            //    return;
            //}

            //switch (bpt)
            //{
            //    case BuildPlatformType.ios:
            //        break;
            //    case BuildPlatformType.apk:
            //        break;
            //    case BuildPlatformType.pkg:
            //        break;
            //    case BuildPlatformType.exe:
            //        break;
            //    case BuildPlatformType.server:
            //        break;
            //    default:
            //        Debug.LogError("错误指令:没有目标平台 ！");
            //        return;

            //}

        }

        public static void BuildServer()
        {
            // UnityEditor.EditorUserBuildSettings.switchEnableRomCompression
            //  PlayerSettings.
           
           var buildPlayerOptions = new BuildPlayerOptions()

           {
               scenes = new[] { "Assets/ArouseBlockchain/Scenes/Server.unity" },
               locationPathName= "ArouseBCServer",
               subtarget = (int)StandaloneBuildSubtarget.Server,
              // targetGroup = BuildTargetGroup.s
               target = BuildTarget.StandaloneOSX,

               options = BuildOptions.Development

           };

            AssetDatabase.Refresh();

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;
            if(summary.result == BuildResult.Succeeded) {
                Debug.Log("发布成功 ：" + (summary.totalSize/1024/2014) +"M");
            }
            else if(summary.result == BuildResult.Failed)
            {
                Debug.LogError("发布错误 ！");
            }

        }


    }

}