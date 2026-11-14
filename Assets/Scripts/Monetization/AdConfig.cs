namespace LastShore
{
    /// <summary>AdMob and store ids for the F7 Developer production listing.</summary>
    public static class AdConfig
    {
        // Live AdMob ids for Last Shore: Island Survival (app 5320622660).
        public const string AndroidAppId = "ca-app-pub-6279186647593327~5320622660";
        public const string BannerUnitId = "ca-app-pub-6279186647593327/4634226952";
        public const string InterstitialUnitId = "ca-app-pub-6279186647593327/9938524521";
        public const string RewardedUnitId = "ca-app-pub-6279186647593327/3761943072";

        // Create this non-consumable in Play Console, base price 4.99 USD, then upload a build to a test track.
        public const string RemoveAdsProductId = "remove_ads";
        public const string RemoveAdsFallbackPrice = "$4.99";

        public const float BannerMarginDp = 8f;
        public const int InterstitialCooldownSeconds = 120;

        public static bool UsingTestAds => AndroidAppId.StartsWith("ca-app-pub-3940256099942544");
    }
}
