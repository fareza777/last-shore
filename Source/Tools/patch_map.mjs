import fs from 'node:fs';
const f = 'C:/Survival Fable 62/Assets/Editor/WorldBuilder.cs';
let s = fs.readFileSync(f, 'utf8').replace(/\r\n/g, '\n');
const a = s.indexOf('        /// <summary>Top-down render of the whole island');
const b = s.indexOf('        // ---------------------------------------------------------------- node factories');
const fn = `        /// <summary>Draws a readable chart of the island from the terrain data (used by the minimap and the map tab).</summary>
        static void RenderMap()
        {
            const int px = 1024;
            var alpha = _td.GetAlphamaps(0, 0, ARes, ARes);
            Color[] pal = { new Color(0.90f, 0.82f, 0.58f), new Color(0.47f, 0.66f, 0.30f), new Color(0.56f, 0.55f, 0.53f), new Color(0.56f, 0.47f, 0.32f), new Color(0.22f, 0.44f, 0.22f) };
            Vector3 sun = new Vector3(-0.6f, 0.65f, 0.45f).normalized;
            var tex = new Texture2D(px, px, TextureFormat.RGB24, false); var cols = new Color[px * px];
            for (int z = 0; z < px; z++)
                for (int x = 0; x < px; x++)
                {
                    float u = (x + 0.5f) / px, v = (z + 0.5f) / px;
                    float hs = _td.GetInterpolatedHeight(u, v) - Sea; Color c;
                    if (hs < 0)
                    {
                        float d = Mathf.Clamp01(-hs / 6f);
                        c = Color.Lerp(new Color(0.36f, 0.78f, 0.80f), new Color(0.07f, 0.30f, 0.46f), d);
                        c *= 0.96f + 0.04f * Mathf.Sin((x + z) * 0.35f);
                    }
                    else
                    {
                        int ax = Mathf.Min(ARes - 1, x * ARes / px), az = Mathf.Min(ARes - 1, z * ARes / px);
                        c = Color.black; for (int l = 0; l < 5; l++) c += pal[l] * alpha[az, ax, l];
                        float shade = Mathf.Clamp01(Vector3.Dot(_td.GetInterpolatedNormal(u, v), sun));
                        c *= 0.55f + 0.6f * shade;
                        if (Mathf.Repeat(hs, 14f) < 0.5f && hs > 3f) c *= 0.8f;           // contour lines
                        if (hs < 0.35f) c = Color.Lerp(c, new Color(0.98f, 0.95f, 0.8f), 0.6f);  // surf line
                    }
                    cols[z * px + x] = c;
                }
            tex.SetPixels(cols); tex.Apply();
            Directory.CreateDirectory("Assets/Resources/UI");
            File.WriteAllBytes("Assets/Resources/UI/IslandMap.png", tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset("Assets/Resources/UI/IslandMap.png");
            var ti = (TextureImporter)AssetImporter.GetAtPath("Assets/Resources/UI/IslandMap.png");
            ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single; ti.mipmapEnabled = false; ti.SaveAndReimport();
        }

`;
s = s.slice(0, a) + fn + s.slice(b);
fs.writeFileSync(f, s); console.log(a > 0 && b > a);
