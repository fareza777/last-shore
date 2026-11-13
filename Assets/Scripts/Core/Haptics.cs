using UnityEngine;

namespace LastShore
{
    /// <summary>Short vibration pulses. Android gets exact millisecond pulses; other phones fall back to the system buzz.</summary>
    public static class Haptics
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject _vibrator; static bool _tried;

        static AndroidJavaObject Vibrator()
        {
            if (_tried) return _vibrator;
            _tried = true;
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            }
            catch (System.Exception e) { Debug.LogWarning("Haptics unavailable: " + e.Message); _vibrator = null; }
            return _vibrator;
        }
#endif

        public static void Pulse(int milliseconds)
        {
            if (!GameSettings.Vibration || !Application.isMobilePlatform) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            var v = Vibrator();
            if (v != null)
            {
                try { v.Call("vibrate", (long)milliseconds); return; }
                catch (System.Exception e) { Debug.LogWarning("Haptics failed: " + e.Message); _vibrator = null; }
            }
#endif
#if UNITY_ANDROID || UNITY_IOS
            if (milliseconds >= 40) Handheld.Vibrate();   // also keeps the VIBRATE permission in the manifest
#endif
        }
    }
}
