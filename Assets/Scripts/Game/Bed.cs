using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastShore
{
    public class Bed : PlacedStructure, IInteractable
    {
        public string Prompt() => Rest.CanSleep ? "Sleep until morning" : "Leaf bed  (sleep after dusk)";
        public void Interact() { Rest.TrySleep(this, 1.5f); }
    }
}
