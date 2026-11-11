using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastShore
{
    public class Workbench : PlacedStructure, IInteractable
    {
        public static readonly List<Workbench> All = new List<Workbench>();
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
        public string Prompt() => "Use the workbench";
        public void Interact() { GameManager.I.InvUI.OpenCrafting(); }
    }
}
