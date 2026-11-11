using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastShore
{
    /// <summary>Notifies the quest system when the player comes within range.</summary>
    public class ZoneTrigger : MonoBehaviour
    {
        public string Zone; public float Radius = 14f;
        float _t;
        void Update()
        {
            _t -= Time.deltaTime; if (_t > 0 || GameManager.I == null || GameManager.I.Player == null) return;
            _t = 0.5f;
            if ((GameManager.I.Player.transform.position - transform.position).sqrMagnitude < Radius * Radius) GameManager.I.Quests.Notify("zone", Zone);
        }
    }
}
