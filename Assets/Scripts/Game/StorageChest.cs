using UnityEngine;

namespace LastShore
{
    /// <summary>A chest the player builds to keep things at camp. Its contents are saved with the structure.</summary>
    public class StorageChest : PlacedStructure, IInteractable
    {
        public const int Size = 18;
        public readonly ItemStack[] Slots = new ItemStack[Size];

        void Awake() { for (int i = 0; i < Size; i++) Slots[i] = new ItemStack(); }

        public string Prompt() => "Open the storage chest";
        public void Interact() { AudioManager.I.Play("ui_open"); GameManager.I.InvUI.OpenChest(this); }

        public override PlacedData ToData() { var d = base.ToData(); d.items = Slots; return d; }
        public override void FromData(PlacedData d)
        {
            if (d.items == null) return;
            for (int i = 0; i < Size && i < d.items.Length; i++) if (d.items[i] != null) Slots[i] = d.items[i];
        }
    }
}
