using System;
using System.Collections;
using GoogleMobileAds.Api;
using UnityEngine;

namespace LastShore
{
    /// <summary>AdMob banner, interstitial and rewarded ads. The banner is lifted above the Android navigation bar.</summary>
    public class AdsManager : MonoBehaviour
    {
        public static AdsManager Instance { get; private set; }

        const string LogTag = "[Ads] ";

        BannerView _banner;
        InterstitialAd _interstitial;
        RewardedAd _rewarded;
        bool _ready;
        bool _bannerWanted;
        bool _loadingInterstitial, _loadingRewarded, _interstitialRetry, _rewardedRetry;
        bool _showingFullScreen;
        float _nextInterstitialAt;
        Action _interstitialDone;
        Action<bool> _rewardedDone;
        bool _rewardEarned;
        float _volumeBeforeAd = 1f;
        float _bannerCheckAt, _nextInsetAt;
        int _lastBannerX = int.MinValue, _lastBannerY = int.MinValue;
        int _navInset;
        int _sdk = -1;
        float _density = -1f;

        public bool RewardedReady => !Application.isMobilePlatform || _rewarded != null;

        void Awake()
        {
            if (Instance != null) { Destroy(this); return; }
            Instance = this;
            IapManager.Changed += OnStoreChanged;
#if UNITY_ANDROID && !UNITY_EDITOR
            MobileAds.Initialize(status =>
            {
                _ready = true;
                Debug.Log(LogTag + "SDK initialized");
                if (_bannerWanted) ShowBanner();
                LoadInterstitial();
                LoadRewarded();
            });
#else
            _ready = true;
#endif
        }

        void OnDestroy() { IapManager.Changed -= OnStoreChanged; }

        void OnStoreChanged()
        {
            if (!IapManager.RemoveAds) return;
            _bannerWanted = false;
            if (_banner != null) { _banner.Destroy(); _banner = null; }
            Debug.Log(LogTag + "ads removed by purchase");
        }

        // ------------------------------------------------------------------ banner
        public void ShowBanner()
        {
            _bannerWanted = true;
            if (IapManager.RemoveAds) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!_ready || _showingFullScreen) return;
            if (_banner == null) CreateBanner();
            _banner.Show();
            RepositionBanner(true);
#endif
        }

        public void HideBanner()
        {
            _bannerWanted = false;
            if (_banner != null) _banner.Hide();
        }

        void HideBannerForAd() { if (_banner != null) _banner.Hide(); }

        void RestoreBannerAfterAd()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!_bannerWanted || IapManager.RemoveAds || _banner == null) return;
            _banner.Show();
            RepositionBanner(true);
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        void CreateBanner()
        {
            _banner = new BannerView(AdConfig.BannerUnitId, AdSize.Banner, 0, 0);
            _banner.OnBannerAdLoaded += () =>
            {
                Debug.Log(LogTag + "banner loaded");
                if (!_bannerWanted || IapManager.RemoveAds || _showingFullScreen) _banner.Hide();
                else RepositionBanner(true);
            };
            _banner.OnBannerAdLoadFailed += err =>
            {
                Debug.LogWarning(LogTag + "banner failed: " + err);
                if (_banner != null) { _banner.Destroy(); _banner = null; }
                Invoke(nameof(RetryBanner), 20f);
            };
            _banner.LoadAd(new AdRequest());
            RepositionBanner(true);
        }

        void RetryBanner()
        {
            if (_bannerWanted && !IapManager.RemoveAds && !_showingFullScreen) ShowBanner();
        }

        void Update()
        {
            if (!_bannerWanted || _banner == null || IapManager.RemoveAds || _showingFullScreen) return;
            if (Time.unscaledTime < _bannerCheckAt) return;
            _bannerCheckAt = Time.unscaledTime + 0.75f;
            RepositionBanner(false);
        }

        void RepositionBanner(bool force)
        {
            if (_banner == null) return;
            if (_density <= 0) _density = DeviceDensity();
            RefreshInset();
            float bottomPx = Mathf.Max(_navInset, Screen.safeArea.yMin);
            float bannerPx = 50f * _density;
            try { float actual = _banner.GetHeightInPixels(); if (actual > 1f) bannerPx = actual; } catch (Exception) { }
            float yPx = Screen.height - bottomPx - bannerPx - AdConfig.BannerMarginDp * _density;
            float xPx = (Screen.width - 320f * _density) * 0.5f;
            int x = Mathf.Max(0, Mathf.RoundToInt(xPx / _density));
            int y = Mathf.Max(0, Mathf.RoundToInt(yPx / _density));
            if (!force && x == _lastBannerX && y == _lastBannerY) return;
            _lastBannerX = x; _lastBannerY = y;
            _banner.SetPosition(x, y);
            Debug.Log(LogTag + "banner " + x + "," + y + "dp nav=" + _navInset + "px screen=" + Screen.width + "x" + Screen.height);
        }

        void RefreshInset()
        {
            if (Time.unscaledTime < _nextInsetAt) return;
            _nextInsetAt = Time.unscaledTime + 0.75f;
            _navInset = NavigationInsetPixels();
        }

        int NavigationInsetPixels()
        {
            try
            {
                if (_sdk < 0) using (var v = new AndroidJavaClass("android.os.Build$VERSION")) _sdk = v.GetStatic<int>("SDK_INT");
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var window = activity.Call<AndroidJavaObject>("getWindow"))
                using (var decor = window.Call<AndroidJavaObject>("getDecorView"))
                using (var insets = decor.Call<AndroidJavaObject>("getRootWindowInsets"))
                {
                    int bottom = 0;
                    if (insets != null)
                    {
                        if (_sdk >= 30)
                        {
                            using (var type = new AndroidJavaClass("android.view.WindowInsets$Type"))
                            {
                                int navBars = type.CallStatic<int>("navigationBars");
                                int gestures = type.CallStatic<int>("systemGestures");
                                // Ignore visibility so the banner clears the navigation bar even while immersive hides it.
                                using (var nav = insets.Call<AndroidJavaObject>("getInsetsIgnoringVisibility", navBars)) bottom = Mathf.Max(bottom, nav.Get<int>("bottom"));
                                using (var ges = insets.Call<AndroidJavaObject>("getInsetsIgnoringVisibility", gestures)) bottom = Mathf.Max(bottom, ges.Get<int>("bottom"));
                            }
                        }
                        else if (_sdk >= 28) bottom = insets.Call<int>("getStableInsetBottom");
                        else bottom = insets.Call<int>("getSystemWindowInsetBottom");

                        using (var cutout = insets.Call<AndroidJavaObject>("getDisplayCutout"))
                            if (cutout != null) bottom = Mathf.Max(bottom, cutout.Call<int>("getSafeInsetBottom"));
                    }
                    return Mathf.Max(bottom, Mathf.RoundToInt(Screen.safeArea.yMin));
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(LogTag + "nav inset failed: " + e.Message);
                return Mathf.RoundToInt(Screen.safeArea.yMin);
            }
        }

        float DeviceDensity()
        {
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var res = activity.Call<AndroidJavaObject>("getResources"))
                using (var dm = res.Call<AndroidJavaObject>("getDisplayMetrics"))
                    return dm.Get<float>("density");
            }
            catch { return 1f; }
        }
#endif

        // ------------------------------------------------------------------ interstitial
        public void MaybeShowInterstitial(Action onDone)
        {
            if (IapManager.RemoveAds) { onDone?.Invoke(); return; }
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!_ready || _interstitial == null || Time.unscaledTime < _nextInterstitialAt)
            {
                onDone?.Invoke();
                return;
            }
            var ad = _interstitial; _interstitial = null;
            _interstitialDone = onDone;
            _showingFullScreen = true;
            HideBannerForAd();
            _volumeBeforeAd = AudioListener.volume;
            AudioListener.volume = 0f;
            bool ended = false;
            void End()
            {
                if (ended) return;
                ended = true;
                _showingFullScreen = false;
                AudioListener.volume = _volumeBeforeAd;
                _nextInterstitialAt = Time.unscaledTime + AdConfig.InterstitialCooldownSeconds;
                var cb = _interstitialDone; _interstitialDone = null;
                ad.Destroy();
                cb?.Invoke();
                RestoreBannerAfterAd();
                LoadInterstitial();
            }
            ad.OnAdFullScreenContentClosed += End;
            ad.OnAdFullScreenContentFailed += _ => End();
            try { ad.Show(); Debug.Log(LogTag + "interstitial shown"); }
            catch (Exception e) { Debug.LogWarning(LogTag + "interstitial show failed: " + e.Message); End(); }
#else
            StartCoroutine(FakeInterstitial(onDone));
#endif
        }

        public void ShowRewarded(Action<bool> onDone)
        {
            if (!Application.isMobilePlatform)
            {
                if (_rewardedDone == null) StartCoroutine(FakeReward(onDone));
                return;
            }
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_rewardedDone != null) return;
            if (_rewarded == null)
            {
                Debug.LogWarning(LogTag + "rewarded not loaded");
                onDone?.Invoke(false);
                LoadRewarded();
                return;
            }
            var ad = _rewarded; _rewarded = null;
            _rewardedDone = onDone;
            _rewardEarned = false;
            _showingFullScreen = true;
            HideBannerForAd();
            _volumeBeforeAd = AudioListener.volume;
            AudioListener.volume = 0f;
            bool ended = false;
            void End()
            {
                if (ended) return;
                ended = true;
                _showingFullScreen = false;
                AudioListener.volume = _volumeBeforeAd;
                var cb = _rewardedDone; _rewardedDone = null;
                bool earned = _rewardEarned;
                _rewardEarned = false;
                ad.Destroy();
                cb?.Invoke(earned);
                RestoreBannerAfterAd();
                LoadRewarded();
            }
            ad.OnAdFullScreenContentClosed += End;
            ad.OnAdFullScreenContentFailed += _ => End();
            try { ad.Show(_ => { _rewardEarned = true; Debug.Log(LogTag + "reward earned"); }); Debug.Log(LogTag + "rewarded shown"); }
            catch (Exception e) { Debug.LogWarning(LogTag + "rewarded show failed: " + e.Message); End(); }
#else
            if (_rewardedDone == null) StartCoroutine(FakeReward(onDone));
#endif
        }

        IEnumerator FakeInterstitial(Action onDone)
        {
            yield return new WaitForSecondsRealtime(0.35f);
            onDone?.Invoke();
        }

        IEnumerator FakeReward(Action<bool> onDone)
        {
            _rewardedDone = onDone;
            yield return new WaitForSecondsRealtime(0.5f);
            var cb = _rewardedDone; _rewardedDone = null;
            cb?.Invoke(true);
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        void LoadInterstitial()
        {
            if (!_ready || IapManager.RemoveAds || _loadingInterstitial || _interstitial != null) return;
            _loadingInterstitial = true;
            InterstitialAd.Load(AdConfig.InterstitialUnitId, new AdRequest(), (ad, err) =>
            {
                _loadingInterstitial = false;
                if (err != null || ad == null)
                {
                    Debug.LogWarning(LogTag + "interstitial load failed: " + err);
                    if (!_interstitialRetry) { _interstitialRetry = true; StartCoroutine(RetryLater(true)); }
                    return;
                }
                _interstitial = ad;
                Debug.Log(LogTag + "interstitial loaded");
            });
        }

        void LoadRewarded()
        {
            if (!_ready || _loadingRewarded || _rewarded != null) return;
            _loadingRewarded = true;
            RewardedAd.Load(AdConfig.RewardedUnitId, new AdRequest(), (ad, err) =>
            {
                _loadingRewarded = false;
                if (err != null || ad == null)
                {
                    Debug.LogWarning(LogTag + "rewarded load failed: " + err);
                    if (!_rewardedRetry) { _rewardedRetry = true; StartCoroutine(RetryLater(false)); }
                    return;
                }
                _rewarded = ad;
                Debug.Log(LogTag + "rewarded loaded");
            });
        }

        IEnumerator RetryLater(bool interstitial)
        {
            yield return new WaitForSecondsRealtime(30f);
            if (interstitial) { _interstitialRetry = false; LoadInterstitial(); }
            else { _rewardedRetry = false; LoadRewarded(); }
        }
#endif
    }
}
