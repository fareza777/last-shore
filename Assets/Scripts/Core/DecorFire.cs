using UnityEngine;

namespace LastShore
{
    /// <summary>A campfire that simply burns (menu backdrop).</summary>
    public class DecorFire : MonoBehaviour
    {
        public float Scale = 1f;
        void Start() { FX.Fire(transform, new Vector3(0, 0.15f, 0), Scale); }
    }
}
