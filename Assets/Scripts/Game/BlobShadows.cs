using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastShore
{
    /// <summary>
    /// Cheap contact shadows for tiers without shadow maps: a soft dark quad under every nearby tree, rock and animal,
    /// so things look planted on the ground. All quads live in two small meshes (static casters, animals), one draw each.
    /// </summary>
    public class BlobShadows : MonoBehaviour
    {
        struct Caster { public ResourceNode Node; public Vector3 Pos, Right, Forward; public float Radius; }

        const float Range = 60f, Lift = 0.12f; const int MaxQuads = 420;
        readonly List<Caster> _casters = new List<Caster>();
        readonly List<Vector3> _verts = new List<Vector3>(MaxQuads * 4);
        readonly List<Vector2> _uvs = new List<Vector2>(MaxQuads * 4);
        readonly List<int> _tris = new List<int>(MaxQuads * 6);
        Mesh _static, _moving; Material _mat; float _timer; bool _logged;

        void Start()
        {
            _mat = Resources.Load<Material>("Materials/Blob");
            if (_mat == null) { Debug.LogWarning("Blob shadow material missing."); enabled = false; return; }
            _static = new Mesh { name = "BlobsStatic" }; _static.MarkDynamic();
            _moving = new Mesh { name = "BlobsMoving" }; _moving.MarkDynamic();

            var terrain = Terrain.activeTerrain;
            foreach (var n in FindObjectsByType<ResourceNode>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                float r = Radius(n.Type); if (r <= 0) continue;
                Vector3 p = n.transform.position; p.y = World.Height(p) + Lift;
                Vector3 normal = Vector3.up;
                if (terrain != null)
                {
                    Vector3 l = p - terrain.transform.position; var s = terrain.terrainData.size;
                    normal = terrain.terrainData.GetInterpolatedNormal(Mathf.Clamp01(l.x / s.x), Mathf.Clamp01(l.z / s.z));
                }
                var rot = Quaternion.FromToRotation(Vector3.up, normal);
                _casters.Add(new Caster { Node = n, Pos = p, Right = rot * Vector3.right, Forward = rot * Vector3.forward, Radius = r * n.transform.localScale.x });
            }
        }

        static float Radius(NodeType t)
        {
            switch (t)
            {
                case NodeType.Tree: return 3.1f;
                case NodeType.Palm: return 2.3f;
                case NodeType.Bamboo: return 1.7f;
                case NodeType.DeadTree: return 1.4f;
                case NodeType.Rock: case NodeType.FlintRock: return 1.25f;
                case NodeType.Bush: case NodeType.Banana: return 1.2f;
                default: return 0f;
            }
        }

        void Quad(Vector3 c, Vector3 right, Vector3 forward, float r)
        {
            int i = _verts.Count;
            _verts.Add(c - right * r - forward * r); _verts.Add(c + right * r - forward * r); _verts.Add(c + right * r + forward * r); _verts.Add(c - right * r + forward * r);
            _uvs.Add(new Vector2(0, 0)); _uvs.Add(new Vector2(1, 0)); _uvs.Add(new Vector2(1, 1)); _uvs.Add(new Vector2(0, 1));
            _tris.Add(i); _tris.Add(i + 2); _tris.Add(i + 1); _tris.Add(i); _tris.Add(i + 3); _tris.Add(i + 2);
        }

        void Fill(Mesh mesh)
        {
            mesh.Clear();
            if (_verts.Count == 0) return;
            mesh.SetVertices(_verts); mesh.SetUVs(0, _uvs); mesh.SetTriangles(_tris, 0, false);
            mesh.bounds = new Bounds(GameManager.I.Player.transform.position, Vector3.one * (Range * 2.5f));
        }

        void LateUpdate()
        {
            var gm = GameManager.I;
            if (GameSettings.FullLighting || gm == null || gm.Player == null || gm.Player.Cam == null) return;
            Vector3 cam = gm.Player.Cam.transform.position;

            _timer -= Time.deltaTime;
            if (_timer <= 0)
            {
                _timer = 0.35f;
                _verts.Clear(); _uvs.Clear(); _tris.Clear();
                // lean the shadows away from the sun a little
                Vector3 sun = RenderSettings.sun != null ? RenderSettings.sun.transform.forward : Vector3.down; sun.y = 0;
                int quads = 0;
                for (int i = 0; i < _casters.Count && quads < MaxQuads; i++)
                {
                    var c = _casters[i];
                    if ((c.Pos - cam).sqrMagnitude > Range * Range || c.Node == null || c.Node.Removed) continue;
                    Quad(c.Pos + sun * (c.Radius * 0.35f), c.Right, c.Forward, c.Radius); quads++;
                }
                Fill(_static);
                if (!_logged) { _logged = true; Debug.Log("[Blobs] casters=" + _casters.Count + " near=" + quads); }
            }

            _verts.Clear(); _uvs.Clear(); _tris.Clear();
            for (int i = 0; i < Animal.All.Count && i < 60; i++)
            {
                Vector3 p = Animal.All[i].transform.position;
                if ((p - cam).sqrMagnitude > Range * Range || p.y < World.SeaLevel - 0.2f) continue;
                Quad(new Vector3(p.x, p.y + Lift, p.z), Vector3.right, Vector3.forward, 0.75f);
            }
            Fill(_moving);

            _mat.SetFloat("_Strength", Mathf.Lerp(0.3f, 0.62f, gm.Sky.DayFactor) * Mathf.Lerp(1f, 0.55f, gm.Sky.Cloud));
            if (_static.vertexCount > 0) Graphics.DrawMesh(_static, Matrix4x4.identity, _mat, 0, null, 0, null, ShadowCastingMode.Off, false);
            if (_moving.vertexCount > 0) Graphics.DrawMesh(_moving, Matrix4x4.identity, _mat, 0, null, 0, null, ShadowCastingMode.Off, false);
        }
    }
}
