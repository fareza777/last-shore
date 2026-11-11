using UnityEngine;

namespace LastShore
{
    /// <summary>Shows a placement ghost while a placeable is held and builds it on use.</summary>
    public class BuildSystem : MonoBehaviour
    {
        public bool Active => _ghost != null && _ghost.activeSelf;
        GameObject _ghost; string _ghostId; float _rot; bool _valid; Vector3 _pos;
        Material _ok, _bad; Renderer[] _renderers;
        readonly Collider[] _overlap = new Collider[16];

        void Start()
        {
            _ok = Resources.Load<Material>("Materials/GhostOk"); _bad = Resources.Load<Material>("Materials/GhostBad");
        }

        void Update()
        {
            var gm = GameManager.I;
            var s = gm.Interaction != null ? gm.Interaction.Current : null;
            var def = s != null && !s.Empty ? s.Def : null;
            if (def == null || def.Kind != ItemKind.Placeable || gm.InputLocked) { if (_ghost != null) _ghost.SetActive(false); return; }

            if (_ghostId != def.Id)
            {
                if (_ghost != null) Destroy(_ghost);
                _ghostId = def.Id;
                var src = Resources.Load<GameObject>("Models/" + def.Model);
                if (src == null) return;
                _ghost = Instantiate(src); _ghost.name = "BuildGhost";
                _renderers = _ghost.GetComponentsInChildren<Renderer>();
                foreach (var t in _ghost.GetComponentsInChildren<Transform>()) t.gameObject.layer = World.GhostLayer;
                foreach (var r in _renderers) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            _ghost.SetActive(true);
            if (GameInput.Rotate) _rot += 30f;

            var cam = gm.Player.Cam.transform;
            Vector3 p;
            if (Physics.Raycast(cam.position, cam.forward, out var hit, 7f, ~(1 << World.GhostLayer), QueryTriggerInteraction.Ignore) && hit.collider is TerrainCollider) p = hit.point;
            else p = World.OnGround(gm.Player.transform.position + gm.Player.transform.forward * 3.2f);
            _pos = p;
            float yaw = gm.Player.Yaw + 180f + _rot;
            _ghost.transform.SetPositionAndRotation(p, Quaternion.Euler(0, yaw, 0));

            _valid = World.IsLand(p, 0.35f) && World.Slope(p) < 24f && (p - gm.Player.transform.position).magnitude < 8f;
            if (_valid)
            {
                float radius = def.PlacePrefab == "Shelter" ? 1.5f : 0.75f;
                int n = Physics.OverlapSphereNonAlloc(p + Vector3.up * (radius + 0.3f), radius, _overlap, ~(1 << World.GhostLayer), QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n; i++)
                    if (!(_overlap[i] is TerrainCollider) && !(_overlap[i] is CharacterController)) { _valid = false; break; }
            }
            var mat = _valid ? _ok : _bad;
            if (mat != null) foreach (var r in _renderers) { if (r.sharedMaterial != mat) { var arr = new Material[r.sharedMaterials.Length]; for (int i = 0; i < arr.Length; i++) arr[i] = mat; r.sharedMaterials = arr; } }
            gm.Hud.SetBuildHint(true, _valid);
        }

        void LateUpdate() { if (!Active && GameManager.I != null && GameManager.I.Hud != null) GameManager.I.Hud.SetBuildHint(false, false); }

        public void TryPlace()
        {
            var gm = GameManager.I;
            if (!Active) return;
            if (!_valid) { gm.Hud.Hint("You can't build here."); return; }
            var def = gm.Interaction.Current.Def;
            var s = gm.SpawnStructure(def.PlacePrefab, _pos, _ghost.transform.eulerAngles.y);
            if (s == null) return;
            gm.Inv.RemoveAt(gm.Interaction.Selected);
            AudioManager.I.PlayAt("place", _pos);
            if (def.PlacePrefab == "Campfire") AudioManager.I.PlayAt("ignite", _pos);
            FX.Burst(_pos + Vector3.up * 0.2f, new Color(0.85f, 0.78f, 0.6f), 22, 0.16f, 3f);
            s.transform.localScale = Vector3.one * 0.85f;
            Tween.Scale(s.transform, Vector3.one, 0.4f, Ease.OutBack);
            gm.Quests.Notify("place", def.PlacePrefab);
            gm.SaveGame();
        }
    }
}
