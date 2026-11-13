// v4 (part f): blob shadows and wet sand were drawing but too faint to see.
import fs from 'node:fs';
const root = 'C:/Survival Fable 62/Assets/';
function patch(file, edits) {
  let s = fs.readFileSync(root + file, 'utf8').replace(/\r\n/g, '\n');
  for (const [from, to] of edits) { const ok = s.includes(from); console.log((ok ? 'ok   ' : 'MISS ') + file + ': ' + from.slice(0, 60).replace(/\n/g, ' ')); if (ok) s = s.replace(from, to); }
  fs.writeFileSync(root + file, s);
}
patch('Shaders/Blob.shader', [
  ['                float a = 1.0 - smoothstep(0.15, 1.0, length(i.uv));\n                return fixed4(0.02, 0.04, 0.06, a * a * _Strength * i.fade);',
   '                float a = 1.0 - smoothstep(0.1, 1.0, length(i.uv));\n                return fixed4(0.02, 0.035, 0.05, a * _Strength * i.fade);'],
]);
patch('Scripts/Game/BlobShadows.cs', [
  ['_mat.SetFloat("_Strength", Mathf.Lerp(0.18f, 0.5f, gm.Sky.DayFactor)', '_mat.SetFloat("_Strength", Mathf.Lerp(0.3f, 0.62f, gm.Sky.DayFactor)'],
]);
patch('Shaders/WetSand.shader', [
  ['_Strength ("Strength", Range(0, 1)) = 0.46', '_Strength ("Strength", Range(0, 1)) = 0.6'],
  ['_Height ("Film height above sea", Float) = 0.24', '_Height ("Film height above sea", Float) = 0.4'],
  ['                return fixed4(0.10, 0.08, 0.05, a * _Strength);', '                return fixed4(0.07, 0.055, 0.03, a * _Strength);'],
]);
patch('Editor/ProjectSetup.cs', [
  ['m.SetFloat("_Strength", 0.46f); m.SetFloat("_Height", 0.24f);', 'm.SetFloat("_Strength", 0.6f); m.SetFloat("_Height", 0.4f);'],
]);
patch('Scripts/Game/GameManager.cs', [
  ['film.transform.position = sea.transform.position + Vector3.up * 0.24f;', 'film.transform.position = sea.transform.position + Vector3.up * 0.4f;'],
]);
patch('Editor/RenderTest.cs', [
  ['film.transform.position = sea.transform.position + Vector3.up * 0.24f;', 'film.transform.position = sea.transform.position + Vector3.up * 0.4f;'],
  ['Shots.Scene(spawn.x + 3f, spawn.y + 2.5f, spawn.z, 28f, 180f, "Source/Shots/test_wet.jpg");', 'ProjectSetup.BuildExtras();\n            Shots.Scene(spawn.x + 3f, spawn.y + 1.8f, spawn.z - 6f, 32f, 180f, "Source/Shots/test_wet.jpg");'],
  ['            Shots.Scene(spawn.x, spawn.y + 2.2f, spawn.z - 2f, 22f, 0f, "Source/Shots/test_blob.jpg");', '            quad.GetComponent<Renderer>().sharedMaterial.SetFloat("_Strength", 0.62f);\n            Shots.Scene(spawn.x, spawn.y + 2.2f, spawn.z - 2f, 22f, 0f, "Source/Shots/test_blob.jpg");'],
]);
