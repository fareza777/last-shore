using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LastShore
{
    /// <summary>Round minimap in the HUD. Appears once the player carries the compass; rotates with the view.</summary>
    public class Minimap : MonoBehaviour
    {
        const float ViewMetres = 300f, SizePx = 210f;
        RectTransform _map; CanvasGroup _group; Image _quest, _event;
        readonly List<(Landmark l, Image dot)> _dots = new List<(Landmark, Image)>();
        float _t;

        public static Minimap Create(Transform parent)
        {
            var root = UI.Rect("Minimap", parent).Pin(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -118), new Vector2(SizePx, SizePx));
            var m = root.gameObject.AddComponent<Minimap>(); m._group = root.Group(); m._group.alpha = 0;
            var mask = UI.Img(root, UI.Disc, new Color(0.04f, 0.1f, 0.14f, 1f), "Mask"); mask.rectTransform.Stretch(4, 4, 4, 4);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var sprite = Resources.Load<Sprite>("UI/IslandMap");
            var map = UI.Img(mask.transform, sprite, sprite != null ? Color.white : new Color(0.1f, 0.3f, 0.35f), "Map"); m._map = map.rectTransform;
            float px = SizePx * World.Size / ViewMetres;
            m._map.anchorMin = m._map.anchorMax = new Vector2(0.5f, 0.5f); m._map.sizeDelta = new Vector2(px, px);
            m._quest = Dot(m._map, UI.Accent, 15); m._event = Dot(m._map, UI.WaterCol, 13);
            var ring = UI.Img(root, UI.Ring, new Color(0.95f, 0.8f, 0.5f, 0.9f), "Ring"); ring.rectTransform.Stretch(-6, -6, -6, -6);
            var arrow = UI.Img(root, UI.Icon("marker"), UI.Accent, "Player"); arrow.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20, 22)); arrow.rectTransform.localRotation = Quaternion.Euler(0, 0, 180);
            var n = UI.Label(root, "N", 17, UI.Accent, TextAnchor.MiddleCenter, FontStyle.Bold).Shadowed(); n.name = "North";
            n.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24, 24));
            return m;
        }

        static Image Dot(RectTransform map, Color c, float size)
        {
            var d = UI.Img(map, UI.Disc, c, "Dot"); d.rectTransform.sizeDelta = new Vector2(size, size); d.gameObject.SetActive(false);
            return d;
        }

        static void Place(Image dot, Vector3 world)
        {
            var a = new Vector2(world.x / World.Size, world.z / World.Size);
            dot.rectTransform.anchorMin = dot.rectTransform.anchorMax = a; dot.rectTransform.anchoredPosition = Vector2.zero;
        }

        void Update()
        {
            var gm = GameManager.I; if (gm == null || gm.Player == null) return;
            bool show = gm.Inv.Count("compass") > 0 && !gm.Cinematic;
            _group.alpha = Mathf.MoveTowards(_group.alpha, show ? 1 : 0, Time.unscaledDeltaTime * 3f);
            if (_group.alpha <= 0) return;
            Vector3 p = gm.Player.transform.position; float yaw = gm.Player.Yaw;
            _map.pivot = new Vector2(p.x / World.Size, p.z / World.Size);
            _map.anchoredPosition = Vector2.zero; _map.localRotation = Quaternion.Euler(0, 0, yaw);
            var north = transform.Find("North") as RectTransform;
            if (north != null) { float r = yaw * Mathf.Deg2Rad; north.anchoredPosition = new Vector2(-Mathf.Sin(r), Mathf.Cos(r)) * (SizePx * 0.5f + 4f); }
            _t -= Time.unscaledDeltaTime; if (_t > 0) return; _t = 0.6f;
            if (_dots.Count != Landmark.All.Count) { foreach (var d in _dots) if (d.dot != null) Destroy(d.dot.gameObject); _dots.Clear(); foreach (var l in Landmark.All) _dots.Add((l, Dot(_map, UI.TextCol, 10))); }
            foreach (var (l, dot) in _dots) { bool on = l != null && l.Found; dot.gameObject.SetActive(on); if (on) Place(dot, l.transform.position); }
            var qm = gm.Quests.Marker; _quest.gameObject.SetActive(qm != null); if (qm != null) Place(_quest, qm.position);
            var em = gm.Events != null ? gm.Events.Marker : null; _event.gameObject.SetActive(em != null); if (em != null) Place(_event, em.position);
        }
    }

    /// <summary>Full island chart shown in the backpack's MAP tab.</summary>
    public class MapView : MonoBehaviour
    {
        RectTransform _map; Image _player; Text _info; GameObject _locked;
        readonly GameObject[,] _fog = new GameObject[10, 10];
        readonly List<(Landmark l, Image dot, Text label)> _marks = new List<(Landmark, Image, Text)>();

        public static MapView Build(RectTransform parent)
        {
            var v = parent.gameObject.AddComponent<MapView>();
            v._info = UI.Label(parent, "", 20, UI.Accent, TextAnchor.UpperLeft, FontStyle.Bold); v._info.rectTransform.Anchor(0, 1, 1, 1, new Vector2(0, -30), Vector2.zero);
            var frame = UI.Img(parent, UI.Rounded, new Color(0.04f, 0.1f, 0.14f, 1f), "Frame"); frame.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -18), new Vector2(660, 660));
            frame.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var sprite = Resources.Load<Sprite>("UI/IslandMap");
            var map = UI.Img(frame.transform, sprite, sprite != null ? Color.white : new Color(0.1f, 0.3f, 0.35f), "Map"); v._map = map.rectTransform; v._map.Stretch(6, 6, 6, 6);
            for (int z = 0; z < 10; z++)
                for (int x = 0; x < 10; x++)
                {
                    var f = UI.Img(v._map, null, new Color(0.035f, 0.08f, 0.11f, 0.94f), "Fog");
                    f.rectTransform.anchorMin = new Vector2(x / 10f, z / 10f); f.rectTransform.anchorMax = new Vector2((x + 1) / 10f, (z + 1) / 10f);
                    f.rectTransform.offsetMin = new Vector2(-0.5f, -0.5f); f.rectTransform.offsetMax = new Vector2(0.5f, 0.5f);
                    v._fog[x, z] = f.gameObject;
                }
            v._player = UI.Img(v._map, UI.Icon("marker"), UI.WaterCol, "Player"); v._player.rectTransform.sizeDelta = new Vector2(24, 26);
            v._locked = UI.Img(parent, UI.Rounded, new Color(0.03f, 0.06f, 0.09f, 0.94f), "Locked").gameObject; ((RectTransform)v._locked.transform).Stretch(0, 0, 0, 40);
            var msg = UI.Label(v._locked.transform, "You have no chart of this island.\n\nTomas's notes mention a brass compass lost with a small plane\nsomewhere in the western jungle.", 24, UI.TextCol, TextAnchor.MiddleCenter);
            msg.rectTransform.Stretch(30, 30, 30, 30); msg.lineSpacing = 1.2f;
            return v;
        }

        void BuildMarks()
        {
            var v = this;
            foreach (var l in Landmark.All)
            {
                var dot = UI.Img(v._map, UI.Disc, UI.Accent, "Dot"); dot.rectTransform.sizeDelta = new Vector2(16, 16);
                var a = new Vector2(l.transform.position.x / World.Size, l.transform.position.z / World.Size);
                dot.rectTransform.anchorMin = dot.rectTransform.anchorMax = a; dot.rectTransform.anchoredPosition = Vector2.zero;
                var label = UI.Label(dot.transform, l.Title, 17, UI.TextCol, TextAnchor.MiddleCenter, FontStyle.Bold).Shadowed(0.9f, 2);
                label.rectTransform.Pin(new Vector2(0.5f, 1), new Vector2(0.5f, 0), new Vector2(0, 3), new Vector2(260, 22)); label.horizontalOverflow = HorizontalWrapMode.Overflow;
                v._marks.Add((l, dot, label));
            }
            _player.transform.SetAsLastSibling();
        }

        public void Refresh()
        {
            var gm = GameManager.I; bool has = gm.Inv.Count("compass") > 0;
            if (_marks.Count != Landmark.All.Count) BuildMarks();
            _locked.SetActive(!has);
            int seen = 0;
            for (int z = 0; z < 10; z++)
                for (int x = 0; x < 10; x++)
                {
                    bool open = gm.Flags.Contains("map_" + x + "_" + z); if (open) seen++;
                    _fog[x, z].SetActive(!open);
                }
            _info.text = UI.Spaced("PLACES  " + gm.DiscoveredCount + " / " + Landmark.All.Count + "     EXPLORED  " + seen + "%");
            foreach (var (l, dot, label) in _marks)
            {
                bool found = l != null && l.Found;
                dot.color = found ? UI.Accent : new Color(1, 1, 1, 0.0f); label.gameObject.SetActive(found);
            }
            Vector3 p = gm.Player.transform.position;
            _player.rectTransform.anchorMin = _player.rectTransform.anchorMax = new Vector2(p.x / World.Size, p.z / World.Size);
            _player.rectTransform.anchoredPosition = Vector2.zero; _player.rectTransform.localRotation = Quaternion.Euler(0, 0, 180 - gm.Player.Yaw);
        }
    }
}
