// v3: performance (single shared material, quality tiers), floating thumb stick, HUD layout, clearer inventory icon, water normals.
import fs from 'node:fs';
const root = 'C:/Survival Fable 62/Assets/';
const results = [];
function patch(file, edits) {
  let s = fs.readFileSync(root + file, 'utf8').replace(/\r\n/g, '\n');
  for (const [from, to] of edits) {
    const ok = s.includes(from);
    results.push((ok ? 'ok   ' : 'MISS ') + file + ': ' + from.slice(0, 56).replace(/\n/g, ' '));
    if (ok) s = s.replace(from, to);
  }
  fs.writeFileSync(root + file, s);
}

// ---------------------------------------------------------------- editor: textures and materials
patch('Editor/ProjectSetup.cs', [
  ['            SaveTex("Noise", 256,', `            SaveTex("WaterNormal", 256, (u, v) =>
            {
                const float e = 1f / 256f;
                float h = Fbm(u, v, 4, 5, 321);
                float dx = Fbm(u + e, v, 4, 5, 321) - Fbm(u - e, v, 4, 5, 321), dz = Fbm(u, v + e, 4, 5, 321) - Fbm(u, v - e, 4, 5, 321);
                return new Color(Mathf.Clamp01(0.5f + dx * 9f), Mathf.Clamp01(0.5f + dz * 9f), h, 1);
            });
            { var wti = (TextureImporter)AssetImporter.GetAtPath(TexDir + "/WaterNormal.png"); wti.sRGBTexture = false; wti.SaveAndReimport(); }
            SaveTex("Noise", 256,`],
  ['                    m.SetFloat("_WindAmount", 1f); m.SetFloat("_Cull", dbl ? 0 : 2); m.doubleSidedGI = dbl; m.enableInstancing = true;\n                });',
   `                    m.SetFloat("_WindAmount", 1f); m.SetFloat("_Cull", dbl ? 0 : 2); m.doubleSidedGI = dbl; m.enableInstancing = true; m.SetFloat("_VertexColor", 0f);
                });`],
  ['            var glow = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/Glow.png");', `            // every model shares this one material: colour and occlusion are baked into vertex colours
            MakeMat(MatDir, "Shared", "LastShore/Lit", m =>
            {
                m.SetColor("_BaseColor", Color.white); m.SetColor("_Emission", Color.black); m.SetFloat("_VertexColor", 1f); m.SetFloat("_Cull", 0f);
                m.SetTexture("_DetailTex", AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/Noise.png")); m.SetFloat("_Detail", 0.38f); m.SetFloat("_DetailScale", 0.9f);
                m.SetFloat("_WindAmount", 1f); m.doubleSidedGI = true; m.enableInstancing = true;
            });
            var glow = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/Glow.png");`],
  ['            MakeMat("Assets/Materials", "TerrainMat",', '            RefreshWater();\n            MakeMat("Assets/Materials", "TerrainMat",'],
  ['var noise = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/Noise.png");\n            foreach (var name in new[] { "Water", "WaterStorm", "WaterPool" })', 'var noise = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/WaterNormal.png");\n            foreach (var name in new[] { "Water", "WaterStorm", "WaterPool" })'],
]);

// ---------------------------------------------------------------- editor: four terrain layers (one terrain pass on mobile)
patch('Editor/WorldBuilder.cs', [
  ['string[] tex = { "Sand", "Grass", "Rock", "Dirt", "Forest" }; float[] tile = { 6, 7, 7, 7, 6 };', 'string[] tex = { "Sand", "Grass", "Rock", "Forest" }; float[] tile = { 6, 7, 7, 6 };'],
  ['var map = new float[ARes, ARes, 5];', 'var map = new float[ARes, ARes, 4];'],
  ['map[z, x, 2] = rock / sum; map[z, x, 3] = dirt / sum; map[z, x, 4] = forest / sum;', 'map[z, x, 2] = rock / sum; map[z, x, 3] = (dirt + forest) / sum;'],
  ['new Color(0.56f, 0.55f, 0.53f), new Color(0.56f, 0.47f, 0.32f), new Color(0.22f, 0.44f, 0.22f) };', 'new Color(0.56f, 0.55f, 0.53f), new Color(0.26f, 0.46f, 0.24f) };'],
  ['c = Color.black; for (int l = 0; l < 5; l++) c += pal[l] * alpha[az, ax, l];', 'c = Color.black; for (int l = 0; l < 4; l++) c += pal[l] * alpha[az, ax, l];'],
]);

// ---------------------------------------------------------------- runtime: quality tiers
patch('Scripts/Core/GameSettings.cs', [
  ['Quality = PlayerPrefs.GetInt("s_quality", Application.isMobilePlatform ? 1 : 2);', 'Quality = PlayerPrefs.GetInt("s_quality3", Application.isMobilePlatform ? 0 : 2);'],
  ['PlayerPrefs.SetInt("s_quality", Quality);', 'PlayerPrefs.SetInt("s_quality3", Quality);'],
  ['                urp.shadowDistance = Quality == 0 ? 40f : Quality == 1 ? 75f : 120f;\n                urp.renderScale = Quality == 0 ? 0.75f : Quality == 1 ? 0.9f : 1f;',
   `                bool mobile = Application.isMobilePlatform;
                urp.shadowDistance = Quality == 0 ? 1f : Quality == 1 ? (mobile ? 32f : 60f) : 120f;
                urp.shadowCascadeCount = Quality == 2 ? 3 : 1;
                urp.renderScale = Quality == 0 ? (mobile ? 0.6f : 0.8f) : Quality == 1 ? (mobile ? 0.75f : 0.9f) : 1f;
                urp.supportsHDR = Quality > 0;`],
  ['            QualitySettings.lodBias = Quality == 0 ? 0.8f : Quality == 1 ? 1f : 1.5f;', '            QualitySettings.lodBias = Quality == 0 ? 0.6f : Quality == 1 ? 0.9f : 1.5f;\n            Shader.SetGlobalFloat("_LS_Lite", Quality == 0 ? 1f : 0f);'],
]);
patch('Scripts/Game/GameManager.cs', [
  ['using UnityEngine;\n', 'using UnityEngine;\nusing UnityEngine.Rendering.Universal;\n'],
  [`            var t = Terrain.activeTerrain; if (t == null) return; int q = GameSettings.Quality;
            t.detailObjectDensity = q == 0 ? 0.35f : q == 1 ? 0.65f : 1f; t.detailObjectDistance = q == 0 ? 35 : q == 1 ? 55 : 80;
            t.heightmapPixelError = q == 0 ? 14 : q == 1 ? 9 : 5; t.basemapDistance = q == 0 ? 70 : q == 1 ? 130 : 220;`,
   `            int q = GameSettings.Quality;
            var t = Terrain.activeTerrain;
            if (t != null)
            {
                t.detailObjectDensity = q == 0 ? 0f : q == 1 ? 0.55f : 1f; t.detailObjectDistance = q == 0 ? 20 : q == 1 ? 50 : 80;
                t.heightmapPixelError = q == 0 ? 22 : q == 1 ? 10 : 5; t.basemapDistance = q == 0 ? 30 : q == 1 ? 110 : 220;
                t.shadowCastingMode = q == 0 ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
            }
            if (RenderSettings.sun != null) RenderSettings.sun.shadows = q == 0 ? LightShadows.None : LightShadows.Soft;
            if (Player != null && Player.Cam != null)
            {
                Player.Cam.farClipPlane = q == 0 ? 520f : q == 1 ? 1000f : 1800f;
                var data = Player.Cam.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = q > 0;
                data.antialiasing = q == 0 ? AntialiasingMode.None : Application.isMobilePlatform ? AntialiasingMode.FastApproximateAntialiasing : AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            }`],
]);
patch('Scripts/Game/EventDirector.cs', [
  ['float q = GameSettings.Quality == 0 ? 0.62f : GameSettings.Quality == 1 ? 0.8f : 1f;', 'float q = GameSettings.Quality == 0 ? 0.5f : GameSettings.Quality == 1 ? 0.78f : 1f;'],
]);

// ---------------------------------------------------------------- HUD: vitals move to the top-left, clear of the thumb stick
patch('Scripts/UI/HUD.cs', [
  ['var vit = UI.Rect("Vitals", _safe).Pin(Vector2.zero, Vector2.zero, new Vector2(40, 36), new Vector2(340, 190));', 'var vit = UI.Rect("Vitals", _safe).Pin(new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -156), new Vector2(340, 190));'],
  ['_status.rectTransform.Pin(Vector2.zero, Vector2.zero, new Vector2(4, 178), new Vector2(600, 28));', '_status.alignment = TextAnchor.UpperLeft; _status.rectTransform.Pin(Vector2.zero, new Vector2(0, 1), new Vector2(-6, -20), new Vector2(700, 28));'],
]);

// ---------------------------------------------------------------- touch stick: appears where the thumb lands, gone when released
patch('Scripts/UI/Menus.cs', [
  ['            Tween.Move(Knob, Vector2.zero, 0.15f, Ease.OutBack); Tween.Move(Base, _rest, 0.25f); Tween.Fade(Visual, 0.45f, 0.3f); Tween.Scale(Base, Vector3.one, 0.2f, Ease.OutCubic);',
   '            Knob.anchoredPosition = Vector2.zero; Tween.Fade(Visual, 0f, 0.18f); Tween.Scale(Base, Vector3.one, 0.2f, Ease.OutCubic);'],
  ['stickArea.rectTransform.Anchor(0, 0, 0.38f, 0.72f, new Vector2(0, 230), Vector2.zero);', 'stickArea.rectTransform.Anchor(0, 0, 0.42f, 0.62f, Vector2.zero, Vector2.zero);'],
  ['stick.Visual = baseRt.Group(); stick.Visual.alpha = 0.45f; stick.Visual.blocksRaycasts = false;', 'stick.Visual = baseRt.Group(); stick.Visual.alpha = 0f; stick.Visual.blocksRaycasts = false;'],
  ['look.rectTransform.Anchor(0.38f, 0, 1, 1, Vector2.zero, Vector2.zero);', 'look.rectTransform.Anchor(0.42f, 0, 1, 1, Vector2.zero, Vector2.zero);'],
]);

// ---------------------------------------------------------------- icons: the backpack button is a grid of slots, not a padlock
{
  let s = fs.readFileSync(root + 'Scripts/UI/UIKit.cs', 'utf8').replace(/\r\n/g, '\n');
  const a = s.indexOf('                case "bag":'); const b = s.indexOf('                case "hammer":');
  if (a > 0 && b > a) {
    s = s.slice(0, a) + `                case "bag":
                    return Raster("i_bag2", S, p =>
                    {
                        float d = 1f;
                        for (int ix = -1; ix <= 1; ix++) for (int iy = -1; iy <= 1; iy++) d = Mathf.Min(d, Box(p, new Vector2(ix * 0.27f, iy * 0.27f), new Vector2(0.105f, 0.105f), 0.035f));
                        return d;
                    });
` + s.slice(b);
    fs.writeFileSync(root + 'Scripts/UI/UIKit.cs', s); results.push('ok   UIKit bag icon');
  } else results.push('MISS UIKit bag icon');
}
console.log(results.join('\n'));
