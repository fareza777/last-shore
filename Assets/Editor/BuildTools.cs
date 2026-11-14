using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LastShore.EditorTools
{
    /// <summary>Command-line / menu builds.</summary>
    public static class BuildTools
    {
        /// <summary>Re-renders item icons and water materials, then builds.</summary>
        public static void RefreshAndBuild() { ProjectSetup.BuildExtras(); ProjectSetup.RenderIcons(); ProjectSetup.RefreshWater(); BuildAndroid(); }

        [MenuItem("Last Shore/5. Build Android APK")]
        public static void BuildAndroid()
        {
            MonetizationSetup.Apply();
            MonetizationSetup.WarnIfTestAds();
            Directory.CreateDirectory("Builds");
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;
            EditorUserBuildSettings.buildAppBundle = false;
            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Boot.unity", "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Intro.unity", "Assets/Scenes/Game.unity" },
                locationPathName = "Builds/LastShore.apk",
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            Debug.Log("[LastShore] Android build: " + s.result + " size=" + (s.totalSize / (1024 * 1024)) + "MB errors=" + s.totalErrors + " time=" + s.totalTime);
            if (Application.isBatchMode) EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
