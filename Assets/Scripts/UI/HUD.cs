using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LastShore
{
    /// <summary>In-game heads-up display: vitals, hotbar, compass, objective, prompts, toasts, subtitles and screen feedback.</summary>
    public class HUD : MonoBehaviour
    {
        Canvas _canvas; CanvasGroup _root; RectTransform _safe;
        Image _hp, _food, _water, _stam, _warm; Text _status;
        readonly Image[] _slotBg = new Image[Inventory.HotbarSize], _slotIcon = new Image[Inventory.HotbarSize], _slotDur = new Image[Inventory.HotbarSize];
        readonly Text[] _slotCount = new Text[Inventory.HotbarSize];
        readonly RectTransform[] _slotRt = new RectTransform[Inventory.HotbarSize];
        Text _itemName; CanvasGroup _itemNameG; Coroutine _itemNameC;
        Image _crossDot, _crossRing; CanvasGroup _promptG; Text _promptText; Text _promptKey; RectTransform _promptRt; string _prompt;
        RectTransform _objRt; CanvasGroup _objG; Text _objTitle, _objBody; string _objBodyStr;
        RectTransform _compass; readonly List<(Text t, float ang)> _ticks = new List<(Text, float)>(); RectTransform _marker; Text _markerDist;
        Text _clock, _dayText; Image _clockIcon;
        RectTransform _toasts; readonly List<RectTransform> _toastList = new List<RectTransform>();
        Text _hint; CanvasGroup _hintG; Coroutine _hintC;
        CanvasGroup _bannerG; Text _bannerBig, _bannerSmall; RectTransform _bannerRt;
        CanvasGroup _subG; Text _subText; Coroutine _subC;
        Image _hurt, _cold; float _hurtA;
        CanvasGroup _buildG; Text _buildText;
        float _slow; RectTransform _evMarker;

        public static HUD Create()
        {
            var canvas = UI.MakeCanvas("HUD", 10);
            var hud = canvas.gameObject.AddComponent<HUD>();
            hud._canvas = canvas; hud.Build();
            return hud;
        }

        void Build()
        {
            _root = _canvas.Group();
            _hurt = UI.Img(_canvas.transform, UI.Vignette, new Color(0.75f, 0.05f, 0.03f, 0), "Hurt"); _hurt.rectTransform.Stretch();
            _cold = UI.Img(_canvas.transform, UI.Vignette, new Color(0.55f, 0.8f, 1f, 0), "Cold"); _cold.rectTransform.Stretch();
            _safe = UI.SafeArea(_canvas);

            // ---- vitals
            var vit = UI.Rect("Vitals", _safe).Pin(new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -156), new Vector2(340, 190));
            var vbg = UI.Img(vit, UI.Rounded, new Color(0.03f, 0.06f, 0.09f, 0.42f), "Bg"); vbg.rectTransform.Stretch(-14, -12, 6, 16);
            _hp = Vital(vit, "heart", UI.Danger, 4, 300, 20);
            _food = Vital(vit, "food", UI.FoodCol, 3, 250, 14);
            _water = Vital(vit, "drop", UI.WaterCol, 2, 250, 14);
            _stam = Vital(vit, "bolt", UI.StaminaCol, 1, 250, 14);
            _warm = Vital(vit, "temp", UI.WarmCol, 0, 250, 14);
            _status = UI.Label(vit, "", 20, UI.Accent, TextAnchor.LowerLeft, FontStyle.Bold).Shadowed();
            _status.alignment = TextAnchor.UpperLeft; _status.rectTransform.Pin(Vector2.zero, new Vector2(0, 1), new Vector2(-6, -20), new Vector2(700, 28));

            // ---- hotbar
            var bar = _hotbar = UI.Rect("Hotbar", _safe).Pin(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(Inventory.HotbarSize * 100, 96));
            for (int i = 0; i < Inventory.HotbarSize; i++)
            {
                int idx = i;
                var bg = UI.Img(bar, UI.Rounded, UI.Bg, "Slot" + i, true);
                bg.rectTransform.Pin(new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(50 + i * 100, 0), new Vector2(90, 90));
                var btn = bg.gameObject.AddComponent<Button>(); btn.transition = Selectable.Transition.None; btn.onClick.AddListener(() => GameInput.TouchHotbar = idx);
                _slotBg[i] = bg; _slotRt[i] = bg.rectTransform;
                var ic = UI.Img(bg.transform, null, Color.white, "Icon"); ic.rectTransform.Stretch(10, 10, 10, 10); ic.preserveAspect = true; _slotIcon[i] = ic;
                var key = UI.Label(bg.transform, (i + 1).ToString(), 16, UI.Dim, TextAnchor.UpperLeft); key.rectTransform.Stretch(9, 0, 0, 5);
                _slotCount[i] = UI.Label(bg.transform, "", 20, UI.TextCol, TextAnchor.LowerRight, FontStyle.Bold).Shadowed(); _slotCount[i].rectTransform.Stretch(0, 5, 9, 0);
                var durBg = UI.Img(bg.transform, UI.Pill, new Color(0, 0, 0, 0.6f), "DurBg"); durBg.rectTransform.Anchor(0, 0, 1, 0, new Vector2(10, 6), new Vector2(-10, 12));
                var dur = UI.Img(durBg.transform, UI.Pill, UI.Good, "Dur"); dur.rectTransform.Stretch(); dur.type = Image.Type.Filled; dur.fillMethod = Image.FillMethod.Horizontal; _slotDur[i] = dur;
            }
            _itemName = UI.Label(_safe, "", 26, UI.TextCol, TextAnchor.MiddleCenter, FontStyle.Bold).Shadowed();
            _itemName.rectTransform.Pin(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 140), new Vector2(700, 36)); _itemNameG = _itemName.Group(); _itemNameG.alpha = 0;

            _buildText = UI.Label(_safe, "", 24, UI.TextCol, TextAnchor.MiddleCenter).Shadowed();
            _buildText.rectTransform.Pin(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 178), new Vector2(900, 34)); _buildG = _buildText.Group(); _buildG.alpha = 0;

            // ---- crosshair + prompt
            _crossRing = UI.Img(_safe, UI.Ring, new Color(1, 1, 1, 0), "CrossRing"); _crossRing.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34, 34));
            _crossDot = UI.Img(_safe, UI.Disc, new Color(1, 1, 1, 0.85f), "CrossDot"); _crossDot.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(7, 7));
            var pr = UI.Img(_safe, UI.Pill, UI.Bg, "Prompt"); _promptRt = pr.rectTransform;
            _promptRt.Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -92), new Vector2(420, 52)); _promptG = pr.Group(); _promptG.alpha = 0;
            var keyBg = UI.Img(pr.transform, UI.RoundedSmall, UI.Accent, "Key"); keyBg.rectTransform.Pin(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(36, 36));
            _promptKey = UI.Label(keyBg.transform, "E", 22, UI.Hex("1A1206"), TextAnchor.MiddleCenter, FontStyle.Bold); _promptKey.rectTransform.Stretch();
            _promptText = UI.Label(pr.transform, "", 24, UI.TextCol, TextAnchor.MiddleLeft); _promptText.rectTransform.Stretch(58, 0, 18, 0); _promptText.horizontalOverflow = HorizontalWrapMode.Overflow;

            // ---- objective
            var ob = UI.Img(_safe, UI.GradientSide, new Color(0.03f, 0.06f, 0.09f, 0.78f), "Objective"); _objRt = ob.rectTransform;
            _objRt.Pin(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -36), new Vector2(640, 104)); _objG = ob.Group(); _objG.alpha = 0;
            var stripe = UI.Img(ob.transform, null, UI.Accent, "Stripe"); stripe.rectTransform.Anchor(0, 0, 0, 1, new Vector2(34, 14), new Vector2(38, -14));
            _objTitle = UI.Label(ob.transform, "", 20, UI.Accent, TextAnchor.UpperLeft, FontStyle.Bold); _objTitle.rectTransform.Anchor(0, 1, 1, 1, new Vector2(52, -40), new Vector2(-20, -12));
            _objBody = UI.Label(ob.transform, "", 23, UI.TextCol, TextAnchor.UpperLeft).Shadowed(0.5f, 1.5f); _objBody.rectTransform.Anchor(0, 0, 1, 1, new Vector2(52, 6), new Vector2(-70, -40));

            // ---- compass
            var cbg = UI.Img(_safe, UI.Pill, new Color(0.03f, 0.06f, 0.09f, 0.5f), "Compass"); _compass = cbg.rectTransform;
            _compass.Pin(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -34), new Vector2(520, 38));
            cbg.gameObject.AddComponent<RectMask2D>();
            string[] names = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
            for (int i = 0; i < 8; i++)
            {
                var t = UI.Label(_compass, names[i], i % 2 == 0 ? 22 : 15, i == 0 ? UI.Accent : i % 2 == 0 ? UI.TextCol : UI.Dim, TextAnchor.MiddleCenter, FontStyle.Bold);
                t.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60, 34));
                _ticks.Add((t, i * 45f));
            }
            var notch = UI.Img(_safe, UI.Icon("marker"), UI.TextCol, "Notch"); notch.rectTransform.Pin(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -22), new Vector2(12, 10));
            var mk = UI.Img(_safe, UI.Icon("marker"), UI.Accent, "Marker"); _marker = mk.rectTransform; _marker.Pin(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -80), new Vector2(22, 20));
            _markerDist = UI.Label(mk.transform, "", 17, UI.Accent, TextAnchor.UpperCenter, FontStyle.Bold).Shadowed(); _markerDist.rectTransform.Pin(new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0, -2), new Vector2(120, 22));

            var ev = UI.Img(_safe, UI.Icon("marker"), UI.WaterCol, "EventMarker"); _evMarker = ev.rectTransform; _evMarker.Pin(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -78), new Vector2(18, 16));
            Minimap.Create(_safe);

            // ---- clock
            var ck = UI.Rect("Clock", _safe).Pin(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -30), new Vector2(260, 70));
            _clockIcon = UI.Img(ck, UI.Icon("sun"), UI.Accent, "Icon"); _clockIcon.rectTransform.Pin(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 0), new Vector2(40, 40));
            _dayText = UI.Label(ck, "DAY 1", 26, UI.TextCol, TextAnchor.UpperRight, FontStyle.Bold).Shadowed(); _dayText.rectTransform.Anchor(0, 0.5f, 1, 1, new Vector2(0, 0), new Vector2(-52, 0));
            _clock = UI.Label(ck, "06:00", 20, UI.Dim, TextAnchor.LowerRight).Shadowed(); _clock.rectTransform.Anchor(0, 0, 1, 0.5f, new Vector2(0, 2), new Vector2(-52, 0));

            // ---- tip card (one-time hints)
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

            // ---- toasts, hint, banner, subtitles
            _toasts = UI.Rect("Toasts", _safe).Pin(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-36, -60), new Vector2(360, 400));
            _hint = UI.Label(_safe, "", 28, UI.TextCol, TextAnchor.MiddleCenter, FontStyle.Bold).Shadowed(0.8f, 2.5f);
            _hint.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -190), new Vector2(1100, 44)); _hintG = _hint.Group(); _hintG.alpha = 0;

            _bannerRt = UI.Rect("Banner", _safe).Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 230), new Vector2(1200, 150)); _bannerG = _bannerRt.Group(); _bannerG.alpha = 0;
            _bannerBig = UI.Label(_bannerRt, "", 58, UI.TextCol, TextAnchor.MiddleCenter, FontStyle.Bold, true).Shadowed(0.7f, 3); _bannerBig.rectTransform.Anchor(0, 0.4f, 1, 1, Vector2.zero, Vector2.zero);
            var line = UI.Img(_bannerRt, null, UI.Accent, "Line"); line.rectTransform.Pin(new Vector2(0.5f, 0.4f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260, 3));
            _bannerSmall = UI.Label(_bannerRt, "", 26, UI.Accent, TextAnchor.MiddleCenter).Shadowed(); _bannerSmall.rectTransform.Anchor(0, 0, 1, 0.36f, Vector2.zero, Vector2.zero);

            var sub = UI.Img(_safe, UI.Rounded, new Color(0, 0, 0, 0.55f), "Subtitles");
            sub.rectTransform.Pin(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 228), new Vector2(1100, 76)); _subG = sub.Group(); _subG.alpha = 0;
            _subText = UI.Label(sub.transform, "", 29, UI.TextCol, TextAnchor.MiddleCenter); _subText.rectTransform.Stretch(24, 6, 24, 6);
        }

        Image Vital(Transform parent, string icon, Color color, int row, float width, float height)
        {
            var ic = UI.Img(parent, UI.Icon(icon), color, "Ic_" + icon);
            ic.rectTransform.Pin(Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(15, 15 + row * 34), new Vector2(28, 28));
            var fill = UI.Bar(parent, color, out var root, "Bar_" + icon);
            root.Pin(Vector2.zero, new Vector2(0, 0.5f), new Vector2(40, 15 + row * 34), new Vector2(width, height));
            return fill;
        }

        void OnEnable() { AudioManager.Subtitle += OnSubtitle; }
        void OnDisable() { AudioManager.Subtitle -= OnSubtitle; }

        void Start()
        {
            var gm = GameManager.I;
            gm.Inv.Changed += RefreshHotbar;
            gm.Interaction.SelectionChanged += () => { RefreshHotbar(); ShowItemName(); };
            gm.Vitals.Damaged += amount => { _hurtA = Mathf.Clamp01(_hurtA + amount / 22f + 0.25f); };
            gm.Vitals.HitFrom += pos => { _dmgPos = pos; _dmgA = 1f; };
            RefreshHotbar();
        }

        RectTransform _hotbar, _tipRt, _dmgRt, _threat, _fishZone, _fishCursor;
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
                _tipRt.anchoredPosition = new Vector2(0, -132);
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
        }
        public void SetHotbarVisible(bool visible) { _hotbar.gameObject.SetActive(visible); }
        public void SetVisible(bool visible) { Tween.Fade(_root, visible ? 1 : 0, 0.5f); }

        // ---------------------------------------------------------------- per-frame
        void Update()
        {
            var gm = GameManager.I; if (gm == null || gm.Player == null) return;
            var v = gm.Vitals; float dt = Time.unscaledDeltaTime;
            _hp.fillAmount = Mathf.Lerp(_hp.fillAmount, v.Health / 100f, dt * 8f);
            _food.fillAmount = v.Hunger / 100f; _water.fillAmount = v.Thirst / 100f; _warm.fillAmount = v.Warmth / 100f;
            _stam.fillAmount = Mathf.Lerp(_stam.fillAmount, v.Stamina / 100f, dt * 10f);
            _stam.color = v.Exhausted ? UI.Danger : UI.StaminaCol;
            Pulse(_food, v.Hunger, UI.FoodCol); Pulse(_water, v.Thirst, UI.WaterCol); Pulse(_warm, v.Warmth, UI.WarmCol);

            _hurtA = Mathf.MoveTowards(_hurtA, 0, dt * 0.9f);
            float low = v.Health < 30 ? (0.25f + Mathf.Sin(Time.unscaledTime * 4f) * 0.12f) * (1 - v.Health / 30f) : 0;
            var hc = _hurt.color; hc.a = Mathf.Max(_hurtA * 0.8f, low); _hurt.color = hc;
            var cc = _cold.color; cc.a = Mathf.Lerp(cc.a, v.Warmth < 30 ? 0.5f * (1 - v.Warmth / 30f) : 0, dt * 2f); _cold.color = cc;

            // crosshair
            bool target = !string.IsNullOrEmpty(_prompt);
            var rc = _crossRing.color; rc.a = Mathf.Lerp(rc.a, target ? 0.9f : 0, dt * 12f); _crossRing.color = rc;
            _crossRing.rectTransform.localScale = Vector3.one * Mathf.Lerp(_crossRing.rectTransform.localScale.x, target ? 1f : 0.5f, dt * 12f);
            _crossDot.enabled = !gm.MenuOpen && !gm.Cinematic;

            // compass
            float yaw = gm.Player.Yaw; float half = _compass.rect.width * 0.5f;
            foreach (var (t, ang) in _ticks)
            {
                float d = Mathf.DeltaAngle(yaw, ang);
                t.rectTransform.anchoredPosition = new Vector2(d / 100f * half, 0);
                var c = t.color; c.a = Mathf.Clamp01(1.15f - Mathf.Abs(d) / 100f); t.color = c;
            }
            var marker = gm.Quests.Marker;
            _marker.gameObject.SetActive(marker != null);
            if (marker != null)
            {
                Vector3 to = marker.position - gm.Player.transform.position;
                float d = Mathf.DeltaAngle(yaw, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg);
                _marker.anchoredPosition = new Vector2(Mathf.Clamp(d / 100f, -1f, 1f) * half, -80 + Mathf.Sin(Time.unscaledTime * 3f) * 2f);
                _markerDist.text = Mathf.RoundToInt(new Vector2(to.x, to.z).magnitude) + " m";
            }

            var em = gm.Events != null ? gm.Events.Marker : null;
            _evMarker.gameObject.SetActive(em != null);
            if (em != null)
            {
                Vector3 to = em.position - gm.Player.transform.position;
                float d = Mathf.DeltaAngle(yaw, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg);
                _evMarker.anchoredPosition = new Vector2(Mathf.Clamp(d / 100f, -1f, 1f) * half, -78);
            }

            UpdateFeedback(gm, yaw, half, dt);

            _slow -= dt;
            if (_slow <= 0)
            {
                _slow = 0.5f; FindThreat(gm);
                _dayText.text = "DAY " + gm.Sky.Day; _clock.text = gm.Sky.ClockText() + WeatherText(gm.Weather.State);
                _clockIcon.sprite = UI.Icon(gm.Sky.IsNight ? "moon" : "sun"); _clockIcon.color = gm.Sky.IsNight ? UI.Hex("BFD2FF") : UI.Accent;
                string s = "";
                if (v.NearFire) s += "BY THE FIRE   "; else if (v.Sheltered) s += "SHELTERED   ";
                if (v.PoisonTime > 0) s += "POISONED   ";
                if (v.Hearty > 0) s += "HEARTY   "; else if (v.WellFed > 0) s += "WELL FED   ";
                if (v.Wet) s += "WET   "; if (v.Warmth < 30) s += "FREEZING   ";
                if (v.Hunger < 15) s += "STARVING   "; if (v.Thirst < 15) s += "DEHYDRATED   "; if (v.Exhausted) s += "EXHAUSTED";
                _status.text = s;
                _status.color = v.PoisonTime > 0 || v.Warmth < 30 || v.Hunger < 15 || v.Thirst < 15 ? UI.Danger : UI.Accent;
                if (_objBodyStr != null && gm.Quests.Stage < Quests.Final) { string b = gm.Quests.Body; if (b != _objBodyStr) { _objBodyStr = b; _objBody.text = b; } }
            }
        }

        static string WeatherText(WeatherState s) => s == WeatherState.Rain ? "   RAIN" : s == WeatherState.Storm ? "   STORM" : s == WeatherState.Overcast ? "   CLOUDY" : "";

        static void Pulse(Image img, float value, Color baseCol)
        {
            img.color = value < 20 ? Color.Lerp(baseCol, UI.Danger, 0.5f + Mathf.Sin(Time.unscaledTime * 6f) * 0.5f) : baseCol;
        }

        // ---------------------------------------------------------------- hotbar
        void RefreshHotbar()
        {
            var gm = GameManager.I; int sel = gm.Interaction != null ? gm.Interaction.Selected : 0;
            for (int i = 0; i < Inventory.HotbarSize; i++)
            {
                var s = gm.Inv.Slots[i];
                _slotIcon[i].enabled = !s.Empty;
                _slotCount[i].text = !s.Empty && s.count > 1 ? s.count.ToString() : "";
                bool dur = !s.Empty && s.Def.Durability > 0;
                _slotDur[i].transform.parent.gameObject.SetActive(dur);
                if (!s.Empty)
                {
                    _slotIcon[i].sprite = s.Def.Icon;
                    if (dur) { float f = (float)s.durability / s.Def.Durability; _slotDur[i].fillAmount = f; _slotDur[i].color = f < 0.25f ? UI.Danger : UI.Good; }
                }
                bool on = i == sel;
                _slotBg[i].color = on ? UI.Hex("3A2C14", 0.95f) : UI.Bg;
                _slotBg[i].sprite = UI.Rounded;
                float target = on ? 1.12f : 1f;
                if (Mathf.Abs(_slotRt[i].localScale.x - target) > 0.01f) Tween.Scale(_slotRt[i], Vector3.one * target, 0.2f, Ease.OutBack);
                var outline = _slotBg[i].GetComponent<Outline>(); if (outline == null) outline = _slotBg[i].gameObject.AddComponent<Outline>();
                outline.effectColor = on ? UI.Accent : new Color(0, 0, 0, 0); outline.effectDistance = new Vector2(2, -2);
            }
        }

        void ShowItemName()
        {
            var s = GameManager.I.Interaction.Current;
            _itemName.text = s.Empty ? "" : s.Def.Name.ToUpperInvariant();
            Tween.Stop(_itemNameC); _itemNameG.alpha = 1;
            _itemNameC = Tween.Fade(_itemNameG, 0, 0.6f, 1.6f);
        }

        // ---------------------------------------------------------------- messages
        public bool HasPrompt => !string.IsNullOrEmpty(_prompt);

        public void SetPrompt(string text)
        {
            if (text == _prompt) return;
            _prompt = text;
            bool show = !string.IsNullOrEmpty(text);
            if (show)
            {
                _promptText.text = text;
                _promptKey.text = GameInput.TouchMode ? "" : "E";
                _promptKey.transform.parent.gameObject.SetActive(!GameInput.TouchMode);
                _promptText.rectTransform.offsetMin = new Vector2(GameInput.TouchMode ? 22 : 58, 0);
                float w = Mathf.Clamp(_promptText.preferredWidth + (GameInput.TouchMode ? 44 : 82), 160, 1100);
                _promptRt.sizeDelta = new Vector2(w, 52);
            }
            Tween.Fade(_promptG, show ? 1 : 0, 0.12f);
        }

        public void SetBuildHint(bool on, bool valid)
        {
            float target = on ? 1 : 0;
            if (on) { _buildText.text = valid ? (GameInput.TouchMode ? "Tap USE to build" : "[Click] Build      [R] Rotate") : "Can't build here"; _buildText.color = valid ? UI.TextCol : UI.Danger; }
            _buildG.alpha = Mathf.MoveTowards(_buildG.alpha, target, Time.unscaledDeltaTime * 6f);
        }

        public void Hint(string text, float duration = 2.4f, bool strong = false)
        {
            _hint.text = text; _hint.color = strong ? UI.Accent : UI.TextCol;
            Tween.Stop(_hintC); _hintG.alpha = 1;
            if (strong) Tween.Punch(_hint.transform, 0.2f, 0.3f);
            _hintC = Tween.Fade(_hintG, 0, 0.5f, duration);
        }

        public void Toast(string text, Sprite icon = null)
        {
            var bg = UI.Img(_toasts, UI.Pill, UI.Bg, "Toast"); var rt = bg.rectTransform;
            rt.Pin(new Vector2(1, 0), new Vector2(1, 0), new Vector2(380, 0), new Vector2(300, 54));
            if (icon != null) { var ic = UI.Img(rt, icon, Color.white, "Icon"); ic.rectTransform.Pin(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(44, 44)); ic.preserveAspect = true; }
            var t = UI.Label(rt, text, 23, UI.TextCol, TextAnchor.MiddleLeft, FontStyle.Bold); t.rectTransform.Stretch(icon != null ? 62 : 24, 0, 16, 0); t.horizontalOverflow = HorizontalWrapMode.Overflow;
            rt.sizeDelta = new Vector2(Mathf.Max(220, t.preferredWidth + (icon != null ? 90 : 48)), 54);
            _toastList.Insert(0, rt);
            while (_toastList.Count > 6) { var old = _toastList[_toastList.Count - 1]; _toastList.RemoveAt(_toastList.Count - 1); if (old != null) Destroy(old.gameObject); }
            for (int i = 0; i < _toastList.Count; i++) if (_toastList[i] != null) Tween.Move(_toastList[i], new Vector2(0, i * 62), 0.3f, Ease.OutCubic);
            var g = bg.Group();
            Tween.Fade(g, 0, 0.5f, 2.8f, () => { _toastList.Remove(rt); if (rt != null) Destroy(rt.gameObject); });
        }

        public void Objective(string title, string body, bool fanfare)
        {
            _objTitle.text = UI.Spaced(title); _objBody.text = body; _objBodyStr = body;
            if (!fanfare) { if (_objG.alpha < 0.01f) _objG.alpha = 1; return; }
            _objRt.anchoredPosition = new Vector2(0, -36);
            Tween.SlideIn(_objRt, _objG, new Vector2(-220, 0), 0.6f);
        }

        public void Banner(string big, string small)
        {
            _bannerBig.text = UI.Spaced(big, 2); _bannerSmall.text = small;
            _bannerRt.localScale = Vector3.one * 0.86f; _bannerG.alpha = 0;
            Tween.Fade(_bannerG, 1, 0.45f);
            Tween.Scale(_bannerRt, Vector3.one, 0.7f, Ease.OutCubic);
            Tween.Fade(_bannerG, 0, 0.7f, 2.6f);
        }

        void OnSubtitle(string text, float duration)
        {
            Tween.Stop(_subC);
            if (string.IsNullOrEmpty(text) || !GameSettings.Subtitles) { Tween.Fade(_subG, 0, 0.2f); return; }
            _subText.text = text;
            Tween.Fade(_subG, 1, 0.25f);
            _subC = Tween.Fade(_subG, 0, 0.4f, duration + 0.6f);
        }

        // ---------------------------------------------------------------- death
        public void ShowDeath(int day, string cause)
        {
            Tween.Fade(_root, 0, 1f);
            var canvas = UI.MakeCanvas("Death", 50);
            var g = canvas.Group(); g.alpha = 0;
            var bg = UI.Img(canvas.transform, null, new Color(0.04f, 0.01f, 0.01f, 0.86f), "Bg", true); bg.rectTransform.Stretch();
            var title = UI.Label(canvas.transform, UI.Spaced("YOU DID NOT SURVIVE", 2), 70, UI.TextCol, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            title.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(1600, 100));
            var sub = UI.Label(canvas.transform, "Day " + day + "  ·  Lost to " + cause, 30, UI.Accent, TextAnchor.MiddleCenter);
            sub.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 70), new Vector2(1200, 44));
            float y = -40;
            if (SaveSystem.HasSave)
            {
                var b1 = UI.Btn(canvas.transform, "LOAD LAST SAVE", () => GameManager.I.ReloadLastSave(), 28, true);
                ((RectTransform)b1.transform).Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(420, 70)); y -= 90;
            }
            var b2 = UI.Btn(canvas.transform, "MAIN MENU", () => GameManager.I.QuitToMenu(false), 28);
            ((RectTransform)b2.transform).Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(420, 70));
            Tween.Fade(g, 1, 1.4f, 1.2f);
            title.transform.localScale = Vector3.one * 1.15f; Tween.Scale(title.transform, Vector3.one, 2.5f, Ease.OutCubic, 1.2f);
        }
    }
}
