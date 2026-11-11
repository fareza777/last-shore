using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastShore
{
    /// <summary>Anything the player has built. Saved by prefab name and transform.</summary>
    public class PlacedStructure : MonoBehaviour
    {
        public string Prefab;
        protected virtual void Start() { if (GameManager.I != null && !string.IsNullOrEmpty(Prefab)) GameManager.I.Register(this); }
        protected virtual void OnDestroy() { if (GameManager.I != null) GameManager.I.Unregister(this); }
        public virtual PlacedData ToData()
        {
            var p = transform.position;
            return new PlacedData { prefab = Prefab, x = p.x, y = p.y, z = p.z, rot = transform.eulerAngles.y };
        }
        public virtual void FromData(PlacedData d) { }
    }
}
