using UnityEngine;

namespace LastShore
{
    /// <summary>Creates the persistent ad and billing components once per session.</summary>
    public static class Monetization
    {
        public static AdsManager Ads { get; private set; }
        public static IapManager Iap { get; private set; }

        public static void Init()
        {
            if (Ads != null) return;
            var go = new GameObject("[Monetization]");
            Object.DontDestroyOnLoad(go);
            Iap = go.AddComponent<IapManager>();
            Ads = go.AddComponent<AdsManager>();
        }
    }
}
