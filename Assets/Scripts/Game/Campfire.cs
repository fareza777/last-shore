using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastShore
{
    public class Campfire : PlacedStructure, IInteractable
    {
        public static readonly List<Campfire> All = new List<Campfire>();
        public bool Lit = true; public float Fuel = 300f;
        const float MaxFuel = 720f;
        GameObject _fire; AudioSource _audio; bool _shown;

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public static bool AnyLitNear(Vector3 pos, float radius)
        {
            foreach (var c in All) if (c.Lit && (c.transform.position - pos).sqrMagnitude < radius * radius) return true;
            return false;
        }

        void Awake()
        {
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.clip = AudioManager.I.Clip("Amb", "fire"); _audio.loop = true; _audio.spatialBlend = 1; _audio.rolloffMode = AudioRolloffMode.Linear; _audio.minDistance = 1.5f; _audio.maxDistance = 22f;
        }

        void Update()
        {
            if (Lit)
            {
                bool rain = GameManager.I != null && GameManager.I.Weather.Raining;
                Fuel -= Time.deltaTime * (rain ? 1.6f : 1f);
                if (Fuel <= 0) { Fuel = 0; Lit = false; }
            }
            if (Lit != _shown)
            {
                _shown = Lit;
                if (Lit && _fire == null) _fire = FX.Fire(transform, new Vector3(0, 0.15f, 0));
                if (_fire != null) _fire.SetActive(Lit);
                if (Lit) { _audio.volume = GameSettings.Sfx * GameSettings.Ambience; _audio.Play(); } else _audio.Stop();
            }
        }

        public string Prompt()
        {
            if (!Lit) return "Light the fire  (uses a stick or log)";
            return Fuel < 150 ? "Add fuel  (" + Mathf.CeilToInt(Fuel / 60f) + " min left)" : "Cook at the fire";
        }

        public void Interact()
        {
            var gm = GameManager.I;
            if (!Lit || Fuel < 150)
            {
                string fuel = gm.Inv.Count("stick") > 0 ? "stick" : gm.Inv.Count("log") > 0 ? "log" : null;
                if (fuel == null) { gm.Hud.Hint("You need a stick or a log to feed the fire."); return; }
                gm.Inv.Remove(fuel, 1);
                Fuel = Mathf.Min(MaxFuel, Fuel + (fuel == "log" ? 240f : 100f));
                if (!Lit) AudioManager.I.PlayAt("ignite", transform.position);
                Lit = true;
                return;
            }
            gm.InvUI.OpenCrafting();
        }

        public override PlacedData ToData() { var d = base.ToData(); d.fuel = Fuel; d.lit = Lit; return d; }
        public override void FromData(PlacedData d) { Fuel = d.fuel; Lit = d.lit; }
    }
}
