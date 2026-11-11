using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LastShore
{
    /// <summary>Settings panel shared by the main menu and the pause menu.</summary>
    public static class SettingsPanel
    {
        public static RectTransform Build(Transform parent, Action onBack)
        {
            var bg = UI.Img(parent, UI.Rounded, UI.Bg, "Settings", true);
            var rt = bg.rectTransform; rt.Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980, 900));
            var title = UI.Label(rt, UI.Spaced("SETTINGS", 2), 36, UI.TextCol, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            title.rectTransform.Anchor(0, 1, 1, 1, new Vector2(0, -84), new Vector2(0, -20));
            var line = UI.Img(rt, null, UI.Accent, "Line"); line.rectTransform.Pin(new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -92), new Vector2(160, 3));

            float y = -128; const float step = 60;
            Slider(rt, "Master volume", ref y, 0, 1, GameSettings.Master, v => { GameSettings.Master = v; AudioListener.volume = v; });
            Slider(rt, "Nature ambience", ref y, 0, 1, GameSettings.Ambience, v => GameSettings.Ambience = v);
            Slider(rt, "Sound effects", ref y, 0, 1, GameSettings.Sfx, v => GameSettings.Sfx = v);
            Slider(rt, "Voice", ref y, 0, 1, GameSettings.Voice, v => GameSettings.Voice = v);
            Slider(rt, "Look sensitivity", ref y, 0.3f, 2.5f, GameSettings.Sensitivity, v => GameSettings.Sensitivity = v);
            Slider(rt, "Field of view", ref y, 55, 95, GameSettings.Fov, v => GameSettings.Fov = v);
            string[] q = { "LOW", "MEDIUM", "HIGH" };
            Choice(rt, "Graphics quality", ref y, () => q[GameSettings.Quality], () => { GameSettings.Quality = (GameSettings.Quality + 1) % 3; GameSettings.Apply(); });
            Choice(rt, "Subtitles", ref y, () => GameSettings.Subtitles ? "ON" : "OFF", () => GameSettings.Subtitles = !GameSettings.Subtitles);
            Choice(rt, "Head bob", ref y, () => GameSettings.HeadBob ? "ON" : "OFF", () => GameSettings.HeadBob = !GameSettings.HeadBob);
            Choice(rt, "Invert vertical look", ref y, () => GameSettings.InvertY ? "ON" : "OFF", () => GameSettings.InvertY = !GameSettings.InvertY);
            Choice(rt, "Vibration", ref y, () => GameSettings.Vibration ? "ON" : "OFF", () => GameSettings.Vibration = !GameSettings.Vibration);
            _ = step;

            var back = UI.Btn(rt, "DONE", () => { GameSettings.Save(); onBack?.Invoke(); }, 26, true);
            ((RectTransform)back.transform).Pin(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(300, 64));
            return rt;
        }

        static void RowLabel(RectTransform parent, string label, float y)
        {
            var t = UI.Label(parent, label, 25, UI.TextCol, TextAnchor.MiddleLeft);
            t.rectTransform.Anchor(0, 1, 0.5f, 1, new Vector2(60, y - 52), new Vector2(0, y));
        }

        static void Slider(RectTransform parent, string label, ref float y, float min, float max, float value, Action<float> set)
        {
            RowLabel(parent, label, y);
            var s = UI.MakeSlider(parent, min, max, value, set);
            ((RectTransform)s.transform).Anchor(0.5f, 1, 1, 1, new Vector2(0, y - 46), new Vector2(-60, y - 6));
            y -= 60;
        }

        static void Choice(RectTransform parent, string label, ref float y, Func<string> get, Action toggle)
        {
            RowLabel(parent, label, y);
            Text txt = null;
            var b = UI.Btn(parent, get(), () => { toggle(); txt.text = get(); Tween.Punch(txt.transform, 0.12f, 0.2f); }, 22);
            ((RectTransform)b.transform).Anchor(1, 1, 1, 1, new Vector2(-280, y - 50), new Vector2(-60, y - 2));
            txt = b.GetComponentInChildren<Text>(); txt.color = UI.Accent;
            y -= 60;
        }
    }

    public class PauseMenu : MonoBehaviour
    {
        public bool Open { get; private set; }
        Canvas _canvas; CanvasGroup _group; RectTransform _main, _settings;

        public static PauseMenu Create()
        {
            var canvas = UI.MakeCanvas("PauseMenu", 30);
            var p = canvas.gameObject.AddComponent<PauseMenu>(); p._canvas = canvas; p.Build();
            canvas.gameObject.SetActive(false);
            return p;
        }

        void Build()
        {
            _group = _canvas.Group();
            var dim = UI.Img(_canvas.transform, null, new Color(0.01f, 0.03f, 0.05f, 0.8f), "Dim", true); dim.rectTransform.Stretch();
            var bg = UI.Img(_canvas.transform, UI.Rounded, UI.Bg, "Main"); _main = bg.rectTransform;
            _main.Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 560));
            var title = UI.Label(_main, UI.Spaced("PAUSED", 3), 44, UI.TextCol, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            title.rectTransform.Anchor(0, 1, 1, 1, new Vector2(0, -120), new Vector2(0, -30));
            var line = UI.Img(_main, null, UI.Accent, "Line"); line.rectTransform.Pin(new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -128), new Vector2(120, 3));
            Add("RESUME", -200, true, Toggle);
            Add("SETTINGS", -290, false, () => ShowSettings(true));
            Add("SAVE & QUIT", -380, false, () => { Time.timeScale = 1; AudioListener.pause = false; GameManager.I.QuitToMenu(true); });
            var hint = UI.Label(_main, "Your journey is saved automatically.", 19, UI.Dim, TextAnchor.MiddleCenter);
            hint.rectTransform.Anchor(0, 0, 1, 0, new Vector2(0, 26), new Vector2(0, 60));
            _settings = SettingsPanel.Build(_canvas.transform, () => ShowSettings(false));
            _settings.gameObject.SetActive(false);
        }

        void Add(string label, float y, bool primary, Action onClick)
        {
            var b = UI.Btn(_main, label, onClick, 28, primary);
            ((RectTransform)b.transform).Pin(new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(400, 70));
        }

        void ShowSettings(bool on)
        {
            _settings.gameObject.SetActive(on); _main.gameObject.SetActive(!on);
            var rt = on ? _settings : _main;
            rt.localScale = Vector3.one * 0.94f; Tween.Scale(rt, Vector3.one, 0.25f, Ease.OutBack);
        }

        public void Toggle()
        {
            Open = !Open;
            Time.timeScale = Open ? 0 : 1; AudioListener.pause = Open;
            if (Open)
            {
                _canvas.gameObject.SetActive(true); ShowSettings(false);
                _group.alpha = 0; Tween.Fade(_group, 1, 0.2f);
            }
            else { GameSettings.Save(); Tween.Fade(_group, 0, 0.15f, 0, () => { if (!Open) _canvas.gameObject.SetActive(false); }); }
        }
    }

    // ====================================================================== touch
    /// <summary>Floating thumb stick with a dead zone and a response curve. Push to the rim to run.</summary>
    public class TouchStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform Base, Knob; public CanvasGroup Visual; public Image Rim;
        Vector2 _origin, _rest; const float Radius = 118f, Dead = 0.12f;
        void Start() { _rest = Base.anchoredPosition; }
        public void OnPointerDown(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, e.position, e.pressEventCamera, out _origin);
            // the local point is relative to the pivot; Base is anchored to the bottom-left corner
            Base.anchoredPosition = _origin - ((RectTransform)transform).rect.min; Knob.anchoredPosition = Vector2.zero;
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
            Knob.anchoredPosition = Vector2.zero; Tween.Fade(Visual, 0f, 0.18f); Tween.Scale(Base, Vector3.one, 0.2f, Ease.OutCubic);
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

            var look = UI.Img(safe, null, new Color(0, 0, 0, 0.001f), "LookArea", true); look.rectTransform.Anchor(0.42f, 0, 1, 1, Vector2.zero, Vector2.zero);
            look.gameObject.AddComponent<TouchLookArea>();

            var stickArea = UI.Img(safe, null, new Color(0, 0, 0, 0.001f), "StickArea", true); stickArea.rectTransform.Anchor(0, 0, 0.42f, 0.62f, Vector2.zero, Vector2.zero);
            var stick = stickArea.gameObject.AddComponent<TouchStick>();
            var baseRt = UI.Rect("Base", stickArea.transform).Pin(new Vector2(0, 0), new Vector2(0.5f, 0.5f), new Vector2(250, 150), new Vector2(270, 270));
            var shade = UI.Img(baseRt, UI.Disc, new Color(0.02f, 0.05f, 0.08f, 0.38f), "Shade"); shade.rectTransform.Stretch(6, 6, 6, 6);
            var rim = UI.Img(baseRt, UI.Icon("joy"), new Color(1, 1, 1, 0.75f), "Rim"); rim.rectTransform.Stretch();
            var knobRt = UI.Rect("Knob", baseRt).Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(112, 112));
            var glow = UI.Img(knobRt, UI.Glow, new Color(0.95f, 0.7f, 0.35f, 0.5f), "Glow"); glow.rectTransform.Stretch(-34, -34, -34, -34);
            UI.Img(knobRt, UI.Disc, new Color(0.96f, 0.94f, 0.9f, 0.92f), "Disc").rectTransform.Stretch();
            UI.Img(knobRt, UI.Ring, new Color(0, 0, 0, 0.25f), "Edge").rectTransform.Stretch(3, 3, 3, 3);
            stick.Base = baseRt; stick.Knob = knobRt; stick.Rim = rim; stick.Visual = baseRt.Group(); stick.Visual.alpha = 0f; stick.Visual.blocksRaycasts = false;

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
