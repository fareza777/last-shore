using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LastShore
{
    /// <summary>Slow drifting camera for the menu backdrop.</summary>
    public class CameraDrift : MonoBehaviour
    {
        Vector3 _pos; Quaternion _rot;
        void Start() { _pos = transform.position; _rot = transform.rotation; }
        void Update()
        {
            float t = Time.unscaledTime;
            transform.position = _pos + new Vector3(Mathf.Sin(t * 0.11f) * 0.7f, Mathf.Sin(t * 0.17f) * 0.18f, Mathf.Cos(t * 0.09f) * 0.4f);
            transform.rotation = _rot * Quaternion.Euler(Mathf.Sin(t * 0.13f) * 0.8f, Mathf.Sin(t * 0.08f) * 2.2f, 0);
        }
    }
}
