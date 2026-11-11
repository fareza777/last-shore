import fs from 'node:fs';
const f = 'C:/Survival Fable 62/Assets/Editor/ProjectSetup.cs';
let s = fs.readFileSync(f, 'utf8').replace(/\r\n/g, '\n');
if (!s.includes('GradeTerrain')) {
  s = s.replace('        // ---------------------------------------------------------------- materials', `        /// <summary>Colour-grades the downloaded terrain photos toward the game's bright tropical palette (keeps their detail).</summary>
        [MenuItem("Last Shore/Grade Terrain Textures")]
        public static void GradeTerrain()
        {
            var targets = new Dictionary<string, Color>
            {
                { "Sand", new Color(0.88f, 0.79f, 0.58f) }, { "Grass", new Color(0.36f, 0.58f, 0.22f) }, { "Forest", new Color(0.27f, 0.40f, 0.17f) },
                { "Dirt", new Color(0.50f, 0.41f, 0.27f) }, { "Rock", new Color(0.54f, 0.53f, 0.50f) },
            };
            foreach (var kv in targets)
            {
                string src = TexDir + "/Terrain/" + kv.Key + "_d.jpg"; if (!File.Exists(src)) continue;
                var tex = new Texture2D(2, 2, TextureFormat.RGB24, false); tex.LoadImage(File.ReadAllBytes(src));
                var px = tex.GetPixels(); Vector3 sum = Vector3.zero;
                foreach (var c in px) sum += new Vector3(c.r, c.g, c.b);
                sum /= px.Length;
                for (int i = 0; i < px.Length; i++)
                {
                    var c = px[i]; float lum = (c.r + c.g + c.b) / Mathf.Max(0.001f, sum.x + sum.y + sum.z);   // local brightness relative to the average
                    lum = Mathf.Lerp(1f, lum, 0.85f);
                    px[i] = new Color(Mathf.Clamp01(kv.Value.r * lum), Mathf.Clamp01(kv.Value.g * lum), Mathf.Clamp01(kv.Value.b * lum), 1);
                }
                tex.SetPixels(px); tex.Apply();
                string dst = TexDir + "/Terrain/" + kv.Key + "_c.png";
                File.WriteAllBytes(dst, tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(dst);
                var ti = (TextureImporter)AssetImporter.GetAtPath(dst);
                ti.maxTextureSize = 1024; ti.wrapMode = TextureWrapMode.Repeat; ti.anisoLevel = 4; ti.mipmapEnabled = true; ti.SaveAndReimport();
            }
            Debug.Log("[LastShore] Terrain textures graded.");
        }

        // ---------------------------------------------------------------- materials`);
  s = s.replace('            BuildMaterials();\n            ImportModels();', '            GradeTerrain();\n            BuildMaterials();\n            ImportModels();');
  fs.writeFileSync(f, s);
}
const w = 'C:/Survival Fable 62/Assets/Editor/WorldBuilder.cs';
let t = fs.readFileSync(w, 'utf8').replace(/\r\n/g, '\n');
t = t.replace('"/Terrain/" + tex[i] + "_d.jpg")', '"/Terrain/" + tex[i] + "_c.png")');
fs.writeFileSync(w, t);
const p = 'C:/Survival Fable 62/Assets/Scripts/Game/PlayerInteraction.cs';
let q = fs.readFileSync(p, 'utf8').replace(/\r\n/g, '\n');
q = q.replace('_arm.transform.localRotation = Quaternion.Euler(64f, -22f, 0f); _arm.transform.localPosition = new Vector3(0.02f, -0.03f, -0.02f);', '_arm.transform.localRotation = Quaternion.Euler(58f, -26f, 0f); _arm.transform.localPosition = new Vector3(0.03f, -0.09f, -0.03f);');
fs.writeFileSync(p, q);
console.log(s.includes('GradeTerrain'), t.includes('_c.png'), q.includes('58f, -26f'));
