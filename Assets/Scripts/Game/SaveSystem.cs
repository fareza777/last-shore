using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LastShore
{
    [Serializable]
    public class PlacedData { public string prefab; public float x, y, z, rot; public float fuel, water; public bool lit; public ItemStack[] items; }

    [Serializable]
    public class SaveData
    {
        public int version = 2;
        public List<string> flags = new List<string>();
        public int unlocked = Inventory.BaseSlots; public float poison;
        public float px, py, pz, yaw;
        public float health = 100, hunger = 100, thirst = 100, stamina = 100, warmth = 100;
        public ItemStack[] slots;
        public int hotbar;
        public float timeOfDay = 0.3f; public int day = 1; public int weather; public float playSeconds;
        public int questStage; public bool boarSeen, stormSeen;
        public List<int> notes = new List<int>();
        public List<int> removedIds = new List<int>();
        public List<float> removedUntil = new List<float>();
        public List<PlacedData> placed = new List<PlacedData>();
        public int raftStage;
        public List<int> lootedCrates = new List<int>();
    }

    public static class SaveSystem
    {
        static string FilePath => Path.Combine(Application.persistentDataPath, "lastshore_save.json");

        public static bool HasSave => File.Exists(FilePath);

        public static void Save(SaveData data)
        {
            try { File.WriteAllText(FilePath, JsonUtility.ToJson(data)); }
            catch (Exception e) { Debug.LogError("Save failed: " + e.Message); }
        }

        public static SaveData Load()
        {
            try
            {
                if (!HasSave) return null;
                var d = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
                return d != null && d.version == 2 && d.slots != null && d.slots.Length == Inventory.Size ? d : null;
            }
            catch (Exception e) { Debug.LogError("Load failed: " + e.Message); return null; }
        }

        public static void Delete()
        {
            try { if (HasSave) File.Delete(FilePath); }
            catch (Exception e) { Debug.LogError("Delete save failed: " + e.Message); }
        }
    }
}
