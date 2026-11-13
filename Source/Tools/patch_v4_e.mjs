// v4 (part e): lighter phone Medium, brighter intro.
import fs from 'node:fs';
const root = 'C:/Survival Fable 62/Assets/';
function patch(file, edits) {
  let s = fs.readFileSync(root + file, 'utf8').replace(/\r\n/g, '\n');
  for (const [from, to] of edits) { const ok = s.includes(from); console.log((ok ? 'ok   ' : 'MISS ') + file + ': ' + from.slice(0, 60).replace(/\n/g, ' ')); if (ok) s = s.replace(from, to); }
  fs.writeFileSync(root + file, s);
}
patch('Scripts/Core/GameSettings.cs', [
  ['Quality == 1 ? (mobile ? 0.8f : 0.9f) : (mobile ? 0.75f : 1f);', 'Quality == 1 ? (mobile ? 0.7f : 0.9f) : (mobile ? 0.75f : 1f);'],
  ['            Shader.SetGlobalFloat("_LS_Lite", Quality == 0 ? 1f : 0f);', '            Shader.SetGlobalFloat("_LS_Lite", FullLighting ? 0f : 1f);   // the grain texture costs three samples per pixel'],
]);
patch('Scripts/Game/GameManager.cs', [
  ['q == 1 ? (phone ? 0.4f : 0.55f)', 'q == 1 ? (phone ? 0.3f : 0.55f)'],
  ['q == 1 ? (phone ? 34 : 50)', 'q == 1 ? (phone ? 28 : 50)'],
]);
patch('Scripts/UI/IntroCinematic.cs', [
  ['float lift = GameSettings.FullLighting ? 1f : 4.5f;', 'float lift = GameSettings.FullLighting ? 1f : 7f;'],
]);
