using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastShore
{
    public enum NodeType { Palm, Tree, Rock, FlintRock, Bush, Fiber, Fern, Bamboo, Banana, Herb, DeadTree, Reed }

    public static class Stations
    {
        public static bool Near(Station s, Vector3 pos)
        {
            if (s == Station.None) return true;
            if (s == Station.Campfire) return Campfire.AnyLitNear(pos, 4.5f);
            if (s == Station.Pot) { foreach (var p in CookingPot.All) if ((p.transform.position - pos).sqrMagnitude < 4.5f * 4.5f) return true; return false; }
            foreach (var w in Workbench.All) if ((w.transform.position - pos).sqrMagnitude < 4.5f * 4.5f) return true;
            return false;
        }
        public static string Name(Station s) => s == Station.Campfire ? "a lit campfire" : s == Station.Pot ? "a cooking pot" : "a workbench";
    }

}
