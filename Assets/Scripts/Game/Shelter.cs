using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastShore
{
    public class Shelter : PlacedStructure, IInteractable
    {
        public static readonly List<Shelter> All = new List<Shelter>();
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
        public static bool AnyNear(Vector3 pos, float radius)
        {
            foreach (var s in All) if ((s.transform.position - pos).sqrMagnitude < radius * radius) return true;
            return false;
        }

        public string Prompt() => Rest.CanSleep ? "Sleep until morning" : "Rest  (you can sleep after dusk)";
        public void Interact() { Rest.TrySleep(this, 1f); }
    }
}
