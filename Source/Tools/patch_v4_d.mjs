// v4 (part d): phone-sized quality tiers, blob shadow fix, brighter intro, stronger wet sand.
import fs from 'node:fs';
const root = 'C:/Survival Fable 62/Assets/';
function patch(file, edits) {
  let s = fs.readFileSync(root + file, 'utf8').replace(/\r\n/g, '\n');
  for (const [from, to] of edits) { const ok = s.includes(from); console.log((ok ? 'ok   ' : 'MISS ') + file + ': ' + from.slice(0, 60).replace(/\n/g, ' ')); if (ok) s = s.replace(from, to); }
  fs.writeFileSync(root + file, s);
}
patch('Shaders/Blob.shader', [
  ['                float a = smoothstep(1.0, 0.15, length(i.uv));', '                float a = 1.0 - smoothstep(0.15, 1.0, length(i.uv));'],
]);
patch('Shaders/WetSand.shader', [
  ['_Strength ("Strength", Range(0, 1)) = 0.34', '_Strength ("Strength", Range(0, 1)) = 0.46'],
]);
patch('Editor/ProjectSetup.cs', [
  ['m.SetFloat("_Strength", 0.34f); m.SetFloat("_Height", 0.24f);', 'm.SetFloat("_Strength", 0.46f); m.SetFloat("_Height", 0.24f);'],
]);
patch('Scripts/Core/GameSettings.cs', [
  ['        public static event Action Changed;', `        public static event Action Changed;
        /// <summary>Real shadow maps, HDR and post-processing. On phones only the High tier can afford them.</summary>
        public static bool FullLighting => Quality == 2 || (Quality == 1 && !Application.isMobilePlatform);`],
  [`                urp.shadowDistance = Quality == 0 ? 1f : Quality == 1 ? (mobile ? 32f : 60f) : 120f;
                urp.shadowCascadeCount = Quality == 2 ? 3 : 1;
                urp.renderScale = Quality == 0 ? (mobile ? 0.6f : 0.8f) : Quality == 1 ? (mobile ? 0.75f : 0.9f) : 1f;
                urp.supportsHDR = Quality > 0;`,
   `                urp.shadowDistance = !FullLighting ? 1f : mobile ? 32f : Quality == 1 ? 60f : 120f;
                urp.shadowCascadeCount = Quality == 2 && !mobile ? 3 : 1;
                urp.renderScale = Quality == 0 ? (mobile ? 0.6f : 0.8f) : Quality == 1 ? (mobile ? 0.8f : 0.9f) : (mobile ? 0.75f : 1f);
                urp.supportsHDR = FullLighting;`],
]);
patch('Scripts/Game/GameManager.cs', [
  ['                t.detailObjectDensity = q == 0 ? 0f : q == 1 ? 0.55f : 1f; t.detailObjectDistance = q == 0 ? 20 : q == 1 ? 50 : 80;',
   '                bool phone = Application.isMobilePlatform;\n                t.detailObjectDensity = q == 0 ? 0f : q == 1 ? (phone ? 0.4f : 0.55f) : (phone ? 0.6f : 1f); t.detailObjectDistance = q == 0 ? 20 : q == 1 ? (phone ? 34 : 50) : (phone ? 50 : 80);'],
  ['                t.shadowCastingMode = q == 0 ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;', '                t.shadowCastingMode = GameSettings.FullLighting ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;'],
  ['RenderSettings.sun.shadows = q == 0 ? LightShadows.None : LightShadows.Soft;', 'RenderSettings.sun.shadows = GameSettings.FullLighting ? LightShadows.Soft : LightShadows.None;'],
  ['                data.renderPostProcessing = q > 0;\n                data.antialiasing = q == 0 ? AntialiasingMode.None :', '                data.renderPostProcessing = GameSettings.FullLighting;\n                data.antialiasing = !GameSettings.FullLighting ? AntialiasingMode.None :'],
]);
patch('Scripts/Game/BlobShadows.cs', [
  ['            if (GameSettings.Quality != 0 || gm == null', '            if (GameSettings.FullLighting || gm == null'],
]);
patch('Scripts/UI/IntroCinematic.cs', [
  ['            float lift = GameSettings.Quality == 0 ? 3.2f : GameSettings.Quality == 1 && Application.isMobilePlatform ? 1.8f : 1f;', '            float lift = GameSettings.FullLighting ? 1f : 4.5f;'],
]);
