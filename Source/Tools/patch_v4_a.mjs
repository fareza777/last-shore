// v4 (part a): world and systems. Sky globals, blob shadows, wet sand, haptics, balance, buffs, camp tiers,
// fishing mini-game, rain versus fire, storm flotsam, landmark rewards, pickup sweep, storage chest item.
import fs from 'node:fs';
const root = 'C:/Survival Fable 62/Assets/';
const results = [];
function patch(file, edits) {
  let s = fs.readFileSync(root + file, 'utf8').replace(/\r\n/g, '\n');
  for (const [from, to] of edits) {
    const ok = s.includes(from);
    results.push((ok ? 'ok   ' : 'MISS ') + file + ': ' + from.slice(0, 60).replace(/\n/g, ' '));
    if (ok) s = s.replace(from, to);
  }
  fs.writeFileSync(root + file, s);
}

// ---------------------------------------------------------------- shader: every plant gets its own shade
patch('Shaders/LastShoreLit.shader', [
  ['                o.albedo = _BaseColor.rgb * lerp(half3(1, 1, 1), lin, _VertexColor);',
   `                o.albedo = _BaseColor.rgb * lerp(half3(1, 1, 1), lin, _VertexColor);
                // every plant gets its own shade, so a grove does not look stamped out (not for things held in the hand)
                float3 origin = GetObjectToWorldMatrix()._m03_m13_m23;
                half tone = frac(sin(dot(origin.xz, float2(12.9898, 78.233))) * 43758.5453) - 0.5h;
                tone *= saturate(v.color.a * 4.0h) * _VertexColor * saturate(distance(origin, _WorldSpaceCameraPos) - 1.5);
                o.albedo *= half3(1.0h + tone * 0.30h, 1.0h + tone * 0.14h, 1.0h - tone * 0.22h);`],
]);

// ---------------------------------------------------------------- sky, weather
patch('Scripts/Game/Environment.cs', [
  ['            if (RenderSettings.skybox != null) { _sky = new Material(RenderSettings.skybox); RenderSettings.skybox = _sky; }',
   `            var gameSky = Resources.Load<Material>("Materials/SkyGame");
            if (gameSky != null) RenderSettings.skybox = gameSky;          // clouds, sun and moon, driven by the globals below
            else if (RenderSettings.skybox != null) { _sky = new Material(RenderSettings.skybox); RenderSettings.skybox = _sky; }`],
  ['            Shader.SetGlobalFloat("_LS_Day", DayFactor * Mathf.Lerp(1f, 0.55f, Cloud));',
   `            Shader.SetGlobalFloat("_LS_Day", DayFactor * Mathf.Lerp(1f, 0.55f, Cloud));

            // ---- sky shader inputs
            Vector3 sunDir = Quaternion.Euler(ang, -38f, 0) * Vector3.back;
            Color top = Color.Lerp(Color.Lerp(new Color(0.012f, 0.028f, 0.085f), new Color(0.13f, 0.36f, 0.78f), DayFactor), new Color(0.30f, 0.27f, 0.50f), dusk * 0.5f);
            top = Color.Lerp(top, grey * Mathf.Lerp(0.1f, 0.72f, DayFactor), Cloud * 0.8f) + Color.white * Flash * 0.3f;
            Color sunCol = Color.Lerp(new Color(1f, 0.48f, 0.2f), new Color(1f, 0.95f, 0.82f), Mathf.Clamp01(elev * 3f)) * Mathf.Clamp01(elev * 8f + 0.6f) * Mathf.Lerp(1f, 0.12f, Cloud);
            Color moonCol = new Color(0.74f, 0.84f, 1f) * Mathf.Clamp01(-elev * 6f) * Mathf.Lerp(1f, 0.08f, Cloud);
            Color cloudCol = Color.Lerp(Color.Lerp(new Color(0.06f, 0.08f, 0.13f), new Color(0.97f, 0.98f, 1f), DayFactor), new Color(1f, 0.64f, 0.48f), dusk * 0.6f);
            cloudCol = Color.Lerp(cloudCol, grey * Mathf.Lerp(0.13f, 0.6f, DayFactor), Cloud * 0.85f) + Color.white * Flash * 0.5f;
            Shader.SetGlobalColor("_LS_SkyTop", top);
            Shader.SetGlobalVector("_LS_SunDir", sunDir); Shader.SetGlobalVector("_LS_MoonDir", -sunDir);
            Shader.SetGlobalColor("_LS_SunCol", sunCol); Shader.SetGlobalColor("_LS_MoonCol", moonCol);
            Shader.SetGlobalColor("_LS_CloudCol", cloudCol);
            Shader.SetGlobalFloat("_LS_CloudCover", Mathf.Lerp(0.2f, 1f, Cloud));`],
  ['            int day = _sky.Day;\n            if (day <= 1) { State = WeatherState.Clear; _timer = 240f; return; }',
   '            int day = _sky.Day; var before = State;\n            if (day <= 1) { State = WeatherState.Clear; _timer = 240f; return; }'],
  ['            _timer = State == WeatherState.Clear ? UnityEngine.Random.Range(220f, 460f) : UnityEngine.Random.Range(100f, 230f);',
   `            _timer = State == WeatherState.Clear ? UnityEngine.Random.Range(220f, 460f) : UnityEngine.Random.Range(100f, 230f);
            // a storm that blows itself out leaves something on the beach
            var gm = GameManager.I;
            if (before == WeatherState.Storm && State != WeatherState.Storm && gm != null && gm.Events != null) gm.Events.SpawnFlotsam();`],
]);

patch('Scripts/Game/EventDirector.cs', [
  ['            if (day < 2 || day % 2 != 0) return;\n            var gm = GameManager.I;\n            Vector3 p = gm.Player.transform.position;',
   `            if (day < 2 || day % 2 != 0) return;
            SpawnFlotsam();
        }

        /// <summary>Washes a crate up on the shore nearest the player and marks it on the compass.</summary>
        public void SpawnFlotsam()
        {
            var gm = GameManager.I; if (gm == null || gm.Player == null || Marker != null) return;
            Vector3 p = gm.Player.transform.position;`],
]);

patch('Scripts/Game/Campfire.cs', [
  ['                Fuel -= Time.deltaTime * (rain ? 1.6f : 1f);',
   `                bool open = rain && !Shelter.AnyNear(transform.position, 5f);   // rain drowns a fire with no roof beside it
                Fuel -= Time.deltaTime * (open ? 6f : rain ? 1.4f : 1f);`],
]);

// ---------------------------------------------------------------- vitals: gentler first days, buffs, damage direction
patch('Scripts/Game/PlayerVitals.cs', [
  ['        public event Action<float> Damaged;', `        public event Action<float> Damaged;
        public event Action<Vector3> HitFrom;
        /// <summary>Seconds left on the buff from a cooked meal (faster stamina and healing).</summary>
        public float WellFed { get; private set; }
        /// <summary>Seconds left on the buff from stew (the cold bites far less).</summary>
        public float Hearty { get; private set; }`],
  ['        public void Damage(float amount, string cause, bool feedback = true)', '        public void Damage(float amount, string cause, bool feedback = true, Vector3? from = null)'],
  ['                AudioManager.I.Play("hurt", 0.8f);\n                GameSettings.Vibrate();', '                AudioManager.I.Play("hurt", 0.8f);\n                Haptics.Pulse(70);\n                if (from.HasValue) HitFrom?.Invoke(from.Value);'],
  ['            Hunger = Mathf.Clamp(Hunger + def.Food, 0, 100);', `            if (def.Id == "stew") { WellFed = 420f; Hearty = 420f; }
            else if (def.Id.EndsWith("_cooked")) WellFed = Mathf.Max(WellFed, 240f);
            Hunger = Mathf.Clamp(Hunger + def.Food, 0, 100);`],
  ['            Hunger = Mathf.Max(0, Hunger - dt * (100f / 900f) * (_pc.Sprinting ? 1.5f : 1f));\n            Thirst = Mathf.Max(0, Thirst - dt * (100f / 660f) * (_pc.Sprinting ? 1.6f : 1f));',
   `            // the first two days are forgiving while the player learns where food and water are
            float needs = (gm.Sky.Day <= 1 ? 0.6f : gm.Sky.Day == 2 ? 0.8f : 1f) * Camp.NeedsMul;
            Hunger = Mathf.Max(0, Hunger - dt * (100f / 900f) * (_pc.Sprinting ? 1.5f : 1f) * needs);
            Thirst = Mathf.Max(0, Thirst - dt * (100f / 660f) * (_pc.Sprinting ? 1.6f : 1f) * needs);
            WellFed = Mathf.Max(0, WellFed - dt); Hearty = Mathf.Max(0, Hearty - dt);`],
  ['            else Stamina = Mathf.Min(100, Stamina + dt * (Hunger > 15 && Thirst > 15 ? 16f : 7f));', '            else Stamina = Mathf.Min(100, Stamina + dt * (Hunger > 15 && Thirst > 15 ? 16f : 7f) * Camp.StaminaMul * (WellFed > 0 ? 1.35f : 1f));'],
  ['            if (warm < 0 && Sheltered) warm *= 0.35f;', '            if (warm < 0 && Sheltered) warm *= 0.35f;\n            if (warm < 0 && Hearty > 0) warm *= 0.3f;'],
  ['Health = Mathf.Min(100, Health + dt * 0.55f);', 'Health = Mathf.Min(100, Health + dt * (WellFed > 0 ? 1.1f : 0.55f));'],
]);
for (const [f, from] of [['Boar.cs', 'GM.Vitals.Damage(Random.Range(15f, 22f), "a wild boar");'], ['Shark.cs', 'GM.Vitals.Damage(Random.Range(22f, 30f), "a shark");'], ['Snake.cs', 'GM.Vitals.Damage(7f, "a snake bite");']])
  patch('Scripts/Game/' + f, [[from, from.replace(');', ', true, transform.position);')]]);

patch('Scripts/Core/GameSettings.cs', [
  ['        public static int Quality = 2; // 0 low, 1 medium, 2 high', '        public static int Quality = 2; // 0 low, 1 medium, 2 high\n        public static bool QualityLocked;  // the player picked a tier by hand: never lower it automatically'],
  ['            Quality = PlayerPrefs.GetInt("s_quality3", Application.isMobilePlatform ? 0 : 2);', '            Quality = PlayerPrefs.GetInt("s_quality4", Application.isMobilePlatform ? 1 : 2);\n            QualityLocked = PlayerPrefs.GetInt("s_qlock", 0) == 1;'],
  ['            PlayerPrefs.SetInt("s_quality3", Quality);', '            PlayerPrefs.SetInt("s_quality4", Quality); PlayerPrefs.SetInt("s_qlock", QualityLocked ? 1 : 0);'],
  [`#if UNITY_ANDROID || UNITY_IOS
            if (Vibration && Application.isMobilePlatform) Handheld.Vibrate();
#endif`, '            Haptics.Pulse(60);'],
]);
patch('Scripts/UI/Menus.cs', [
  ['() => { GameSettings.Quality = (GameSettings.Quality + 1) % 3; GameSettings.Apply(); });', '() => { GameSettings.Quality = (GameSettings.Quality + 1) % 3; GameSettings.QualityLocked = true; GameSettings.Apply(); });'],
]);

// ---------------------------------------------------------------- game manager
patch('Scripts/Game/GameManager.cs', [
  ['            gameObject.AddComponent<AnimalSpawner>();', `            gameObject.AddComponent<AnimalSpawner>();
            gameObject.AddComponent<BlobShadows>();
            gameObject.AddComponent<Tips>();
            // wet sand: a dark film just above the sea, visible only along the waterline
            var sea = GameObject.Find("Sea"); var wet = Resources.Load<Material>("Materials/WetSand");
            if (sea != null && wet != null)
            {
                var film = Instantiate(sea, sea.transform.parent); film.name = "WetSand";
                film.transform.position = sea.transform.position + Vector3.up * 0.24f;
                film.GetComponent<Renderer>().sharedMaterial = wet;
            }`],
  ['            Vitals.Died += OnDied;', `            Vitals.Died += OnDied;
            // never leave the player stuck in a menu while something is biting them
            Vitals.Damaged += _ => { if (InvUI.Open) { InvUI.Close(); Hud.Hint("You are under attack!", 2f, true); } };`],
  ['            if (_fpsTime >= 5f) { Debug.Log("[FPS] " + (_fpsFrames / _fpsTime).ToString("0.0") + " quality=" + GameSettings.Quality + " " + Screen.width + "x" + Screen.height); _fpsFrames = 0; _fpsTime = 0; }',
   `            if (_fpsTime >= 5f)
            {
                float fps = _fpsFrames / _fpsTime; _fpsFrames = 0; _fpsTime = 0;
                Debug.Log("[FPS] " + fps.ToString("0.0") + " quality=" + GameSettings.Quality + " " + Screen.width + "x" + Screen.height);
                // a phone that cannot hold the chosen tier drops one step by itself, unless the player picked it by hand
                if (Application.isMobilePlatform && GameSettings.Quality > 0 && !GameSettings.QualityLocked && !MenuOpen)
                {
                    _slowWindows = fps < 27f ? _slowWindows + 1 : 0;
                    if (_slowWindows >= 3) { _slowWindows = 0; GameSettings.Quality--; GameSettings.Save(); Hud.Toast("Graphics lowered for smoother play"); }
                }
            }
            _exploreT -= Time.deltaTime;
            if (_exploreT <= 0)
            {
                _exploreT = 1f; Vector3 pp = Player.transform.position;
                for (int ox = -1; ox <= 1; ox++) for (int oz = -1; oz <= 1; oz++)
                    Flags.Add("map_" + Mathf.Clamp(Mathf.FloorToInt((pp.x + ox * 45f) / 100f), 0, 9) + "_" + Mathf.Clamp(Mathf.FloorToInt((pp.z + oz * 45f) / 100f), 0, 9));
            }`],
  ['        float _autosave, _fpsTime; int _fpsFrames;', '        float _autosave, _fpsTime, _exploreT; int _fpsFrames, _slowWindows;'],
  ['        public void Register(PlacedStructure s) { if (!_placed.Contains(s)) _placed.Add(s); }', `        public void Register(PlacedStructure s) { if (!_placed.Contains(s)) _placed.Add(s); }
        public int CountPlaced(string prefab) { int n = 0; foreach (var s in _placed) if (s != null && s.Prefab == prefab) n++; return n; }`],
]);
patch('Scripts/Game/BuildSystem.cs', [
  ['            gm.Quests.Notify("place", def.PlacePrefab);', '            gm.Quests.Notify("place", def.PlacePrefab);\n            gm.Register(s); Camp.Check();'],
]);
patch('Scripts/Game/SaveSystem.cs', [
  ['public float fuel, water; public bool lit; }', 'public float fuel, water; public bool lit; public ItemStack[] items; }'],
]);

// ---------------------------------------------------------------- items: storage chest, sorting
patch('Scripts/Game/Items.cs', [
  ['            d = Add("fence_kit", "Stake Fence",', '            d = Add("chest_kit", "Storage Chest", ItemKind.Placeable, "Chest", "Keeps eighteen stacks of supplies safe at camp.", 2); d.PlacePrefab = "StorageChest";\n            d = Add("fence_kit", "Stake Fence",'],
  ['            R("bed_kit", Station.None, "Camp",', '            R("chest_kit", Station.None, "Camp", ("log", 3), ("stick", 4), ("rope", 1));\n            R("bed_kit", Station.None, "Camp",'],
  ['        public void NotifyChanged() => Changed?.Invoke();', `        public void NotifyChanged() => Changed?.Invoke();

        /// <summary>Tidies the backpack (not the hotbar): merges split stacks and groups items by kind, then by name.</summary>
        public void Sort()
        {
            var list = new List<ItemStack>();
            for (int i = HotbarSize; i < Unlocked; i++)
            {
                var s = Slots[i]; if (s.Empty) continue;
                if (s.Def.Stack > 1)
                    foreach (var o in list)
                        if (o.id == s.id && o.count < s.Def.Stack && s.count > 0) { int n = Mathf.Min(s.count, s.Def.Stack - o.count); o.count += n; s.count -= n; }
                if (s.count > 0) list.Add(s);
            }
            list.Sort((a, b) => { int k = ((int)a.Def.Kind).CompareTo((int)b.Def.Kind); return k != 0 ? k : string.CompareOrdinal(a.Def.Name, b.Def.Name); });
            for (int i = HotbarSize; i < Unlocked; i++) Slots[i] = i - HotbarSize < list.Count ? list[i - HotbarSize] : new ItemStack();
            Changed?.Invoke();
        }`],
]);

// ---------------------------------------------------------------- pickups: one tap sweeps everything within reach
patch('Scripts/Game/Pickup.cs', [
  ['            if (left == Count) { GameManager.I.Hud.Hint("Your backpack is full."); return; }\n            AudioManager.I.Play("pickup", 0.9f, 0.12f);\n            Count = left;',
   `            if (left == Count)
            {
                if (!_sweeping) { GameManager.I.Hud.Hint("Your backpack is full."); Tips.Show("full", "Backpack full. Drop what you do not need, or build a Storage Chest at camp."); }
                return;
            }
            if (!_sweeping) AudioManager.I.Play("pickup", 0.9f, 0.12f);
            Count = left;
            if (!_sweeping)
            {
                _sweeping = true;
                foreach (var c in Physics.OverlapSphere(transform.position, 2.6f, 1 << World.InteractLayer, QueryTriggerInteraction.Collide))
                {
                    var other = c.GetComponent<Pickup>();
                    if (other != null && other != this && !other.Removed) other.Interact();
                }
                _sweeping = false;
            }`],
  ['        public bool Removed { get; private set; }', '        static bool _sweeping;\n        public bool Removed { get; private set; }'],
]);

// ---------------------------------------------------------------- landmarks hand out supplies
patch('Scripts/Game/Landmark.cs', [
  ['            gm.Hud.Banner(Title, "DISCOVERED  ·  " + gm.DiscoveredCount + " of " + All.Count + " places");',
   `            gm.Hud.Banner(Title, "DISCOVERED  ·  " + gm.DiscoveredCount + " of " + All.Count + " places  ·  supplies found");
            int pick = 0; foreach (char ch in Id) pick += ch;
            var reward = Rewards[pick % Rewards.Length];
            int left = gm.Inv.Add(reward.id, reward.n);
            if (left > 0) Pickup.Spawn(reward.id, left, gm.Player.transform.position + gm.Player.transform.forward * 1.2f + Vector3.up * 0.4f);`],
  ['        float _t;', `        float _t;
        static readonly (string id, int n)[] Rewards =
        {
            ("bandage", 2), ("rope", 2), ("arrow", 6), ("torch", 1), ("fish_cooked", 2), ("antidote", 1), ("flint", 3), ("coconut", 3),
        };`],
]);

// ---------------------------------------------------------------- resources: feedback and a heavier fall
patch('Scripts/Game/ResourceNode.cs', [
  ['        int _hp = -1; bool _removed, _falling;', '        int _hp = -1; bool _removed, _falling;\n        /// <summary>True once a tree or rock has been harvested away (bushes keep their body).</summary>\n        public bool Removed => _removed && !KeepCollider;'],
  ['            FX.Burst(point, Wood ? new Color(0.78f, 0.6f, 0.38f) : new Color(0.62f, 0.63f, 0.65f), 9);',
   '            FX.Burst(point, Wood ? new Color(0.78f, 0.6f, 0.38f) : new Color(0.62f, 0.63f, 0.65f), 16, 0.13f, 3.4f);\n            GameManager.I.Hud.NodeBar(Mathf.Max(0, _hp) / (float)MaxHp);'],
  ['            FX.Burst(transform.position + away.normalized * 3f + Vector3.up * 0.4f, new Color(0.3f, 0.6f, 0.28f), 26, 0.2f, 3.5f);\n            GameManager.I.Player.Shake(0.5f);',
   `            // the crown hits the ground: leaves and dust along the trunk, then a small bounce
            for (int i = 1; i <= 4; i++)
                FX.Burst(transform.position + away.normalized * (i * 1.5f) + Vector3.up * 0.4f, i < 3 ? new Color(0.72f, 0.64f, 0.46f) : new Color(0.3f, 0.6f, 0.28f), 14, 0.2f, 3.5f);
            GameManager.I.Player.Shake(0.7f); Haptics.Pulse(80);
            float bounce = 0;
            while (bounce < 1f) { bounce += Time.deltaTime / 0.32f; t.rotation = Quaternion.AngleAxis(86f - Mathf.Sin(bounce * Mathf.PI) * 5f, axis) * transform.rotation * r0; yield return null; }`],
]);

// ---------------------------------------------------------------- player: splashes in the water
patch('Scripts/Game/PlayerController.cs', [
  ['            if (inWater && !_wasInWater && _vy < -2f) AudioManager.I.Play("splash", 0.7f);',
   `            if (inWater && !_wasInWater)
            {
                if (_vy < -2f) AudioManager.I.Play("splash", 0.7f);
                FX.Burst(new Vector3(transform.position.x, World.SeaLevel + 0.05f, transform.position.z), new Color(0.9f, 0.96f, 1f, 0.85f), 18, 0.1f, 3.2f);
            }`],
  ['                if (_stepDist > (Sprinting ? 2.5f : 2.0f) && !Swimming)\n                {\n                    _stepDist = 0;',
   `                if (_stepDist > 2.2f && Swimming)
                {
                    _stepDist = 0;
                    AudioManager.I.Play("splash", 0.14f, 0.2f);
                    FX.Burst(new Vector3(transform.position.x, World.SeaLevel + 0.05f, transform.position.z) + transform.forward * 0.5f, new Color(0.9f, 0.96f, 1f, 0.8f), 7, 0.08f, 2f);
                }
                if (_stepDist > (Sprinting ? 2.5f : 2.0f) && !Swimming)
                {
                    _stepDist = 0;
                    if (depth > 0.05f) FX.Burst(new Vector3(transform.position.x, World.SeaLevel + 0.05f, transform.position.z), new Color(0.9f, 0.96f, 1f, 0.8f), 8, 0.08f, 2.4f);`],
]);

// ---------------------------------------------------------------- interaction: hit feel, camp perk, fishing mini-game
patch('Scripts/Game/PlayerInteraction.cs', [
  ['_torchFire = FX.Fire(_held.transform, new Vector3(0, 0.5f, 0), 0.35f, false, 16f);', '_torchFire = FX.Fire(_held.transform, new Vector3(0, 0.5f, 0), 0.35f, false, 21f);'],
  ['            if (s.Empty || s.Def.Durability <= 0) return;\n            s.durability -= amount;', '            if (s.Empty || s.Def.Durability <= 0 || Camp.SaveWear) return;\n            s.durability -= amount;'],
  ['            _pc.Shake(0.25f);\n            if (tool != null) Wear(1);', '            _pc.Shake(0.3f); Haptics.Pulse(18);\n            Tween.Punch(_pc.Hand, 0.1f, 0.16f);\n            if (tool != null) Wear(1);'],
  ['        int _fish; float _fishTimer; Vector3 _fishPos, _fishStart; GameObject _bobber;', '        int _fish; float _fishTimer, _reel, _reelSpeed, _zone; Vector3 _fishPos, _fishStart; GameObject _bobber;\n        const float ZoneWidth = 0.24f;'],
  ['            if (_fish == 2) prompt = null;', '            if (_fish >= 2) prompt = null;'],
  ['                float dip = _fish == 2 ? Mathf.Sin', '                float dip = _fish >= 2 ? Mathf.Sin'],
  ['                GameSettings.Vibrate();\n            }\n            else if (_fish == 2 && _fishTimer <= 0)\n            {\n                GameManager.I.Hud.Hint("It got away.");\n                CancelFishing();\n            }',
   `                Haptics.Pulse(60);
            }
            else if (_fish == 2 && _fishTimer <= 0)
            {
                GameManager.I.Hud.Hint("It got away.");
                CancelFishing();
            }
            else if (_fish == 3)
            {
                // reeling: a marker sweeps the bar, land it inside the green zone
                _reel += Time.deltaTime * _reelSpeed;
                GameManager.I.Hud.FishBar(true, Mathf.PingPong(_reel, 1f), _zone, ZoneWidth);
                if (_fishTimer <= 0) { GameManager.I.Hud.Hint("The line went slack."); CancelFishing(); }
            }`],
  [`            if (_fish == 2)
            {
                _inv.Add("fish_raw", 1);
                AudioManager.I.Play("splash", 0.6f);
                FX.Burst(_fishPos, new Color(0.8f, 0.92f, 1f, 0.8f), 12, 0.09f, 3f);
                Wear(1);
            }
            else GameManager.I.Hud.Hint("Reeled in too early.");
            CancelFishing();`,
   `            if (_fish == 2)
            {
                // hooked: now reel it in
                _fish = 3; _fishTimer = 6f; _reel = UnityEngine.Random.value; _reelSpeed = UnityEngine.Random.Range(0.8f, 1.15f); _zone = UnityEngine.Random.Range(0.2f, 0.8f);
                GameManager.I.Hud.Hint(GameInput.TouchMode ? "Hooked!  Tap USE when the marker is in the green" : "Hooked!  Click when the marker is in the green", 3f, true);
                Haptics.Pulse(25);
                return;
            }
            if (_fish == 3)
            {
                float miss = Mathf.Abs(Mathf.PingPong(_reel, 1f) - _zone);
                if (miss <= ZoneWidth * 0.5f)
                {
                    bool perfect = miss < 0.04f;
                    _inv.Add("fish_raw", perfect ? 2 : 1);
                    if (perfect) GameManager.I.Hud.Hint("Perfect catch!  Two fish", 2f, true);
                    AudioManager.I.Play("splash", 0.6f); Haptics.Pulse(40);
                    FX.Burst(_fishPos, new Color(0.8f, 0.92f, 1f, 0.8f), 14, 0.09f, 3f);
                    Wear(1);
                }
                else GameManager.I.Hud.Hint("It slipped the hook.");
            }
            else GameManager.I.Hud.Hint("Reeled in too early.");
            CancelFishing();`],
  ['            _fish = 0;\n            if (_bobber != null) Destroy(_bobber);', '            _fish = 0;\n            if (_bobber != null) Destroy(_bobber);\n            if (GameManager.I != null && GameManager.I.Hud != null) GameManager.I.Hud.FishBar(false, 0, 0, 0);'],
]);

// ---------------------------------------------------------------- intro: readable on phones with no post-processing
patch('Scripts/UI/IntroCinematic.cs', [
  ['            if (_light != null) _light.intensity = 0.22f + flicker * 3.2f;\n            RenderSettings.ambientLight = new Color(0.05f, 0.065f, 0.09f) + Color.white * flicker * 0.3f;\n            RenderSettings.fogColor = new Color(0.035f, 0.05f, 0.07f) + new Color(0.5f, 0.55f, 0.65f) * flicker * 0.5f;',
   `            // without HDR and bloom the storm reads as plain black, so those tiers get more base light
            float lift = GameSettings.Quality == 0 ? 3.2f : GameSettings.Quality == 1 && Application.isMobilePlatform ? 1.8f : 1f;
            if (_light != null) _light.intensity = 0.22f * lift + flicker * 3.2f;
            RenderSettings.ambientLight = new Color(0.05f, 0.065f, 0.09f) * lift + Color.white * flicker * 0.3f;
            RenderSettings.fogColor = new Color(0.035f, 0.05f, 0.07f) * Mathf.Sqrt(lift) + new Color(0.5f, 0.55f, 0.65f) * flicker * 0.5f;`],
]);

console.log(results.join('\n'));
