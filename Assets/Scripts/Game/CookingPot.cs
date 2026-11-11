using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastShore
{
    public class CookingPot : PlacedStructure, IInteractable
    {
        public static readonly List<CookingPot> All = new List<CookingPot>();
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
        protected override void Start() { base.Start(); FX.Fire(transform, new Vector3(0, 0.08f, 0), 0.45f, true, 7f); }
        public string Prompt() => "Cook in the pot";
        public void Interact() { AudioManager.I.PlayAt("bubble", transform.position, 0.7f); GameManager.I.InvUI.OpenCrafting(); }
    }
}
