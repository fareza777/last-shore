import fs from 'node:fs';
const root = 'C:/Survival Fable 62/Assets/';
function patch(file, edits) {
  let s = fs.readFileSync(root + file, 'utf8').replace(/\r\n/g, '\n');
  for (const [from, to] of edits) { const ok = s.includes(from); console.log((ok ? 'ok   ' : 'MISS ') + file + ': ' + from.slice(0, 50)); if (ok) s = s.replace(from, to); }
  fs.writeFileSync(root + file, s);
}
patch('Scripts/UI/HUD.cs', [
  ['            var bar = UI.Rect("Hotbar", _safe)', '            var bar = _hotbar = UI.Rect("Hotbar", _safe)'],
  ['        public void SetVisible(bool visible)', '        RectTransform _hotbar;\n        public void SetHotbarVisible(bool visible) { _hotbar.gameObject.SetActive(visible); }\n        public void SetVisible(bool visible)'],
]);
patch('Scripts/UI/InventoryUI.cs', [
  ['                Open = true; _canvas.gameObject.SetActive(true);', '                Open = true; _canvas.gameObject.SetActive(true); GameManager.I.Hud.SetHotbarVisible(false);'],
  ['            Open = false; _selected = -1;', '            Open = false; _selected = -1; GameManager.I.Hud.SetHotbarVisible(true);'],
]);
