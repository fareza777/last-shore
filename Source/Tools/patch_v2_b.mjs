// v2 UI patches: HUD (minimap, event marker, poison), inventory (6 rows, MAP tab), loading screen, touch controls.
import fs from 'node:fs';
const dir = 'C:/Survival Fable 62/Assets/Scripts/';
const results = [];
function patch(file, edits) {
  let s = fs.readFileSync(dir + file, 'utf8').replace(/\r\n/g, '\n');
  for (const [from, to] of edits) {
    const ok = s.includes(from);
    results.push((ok ? 'ok   ' : 'MISS ') + file + ': ' + from.slice(0, 56).replace(/\n/g, ' '));
    if (ok) s = s.replace(from, to);
  }
  fs.writeFileSync(dir + file, s);
}

// ---------------------------------------------------------------- MapUI fixes (lazy landmark lists, north tick)
patch('UI/MapUI.cs', [
  ['            foreach (var l in Landmark.All) m._dots.Add((l, Dot(m._map, UI.TextCol, 10)));\n', ''],
  ['            if (north != null) { float r = yaw * Mathf.Deg2Rad; north.anchoredPosition = new Vector2(-Mathf.Sin(-r), Mathf.Cos(-r)) * (SizePx * 0.5f + 2f) * 1f; north.anchoredPosition = new Vector2(Mathf.Sin(-r) * -1f, Mathf.Cos(r)) * (SizePx * 0.5f + 2f); }',
   '            if (north != null) { float r = yaw * Mathf.Deg2Rad; north.anchoredPosition = new Vector2(-Mathf.Sin(r), Mathf.Cos(r)) * (SizePx * 0.5f + 4f); }'],
  ['            _t -= Time.unscaledDeltaTime; if (_t > 0) return; _t = 0.6f;\n', '            _t -= Time.unscaledDeltaTime; if (_t > 0) return; _t = 0.6f;\n            if (_dots.Count != Landmark.All.Count) { foreach (var d in _dots) if (d.dot != null) Destroy(d.dot.gameObject); _dots.Clear(); foreach (var l in Landmark.All) _dots.Add((l, Dot(_map, UI.TextCol, 10))); }\n'],
  ['            foreach (var l in Landmark.All)\n            {\n                var dot = UI.Img(v._map, UI.Disc, UI.Accent, "Dot");', '            return v;\n        }\n\n        void BuildMarks()\n        {\n            var v = this;\n            foreach (var l in Landmark.All)\n            {\n                var dot = UI.Img(v._map, UI.Disc, UI.Accent, "Dot");'],
  ['                v._marks.Add((l, dot, label));\n            }\n            v._player = UI.Img(v._map', '                v._marks.Add((l, dot, label));\n            }\n            _player.transform.SetAsLastSibling();\n        }\n\n        static void Unused(MapView v)\n        {\n            v._player = UI.Img(v._map'],
  ['            msg.rectTransform.Stretch(30, 30, 30, 30); msg.lineSpacing = 1.2f;\n            return v;', '            msg.rectTransform.Stretch(30, 30, 30, 30); msg.lineSpacing = 1.2f;'],
  ['            var gm = GameManager.I; bool has = gm.Inv.Count("compass") > 0;\n            _locked.SetActive(!has);', '            var gm = GameManager.I; bool has = gm.Inv.Count("compass") > 0;\n            if (_marks.Count != Landmark.All.Count) BuildMarks();\n            _locked.SetActive(!has);'],
]);
// the player arrow and the locked panel must be created inside Build (before the early return added above)
{
  let s = fs.readFileSync(dir + 'UI/MapUI.cs', 'utf8');
  const a = s.indexOf('        static void Unused(MapView v)\n        {\n'); const b = s.indexOf('        public void Refresh()');
  if (a > 0 && b > a) {
    const body = s.slice(a + '        static void Unused(MapView v)\n        {\n'.length, s.lastIndexOf('        }', b));
    s = s.slice(0, a) + s.slice(b);
    s = s.replace('            return v;\n        }\n\n        void BuildMarks()', body + '            return v;\n        }\n\n        void BuildMarks()');
    fs.writeFileSync(dir + 'UI/MapUI.cs', s); results.push('ok   MapUI restructure');
  } else results.push('MISS MapUI restructure');
}

// ---------------------------------------------------------------- HUD
patch('UI/HUD.cs', [
  ['        float _slow;', '        float _slow; RectTransform _evMarker;'],
  ['            _hp = Vital(vit, "heart", UI.Danger, 4, 300, 20);', '            var vbg = UI.Img(vit, UI.Rounded, new Color(0.03f, 0.06f, 0.09f, 0.42f), "Bg"); vbg.rectTransform.Stretch(-14, -12, 6, 16);\n            _hp = Vital(vit, "heart", UI.Danger, 4, 300, 20);'],
  ['            // ---- clock', '            var ev = UI.Img(_safe, UI.Icon("marker"), UI.WaterCol, "EventMarker"); _evMarker = ev.rectTransform; _evMarker.Pin(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -78), new Vector2(18, 16));\n            Minimap.Create(_safe);\n\n            // ---- clock'],
  ['            _slow -= dt;\n            if (_slow <= 0)', '            var em = gm.Events != null ? gm.Events.Marker : null;\n            _evMarker.gameObject.SetActive(em != null);\n            if (em != null)\n            {\n                Vector3 to = em.position - gm.Player.transform.position;\n                float d = Mathf.DeltaAngle(yaw, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg);\n                _evMarker.anchoredPosition = new Vector2(Mathf.Clamp(d / 100f, -1f, 1f) * half, -78);\n            }\n\n            _slow -= dt;\n            if (_slow <= 0)'],
  ['                if (v.Wet) s += "WET   ";', '                if (v.PoisonTime > 0) s += "POISONED   ";\n                if (v.Wet) s += "WET   ";'],
  ['                _status.color = v.Warmth < 30 ||', '                _status.color = v.PoisonTime > 0 || v.Warmth < 30 ||'],
]);

// ---------------------------------------------------------------- Inventory
patch('UI/InventoryUI.cs', [
  ['        float _refresh;', '        float _refresh; RectTransform _mapPanel; MapView _mapView; Image _tabMap;'],
  ['new Vector2(col * 120, -row * 120 - (row > 0 ? 16 : 0)), new Vector2(110, 110));', 'new Vector2(col * 120, -row * 101 - (row > 0 ? 12 : 0)), new Vector2(110, 93));'],
  ['new Vector2(0, -119), new Vector2(710, 2));', 'new Vector2(0, -99), new Vector2(710, 2));'],
  ['            _tabJournal = Tab(right.transform, "JOURNAL", 1, () => ShowTab(false));', '            _tabJournal = Tab(right.transform, "JOURNAL", 1, () => ShowTab(false));\n            _tabMap = Tab(right.transform, "MAP", 2, () => ShowTabIndex(2));'],
  ['            _journalPanel = UI.Rect("Journal", right.transform).Stretch(30, 24, 30, 96);', '            _mapPanel = UI.Rect("Map", right.transform).Stretch(26, 24, 26, 96); _mapView = MapView.Build(_mapPanel);\n            _journalPanel = UI.Rect("Journal", right.transform).Stretch(30, 24, 30, 96);'],
  ['new Vector2(26 + index * 232, -18), new Vector2(222, 58));\n            Destroy(b.GetComponent<ButtonFX>());', 'new Vector2(26 + index * 196, -18), new Vector2(188, 58));\n            Destroy(b.GetComponent<ButtonFX>());'],
  ['        void ShowTab(bool craft)\n        {\n            _craftPanel.gameObject.SetActive(craft); _journalPanel.gameObject.SetActive(!craft);\n            _tabCraft.color = craft ? UI.Hex("3A2C14", 0.95f) : UI.BgSoft; _tabJournal.color = craft ? UI.BgSoft : UI.Hex("3A2C14", 0.95f);\n            if (!craft) RefreshJournal();\n        }',
   `        void ShowTab(bool craft) { ShowTabIndex(craft ? 0 : 1); }

        void ShowTabIndex(int tab)
        {
            _craftPanel.gameObject.SetActive(tab == 0); _journalPanel.gameObject.SetActive(tab == 1); _mapPanel.gameObject.SetActive(tab == 2);
            Color on = UI.Hex("3A2C14", 0.95f);
            _tabCraft.color = tab == 0 ? on : UI.BgSoft; _tabJournal.color = tab == 1 ? on : UI.BgSoft; _tabMap.color = tab == 2 ? on : UI.BgSoft;
            if (tab == 1) RefreshJournal();
            if (tab == 2) _mapView.Refresh();
        }`],
  ['            var inv = GameManager.I.Inv;\n            if (_selected == i) _selected = -1;', '            var inv = GameManager.I.Inv;\n            if (i >= inv.Unlocked) { GameManager.I.Hud.Hint("Craft a Hide Pack to carry more."); return; }\n            if (_selected == i) _selected = -1;'],
  ['                _slotBg[i].color = i == _selected ? UI.Hex("5A4320", 0.98f) : UI.Slot;', '                _slotBg[i].color = i >= inv.Unlocked ? UI.Hex("0A1118", 0.55f) : i == _selected ? UI.Hex("5A4320", 0.98f) : UI.Slot;'],
  ['                _useLabel.text = d.Kind == ItemKind.Food', '                _useBtn.gameObject.SetActive(d.Kind != ItemKind.Special && d.Kind != ItemKind.Resource);\n                _useLabel.text = d.Kind == ItemKind.Food'],
  ['                row.btnLabel.text = row.r.Station == Station.Campfire ? "COOK" : "CRAFT";', '                row.btnLabel.text = row.r.Station == Station.Campfire || row.r.Station == Station.Pot ? "COOK" : "CRAFT";'],
  ['            AudioManager.I.Play(r.Station == Station.Campfire ? "ignite" : "craft", 0.8f);', '            AudioManager.I.Play(r.Station == Station.Campfire ? "ignite" : r.Station == Station.Pot ? "bubble" : "craft", 0.8f);'],
]);

// ---------------------------------------------------------------- loading screen
patch('UI/UIKit.cs', [
  ['        public static void Load(string scene, float duration = 0.7f)\n        {\n            Time.timeScale = 1f;\n            FadeOut(duration, () =>\n            {\n                AudioManager.I.StopVoice();\n                UnityEngine.SceneManagement.SceneManager.LoadScene(scene);\n                FadeIn(0.9f, 0.25f);\n            });\n        }',
   `        static readonly string[] Tips =
        {
            "Boars commit to their charge. Step aside at the last moment.", "Deer see almost all the way round. Come from behind, slowly.",
            "A lit torch keeps most animals at arm's length.", "Monkeys only creep up while you look away.", "Herbs slow poison. A tonic from the cooking pot clears it.",
            "Sharks cannot follow you into the shallows.", "A fish trap in shallow water feeds you while you work.", "The Hide Pack adds two rows to your backpack.",
            "Fresh water never runs out at the falls.", "Light the pyre at the Lookout. Ships do pass.", "A knife gets more meat and hide from every kill.",
        };

        public static void Load(string scene, float duration = 0.7f)
        {
            Time.timeScale = 1f; AudioListener.pause = false;
            FadeOut(duration, () => { AudioManager.I.StopVoice(); Get().StartCoroutine(Get().LoadRoutine(scene)); });
        }

        System.Collections.IEnumerator LoadRoutine(string scene)
        {
            RectTransform card = null; Image bar = null;
            if (scene == "Game")
            {
                card = UI.Rect("Loading", transform).Stretch();
                var title = UI.Label(card, UI.Spaced(GameSettings.GameTitle, 3), 64, UI.TextCol, TextAnchor.MiddleCenter, FontStyle.Normal, true);
                title.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(1400, 90));
                var tip = UI.Label(card, Tips[UnityEngine.Random.Range(0, Tips.Length)], 26, UI.Dim, TextAnchor.MiddleCenter);
                tip.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(1300, 40));
                bar = UI.Bar(card, UI.Accent, out var barRoot, "Progress");
                barRoot.Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -90), new Vector2(420, 10)); bar.fillAmount = 0;
            }
            var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(scene);
            while (op != null && !op.isDone) { if (bar != null) bar.fillAmount = Mathf.Lerp(bar.fillAmount, op.progress, 0.2f); yield return null; }
            if (card != null) Destroy(card.gameObject);
            yield return null;
            if (scene != "Game") FadeIn(0.9f, 0.2f);
        }`],
  ['                case "sun":', `                case "joy":
                    return Raster("i_joy", 256, p =>
                    {
                        float d = Mathf.Abs(Circle(p, Vector2.zero, 0.455f)) - 0.014f;
                        for (int i = 0; i < 4; i++)
                        {
                            float a = i * Mathf.PI * 0.5f; var c = new Vector2(Mathf.Cos(a), Mathf.Sin(a)); var t = new Vector2(-c.y, c.x);
                            d = Mathf.Min(d, Capsule(p, c * 0.37f, c * 0.31f + t * 0.06f, 0.012f));
                            d = Mathf.Min(d, Capsule(p, c * 0.37f, c * 0.31f - t * 0.06f, 0.012f));
                        }
                        return d;
                    });
                case "sun":`],
]);
// SceneFader has no UI.Bar access issues (same namespace) — nothing else needed.

// ---------------------------------------------------------------- touch controls: replace the whole section
{
  let s = fs.readFileSync(dir + 'UI/Menus.cs', 'utf8').replace(/\r\n/g, '\n');
  const a = s.indexOf('    // ====================================================================== touch');
  if (a < 0) results.push('MISS Menus touch section');
  else {
    s = s.slice(0, a) + `    // ====================================================================== touch
    /// <summary>Floating thumb stick with a dead zone and a response curve. Push to the rim to run.</summary>
    public class TouchStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform Base, Knob; public CanvasGroup Visual; public Image Rim;
        Vector2 _origin, _rest; const float Radius = 118f, Dead = 0.12f;
        void Start() { _rest = Base.anchoredPosition; }
        public void OnPointerDown(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, e.position, e.pressEventCamera, out _origin);
            Base.anchoredPosition = _origin; Knob.anchoredPosition = Vector2.zero;
            Tween.Fade(Visual, 1f, 0.12f); Tween.Scale(Base, Vector3.one * 1.06f, 0.15f, Ease.OutBack);
        }
        public void OnDrag(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, e.position, e.pressEventCamera, out var p);
            Vector2 d = Vector2.ClampMagnitude(p - _origin, Radius);
            Knob.anchoredPosition = d;
            float m = d.magnitude / Radius; float k = m <= Dead ? 0 : Mathf.Pow((m - Dead) / (1 - Dead), 1.25f);
            GameInput.TouchMove = d.sqrMagnitude > 0.01f ? d.normalized * k : Vector2.zero;
            bool run = m > 0.94f && d.y > 0;
            GameInput.TouchSprint = run || TouchControls.SprintLocked;
            Rim.color = run ? UI.Accent : new Color(1, 1, 1, 0.75f);
        }
        public void OnPointerUp(PointerEventData e)
        {
            GameInput.TouchMove = Vector2.zero; GameInput.TouchSprint = TouchControls.SprintLocked;
            Tween.Move(Knob, Vector2.zero, 0.15f, Ease.OutBack); Tween.Move(Base, _rest, 0.25f); Tween.Fade(Visual, 0.45f, 0.3f); Tween.Scale(Base, Vector3.one, 0.2f, Ease.OutCubic);
            Rim.color = new Color(1, 1, 1, 0.75f);
        }
        void OnDisable() { GameInput.TouchMove = Vector2.zero; GameInput.TouchSprint = false; }
    }

    public class TouchLookArea : MonoBehaviour, IDragHandler
    {
        public void OnDrag(PointerEventData e) { GameInput.TouchLook += e.delta * (1080f / Mathf.Max(1, Screen.height)); }
    }

    public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public Action Down, Up; public Image Fill; public Color Idle;
        public void OnPointerDown(PointerEventData e)
        {
            Down?.Invoke(); Tween.Scale(transform, Vector3.one * 0.9f, 0.07f, Ease.OutQuad);
            if (Fill != null) Fill.color = new Color(UI.Accent.r, UI.Accent.g, UI.Accent.b, 0.75f);
        }
        public void OnPointerUp(PointerEventData e)
        {
            Up?.Invoke(); Tween.Scale(transform, Vector3.one, 0.2f, Ease.OutBack);
            if (Fill != null) Tween.Color(Fill, Idle, 0.2f);
        }
    }

    public class TouchControls : MonoBehaviour
    {
        public static bool SprintLocked;
        CanvasGroup _group; GameObject _rotate; Image _actRing, _sprintFill; RectTransform _act;

        public static TouchControls Create()
        {
            var canvas = UI.MakeCanvas("TouchControls", 8);
            var tc = canvas.gameObject.AddComponent<TouchControls>();
            tc._group = canvas.Group(); SprintLocked = false;
            var safe = UI.SafeArea(canvas);

            var look = UI.Img(safe, null, new Color(0, 0, 0, 0.001f), "LookArea", true); look.rectTransform.Anchor(0.38f, 0, 1, 1, Vector2.zero, Vector2.zero);
            look.gameObject.AddComponent<TouchLookArea>();

            var stickArea = UI.Img(safe, null, new Color(0, 0, 0, 0.001f), "StickArea", true); stickArea.rectTransform.Anchor(0, 0, 0.38f, 0.72f, new Vector2(0, 230), Vector2.zero);
            var stick = stickArea.gameObject.AddComponent<TouchStick>();
            var baseRt = UI.Rect("Base", stickArea.transform).Pin(new Vector2(0, 0), new Vector2(0.5f, 0.5f), new Vector2(250, 150), new Vector2(270, 270));
            var shade = UI.Img(baseRt, UI.Disc, new Color(0.02f, 0.05f, 0.08f, 0.38f), "Shade"); shade.rectTransform.Stretch(6, 6, 6, 6);
            var rim = UI.Img(baseRt, UI.Icon("joy"), new Color(1, 1, 1, 0.75f), "Rim"); rim.rectTransform.Stretch();
            var knobRt = UI.Rect("Knob", baseRt).Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(112, 112));
            var glow = UI.Img(knobRt, UI.Glow, new Color(0.95f, 0.7f, 0.35f, 0.5f), "Glow"); glow.rectTransform.Stretch(-34, -34, -34, -34);
            UI.Img(knobRt, UI.Disc, new Color(0.96f, 0.94f, 0.9f, 0.92f), "Disc").rectTransform.Stretch();
            UI.Img(knobRt, UI.Ring, new Color(0, 0, 0, 0.25f), "Edge").rectTransform.Stretch(3, 3, 3, 3);
            stick.Base = baseRt; stick.Knob = knobRt; stick.Rim = rim; stick.Visual = baseRt.Group(); stick.Visual.alpha = 0.45f; stick.Visual.blocksRaycasts = false;

            Btn(safe, "strike", "USE", new Vector2(-180, 250), 180, () => { GameInput.TouchUse = true; GameInput.TouchUseHeld = true; }, () => GameInput.TouchUseHeld = false, out _);
            tc._act = (RectTransform)Btn(safe, "hand", "TAKE", new Vector2(-385, 190), 128, () => GameInput.TouchInteract = true, null, out tc._actRing).transform;
            Btn(safe, "up", "JUMP", new Vector2(-135, 455), 122, () => GameInput.TouchJump = true, null, out _);
            var sprint = Btn(safe, "bolt", "RUN", new Vector2(-335, 395), 110, null, null, out _);
            var sb = sprint.GetComponent<TouchButton>(); tc._sprintFill = sb.Fill;
            sb.Down = () => { SprintLocked = !SprintLocked; GameInput.TouchSprint = SprintLocked; };
            sb.Up = () => { };
            tc._rotate = Btn(safe, "hammer", "TURN", new Vector2(-530, 330), 108, () => GameInput.TouchRotate = true, null, out _);
            Btn(safe, "bag", null, new Vector2(-70, -350), 96, () => GameInput.TouchInventory = true, null, out _, true);
            Btn(safe, "pause", null, new Vector2(-185, -350), 96, () => GameInput.TouchPause = true, null, out _, true);
            return tc;
        }

        static GameObject Btn(Transform parent, string icon, string label, Vector2 pos, float size, Action down, Action up, out Image ring, bool top = false)
        {
            var root = UI.Rect("T_" + icon, parent).Pin(top ? new Vector2(1, 1) : new Vector2(1, 0), new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
            var hit = root.gameObject.AddComponent<Image>(); hit.sprite = UI.Disc; hit.color = new Color(0, 0, 0, 0.001f); hit.raycastTarget = true;
            var fill = UI.Img(root, UI.Disc, new Color(0.03f, 0.07f, 0.11f, 0.55f), "Fill"); fill.rectTransform.Stretch(5, 5, 5, 5);
            var shine = UI.Img(root, UI.GradientUp, new Color(1, 1, 1, 0.10f), "Shine"); shine.rectTransform.Stretch(12, size * 0.45f, 12, 12);
            ring = UI.Img(root, UI.Ring, new Color(1, 1, 1, 0.6f), "Ring"); ring.rectTransform.Stretch();
            var ic = UI.Img(root, UI.Icon(icon), new Color(1, 1, 1, 0.95f), "Icon");
            ic.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, label != null ? size * 0.07f : 0), new Vector2(size * 0.42f, size * 0.42f));
            if (label != null)
            {
                var t = UI.Label(root, label, Mathf.RoundToInt(size * 0.13f), new Color(1, 1, 1, 0.8f), TextAnchor.MiddleCenter, FontStyle.Bold);
                t.rectTransform.Pin(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, size * 0.14f), new Vector2(size, size * 0.2f));
            }
            var tb = root.gameObject.AddComponent<TouchButton>(); tb.Down = down; tb.Up = up; tb.Fill = fill; tb.Idle = fill.color;
            return root.gameObject;
        }

        void Update()
        {
            var gm = GameManager.I; if (gm == null) return;
            bool show = !gm.InputLocked;
            _group.alpha = Mathf.MoveTowards(_group.alpha, show ? 1 : 0, Time.unscaledDeltaTime * 6f);
            _group.blocksRaycasts = show;
            if (_rotate != null) _rotate.SetActive(gm.Build != null && gm.Build.Active);
            // the action button lights up whenever there is something to interact with
            bool target = gm.Hud != null && gm.Hud.HasPrompt;
            _actRing.color = Color.Lerp(_actRing.color, target ? UI.Accent : new Color(1, 1, 1, 0.6f), Time.unscaledDeltaTime * 10f);
            _act.localScale = Vector3.one * (target ? 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.04f : 1f);
            if (_sprintFill != null && SprintLocked) _sprintFill.color = new Color(UI.Accent.r, UI.Accent.g, UI.Accent.b, 0.7f);
        }
    }
}
`;
    fs.writeFileSync(dir + 'UI/Menus.cs', s); results.push('ok   Menus touch section replaced');
  }
}
patch('UI/HUD.cs', [['        public void SetPrompt(string text)\n        {', '        public bool HasPrompt => !string.IsNullOrEmpty(_prompt);\n\n        public void SetPrompt(string text)\n        {']]);
console.log(results.join('\n'));
