using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LastShore.EditorTools
{
    /// <summary>
    /// Command-line App Bundle build. Signing material comes from environment variables so no
    /// keystore password is ever written into the project.
    /// </summary>
    public static class BuildBundle
    {
        public static void BuildAab()
        {
            MonetizationSetup.Apply();
            MonetizationSetup.WarnIfTestAds();
            Directory.CreateDirectory("Builds");

            string storePass = Environment.GetEnvironmentVariable("LASTSHORE_KEYSTORE_PASS");
            string aliasPass = Environment.GetEnvironmentVariable("LASTSHORE_ALIAS_PASS");
            string keystore = Environment.GetEnvironmentVariable("LASTSHORE_KEYSTORE");
            string alias = Environment.GetEnvironmentVariable("LASTSHORE_ALIAS");
            if (string.IsNullOrEmpty(keystore)) keystore = "lastshore-upload.keystore";
            if (string.IsNullOrEmpty(alias)) alias = "lastshore";

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = Path.GetFullPath(keystore);
            PlayerSettings.Android.keystorePass = storePass;
            PlayerSettings.Android.keyaliasName = alias;
            PlayerSettings.Android.keyaliasPass = aliasPass;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            EditorUserBuildSettings.buildAppBundle = true;
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Boot.unity", "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Intro.unity", "Assets/Scenes/Game.unity" },
                locationPathName = "Builds/LastShore.aab",
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            Debug.Log("[LastShore] AAB build: " + s.result + " size=" + (s.totalSize / (1024 * 1024)) + "MB errors=" + s.totalErrors + " time=" + s.totalTime);

            PlayerSettings.Android.keystorePass = string.Empty;
            PlayerSettings.Android.keyaliasPass = string.Empty;
            AssetDatabase.SaveAssets();

            if (Application.isBatchMode) EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
