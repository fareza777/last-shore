// v3b: water depth from a baked shore map (no camera depth texture needed), FPS logging for device profiling.
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

patch('Shaders/Water.shader', [
  ['            sampler2D _CameraDepthTexture;\n', '            sampler2D _LS_ShoreMap;       // terrain height near sea level, baked by the world builder\n            float4 _LS_ShoreParams;       // x = 1 / world size, y = lowest height, z = height range, w = 1 when a map is set\n'],
  ['                float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screen)));\n                float depth = max(0, sceneZ - i.screen.z);',
   '                float ground = tex2D(_LS_ShoreMap, i.wpos.xz * _LS_ShoreParams.x).r * _LS_ShoreParams.z + _LS_ShoreParams.y;\n                float depth = _LS_ShoreParams.w > 0.5 ? max(0, i.wpos.y - ground) : 30.0;'],
]);

patch('Editor/WorldBuilder.cs', [
  ['            RenderMap();\n            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Game.unity");', '            RenderMap(); BakeShoreMap();\n            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Game.unity");'],
  ['        // ---------------------------------------------------------------- node factories', `        /// <summary>Bakes terrain height around sea level into a texture so the water shader knows how deep it is anywhere.</summary>
        static void BakeShoreMap()
        {
            const int px = 512;
            var tex = new Texture2D(px, px, TextureFormat.RGB24, false); var cols = new Color[px * px];
            for (int z = 0; z < px; z++)
                for (int x = 0; x < px; x++)
                {
                    float h = _td.GetInterpolatedHeight((x + 0.5f) / px, (z + 0.5f) / px);
                    float v = Mathf.Clamp01((h - (Sea - World.ShoreLow)) / World.ShoreRange);
                    cols[z * px + x] = new Color(v, v, v, 1);
                }
            tex.SetPixels(cols); tex.Apply();
            Directory.CreateDirectory("Assets/Resources/World");
            const string path = "Assets/Resources/World/ShoreMap.png";
            File.WriteAllBytes(path, tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.sRGBTexture = false; ti.wrapMode = TextureWrapMode.Clamp; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.maxTextureSize = 512; ti.SaveAndReimport();
        }

        // ---------------------------------------------------------------- node factories`],
]);

patch('Scripts/Game/GameManager.cs', [
  ['        public const float Size = 1000f;', '        public const float Size = 1000f;\n        public const float ShoreLow = 8f, ShoreRange = 16f;   // the shore map covers heights from SeaLevel - 8 to SeaLevel + 8'],
  ['            ApplyQuality(); GameSettings.Changed += ApplyQuality;', `            var shore = Resources.Load<Texture2D>("World/ShoreMap");
            Shader.SetGlobalTexture("_LS_ShoreMap", shore);
            Shader.SetGlobalVector("_LS_ShoreParams", new Vector4(1f / World.Size, World.SeaLevel - World.ShoreLow, World.ShoreRange, shore != null ? 1f : 0f));
            ApplyQuality(); GameSettings.Changed += ApplyQuality;`],
  ['            PlaySeconds += Time.deltaTime;', `            PlaySeconds += Time.deltaTime;
            _fpsFrames++; _fpsTime += Time.unscaledDeltaTime;
            if (_fpsTime >= 5f) { Debug.Log("[FPS] " + (_fpsFrames / _fpsTime).ToString("0.0") + " quality=" + GameSettings.Quality + " " + Screen.width + "x" + Screen.height); _fpsFrames = 0; _fpsTime = 0; }`],
  ['        float _autosave;', '        float _autosave, _fpsTime; int _fpsFrames;'],
]);
patch('Scripts/Core/SceneLook.cs', [
  ['        void OnEnable() { Apply(); }', '        void OnEnable() { Shader.SetGlobalVector("_LS_ShoreParams", new Vector4(0.001f, 0, 16, 0)); Apply(); }'],
]);
patch('Scripts/Core/GameSettings.cs', [
  ['                urp.supportsHDR = Quality > 0;', '                urp.supportsHDR = Quality > 0;\n                urp.supportsCameraDepthTexture = false;   // the water reads a baked shore map instead'],
]);
console.log(results.join('\n'));
