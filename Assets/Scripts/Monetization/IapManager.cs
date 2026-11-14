using System;
using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;

namespace LastShore
{
    /// <summary>Google Play Billing for the non-consumable Remove Ads product.</summary>
    public class IapManager : MonoBehaviour
    {
        public static IapManager Instance { get; private set; }
        public static bool RemoveAds { get; private set; }
        public static bool StoreReady { get; private set; }
        public static bool Busy { get; private set; }
        public static string PriceText { get; private set; } = AdConfig.RemoveAdsFallbackPrice;
        public static string Message { get; private set; }
        public static event Action Changed;

        static float _busyUntil;

        const string PrefKey = "iap_remove_ads";
        const string AckKey = "iap_remove_ads_ack";
        const string LogTag = "[IAP] ";

        void Awake()
        {
            if (Instance != null) { Destroy(this); return; }
            Instance = this;
            RemoveAds = PlayerPrefs.GetInt(PrefKey, 0) == 1;
            if (RemoveAds) { PriceText = "OWNED"; Debug.Log(LogTag + "remove ads already owned"); }
            InitStore();
        }

        void Update()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            while (_main.TryDequeue(out var action))
            {
                try { action(); }
                catch (Exception e) { Debug.LogException(e); }
            }
            if (Busy && _busyUntil > 0 && Time.unscaledTime > _busyUntil)
            {
                _busyUntil = 0;
                Busy = false;
                Message = "Purchase flow timed out. Please try again.";
                Raise();
            }
#endif
        }

        static void MarkBusy(float seconds)
        {
            Busy = true;
            _busyUntil = Time.unscaledTime + seconds;
            Raise();
        }

        public static void Purchase()
        {
            if (Instance == null || RemoveAds || Busy) return;
            Message = null;
            MarkBusy(180f);
            Instance.PurchaseInternal();
        }

        public static void Restore()
        {
            if (Instance == null || Busy) return;
            Message = null;
            MarkBusy(60f);
            Instance.RestoreInternal();
        }

        static void Raise() { try { Changed?.Invoke(); } catch (Exception e) { Debug.LogException(e); } }

        static void Grant()
        {
            bool first = !RemoveAds;
            RemoveAds = true;
            PriceText = "OWNED";
            PlayerPrefs.SetInt(PrefKey, 1);
            PlayerPrefs.Save();
            Busy = false;
            _busyUntil = 0;
            Message = "Ads removed. Thank you.";
            Raise();
            if (first) Debug.Log(LogTag + "remove ads granted");
        }

        static void Fail(string message)
        {
            Busy = false;
            _busyUntil = 0;
            Message = message;
            Raise();
            Debug.LogWarning(LogTag + message);
        }

        static string Describe(int code)
        {
            switch (code)
            {
                case 0: return null;
                case 1: return "Purchase cancelled.";
                case 2: return "Google Play is not reachable. Try again.";
                case 3: return "Google Play billing is unavailable on this device.";
                case 4: return "Remove Ads is not available in Google Play yet.";
                case 5: return "Store configuration error. Please try again later.";
                case 6: return "Purchase failed. Please try again.";
                case 7: return "Purchase already owned.";
                case 8: return "This purchase is no longer owned.";
                case 12: return "Network error. Check your connection.";
                case -1: return "Google Play disconnected. Try again in a moment.";
                case -2: return "Purchases are not supported on this device.";
                case -3: return "Google Play timed out. Try again.";
                default: return "Purchase failed (code " + code + ").";
            }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        readonly ConcurrentQueue<Action> _main = new ConcurrentQueue<Action>();
        AndroidJavaObject _client, _activity, _product;
        string _offerToken;
        bool _connected, _connecting;
        int _connectAttempts;
        AndroidJavaProxy _updateListener, _stateListener, _productListener, _ackListener, _restoreListener;

        void InitStore()
        {
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    _activity = player.GetStatic<AndroidJavaObject>("currentActivity");

                _updateListener = new UpdateListener(this);
                using (var billing = new AndroidJavaClass("com.android.billingclient.api.BillingClient"))
                using (var builder = billing.CallStatic<AndroidJavaObject>("newBuilder", _activity))
                {
                    builder.Call<AndroidJavaObject>("setListener", _updateListener);
                    using (var pendingClass = new AndroidJavaClass("com.android.billingclient.api.PendingPurchasesParams"))
                    using (var pendingBuilder = pendingClass.CallStatic<AndroidJavaObject>("newBuilder"))
                    {
                        pendingBuilder.Call<AndroidJavaObject>("enableOneTimeProducts");
                        using (var pending = pendingBuilder.Call<AndroidJavaObject>("build"))
                            builder.Call<AndroidJavaObject>("enablePendingPurchases", pending);
                    }
                    _client = builder.Call<AndroidJavaObject>("build");
                }
                Connect();
            }
            catch (Exception e)
            {
                StoreReady = true;
                Debug.LogError(LogTag + "billing init failed: " + e);
            }
        }

        void Connect()
        {
            if (_client == null || _connecting || _connected) return;
            _connecting = true;
            _stateListener = new StateListener(this);
            try { _client.Call("startConnection", _stateListener); }
            catch (Exception e)
            {
                _connecting = false;
                Debug.LogWarning(LogTag + "connect failed: " + e.Message);
                RetryConnect();
            }
        }

        void RetryConnect()
        {
            if (_connected || _connectAttempts >= 5) return;
            _connectAttempts++;
            StartCoroutine(ConnectLater());
        }

        IEnumerator ConnectLater()
        {
            yield return new WaitForSecondsRealtime(6f);
            Connect();
        }

        internal void OnSetupFinished(int code, string debug)
        {
            _main.Enqueue(() =>
            {
                _connecting = false;
                if (code == 0)
                {
                    _connected = true;
                    _connectAttempts = 0;
                    StoreReady = true;
                    Debug.Log(LogTag + "billing connected");
                    string pendingAck = PlayerPrefs.GetString(AckKey, "");
                    if (!string.IsNullOrEmpty(pendingAck)) Acknowledge(pendingAck);
                    QueryProduct();
                    QueryPurchases(true);
                }
                else
                {
                    StoreReady = true;
                    Message = Describe(code) ?? ("Billing unavailable (" + code + ").");
                    Raise();
                    Debug.LogWarning(LogTag + "setup failed " + code + " " + debug);
                    RetryConnect();
                }
            });
        }

        internal void OnDisconnected()
        {
            _main.Enqueue(() =>
            {
                _connected = false;
                _connecting = false;
                Debug.LogWarning(LogTag + "billing disconnected");
                if (!RemoveAds) RetryConnect();
            });
        }

        void QueryProduct()
        {
            if (_client == null) return;
            try
            {
                using (var queryClass = new AndroidJavaClass("com.android.billingclient.api.QueryProductDetailsParams"))
                using (var queryBuilder = queryClass.CallStatic<AndroidJavaObject>("newBuilder"))
                using (var productClass = new AndroidJavaClass("com.android.billingclient.api.QueryProductDetailsParams$Product"))
                using (var productBuilder = productClass.CallStatic<AndroidJavaObject>("newBuilder"))
                {
                    productBuilder.Call<AndroidJavaObject>("setProductId", AdConfig.RemoveAdsProductId);
                    productBuilder.Call<AndroidJavaObject>("setProductType", "inapp");
                    using (var product = productBuilder.Call<AndroidJavaObject>("build"))
                    using (var list = new AndroidJavaObject("java.util.ArrayList"))
                    {
                        list.Call<bool>("add", product);
                        queryBuilder.Call<AndroidJavaObject>("setProductList", list);
                        using (var query = queryBuilder.Call<AndroidJavaObject>("build"))
                        {
                            _productListener = new ProductListener(this);
                            _client.Call("queryProductDetailsAsync", query, _productListener);
                        }
                    }
                }
            }
            catch (Exception e) { Debug.LogWarning(LogTag + "product query failed: " + e.Message); }
        }

        internal void OnProducts(int code, AndroidJavaObject result)
        {
            AndroidJavaObject product = null;
            string price = null, offerToken = null;
            if (code == 0 && result != null)
            {
                try
                {
                    using (var list = result.Call<AndroidJavaObject>("getProductDetailsList"))
                    {
                        int n = list != null ? list.Call<int>("size") : 0;
                        for (int i = 0; i < n; i++)
                        {
                            var p = list.Call<AndroidJavaObject>("get", i);
                            if (p.Call<string>("getProductId") != AdConfig.RemoveAdsProductId) { p.Dispose(); continue; }
                            product = p;
                            using (var offers = p.Call<AndroidJavaObject>("getOneTimePurchaseOfferDetailsList"))
                            {
                                if (offers != null && offers.Call<int>("size") > 0)
                                {
                                    using (var offer = offers.Call<AndroidJavaObject>("get", 0))
                                    {
                                        price = offer.Call<string>("getFormattedPrice");
                                        offerToken = offer.Call<string>("getOfferToken");
                                    }
                                }
                            }
                            if (price == null)
                            {
                                using (var offer = p.Call<AndroidJavaObject>("getOneTimePurchaseOfferDetails"))
                                    if (offer != null) price = offer.Call<string>("getFormattedPrice");
                            }
                            break;
                        }
                    }
                }
                catch (Exception e) { Debug.LogWarning(LogTag + "product parse failed: " + e.Message); }
            }

            _main.Enqueue(() =>
            {
                StoreReady = true;
                _product = product;
                _offerToken = offerToken;
                if (!RemoveAds && !string.IsNullOrEmpty(price)) PriceText = price;
                if (product == null && code == 0) Message = "Remove Ads is not set up in Google Play yet.";
                Debug.Log(LogTag + (product != null ? "product ready " + price : "product missing") + " code=" + code);
                Raise();
            });
        }

        internal void OnPurchaseUpdate(int code, string debug, AndroidJavaObject purchases)
        {
            bool owned = false, acknowledged = false;
            string token = null;
            try
            {
                int n = purchases != null ? purchases.Call<int>("size") : 0;
                for (int i = 0; i < n; i++)
                {
                    using (var p = purchases.Call<AndroidJavaObject>("get", i))
                    using (var products = p.Call<AndroidJavaObject>("getProducts"))
                    {
                        int m = products != null ? products.Call<int>("size") : 0;
                        for (int j = 0; j < m; j++)
                            if (products.Call<string>("get", j) == AdConfig.RemoveAdsProductId)
                            {
                                owned = p.Call<int>("getPurchaseState") == 1;
                                acknowledged = p.Call<bool>("isAcknowledged");
                                token = p.Call<string>("getPurchaseToken");
                            }
                    }
                }
            }
            catch (Exception e) { Debug.LogWarning(LogTag + "purchase parse failed: " + e.Message); }

            _main.Enqueue(() =>
            {
                Debug.Log(LogTag + "purchase update code=" + code + " owned=" + owned + " " + debug);
                if (code == 0 && owned)
                {
                    Grant();
                    if (!acknowledged && !string.IsNullOrEmpty(token)) Acknowledge(token);
                }
                else if (code == 0)
                {
                    Fail("Purchase is pending. It will unlock when approved.");
                }
                else if (code == 7)
                {
                    QueryPurchases(false);
                }
                else Fail(Describe(code) ?? "Purchase failed.");
            });
        }

        internal void OnRestore(int code, AndroidJavaObject purchases, bool initial)
        {
            bool owned = false, acknowledged = false;
            string token = null;
            try
            {
                int n = purchases != null ? purchases.Call<int>("size") : 0;
                for (int i = 0; i < n; i++)
                {
                    using (var p = purchases.Call<AndroidJavaObject>("get", i))
                    using (var products = p.Call<AndroidJavaObject>("getProducts"))
                    {
                        int m = products != null ? products.Call<int>("size") : 0;
                        for (int j = 0; j < m; j++)
                            if (products.Call<string>("get", j) == AdConfig.RemoveAdsProductId)
                            {
                                owned = p.Call<int>("getPurchaseState") == 1;
                                acknowledged = p.Call<bool>("isAcknowledged");
                                token = p.Call<string>("getPurchaseToken");
                            }
                    }
                }
            }
            catch (Exception e) { Debug.LogWarning(LogTag + "restore parse failed: " + e.Message); }

            _main.Enqueue(() =>
            {
                if (owned)
                {
                    Grant();
                    if (!acknowledged && !string.IsNullOrEmpty(token)) Acknowledge(token);
                }
                else if (code != 0) { if (!initial) Fail(Describe(code) ?? "Could not check purchases."); }
                else if (!initial) Fail("No previous purchase found.");
                else { Busy = false; }
                Debug.Log(LogTag + "restore code=" + code + " owned=" + owned);
            });
        }

        internal void OnAck(int code)
        {
            _main.Enqueue(() =>
            {
                if (code == 0)
                {
                    PlayerPrefs.DeleteKey(AckKey);
                    PlayerPrefs.Save();
                    Debug.Log(LogTag + "purchase acknowledged");
                }
                else Debug.LogWarning(LogTag + "acknowledge failed code=" + code);
            });
        }

        void Acknowledge(string token)
        {
            if (_client == null || string.IsNullOrEmpty(token)) return;
            PlayerPrefs.SetString(AckKey, token);
            PlayerPrefs.Save();
            try
            {
                using (var cls = new AndroidJavaClass("com.android.billingclient.api.AcknowledgePurchaseParams"))
                using (var builder = cls.CallStatic<AndroidJavaObject>("newBuilder"))
                {
                    builder.Call<AndroidJavaObject>("setPurchaseToken", token);
                    using (var param = builder.Call<AndroidJavaObject>("build"))
                    {
                        _ackListener = new AckListener(this);
                        _client.Call("acknowledgePurchase", param, _ackListener);
                    }
                }
            }
            catch (Exception e) { Debug.LogWarning(LogTag + "acknowledge failed: " + e.Message); }
        }

        void QueryPurchases(bool initial)
        {
            if (_client == null || !_connected) return;
            try
            {
                using (var cls = new AndroidJavaClass("com.android.billingclient.api.QueryPurchasesParams"))
                using (var builder = cls.CallStatic<AndroidJavaObject>("newBuilder"))
                {
                    builder.Call<AndroidJavaObject>("setProductType", "inapp");
                    using (var param = builder.Call<AndroidJavaObject>("build"))
                    {
                        _restoreListener = new RestoreListener(this, initial);
                        _client.Call("queryPurchasesAsync", param, _restoreListener);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(LogTag + "purchase query failed: " + e.Message);
                if (!initial) Fail("Could not check purchases.");
            }
        }

        void PurchaseInternal()
        {
            if (!_connected || _client == null)
            {
                Fail("Google Play is not ready yet. Try again in a moment.");
                return;
            }
            if (_product == null)
            {
                QueryProduct();
                Fail("Remove Ads is not available in Google Play yet.");
                return;
            }
            try
            {
                using (var paramsClass = new AndroidJavaClass("com.android.billingclient.api.BillingFlowParams$ProductDetailsParams"))
                using (var paramsBuilder = paramsClass.CallStatic<AndroidJavaObject>("newBuilder"))
                {
                    paramsBuilder.Call<AndroidJavaObject>("setProductDetails", _product);
                    if (!string.IsNullOrEmpty(_offerToken)) paramsBuilder.Call<AndroidJavaObject>("setOfferToken", _offerToken);
                    using (var details = paramsBuilder.Call<AndroidJavaObject>("build"))
                    using (var list = new AndroidJavaObject("java.util.ArrayList"))
                    {
                        list.Call<bool>("add", details);
                        using (var flowClass = new AndroidJavaClass("com.android.billingclient.api.BillingFlowParams"))
                        using (var flowBuilder = flowClass.CallStatic<AndroidJavaObject>("newBuilder"))
                        {
                            flowBuilder.Call<AndroidJavaObject>("setProductDetailsParamsList", list);
                            using (var flow = flowBuilder.Call<AndroidJavaObject>("build"))
                            {
                                int code = _client.Call<int>("launchBillingFlow", _activity, flow);
                                if (code != 0)
                                {
                                    if (code == 7) QueryPurchases(false);
                                    else Fail(Describe(code) ?? "Could not open Google Play.");
                                }
                                else
                                {
                                    Message = "Opening Google Play...";
                                    Raise();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e) { Fail("Could not open Google Play: " + e.Message); }
        }

        void RestoreInternal()
        {
            if (!_connected) { Fail("Google Play is not ready yet. Try again in a moment."); return; }
            QueryPurchases(false);
        }

        // ---------------------------------------------------------------- Java proxies
        class UpdateListener : AndroidJavaProxy
        {
            readonly IapManager _owner;
            public UpdateListener(IapManager owner) : base("com.android.billingclient.api.PurchasesUpdatedListener") { _owner = owner; }
            public void onPurchasesUpdated(AndroidJavaObject result, AndroidJavaObject purchases)
            {
                try { _owner.OnPurchaseUpdate(result.Call<int>("getResponseCode"), result.Call<string>("getDebugMessage"), purchases); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        class StateListener : AndroidJavaProxy
        {
            readonly IapManager _owner;
            public StateListener(IapManager owner) : base("com.android.billingclient.api.BillingClientStateListener") { _owner = owner; }
            public void onBillingSetupFinished(AndroidJavaObject result)
            {
                try { _owner.OnSetupFinished(result.Call<int>("getResponseCode"), result.Call<string>("getDebugMessage")); }
                catch (Exception e) { Debug.LogException(e); }
            }
            public void onBillingServiceDisconnected() { _owner.OnDisconnected(); }
        }

        class ProductListener : AndroidJavaProxy
        {
            readonly IapManager _owner;
            public ProductListener(IapManager owner) : base("com.android.billingclient.api.ProductDetailsResponseListener") { _owner = owner; }
            public void onProductDetailsResponse(AndroidJavaObject result, AndroidJavaObject products)
            {
                try { _owner.OnProducts(result.Call<int>("getResponseCode"), products); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        class RestoreListener : AndroidJavaProxy
        {
            readonly IapManager _owner;
            readonly bool _initial;
            public RestoreListener(IapManager owner, bool initial) : base("com.android.billingclient.api.PurchasesResponseListener") { _owner = owner; _initial = initial; }
            public void onQueryPurchasesResponse(AndroidJavaObject result, AndroidJavaObject purchases)
            {
                try { _owner.OnRestore(result.Call<int>("getResponseCode"), purchases, _initial); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        class AckListener : AndroidJavaProxy
        {
            readonly IapManager _owner;
            public AckListener(IapManager owner) : base("com.android.billingclient.api.AcknowledgePurchaseResponseListener") { _owner = owner; }
            public void onAcknowledgePurchaseResponse(AndroidJavaObject result)
            {
                try { _owner.OnAck(result.Call<int>("getResponseCode")); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }
#else
        void InitStore()
        {
            StoreReady = true;
            Debug.Log(LogTag + "editor store: Remove Ads is simulated");
        }

        void PurchaseInternal()
        {
            StartCoroutine(FakePurchase());
        }

        void RestoreInternal()
        {
            Busy = false;
            Message = RemoveAds ? "Ads are already removed." : "No previous purchase found in this build.";
            Raise();
        }

        IEnumerator FakePurchase()
        {
            yield return new WaitForSecondsRealtime(0.6f);
            Grant();
        }
#endif
    }
}
