using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastShore
{
    /// <summary>A planted torch: permanent light, and animals give it room.</summary>
    public class TorchStand : PlacedStructure
    {
        public static readonly List<TorchStand> All = new List<TorchStand>();
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
        protected override void Start() { base.Start(); FX.Fire(transform, new Vector3(0, 1.85f, 0), 0.4f, false, 15f); }
    }
}
