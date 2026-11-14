using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace LastShore.EditorTools
{
    /// <summary>Pushes the AdMob app id from AdConfig into the Google Mobile Ads settings asset.</summary>
    public static class MonetizationSetup
    {
        [InitializeOnLoadMethod]
        static void AutoSync()
        {
            EditorApplication.delayCall += () => { try { Apply(); } catch (Exception e) { Debug.LogWarning("[LastShore] AdMob auto sync: " + e.Message); } };
        }

        [MenuItem("Last Shore/6. Configure Monetization")]
        public static void Apply()
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("GoogleMobileAds.Editor.GoogleMobileAdsSettings", false))
                .FirstOrDefault(t => t != null);
            if (type == null)
            {
                Debug.LogWarning("[LastShore] Google Mobile Ads package is not loaded; AdMob app id was not synced.");
                return;
            }

            var load = type.GetMethod("LoadInstance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var settings = load != null ? load.Invoke(null, null) : null;
            if (settings == null) return;

            var appId = type.GetProperty("GoogleMobileAdsAndroidAppId");
            var preprocessor = type.GetProperty("EnableGradleBuildPreProcessor");
            var edm = type.GetProperty("EnableExternalDependencyManager");
            bool dirty = false;

            if (appId != null && !Equals(appId.GetValue(settings), AdConfig.AndroidAppId))
            {
                appId.SetValue(settings, AdConfig.AndroidAppId);
                dirty = true;
                Debug.Log("[LastShore] AdMob app id synced: " + AdConfig.AndroidAppId);
            }
            if (preprocessor != null && preprocessor.GetValue(settings) is bool enabled && !enabled)
            {
                preprocessor.SetValue(settings, true);
                dirty = true;
            }
            if (edm != null && edm.GetValue(settings) is bool edmEnabled && !edmEnabled)
            {
                edm.SetValue(settings, true);
                dirty = true;
            }
            if (dirty)
            {
                EditorUtility.SetDirty((UnityEngine.Object)settings);
                AssetDatabase.SaveAssets();
            }
        }

        public static void WarnIfTestAds()
        {
            if (AdConfig.UsingTestAds)
                Debug.LogWarning("[LastShore] AdMob is using Google TEST app and ad unit ids. Replace them in AdConfig.cs before publishing.");
        }
    }
}
