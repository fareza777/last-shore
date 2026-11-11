using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastShore
{
    /// <summary>An item lying in the world.</summary>
    public class Pickup : WorldNode, IInteractable
    {
        public string ItemId; public int Count = 1; public bool Dynamic;

        public string Prompt()
        {
            var d = ItemDB.Get(ItemId);
            return d == null || Removed ? null : "Pick up " + d.Name + (Count > 1 ? " x" + Count : "");
        }

        public void Interact()
        {
            int left = GameManager.I.Inv.Add(ItemId, Count);
            if (left == Count) { GameManager.I.Hud.Hint("Your backpack is full."); return; }
            AudioManager.I.Play("pickup", 0.9f, 0.12f);
            Count = left;
            if (left > 0) return;
            if (Dynamic) Destroy(gameObject);
            else { SetRemoved(true); GameManager.I.MarkRemoved(this, -1); }
        }

        public bool Removed { get; private set; }
        public override void SetRemoved(bool removed)
        {
            Removed = removed;
            foreach (Transform t in transform) t.gameObject.SetActive(!removed);
            var c = GetComponent<Collider>(); if (c != null) c.enabled = !removed;
        }

        /// <summary>Builds a pickup object for an item (used at runtime for drops and by the editor world builder).</summary>
        public static Pickup Build(string itemId, int count, Vector3 pos, float yaw = 0)
        {
            var def = ItemDB.Get(itemId);
            var go = new GameObject("Pickup_" + itemId) { layer = World.InteractLayer };
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            var src = Resources.Load<GameObject>("Models/" + def.Model);
            float radius = 0.3f;
            if (src != null)
            {
                var m = Instantiate(src, go.transform); m.name = "Model";
                foreach (var t in m.GetComponentsInChildren<Transform>()) t.gameObject.layer = World.InteractLayer;
                var b = new Bounds(go.transform.position, Vector3.zero);
                foreach (var r in m.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
                radius = Mathf.Clamp(b.extents.magnitude * 0.9f, 0.3f, 0.9f);
            }
            var c = go.AddComponent<SphereCollider>(); c.isTrigger = true; c.radius = radius; c.center = new Vector3(0, radius * 0.4f, 0);
            var p = go.AddComponent<Pickup>(); p.ItemId = itemId; p.Count = count;
            return p;
        }

        public static Pickup Spawn(string itemId, int count, Vector3 pos)
        {
            Vector3 g = World.OnGround(pos);
            var p = Build(itemId, count, new Vector3(pos.x, Mathf.Max(g.y, World.SeaLevel - 0.3f), pos.z), Random.Range(0, 360f));
            p.Dynamic = true; p.Id = -1;
            p.transform.localScale = Vector3.zero;
            Tween.Scale(p.transform, Vector3.one, 0.35f, Ease.OutBack);
            return p;
        }
    }
}
