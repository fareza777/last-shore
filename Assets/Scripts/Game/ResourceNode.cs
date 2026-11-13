using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastShore
{
    /// <summary>Harvestable world resource: trees, bamboo and rocks need tools; bushes and plants are picked by hand.</summary>
    public class ResourceNode : WorldNode, IInteractable, IHittable
    {
        public NodeType Type;
        public GameObject Visual;     // hidden when harvested
        public GameObject Depleted;   // shown when harvested (stump), optional
        public bool KeepCollider;
        int _hp = -1; bool _removed, _falling;
        /// <summary>True once a tree or rock has been harvested away (bushes keep their body).</summary>
        public bool Removed => _removed && !KeepCollider;

        bool ByHand => Type == NodeType.Bush || Type == NodeType.Fiber || Type == NodeType.Fern || Type == NodeType.Banana || Type == NodeType.Herb || Type == NodeType.Reed;
        bool Wood => Type == NodeType.Palm || Type == NodeType.Tree || Type == NodeType.Bamboo || Type == NodeType.DeadTree;
        int MaxHp => Type == NodeType.Tree || Type == NodeType.FlintRock ? 5 : Type == NodeType.Bamboo || Type == NodeType.DeadTree ? 3 : 4;
        float RespawnDays => Type == NodeType.Bush || Type == NodeType.Banana ? 1.5f : ByHand ? 2f : Wood ? 4f : 5f;

        public string Prompt()
        {
            if (_removed || !ByHand) return null;
            switch (Type)
            {
                case NodeType.Bush: return "Pick berries";
                case NodeType.Fiber: return "Gather plant fiber";
                case NodeType.Banana: return "Pick bananas";
                case NodeType.Herb: return "Gather healing herbs";
                case NodeType.Reed: return "Cut reeds";
                default: return "Cut palm leaves";
            }
        }

        public void Interact()
        {
            if (_removed || !ByHand) return;
            switch (Type)
            {
                case NodeType.Bush: Give(("berries", 3)); break;
                case NodeType.Fiber: Give(("fiber", 2)); break;
                case NodeType.Banana: Give(("banana", 2)); break;
                case NodeType.Herb: Give(("herb", 1)); break;
                case NodeType.Reed: Give(("fiber", 3)); break;
                default: Give(("leaf", 2)); break;
            }
            AudioManager.I.Play("pickup", 0.8f);
            FX.Burst(transform.position + Vector3.up * 0.5f, new Color(0.35f, 0.65f, 0.3f), 8);
            Remove();
        }

        public void Hit(ItemDef tool, float damage, Vector3 point)
        {
            if (_removed || _falling) return;
            if (ByHand) { Interact(); return; }
            ToolType needs = Wood ? ToolType.Axe : ToolType.Pickaxe;
            if (tool == null || tool.Tool != needs)
            {
                GameManager.I.Hud.Hint(Wood ? "You need an axe to cut this down." : "You need a pickaxe to break rock.");
                AudioManager.I.PlayAt("hit", point, 0.4f);
                return;
            }
            if (_hp < 0) _hp = MaxHp;
            _hp--;
            AudioManager.I.PlayAt(Wood ? "chop" : "mine", point, 1f);
            Animal.Noise(transform.position, 26f);
            FX.Burst(point, Wood ? new Color(0.78f, 0.6f, 0.38f) : new Color(0.62f, 0.63f, 0.65f), 16, 0.13f, 3.4f);
            GameManager.I.Hud.NodeBar(Mathf.Max(0, _hp) / (float)MaxHp);
            if (Visual != null) StartCoroutine(Wobble());
            if (_hp > 0) return;
            switch (Type)
            {
                case NodeType.Palm: Give(("log", 2), ("leaf", 3), ("coconut", 1)); break;
                case NodeType.Tree: Give(("log", 3), ("stick", 2)); break;
                case NodeType.Bamboo: Give(("bamboo", 4)); break;
                case NodeType.DeadTree: Give(("stick", 5), ("log", 1)); break;
                case NodeType.Rock: Give(("stone", 3)); if (Random.value < 0.4f) Give(("flint", 1)); break;
                default: Give(("flint", 3), ("stone", 1)); break;
            }
            if (Wood) { AudioManager.I.PlayAt("tree_fall", transform.position, 1f); StartCoroutine(Fall()); }
            else { AudioManager.I.PlayAt("rock_break", transform.position, 1f); FX.Burst(transform.position + Vector3.up * 0.5f, new Color(0.6f, 0.6f, 0.62f), 26, 0.2f, 4f); Remove(); }
        }

        void Give(params (string id, int n)[] items)
        {
            var inv = GameManager.I.Inv;
            foreach (var it in items)
            {
                int left = inv.Add(it.id, it.n);
                if (left > 0) Pickup.Spawn(it.id, left, transform.position + Vector3.up * 0.6f + Random.insideUnitSphere * 0.5f);
            }
        }

        IEnumerator Wobble()
        {
            var t = Visual.transform; Quaternion r0 = t.localRotation; float time = 0;
            Vector3 axis = Random.onUnitSphere; axis.y = 0;
            while (time < 0.3f) { time += Time.deltaTime; t.localRotation = r0 * Quaternion.AngleAxis(Mathf.Sin(time * 40f) * 2.5f * (1 - time / 0.3f), axis); yield return null; }
            t.localRotation = r0;
        }

        IEnumerator Fall()
        {
            _falling = true;
            var col = GetComponent<Collider>(); if (col != null) col.enabled = false;
            var t = Visual.transform; Quaternion r0 = t.localRotation;
            Vector3 away = transform.position - GameManager.I.Player.transform.position; away.y = 0;
            Vector3 axis = Vector3.Cross(Vector3.up, away.normalized);
            float time = 0;
            while (time < 1f) { time += Time.deltaTime / 1.5f; t.rotation = Quaternion.AngleAxis(Tween.Evaluate(Ease.InCubic, time) * 86f, axis) * transform.rotation * r0; yield return null; }
            // the crown hits the ground: leaves and dust along the trunk, then a small bounce
            for (int i = 1; i <= 4; i++)
                FX.Burst(transform.position + away.normalized * (i * 1.5f) + Vector3.up * 0.4f, i < 3 ? new Color(0.72f, 0.64f, 0.46f) : new Color(0.3f, 0.6f, 0.28f), 14, 0.2f, 3.5f);
            GameManager.I.Player.Shake(0.7f); Haptics.Pulse(80);
            float bounce = 0;
            while (bounce < 1f) { bounce += Time.deltaTime / 0.32f; t.rotation = Quaternion.AngleAxis(86f - Mathf.Sin(bounce * Mathf.PI) * 5f, axis) * transform.rotation * r0; yield return null; }
            yield return new WaitForSeconds(0.5f);
            t.localRotation = r0; _falling = false;
            Remove();
        }

        void Remove()
        {
            SetRemoved(true);
            GameManager.I.MarkRemoved(this, RespawnDays);
        }

        public override void SetRemoved(bool removed)
        {
            _removed = removed; _hp = -1;
            if (Visual != null) Visual.SetActive(!removed);
            if (Depleted != null) Depleted.SetActive(removed);
            var col = GetComponent<Collider>();
            if (col != null && !KeepCollider) col.enabled = !removed || Depleted != null;
        }
    }
}
