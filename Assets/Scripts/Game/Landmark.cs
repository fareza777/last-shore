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
            gm.Hud.Banner(Title, "DISCOVERED  ·  " + gm.DiscoveredCount + " of " + All.Count + " places");
            if (!string.IsNullOrEmpty(Voice)) AudioManager.I.Say(Voice, Story.Line(Voice));
            gm.Quests.Notify("zone", Id);
            gm.SaveGame();
        }
    }
}
