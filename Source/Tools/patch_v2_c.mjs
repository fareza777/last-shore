// v2 patches to the editor project setup: new shader on palette materials, noise + terrain textures, new prefabs.
import fs from 'node:fs';
const f = 'C:/Survival Fable 62/Assets/Editor/ProjectSetup.cs';
let s = fs.readFileSync(f, 'utf8').replace(/\r\n/g, '\n');
const results = [];
function rep(from, to) { const ok = s.includes(from); results.push((ok ? 'ok   ' : 'MISS ') + from.slice(0, 60).replace(/\n/g, ' ')); if (ok) s = s.replace(from, to); }

rep('"Assets/Scenes", "Assets/World", "Assets/Materials" })', '"Assets/Scenes", "Assets/World", "Assets/Materials", "Assets/Resources/UI" })');

// detail noise + terrain texture import settings
rep('            SaveTex("Sand", 512, (u, v) =>', `            SaveTex("Noise", 256, (u, v) => { float n = Fbm(u, v, 4, 5, 123) * 0.65f + Fbm(u, v, 32, 3, 7) * 0.35f; return new Color(n, n, n, 1); });
            foreach (var file in Directory.GetFiles(TexDir + "/Terrain", "*.jpg"))
            {
                string p = file.Replace('\\\\', '/');
                var ti = (TextureImporter)AssetImporter.GetAtPath(p); if (ti == null) continue;
                ti.textureType = p.EndsWith("_n.jpg") ? TextureImporterType.NormalMap : TextureImporterType.Default;
                ti.maxTextureSize = 1024; ti.wrapMode = TextureWrapMode.Repeat; ti.anisoLevel = 4; ti.mipmapEnabled = true;
                ti.SaveAndReimport();
            }
            SaveTex("Sand", 512, (u, v) =>`);

// palette materials on the custom shader
rep('"\\"(\\\\w+)\\":\\\\s*\\\\{\\\\s*\\"hex\\":\\\\s*\\"#(\\\\w+)\\",\\\\s*\\"rough\\":\\\\s*([\\\\d.]+),\\\\s*\\"double\\":\\\\s*(true|false)"',
    '"\\"(\\\\w+)\\":\\\\s*\\\\{\\\\s*\\"hex\\":\\\\s*\\"#(\\\\w+)\\",\\\\s*\\"rough\\":\\\\s*([\\\\d.]+),\\\\s*\\"double\\":\\\\s*(true|false),\\\\s*\\"emit\\":\\\\s*([\\\\d.]+)"');
rep(`                MakeMat(MatDir, name, "Universal Render Pipeline/Lit", m =>
                {
                    m.SetColor("_BaseColor", col); m.SetFloat("_Smoothness", Mathf.Clamp01((1 - rough) * 0.75f)); m.SetFloat("_Metallic", 0);
                    m.SetFloat("_Cull", dbl ? 0 : 2); m.doubleSidedGI = dbl; m.enableInstancing = true;
                });`,
`                float emit = float.Parse(mt.Groups[5].Value, System.Globalization.CultureInfo.InvariantCulture);
                var noise = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/Noise.png");
                bool smooth = name == "Canvas" || name == "CanvasOld" || name == "Paper" || name == "Waterfall" || name == "Skin";
                MakeMat(MatDir, name, "LastShore/Lit", m =>
                {
                    m.SetColor("_BaseColor", col); m.SetFloat("_Smoothness", Mathf.Clamp01((1 - rough) * 0.75f));
                    m.SetColor("_Emission", col * emit); m.SetTexture("_DetailTex", noise); m.SetFloat("_Detail", smooth ? 0.15f : 0.42f); m.SetFloat("_DetailScale", 0.9f);
                    m.SetFloat("_WindAmount", 1f); m.SetFloat("_Cull", dbl ? 0 : 2); m.doubleSidedGI = dbl; m.enableInstancing = true;
                });`);
rep(`                m.SetFloat("_WaveHeight", 0.07f); m.SetFloat("_WaveLen", 46f); m.SetFloat("_WaveSpeed", 1f); m.SetFloat("_Ripple", 1f); m.SetFloat("_DepthRange", 7f); m.SetFloat("_FoamRange", 0.9f);
            });`,
`                m.SetFloat("_WaveHeight", 0.07f); m.SetFloat("_WaveLen", 46f); m.SetFloat("_WaveSpeed", 1f); m.SetFloat("_Ripple", 1f); m.SetFloat("_DepthRange", 7f); m.SetFloat("_FoamRange", 0.9f);
            });
            MakeMat("Assets/Materials", "WaterPool", "LastShore/Water", m =>
            {
                m.SetFloat("_WaveHeight", 0.0f); m.SetFloat("_WaveLen", 20f); m.SetFloat("_WaveSpeed", 1.4f); m.SetFloat("_Ripple", 1.3f); m.SetFloat("_DepthRange", 2.2f); m.SetFloat("_FoamRange", 0.5f);
                m.SetColor("_Shallow", new Color(0.25f, 0.82f, 0.74f, 0.5f)); m.SetColor("_Deep", new Color(0.03f, 0.36f, 0.42f, 0.9f));
            });`);

// new gameplay prefabs
rep('            go = new GameObject("Crab"); Spawn("Crab", go.transform);', `            Structure<Bed>("Bed", new Vector3(1.2f, 0.4f, 2.1f));
            Structure<CookingPot>("CookingPot", new Vector3(1.0f, 1.2f, 1.0f));
            Structure<FishTrap>("FishTrap", new Vector3(1.0f, 0.7f, 1.2f));
            Structure<TorchStand>("TorchStand", new Vector3(0.3f, 1.8f, 0.3f));
            Structure<Fence>("Fence", new Vector3(2.4f, 1.4f, 0.3f));
            AnimalPrefab<Deer>("Deer", 0.36f, 1.7f, 0.95f, 2);
            AnimalPrefab<Monkey>("Monkey", 0.22f, 0.8f, 0.4f, 2);
            AnimalPrefab<Shark>("Shark", 0.5f, 3.4f, 0f, 2);
            AnimalPrefab<Snake>("Snake", 0.16f, 0.5f, 0.1f, 2);
            AnimalPrefab<Turtle>("Turtle", 0.32f, 0.8f, 0.18f, 2);
            AnimalPrefab<Fowl>("Fowl", 0.2f, 0.5f, 0.3f, 1);

            go = new GameObject("Crab"); Spawn("Crab", go.transform);`);
rep('        public static GameObject BuildShelter(bool withComponent)', `        static void Structure<T>(string name, Vector3 box) where T : PlacedStructure
        {
            var go = new GameObject(name); Spawn(name, go.transform); SetLayer(go, 11);
            var b = go.AddComponent<BoxCollider>(); b.size = box; b.center = new Vector3(0, box.y * 0.5f, 0);
            go.AddComponent<T>().Prefab = name; SavePrefab(go, name);
        }

        static void AnimalPrefab<T>(string name, float radius, float length, float centerY, int direction) where T : Animal
        {
            var go = new GameObject(name); Spawn(name, go.transform);
            var c = go.AddComponent<CapsuleCollider>(); c.direction = direction; c.radius = radius; c.height = Mathf.Max(length, radius * 2f); c.center = new Vector3(0, centerY, 0);
            go.AddComponent<Rigidbody>().isKinematic = true; go.AddComponent<T>(); SavePrefab(go, name);
        }

        public static GameObject BuildShelter(bool withComponent)`);
// pickups, held items and icons should not be culled by the old per-layer distances; nothing else to change.
fs.writeFileSync(f, s);
console.log(results.join('\n'));
