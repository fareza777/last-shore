using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LastShore
{
    /// <summary>World constants and terrain helpers.</summary>
    public static class World
    {
        public const float SeaLevel = 6f;
        public const float Size = 1000f;
        public const float ShoreLow = 8f, ShoreRange = 16f;   // the shore map covers heights from SeaLevel - 8 to SeaLevel + 8
        public static Vector3 Center => new Vector3(Size * 0.5f, SeaLevel, Size * 0.5f);
        public const int InteractLayer = 8;   // "Interact": pickups and other trigger-only targets
        public const int GhostLayer = 2;      // Ignore Raycast

        public static float Height(Vector3 p)
        {
            var t = Terrain.activeTerrain;
            return t != null ? t.SampleHeight(p) + t.transform.position.y : 0f;
        }
        public static Vector3 OnGround(Vector3 p) { p.y = Height(p); return p; }
        public static bool IsLand(Vector3 p, float margin = 0.25f) => Height(p) > SeaLevel + margin;
        public static float Slope(Vector3 p)
        {
            var t = Terrain.activeTerrain;
            if (t == null) return 0;
            Vector3 l = p - t.transform.position; var s = t.terrainData.size;
            return t.terrainData.GetSteepness(Mathf.Clamp01(l.x / s.x), Mathf.Clamp01(l.z / s.z));
        }
        public static Transform Find(string name) { var g = GameObject.Find(name); return g != null ? g.transform : null; }
    }

    /// <summary>Base for anything in the world whose removed/harvested state is saved by id.</summary>
    public abstract class WorldNode : MonoBehaviour
    {
        public int Id;
        public abstract void SetRemoved(bool removed);
    }

    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager I;
        public static bool StartNewGame = true;

        public Inventory Inv { get; private set; }
        public PlayerController Player { get; private set; }
        public PlayerVitals Vitals { get; private set; }
        public PlayerInteraction Interaction { get; private set; }
        public DayNight Sky { get; private set; }
        public Weather Weather { get; private set; }
        public Quests Quests { get; private set; }
        public BuildSystem Build { get; private set; }
        public HUD Hud { get; private set; }
        public InventoryUI InvUI { get; private set; }
        public PauseMenu Pause { get; private set; }

        public bool Cinematic, Dead;
        public float PlaySeconds;
        public readonly List<int> Notes = new List<int>();
        public readonly List<int> LootedCrates = new List<int>();
        public int RaftStage;
        public readonly HashSet<string> Flags = new HashSet<string>();
        public EventDirector Events { get; private set; }

        /// <summary>Speaks a narrator line the first time something happens.</summary>
        public void SayOnce(string key) { if (Flags.Add("said_" + key)) AudioManager.I.Say(key, Story.Line(key)); }
        public int DiscoveredCount { get { int n = 0; foreach (var l in Landmark.All) if (l.Found) n++; return n; } }

        readonly Dictionary<int, WorldNode> _nodes = new Dictionary<int, WorldNode>();
        readonly Dictionary<int, float> _removedUntil = new Dictionary<int, float>();
        readonly List<PlacedStructure> _placed = new List<PlacedStructure>();
        float _autosave, _fpsTime; int _fpsFrames;

        public bool MenuOpen => (InvUI != null && InvUI.Open) || (Pause != null && Pause.Open);
        public bool InputLocked => Cinematic || Dead || MenuOpen;
        public float DayFloat => Sky.Day + Sky.TimeOfDay;

        void Awake()
        {
            I = this;
            Time.timeScale = 1f;
            GameSettings.Load();
            Inv = new Inventory();
            Sky = GetOrAdd<DayNight>(); Weather = GetOrAdd<Weather>(); Quests = GetOrAdd<Quests>(); Build = GetOrAdd<BuildSystem>();
            foreach (var n in FindObjectsByType<WorldNode>(FindObjectsInactive.Include, FindObjectsSortMode.None)) _nodes[n.Id] = n;

            var spawn = World.Find("PlayerSpawn");
            Vector3 pos = spawn != null ? spawn.position : new Vector3(World.Size * 0.5f, 12, 120);
            float yaw = spawn != null ? spawn.eulerAngles.y : 0;
            Player = PlayerController.Create(World.OnGround(pos) + Vector3.up * 0.1f, yaw);
            Vitals = Player.GetComponent<PlayerVitals>();
            Interaction = Player.GetComponent<PlayerInteraction>();

            Hud = HUD.Create(); InvUI = InventoryUI.Create(); Pause = PauseMenu.Create();
            if (GameInput.TouchMode) TouchControls.Create();
            var shore = Resources.Load<Texture2D>("World/ShoreMap");
            Shader.SetGlobalTexture("_LS_ShoreMap", shore);
            Shader.SetGlobalVector("_LS_ShoreParams", new Vector4(1f / World.Size, World.SeaLevel - World.ShoreLow, World.ShoreRange, shore != null ? 1f : 0f));
            ApplyQuality(); GameSettings.Changed += ApplyQuality;
            gameObject.AddComponent<AnimalSpawner>();
            gameObject.AddComponent<WorldStreamer>();
            Events = gameObject.AddComponent<EventDirector>();
        }

        T GetOrAdd<T>() where T : Component { var c = GetComponent<T>(); return c != null ? c : gameObject.AddComponent<T>(); }

        void Start()
        {
            var data = StartNewGame ? null : SaveSystem.Load();
            if (data != null) ApplySave(data);
            else StartCoroutine(NewGameSequence());
            Vitals.Died += OnDied;
            Inv.Added += (def, n) => { Hud.Toast("+" + n + "  " + def.Name, def.Icon); Quests.Notify("pickup", def.Id); };
            Sky.NewDay += d => { Hud.Banner("DAY " + d, DayLine(d)); Quests.Notify("newday"); RespawnNodes(); SaveGame(); };
        }

        static string DayLine(int d) => d == 2 ? "You made it through the night." : d < 6 ? "Keep going." : d < 12 ? "The storm season is getting closer." : "The sea will not wait much longer.";

        IEnumerator NewGameSequence()
        {
            Cinematic = true;
            Sky.TimeOfDay = 0.30f; Sky.Day = 1;
            Hud.SetVisible(false);
            SceneFader.SetBlack();
            Quests.SetStage(0, false);
            AudioManager.I.Say("intro_6", Story.Intro[5]);
            yield return Player.WakeUp();
            Hud.SetVisible(true);
            Cinematic = false;
            AudioManager.I.Say("wake", Story.Line("wake"));
            yield return new WaitForSeconds(1.2f);
            Quests.Announce();
            SaveGame();
        }

        void Update()
        {
            GameInput.Poll();
            if (Dead || Cinematic) { ApplyCursor(); return; }
            PlaySeconds += Time.deltaTime;
            _fpsFrames++; _fpsTime += Time.unscaledDeltaTime;
            if (_fpsTime >= 5f) { Debug.Log("[FPS] " + (_fpsFrames / _fpsTime).ToString("0.0") + " quality=" + GameSettings.Quality + " " + Screen.width + "x" + Screen.height); _fpsFrames = 0; _fpsTime = 0; }
            if (GameInput.Pause) { if (InvUI.Open) InvUI.Close(); else Pause.Toggle(); }
            else if (GameInput.Inventory && !Pause.Open) InvUI.Toggle();
            _autosave += Time.deltaTime;
            if (_autosave > 150f && !MenuOpen) { _autosave = 0; SaveGame(); }
            ApplyCursor();
        }

        void ApplyCursor()
        {
            bool free = GameInput.TouchMode || MenuOpen || Dead || (Cinematic && EndingCinematic.Active);
            Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = free;
        }

        // ---------------------------------------------------------------- world state
        public void MarkRemoved(WorldNode node, float respawnAfterDays)
        {
            _removedUntil[node.Id] = respawnAfterDays < 0 ? float.MaxValue : DayFloat + respawnAfterDays;
        }

        void RespawnNodes()
        {
            var back = new List<int>();
            foreach (var kv in _removedUntil) if (kv.Value <= DayFloat) back.Add(kv.Key);
            foreach (int id in back)
            {
                _removedUntil.Remove(id);
                if (_nodes.TryGetValue(id, out var n) && n != null && (n.transform.position - Player.transform.position).sqrMagnitude > 30 * 30) n.SetRemoved(false);
                else if (n != null) _removedUntil[id] = DayFloat + 0.5f;
            }
        }

        public void Register(PlacedStructure s) { if (!_placed.Contains(s)) _placed.Add(s); }
        public void Unregister(PlacedStructure s) { _placed.Remove(s); }

        public PlacedStructure SpawnStructure(string prefab, Vector3 pos, float rot)
        {
            var src = Resources.Load<GameObject>("Prefabs/" + prefab);
            if (src == null) { Debug.LogError("Missing structure prefab " + prefab); return null; }
            var go = Instantiate(src, pos, Quaternion.Euler(0, rot, 0));
            var s = go.GetComponent<PlacedStructure>(); if (s == null) s = go.AddComponent<PlacedStructure>();
            s.Prefab = prefab;
            return s;
        }

        // ---------------------------------------------------------------- save / load
        public void SaveGame()
        {
            if (Dead || EndingCinematic.Active) return;
            var d = new SaveData();
            var p = Player.transform.position;
            d.px = p.x; d.py = p.y; d.pz = p.z; d.yaw = Player.transform.eulerAngles.y;
            d.health = Vitals.Health; d.hunger = Vitals.Hunger; d.thirst = Vitals.Thirst; d.stamina = Vitals.Stamina; d.warmth = Vitals.Warmth;
            d.slots = Inv.Slots; d.hotbar = Interaction.Selected;
            d.timeOfDay = Sky.TimeOfDay; d.day = Sky.Day; d.weather = (int)Weather.State; d.playSeconds = PlaySeconds;
            d.questStage = Quests.Stage; d.boarSeen = Quests.BoarSeen; d.stormSeen = Quests.StormSeen;
            d.flags = new List<string>(Flags); d.unlocked = Inv.Unlocked; d.poison = Vitals.PoisonTime;
            d.notes = new List<int>(Notes); d.lootedCrates = new List<int>(LootedCrates); d.raftStage = RaftStage;
            foreach (var kv in _removedUntil) { d.removedIds.Add(kv.Key); d.removedUntil.Add(kv.Value); }
            foreach (var s in _placed) if (s != null) d.placed.Add(s.ToData());
            SaveSystem.Save(d);
        }

        void ApplySave(SaveData d)
        {
            Player.Teleport(new Vector3(d.px, d.py + 0.2f, d.pz), d.yaw);
            Vitals.Set(d.health, d.hunger, d.thirst, d.stamina, d.warmth);
            for (int i = 0; i < Inventory.Size; i++) Inv.Slots[i] = d.slots[i] ?? new ItemStack();
            Inv.NotifyChanged();
            Interaction.Select(d.hotbar);
            Sky.TimeOfDay = d.timeOfDay; Sky.Day = d.day; Weather.Force((WeatherState)d.weather); PlaySeconds = d.playSeconds;
            foreach (var f in d.flags) Flags.Add(f);
            Inv.Unlocked = Mathf.Clamp(d.unlocked, Inventory.BaseSlots, Inventory.Size); Vitals.SetPoison(d.poison);
            Notes.AddRange(d.notes); LootedCrates.AddRange(d.lootedCrates); RaftStage = d.raftStage;
            Quests.BoarSeen = d.boarSeen; Quests.StormSeen = d.stormSeen;
            for (int i = 0; i < d.removedIds.Count; i++)
            {
                _removedUntil[d.removedIds[i]] = d.removedUntil[i];
                if (_nodes.TryGetValue(d.removedIds[i], out var n) && n != null) n.SetRemoved(true);
            }
            foreach (var pd in d.placed)
            {
                var s = SpawnStructure(pd.prefab, new Vector3(pd.x, pd.y, pd.z), pd.rot);
                if (s != null) s.FromData(pd);
            }
            foreach (var c in FindObjectsByType<LootCrate>(FindObjectsSortMode.None)) c.Refresh();
            foreach (var n in FindObjectsByType<NotePickup>(FindObjectsSortMode.None)) n.Refresh();
            var raft = FindFirstObjectByType<RaftSite>(); if (raft != null) raft.Refresh();
            Quests.SetStage(d.questStage, false);
            SceneFader.FadeIn(1.2f, 0.2f);
            Tween.Delay(this, 1.5f, () => Quests.Announce());
        }

        // ---------------------------------------------------------------- death
        void OnDied(string cause)
        {
            if (Dead) return;
            Dead = true;
            if (InvUI.Open) InvUI.Close();
            AudioManager.I.Play("hurt");
            Player.FallOver();
            Hud.ShowDeath(Sky.Day, cause);
        }

        public void ReloadLastSave() { StartNewGame = false; SceneFader.Load("Game"); }
        public void QuitToMenu(bool save) { if (save) SaveGame(); SceneFader.Load("MainMenu"); }

        void OnApplicationPause(bool paused) { if (paused && !Dead && !Cinematic && Player != null) SaveGame(); }
        void ApplyQuality()
        {
            int q = GameSettings.Quality;
            var t = Terrain.activeTerrain;
            if (t != null)
            {
                t.detailObjectDensity = q == 0 ? 0f : q == 1 ? 0.55f : 1f; t.detailObjectDistance = q == 0 ? 20 : q == 1 ? 50 : 80;
                t.heightmapPixelError = q == 0 ? 22 : q == 1 ? 10 : 5; t.basemapDistance = q == 0 ? 60 : q == 1 ? 110 : 220;
                t.shadowCastingMode = q == 0 ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
            }
            if (RenderSettings.sun != null) RenderSettings.sun.shadows = q == 0 ? LightShadows.None : LightShadows.Soft;
            if (Player != null && Player.Cam != null)
            {
                Player.Cam.farClipPlane = q == 0 ? 520f : q == 1 ? 1000f : 1800f;
                var data = Player.Cam.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = q > 0;
                data.antialiasing = q == 0 ? AntialiasingMode.None : Application.isMobilePlatform ? AntialiasingMode.FastApproximateAntialiasing : AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            }
        }

        void OnDestroy() { GameSettings.Changed -= ApplyQuality; if (I == this) I = null; }
    }
}
