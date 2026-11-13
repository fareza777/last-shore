using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LastShore
{
    /// <summary>Backpack, crafting and journal in one screen.</summary>
    public class InventoryUI : MonoBehaviour
    {
        public bool Open { get; private set; }

        Canvas _canvas; CanvasGroup _group; RectTransform _window;
        readonly Image[] _slotBg = new Image[Inventory.Size], _slotIcon = new Image[Inventory.Size];
        readonly Text[] _slotCount = new Text[Inventory.Size];
        int _selected = -1;
        Image _detIcon; Text _detName, _detDesc; Button _useBtn, _dropBtn; Text _useLabel;
        RectTransform _craftPanel, _journalPanel, _craftContent; Image _tabCraft, _tabJournal;
        readonly List<(Recipe r, Button btn, Text btnLabel, Text cost, Image bg)> _rows = new List<(Recipe, Button, Text, Text, Image)>();
        Text _jTitle, _jBody, _noteTitle, _noteBody; readonly List<Text> _noteButtons = new List<Text>();
        float _refresh; RectTransform _mapPanel; MapView _mapView; Image _tabMap;
        StorageChest _chest; int _chestSel = -1; RectTransform _chestPanel; Text _jCamp;
        readonly Image[] _cBg = new Image[StorageChest.Size], _cIcon = new Image[StorageChest.Size]; readonly Text[] _cCount = new Text[StorageChest.Size];

        public static InventoryUI Create()
        {
            var canvas = UI.MakeCanvas("InventoryUI", 20);
            var ui = canvas.gameObject.AddComponent<InventoryUI>();
            ui._canvas = canvas; ui.Build();
            canvas.gameObject.SetActive(false);
            return ui;
        }

        void Build()
        {
            _group = _canvas.Group();
            var dim = UI.Img(_canvas.transform, null, new Color(0.01f, 0.03f, 0.05f, 0.72f), "Dim", true); dim.rectTransform.Stretch();
            var safe = UI.SafeArea(_canvas);
            _window = UI.Rect("Window", safe).Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1560, 860));

            // ================= left: backpack
            var left = UI.Img(_window, UI.Rounded, UI.Bg, "Backpack"); left.rectTransform.Anchor(0, 0, 0, 1, Vector2.zero, new Vector2(790, 0));
            Header(left.transform, "BACKPACK", "bag");
            var sort = UI.Btn(left.transform, "SORT", SortPack, 22); ((RectTransform)sort.transform).Pin(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-38, -20), new Vector2(124, 52));
            var grid = UI.Rect("Grid", left.transform).Pin(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -92), new Vector2(714, 600));
            for (int i = 0; i < Inventory.Size; i++)
            {
                int idx = i; int col = i % 6, row = i / 6;
                var bg = UI.Img(grid, UI.Rounded, UI.Slot, "Slot" + i, true);
                bg.rectTransform.Pin(new Vector2(0, 1), new Vector2(0, 1), new Vector2(col * 120, -row * 101 - (row > 0 ? 12 : 0)), new Vector2(110, 93));
                var b = bg.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None; b.onClick.AddListener(() => ClickSlot(idx));
                var ic = UI.Img(bg.transform, null, Color.white, "Icon"); ic.rectTransform.Stretch(12, 12, 12, 12); ic.preserveAspect = true;
                var ct = UI.Label(bg.transform, "", 22, UI.TextCol, TextAnchor.LowerRight, FontStyle.Bold).Shadowed(); ct.rectTransform.Stretch(0, 6, 10, 0);
                if (row == 0) { var k = UI.Label(bg.transform, (i + 1).ToString(), 16, UI.Accent, TextAnchor.UpperLeft, FontStyle.Bold); k.rectTransform.Stretch(10, 0, 0, 6); }
                _slotBg[i] = bg; _slotIcon[i] = ic; _slotCount[i] = ct;
            }
            var hotLine = UI.Img(grid, null, UI.Hex("F2B45A", 0.5f), "HotbarLine"); hotLine.rectTransform.Pin(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -99), new Vector2(710, 2));

            var det = UI.Img(left.transform, UI.Rounded, UI.BgSoft, "Detail"); det.rectTransform.Anchor(0, 0, 1, 0, new Vector2(38, 30), new Vector2(-38, 150));
            _detIcon = UI.Img(det.transform, null, Color.white, "Icon"); _detIcon.rectTransform.Pin(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(92, 92)); _detIcon.preserveAspect = true;
            _detName = UI.Label(det.transform, "", 28, UI.TextCol, TextAnchor.UpperLeft, FontStyle.Bold); _detName.rectTransform.Anchor(0, 0.5f, 1, 1, new Vector2(124, 0), new Vector2(-250, -12));
            _detDesc = UI.Label(det.transform, "Select an item.", 21, UI.Dim, TextAnchor.UpperLeft); _detDesc.rectTransform.Anchor(0, 0, 1, 0.56f, new Vector2(124, 8), new Vector2(-250, 0));
            _useBtn = UI.Btn(det.transform, "USE", UseSelected, 24, true); ((RectTransform)_useBtn.transform).Pin(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-132, 0), new Vector2(104, 60));
            _useLabel = _useBtn.GetComponentInChildren<Text>();
            _dropBtn = UI.Btn(det.transform, "DROP", DropSelected, 24); ((RectTransform)_dropBtn.transform).Pin(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(104, 60));

            // ================= right: tabs
            var right = UI.Img(_window, UI.Rounded, UI.Bg, "Right"); right.rectTransform.Anchor(0, 0, 1, 1, new Vector2(810, 0), Vector2.zero);
            _tabCraft = Tab(right.transform, "CRAFTING", 0, () => ShowTab(true));
            _tabJournal = Tab(right.transform, "JOURNAL", 1, () => ShowTab(false));
            _tabMap = Tab(right.transform, "MAP", 2, () => ShowTabIndex(2));
            var close = UI.IconBtn(right.transform, "close", Close, 56); ((RectTransform)close.transform).Pin(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-18, -16), new Vector2(56, 56));

            _craftPanel = UI.Rect("Craft", right.transform).Stretch(26, 24, 26, 96);
            var view = UI.Rect("Viewport", _craftPanel).Stretch(); view.gameObject.AddComponent<RectMask2D>();
            var viewImg = view.gameObject.AddComponent<Image>(); viewImg.color = new Color(0, 0, 0, 0.001f);
            _craftContent = UI.Rect("Content", view); _craftContent.anchorMin = new Vector2(0, 1); _craftContent.anchorMax = new Vector2(1, 1); _craftContent.pivot = new Vector2(0.5f, 1); _craftContent.sizeDelta = Vector2.zero;
            var vl = _craftContent.gameObject.AddComponent<VerticalLayoutGroup>(); vl.spacing = 10; vl.childForceExpandHeight = false; vl.childControlHeight = true; vl.childControlWidth = true; vl.childForceExpandWidth = true;
            _craftContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var sr = _craftPanel.gameObject.AddComponent<ScrollRect>(); sr.viewport = view; sr.content = _craftContent; sr.horizontal = false; sr.scrollSensitivity = 40; sr.movementType = ScrollRect.MovementType.Clamped;
            string group = null;
            foreach (var r in ItemDB.Recipes)
            {
                if (r.Group != group)
                {
                    group = r.Group;
                    var gh = UI.Label(_craftContent, UI.Spaced(group.ToUpperInvariant()), 18, UI.Accent, TextAnchor.LowerLeft, FontStyle.Bold);
                    gh.gameObject.AddComponent<LayoutElement>().preferredHeight = 34;
                }
                Row(r);
            }

            _chestPanel = UI.Rect("Chest", right.transform).Stretch(26, 24, 26, 96);
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
            var chNote = UI.Label(_chestPanel, "Tap an item, then tap a slot in the other grid to move it.\nSTORE ALL keeps your hotbar and moves everything else.", 21, UI.Dim, TextAnchor.UpperLeft);
            chNote.rectTransform.Anchor(0, 1, 1, 1, new Vector2(0, -530), new Vector2(0, -456)); chNote.lineSpacing = 1.2f;
            _chestPanel.gameObject.SetActive(false);

            _mapPanel = UI.Rect("Map", right.transform).Stretch(26, 24, 26, 96); _mapView = MapView.Build(_mapPanel);
            _journalPanel = UI.Rect("Journal", right.transform).Stretch(30, 24, 30, 96);
            _jTitle = UI.Label(_journalPanel, "", 20, UI.Accent, TextAnchor.UpperLeft, FontStyle.Bold); _jTitle.rectTransform.Anchor(0, 1, 1, 1, new Vector2(0, -30), Vector2.zero);
            _jBody = UI.Label(_journalPanel, "", 24, UI.TextCol, TextAnchor.UpperLeft); _jBody.rectTransform.Anchor(0, 1, 1, 1, new Vector2(0, -110), new Vector2(0, -34));
            var nh = UI.Label(_journalPanel, UI.Spaced("PAGES FOUND"), 18, UI.Accent, TextAnchor.UpperLeft, FontStyle.Bold); nh.rectTransform.Anchor(0, 1, 1, 1, new Vector2(0, -214), new Vector2(0, -184));
            _jCamp = UI.Label(_journalPanel, "", 19, UI.Dim, TextAnchor.UpperLeft); _jCamp.rectTransform.Anchor(0, 1, 1, 1, new Vector2(0, -176), new Vector2(0, -118)); _jCamp.lineSpacing = 1.15f;
            for (int i = 0; i < Story.Notes.Length; i++)
            {
                int idx = i;
                var b = UI.Btn(_journalPanel, "· · ·", () => SelectNote(idx), 20, false, "Note" + i);   // needs a label, or the button has no Text to fill in
                ((RectTransform)b.transform).Pin(new Vector2(0, 1), new Vector2(0, 1), new Vector2((i % 3) * 232, -224 - (i / 3) * 62), new Vector2(222, 52));
                _noteButtons.Add(b.GetComponentInChildren<Text>());
            }
            var paper = UI.Img(_journalPanel, UI.Rounded, UI.Hex("EFE4C6"), "Paper"); paper.rectTransform.Anchor(0, 0, 1, 1, new Vector2(0, 0), new Vector2(0, -360));
            _noteTitle = UI.Label(paper.transform, "", 28, UI.Hex("3B2F22"), TextAnchor.UpperLeft, FontStyle.Bold, true); _noteTitle.rectTransform.Anchor(0, 1, 1, 1, new Vector2(30, -62), new Vector2(-30, -20));
            _noteBody = UI.Label(paper.transform, "", 24, UI.Hex("4A3D2E"), TextAnchor.UpperLeft, FontStyle.Italic); _noteBody.rectTransform.Stretch(30, 24, 30, 76); _noteBody.lineSpacing = 1.25f;
        }

        void Header(Transform parent, string title, string icon)
        {
            var ic = UI.Img(parent, UI.Icon(icon), UI.Accent, "HeaderIcon"); ic.rectTransform.Pin(new Vector2(0, 1), new Vector2(0, 1), new Vector2(38, -26), new Vector2(38, 38));
            var t = UI.Label(parent, UI.Spaced(title, 2), 30, UI.TextCol, TextAnchor.MiddleLeft, FontStyle.Bold, true); t.rectTransform.Pin(new Vector2(0, 1), new Vector2(0, 1), new Vector2(92, -24), new Vector2(500, 44));
        }

        Image Tab(Transform parent, string label, int index, System.Action onClick)
        {
            var b = UI.Btn(parent, label, onClick, 24);
            ((RectTransform)b.transform).Pin(new Vector2(0, 1), new Vector2(0, 1), new Vector2(26 + index * 196, -18), new Vector2(188, 58));
            Destroy(b.GetComponent<ButtonFX>());
            return b.GetComponent<Image>();
        }

        void Row(Recipe r)
        {
            var def = ItemDB.Get(r.Result);
            var bg = UI.Img(_craftContent, UI.Rounded, UI.Slot, "Recipe_" + r.Result);
            bg.gameObject.AddComponent<LayoutElement>().preferredHeight = 96;
            var ic = UI.Img(bg.transform, def.Icon, Color.white, "Icon"); ic.rectTransform.Pin(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(74, 74)); ic.preserveAspect = true;
            var name = UI.Label(bg.transform, def.Name, 25, UI.TextCol, TextAnchor.UpperLeft, FontStyle.Bold); name.rectTransform.Anchor(0, 0.5f, 1, 1, new Vector2(100, 0), new Vector2(-170, -12));
            var cost = UI.Label(bg.transform, "", 20, UI.Dim, TextAnchor.UpperLeft); cost.rectTransform.Anchor(0, 0, 1, 0.52f, new Vector2(100, 6), new Vector2(-170, 0)); cost.horizontalOverflow = HorizontalWrapMode.Overflow;
            var btn = UI.Btn(bg.transform, "CRAFT", () => Craft(r), 22, true); ((RectTransform)btn.transform).Pin(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-14, 0), new Vector2(144, 60));
            _rows.Add((r, btn, btn.GetComponentInChildren<Text>(), cost, btn.GetComponent<Image>()));
        }

        // ---------------------------------------------------------------- open / close
        public void Toggle() { if (Open) Close(); else Show(true); }
        public void OpenCrafting() { Show(true); }

        /// <summary>Opens the backpack next to a storage chest.</summary>
        public void OpenChest(StorageChest chest)
        {
            Show(true);
            _chest = chest; _chestSel = -1; _selected = -1;
            ShowTabIndex(3); Refresh();
        }

        void Show(bool craftTab)
        {
            if (!Open)
            {
                Open = true; _canvas.gameObject.SetActive(true); GameManager.I.Hud.SetHotbarVisible(false);
                GameManager.I.Inv.Changed -= Refresh; GameManager.I.Inv.Changed += Refresh;
                AudioManager.I.UI("ui_open");
                _group.alpha = 0; Tween.Fade(_group, 1, 0.2f);
                _window.localScale = Vector3.one * 0.94f; Tween.Scale(_window, Vector3.one, 0.3f, Ease.OutBack);
            }
            ShowTab(craftTab); Refresh();
        }

        public void Close()
        {
            if (!Open) return;
            Open = false; _selected = -1; _chest = null; _chestSel = -1; GameManager.I.Hud.SetHotbarVisible(true);
            GameManager.I.Inv.Changed -= Refresh;
            Tween.Fade(_group, 0, 0.15f, 0, () => { if (!Open) _canvas.gameObject.SetActive(false); });
        }

        public void ShowNote(int index) { Show(false); SelectNote(index); }

        void ShowTab(bool craft) { ShowTabIndex(craft ? 0 : 1); }

        void ShowTabIndex(int tab)
        {
            _craftPanel.gameObject.SetActive(tab == 0); _journalPanel.gameObject.SetActive(tab == 1); _mapPanel.gameObject.SetActive(tab == 2);
            _chestPanel.gameObject.SetActive(tab == 3); if (tab != 3) { _chest = null; _chestSel = -1; }
            Color on = UI.Hex("3A2C14", 0.95f);
            _tabCraft.color = tab == 0 ? on : UI.BgSoft; _tabJournal.color = tab == 1 ? on : UI.BgSoft; _tabMap.color = tab == 2 ? on : UI.BgSoft;
            if (tab == 1) RefreshJournal();
            if (tab == 2) _mapView.Refresh();
        }

        void Update()
        {
            if (!Open) return;
            _refresh -= Time.unscaledDeltaTime;
            if (_refresh <= 0) { _refresh = 0.5f; RefreshRecipes(); }
        }

        // ---------------------------------------------------------------- backpack
        void ClickSlot(int i)
        {
            var inv = GameManager.I.Inv;
            if (i >= inv.Unlocked) { GameManager.I.Hud.Hint("Craft a Hide Pack to carry more."); return; }
            if (_chest != null && _chestSel >= 0)
            {
                Exchange(_chest.Slots, _chestSel, inv.Slots, i); _chestSel = -1;
                AudioManager.I.UI("pickup", 0.4f); inv.NotifyChanged(); Refresh(); return;
            }
            if (_selected == i) _selected = -1;
            else if (_selected >= 0 && !inv.Slots[_selected].Empty) { inv.Swap(_selected, i); _selected = -1; AudioManager.I.UI("pickup", 0.4f); }
            else _selected = inv.Slots[i].Empty ? -1 : i;
            Refresh();
        }

        // ---------------------------------------------------------------- storage chest
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

        void UseSelected()
        {
            if (_selected < 0) return;
            var gm = GameManager.I; var s = gm.Inv.Slots[_selected];
            if (s.Empty) return;
            var def = s.Def;
            if (def.Kind == ItemKind.Food || def.Kind == ItemKind.Drink || def.Kind == ItemKind.Medical) gm.Interaction.Consume(_selected);
            else
            {
                // equip: move to the active hotbar slot
                if (_selected != gm.Interaction.Selected) gm.Inv.Swap(_selected, gm.Interaction.Selected);
                gm.Interaction.Select(gm.Interaction.Selected);
                _selected = -1; Close(); return;
            }
            if (gm.Inv.Slots[_selected].Empty) _selected = -1;
            Refresh();
        }

        void DropSelected()
        {
            if (_selected < 0) return;
            var gm = GameManager.I; var s = gm.Inv.Slots[_selected];
            if (s.Empty) return;
            var p = gm.Player.transform;
            var drop = Pickup.Spawn(s.id, 1, p.position + p.forward * 1.2f + Vector3.up * 0.3f);
            if (s.Def.Durability > 0) drop.Count = 1;
            gm.Inv.RemoveAt(_selected, 1);
            if (gm.Inv.Slots[_selected].Empty) _selected = -1;
            Refresh();
        }

        void Refresh()
        {
            if (!Open) return;
            var inv = GameManager.I.Inv;
            for (int i = 0; i < Inventory.Size; i++)
            {
                var s = inv.Slots[i];
                _slotIcon[i].enabled = !s.Empty; if (!s.Empty) _slotIcon[i].sprite = s.Def.Icon;
                _slotCount[i].text = !s.Empty && s.count > 1 ? s.count.ToString() : !s.Empty && s.Def.Durability > 0 ? Mathf.CeilToInt(100f * s.durability / s.Def.Durability) + "%" : "";
                _slotBg[i].color = i >= inv.Unlocked ? UI.Hex("0A1118", 0.55f) : i == _selected ? UI.Hex("5A4320", 0.98f) : UI.Slot;
            }
            if (_chest != null)
                for (int i = 0; i < StorageChest.Size; i++)
                {
                    var s = _chest.Slots[i];
                    _cIcon[i].enabled = !s.Empty; if (!s.Empty) _cIcon[i].sprite = s.Def.Icon;
                    _cCount[i].text = !s.Empty && s.count > 1 ? s.count.ToString() : !s.Empty && s.Def.Durability > 0 ? Mathf.CeilToInt(100f * s.durability / s.Def.Durability) + "%" : "";
                    _cBg[i].color = i == _chestSel ? UI.Hex("5A4320", 0.98f) : UI.Slot;
                }
            var sel = _selected >= 0 ? inv.Slots[_selected] : null;
            bool has = sel != null && !sel.Empty;
            _detIcon.enabled = has; _useBtn.gameObject.SetActive(has); _dropBtn.gameObject.SetActive(has);
            if (has)
            {
                var d = sel.Def; _detIcon.sprite = d.Icon; _detName.text = d.Name; _detDesc.text = d.Desc;
                _useBtn.gameObject.SetActive(d.Kind != ItemKind.Special && d.Kind != ItemKind.Resource);
                _useLabel.text = d.Kind == ItemKind.Food ? "EAT" : d.Kind == ItemKind.Drink ? "DRINK" : d.Kind == ItemKind.Medical ? "USE" : "EQUIP";
            }
            else { _detName.text = ""; _detDesc.text = "Select an item. Select a second slot to move it."; }
            RefreshRecipes();
        }

        // ---------------------------------------------------------------- crafting
        void RefreshRecipes()
        {
            var gm = GameManager.I; var inv = gm.Inv; Vector3 pos = gm.Player.transform.position;
            foreach (var row in _rows)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var c in row.r.Cost)
                {
                    int have = inv.Count(c.id); bool ok = have >= c.n;
                    sb.Append(ok ? "<color=#7DCB70>" : "<color=#E2584C>").Append(Mathf.Min(have, c.n)).Append('/').Append(c.n).Append("</color> ").Append(ItemDB.Get(c.id).Name).Append("    ");
                }
                bool station = Stations.Near(row.r.Station, pos);
                if (!station) sb.Append("<color=#F2B45A>Needs ").Append(Stations.Name(row.r.Station)).Append("</color>");
                row.cost.text = sb.ToString();
                bool can = station && inv.CanCraft(row.r);
                row.btn.interactable = can;
                row.bg.color = can ? UI.Accent : UI.Hex("2B3642", 0.9f);
                row.btnLabel.color = can ? UI.Hex("1A1206") : UI.Dim;
                row.btnLabel.text = row.r.Station == Station.Campfire || row.r.Station == Station.Pot ? "COOK" : "CRAFT";
                var fx = row.btn.GetComponent<ButtonFX>(); if (fx != null) { fx.Normal = row.bg.color; fx.enabled = can; }
            }
        }

        void Craft(Recipe r)
        {
            var gm = GameManager.I;
            if (!Stations.Near(r.Station, gm.Player.transform.position) || !gm.Inv.Craft(r)) return;
            AudioManager.I.Play(r.Station == Station.Campfire ? "ignite" : r.Station == Station.Pot ? "bubble" : "craft", 0.8f);
            gm.Quests.Notify("craft", r.Result);
            Refresh();
        }

        // ---------------------------------------------------------------- journal
        void RefreshJournal()
        {
            var gm = GameManager.I;
            _jTitle.text = UI.Spaced("CURRENT OBJECTIVE  ·  " + gm.Quests.Title);
            _jBody.text = gm.Quests.Body;
            _jCamp.text = Camp.Summary + "\nPLACES  ·  " + gm.DiscoveredCount + " of " + Landmark.All.Count + " discovered";
            for (int i = 0; i < _noteButtons.Count; i++)
            {
                bool found = gm.Notes.Contains(i);
                _noteButtons[i].text = found ? Story.Notes[i].title : "· · ·";
                _noteButtons[i].color = found ? UI.TextCol : UI.Dim;
            }
            if (string.IsNullOrEmpty(_noteTitle.text))
            {
                if (gm.Notes.Count > 0) SelectNote(gm.Notes[gm.Notes.Count - 1]);
                else { _noteTitle.text = "No pages yet"; _noteBody.text = "Weathered pages left by someone before you may still be scattered across the island."; }
            }
        }

        void SelectNote(int i)
        {
            if (!GameManager.I.Notes.Contains(i)) return;
            _noteTitle.text = Story.Notes[i].title; _noteBody.text = Story.Notes[i].body;
            RefreshJournal();
            Tween.Punch(_noteTitle.transform.parent, 0.03f, 0.25f);
        }
    }
}
