using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastShore
{
    /// <summary>Fresh water: drink your fill and top up flasks.</summary>
    public class WaterSource : MonoBehaviour, IInteractable
    {
        public string Prompt() => GameManager.I.Inv.Count("flask_empty") > 0 ? "Fill your flasks with fresh water" : "Drink fresh water";
        public void Interact()
        {
            var gm = GameManager.I; int n = gm.Inv.Count("flask_empty");
            if (n > 0) { gm.Inv.Remove("flask_empty", n); gm.Inv.Add("flask_full", n); AudioManager.I.Play("splash", 0.4f); }
            else { gm.Vitals.Drink(100f); AudioManager.I.Play("drink", 0.8f); }
            gm.Quests.Notify("consume", "water");
        }
    }
}
