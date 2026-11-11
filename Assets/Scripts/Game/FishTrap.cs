using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastShore
{
    /// <summary>Set in the shallows; fills slowly with fish while the player is busy elsewhere.</summary>
    public class FishTrap : PlacedStructure, IInteractable
    {
        public float Fish;
        const float Max = 3f;
        void Update() { if (GameManager.I != null) Fish = Mathf.Min(Max, Fish + Time.deltaTime / 210f); }
        public string Prompt() => Fish >= 1f ? "Empty the fish trap  (" + Mathf.FloorToInt(Fish) + ")" : "Fish trap  (nothing yet)";
        public void Interact()
        {
            int n = Mathf.FloorToInt(Fish);
            if (n < 1) { GameManager.I.Hud.Hint("The trap is empty. Give it time."); return; }
            Fish -= n; AudioManager.I.Play("splash", 0.5f);
            int left = GameManager.I.Inv.Add("fish_raw", n);
            if (left > 0) Pickup.Spawn("fish_raw", left, transform.position + Vector3.up * 0.5f);
        }
        public override PlacedData ToData() { var d = base.ToData(); d.water = Fish; return d; }
        public override void FromData(PlacedData d) { Fish = d.water; }
    }
}
