using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LastShore.EditorTools
{
    /// <summary>V2 ISLAND. Generates the 1 km island (terrain, sea, jungle, resources, twelve named places) and the Boot / MainMenu / Intro scenes.</summary>
    public static class WorldBuilder
    {
        const float Size = World.Size, MaxH = 160f, Sea = World.SeaLevel;
        const int Res = 1025, ARes = 1024;
        static TerrainData _td; static System.Random _rng; static int _id;
        static readonly List<Vector3> Used = new List<Vector3>();   // x, z, radius
        static Transform _world;
        static readonly Vector2 C = new Vector2(Size * 0.5f, Size * 0.5f);

        [MenuItem("Last Shore/3. Build All Scenes")]
        public static void BuildAll()
        {
            BuildGame(); BuildMenu(); BuildIntro(); BuildBoot();
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/Boot.unity", true), new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Intro.unity", true), new EditorBuildSettingsScene("Assets/Scenes/Game.unity", true),
            };
            EditorSceneManager.OpenScene("Assets/Scenes/Boot.unity");
            Debug.Log("[LastShore] All scenes built.");
        }

        // ================================================================ helpers
        static float Rand(float a, float b) => a + (float)_rng.NextDouble() * (b - a);
        static float H(float x, float z) => _td.GetInterpolatedHeight(x / Size, z / Size);
        static Vector3 Ground(float x, float z) => new Vector3(x, H(x, z), z);
        static Vector3 Ground(Vector2 p) => Ground(p.x, p.y);
        static float Forest(float x, float z) => Mathf.PerlinNoise(x * 0.011f + 31f, z * 0.011f + 17f);

        static float RawHeight(float wx, float wz)
        {
            float dx = wx - C.x, dz = wz - C.y; float dist = Mathf.Sqrt(dx * dx + dz * dz); float ang = Mathf.Atan2(dz, dx);
            float coast = 365 + 70 * Mathf.PerlinNoise(Mathf.Cos(ang) * 1.3f + 7.3f, Mathf.Sin(ang) * 1.3f + 3.1f) + 22 * Mathf.Sin(ang * 3 + 1.3f) + 12 * Mathf.Sin(ang * 7 + 0.4f);
            float bay = Mathf.DeltaAngle(ang * Mathf.Rad2Deg, -42f) / 13f; coast -= 115f * Mathf.Exp(-bay * bay);      // a deep bay on the south-east
            float e = 1 - dist / coast;
            if (e <= 0) return Mathf.Max(0, Sea + e * 30f);
            float beach = ProjectSetup.SS(0, 0.07f, e), inland = ProjectSetup.SS(0.05f, 0.42f, e);
            float n = Mathf.PerlinNoise(wx * 0.006f + 11, wz * 0.006f + 5) * 0.55f + Mathf.PerlinNoise(wx * 0.016f + 3, wz * 0.016f + 17) * 0.3f + Mathf.PerlinNoise(wx * 0.045f, wz * 0.045f) * 0.15f;
            float peak = Mathf.Pow(Mathf.Clamp01(1 - Vector2.Distance(new Vector2(wx, wz), new Vector2(400, 590)) / 235f), 1.8f) * 95f;
            float hill = Mathf.Pow(Mathf.Clamp01(1 - Vector2.Distance(new Vector2(wx, wz), new Vector2(700, 520)) / 160f), 1.7f) * 30f;
            return Sea + beach * 1.6f + inland * (4 + n * 30 + peak + hill);
        }

        static Vector2 Shore(Vector2 dir, float above = 0.9f)
        {
            dir.Normalize(); Vector2 p = C;
            for (int i = 0; i < 560; i++) { p += dir; if (i > 60 && RawHeight(p.x, p.y) < Sea + above) break; }
            return p;
        }

        static void Flatten(float[,] h, Vector2 c, float radius, float lower = 0f, float absolute = -1f)
        {
            float target = (absolute >= 0 ? absolute : RawHeight(c.x, c.y) - lower) / MaxH; float outer = radius * 1.7f; float cell = Size / (Res - 1);
            int x0 = Mathf.Max(0, Mathf.FloorToInt((c.x - outer) / cell)), x1 = Mathf.Min(Res - 1, Mathf.CeilToInt((c.x + outer) / cell));
            int z0 = Mathf.Max(0, Mathf.FloorToInt((c.y - outer) / cell)), z1 = Mathf.Min(Res - 1, Mathf.CeilToInt((c.y + outer) / cell));
            for (int z = z0; z <= z1; z++)
                for (int x = x0; x <= x1; x++)
                {
                    float d = Vector2.Distance(new Vector2(x * cell, z * cell), c);
                    float w = 1 - ProjectSetup.SS(radius, outer, d);
                    h[z, x] = Mathf.Lerp(h[z, x], target, w);
                }
        }

        static bool Free(float x, float z, float r)
        {
            for (int i = Used.Count - 1; i >= 0; i--) { var u = Used[i]; float dx = u.x - x, dz = u.y - z, rr = u.z + r; if (dx * dx + dz * dz < rr * rr) return false; }
            return true;
        }

        static bool Pick(out Vector3 pos, float minHs, float maxHs, float maxSlope, float radius, Func<Vector3, bool> extra = null, float margin = 40f)
        {
            for (int i = 0; i < 90; i++)
            {
                float x = Rand(margin, Size - margin), z = Rand(margin, Size - margin);
                float y = H(x, z), hs = y - Sea;
                if (hs < minHs || hs > maxHs) continue;
                if (_td.GetSteepness(x / Size, z / Size) > maxSlope) continue;
                pos = new Vector3(x, y, z);
                if (extra != null && !extra(pos)) continue;
                if (!Free(x, z, radius)) continue;
                Used.Add(new Vector3(x, z, radius));
                return true;
            }
            pos = Vector3.zero; return false;
        }

        static Quaternion Tilt(Vector3 p, float yaw) => Quaternion.FromToRotation(Vector3.up, _td.GetInterpolatedNormal(p.x / Size, p.z / Size)) * Quaternion.Euler(0, yaw, 0);
        static Transform Group(string name) { var g = new GameObject(name); g.transform.SetParent(_world, false); return g.transform; }
        static void NoShadows(GameObject go) { foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.Off; }
        static float Dist(Vector3 p, Vector2 q) => Vector2.Distance(new Vector2(p.x, p.z), q);

        static Mesh GridMesh(string assetName, float size, int seg, Func<float, float, float> height = null, float uvTile = 1f)
        {
            var verts = new Vector3[(seg + 1) * (seg + 1)]; var uv = new Vector2[verts.Length]; var tris = new int[seg * seg * 6];
            for (int z = 0; z <= seg; z++)
                for (int x = 0; x <= seg; x++)
                {
                    float px = (x / (float)seg - 0.5f) * size, pz = (z / (float)seg - 0.5f) * size;
                    verts[z * (seg + 1) + x] = new Vector3(px, height != null ? height(px, pz) : 0, pz);
                    uv[z * (seg + 1) + x] = new Vector2(x / (float)seg, z / (float)seg) * uvTile;
                }
            int t = 0;
            for (int z = 0; z < seg; z++)
                for (int x = 0; x < seg; x++)
                {
                    int i = z * (seg + 1) + x;
                    tris[t++] = i; tris[t++] = i + seg + 1; tris[t++] = i + 1; tris[t++] = i + 1; tris[t++] = i + seg + 1; tris[t++] = i + seg + 2;
                }
            var m = new Mesh { name = assetName, indexFormat = IndexFormat.UInt32, vertices = verts, uv = uv, triangles = tris };
            m.RecalculateNormals(); m.RecalculateBounds();
            var b = m.bounds; b.Expand(new Vector3(0, 6, 0)); m.bounds = b;
            AssetDatabase.CreateAsset(m, "Assets/World/" + assetName + ".asset");
            return m;
        }

        static GameObject MeshObject(string name, Mesh mesh, Material mat, Vector3 pos, bool shadows = false)
        {
            var go = new GameObject(name); go.transform.position = pos;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = shadows;
            return go;
        }

        static Camera MakeCamera(string name, Vector3 pos, Quaternion rot, float fov, bool skybox, Color clear)
        {
            var go = new GameObject(name) { tag = "MainCamera" }; go.transform.SetPositionAndRotation(pos, rot);
            var cam = go.AddComponent<Camera>(); cam.fieldOfView = fov; cam.nearClipPlane = 0.1f; cam.farClipPlane = 1500f;
            cam.clearFlags = skybox ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor; cam.backgroundColor = clear;
            go.AddComponent<AudioListener>();
            var d = cam.GetUniversalAdditionalCameraData(); d.renderPostProcessing = true; d.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            return cam;
        }

        static Light MakeSun(string name, Quaternion rot, Color color, float intensity)
        {
            var go = new GameObject(name); go.transform.rotation = rot;
            var l = go.AddComponent<Light>(); l.type = LightType.Directional; l.color = color; l.intensity = intensity; l.shadows = LightShadows.Soft; l.shadowStrength = 0.85f;
            RenderSettings.sun = l;
            return l;
        }

        static void AddPostFX()
        {
            var go = new GameObject("PostFX"); var v = go.AddComponent<Volume>(); v.isGlobal = true;
            v.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/PostFX.asset");
        }

        static void NoBake()
        {
            var ls = new LightingSettings { bakedGI = false, realtimeGI = false };
            Lightmapping.lightingSettings = ls;
        }

        static Material Asset(string name) => AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/" + name + ".mat");

        // ================================================================ GAME
        [MenuItem("Last Shore/Build Game Scene Only")]
        public static void BuildGame()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            _rng = new System.Random(20262); _id = 1; Used.Clear();
            NoBake();
            RenderSettings.skybox = Asset("Sky"); RenderSettings.ambientMode = AmbientMode.Trilight; RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            MakeSun("Sun", Quaternion.Euler(40, -38, 0), new Color(1f, 0.96f, 0.88f), 1.3f);
            AddPostFX();

            // ---- named places, located on the raw height field
            Vector2 south = Shore(new Vector2(0, -1)), east = Shore(new Vector2(1, 0.12f)), north = Shore(new Vector2(0.05f, 1));
            Vector2 eastIn = (C - east).normalized;
            Vector2 spawn = south + new Vector2(0, 8), raftP = east + eastIn * 2f, camp = east + eastIn * 72f + new Vector2(0, 12), wreck = north + new Vector2(0, 2);
            Vector2 mid = Vector2.Lerp(spawn, camp, 0.45f);
            Vector2 lookout = new Vector2(700, 520), plane = new Vector2(292, 470), bigTree = new Vector2(520, 392), bamboo = new Vector2(630, 628), tent = new Vector2(600, 722);
            Vector2 lightDir = new Vector2(-0.74f, 0.67f); Vector2 lightShore = Shore(lightDir); Vector2 light = lightShore - lightDir.normalized * 16f;
            Vector2 jettyDir = new Vector2(-0.62f, -0.78f).normalized; Vector2 jetty = Shore(jettyDir, 0.12f);
            Vector2 archDir = new Vector2(0.86f, -0.5f).normalized; Vector2 archShore = Shore(archDir); Vector2 arch = archShore + archDir * 15f;
            Vector2 falls = new Vector2(452, 520);
            // the falls sit on the flank of the main peak: find which way is uphill there
            Vector2 up = new Vector2(RawHeight(falls.x + 6, falls.y) - RawHeight(falls.x - 6, falls.y), RawHeight(falls.x, falls.y + 6) - RawHeight(falls.x, falls.y - 6)).normalized;
            Vector2 pool = falls - up * 6f;
            float poolFloor = RawHeight(pool.x - up.x * 9f, pool.y - up.y * 9f) - 1.1f;   // cut the basin into the slope, below its downhill lip

            // ---- terrain
            var h = new float[Res, Res];
            for (int z = 0; z < Res; z++) for (int x = 0; x < Res; x++) h[z, x] = RawHeight(x * Size / (Res - 1), z * Size / (Res - 1)) / MaxH;
            Flatten(h, camp, 12f); Flatten(h, spawn, 6f); Flatten(h, raftP, 6f); Flatten(h, mid, 6f); Flatten(h, lookout, 10f); Flatten(h, plane, 12f);
            Flatten(h, bigTree, 9f); Flatten(h, light, 10f); Flatten(h, tent, 8f); Flatten(h, bamboo, 14f); Flatten(h, pool, 8f, 0f, poolFloor);
            _td = new TerrainData { heightmapResolution = Res, size = new Vector3(Size, MaxH, Size) };
            _td.SetHeights(0, 0, h);
            AssetDatabase.CreateAsset(_td, "Assets/World/IslandTerrain.asset");

            string[] tex = { "Sand", "Grass", "Rock", "Forest" }; float[] tile = { 6, 7, 7, 6 };
            var layers = new TerrainLayer[tex.Length];
            for (int i = 0; i < tex.Length; i++)
            {
                var tl = new TerrainLayer
                {
                    diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(ProjectSetup.TexDir + "/Terrain/" + tex[i] + "_c.png"),
                    normalMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(ProjectSetup.TexDir + "/Terrain/" + tex[i] + "_n.jpg"),
                    tileSize = new Vector2(tile[i], tile[i]), smoothness = 0, metallic = 0, normalScale = 1f,
                };
                AssetDatabase.CreateAsset(tl, "Assets/World/Layer_" + tex[i] + ".terrainlayer"); layers[i] = tl;
            }
            _td.terrainLayers = layers; _td.alphamapResolution = ARes;
            var map = new float[ARes, ARes, 4]; var grassMask = new float[ARes, ARes];
            for (int z = 0; z < ARes; z++)
                for (int x = 0; x < ARes; x++)
                {
                    float u = (x + 0.5f) / ARes, v = (z + 0.5f) / ARes; float wx = u * Size, wz = v * Size;
                    float hs = _td.GetInterpolatedHeight(u, v) - Sea, slope = _td.GetSteepness(u, v);
                    float n = Mathf.PerlinNoise(u * 120, v * 120), n2 = Mathf.PerlinNoise(u * 28 + 9, v * 28 + 4);
                    float sand = 1 - ProjectSetup.SS(1.5f, 3.2f, hs + n * 0.9f);
                    float rock = ProjectSetup.SS(26f, 39f, slope + n * 6f) * (1 - sand * 0.6f);
                    float rest = Mathf.Clamp01(1 - sand - rock);
                    float forest = rest * ProjectSetup.SS(0.5f, 0.66f, Forest(wx, wz) + (n - 0.5f) * 0.12f) * 0.9f;
                    float dirt = (rest - forest) * ProjectSetup.SS(0.56f, 0.74f, n2) * 0.8f;
                    float grass = Mathf.Max(0, rest - forest - dirt);
                    float sum = sand + rock + dirt + grass + forest;
                    map[z, x, 0] = sand / sum; map[z, x, 1] = grass / sum; map[z, x, 2] = rock / sum; map[z, x, 3] = (dirt + forest) / sum;
                    grassMask[z, x] = slope < 30 ? grass / sum : 0;
                }
            _td.SetAlphamaps(0, 0, map);

            // ---- terrain grass (GPU instanced detail meshes)
            var grassSrc = ProjectSetup.Model("GrassTuft"); var gmf = grassSrc != null ? grassSrc.GetComponentInChildren<MeshFilter>() : null;
            if (gmf != null)
            {
                var gp = new GameObject("GrassDetail"); gp.AddComponent<MeshFilter>().sharedMesh = gmf.sharedMesh;
                var gr = gp.AddComponent<MeshRenderer>(); gr.sharedMaterial = gmf.GetComponent<MeshRenderer>().sharedMaterial; gr.shadowCastingMode = ShadowCastingMode.Off;
                var grassPrefab = PrefabUtility.SaveAsPrefabAsset(gp, "Assets/World/GrassDetail.prefab"); UnityEngine.Object.DestroyImmediate(gp);
                _td.SetDetailResolution(ARes, 32);
                _td.detailPrototypes = new[] { new DetailPrototype { prototype = grassPrefab, usePrototypeMesh = true, renderMode = DetailRenderMode.VertexLit, useInstancing = true,
                    minWidth = 1.0f, maxWidth = 1.9f, minHeight = 0.9f, maxHeight = 1.8f, noiseSpread = 0.4f, healthyColor = Color.white, dryColor = new Color(0.92f, 0.9f, 0.72f) } };
                var det = new int[ARes, ARes];
                for (int z = 0; z < ARes; z++) for (int x = 0; x < ARes; x++) det[z, x] = grassMask[z, x] > 0.5f && _rng.NextDouble() < 0.42 ? 1 + _rng.Next(2) : 0;
                _td.SetDetailLayer(0, 0, 0, det);
            }

            var terrainGo = Terrain.CreateTerrainGameObject(_td); terrainGo.name = "Island";
            var terrain = terrainGo.GetComponent<Terrain>();
            terrain.materialTemplate = Asset("TerrainMat"); terrain.drawInstanced = true; terrain.heightmapPixelError = 6; terrain.basemapDistance = 200;
            terrain.detailObjectDistance = 70; terrain.detailObjectDensity = 0.9f; terrain.shadowCastingMode = ShadowCastingMode.On;

            // ---- sea
            var water = Asset("Water");
            MeshObject("Sea", GridMesh("SeaMesh", 2400, 160), water, new Vector3(C.x, Sea, C.y));
            MeshObject("SeaFar", GridMesh("SeaFarMesh", 14000, 6), water, new Vector3(C.x, Sea - 0.3f, C.y));

            _world = new GameObject("World").transform;
            foreach (var p in new[] { (spawn, 5f), (raftP, 8f), (camp, 15f), (wreck, 13f), (mid, 6f), (lookout, 13f), (plane, 15f), (bigTree, 9f), (light, 12f), (tent, 9f), (pool, 13f), (falls + up * 4f, 9f), (jetty, 9f), (bamboo, 4f) })
                Used.Add(new Vector3(p.Item1.x, p.Item1.y, p.Item2));

            var pickups = Group("Pickups"); var plants = Group("Plants"); var trees = Group("Trees"); var rocks = Group("Rocks"); var deco = Group("Decor"); var boulders = Group("Boulders"); var flowers = Group("Flowers");

            // ---- guaranteed starter resources around the first beach
            Func<Vector3, bool> nearSpawn = p => { float d = Dist(p, spawn); return d > 5 && d < 36; };
            for (int i = 0; i < 8; i++) if (Pick(out var p, 0.6f, 7, 20, 1.0f, nearSpawn)) MakePickup(pickups, "stick", p);
            for (int i = 0; i < 8; i++) if (Pick(out var p, 0.6f, 7, 20, 1.0f, nearSpawn)) MakePickup(pickups, "stone", p);
            for (int i = 0; i < 4; i++) if (Pick(out var p, 0.7f, 5, 20, 1.2f, nearSpawn)) MakePickup(pickups, "coconut", p);
            for (int i = 0; i < 6; i++) if (Pick(out var p, 1.2f, 9, 20, 1.2f, nearSpawn)) MakePlant(plants, NodeType.Fiber, "FiberPlant", p);
            for (int i = 0; i < 3; i++) if (Pick(out var p, 1.0f, 5, 15, 2.6f, nearSpawn)) MakeTree(trees, NodeType.Palm, "Palm" + (1 + i % 3), null, p, 0.32f, 6f);
            // ---- the bamboo thicket
            for (int i = 0; i < 16; i++) if (Pick(out var p, 2f, 80, 30, 2.4f, q => Dist(q, bamboo) < 26)) MakeTree(trees, NodeType.Bamboo, "BambooGrove", null, p, 0.55f, 5f);

            // ---- jungle, groves and resources
            Func<Vector3, bool> grove = p => Forest(p.x, p.z) > 0.42f;
            for (int i = 0; i < 400; i++) if (Pick(out var p, 0.9f, 4.8f, 22, 2.6f)) MakeTree(trees, NodeType.Palm, "Palm" + (1 + _rng.Next(3)), null, p, 0.32f, 6f);
            for (int i = 0; i < 1250; i++) if (Pick(out var p, 4f, 120, 30, 2.7f, grove)) { int k = 1 + _rng.Next(3); MakeTree(trees, NodeType.Tree, "Tree" + k, "Tree" + k + "_L", p, 0.36f, 6f); }
            for (int i = 0; i < 200; i++) if (Pick(out var p, 4f, 120, 28, 3.2f)) { int k = 1 + _rng.Next(3); MakeTree(trees, NodeType.Tree, "Tree" + k, "Tree" + k + "_L", p, 0.36f, 6f); }
            for (int i = 0; i < 36; i++) if (Pick(out var p, 3f, 90, 26, 2.4f)) MakeTree(trees, NodeType.Bamboo, "BambooGrove", null, p, 0.55f, 5f);
            for (int i = 0; i < 46; i++) if (Pick(out var p, 3f, 100, 30, 3f)) MakeTree(trees, NodeType.DeadTree, "DeadTree", null, p, 0.3f, 4.5f);
            for (int i = 0; i < 80; i++) if (Pick(out var p, 2.5f, 60, 24, 2.2f)) MakeBanana(plants, p);
            for (int i = 0; i < 330; i++) if (Pick(out var p, 0.8f, 130, 38, 2.0f)) MakeRock(rocks, NodeType.Rock, "Rock" + (1 + _rng.Next(3)), p);
            for (int i = 0; i < 85; i++) if (Pick(out var p, 3f, 130, 38, 2.0f)) MakeRock(rocks, NodeType.FlintRock, "FlintRock", p);
            for (int i = 0; i < 170; i++) if (Pick(out var p, -2.5f, 135, 62, 5.5f, null, 20)) MakeBoulder(boulders, _rng.NextDouble() < 0.3 ? "Cliff" : "Boulder", p);
            for (int i = 0; i < 175; i++) if (Pick(out var p, 2.5f, 70, 26, 1.6f)) MakeBush(plants, p);
            for (int i = 0; i < 330; i++) if (Pick(out var p, 1.4f, 70, 28, 1.0f)) MakePlant(plants, NodeType.Fiber, "FiberPlant", p);
            for (int i = 0; i < 430; i++) if (Pick(out var p, 3.5f, 110, 32, 1.0f)) MakePlant(plants, NodeType.Fern, "Fern", p);
            for (int i = 0; i < 95; i++) if (Pick(out var p, 4f, 90, 28, 1.0f)) MakePlant(plants, NodeType.Herb, "HerbPlant", p);
            for (int i = 0; i < 380; i++) if (Pick(out var p, 0.7f, 100, 30, 0.8f)) MakePickup(pickups, "stick", p);
            for (int i = 0; i < 300; i++) if (Pick(out var p, 0.5f, 110, 32, 0.8f)) MakePickup(pickups, "stone", p);
            for (int i = 0; i < 120; i++) if (Pick(out var p, 0.7f, 4.8f, 22, 0.9f)) MakePickup(pickups, "coconut", p);
            for (int i = 0; i < 130; i++) if (Pick(out var p, 6f, 110, 28, 0.8f)) MakePickup(pickups, "mushroom", p);
            for (int i = 0; i < 85; i++) if (Pick(out var p, 2f, 120, 34, 0.8f)) MakePickup(pickups, "flint", p);
            for (int i = 0; i < 70; i++) if (Pick(out var p, -0.2f, 0.6f, 14, 0.9f)) MakePickup(pickups, "clam", p);
            for (int i = 0; i < 44; i++) if (Pick(out var p, 0.3f, 2.4f, 16, 0.9f)) MakePickup(pickups, "feather", p);
            for (int i = 0; i < 80; i++) if (Pick(out var p, 0.25f, 1.7f, 16, 1.5f)) NoShadows(MakeDecor(deco, "Driftwood", p, 1f, 1.5f));
            for (int i = 0; i < 180; i++) if (Pick(out var p, 0.15f, 1.5f, 16, 0.5f)) NoShadows(MakeDecor(deco, "SeaShell", p, 0.8f, 1.6f));
            for (int i = 0; i < 300; i++) if (Pick(out var p, 3f, 90, 26, 0.7f)) NoShadows(MakeDecor(flowers, _rng.NextDouble() < 0.5 ? "FlowerPink" : "FlowerYellow", p, 0.9f, 1.5f));

            BuildLocations(spawn, mid, camp, wreck, raftP, lookout, plane, bigTree, bamboo, tent, light, lightDir, jetty, jettyDir, arch, archShore, falls, up, pool);

            // ---- animal spawn markers
            var spawns = Group("Spawns");
            Action<string, int, float, float, float, float, Func<Vector3, bool>> mark = (name, count, minHs, maxHs, slope, radius, extra) =>
            {
                for (int i = 0; i < count; i++)
                    if (Pick(out var p, minHs, maxHs, slope, radius, extra)) { var m = new GameObject(name + i); m.transform.SetParent(spawns, false); m.transform.position = p; }
            };
            Func<Vector3, bool> wild = q => Dist(q, spawn) > 130 && Dist(q, camp) > 55;
            mark("Boar", 15, 5, 70, 20, 7, wild);
            mark("Deer", 11, 8, 95, 20, 8, q => Dist(q, spawn) > 100 && Forest(q.x, q.z) < 0.55f);
            mark("Monkey", 9, 8, 90, 26, 6, q => Dist(q, spawn) > 110 && Forest(q.x, q.z) > 0.45f);
            mark("Snake", 20, 4, 80, 24, 5, q => Dist(q, spawn) > 120 && Dist(q, camp) > 30);
            mark("Turtle", 7, 0.3f, 1.6f, 12, 5, null);
            mark("Fowl", 11, 3, 50, 22, 6, q => Dist(q, spawn) > 60);
            mark("Crab", 24, 0.25f, 1.9f, 14, 4, null);
            foreach (Transform t in spawns) if (t.name.StartsWith("Fowl")) MakePickup(pickups, "egg", Ground(t.position.x + 1.5f, t.position.z + 1f));
            for (int i = 0; i < 5; i++)
            {
                float a = i * 1.2566f + 0.4f; Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a)); Vector2 s = Shore(d) + d * 95f;
                var m = new GameObject("Shark" + i); m.transform.SetParent(spawns, false); m.transform.position = new Vector3(s.x, Sea - 0.45f, s.y);
            }

            var ps = new GameObject("PlayerSpawn"); ps.transform.SetPositionAndRotation(Ground(spawn) + Vector3.up * 0.1f, Quaternion.identity);
            new GameObject("GameManager").AddComponent<GameManager>();

            RenderMap(); BakeShoreMap();
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Game.unity");
            Debug.Log("[LastShore] Game scene built: nodes=" + (_id - 1) + " spawn=" + Ground(spawn) + " camp=" + Ground(camp) + " falls=" + Ground(falls));
        }

        /// <summary>Draws a readable chart of the island from the terrain data (used by the minimap and the map tab).</summary>
        static void RenderMap()
        {
            const int px = 1024;
            var alpha = _td.GetAlphamaps(0, 0, ARes, ARes);
            Color[] pal = { new Color(0.90f, 0.82f, 0.58f), new Color(0.47f, 0.66f, 0.30f), new Color(0.56f, 0.55f, 0.53f), new Color(0.26f, 0.46f, 0.24f) };
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
                        c = Color.black; for (int l = 0; l < 4; l++) c += pal[l] * alpha[az, ax, l];
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

        /// <summary>Bakes terrain height around sea level into a texture so the water shader knows how deep it is anywhere.</summary>
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

        // ---------------------------------------------------------------- node factories
        static void MakeTree(Transform parent, NodeType type, string model, string lodModel, Vector3 p, float colRadius, float colHeight)
        {
            var go = new GameObject(model) { layer = 10 }; go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(p - Vector3.up * 0.1f, Quaternion.Euler(0, Rand(0, 360), 0)); go.transform.localScale = Vector3.one * Rand(0.85f, 1.3f);
            var vis = new GameObject("Visual") { layer = 10 }; vis.transform.SetParent(go.transform, false);
            var l0 = ProjectSetup.Spawn(model, vis.transform, "L0"); ProjectSetup.SetLayer(l0, 10);
            var lg = go.AddComponent<LODGroup>();
            if (lodModel != null)
            {
                var l1 = ProjectSetup.Spawn(lodModel, vis.transform, "L1"); ProjectSetup.SetLayer(l1, 10); NoShadows(l1);
                lg.SetLODs(new[] { new LOD(0.16f, l0.GetComponentsInChildren<Renderer>()), new LOD(0.014f, l1.GetComponentsInChildren<Renderer>()) });
            }
            else lg.SetLODs(new[] { new LOD(0.014f, l0.GetComponentsInChildren<Renderer>()) });
            lg.RecalculateBounds();
            var stump = ProjectSetup.Spawn("Stump", go.transform, "Stump"); ProjectSetup.SetLayer(stump, 10); stump.SetActive(false);
            if (type == NodeType.Bamboo) stump.transform.localScale = Vector3.one * 0.5f;
            var c = go.AddComponent<CapsuleCollider>(); c.radius = colRadius; c.height = colHeight; c.center = new Vector3(0, colHeight * 0.5f, 0);
            var n = go.AddComponent<ResourceNode>(); n.Id = _id++; n.Type = type; n.Visual = vis; n.Depleted = stump;
        }

        static void MakeBanana(Transform parent, Vector3 p)
        {
            var go = new GameObject("BananaPlant") { layer = 9 }; go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(p - Vector3.up * 0.05f, Quaternion.Euler(0, Rand(0, 360), 0)); go.transform.localScale = Vector3.one * Rand(0.9f, 1.25f);
            var vis = ProjectSetup.Spawn("BananaPlant", go.transform, "Body"); ProjectSetup.SetLayer(vis, 9);
            Transform fruit = null; foreach (var t in vis.GetComponentsInChildren<Transform>(true)) if (t.name == "Fruit") fruit = t;
            var c = go.AddComponent<CapsuleCollider>(); c.radius = 0.45f; c.height = 2.6f; c.center = new Vector3(0, 1.3f, 0);
            var n = go.AddComponent<ResourceNode>(); n.Id = _id++; n.Type = NodeType.Banana; n.Visual = fruit != null ? fruit.gameObject : null; n.KeepCollider = true;
        }

        static void MakeRock(Transform parent, NodeType type, string model, Vector3 p)
        {
            var go = new GameObject(model) { layer = 9 }; go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(p - Vector3.up * 0.12f, Tilt(p, Rand(0, 360))); go.transform.localScale = Vector3.one * Rand(0.9f, 1.5f);
            var vis = ProjectSetup.Spawn(model, go.transform, "Visual"); ProjectSetup.SetLayer(vis, 9);
            var mf = vis.GetComponentInChildren<MeshFilter>();
            var mc = go.AddComponent<MeshCollider>(); mc.sharedMesh = mf != null ? mf.sharedMesh : null; mc.convex = true;
            var n = go.AddComponent<ResourceNode>(); n.Id = _id++; n.Type = type; n.Visual = vis;
        }

        static void MakeBoulder(Transform parent, string model, Vector3 p)
        {
            var go = ProjectSetup.Spawn(model, parent, model); ProjectSetup.SetLayer(go, 10);
            float s = Rand(0.7f, 1.6f);
            go.transform.SetPositionAndRotation(p - Vector3.up * 0.5f * s, Quaternion.Euler(Rand(-8, 8), Rand(0, 360), Rand(-8, 8))); go.transform.localScale = new Vector3(s, s * Rand(0.8f, 1.2f), s);
            var mf = go.GetComponentInChildren<MeshFilter>();
            if (mf != null) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
        }

        static void MakeBush(Transform parent, Vector3 p)
        {
            var go = new GameObject("BerryBush") { layer = 9 }; go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(p - Vector3.up * 0.05f, Quaternion.Euler(0, Rand(0, 360), 0)); go.transform.localScale = Vector3.one * Rand(0.9f, 1.3f);
            var vis = ProjectSetup.Spawn("BerryBush", go.transform, "Body"); ProjectSetup.SetLayer(vis, 9);
            Transform berries = null; foreach (var t in vis.GetComponentsInChildren<Transform>(true)) if (t.name == "Berries") berries = t;
            var c = go.AddComponent<SphereCollider>(); c.radius = 0.75f; c.center = new Vector3(0, 0.55f, 0);
            var n = go.AddComponent<ResourceNode>(); n.Id = _id++; n.Type = NodeType.Bush; n.Visual = berries != null ? berries.gameObject : null; n.KeepCollider = true;
        }

        static void MakePlant(Transform parent, NodeType type, string model, Vector3 p)
        {
            var go = new GameObject(model) { layer = World.InteractLayer }; go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(p, Tilt(p, Rand(0, 360))); go.transform.localScale = Vector3.one * Rand(0.9f, 1.4f);
            var vis = ProjectSetup.Spawn(model, go.transform, "Visual"); ProjectSetup.SetLayer(vis, 9); NoShadows(vis);
            var c = go.AddComponent<SphereCollider>(); c.isTrigger = true; c.radius = 0.55f; c.center = new Vector3(0, 0.4f, 0);
            var n = go.AddComponent<ResourceNode>(); n.Id = _id++; n.Type = type; n.Visual = vis;
        }

        static void MakePickup(Transform parent, string item, Vector3 p)
        {
            var pk = Pickup.Build(item, 1, p + Vector3.up * 0.02f, Rand(0, 360));
            pk.transform.SetParent(parent, true); pk.transform.rotation = Tilt(p, Rand(0, 360)); pk.Id = _id++; NoShadows(pk.gameObject);
        }

        static GameObject MakeDecor(Transform parent, string model, Vector3 p, float minS, float maxS)
        {
            var go = ProjectSetup.Spawn(model, parent, model); ProjectSetup.SetLayer(go, 9);
            go.transform.SetPositionAndRotation(p, Tilt(p, Rand(0, 360))); go.transform.localScale = Vector3.one * Rand(minS, maxS);
            return go;
        }

        static GameObject Prop(Transform parent, string model, Vector3 p, float yaw, Vector3 colliderSize, int layer = 11)
        {
            var go = new GameObject(model) { layer = layer }; go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(p, Quaternion.Euler(0, yaw, 0));
            var vis = ProjectSetup.Spawn(model, go.transform); ProjectSetup.SetLayer(vis, layer);
            if (colliderSize != Vector3.zero) { var b = go.AddComponent<BoxCollider>(); b.size = colliderSize; b.center = new Vector3(0, colliderSize.y * 0.5f, 0); }
            return go;
        }

        /// <summary>A big set piece with an exact mesh collider.</summary>
        static GameObject SetPiece(Transform parent, string model, Vector3 p, Quaternion rot, float scale = 1f)
        {
            var go = ProjectSetup.Spawn(model, parent, model); ProjectSetup.SetLayer(go, 10);
            go.transform.SetPositionAndRotation(p, rot); go.transform.localScale = Vector3.one * scale;
            var mf = go.GetComponent<MeshFilter>(); if (mf == null) mf = go.GetComponentInChildren<MeshFilter>();
            if (mf != null) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
            return go;
        }

        static void MakeNote(Transform parent, int index, Vector3 p, float yaw)
        {
            var go = new GameObject("Note" + index) { layer = World.InteractLayer }; go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(p + Vector3.up * 0.03f, Quaternion.Euler(0, yaw, 0));
            var vis = ProjectSetup.Spawn("JournalPage", go.transform); vis.transform.localScale = Vector3.one * 1.7f; ProjectSetup.SetLayer(vis, World.InteractLayer);
            var b = go.AddComponent<BoxCollider>(); b.isTrigger = true; b.size = new Vector3(0.8f, 0.35f, 0.8f); b.center = new Vector3(0, 0.15f, 0);
            go.AddComponent<NotePickup>().Index = index;
        }

        static void MakeCrate(Transform parent, int index, Vector3 p, float yaw, string item, int count, string item2 = null, int count2 = 0, string model = "Crate")
        {
            var go = Prop(parent, model, p, yaw, model == "Chest" ? new Vector3(0.95f, 0.62f, 0.6f) : new Vector3(0.85f, 0.6f, 0.65f));
            var lc = go.AddComponent<LootCrate>(); lc.Index = index; lc.ItemId = item; lc.Count = count; lc.ItemId2 = item2; lc.Count2 = count2;
        }

        static GameObject Marker(Transform parent, string name, Vector3 p) { var m = new GameObject(name); m.transform.SetParent(parent, false); m.transform.position = p; return m; }

        static void Place(Transform parent, string id, string title, string voice, Vector3 p, float radius)
        {
            var lm = Marker(parent, "Landmark_" + id, p).AddComponent<Landmark>(); lm.Id = id; lm.Title = title; lm.Voice = voice; lm.Radius = radius;
        }

        static GameObject Instance(Transform parent, string prefab, Vector3 p, float yaw)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/" + prefab + ".prefab"), parent);
            go.transform.SetPositionAndRotation(p, Quaternion.Euler(0, yaw, 0));
            var ps = go.GetComponent<PlacedStructure>(); if (ps != null) ps.Prefab = "";     // part of the world, not a saved player structure
            return go;
        }

        static void BuildLocations(Vector2 spawn, Vector2 mid, Vector2 camp, Vector2 wreck, Vector2 raftP, Vector2 lookout, Vector2 plane, Vector2 bigTree, Vector2 bamboo, Vector2 tent,
            Vector2 light, Vector2 lightDir, Vector2 jetty, Vector2 jettyDir, Vector2 arch, Vector2 archShore, Vector2 falls, Vector2 up, Vector2 pool)
        {
            var loc = Group("Locations");

            // --- First Shore: where the player wakes
            MakeNote(loc, 0, Ground(spawn + new Vector2(4.5f, 4f)), 30);
            MakeCrate(loc, 4, Ground(spawn + new Vector2(6.2f, 3.2f)), 25, "flask_empty", 1, "bandage", 1);
            Prop(loc, "Barrel", Ground(spawn + new Vector2(-5f, -1.5f)) - Vector3.up * 0.1f, 0, new Vector3(0.65f, 0.9f, 0.65f)).transform.rotation = Quaternion.Euler(78, 40, 0);
            MakeDecor(loc, "Driftwood", Ground(spawn + new Vector2(3.5f, -3f)), 1.4f, 1.6f);
            Place(loc, "shore", "FIRST SHORE", null, Ground(spawn), 30f);

            // --- a cold fire ring on the way inland
            var oldFire = Instance(loc, "Campfire", Ground(mid), 0); oldFire.name = "OldFireRing"; var cf = oldFire.GetComponent<Campfire>(); cf.Lit = false; cf.Fuel = 0;
            MakeNote(loc, 1, Ground(mid + new Vector2(1.6f, 0.8f)), 200);
            Prop(loc, "Log", Ground(mid + new Vector2(-1.8f, 0.6f)), 70, new Vector3(1.5f, 0.34f, 0.36f));

            // --- Castaway Camp
            Vector3 c3 = Ground(camp);
            var sh = ProjectSetup.BuildShelter(true); sh.name = "OldShelter"; sh.transform.SetParent(loc, false);
            sh.transform.SetPositionAndRotation(Ground(camp + new Vector2(-3.5f, 2.5f)), Quaternion.Euler(0, 125, 0)); sh.GetComponent<Shelter>().Prefab = "";
            var fire = Instance(loc, "Campfire", c3, 0); fire.name = "CampFireRing"; cf = fire.GetComponent<Campfire>(); cf.Lit = false; cf.Fuel = 0;
            Instance(loc, "Workbench", Ground(camp + new Vector2(3.6f, 2.8f)), 215).name = "CampWorkbench";
            MakeCrate(loc, 0, Ground(camp + new Vector2(4.6f, -2.2f)), 300, "axe", 1, "bandage", 2);
            Prop(loc, "Barrel", Ground(camp + new Vector2(5.6f, -1f)), 0, new Vector3(0.65f, 0.9f, 0.65f));
            Prop(loc, "Log", Ground(camp + new Vector2(-0.5f, -2.4f)), 95, new Vector3(1.5f, 0.34f, 0.36f));
            Prop(loc, "Log", Ground(camp + new Vector2(2.3f, -0.6f)), 10, new Vector3(1.5f, 0.34f, 0.36f));
            MakeNote(loc, 2, Ground(camp + new Vector2(-2.2f, 0.4f)), 80); MakeNote(loc, 3, Ground(camp + new Vector2(2.9f, 1.6f)), 310);
            Marker(loc, "Marker_Camp", c3); Place(loc, "camp", "CASTAWAY CAMP", null, c3, 18f);

            // --- the wreck of the Meridian
            Vector3 w3 = Ground(wreck); w3.y = Mathf.Max(w3.y, Sea + 0.1f);
            var wr = ProjectSetup.Spawn("Wreck", loc, "MeridianWreck"); ProjectSetup.SetLayer(wr, 10);
            wr.transform.SetPositionAndRotation(w3 - Vector3.up * 0.15f, Quaternion.Euler(0, 160, 0)); wr.transform.localScale = Vector3.one * 1.15f;
            foreach (var mf in wr.GetComponentsInChildren<MeshFilter>()) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
            MakeCrate(loc, 1, Ground(wreck + new Vector2(5.5f, -5.5f)), 20, "rope", 3);
            MakeCrate(loc, 2, Ground(wreck + new Vector2(-6.5f, -4.5f)), 75, "rope", 3);
            MakeCrate(loc, 3, Ground(wreck + new Vector2(-1f, -8f)), 140, "canvas", 1, "flask_empty", 1);
            Prop(loc, "Barrel", Ground(wreck + new Vector2(3f, -8.5f)), 0, new Vector3(0.65f, 0.9f, 0.65f));
            Marker(loc, "Marker_Wreck", Ground(wreck + new Vector2(0, -5f))); Place(loc, "wreck", "THE MERIDIAN", null, w3, 26f);

            // --- Tomas's raft
            Vector3 r3 = Ground(raftP); r3.y = Mathf.Max(r3.y, Sea + 0.25f);
            Vector2 seaward = (raftP - C).normalized;
            var rs = new GameObject("RaftSite") { layer = 11 }; rs.transform.SetParent(loc, false);
            rs.transform.SetPositionAndRotation(r3, Quaternion.LookRotation(new Vector3(seaward.x, 0, seaward.y)));
            var frame = ProjectSetup.Spawn("RaftFrame", rs.transform, "Frame"); var raft = ProjectSetup.Spawn("Raft", rs.transform, "Raft"); raft.SetActive(false);
            ProjectSetup.SetLayer(frame, 11);
            var bc = rs.AddComponent<BoxCollider>(); bc.size = new Vector3(2.8f, 0.7f, 3.8f); bc.center = new Vector3(0, 0.35f, 0);
            var site = rs.AddComponent<RaftSite>(); site.FrameVisual = frame; site.RaftVisual = raft;
            MakeNote(loc, 4, Ground(raftP - seaward * 5f + new Vector2(0, 2.5f)), 15);
            Prop(loc, "Log", Ground(raftP - seaward * 4.5f + new Vector2(0, -3f)), 40, new Vector3(1.5f, 0.34f, 0.36f));
            Marker(loc, "Marker_Raft", r3); Place(loc, "raft", "TOMAS'S RAFT", null, r3, 16f);

            // --- Emerald Falls: fresh water on the flank of the peak
            Vector3 f3 = Ground(pool); Vector3 down = new Vector3(-up.x, 0, -up.y);
            var wf = SetPiece(loc, "Waterfall", f3 - down * 6.5f, Quaternion.LookRotation(down));
            wf.AddComponent<WaterfallFX>();
            var poolGo = MeshObject("FallsPool", GridMesh("PoolMesh", 19, 6), Asset("WaterPool"), f3 + Vector3.up * 0.7f); poolGo.transform.SetParent(loc, true);
            var src = new GameObject("FreshWater") { layer = World.InteractLayer }; src.transform.SetParent(loc, false); src.transform.position = f3 + Vector3.up * 0.6f;
            var sc = src.AddComponent<SphereCollider>(); sc.isTrigger = true; sc.radius = 6.5f; src.AddComponent<WaterSource>();
            for (int i = 0; i < 9; i++) { float a = i * 0.7f; var q = pool + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Rand(8f, 11f); MakePlant(loc, i % 3 == 0 ? NodeType.Herb : NodeType.Reed, i % 3 == 0 ? "HerbPlant" : "Reed", Ground(q)); }
            Place(loc, "falls", "EMERALD FALLS", "found_falls", f3, 20f);

            // --- The Lookout: tower and signal pyre on the east hill
            Vector3 l3 = Ground(lookout);
            var tower = ProjectSetup.Spawn("Watchtower", loc, "Watchtower"); ProjectSetup.SetLayer(tower, 11); tower.transform.SetPositionAndRotation(l3, Quaternion.Euler(0, 200, 0));
            Action<string, Vector3, Vector3, bool, int> box = (name, center, size, trigger, layer) =>
            {
                var g = new GameObject(name) { layer = layer }; g.transform.SetParent(tower.transform, false); g.transform.localPosition = center;
                var b = g.AddComponent<BoxCollider>(); b.size = size; b.isTrigger = trigger;
            };
            box("Platform", new Vector3(0, 6.03f, 0), new Vector3(2.6f, 0.14f, 2.6f), false, 11);
            box("RailL", new Vector3(1.15f, 6.6f, 0), new Vector3(0.1f, 1.1f, 2.5f), false, 11); box("RailR", new Vector3(-1.15f, 6.6f, 0), new Vector3(0.1f, 1.1f, 2.5f), false, 11);
            box("RailB", new Vector3(0, 6.6f, -1.15f), new Vector3(2.5f, 1.1f, 0.1f), false, 11);
            box("LadderBase", new Vector3(0, 1.2f, 1.75f), new Vector3(1.5f, 2.4f, 1.0f), true, World.InteractLayer);
            box("LadderTop", new Vector3(0, 7.0f, 1.05f), new Vector3(1.3f, 1.8f, 0.5f), true, World.InteractLayer);
            var cl = tower.AddComponent<Climbable>(); cl.TopOffset = new Vector3(0, 6.25f, 0); cl.BottomOffset = new Vector3(0, 0.3f, 2.9f);
            var pyre = Prop(loc, "SignalPyre", Ground(lookout + new Vector2(6.5f, -2f)), 0, Vector3.zero); var pc = pyre.AddComponent<CapsuleCollider>(); pc.radius = 1.3f; pc.height = 2.6f; pc.center = new Vector3(0, 1.3f, 0);
            pyre.AddComponent<SignalPyre>();
            Place(loc, "lookout", "THE LOOKOUT", "found_lookout", l3, 16f);

            // --- Lost Flight: plane wreck in the western jungle, with the compass
            Vector3 p3 = Ground(plane);
            SetPiece(loc, "PlaneWreck", p3, Quaternion.Euler(0, 35, 0));
            MakeCrate(loc, 5, Ground(plane + new Vector2(3.5f, -4f)), 60, "compass", 1, "flask_empty", 1, "Chest");
            MakeCrate(loc, 6, Ground(plane + new Vector2(-5f, 3.5f)), 200, "rope", 2, "bandage", 2);
            Place(loc, "plane", "LOST FLIGHT", "found_plane", p3, 20f);

            // --- Greywatch Light on the north-west cape
            Vector3 g3 = Ground(light);
            SetPiece(loc, "Lighthouse", g3, Quaternion.LookRotation(new Vector3(lightDir.x, 0, lightDir.y)));
            MakeCrate(loc, 7, Ground(light - lightDir.normalized * 5f + new Vector2(2, 2)), 110, "canvas", 1, "arrow", 6, "Chest");
            MakeCrate(loc, 8, Ground(light - lightDir.normalized * 6f + new Vector2(-3, -1)), 20, "antidote", 1, "bandage", 2);
            Place(loc, "light", "GREYWATCH LIGHT", "found_light", g3, 22f);

            // --- The Old Jetty on the south-west shore
            Vector3 j3 = Ground(jetty); Vector3 jOut = new Vector3(jettyDir.x, 0, jettyDir.y);
            SetPiece(loc, "Pier", new Vector3(j3.x, Sea - 0.15f, j3.z) - jOut * 1.5f, Quaternion.LookRotation(-jOut));
            Prop(loc, "Rowboat", Ground(jetty - jettyDir * 9f + new Vector2(5f, 0)), 70, new Vector3(1.6f, 0.6f, 3.2f));
            MakeCrate(loc, 10, Ground(jetty - jettyDir * 10f + new Vector2(-3f, 1f)), 300, "rod", 1, "trap_kit", 1);
            Place(loc, "jetty", "THE OLD JETTY", "found_jetty", j3, 20f);

            // --- Sea Arch and the tide pools
            Vector3 aOut = new Vector3((arch - archShore).normalized.x, 0, (arch - archShore).normalized.y);
            SetPiece(loc, "RockArch", new Vector3(arch.x, Sea - 1.4f, arch.y), Quaternion.LookRotation(Vector3.Cross(Vector3.up, aOut)), 1.2f);
            Place(loc, "arch", "SEA ARCH", "found_arch", Ground(archShore), 26f);

            // --- Grandfather Tree
            Vector3 t3 = Ground(bigTree);
            var big = ProjectSetup.Spawn("BigTree", loc, "GrandfatherTree"); ProjectSetup.SetLayer(big, 10); big.transform.SetPositionAndRotation(t3 - Vector3.up * 0.3f, Quaternion.Euler(0, 40, 0)); big.transform.localScale = Vector3.one * 1.25f;
            var bcap = big.AddComponent<CapsuleCollider>(); bcap.radius = 1.3f; bcap.height = 10f; bcap.center = new Vector3(0, 5f, 0);
            for (int i = 0; i < 5; i++) { float a = i * 1.3f; MakeBanana(loc, Ground(bigTree + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Rand(9f, 13f))); }
            var ms = World.Find("Spawns");
            Place(loc, "tree", "GRANDFATHER TREE", "found_tree", t3, 18f);

            // --- Bamboo Thicket and the Survey Camp
            Place(loc, "bamboo", "BAMBOO THICKET", "found_bamboo", Ground(bamboo), 22f);
            Vector3 s3 = Ground(tent);
            Prop(loc, "Tent", s3, 150, Vector3.zero);
            MakeCrate(loc, 9, Ground(tent + new Vector2(2.6f, 1.2f)), 240, "bow", 1, "arrow", 8, "Chest");
            Instance(loc, "TorchStand", Ground(tent + new Vector2(-2.2f, 2.4f)), 0);
            Prop(loc, "Log", Ground(tent + new Vector2(0.5f, -2.8f)), 100, new Vector3(1.5f, 0.34f, 0.36f));
            Place(loc, "tent", "SURVEY CAMP", "found_tent", s3, 18f);
        }

        // ================================================================ MAIN MENU
        public static void BuildMenu()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            _rng = new System.Random(77); NoBake();
            var sky = new Material(Asset("Sky")); sky.SetFloat("_Exposure", 1.0f); sky.SetFloat("_AtmosphereThickness", 1.9f); sky.SetFloat("_SunSize", 0.06f);
            AssetDatabase.CreateAsset(sky, "Assets/Materials/SkyMenu.mat");
            RenderSettings.skybox = sky; RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.42f, 0.38f, 0.5f); RenderSettings.ambientEquatorColor = new Color(0.5f, 0.36f, 0.3f); RenderSettings.ambientGroundColor = new Color(0.16f, 0.13f, 0.12f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogColor = new Color(0.86f, 0.52f, 0.38f); RenderSettings.fogDensity = 0.006f;
            MakeSun("Sun", Quaternion.Euler(5.5f, 196, 0), new Color(1f, 0.56f, 0.3f), 1.25f);
            AddPostFX();
            var look = new GameObject("SceneLook").AddComponent<SceneLook>(); look.Horizon = new Color(0.95f, 0.6f, 0.42f); look.Day = 0.5f;

            var sand = new Material(ProjectSetup.Mat("SandMat")); sand.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(ProjectSetup.TexDir + "/Sand.png")); sand.SetColor("_BaseColor", Color.white);
            AssetDatabase.CreateAsset(sand, "Assets/Materials/SandGround.mat");
            Func<float, float, float> gh = (x, z) => Mathf.Clamp(-z * 0.075f, -5f, 6f) + (Mathf.PerlinNoise(x * 0.09f + 4, z * 0.09f + 8) - 0.5f) * 0.5f + Mathf.Max(0, -z - 14) * 0.06f;
            var ground = MeshObject("Beach", GridMesh("MenuBeachMesh", 240, 90, gh, 26), sand, Vector3.zero, true);
            ground.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
            MeshObject("Sea", GridMesh("MenuSeaMesh", 700, 70), Asset("Water"), new Vector3(0, 0, 150));
            MeshObject("SeaFar", GridMesh("MenuSeaFarMesh", 9000, 4), Asset("Water"), new Vector3(0, -0.2f, 0));

            var set = new GameObject("Set").transform; _world = set;
            Func<float, float, Vector3> g = (x, z) => new Vector3(x, gh(x, z), z);
            var fire = Prop(set, "Campfire", g(2.6f, -4.2f), 20, Vector3.zero, 0); fire.AddComponent<DecorFire>();
            var shel = ProjectSetup.Spawn("Shelter", set, "Shelter"); shel.transform.SetPositionAndRotation(g(-6.2f, -6.5f), Quaternion.Euler(0, 55, 0));
            Prop(set, "Log", g(4.6f, -5.4f), 60, Vector3.zero, 0); Prop(set, "Log", g(0.6f, -5.6f), 115, Vector3.zero, 0);
            Prop(set, "Crate", g(5.6f, -7.2f), 15, Vector3.zero, 0); Prop(set, "Barrel", g(6.6f, -6.4f), 0, Vector3.zero, 0);
            var spear = ProjectSetup.Spawn("Spear", set, "Spear"); spear.transform.SetPositionAndRotation(g(-3.6f, -5.2f) + Vector3.up * 0.55f, Quaternion.Euler(12, 0, 8));
            var raft = ProjectSetup.Spawn("Raft", set, "Raft"); raft.transform.SetPositionAndRotation(new Vector3(9f, 0.12f, 2.5f), Quaternion.Euler(2, 200, -2));
            (string m, float x, float z, float s)[] items =
            {
                ("Palm1", -11, -3, 1.15f), ("Palm2", -8.5f, 0.5f, 1.0f), ("Palm3", 13, -6, 1.2f), ("Palm1", 17, -1, 1.05f), ("Palm2", -16, -9, 1.25f), ("Palm3", 8, -14, 1.2f), ("Palm1", -3, -17, 1.3f),
                ("Tree1", -14, -20, 1.2f), ("Tree2", 4, -24, 1.3f), ("Tree3", 16, -19, 1.2f), ("Tree1", -24, -14, 1.3f), ("Tree2", 26, -12, 1.3f),
                ("Boulder", -15, 3, 0.8f), ("Rock1", 10, -3, 1.2f), ("Rock2", -2, 1.5f, 0.8f), ("Cliff", 24, 6, 1.0f), ("Boulder", 30, -4, 1.1f),
                ("Fern", -9, -8, 1.3f), ("Fern", 9.5f, -9, 1.2f), ("BerryBush", -12.5f, -10, 1.2f), ("FiberPlant", 7.5f, -2.5f, 1.2f), ("FiberPlant", -5, -2, 1.1f),
                ("Driftwood", 1, 0.2f, 1.4f), ("SeaShell", 3.2f, -1.2f, 1.5f), ("SeaShell", -1.6f, -0.8f, 1.2f), ("CoconutItem", -9.5f, -2, 1f),
            };
            foreach (var it in items)
            {
                var o = ProjectSetup.Spawn(it.m, set, it.m); o.transform.SetPositionAndRotation(g(it.x, it.z) - Vector3.up * 0.08f, Quaternion.Euler(0, Rand(0, 360), 0)); o.transform.localScale = Vector3.one * it.s;
            }
            var cam = MakeCamera("MenuCamera", new Vector3(-1.2f, gh(-1.2f, -11.5f) + 1.45f, -11.5f), Quaternion.Euler(1.5f, 14f, 0), 48, true, Color.black);
            cam.gameObject.AddComponent<CameraDrift>();
            new GameObject("MainMenu").AddComponent<MainMenu>();
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
        }

        // ================================================================ INTRO
        public static void BuildIntro()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            NoBake();
            RenderSettings.skybox = null; RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.05f, 0.065f, 0.09f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogColor = new Color(0.035f, 0.05f, 0.07f); RenderSettings.fogDensity = 0.022f;
            var l = MakeSun("StormLight", Quaternion.Euler(48, 30, 0), new Color(0.6f, 0.72f, 1f), 0.22f); l.shadowStrength = 0.6f;
            AddPostFX();
            var look = new GameObject("SceneLook").AddComponent<SceneLook>(); look.Horizon = new Color(0.04f, 0.055f, 0.075f); look.Day = 0.12f;
            MeshObject("Sea", GridMesh("IntroSeaMesh", 360, 180), Asset("WaterStorm"), Vector3.zero);
            var boat = ProjectSetup.Spawn("Sailboat", null, "Boat"); boat.transform.position = new Vector3(0, 0.35f, 0);
            MakeCamera("IntroCamera", new Vector3(17, 6, 9), Quaternion.Euler(8, -120, 0), 46, false, new Color(0.03f, 0.045f, 0.065f));
            new GameObject("Intro").AddComponent<IntroCinematic>();
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Intro.unity");
        }

        // ================================================================ BOOT
        public static void BuildBoot()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = MakeCamera("BootCamera", Vector3.zero, Quaternion.identity, 60, false, new Color(0.02f, 0.035f, 0.05f));
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            new GameObject("Splash").AddComponent<SplashScreen>();
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Boot.unity");
        }
    }
}
