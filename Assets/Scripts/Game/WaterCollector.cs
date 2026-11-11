using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastShore
{
    public class WaterCollector : PlacedStructure, IInteractable
    {
        public float Water = 1f;
        const float Max = 3f;

        void Update()
        {
            if (GameManager.I == null) return;
            Water = Mathf.Min(Max, Water + Time.deltaTime * (GameManager.I.Weather.Raining ? 1f / 35f : 1f / 480f));
        }

        public string Prompt()
        {
            int n = Mathf.FloorToInt(Water);
            if (n < 1) return "Rain collector  (empty)";
            return (GameManager.I.Inv.Count("flask_empty") > 0 ? "Fill flask" : "Drink") + "  (" + n + "/" + (int)Max + ")";
        }

        public void Interact()
        {
            var gm = GameManager.I;
            if (Water < 1f) { gm.Hud.Hint("The bowl is empty. It fills when it rains."); return; }
            Water -= 1f;
            if (gm.Inv.Count("flask_empty") > 0) { gm.Inv.Remove("flask_empty", 1); gm.Inv.Add("flask_full", 1); AudioManager.I.Play("splash", 0.4f); }
            else { gm.Vitals.Drink(38f); AudioManager.I.Play("drink", 0.8f); gm.Quests.Notify("consume", "water"); }
        }

        public override PlacedData ToData() { var d = base.ToData(); d.water = Water; return d; }
        public override void FromData(PlacedData d) { Water = d.water; }
    }
}
