// v2 patches to existing gameplay scripts. Each replacement reports whether it applied.
import fs from 'node:fs';
const dir = 'C:/Survival Fable 62/Assets/Scripts/';
const results = [];
function patch(file, edits) {
  let s = fs.readFileSync(dir + file, 'utf8').replace(/\r\n/g, '\n');
  for (const [from, to] of edits) {
    const ok = s.includes(from);
    results.push((ok ? 'ok   ' : 'MISS ') + file + ': ' + from.slice(0, 50).replace(/\n/g, ' '));
    if (ok) s = s.replace(from, to);
  }
  fs.writeFileSync(dir + file, s);
}

// ---------------------------------------------------------------- Pickup: removal must survive world streaming
patch('Game/Pickup.cs', [
  ['        public override void SetRemoved(bool removed) { gameObject.SetActive(!removed); }',
   `        public bool Removed { get; private set; }
        public override void SetRemoved(bool removed)
        {
            Removed = removed;
            foreach (Transform t in transform) t.gameObject.SetActive(!removed);
            var c = GetComponent<Collider>(); if (c != null) c.enabled = !removed;
        }`],
  ['            var d = ItemDB.Get(ItemId);\n            return d == null ? null', '            var d = ItemDB.Get(ItemId);\n            return d == null || Removed ? null'],
]);

// ---------------------------------------------------------------- PlayerController: expose velocity, longer view
patch('Game/PlayerController.cs', [
  ['        public float Yaw => _yaw;', '        public float Yaw => _yaw;\n        public Vector3 Velocity => _cc != null ? _cc.velocity : Vector3.zero;'],
  ['pc.Cam.farClipPlane = 900f;', 'pc.Cam.farClipPlane = 1800f;'],
  ['data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;', 'data.antialiasing = Application.isMobilePlatform ? AntialiasingMode.FastApproximateAntialiasing : AntialiasingMode.SubpixelMorphologicalAntiAliasing;'],
  ['            var dist = new float[32]; dist[World.InteractLayer] = 70f; dist[9] = 110f; dist[10] = 260f;\n            pc.Cam.layerCullDistances = dist; pc.Cam.layerCullSpherical = true;\n', ''],
]);

// ---------------------------------------------------------------- PlayerVitals: poison
patch('Game/PlayerVitals.cs', [
  ['        public bool Wet { get; private set; }', '        public bool Wet { get; private set; }\n        public float PoisonTime { get; private set; }\n        public void Poison(float seconds) { PoisonTime = Mathf.Max(PoisonTime, seconds); }\n        public void SetPoison(float seconds) { PoisonTime = seconds; }'],
  ['            Hunger = Mathf.Clamp(Hunger + def.Food, 0, 100);', '            if (def.CuresPoison) PoisonTime = def.Id == "antidote" ? 0 : Mathf.Max(0, PoisonTime - 28f);\n            Hunger = Mathf.Clamp(Hunger + def.Food, 0, 100);'],
  ['            if (Hunger <= 0) Damage(dt * 0.7f, "starvation", false);', '            if (PoisonTime > 0) { PoisonTime -= dt; Damage(dt * 0.55f, "poison", false); }\n            if (Hunger <= 0) Damage(dt * 0.7f, "starvation", false);'],
]);

// ---------------------------------------------------------------- PlayerInteraction: arm, bow, knife, noise
patch('Game/PlayerInteraction.cs', [
  ['        GameObject _held; string _heldId;', '        GameObject _held, _arm; string _heldId;'],
  ['            _inv.Changed += RefreshHeld;\n            RefreshHeld();',
   `            _inv.Changed += RefreshHeld;
            var armSrc = Resources.Load<GameObject>("Models/Arm");
            if (armSrc != null)
            {
                _arm = Instantiate(armSrc, _pc.Hand); _arm.name = "Arm";
                _arm.transform.localRotation = Quaternion.Euler(64f, -22f, 0f); _arm.transform.localPosition = new Vector3(0.02f, -0.03f, -0.02f);
                foreach (var r in _arm.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            RefreshHeld();`],
  ['                case ItemKind.Tool:\n                    if (def.Tool == ToolType.Rod && TryCast()) return;',
   '                case ItemKind.Tool:\n                    if (def.Tool == ToolType.Bow) { Shoot(def); return; }\n                    if (def.Tool == ToolType.Rod && TryCast()) return;'],
  ['            _swinging = true; _cooldown = tool == null ? 0.5f : 0.62f;', '            _swinging = true; _cooldown = tool == null ? 0.5f : tool.Tool == ToolType.Knife ? 0.38f : 0.62f;'],
  ['            h.Hit(tool, tool != null ? tool.Damage : 5f, hit.point);', '            h.Hit(tool, tool != null ? tool.Damage : 5f, hit.point);\n            Animal.Noise(hit.point, 18f);'],
  ['        // ---------------------------------------------------------------- fishing',
   `        void Shoot(ItemDef bow)
        {
            if (_inv.Count("arrow") < 1) { GameManager.I.Hud.Hint("No arrows. Craft some from sticks, flint and feathers."); _cooldown = 0.4f; return; }
            _inv.Remove("arrow", 1); _cooldown = 0.95f;
            AudioManager.I.Play("bow", 0.8f, 0.1f);
            var cam = _pc.Cam.transform;
            ArrowProjectile.Fire(cam.position + cam.forward * 0.6f + cam.right * 0.12f - cam.up * 0.08f, cam.forward, bow);
            Tween.Punch(_pc.Hand, 0.18f, 0.25f);
            Wear(1);
        }

        // ---------------------------------------------------------------- fishing`],
]);

// arrow projectile lives next to the interaction code (added at runtime, never serialized)
let pi = fs.readFileSync(dir + 'Game/PlayerInteraction.cs', 'utf8');
if (!pi.includes('class ArrowProjectile')) {
  const end = pi.lastIndexOf('}');
  pi = pi.slice(0, end) + `
    /// <summary>A flying arrow: simple ballistic flight, damages what it hits and can usually be picked up again.</summary>
    public class ArrowProjectile : MonoBehaviour
    {
        Vector3 _vel; ItemDef _bow; float _life = 6f;

        public static void Fire(Vector3 pos, Vector3 dir, ItemDef bow)
        {
            var src = Resources.Load<GameObject>("Models/Arrow");
            var go = src != null ? Instantiate(src, pos, Quaternion.identity) : new GameObject("Arrow");
            go.transform.position = pos;
            var a = go.AddComponent<ArrowProjectile>(); a._vel = dir.normalized * 52f; a._bow = bow;
        }

        void Update()
        {
            float dt = Time.deltaTime; _life -= dt;
            _vel += Vector3.down * 9.8f * dt;
            Vector3 step = _vel * dt;
            if (Physics.Raycast(transform.position, step.normalized, out var hit, step.magnitude + 0.05f, ~((1 << World.GhostLayer) | (1 << 5)), QueryTriggerInteraction.Ignore) && !(hit.collider is CharacterController))
            {
                var h = hit.collider.GetComponentInParent<IHittable>();
                AudioManager.I.PlayAt("arrow_hit", hit.point, 0.9f);
                if (h != null && !(h is ResourceNode)) h.Hit(_bow, _bow.Damage, hit.point);
                if (Random.value < 0.7f) Pickup.Spawn("arrow", 1, hit.point + hit.normal * 0.15f);
                Destroy(gameObject); return;
            }
            transform.position += step;
            transform.rotation = Quaternion.LookRotation(_vel) * Quaternion.Euler(90, 0, 0);
            if (_life <= 0 || transform.position.y < World.SeaLevel - 3f) Destroy(gameObject);
        }
    }
}
`;
  fs.writeFileSync(dir + 'Game/PlayerInteraction.cs', pi);
}

// ---------------------------------------------------------------- GameManager: flags, world size, new systems, save
patch('Game/GameManager.cs', [
  ['        public const float SeaLevel = 6f;', '        public const float SeaLevel = 6f;\n        public const float Size = 1000f;\n        public static Vector3 Center => new Vector3(Size * 0.5f, SeaLevel, Size * 0.5f);'],
  ['        public int RaftStage;', `        public int RaftStage;
        public readonly HashSet<string> Flags = new HashSet<string>();
        public EventDirector Events { get; private set; }

        /// <summary>Speaks a narrator line the first time something happens.</summary>
        public void SayOnce(string key) { if (Flags.Add("said_" + key)) AudioManager.I.Say(key, Story.Line(key)); }
        public int DiscoveredCount { get { int n = 0; foreach (var l in Landmark.All) if (l.Found) n++; return n; } }`],
  ['            gameObject.AddComponent<AnimalSpawner>();', '            gameObject.AddComponent<AnimalSpawner>();\n            gameObject.AddComponent<WorldStreamer>();\n            Events = gameObject.AddComponent<EventDirector>();'],
  ['new Vector3(250, 12, 80)', 'new Vector3(World.Size * 0.5f, 12, 120)'],
  ['            d.notes = new List<int>(Notes);', '            d.flags = new List<string>(Flags); d.unlocked = Inv.Unlocked; d.poison = Vitals.PoisonTime;\n            d.notes = new List<int>(Notes);'],
  ['            Notes.AddRange(d.notes);', '            foreach (var f in d.flags) Flags.Add(f);\n            Inv.Unlocked = Mathf.Clamp(d.unlocked, Inventory.BaseSlots, Inventory.Size); Vitals.SetPoison(d.poison);\n            Notes.AddRange(d.notes);'],
]);
patch('Game/SaveSystem.cs', [
  ['        public int version = 1;', '        public int version = 2;\n        public List<string> flags = new List<string>();\n        public int unlocked = Inventory.BaseSlots; public float poison;'],
  ['                return d != null && d.slots != null && d.slots.Length == Inventory.Size ? d : null;', '                return d != null && d.version == 2 && d.slots != null && d.slots.Length == Inventory.Size ? d : null;'],
]);
patch('UI/Cinematics.cs', [['new Vector3(250, 0, 250)', 'new Vector3(World.Size * 0.5f, 0, World.Size * 0.5f)']]);

// ---------------------------------------------------------------- Story: new lines
const story = JSON.parse(fs.readFileSync('C:/Survival Fable 62/Source/Story/story.json', 'utf8'));
let st = fs.readFileSync(dir + 'Game/Story.cs', 'utf8');
const marker = '            { "boar", "Boars. They\'re dangerous, but they avoid the fire." },';
if (st.includes(marker) && !st.includes('found_falls')) {
  const extra = Object.entries(story.lines).filter(([k]) => !st.includes('{ "' + k + '"')).map(([k, v]) => '            { "' + k + '", "' + v.replace(/"/g, '\\"') + '" },').join('\n');
  st = st.replace(marker, marker + '\n' + extra);
  fs.writeFileSync(dir + 'Game/Story.cs', st);
  results.push('ok   Story.cs lines added');
} else results.push((st.includes('found_falls') ? 'ok   ' : 'MISS ') + 'Story.cs');
console.log(results.join('\n'));
