using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LastShore
{
    /// <summary>Code-first UI toolkit: procedural sprites, consistent styling and responsive anchoring helpers.</summary>
    public static class UI
    {
        public static readonly Color Bg = Hex("0C141D", 0.90f);
        public static readonly Color BgSoft = Hex("16222F", 0.82f);
        public static readonly Color Slot = Hex("1E2C3A", 0.92f);
        public static readonly Color Accent = Hex("F2B45A");
        public static readonly Color TextCol = Hex("F4EFE3");
        public static readonly Color Dim = Hex("9FAAB5");
        public static readonly Color Danger = Hex("E2584C");
        public static readonly Color Good = Hex("7DCB70");
        public static readonly Color WaterCol = Hex("5CB8EA");
        public static readonly Color FoodCol = Hex("E9A04B");
        public static readonly Color StaminaCol = Hex("E9DC6A");
        public static readonly Color WarmCol = Hex("F0795A");

        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        static Font _title, _body;

        public static Color Hex(string hex, float a = 1f)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c); c.a = a; return c;
        }

        public static Font BodyFont
        {
            get
            {
                if (_body == null) _body = Resources.Load<Font>("Fonts/Body");
                if (_body == null) _body = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _body;
            }
        }

        public static Font TitleFont
        {
            get
            {
                if (_title == null) _title = Resources.Load<Font>("Fonts/Title");
                return _title != null ? _title : BodyFont;
            }
        }

        /// <summary>Adds spacing between letters for display titles.</summary>
        public static string Spaced(string s, int gaps = 1)
        {
            var sb = new System.Text.StringBuilder();
            string gap = gaps > 2 ? "  " : " ";
            for (int i = 0; i < s.Length; i++) { sb.Append(s[i]); if (i < s.Length - 1) sb.Append(s[i] == ' ' ? gap + gap : gap); }
            return sb.ToString();
        }

        // ------------------------------------------------------------------ sprites
        static float Circle(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;
        static float Capsule(Vector2 p, Vector2 a, Vector2 b, float r)
        {
            Vector2 pa = p - a, ba = b - a; float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude - r;
        }
        static float Box(Vector2 p, Vector2 c, Vector2 half, float r = 0f)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y)) - half + new Vector2(r, r);
            return new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - r;
        }
        static float Poly(Vector2 p, params Vector2[] v)
        {
            float d = Vector2.Dot(p - v[0], p - v[0]); float s = 1f;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
            {
                Vector2 e = v[j] - v[i], w = p - v[i];
                Vector2 b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                d = Mathf.Min(d, Vector2.Dot(b, b));
                bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        }
        static float Heart(Vector2 p)
        {
            p = new Vector2(Mathf.Abs(p.x) * 1.15f, (p.y + 0.46f) * 1.15f);
            if (p.y + p.x > 1f) return ((p - new Vector2(0.25f, 0.75f)).magnitude - Mathf.Sqrt(2f) / 4f) / 1.15f;
            Vector2 a = p - new Vector2(0, 1); Vector2 b = p - 0.5f * Mathf.Max(p.x + p.y, 0) * Vector2.one;
            return Mathf.Sqrt(Mathf.Min(Vector2.Dot(a, a), Vector2.Dot(b, b))) * Mathf.Sign(p.x - p.y) / 1.15f;
        }

        static Sprite Raster(string key, int size, Func<Vector2, float> sdf, Vector4 border = default, Func<Vector2, float, float> alphaFn = null)
        {
            if (Cache.TryGetValue(key, out var sp) && sp != null) return sp;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = key };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2((x + 0.5f) / size - 0.5f, (y + 0.5f) / size - 0.5f);
                    float d = sdf(p);
                    float a = alphaFn != null ? alphaFn(p, d) : Mathf.Clamp01(0.5f - d * size);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255));
                }
            tex.SetPixels32(px); tex.Apply(false, true);
            sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, border);
            Cache[key] = sp; return sp;
        }

        public static Sprite Rounded => Raster("rounded", 64, p => Box(p, Vector2.zero, new Vector2(0.5f, 0.5f), 0.28f), new Vector4(22, 22, 22, 22));
        public static Sprite RoundedSmall => Raster("roundedS", 32, p => Box(p, Vector2.zero, new Vector2(0.5f, 0.5f), 0.22f), new Vector4(9, 9, 9, 9));
        public static Sprite Pill => Raster("pill", 64, p => Box(p, Vector2.zero, new Vector2(0.5f, 0.5f), 0.5f), new Vector4(31, 31, 31, 31));
        public static Sprite Frame => Raster("frame", 64, p => Mathf.Abs(Box(p, Vector2.zero, new Vector2(0.47f, 0.47f), 0.26f)) - 0.025f, new Vector4(22, 22, 22, 22));
        public static Sprite Solid => Raster("solid", 8, p => -1f);
        public static Sprite Disc => Raster("disc", 128, p => Circle(p, Vector2.zero, 0.48f));
        public static Sprite Ring => Raster("ring", 128, p => Mathf.Abs(Circle(p, Vector2.zero, 0.43f)) - 0.035f);
        public static Sprite Glow => Raster("glow", 128, p => 0, default, (p, d) => Mathf.Pow(Mathf.Clamp01(1 - p.magnitude * 2f), 2.2f));
        public static Sprite Vignette => Raster("vignette", 128, p => 0, default, (p, d) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.28f, 0.74f, p.magnitude)));
        public static Sprite GradientUp => Raster("gradUp", 64, p => 0, default, (p, d) => Mathf.Pow(Mathf.Clamp01(0.5f - p.y), 1.6f));
        public static Sprite GradientSide => Raster("gradSide", 64, p => 0, default, (p, d) => Mathf.Pow(Mathf.Clamp01(0.5f - p.x), 1.3f));

        public static Sprite Icon(string name)
        {
            const int S = 96;
            switch (name)
            {
                case "heart": return Raster("i_heart", S, p => Heart(p * 1.12f) / 1.12f);
                case "drop": return Raster("i_drop", S, p => Mathf.Min(Circle(p, new Vector2(0, -0.13f), 0.27f), Poly(p, new Vector2(-0.235f, -0.0f), new Vector2(0.235f, -0.0f), new Vector2(0, 0.44f))));
                case "food": return Raster("i_food", S, p => Mathf.Min(Mathf.Min(Circle(p, new Vector2(0.09f, 0.1f), 0.26f), Capsule(p, new Vector2(-0.08f, -0.08f), new Vector2(-0.27f, -0.27f), 0.06f)),
                    Mathf.Min(Circle(p, new Vector2(-0.33f, -0.25f), 0.07f), Circle(p, new Vector2(-0.25f, -0.33f), 0.07f))));
                case "bolt": return Raster("i_bolt", S, p => Poly(p, new Vector2(0.1f, 0.45f), new Vector2(-0.24f, -0.06f), new Vector2(-0.03f, -0.06f), new Vector2(-0.12f, -0.45f), new Vector2(0.24f, 0.08f), new Vector2(0.03f, 0.08f)));
                case "temp": return Raster("i_temp", S, p => Mathf.Min(Capsule(p, new Vector2(0, 0.32f), new Vector2(0, -0.15f), 0.085f), Circle(p, new Vector2(0, -0.26f), 0.17f)));
                case "bag":
                    return Raster("i_bag2", S, p =>
                    {
                        float d = 1f;
                        for (int ix = -1; ix <= 1; ix++) for (int iy = -1; iy <= 1; iy++) d = Mathf.Min(d, Box(p, new Vector2(ix * 0.27f, iy * 0.27f), new Vector2(0.105f, 0.105f), 0.035f));
                        return d;
                    });
                case "hammer": return Raster("i_hammer", S, p => Mathf.Min(Box(p, new Vector2(0, 0.24f), new Vector2(0.3f, 0.12f), 0.04f), Capsule(p, new Vector2(0, 0.1f), new Vector2(0, -0.4f), 0.055f)));
                case "book": return Raster("i_book", S, p => Mathf.Max(Box(p, Vector2.zero, new Vector2(0.31f, 0.38f), 0.05f), -Box(p, new Vector2(-0.19f, 0), new Vector2(0.016f, 0.3f))));
                case "pause": return Raster("i_pause", S, p => Mathf.Min(Box(p, new Vector2(-0.14f, 0), new Vector2(0.075f, 0.3f), 0.03f), Box(p, new Vector2(0.14f, 0), new Vector2(0.075f, 0.3f), 0.03f)));
                case "close": return Raster("i_close", S, p => Mathf.Min(Capsule(p, new Vector2(-0.26f, -0.26f), new Vector2(0.26f, 0.26f), 0.055f), Capsule(p, new Vector2(-0.26f, 0.26f), new Vector2(0.26f, -0.26f), 0.055f)));
                case "check": return Raster("i_check", S, p => Mathf.Min(Capsule(p, new Vector2(-0.3f, -0.02f), new Vector2(-0.1f, -0.24f), 0.06f), Capsule(p, new Vector2(-0.1f, -0.24f), new Vector2(0.32f, 0.24f), 0.06f)));
                case "up": return Raster("i_up", S, p => Mathf.Min(Mathf.Min(Capsule(p, new Vector2(-0.26f, -0.02f), new Vector2(0, 0.26f), 0.06f), Capsule(p, new Vector2(0.26f, -0.02f), new Vector2(0, 0.26f), 0.06f)),
                    Mathf.Min(Capsule(p, new Vector2(-0.26f, -0.3f), new Vector2(0, -0.02f), 0.06f), Capsule(p, new Vector2(0.26f, -0.3f), new Vector2(0, -0.02f), 0.06f))));
                case "marker": return Raster("i_marker", S, p => Poly(p, new Vector2(-0.3f, 0.3f), new Vector2(0.3f, 0.3f), new Vector2(0, -0.36f)));
                case "hand": return Raster("i_hand", S, p => Mathf.Min(Mathf.Abs(Circle(p, Vector2.zero, 0.33f)) - 0.045f, Circle(p, Vector2.zero, 0.13f)));
                case "strike": return Raster("i_strike", S, p => Mathf.Min(Capsule(p, new Vector2(-0.3f, -0.3f), new Vector2(0.12f, 0.12f), 0.055f), Box(p, new Vector2(0.2f, 0.2f), new Vector2(0.17f, 0.17f), 0.06f)));
                case "star":
                    {
                        var pts = new Vector2[10];
                        for (int i = 0; i < 10; i++) { float a = Mathf.PI / 2 + i * Mathf.PI / 5; float r = i % 2 == 0 ? 0.46f : 0.2f; pts[i] = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r); }
                        return Raster("i_star", S, p => Poly(p, pts) - 0.02f);
                    }
                case "joy":
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
                case "sun": return Raster("i_sun", S, p => Circle(p, Vector2.zero, 0.3f));
                case "moon": return Raster("i_moon", S, p => Mathf.Max(Circle(p, Vector2.zero, 0.34f), -Circle(p, new Vector2(0.16f, 0.1f), 0.28f)));
                default: return Disc;
            }
        }

        // ------------------------------------------------------------------ building blocks
        public static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            go.GetComponent<EventSystem>().sendNavigationEvents = false;
        }

        public static Canvas MakeCanvas(string name, int order, Transform parent = null)
        {
            EnsureEventSystem();
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent != null) go.transform.SetParent(parent, false);
            var c = go.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = order;
            var s = go.GetComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; s.referenceResolution = new Vector2(1920, 1080);
            s.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; s.matchWidthOrHeight = 0.6f;
            return c;
        }

        /// <summary>Full-screen child that follows the device safe area (notches, rounded corners).</summary>
        public static RectTransform SafeArea(Canvas canvas)
        {
            var rt = Rect("SafeArea", canvas.transform).Stretch();
            rt.gameObject.AddComponent<SafeAreaFitter>();
            return rt;
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static Image Img(Transform parent, Sprite sprite, Color color, string name = "Image", bool raycast = false)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite; img.color = color; img.raycastTarget = raycast;
            if (sprite != null && sprite.border.sqrMagnitude > 0) { img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 1f; }
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter,
            FontStyle style = FontStyle.Normal, bool title = false, string name = "Text")
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = title ? TitleFont : BodyFont; t.text = text; t.fontSize = size; t.color = color; t.alignment = anchor; t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false; t.supportRichText = true;
            return t;
        }

        public static Text Shadowed(this Text t, float alpha = 0.6f, float dist = 2f)
        {
            var s = t.gameObject.AddComponent<Shadow>(); s.effectColor = new Color(0, 0, 0, alpha); s.effectDistance = new Vector2(dist * 0.6f, -dist);
            return t;
        }

        public static Button Btn(Transform parent, string label, Action onClick, int fontSize = 30, bool primary = false, string name = null)
        {
            var img = Img(parent, Rounded, primary ? Accent : BgSoft, name ?? ("Btn_" + label), true);
            var b = img.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
            var fx = img.gameObject.AddComponent<ButtonFX>(); fx.Normal = img.color; fx.Hover = primary ? Hex("FFD08A") : Hex("2A3D52", 0.95f);
            if (!string.IsNullOrEmpty(label))
            {
                var t = Label(img.transform, label, fontSize, primary ? Hex("1A1206") : TextCol, TextAnchor.MiddleCenter, FontStyle.Bold);
                t.rectTransform.Stretch(12, 4, 12, 4);
                fx.LabelText = t;
            }
            b.onClick.AddListener(() => { onClick?.Invoke(); });
            return b;
        }

        public static Button IconBtn(Transform parent, string icon, Action onClick, float size = 96, string name = null)
        {
            var img = Img(parent, Disc, Hex("0C141D", 0.55f), name ?? ("IconBtn_" + icon), true);
            img.rectTransform.sizeDelta = new Vector2(size, size);
            var b = img.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
            var fx = img.gameObject.AddComponent<ButtonFX>(); fx.Normal = img.color; fx.Hover = Hex("2A3D52", 0.8f);
            var ic = Img(img.transform, Icon(icon), TextCol, "Icon");
            ic.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.55f, size * 0.55f));
            b.onClick.AddListener(() => onClick?.Invoke());
            return b;
        }

        /// <summary>Horizontal value bar. Returns the fill image; set fillAmount 0..1.</summary>
        public static Image Bar(Transform parent, Color color, out RectTransform root, string name = "Bar")
        {
            var bg = Img(parent, Pill, Hex("000000", 0.5f), name); root = bg.rectTransform; bg.pixelsPerUnitMultiplier = 5f;
            var fill = Img(bg.transform, Solid, color, "Fill");
            fill.rectTransform.Stretch(4, 4, 4, 4);
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillOrigin = 0;
            return fill;
        }

        public static Slider MakeSlider(Transform parent, float min, float max, float value, Action<float> onChange)
        {
            var root = Rect("Slider", parent);
            var s = root.gameObject.AddComponent<Slider>();
            var bg = Img(root, Pill, Hex("000000", 0.55f), "Bg", true); bg.rectTransform.Anchor(0, 0.5f, 1, 0.5f, new Vector2(0, -7), new Vector2(0, 7));
            var fillArea = Rect("FillArea", root); fillArea.Anchor(0, 0.5f, 1, 0.5f, new Vector2(0, -7), new Vector2(0, 7));
            var fill = Img(fillArea, Pill, Accent, "Fill"); fill.rectTransform.Stretch();
            var handleArea = Rect("HandleArea", root); handleArea.Stretch(14, 0, 14, 0);
            var handle = Img(handleArea, Disc, TextCol, "Handle", true); handle.rectTransform.sizeDelta = new Vector2(34, 34);
            handle.rectTransform.anchorMin = new Vector2(0, 0.5f); handle.rectTransform.anchorMax = new Vector2(0, 0.5f);
            s.fillRect = fill.rectTransform; s.handleRect = handle.rectTransform; s.targetGraphic = handle; s.direction = Slider.Direction.LeftToRight;
            s.minValue = min; s.maxValue = max; s.value = value;
            s.onValueChanged.AddListener(v => onChange?.Invoke(v));
            return s;
        }

        // ------------------------------------------------------------------ layout extensions
        public static RectTransform Stretch(this RectTransform rt, float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = new Vector2(left, bottom); rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static RectTransform Anchor(this RectTransform rt, float minX, float minY, float maxX, float maxY, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = new Vector2(minX, minY); rt.anchorMax = new Vector2(maxX, maxY); rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
            return rt;
        }

        public static RectTransform Pin(this RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = pivot; rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        public static CanvasGroup Group(this Component c)
        {
            var g = c.GetComponent<CanvasGroup>();
            return g != null ? g : c.gameObject.AddComponent<CanvasGroup>();
        }
    }

    /// <summary>Hover / press micro-animations and click sound for buttons.</summary>
    public class ButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        public Color Normal, Hover;
        public Text LabelText;
        Image _img; Coroutine _scale;
        bool _hover;

        void Awake() { _img = GetComponent<Image>(); }
        void OnDisable() { transform.localScale = Vector3.one; if (_img != null) _img.color = Normal; _hover = false; }

        void ScaleTo(float s, float d, Ease e) { Tween.Stop(_scale); _scale = Tween.Scale(transform, Vector3.one * s, d, e); }
        public void OnPointerEnter(PointerEventData e) { _hover = true; ScaleTo(1.045f, 0.18f, Ease.OutBack); Tween.Color(_img, Hover, 0.15f); }
        public void OnPointerExit(PointerEventData e) { _hover = false; ScaleTo(1f, 0.18f, Ease.OutCubic); Tween.Color(_img, Normal, 0.2f); }
        public void OnPointerDown(PointerEventData e) { ScaleTo(0.94f, 0.08f, Ease.OutQuad); }
        public void OnPointerUp(PointerEventData e) { ScaleTo(_hover ? 1.045f : 1f, 0.22f, Ease.OutBack); }
        public void OnPointerClick(PointerEventData e) { AudioManager.I.UI(); }
    }

    public class SafeAreaFitter : MonoBehaviour
    {
        Rect _last;
        void Update()
        {
            var sa = Screen.safeArea;
            if (sa == _last || Screen.width <= 0 || Screen.height <= 0) return;
            _last = sa;
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
            rt.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }

    /// <summary>Global fade-to-black used for scene transitions.</summary>
    public class SceneFader : MonoBehaviour
    {
        static SceneFader _inst;
        CanvasGroup _group;

        static SceneFader Get()
        {
            if (_inst != null) return _inst;
            var canvas = UI.MakeCanvas("[Fader]", 1000);
            DontDestroyOnLoad(canvas.gameObject);
            var img = UI.Img(canvas.transform, null, Color.black, "Black", true); img.rectTransform.Stretch();
            _inst = canvas.gameObject.AddComponent<SceneFader>();
            _inst._group = canvas.Group(); _inst._group.alpha = 0; _inst._group.blocksRaycasts = false;
            return _inst;
        }

        public static void SetBlack() { var f = Get(); f._group.alpha = 1; }

        public static void FadeIn(float duration = 0.8f, float delay = 0f)
        {
            var f = Get(); f._group.blocksRaycasts = false;
            Tween.Fade(f._group, 0, duration, delay, null, Ease.InOutQuad);
        }

        public static void FadeOut(float duration, Action done)
        {
            var f = Get(); f._group.blocksRaycasts = true;
            Tween.Fade(f._group, 1, duration, 0, done, Ease.InOutQuad);
        }

        static readonly string[] Tips =
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
        }
    }
}
