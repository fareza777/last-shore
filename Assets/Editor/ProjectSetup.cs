using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LastShore.EditorTools
{
    /// <summary>One-click project assembly: render pipeline, materials, model prefabs, gameplay prefabs and item icons.</summary>
    public static class ProjectSetup
    {
        public const string MatDir = "Assets/Resources/Materials", ModelDir = "Assets/Models", TexDir = "Assets/Textures";

        [MenuItem("Last Shore/1. Setup Project")]
        public static void Run()
        {
            foreach (var d in new[] { "Assets/Settings", MatDir, TexDir, "Assets/Resources/Models", "Assets/Resources/Prefabs", "Assets/Resources/Icons", "Assets/Scenes", "Assets/World", "Assets/Materials", "Assets/Resources/UI" })
                Directory.CreateDirectory(d);
            AssetDatabase.Refresh();
            SetupLayers();
            SetupPlayer();
            SetupPipeline();
            BuildTextures();
            GradeTerrain();
            BuildMaterials();
            ImportModels();
            BuildPrefabs();
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("[LastShore] Project setup complete.");
        }

        // ---------------------------------------------------------------- settings
        static void SetupLayers()
        {
            var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tm.FindProperty("layers");
            string[] names = { "Interact", "Props", "Trees", "Structure" };
            for (int i = 0; i < names.Length; i++) layers.GetArrayElementAtIndex(8 + i).stringValue = names[i];
            tm.ApplyModifiedProperties();
        }

        static void SetupPlayer()
        {
            PlayerSettings.companyName = "Fable 62";
            PlayerSettings.productName = "Last Shore";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.fable62.lastshore");
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, "com.fable62.lastshore");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true; PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false; PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.backgroundColor = new Color(0.02f, 0.035f, 0.05f);
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        }

        static void SetupPipeline()
        {
            const string assetPath = "Assets/Settings/URP_Asset.asset", rendererPath = "Assets/Settings/URP_Renderer.asset";
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
            if (asset == null)
            {
                var rd = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rd, rendererPath);
                ResourceReloader.ReloadAllNullIn(rd, "Packages/com.unity.render-pipelines.universal");
                rd.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
                EditorUtility.SetDirty(rd);
                asset = UniversalRenderPipelineAsset.Create(rd);
                AssetDatabase.CreateAsset(asset, assetPath);
            }
            asset.supportsCameraDepthTexture = true; asset.supportsCameraOpaqueTexture = false; asset.supportsHDR = true;
            asset.msaaSampleCount = 1; asset.shadowDistance = 120f; asset.shadowCascadeCount = 3; asset.renderScale = 1f;
            var so = new SerializedObject(asset);
            Set(so, "m_MainLightShadowmapResolution", 2048); Set(so, "m_SoftShadowsSupported", true);
            Set(so, "m_AdditionalLightsRenderingMode", 1); Set(so, "m_AdditionalLightsPerObjectLimit", 4);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
            GraphicsSettings.defaultRenderPipeline = asset;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++) { QualitySettings.SetQualityLevel(i, false); QualitySettings.renderPipeline = asset; }
            QualitySettings.SetQualityLevel(current, false);

            const string profilePath = "Assets/Settings/PostFX.asset";
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath) == null)
            {
                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
                var tone = AddOverride<Tonemapping>(profile); tone.mode.Override(TonemappingMode.ACES);
                var bloom = AddOverride<Bloom>(profile); bloom.intensity.Override(0.55f); bloom.threshold.Override(1.0f); bloom.scatter.Override(0.72f);
                var ca = AddOverride<ColorAdjustments>(profile); ca.postExposure.Override(0.25f); ca.contrast.Override(14f); ca.saturation.Override(16f);
                var vig = AddOverride<Vignette>(profile); vig.intensity.Override(0.26f); vig.smoothness.Override(0.45f);
                var wb = AddOverride<WhiteBalance>(profile); wb.temperature.Override(6f);
                EditorUtility.SetDirty(profile);
            }
        }

        static T AddOverride<T>(VolumeProfile profile) where T : VolumeComponent
        {
            var c = profile.Add<T>(true);
            AssetDatabase.AddObjectToAsset(c, profile);
            return c;
        }

        static void Set(SerializedObject so, string prop, int v) { var p = so.FindProperty(prop); if (p != null) p.intValue = v; }
        static void Set(SerializedObject so, string prop, bool v) { var p = so.FindProperty(prop); if (p != null) p.boolValue = v; }

        // ---------------------------------------------------------------- textures
        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
            }
        }

        static float VNoise(float x, float y, int period, int seed)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi; fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            int x0 = ((xi % period) + period) % period, x1 = (x0 + 1) % period, y0 = ((yi % period) + period) % period, y1 = (y0 + 1) % period;
            return Mathf.Lerp(Mathf.Lerp(Hash(x0, y0, seed), Hash(x1, y0, seed), fx), Mathf.Lerp(Hash(x0, y1, seed), Hash(x1, y1, seed), fx), fy);
        }

        /// <summary>GLSL-style smoothstep: 0 below a, 1 above b.</summary>
        public static float SS(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3 - 2 * t); }

        /// <summary>Tileable fractal noise, 0..1.</summary>
        public static float Fbm(float u, float v, int period, int octaves, int seed)
        {
            float sum = 0, amp = 0.5f, norm = 0;
            for (int i = 0; i < octaves; i++) { sum += VNoise(u * period, v * period, period, seed + i * 17) * amp; norm += amp; amp *= 0.5f; period *= 2; }
            return sum / norm;
        }

        static Texture2D SaveTex(string name, int size, Func<float, float, Color> f, bool repeat = true, bool alpha = false)
        {
            string path = TexDir + "/" + name + ".png";
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) tex.SetPixel(x, y, f((x + 0.5f) / size, (y + 0.5f) / size));
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp; ti.alphaIsTransparency = alpha; ti.mipmapEnabled = true; ti.anisoLevel = 4;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void BuildTextures()
        {
            SaveTex("Glow", 64, (u, v) => { float d = new Vector2(u - 0.5f, v - 0.5f).magnitude * 2f; float a = Mathf.Pow(Mathf.Clamp01(1 - d), 1.8f); return new Color(1, 1, 1, a); }, false, true);
            SaveTex("WaterNormal", 256, (u, v) =>
            {
                const float e = 1f / 256f;
                float h = Fbm(u, v, 4, 5, 321);
                float dx = Fbm(u + e, v, 4, 5, 321) - Fbm(u - e, v, 4, 5, 321), dz = Fbm(u, v + e, 4, 5, 321) - Fbm(u, v - e, 4, 5, 321);
                return new Color(Mathf.Clamp01(0.5f + dx * 9f), Mathf.Clamp01(0.5f + dz * 9f), h, 1);
            });
            { var wti = (TextureImporter)AssetImporter.GetAtPath(TexDir + "/WaterNormal.png"); wti.sRGBTexture = false; wti.SaveAndReimport(); }
            SaveTex("Noise", 256, (u, v) => { float n = Fbm(u, v, 4, 5, 123) * 0.65f + Fbm(u, v, 32, 3, 7) * 0.35f; return new Color(n, n, n, 1); });
            foreach (var file in Directory.GetFiles(TexDir + "/Terrain", "*.jpg"))
            {
                string p = file.Replace('\\', '/');
                var ti = (TextureImporter)AssetImporter.GetAtPath(p); if (ti == null) continue;
                ti.textureType = p.EndsWith("_n.jpg") ? TextureImporterType.NormalMap : TextureImporterType.Default;
                ti.maxTextureSize = 1024; ti.wrapMode = TextureWrapMode.Repeat; ti.anisoLevel = 4; ti.mipmapEnabled = true;
                ti.SaveAndReimport();
            }
            SaveTex("Sand", 512, (u, v) =>
            {
                float n = Fbm(u, v, 6, 5, 3), g = Fbm(u, v, 48, 2, 9), rip = Mathf.Sin((v + n * 0.08f) * Mathf.PI * 2 * 22) * 0.5f + 0.5f;
                Color c = Color.Lerp(new Color(0.80f, 0.70f, 0.50f), new Color(0.93f, 0.86f, 0.68f), n);
                return c * (0.93f + g * 0.1f + rip * 0.035f);
            });
            SaveTex("Grass", 512, (u, v) =>
            {
                float n = Fbm(u, v, 5, 5, 21), g = Fbm(u, v, 64, 2, 5), p = Fbm(u, v, 3, 3, 40);
                Color c = Color.Lerp(new Color(0.20f, 0.36f, 0.13f), new Color(0.38f, 0.56f, 0.20f), n);
                c = Color.Lerp(c, new Color(0.50f, 0.56f, 0.24f), SS(0.55f, 0.8f, p) * 0.5f);
                return c * (0.85f + g * 0.3f);
            });
            SaveTex("Rock", 512, (u, v) =>
            {
                float n = Fbm(u, v, 4, 6, 77), cr = Mathf.Abs(Fbm(u, v, 8, 4, 12) - 0.5f) * 2f;
                Color c = Color.Lerp(new Color(0.33f, 0.33f, 0.34f), new Color(0.60f, 0.59f, 0.56f), n);
                return c * Mathf.Lerp(0.72f, 1.05f, SS(0.02f, 0.2f, cr));
            });
            SaveTex("Dirt", 512, (u, v) =>
            {
                float n = Fbm(u, v, 6, 5, 55), g = Fbm(u, v, 40, 2, 2);
                Color c = Color.Lerp(new Color(0.24f, 0.19f, 0.12f), new Color(0.42f, 0.34f, 0.22f), n);
                c = Color.Lerp(c, new Color(0.22f, 0.33f, 0.14f), SS(0.5f, 0.75f, Fbm(u, v, 4, 3, 90)) * 0.6f);
                return c * (0.85f + g * 0.3f);
            });
        }

        /// <summary>Colour-grades the downloaded terrain photos toward the game's bright tropical palette (keeps their detail).</summary>
        [MenuItem("Last Shore/Grade Terrain Textures")]
        public static void GradeTerrain()
        {
            var targets = new Dictionary<string, Color>
            {
                { "Sand", new Color(0.88f, 0.79f, 0.58f) }, { "Grass", new Color(0.35f, 0.52f, 0.23f) }, { "Forest", new Color(0.27f, 0.40f, 0.17f) },
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

        // ---------------------------------------------------------------- materials
        public static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/" + name + ".mat");

        static Material MakeMat(string dir, string name, string shader, Action<Material> init)
        {
            string path = dir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var sh = Shader.Find(shader);
            if (sh == null) { Debug.LogError("Shader not found: " + shader); return m; }
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); } else m.shader = sh;
            init(m); EditorUtility.SetDirty(m);
            return m;
        }

        static void BuildMaterials()
        {
            string json = File.ReadAllText(ModelDir + "/palette.json");
            foreach (Match mt in Regex.Matches(json, "\"(\\w+)\":\\s*\\{\\s*\"hex\":\\s*\"#(\\w+)\",\\s*\"rough\":\\s*([\\d.]+),\\s*\"double\":\\s*(true|false),\\s*\"emit\":\\s*([\\d.]+)"))
            {
                string name = mt.Groups[1].Value; ColorUtility.TryParseHtmlString("#" + mt.Groups[2].Value, out var col);
                float rough = float.Parse(mt.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture); bool dbl = mt.Groups[4].Value == "true";
                float emit = float.Parse(mt.Groups[5].Value, System.Globalization.CultureInfo.InvariantCulture);
                var noise = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/Noise.png");
                bool smooth = name == "Canvas" || name == "CanvasOld" || name == "Paper" || name == "Waterfall" || name == "Skin";
                MakeMat(MatDir, name, "LastShore/Lit", m =>
                {
                    m.SetColor("_BaseColor", col); m.SetFloat("_Smoothness", Mathf.Clamp01((1 - rough) * 0.75f));
                    m.SetColor("_Emission", col * emit); m.SetTexture("_DetailTex", noise); m.SetFloat("_Detail", smooth ? 0.15f : 0.42f); m.SetFloat("_DetailScale", 0.9f);
                    m.SetFloat("_WindAmount", 1f); m.SetFloat("_Cull", dbl ? 0 : 2); m.doubleSidedGI = dbl; m.enableInstancing = true; m.SetFloat("_VertexColor", 0f);
                });
            }
            // every model shares this one material: colour and occlusion are baked into vertex colours
            MakeMat(MatDir, "Shared", "LastShore/Lit", m =>
            {
                m.SetColor("_BaseColor", Color.white); m.SetColor("_Emission", Color.black); m.SetFloat("_VertexColor", 1f); m.SetFloat("_Cull", 0f);
                m.SetTexture("_DetailTex", AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/Noise.png")); m.SetFloat("_Detail", 0.38f); m.SetFloat("_DetailScale", 0.9f);
                m.SetFloat("_WindAmount", 1f); m.doubleSidedGI = true; m.enableInstancing = true;
            });
            var glow = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/Glow.png");
            MakeMat(MatDir, "ParticleAdd", "LastShore/ParticleAdd", m => m.mainTexture = glow);
            MakeMat(MatDir, "ParticleAlpha", "LastShore/ParticleAlpha", m => m.mainTexture = glow);
            MakeMat(MatDir, "GhostOk", "LastShore/Ghost", m => m.SetColor("_Color", new Color(0.45f, 1f, 0.55f, 0.42f)));
            MakeMat(MatDir, "GhostBad", "LastShore/Ghost", m => m.SetColor("_Color", new Color(1f, 0.3f, 0.25f, 0.42f)));
            MakeMat("Assets/Materials", "Water", "LastShore/Water", m =>
            {
                m.SetFloat("_WaveHeight", 0.07f); m.SetFloat("_WaveLen", 46f); m.SetFloat("_WaveSpeed", 1f); m.SetFloat("_Ripple", 1f); m.SetFloat("_DepthRange", 7f); m.SetFloat("_FoamRange", 0.9f);
            });
            MakeMat("Assets/Materials", "WaterPool", "LastShore/Water", m =>
            {
                m.SetFloat("_WaveHeight", 0.0f); m.SetFloat("_WaveLen", 20f); m.SetFloat("_WaveSpeed", 1.4f); m.SetFloat("_Ripple", 1.3f); m.SetFloat("_DepthRange", 2.2f); m.SetFloat("_FoamRange", 0.5f);
                m.SetColor("_Shallow", new Color(0.25f, 0.82f, 0.74f, 0.5f)); m.SetColor("_Deep", new Color(0.03f, 0.36f, 0.42f, 0.9f));
            });
            MakeMat("Assets/Materials", "WaterStorm", "LastShore/Water", m =>
            {
                m.SetFloat("_WaveHeight", 1.15f); m.SetFloat("_WaveLen", 24f); m.SetFloat("_WaveSpeed", 1.5f); m.SetFloat("_Ripple", 2.6f); m.SetFloat("_DepthRange", 3f); m.SetFloat("_FoamRange", 0.5f);
                m.SetColor("_Shallow", new Color(0.10f, 0.22f, 0.26f, 0.95f)); m.SetColor("_Deep", new Color(0.02f, 0.06f, 0.09f, 0.98f)); m.SetFloat("_Gloss", 60f);
            });
            MakeMat("Assets/Materials", "Sky", "Skybox/Procedural", m =>
            {
                m.SetFloat("_SunSize", 0.045f); m.SetFloat("_SunSizeConvergence", 6f); m.SetFloat("_AtmosphereThickness", 1.05f); m.SetFloat("_Exposure", 1.2f);
                m.SetColor("_GroundColor", new Color(0.23f, 0.33f, 0.4f));
            });
            RefreshWater();
            MakeMat("Assets/Materials", "TerrainMat", "Universal Render Pipeline/Terrain/Lit", m => m.enableInstancing = true);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Last Shore/Refresh Water Materials")]
        /// <summary>Assets added after the first full setup: sky, blob shadow and wet sand materials, the storage chest prefab.</summary>
        public static void BuildExtras()
        {
            AssetDatabase.Refresh();
            var noise = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/WaterNormal.png");
            MakeMat(MatDir, "SkyGame", "LastShore/Sky", m => m.SetTexture("_NoiseTex", noise));
            MakeMat(MatDir, "Blob", "LastShore/Blob", m => m.enableInstancing = true);
            MakeMat(MatDir, "WetSand", "LastShore/WetSand", m => { m.SetFloat("_Strength", 0.6f); m.SetFloat("_Height", 0.4f); });
            var go = new GameObject("StorageChest"); Spawn("Chest", go.transform); SetLayer(go, 11);
            var b = go.AddComponent<BoxCollider>(); b.size = new Vector3(0.95f, 0.62f, 0.6f); b.center = new Vector3(0, 0.31f, 0);
            go.AddComponent<StorageChest>().Prefab = "StorageChest"; SavePrefab(go, "StorageChest");
            AssetDatabase.SaveAssets();
            Debug.Log("[LastShore] Extras built.");
        }

        public static void RefreshWater()
        {
            var noise = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/WaterNormal.png");
            foreach (var name in new[] { "Water", "WaterStorm", "WaterPool" })
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/" + name + ".mat"); if (m == null) continue;
                m.shader = Shader.Find("LastShore/Water"); m.SetTexture("_NoiseTex", noise);
                if (name == "Water") { m.SetColor("_Shallow", new Color(0.16f, 0.86f, 0.80f, 0.5f)); m.SetColor("_Deep", new Color(0.01f, 0.15f, 0.42f, 0.97f)); m.SetFloat("_Gloss", 260f); m.SetFloat("_DepthRange", 6f); m.SetFloat("_FoamRange", 0.8f); m.SetFloat("_Ripple", 1f); }
                if (name == "WaterStorm") { m.SetFloat("_Ripple", 1.5f); m.SetFloat("_Gloss", 90f); }
                if (name == "WaterPool") { m.SetFloat("_Ripple", 0.8f); m.SetColor("_Shallow", new Color(0.25f, 0.85f, 0.76f, 0.4f)); }
                EditorUtility.SetDirty(m);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[LastShore] Water materials refreshed.");
        }

        // ---------------------------------------------------------------- models
        static void ImportModels()
        {
            var mats = new Dictionary<string, Material>();
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { MatDir }))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid)); mats[m.name] = m;
            }
            foreach (var file in Directory.GetFiles(ModelDir, "*.fbx"))
            {
                string path = file.Replace('\\', '/');
                var mi = (ModelImporter)AssetImporter.GetAtPath(path);
                if (mi == null) continue;
                mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                mi.importAnimation = false; mi.importCameras = false; mi.importLights = false; mi.animationType = ModelImporterAnimationType.None;
                mi.importBlendShapes = false; mi.isReadable = true;
                foreach (var kv in mats) mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key), kv.Value);
                mi.SaveAndReimport();
                string name = Path.GetFileNameWithoutExtension(path);
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
                PrefabUtility.SaveAsPrefabAsset(inst, "Assets/Resources/Models/" + name + ".prefab");
                UnityEngine.Object.DestroyImmediate(inst);
            }
        }

        public static GameObject Model(string name) => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Models/" + name + ".prefab");

        public static GameObject Spawn(string model, Transform parent, string childName = "Model")
        {
            var src = Model(model);
            if (src == null) { Debug.LogError("Missing model " + model); return new GameObject(childName); }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(src, parent);
            go.name = childName; go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity;
            return go;
        }

        public static void SetLayer(GameObject go, int layer) { foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer; }

        // ---------------------------------------------------------------- gameplay prefabs
        static void SavePrefab(GameObject root, string name)
        {
            PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/Prefabs/" + name + ".prefab");
            UnityEngine.Object.DestroyImmediate(root);
        }

        static void BuildPrefabs()
        {
            var go = new GameObject("Campfire"); Spawn("Campfire", go.transform); SetLayer(go, 11);
            var sc = go.AddComponent<SphereCollider>(); sc.radius = 0.62f; sc.center = new Vector3(0, 0.25f, 0);
            go.AddComponent<Campfire>().Prefab = "Campfire"; SavePrefab(go, "Campfire");

            go = BuildShelter(true); SavePrefab(go, "Shelter");

            go = new GameObject("Workbench"); Spawn("Workbench", go.transform); SetLayer(go, 11);
            var bc = go.AddComponent<BoxCollider>(); bc.size = new Vector3(1.6f, 0.9f, 0.8f); bc.center = new Vector3(0, 0.45f, 0);
            go.AddComponent<Workbench>().Prefab = "Workbench"; SavePrefab(go, "Workbench");

            go = new GameObject("WaterCollector"); Spawn("WaterCollector", go.transform); SetLayer(go, 11);
            var cc = go.AddComponent<CapsuleCollider>(); cc.radius = 0.5f; cc.height = 1.6f; cc.center = new Vector3(0, 0.8f, 0);
            go.AddComponent<WaterCollector>().Prefab = "WaterCollector"; SavePrefab(go, "WaterCollector");

            go = new GameObject("Boar"); Spawn("Boar", go.transform);
            cc = go.AddComponent<CapsuleCollider>(); cc.direction = 2; cc.radius = 0.4f; cc.height = 1.6f; cc.center = new Vector3(0, 0.62f, 0.1f);
            go.AddComponent<Rigidbody>().isKinematic = true; go.AddComponent<Boar>(); SavePrefab(go, "Boar");

            Structure<Bed>("Bed", new Vector3(1.2f, 0.4f, 2.1f));
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

            go = new GameObject("Crab"); Spawn("Crab", go.transform);
            sc = go.AddComponent<SphereCollider>(); sc.radius = 0.3f; sc.center = new Vector3(0, 0.15f, 0);
            go.AddComponent<Rigidbody>().isKinematic = true; go.AddComponent<Crab>(); SavePrefab(go, "Crab");
        }

        static void Structure<T>(string name, Vector3 box) where T : PlacedStructure
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

        public static GameObject BuildShelter(bool withComponent)
        {
            var go = new GameObject("Shelter"); Spawn("Shelter", go.transform); SetLayer(go, 11);
            var roof = new GameObject("Roof") { layer = 11 }; roof.transform.SetParent(go.transform, false);
            roof.transform.localPosition = new Vector3(0, 0.9f, -0.2f); roof.transform.localRotation = Quaternion.Euler(-37f, 0, 0);
            roof.AddComponent<BoxCollider>().size = new Vector3(2.7f, 0.12f, 2.9f);
            var floor = go.AddComponent<BoxCollider>(); floor.size = new Vector3(2.4f, 0.5f, 2.0f); floor.center = new Vector3(0, 0.25f, -0.4f); floor.isTrigger = true;
            if (withComponent) go.AddComponent<Shelter>().Prefab = "Shelter";
            return go;
        }

        [MenuItem("Last Shore/4. Apply App Icon")]
        public static void ApplyIcon()
        {
            const string path = "Assets/Icon/AppIcon.png";
            AssetDatabase.ImportAsset(path);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Default; ti.mipmapEnabled = false; ti.npotScale = TextureImporterNPOTScale.None; ti.SaveAndReimport();
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { tex }, IconKind.Any);
            AssetDatabase.SaveAssets();
            Debug.Log("[LastShore] App icon applied.");
        }

        // ---------------------------------------------------------------- icons
        [MenuItem("Last Shore/2. Render Item Icons")]
        /// <summary>Dark soft outline so icons stay readable on any slot colour.</summary>
        static void Outline(Texture2D tex, int size)
        {
            var src = tex.GetPixels32(); var dst = new Color32[src.Length]; const int R = 5;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    var c = src[y * size + x]; float best = 0;
                    if (c.a < 255)
                        for (int dy = -R; dy <= R; dy++)
                            for (int dx = -R; dx <= R; dx++)
                            {
                                int xx = x + dx, yy = y + dy; if (xx < 0 || yy < 0 || xx >= size || yy >= size) continue;
                                float a = src[yy * size + xx].a / 255f; if (a <= 0) continue;
                                float f = a * Mathf.Clamp01(1.25f - Mathf.Sqrt(dx * dx + dy * dy) / R); if (f > best) best = f;
                            }
                    float ca = c.a / 255f, oa = best * 0.85f, outA = ca + oa * (1 - ca);
                    if (outA <= 0) { dst[y * size + x] = new Color32(0, 0, 0, 0); continue; }
                    float k = ca / outA; var dark = new Color(0.05f, 0.07f, 0.10f);
                    dst[y * size + x] = new Color(Mathf.Lerp(dark.r, c.r / 255f, k), Mathf.Lerp(dark.g, c.g / 255f, k), Mathf.Lerp(dark.b, c.b / 255f, k), outA);
                }
            tex.SetPixels32(dst); tex.Apply();
        }

        public static void RenderIcons()
        {
            const int size = 256, layer = 31;
            var camGo = new GameObject("IconCam"); var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0, 0, 0, 0); cam.cullingMask = 1 << layer;
            cam.allowHDR = false; cam.nearClipPlane = 0.01f; cam.farClipPlane = 100f; cam.enabled = false;
            var data = cam.GetUniversalAdditionalCameraData(); data.renderPostProcessing = false; data.renderShadows = false;
            var key = new GameObject("IconKey").AddComponent<Light>(); key.type = LightType.Directional; key.intensity = 1.5f; key.transform.rotation = Quaternion.Euler(38, 28, 0); key.cullingMask = 1 << layer;
            var fill = new GameObject("IconFill").AddComponent<Light>(); fill.type = LightType.Directional; fill.intensity = 0.7f; fill.color = new Color(0.7f, 0.8f, 1f); fill.transform.rotation = Quaternion.Euler(-20, -140, 0); fill.cullingMask = 1 << layer;
            var rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32); cam.targetTexture = rt;
            var prevAmbientMode = RenderSettings.ambientMode; var prevAmbient = RenderSettings.ambientLight; var prevFog = RenderSettings.fog;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.58f); RenderSettings.fog = false;
            Vector3 origin = new Vector3(3000, 3000, 3000);
            try
            {
                foreach (var def in ItemDB.All)
                {
                    var src = Model(def.Model); if (src == null) continue;
                    var inst = UnityEngine.Object.Instantiate(src, origin, def.Kind == ItemKind.Tool ? Quaternion.Euler(0, 20, -38) : Quaternion.Euler(0, 25, 0));
                    SetLayer(inst, layer);
                    var b = new Bounds(origin, Vector3.zero); bool first = true;
                    foreach (var r in inst.GetComponentsInChildren<Renderer>()) { if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
                    Vector3 dir = def.Kind == ItemKind.Tool ? new Vector3(0, 0.15f, -1).normalized : new Vector3(-0.55f, 0.6f, -0.8f).normalized;
                    cam.transform.position = b.center - dir * -20f; cam.transform.position = b.center + new Vector3(-dir.x, dir.y, dir.z) * 20f;
                    cam.transform.LookAt(b.center);
                    cam.orthographicSize = Mathf.Max(0.06f, b.extents.magnitude * (def.Kind == ItemKind.Tool ? 0.78f : 0.9f));
                    var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                    // render, measure the silhouette, then re-frame so the item fills the icon
                    for (int pass = 0; pass < 2; pass++)
                    {
                        cam.Render();
                        RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, size, size), 0, 0); tex.Apply(); RenderTexture.active = null;
                        if (pass == 1) break;
                        var px = tex.GetPixels32(); int x0 = size, x1 = -1, y0 = size, y1 = -1;
                        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) if (px[y * size + x].a > 8) { if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; if (y > y1) y1 = y; }
                        if (x1 < 0) break;
                        float unit = cam.orthographicSize * 2f / size;
                        cam.transform.position += cam.transform.right * (((x0 + x1 + 1) * 0.5f - size * 0.5f) * unit) + cam.transform.up * (((y0 + y1 + 1) * 0.5f - size * 0.5f) * unit);
                        cam.orthographicSize *= Mathf.Max(x1 - x0 + 1, y1 - y0 + 1) / (size * 0.86f);
                    }
                    Outline(tex, size);
                    string path = "Assets/Resources/Icons/" + def.Id + ".png";
                    File.WriteAllBytes(path, tex.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(tex); UnityEngine.Object.DestroyImmediate(inst);
                }
            }
            finally
            {
                cam.targetTexture = null; rt.Release();
                UnityEngine.Object.DestroyImmediate(camGo); UnityEngine.Object.DestroyImmediate(key.gameObject); UnityEngine.Object.DestroyImmediate(fill.gameObject);
                RenderSettings.ambientMode = prevAmbientMode; RenderSettings.ambientLight = prevAmbient; RenderSettings.fog = prevFog;
            }
            AssetDatabase.Refresh();
            foreach (var def in ItemDB.All)
            {
                string path = "Assets/Resources/Icons/" + def.Id + ".png";
                var ti = AssetImporter.GetAtPath(path) as TextureImporter; if (ti == null) continue;
                ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single; ti.alphaIsTransparency = true; ti.mipmapEnabled = false;
                ti.SaveAndReimport();
            }
            Debug.Log("[LastShore] Icons rendered.");
        }
    }
}
