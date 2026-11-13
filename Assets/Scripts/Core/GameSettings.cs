using System;
using UnityEngine;

namespace LastShore
{
    /// <summary>Player options, persisted in PlayerPrefs.</summary>
    public static class GameSettings
    {
        public const string GameTitle = "LAST SHORE";
        public const string Studio = "FABLE 62";
        public const string ShareText = "I'm stranded on a wild island in Last Shore. Can you survive and sail home?";

        public static float Master = 1f, Ambience = 0.8f, Sfx = 0.9f, Voice = 1f;
        public static float Sensitivity = 1f, Fov = 70f;
        public static bool InvertY, Subtitles = true, HeadBob = true, Vibration = true;
        public static int Quality = 2; // 0 low, 1 medium, 2 high
        public static bool QualityLocked;  // the player picked a tier by hand: never lower it automatically
        public static event Action Changed;
        /// <summary>Real shadow maps, HDR and post-processing. On phones only the High tier can afford them.</summary>
        public static bool FullLighting => Quality == 2 || (Quality == 1 && !Application.isMobilePlatform);

        static bool _loaded;

        public static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            Master = PlayerPrefs.GetFloat("s_master", 1f);
            Ambience = PlayerPrefs.GetFloat("s_amb", 0.8f);
            Sfx = PlayerPrefs.GetFloat("s_sfx", 0.9f);
            Voice = PlayerPrefs.GetFloat("s_voice", 1f);
            Sensitivity = PlayerPrefs.GetFloat("s_sens", 1f);
            Fov = PlayerPrefs.GetFloat("s_fov", 70f);
            InvertY = PlayerPrefs.GetInt("s_invy", 0) == 1;
            Subtitles = PlayerPrefs.GetInt("s_subs", 1) == 1;
            HeadBob = PlayerPrefs.GetInt("s_bob", 1) == 1;
            Vibration = PlayerPrefs.GetInt("s_vib", 1) == 1;
            Quality = PlayerPrefs.GetInt("s_quality4", Application.isMobilePlatform ? 1 : 2);
            QualityLocked = PlayerPrefs.GetInt("s_qlock", 0) == 1;
            Apply();
        }

        public static void Save()
        {
            PlayerPrefs.SetFloat("s_master", Master);
            PlayerPrefs.SetFloat("s_amb", Ambience);
            PlayerPrefs.SetFloat("s_sfx", Sfx);
            PlayerPrefs.SetFloat("s_voice", Voice);
            PlayerPrefs.SetFloat("s_sens", Sensitivity);
            PlayerPrefs.SetFloat("s_fov", Fov);
            PlayerPrefs.SetInt("s_invy", InvertY ? 1 : 0);
            PlayerPrefs.SetInt("s_subs", Subtitles ? 1 : 0);
            PlayerPrefs.SetInt("s_bob", HeadBob ? 1 : 0);
            PlayerPrefs.SetInt("s_vib", Vibration ? 1 : 0);
            PlayerPrefs.SetInt("s_quality4", Quality); PlayerPrefs.SetInt("s_qlock", QualityLocked ? 1 : 0);
            PlayerPrefs.Save();
            Apply();
        }

        public static void Apply()
        {
            AudioListener.volume = Master;
            Quality = Mathf.Clamp(Quality, 0, 2);
            var urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
            if (urp != null)
            {
                bool mobile = Application.isMobilePlatform;
                urp.shadowDistance = !FullLighting ? 1f : mobile ? 32f : Quality == 1 ? 60f : 120f;
                urp.shadowCascadeCount = Quality == 2 && !mobile ? 3 : 1;
                urp.renderScale = Quality == 0 ? (mobile ? 0.6f : 0.8f) : Quality == 1 ? (mobile ? 0.7f : 0.9f) : (mobile ? 0.75f : 1f);
                urp.supportsHDR = FullLighting;
                urp.supportsCameraDepthTexture = false;   // the water reads a baked shore map instead
            }
            QualitySettings.lodBias = Quality == 0 ? 0.6f : Quality == 1 ? 0.9f : 1.5f;
            Shader.SetGlobalFloat("_LS_Lite", FullLighting ? 0f : 1f);   // the grain texture costs three samples per pixel
            Application.targetFrameRate = Application.isMobilePlatform ? 60 : -1;
            QualitySettings.vSyncCount = Application.isMobilePlatform ? 0 : 1;
            Changed?.Invoke();
        }

        public static void Vibrate()
        {
            Haptics.Pulse(60);
        }
    }
}
