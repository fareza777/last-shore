// v4 (part b): interface. Tip card, resource bar, damage direction, threat marker, fishing bar, buffs,
// storage chest panel, sort button, camp line in the journal, explored-area fog on the map, editor extras.
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

// ---------------------------------------------------------------- HUD
patch('Scripts/UI/HUD.cs', [
  ['            // ---- toasts, hint, banner, subtitles', `            // ---- tip card (one-time hints)
            var tip = UI.Img(_safe, UI.Rounded, UI.Bg, "Tip"); _tipRt = tip.rectTransform;
            _tipRt.Pin(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -132), new Vector2(900, 62)); _tipG = tip.Group(); _tipG.alpha = 0; _tipG.blocksRaycasts = false;
            var tipTag = UI.Img(tip.transform, UI.RoundedSmall, UI.Accent, "Tag"); tipTag.rectTransform.Pin(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(62, 34));
            UI.Label(tipTag.transform, "TIP", 18, UI.Hex("1A1206"), TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Stretch();
            _tipText = UI.Label(tip.transform, "", 23, UI.TextCol, TextAnchor.MiddleLeft); _tipText.rectTransform.Stretch(90, 4, 18, 4); _tipText.horizontalOverflow = HorizontalWrapMode.Overflow;

            // ---- how much is left of the tree or rock being worked
            _nodeFill = UI.Bar(_safe, UI.Accent, out var nodeRt, "NodeBar");
            nodeRt.Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -50), new Vector2(160, 14)); _nodeG = nodeRt.Group(); _nodeG.alpha = 0;

            // ---- where a hit came from, and where the nearest hostile animal is
            _dmgRt = UI.Rect("DamageDir", _safe).Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10, 10));
            var arrow = UI.Img(_dmgRt, UI.Icon("marker"), UI.Danger, "Arrow"); arrow.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 215), new Vector2(58, 50));
            _dmgG = _dmgRt.Group(); _dmgG.alpha = 0;
            var th = UI.Img(_safe, UI.Icon("marker"), UI.Danger, "Threat"); _threat = th.rectTransform; _threat.Pin(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -78), new Vector2(26, 23));
            th.gameObject.SetActive(false);

            // ---- fishing: land the marker inside the green zone
            var fb = UI.Img(_safe, UI.Pill, UI.Bg, "FishBar"); var fbRt = fb.rectTransform;
            fbRt.Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -140), new Vector2(FishW + 20, 30)); _fishG = fb.Group(); _fishG.alpha = 0;
            _fishZone = UI.Img(fb.transform, UI.Pill, UI.Good, "Zone").rectTransform; _fishCursor = UI.Img(fb.transform, UI.Pill, UI.TextCol, "Cursor").rectTransform;

            // ---- toasts, hint, banner, subtitles`],
  ['            gm.Vitals.Damaged += amount => { _hurtA = Mathf.Clamp01(_hurtA + amount / 22f + 0.25f); };',
   '            gm.Vitals.Damaged += amount => { _hurtA = Mathf.Clamp01(_hurtA + amount / 22f + 0.25f); };\n            gm.Vitals.HitFrom += pos => { _dmgPos = pos; _dmgA = 1f; };'],
  ['        RectTransform _hotbar;', `        RectTransform _hotbar, _tipRt, _dmgRt, _threat, _fishZone, _fishCursor;
        CanvasGroup _tipG, _nodeG, _dmgG, _fishG; Text _tipText; Image _nodeFill; Coroutine _nodeC;
        readonly Queue<string> _tips = new Queue<string>(); float _tipTimer, _dmgA; Vector3 _dmgPos; Transform _threatT;
        const float FishW = 440f;

        /// <summary>Queues a one-time hint card under the compass.</summary>
        public void Tip(string text) { _tips.Enqueue(text); }

        /// <summary>Shows how much of a tree or rock is left (1 = untouched, 0 = done).</summary>
        public void NodeBar(float fraction)
        {
            _nodeFill.fillAmount = fraction;
            Tween.Stop(_nodeC); _nodeG.alpha = 1;
            _nodeC = Tween.Fade(_nodeG, 0, 0.4f, 1.3f);
        }

        /// <summary>Fishing reel bar: cursor and zone are 0..1 along the bar, width is the zone's share of it.</summary>
        public void FishBar(bool show, float cursor, float zone, float width)
        {
            _fishG.alpha = show ? 1 : 0;
            if (!show) return;
            _fishZone.Pin(new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(10 + zone * FishW, 0), new Vector2(width * FishW, 20));
            _fishCursor.Pin(new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(10 + cursor * FishW, 0), new Vector2(9, 40));
        }

        void UpdateFeedback(GameManager gm, float yaw, float half, float dt)
        {
            // tip queue: one card at a time
            _tipTimer -= dt;
            if (_tipTimer <= 0 && _tips.Count > 0 && !gm.Cinematic)
            {
                _tipText.text = _tips.Dequeue(); _tipTimer = 7.5f;
                _tipRt.sizeDelta = new Vector2(Mathf.Clamp(_tipText.preferredWidth + 124, 320, 1500), 62);
                Tween.SlideIn(_tipRt, _tipG, new Vector2(0, 30), 0.45f);
                Tween.Fade(_tipG, 0, 0.6f, 6.6f);
                AudioManager.I.UI("ui_open", 0.35f);
            }

            // hit direction: the arrow sits on the side the blow came from
            _dmgA = Mathf.MoveTowards(_dmgA, 0, dt * 0.55f); _dmgG.alpha = Mathf.Clamp01(_dmgA * 1.6f);
            if (_dmgA > 0)
            {
                Vector3 to = _dmgPos - gm.Player.transform.position;
                _dmgRt.localRotation = Quaternion.Euler(0, 0, -Mathf.DeltaAngle(yaw, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg));
            }

            // nearest animal that is hunting the player, shown on the compass
            bool threat = _threatT != null;
            if (_threat.gameObject.activeSelf != threat) _threat.gameObject.SetActive(threat);
            if (threat)
            {
                Vector3 to = _threatT.position - gm.Player.transform.position;
                float d = Mathf.DeltaAngle(yaw, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg);
                _threat.anchoredPosition = new Vector2(Mathf.Clamp(d / 100f, -1f, 1f) * half, -78);
                _threat.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 9f) * 0.18f);
            }
        }

        void FindThreat(GameManager gm)
        {
            _threatT = null; float best = 45f * 45f; Vector3 p = gm.Player.transform.position;
            foreach (var a in Animal.All)
            {
                if (!a.Engaged || !a.Hostile) continue;
                float d = (a.transform.position - p).sqrMagnitude;
                if (d < best) { best = d; _threatT = a.transform; }
            }
        }`],
  ['            _slow -= dt;\n            if (_slow <= 0)\n            {\n                _slow = 0.5f;', '            UpdateFeedback(gm, yaw, half, dt);\n\n            _slow -= dt;\n            if (_slow <= 0)\n            {\n                _slow = 0.5f; FindThreat(gm);'],
  ['                if (v.PoisonTime > 0) s += "POISONED   ";', '                if (v.PoisonTime > 0) s += "POISONED   ";\n                if (v.Hearty > 0) s += "HEARTY   "; else if (v.WellFed > 0) s += "WELL FED   ";'],
]);

// animals that can hurt the player say so, for the threat marker
patch('Scripts/Game/Animals.cs', [
  ['        public bool Engaged { get; protected set; }      // busy with the player: do not despawn', '        public bool Engaged { get; protected set; }      // busy with the player: do not despawn\n        public virtual bool Hostile => false;             // true for animals that attack'],
]);
for (const f of ['Boar.cs', 'Shark.cs', 'Snake.cs']) {
  let s = fs.readFileSync(root + 'Scripts/Game/' + f, 'utf8').replace(/\r\n/g, '\n');
  const m = s.match(/    public class \w+ : Animal\n    \{\n/);
  if (m && !s.includes('override bool Hostile')) { s = s.replace(m[0], m[0] + '        public override bool Hostile => true;\n'); results.push('ok   ' + f + ': Hostile'); }
  else results.push('MISS ' + f + ': Hostile');
  fs.writeFileSync(root + 'Scripts/Game/' + f, s);
}

// ---------------------------------------------------------------- backpack: chest panel, sort, camp line
patch('Scripts/UI/InventoryUI.cs', [
  ['        float _refresh; RectTransform _mapPanel; MapView _mapView; Image _tabMap;', `        float _refresh; RectTransform _mapPanel; MapView _mapView; Image _tabMap;
        StorageChest _chest; int _chestSel = -1; RectTransform _chestPanel; Text _jCamp;
        readonly Image[] _cBg = new Image[StorageChest.Size], _cIcon = new Image[StorageChest.Size]; readonly Text[] _cCount = new Text[StorageChest.Size];`],
  ['            Header(left.transform, "BACKPACK", "bag");', `            Header(left.transform, "BACKPACK", "bag");
            var sort = UI.Btn(left.transform, "SORT", SortPack, 22); ((RectTransform)sort.transform).Pin(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-38, -20), new Vector2(124, 52));`],
  ['            _mapPanel = UI.Rect("Map", right.transform)', `            _chestPanel = UI.Rect("Chest", right.transform).Stretch(26, 24, 26, 96);
            var chTitle = UI.Label(_chestPanel, UI.Spaced("STORAGE CHEST"), 20, UI.Accent, TextAnchor.UpperLeft, FontStyle.Bold); chTitle.rectTransform.Anchor(0, 1, 1, 1, new Vector2(0, -30), Vector2.zero);
            var cgrid = UI.Rect("Grid", _chestPanel).Pin(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -48), new Vector2(690, 310));
            for (int i = 0; i < StorageChest.Size; i++)
            {
                int idx = i; int col = i % 6, row = i / 6;
                var bg = UI.Img(cgrid, UI.Rounded, UI.Slot, "Chest" + i, true);
                bg.rectTransform.Pin(new Vector2(0, 1), new Vector2(0, 1), new Vector2(col * 116, -row * 101), new Vector2(106, 93));
                var b = bg.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None; b.onClick.AddListener(() => ClickChest(idx));
                var ic = UI.Img(bg.transform, null, Color.white, "Icon"); ic.rectTransform.Stretch(12, 12, 12, 12); ic.preserveAspect = true;
                var ct = UI.Label(bg.transform, "", 22, UI.TextCol, TextAnchor.LowerRight, FontStyle.Bold).Shadowed(); ct.rectTransform.Stretch(0, 6, 10, 0);
                _cBg[i] = bg; _cIcon[i] = ic; _cCount[i] = ct;
            }
            var storeAll = UI.Btn(_chestPanel, "STORE ALL", StoreAll, 24, true); ((RectTransform)storeAll.transform).Pin(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -372), new Vector2(240, 62));
            var takeAll = UI.Btn(_chestPanel, "TAKE ALL", TakeAll, 24); ((RectTransform)takeAll.transform).Pin(new Vector2(0, 1), new Vector2(0, 1), new Vector2(256, -372), new Vector2(240, 62));
            var chNote = UI.Label(_chestPanel, "Tap an item, then tap a slot in the other grid to move it.\\nSTORE ALL keeps your hotbar and moves everything else.", 21, UI.Dim, TextAnchor.UpperLeft);
            chNote.rectTransform.Anchor(0, 1, 1, 1, new Vector2(0, -530), new Vector2(0, -456)); chNote.lineSpacing = 1.2f;
            _chestPanel.gameObject.SetActive(false);

            _mapPanel = UI.Rect("Map", right.transform)`],
  ['nh.rectTransform.Anchor(0, 1, 1, 1, new Vector2(0, -160), new Vector2(0, -130));', `nh.rectTransform.Anchor(0, 1, 1, 1, new Vector2(0, -214), new Vector2(0, -184));
            _jCamp = UI.Label(_journalPanel, "", 19, UI.Dim, TextAnchor.UpperLeft); _jCamp.rectTransform.Anchor(0, 1, 1, 1, new Vector2(0, -176), new Vector2(0, -118)); _jCamp.lineSpacing = 1.15f;`],
  ['new Vector2((i % 3) * 232, -170 - (i / 3) * 62)', 'new Vector2((i % 3) * 232, -224 - (i / 3) * 62)'],
  ['paper.rectTransform.Anchor(0, 0, 1, 1, new Vector2(0, 0), new Vector2(0, -306));', 'paper.rectTransform.Anchor(0, 0, 1, 1, new Vector2(0, 0), new Vector2(0, -360));'],
  ['        public void OpenCrafting() { Show(true); }', `        public void OpenCrafting() { Show(true); }

        /// <summary>Opens the backpack next to a storage chest.</summary>
        public void OpenChest(StorageChest chest)
        {
            Show(true);
            _chest = chest; _chestSel = -1; _selected = -1;
            ShowTabIndex(3); Refresh();
        }`],
  ['            Open = false; _selected = -1; GameManager.I.Hud.SetHotbarVisible(true);', '            Open = false; _selected = -1; _chest = null; _chestSel = -1; GameManager.I.Hud.SetHotbarVisible(true);'],
  ['            _craftPanel.gameObject.SetActive(tab == 0); _journalPanel.gameObject.SetActive(tab == 1); _mapPanel.gameObject.SetActive(tab == 2);',
   '            _craftPanel.gameObject.SetActive(tab == 0); _journalPanel.gameObject.SetActive(tab == 1); _mapPanel.gameObject.SetActive(tab == 2);\n            _chestPanel.gameObject.SetActive(tab == 3); if (tab != 3) { _chest = null; _chestSel = -1; }'],
  ['            if (i >= inv.Unlocked) { GameManager.I.Hud.Hint("Craft a Hide Pack to carry more."); return; }',
   `            if (i >= inv.Unlocked) { GameManager.I.Hud.Hint("Craft a Hide Pack to carry more."); return; }
            if (_chest != null && _chestSel >= 0)
            {
                Exchange(_chest.Slots, _chestSel, inv.Slots, i); _chestSel = -1;
                AudioManager.I.UI("pickup", 0.4f); inv.NotifyChanged(); Refresh(); return;
            }`],
  ['        void UseSelected()', `        // ---------------------------------------------------------------- storage chest
        /// <summary>Moves a stack between two slots: same items merge, different items trade places.</summary>
        static void Exchange(ItemStack[] from, int a, ItemStack[] to, int b)
        {
            if (from == to && a == b) return;
            var sa = from[a]; var sb = to[b];
            if (!sa.Empty && !sb.Empty && sa.id == sb.id && sa.Def.Stack > 1)
            {
                int n = Mathf.Min(sa.count, sb.Def.Stack - sb.count);
                sb.count += n; sa.count -= n; if (sa.count <= 0) sa.Clear();
            }
            else { from[a] = sb; to[b] = sa; }
        }

        /// <summary>Puts a stack into a container, topping up matching stacks first. Whatever does not fit stays behind.</summary>
        static void Deposit(ItemStack s, ItemStack[] dest, int start, int end)
        {
            if (s.Def.Stack > 1)
                for (int i = start; i < end && s.count > 0; i++)
                {
                    var d = dest[i];
                    if (!d.Empty && d.id == s.id && d.count < s.Def.Stack) { int n = Mathf.Min(s.count, s.Def.Stack - d.count); d.count += n; s.count -= n; }
                }
            for (int i = start; i < end && s.count > 0; i++)
                if (dest[i].Empty) { dest[i].id = s.id; dest[i].count = s.count; dest[i].durability = s.durability; s.count = 0; }
            if (s.count <= 0) s.Clear();
        }

        void ClickChest(int i)
        {
            if (_chest == null) return;
            var inv = GameManager.I.Inv;
            if (_selected >= 0 && !inv.Slots[_selected].Empty) { Exchange(inv.Slots, _selected, _chest.Slots, i); _selected = -1; AudioManager.I.UI("pickup", 0.4f); inv.NotifyChanged(); }
            else if (_chestSel == i) _chestSel = -1;
            else if (_chestSel >= 0 && !_chest.Slots[_chestSel].Empty) { Exchange(_chest.Slots, _chestSel, _chest.Slots, i); _chestSel = -1; AudioManager.I.UI("pickup", 0.4f); }
            else _chestSel = _chest.Slots[i].Empty ? -1 : i;
            Refresh();
        }

        void StoreAll()
        {
            if (_chest == null) return;
            var inv = GameManager.I.Inv;
            for (int i = Inventory.HotbarSize; i < inv.Unlocked; i++) if (!inv.Slots[i].Empty) Deposit(inv.Slots[i], _chest.Slots, 0, StorageChest.Size);
            _selected = -1; _chestSel = -1; AudioManager.I.UI("pickup", 0.5f); inv.NotifyChanged(); Refresh();
        }

        void TakeAll()
        {
            if (_chest == null) return;
            var inv = GameManager.I.Inv;
            foreach (var s in _chest.Slots) if (!s.Empty) Deposit(s, inv.Slots, Inventory.HotbarSize, inv.Unlocked);
            foreach (var s in _chest.Slots) if (!s.Empty) Deposit(s, inv.Slots, 0, Inventory.HotbarSize);
            _selected = -1; _chestSel = -1; AudioManager.I.UI("pickup", 0.5f); inv.NotifyChanged(); Refresh();
        }

        void SortPack() { _selected = -1; AudioManager.I.UI("pickup", 0.4f); GameManager.I.Inv.Sort(); Refresh(); }

        void UseSelected()`],
  ['            var sel = _selected >= 0 ? inv.Slots[_selected] : null;', `            if (_chest != null)
                for (int i = 0; i < StorageChest.Size; i++)
                {
                    var s = _chest.Slots[i];
                    _cIcon[i].enabled = !s.Empty; if (!s.Empty) _cIcon[i].sprite = s.Def.Icon;
                    _cCount[i].text = !s.Empty && s.count > 1 ? s.count.ToString() : !s.Empty && s.Def.Durability > 0 ? Mathf.CeilToInt(100f * s.durability / s.Def.Durability) + "%" : "";
                    _cBg[i].color = i == _chestSel ? UI.Hex("5A4320", 0.98f) : UI.Slot;
                }
            var sel = _selected >= 0 ? inv.Slots[_selected] : null;`],
  ['            _jBody.text = gm.Quests.Body;', '            _jBody.text = gm.Quests.Body;\n            _jCamp.text = Camp.Summary + "\\nPLACES  ·  " + gm.DiscoveredCount + " of " + Landmark.All.Count + " discovered";'],
]);

// ---------------------------------------------------------------- map: unexplored squares stay covered
patch('Scripts/UI/MapUI.cs', [
  ['        RectTransform _map; Image _player; Text _info; GameObject _locked;', '        RectTransform _map; Image _player; Text _info; GameObject _locked;\n        readonly GameObject[,] _fog = new GameObject[10, 10];'],
  ['            v._player = UI.Img(v._map, UI.Icon("marker"), UI.WaterCol, "Player"); v._player.rectTransform.sizeDelta = new Vector2(24, 26);',
   `            for (int z = 0; z < 10; z++)
                for (int x = 0; x < 10; x++)
                {
                    var f = UI.Img(v._map, null, new Color(0.035f, 0.08f, 0.11f, 0.94f), "Fog");
                    f.rectTransform.anchorMin = new Vector2(x / 10f, z / 10f); f.rectTransform.anchorMax = new Vector2((x + 1) / 10f, (z + 1) / 10f);
                    f.rectTransform.offsetMin = new Vector2(-0.5f, -0.5f); f.rectTransform.offsetMax = new Vector2(0.5f, 0.5f);
                    v._fog[x, z] = f.gameObject;
                }
            v._player = UI.Img(v._map, UI.Icon("marker"), UI.WaterCol, "Player"); v._player.rectTransform.sizeDelta = new Vector2(24, 26);`],
  ['            _info.text = UI.Spaced("PLACES DISCOVERED  " + gm.DiscoveredCount + " / " + Landmark.All.Count);',
   `            int seen = 0;
            for (int z = 0; z < 10; z++)
                for (int x = 0; x < 10; x++)
                {
                    bool open = gm.Flags.Contains("map_" + x + "_" + z); if (open) seen++;
                    _fog[x, z].SetActive(!open);
                }
            _info.text = UI.Spaced("PLACES  " + gm.DiscoveredCount + " / " + Landmark.All.Count + "     EXPLORED  " + seen + "%");`],
]);

// ---------------------------------------------------------------- editor: materials and the chest prefab
patch('Editor/ProjectSetup.cs', [
  ['        public static void RefreshWater()', `        /// <summary>Assets added after the first full setup: sky, blob shadow and wet sand materials, the storage chest prefab.</summary>
        public static void BuildExtras()
        {
            AssetDatabase.Refresh();
            var noise = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/WaterNormal.png");
            MakeMat(MatDir, "SkyGame", "LastShore/Sky", m => m.SetTexture("_NoiseTex", noise));
            MakeMat(MatDir, "Blob", "LastShore/Blob", m => m.enableInstancing = true);
            MakeMat(MatDir, "WetSand", "LastShore/WetSand", m => { m.SetFloat("_Strength", 0.34f); m.SetFloat("_Height", 0.24f); });
            var go = new GameObject("StorageChest"); Spawn("Chest", go.transform); SetLayer(go, 11);
            var b = go.AddComponent<BoxCollider>(); b.size = new Vector3(0.95f, 0.62f, 0.6f); b.center = new Vector3(0, 0.31f, 0);
            go.AddComponent<StorageChest>().Prefab = "StorageChest"; SavePrefab(go, "StorageChest");
            AssetDatabase.SaveAssets();
            Debug.Log("[LastShore] Extras built.");
        }

        public static void RefreshWater()`],
]);
patch('Editor/BuildTools.cs', [
  ['public static void RefreshAndBuild() { ProjectSetup.RenderIcons(); ProjectSetup.RefreshWater(); BuildAndroid(); }', 'public static void RefreshAndBuild() { ProjectSetup.BuildExtras(); ProjectSetup.RenderIcons(); ProjectSetup.RefreshWater(); BuildAndroid(); }'],
]);

console.log(results.join('\n'));
