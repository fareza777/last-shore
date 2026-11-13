using UnityEngine;

namespace LastShore
{
    /// <summary>
    /// Camp progression: building the right things raises the camp tier, and each tier grants a lasting perk.
    /// Tiers are remembered as flags, so they survive saving and are never lost again.
    /// </summary>
    public static class Camp
    {
        public static readonly string[] Names = { "No camp", "Foothold", "Homestead", "Stronghold" };
        static readonly string[] Perks =
        {
            "", "Stamina returns 20% faster.", "Hunger and thirst build 15% slower.", "Tools wear out 30% slower.",
        };

        public static int Tier
        {
            get
            {
                var gm = GameManager.I; if (gm == null) return 0;
                return gm.Flags.Contains("camp3") ? 3 : gm.Flags.Contains("camp2") ? 2 : gm.Flags.Contains("camp1") ? 1 : 0;
            }
        }

        public static float StaminaMul => Tier >= 1 ? 1.2f : 1f;
        public static float NeedsMul => Tier >= 2 ? 0.85f : 1f;
        public static bool SaveWear => Tier >= 3 && Random.value < 0.3f;

        static string Need(string prefab, int n, string label)
        {
            int have = Mathf.Min(GameManager.I.CountPlaced(prefab), n);
            return have >= n ? null : label + (n > 1 ? " " + have + "/" + n : "");
        }

        static string Missing(int tier)
        {
            string[] parts = tier == 1 ? new[] { Need("Campfire", 1, "Campfire"), Need("Shelter", 1, "Shelter") }
                : tier == 2 ? new[] { Need("Workbench", 1, "Workbench"), Need("WaterCollector", 1, "Rain Collector"), Need("StorageChest", 1, "Storage Chest") }
                : new[] { Need("CookingPot", 1, "Cooking Pot"), Need("Fence", 4, "Stake Fence"), Need("TorchStand", 2, "Standing Torch") };
            string s = "";
            foreach (var p in parts) if (p != null) s += (s.Length > 0 ? ", " : "") + p;
            return s;
        }

        /// <summary>One line for the journal: current tier and what the next one still needs.</summary>
        public static string Summary
        {
            get
            {
                int t = Tier;
                if (t >= 3) return "CAMP  ·  Stronghold. Nothing left to build.";
                return "CAMP  ·  " + Names[t] + "    Next, " + Names[t + 1] + ": " + Missing(t + 1);
            }
        }

        /// <summary>Called whenever something is built.</summary>
        public static void Check()
        {
            var gm = GameManager.I; if (gm == null) return;
            for (int t = Tier + 1; t <= 3; t++)
            {
                if (Missing(t).Length > 0) break;
                gm.Flags.Add("camp" + t);
                AudioManager.I.Play("discover", 0.8f, 0);
                gm.Hud.Banner("CAMP: " + Names[t].ToUpperInvariant(), Perks[t]);
            }
        }
    }
}
