using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastShore
{
    public class LootCrate : MonoBehaviour, IInteractable
    {
        public int Index; public string ItemId = "rope"; public int Count = 2; public string ItemId2; public int Count2;
        bool _looted;
        public string Prompt() => _looted ? null : "Search the crate";
        public void Interact()
        {
            if (_looted) return;
            var gm = GameManager.I;
            _looted = true; gm.LootedCrates.Add(Index);
            AudioManager.I.Play("ui_open");
            Tween.Punch(transform, 0.12f);
            Drop(ItemId, Count); if (!string.IsNullOrEmpty(ItemId2)) Drop(ItemId2, Count2);
            gm.Quests.Notify("loot");
        }
        void Drop(string id, int n)
        {
            int left = GameManager.I.Inv.Add(id, n);
            if (left > 0) Pickup.Spawn(id, left, transform.position + transform.forward * 0.9f + Vector3.up * 0.5f);
        }
        public void Refresh() { _looted = GameManager.I.LootedCrates.Contains(Index); }
    }
}
