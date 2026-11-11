using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastShore
{
    public enum ItemKind { Resource, Food, Drink, Medical, Tool, Placeable, Special }
    public enum ToolType { None, Axe, Pickaxe, Spear, Torch, Rod, Bow, Knife }
    public enum Station { None, Campfire, Workbench, Pot }

    public sealed class ItemDef
    {
        public string Id, Name, Desc, Model;
        public ItemKind Kind;
        public int Stack = 20;
        public float Food, Water, Health;       // applied on consume
        public bool CuresPoison;
        public string ReturnsItem;               // e.g. empty flask after drinking
        public ToolType Tool; public float Damage; public int Durability;
        public string PlacePrefab;               // structure prefab for placeables
        Sprite _icon;
        public Sprite Icon => _icon != null ? _icon : (_icon = Resources.Load<Sprite>("Icons/" + Id));
    }

    public sealed class Recipe
    {
        public string Result; public int Count = 1; public Station Station; public string Group;
        public (string id, int n)[] Cost;
        public string Returns;                   // by-product handed back (e.g. the empty flask)
    }

    public static class ItemDB
    {
        static readonly Dictionary<string, ItemDef> Map = new Dictionary<string, ItemDef>();
        public static readonly List<ItemDef> All = new List<ItemDef>();
        public static readonly List<Recipe> Recipes = new List<Recipe>();

        static ItemDef Add(string id, string name, ItemKind kind, string model, string desc, int stack = 20)
        {
            var d = new ItemDef { Id = id, Name = name, Kind = kind, Model = model, Desc = desc, Stack = stack };
            Map[id] = d; All.Add(d); return d;
        }

        static Recipe R(string result, Station st, string group, params (string, int)[] cost)
        {
            var r = new Recipe { Result = result, Station = st, Group = group, Cost = cost };
            Recipes.Add(r); return r;
        }

        static ItemDB()
        {
            // ---- resources
            Add("stick", "Stick", ItemKind.Resource, "Stick", "A dry branch. The start of every tool.");
            Add("stone", "Stone", ItemKind.Resource, "StoneItem", "A fist-sized rock.");
            Add("flint", "Flint", ItemKind.Resource, "FlintItem", "Hard, sharp-edged stone. Holds an edge.");
            Add("log", "Log", ItemKind.Resource, "Log", "Heavy timber for building.", 12);
            Add("fiber", "Plant Fiber", ItemKind.Resource, "FiberItem", "Tough strands that twist into cord.");
            Add("leaf", "Palm Leaf", ItemKind.Resource, "PalmLeaf", "Broad waterproof frond. Good roofing.");
            Add("bamboo", "Bamboo", ItemKind.Resource, "BambooItem", "Light, hollow and very strong.");
            Add("rope", "Rope", ItemKind.Resource, "RopeCoil", "Strong twisted cord.");
            Add("canvas", "Sailcloth", ItemKind.Resource, "CanvasRoll", "Salvaged canvas from the Meridian.", 4);
            Add("hide", "Hide", ItemKind.Resource, "HideItem", "Thick animal hide.");
            Add("bone", "Bone", ItemKind.Resource, "BoneItem", "Hard and easy to shape.");
            Add("feather", "Feather", ItemKind.Resource, "FeatherItem", "Fletching for arrows.");

            // ---- food and drink
            var d = Add("coconut", "Coconut", ItemKind.Food, "CoconutItem", "Sweet water and a little flesh.", 10); d.Food = 10; d.Water = 28;
            d = Add("berries", "Wild Berries", ItemKind.Food, "BerriesItem", "Tart and juicy."); d.Food = 8; d.Water = 4;
            d = Add("banana", "Bananas", ItemKind.Food, "BananaItem", "Ripe and filling."); d.Food = 18; d.Water = 5;
            d = Add("mushroom", "Mushroom", ItemKind.Food, "Mushroom", "Earthy. Fills a small gap."); d.Food = 7;
            d = Add("egg", "Wild Eggs", ItemKind.Food, "EggItem", "Rich food from a jungle fowl's nest.", 10); d.Food = 14;
            d = Add("clam", "Clam", ItemKind.Food, "ClamItem", "Dug from the tide pools. Salty."); d.Food = 9; d.Water = -4;
            d = Add("meat_raw", "Raw Meat", ItemKind.Food, "MeatRaw", "Eat it raw and you will regret it. Cook it."); d.Food = 12; d.Health = -10;
            d = Add("meat_cooked", "Cooked Meat", ItemKind.Food, "MeatCooked", "A proper meal."); d.Food = 42; d.Health = 6;
            d = Add("venison_raw", "Raw Venison", ItemKind.Food, "VenisonRaw", "Lean deer meat. Cook it first."); d.Food = 14; d.Health = -8;
            d = Add("venison_cooked", "Roast Venison", ItemKind.Food, "VenisonCooked", "The best meal on the island."); d.Food = 58; d.Health = 10;
            d = Add("fish_raw", "Raw Fish", ItemKind.Food, "FishRaw", "Fresh catch. Better cooked."); d.Food = 10; d.Health = -6;
            d = Add("fish_cooked", "Grilled Fish", ItemKind.Food, "FishCooked", "Light, filling and safe."); d.Food = 32; d.Health = 4;
            d = Add("stew", "Island Stew", ItemKind.Food, "Stew", "Hot, thick and restoring. Food and water in one bowl.", 5); d.Food = 75; d.Water = 30; d.Health = 15;
            Add("flask_empty", "Empty Flask", ItemKind.Resource, "Flask", "A coconut-shell flask. Fill it at fresh water.", 5);
            d = Add("flask_full", "Water Flask", ItemKind.Drink, "Flask", "Clean fresh water.", 5); d.Water = 50; d.ReturnsItem = "flask_empty";

            // ---- medicine
            d = Add("herb", "Healing Herb", ItemKind.Medical, "HerbItem", "Chewed raw it slows poison and eases pain."); d.Health = 8; d.CuresPoison = true;
            d = Add("bandage", "Leaf Bandage", ItemKind.Medical, "Bandage", "Stops bleeding and helps wounds close.", 10); d.Health = 35;
            d = Add("antidote", "Herbal Tonic", ItemKind.Medical, "Antidote", "Clears poison at once and restores strength.", 5); d.Health = 25; d.CuresPoison = true; d.Water = 20; d.ReturnsItem = "flask_empty";

            // ---- tools and weapons
            d = Add("axe", "Stone Axe", ItemKind.Tool, "Axe", "Fells trees and bamboo. A fair weapon in a pinch.", 1); d.Tool = ToolType.Axe; d.Damage = 20; d.Durability = 100;
            d = Add("pickaxe", "Stone Pickaxe", ItemKind.Tool, "Pickaxe", "Breaks rock into stone and flint.", 1); d.Tool = ToolType.Pickaxe; d.Damage = 15; d.Durability = 90;
            d = Add("knife", "Flint Knife", ItemKind.Tool, "Knife", "Fast and light. Gets more meat and hide from a kill.", 1); d.Tool = ToolType.Knife; d.Damage = 16; d.Durability = 110;
            d = Add("spear", "Flint Spear", ItemKind.Tool, "Spear", "Long reach. The best answer to a charging boar.", 1); d.Tool = ToolType.Spear; d.Damage = 34; d.Durability = 70;
            d = Add("bow", "Bamboo Bow", ItemKind.Tool, "Bow", "Silent and deadly at range. Needs arrows.", 1); d.Tool = ToolType.Bow; d.Damage = 46; d.Durability = 90;
            Add("arrow", "Arrow", ItemKind.Resource, "Arrow", "Flint-tipped. Often recoverable.", 30);
            d = Add("torch", "Torch", ItemKind.Tool, "Torch", "Light in the dark. Most animals keep their distance from it.", 1); d.Tool = ToolType.Torch; d.Damage = 8; d.Durability = 300;
            d = Add("rod", "Fishing Rod", ItemKind.Tool, "FishingRod", "Cast into the sea and wait for a bite.", 1); d.Tool = ToolType.Rod; d.Damage = 4; d.Durability = 45;

            // ---- special
            Add("compass", "Brass Compass", ItemKind.Special, "Compass", "Shows the island map while you carry it.", 1);
            Add("hide_pack", "Hide Pack", ItemKind.Special, "Backpack", "Twelve more places to carry things.", 1);

            // ---- placeables
            d = Add("campfire_kit", "Campfire", ItemKind.Placeable, "Campfire", "Warmth, light, cooked food. Boars keep away.", 3); d.PlacePrefab = "Campfire";
            d = Add("shelter_kit", "Shelter", ItemKind.Placeable, "Shelter", "A lean-to. Sleep through the night and stay dry.", 2); d.PlacePrefab = "Shelter";
            d = Add("bed_kit", "Leaf Bed", ItemKind.Placeable, "Bed", "Sleep anywhere. Deeper rest heals more.", 2); d.PlacePrefab = "Bed";
            d = Add("workbench_kit", "Workbench", ItemKind.Placeable, "Workbench", "A solid surface for finer work.", 2); d.PlacePrefab = "Workbench";
            d = Add("pot_kit", "Cooking Pot", ItemKind.Placeable, "CookingPot", "Simmer stews and brew tonics.", 2); d.PlacePrefab = "CookingPot";
            d = Add("collector_kit", "Rain Collector", ItemKind.Placeable, "WaterCollector", "Funnels rain into a bowl. Fill flasks here.", 2); d.PlacePrefab = "WaterCollector";
            d = Add("trap_kit", "Fish Trap", ItemKind.Placeable, "FishTrap", "Set it in shallow water. It catches fish while you work.", 3); d.PlacePrefab = "FishTrap";
            d = Add("torchstand_kit", "Standing Torch", ItemKind.Placeable, "TorchStand", "A light that stays where you plant it.", 6); d.PlacePrefab = "TorchStand";
            d = Add("fence_kit", "Stake Fence", ItemKind.Placeable, "Fence", "Sharpened stakes. Animals cannot push through.", 12); d.PlacePrefab = "Fence";

            R("rope", Station.None, "Basics", ("fiber", 3));
            R("bandage", Station.None, "Basics", ("fiber", 2), ("leaf", 1));
            R("flask_empty", Station.None, "Basics", ("coconut", 1), ("fiber", 1));
            R("axe", Station.None, "Tools", ("stick", 2), ("stone", 2), ("fiber", 2));
            R("pickaxe", Station.None, "Tools", ("stick", 2), ("stone", 3), ("fiber", 2));
            R("knife", Station.None, "Tools", ("stick", 1), ("flint", 1), ("fiber", 1));
            R("torch", Station.None, "Tools", ("stick", 1), ("fiber", 2));
            R("spear", Station.None, "Tools", ("stick", 2), ("flint", 1), ("fiber", 2));
            R("rod", Station.Workbench, "Tools", ("bamboo", 1), ("rope", 1), ("bone", 1));
            R("bow", Station.Workbench, "Tools", ("bamboo", 2), ("rope", 1));
            R("arrow", Station.None, "Tools", ("stick", 1), ("flint", 1), ("feather", 1)).Count = 4;
            R("hide_pack", Station.Workbench, "Tools", ("hide", 3), ("rope", 2));
            R("campfire_kit", Station.None, "Camp", ("stick", 4), ("stone", 5));
            R("shelter_kit", Station.None, "Camp", ("log", 4), ("stick", 6), ("leaf", 8));
            R("workbench_kit", Station.None, "Camp", ("log", 4), ("stick", 4), ("rope", 1));
            R("bed_kit", Station.None, "Camp", ("log", 2), ("leaf", 8), ("hide", 1));
            R("torchstand_kit", Station.None, "Camp", ("stick", 2), ("fiber", 2), ("stone", 2));
            R("fence_kit", Station.None, "Camp", ("stick", 5), ("rope", 1)).Count = 2;
            R("collector_kit", Station.Workbench, "Camp", ("bamboo", 3), ("leaf", 4), ("coconut", 1));
            R("pot_kit", Station.Workbench, "Camp", ("stone", 6), ("stick", 3), ("rope", 1));
            R("trap_kit", Station.Workbench, "Camp", ("bamboo", 4), ("rope", 2));
            R("meat_cooked", Station.Campfire, "Cooking", ("meat_raw", 1));
            R("venison_cooked", Station.Campfire, "Cooking", ("venison_raw", 1));
            R("fish_cooked", Station.Campfire, "Cooking", ("fish_raw", 1));
            R("stew", Station.Pot, "Cooking", ("meat_raw", 1), ("mushroom", 2), ("flask_full", 1)).Returns = "flask_empty";
            R("antidote", Station.Pot, "Cooking", ("herb", 2), ("flask_full", 1));
        }

        public static ItemDef Get(string id) => id != null && Map.TryGetValue(id, out var d) ? d : null;
    }

    [Serializable]
    public class ItemStack
    {
        public string id; public int count; public int durability;
        public bool Empty => string.IsNullOrEmpty(id) || count <= 0;
        public ItemDef Def => ItemDB.Get(id);
        public void Clear() { id = null; count = 0; durability = 0; }
    }

    /// <summary>Slot based inventory. Slots 0..HotbarSize-1 form the hotbar; the Hide Pack unlocks the last two rows.</summary>
    public class Inventory
    {
        public const int HotbarSize = 6, Size = 36, BaseSlots = 24;
        public readonly ItemStack[] Slots = new ItemStack[Size];
        public int Unlocked = BaseSlots;
        public event Action Changed;
        public event Action<ItemDef, int> Added;

        public Inventory() { for (int i = 0; i < Size; i++) Slots[i] = new ItemStack(); }

        public int Count(string id)
        {
            int n = 0;
            foreach (var s in Slots) if (!s.Empty && s.id == id) n += s.count;
            return n;
        }

        /// <summary>Adds items; returns how many did not fit.</summary>
        public int Add(string id, int count, int durability = -1, bool notify = true)
        {
            var def = ItemDB.Get(id);
            if (def == null || count <= 0) return count;
            int left = count;
            if (def.Stack > 1)
                for (int i = 0; i < Unlocked && left > 0; i++)
                {
                    var s = Slots[i];
                    if (!s.Empty && s.id == id && s.count < def.Stack) { int n = Mathf.Min(left, def.Stack - s.count); s.count += n; left -= n; }
                }
            // tools and placeables prefer the hotbar, everything else fills the backpack first
            for (int pass = 0; pass < 2 && left > 0; pass++)
            {
                bool hotbarFirst = def.Kind == ItemKind.Tool || def.Kind == ItemKind.Placeable;
                int start = hotbarFirst == (pass == 0) ? 0 : HotbarSize;
                int end = start == 0 ? HotbarSize : Unlocked;
                for (int i = start; i < end && left > 0; i++)
                {
                    var s = Slots[i];
                    if (!s.Empty) continue;
                    int n = Mathf.Min(left, def.Stack);
                    s.id = id; s.count = n; s.durability = durability >= 0 ? durability : def.Durability; left -= n;
                }
            }
            if (left < count)
            {
                Changed?.Invoke();
                if (notify) Added?.Invoke(def, count - left);
            }
            return left;
        }

        public bool Remove(string id, int count)
        {
            if (Count(id) < count) return false;
            for (int i = Size - 1; i >= 0 && count > 0; i--)
            {
                var s = Slots[i];
                if (s.Empty || s.id != id) continue;
                int n = Mathf.Min(count, s.count); s.count -= n; count -= n;
                if (s.count <= 0) s.Clear();
            }
            Changed?.Invoke();
            return true;
        }

        public void RemoveAt(int slot, int count = 1)
        {
            var s = Slots[slot];
            if (s.Empty) return;
            s.count -= count;
            if (s.count <= 0) s.Clear();
            Changed?.Invoke();
        }

        public void Swap(int a, int b)
        {
            if (a == b || a >= Unlocked || b >= Unlocked) return;
            var sa = Slots[a]; var sb = Slots[b];
            if (!sa.Empty && !sb.Empty && sa.id == sb.id && sa.Def.Stack > 1)
            {
                int n = Mathf.Min(sa.count, sb.Def.Stack - sb.count);
                sb.count += n; sa.count -= n; if (sa.count <= 0) sa.Clear();
            }
            else { Slots[a] = sb; Slots[b] = sa; }
            Changed?.Invoke();
        }

        public bool CanCraft(Recipe r)
        {
            foreach (var c in r.Cost) if (Count(c.id) < c.n) return false;
            return true;
        }

        public bool Craft(Recipe r)
        {
            if (!CanCraft(r)) return false;
            foreach (var c in r.Cost) Remove(c.id, c.n);
            if (r.Result == "hide_pack") { Unlocked = Size; Changed?.Invoke(); Added?.Invoke(ItemDB.Get(r.Result), 1); }
            else Add(r.Result, r.Count);
            if (r.Returns != null) Add(r.Returns, 1, -1, false);
            return true;
        }

        public void NotifyChanged() => Changed?.Invoke();
    }
}
