using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastShore
{
    /// <summary>The great pyre at the Lookout. Once lit it burns for good and can be seen from the sea.</summary>
    public class SignalPyre : MonoBehaviour, IInteractable
    {
        public static readonly List<SignalPyre> All = new List<SignalPyre>();
        public bool Lit;
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
        void Start() { if (GameManager.I != null && GameManager.I.Flags.Contains("pyre_lit")) Light(false); }
        public string Prompt() => Lit ? null : "Light the signal pyre  (needs a torch or 3 sticks)";
        public void Interact()
        {
            var gm = GameManager.I;
            if (Lit) return;
            if (!gm.Interaction.HoldingTorch && !gm.Inv.Remove("stick", 3)) { gm.Hud.Hint("Bring a lit torch, or three dry sticks."); return; }
            Light(true);
        }
        void Light(bool fresh)
        {
            Lit = true;
            var fire = FX.Fire(transform, new Vector3(0, 0.6f, 0), 2.6f, true, 46f);
            var l = fire.GetComponentInChildren<Light>(); if (l != null) l.intensity = 5f;
            if (!fresh) return;
            var gm = GameManager.I; gm.Flags.Add("pyre_lit");
            AudioManager.I.PlayAt("roar_fire", transform.position, 1f); gm.Player.Shake(0.5f);
            gm.Hud.Banner("SIGNAL PYRE LIT", "It can be seen for miles");
            gm.SayOnce("pyre"); gm.SaveGame();
        }
    }
}
