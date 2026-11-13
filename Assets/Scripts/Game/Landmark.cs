using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastShore
{
    /// <summary>A named place. Walking into it for the first time records the discovery and reveals it on the map.</summary>
    public class Landmark : MonoBehaviour
    {
        public static readonly List<Landmark> All = new List<Landmark>();
        public string Id, Title, Subtitle, Voice; public float Radius = 22f;
        float _t;
        static readonly (string id, int n)[] Rewards =
        {
            ("bandage", 2), ("rope", 2), ("arrow", 6), ("torch", 1), ("fish_cooked", 2), ("antidote", 1), ("flint", 3), ("coconut", 3),
        };
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
        public bool Found => GameManager.I != null && GameManager.I.Flags.Contains("found_" + Id);
        void Update()
        {
            _t -= Time.deltaTime; if (_t > 0) return; _t = 0.5f;
            var gm = GameManager.I; if (gm == null || gm.Player == null || gm.Cinematic || Found) return;
            if ((gm.Player.transform.position - transform.position).sqrMagnitude > Radius * Radius) return;
            gm.Flags.Add("found_" + Id);
            AudioManager.I.Play("discover", 0.8f, 0);
            gm.Hud.Banner(Title, "DISCOVERED  ·  " + gm.DiscoveredCount + " of " + All.Count + " places  ·  supplies found");
            int pick = 0; foreach (char ch in Id) pick += ch;
            var reward = Rewards[pick % Rewards.Length];
            int left = gm.Inv.Add(reward.id, reward.n);
            if (left > 0) Pickup.Spawn(reward.id, left, gm.Player.transform.position + gm.Player.transform.forward * 1.2f + Vector3.up * 0.4f);
            if (!string.IsNullOrEmpty(Voice)) AudioManager.I.Say(Voice, Story.Line(Voice));
            gm.Quests.Notify("zone", Id);
            gm.SaveGame();
        }
    }
}
