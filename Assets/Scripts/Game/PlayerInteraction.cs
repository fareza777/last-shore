using System;
using System.Collections;
using UnityEngine;

namespace LastShore
{
    public interface IInteractable { string Prompt(); void Interact(); }
    public interface IHittable { void Hit(ItemDef tool, float damage, Vector3 point); }

    /// <summary>Hotbar, held item view, looking at / using things, tool swings and fishing.</summary>
    public class PlayerInteraction : MonoBehaviour
    {
        public int Selected { get; private set; }
        public bool HoldingTorch { get; private set; }
        public event Action SelectionChanged;

        PlayerController _pc; PlayerVitals _vitals; Inventory _inv;
        GameObject _held, _arm; string _heldId; Light _torchLight; GameObject _torchFire;
        float _cooldown, _torchBurn; bool _swinging;
        IInteractable _target;
        int _mask;

        // fishing
        int _fish; float _fishTimer, _reel, _reelSpeed, _zone; Vector3 _fishPos, _fishStart; GameObject _bobber;
        const float ZoneWidth = 0.24f;

        public ItemStack Current => _inv.Slots[Selected];

        void Awake()
        {
            _pc = GetComponent<PlayerController>(); _vitals = GetComponent<PlayerVitals>(); _inv = GameManager.I.Inv;
            _mask = ~((1 << World.GhostLayer) | (1 << 5));
            _inv.Changed += RefreshHeld;
            var armSrc = Resources.Load<GameObject>("Models/Arm");
            if (armSrc != null)
            {
                _arm = Instantiate(armSrc, _pc.Hand); _arm.name = "Arm";
                _arm.transform.localRotation = Quaternion.Euler(58f, -26f, 0f); _arm.transform.localPosition = new Vector3(0.03f, -0.09f, -0.03f);
                foreach (var r in _arm.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            RefreshHeld();
        }

        public void Select(int slot)
        {
            slot = (slot % Inventory.HotbarSize + Inventory.HotbarSize) % Inventory.HotbarSize;
            if (slot == Selected && _heldId == Current.id) return;
            Selected = slot; CancelFishing();
            RefreshHeld(); SelectionChanged?.Invoke();
        }

        void RefreshHeld()
        {
            var s = Current;
            string id = s.Empty ? null : s.id;
            if (id == _heldId) return;
            _heldId = id;
            if (_held != null) Destroy(_held);
            _torchLight = null; _torchFire = null; HoldingTorch = false;
            if (id == null) return;
            var def = s.Def;
            var src = Resources.Load<GameObject>("Models/" + def.Model);
            if (src == null) return;
            _held = Instantiate(src, _pc.Hand);
            foreach (var r in _held.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (def.Kind == ItemKind.Tool)
            {
                _held.transform.localScale = Vector3.one * 0.72f;
                _held.transform.localPosition = def.Tool == ToolType.Spear ? new Vector3(0, -0.25f, -0.25f) : def.Tool == ToolType.Rod ? new Vector3(0, -0.2f, 0) : Vector3.zero;
                _held.transform.localRotation = def.Tool == ToolType.Spear ? Quaternion.Euler(78, -8, 0) : def.Tool == ToolType.Rod ? Quaternion.Euler(40, 0, 0) : Quaternion.Euler(12, -100, -8);
                if (def.Tool == ToolType.Torch)
                {
                    HoldingTorch = true;
                    _torchFire = FX.Fire(_held.transform, new Vector3(0, 0.5f, 0), 0.35f, false, 21f);
                    _torchLight = _torchFire.GetComponentInChildren<Light>();
                }
            }
            else
            {
                var b = new Bounds(_held.transform.position, Vector3.zero);
                foreach (var r in _held.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
                float k = 0.26f / Mathf.Max(0.05f, Mathf.Max(b.size.x, b.size.y, b.size.z));
                _held.transform.localScale = Vector3.one * Mathf.Min(k, 1.4f);
                _held.transform.localPosition = new Vector3(0, -0.02f, 0);
                _held.transform.localRotation = Quaternion.Euler(-10, 30, 0);
            }
            _pc.Hand.localRotation = Quaternion.Euler(40, 0, 0);
            Tween.To(_pc.Hand, 40, 0, 0.25f, v => { if (!_swinging) _pc.Hand.localRotation = Quaternion.Euler(v, 0, 0); }, Ease.OutBack, 0, null, false);
        }

        void Update()
        {
            var gm = GameManager.I;
            _cooldown -= Time.deltaTime;
            if (gm.InputLocked) { gm.Hud.SetPrompt(null); return; }

            if (GameInput.HotbarKey >= 0) Select(GameInput.HotbarKey);
            else if (GameInput.HotbarScroll != 0) Select(Selected + GameInput.HotbarScroll);

            // ---- what are we looking at
            _target = null;
            var cam = _pc.Cam.transform;
            if (Physics.Raycast(cam.position, cam.forward, out var hit, 3.3f, _mask, QueryTriggerInteraction.Collide))
                _target = hit.collider.GetComponentInParent<IInteractable>();
            string prompt = _target?.Prompt();
            if (_fish >= 2) prompt = null;
            gm.Hud.SetPrompt(string.IsNullOrEmpty(prompt) ? null : prompt);
            if (GameInput.Interact && _target != null && !string.IsNullOrEmpty(prompt)) { _target.Interact(); return; }

            // ---- torch burns while held
            if (HoldingTorch && !Current.Empty)
            {
                _torchBurn += Time.deltaTime;
                if (_torchBurn >= 1f) { _torchBurn = 0; Wear(1); }
            }

            UpdateFishing();
            if (GameInput.UseDown && _cooldown <= 0) Use();
        }

        void Use()
        {
            var s = Current;
            if (_fish != 0) { FishingUse(); return; }
            if (s.Empty) { StartCoroutine(Swing(null)); return; }
            var def = s.Def;
            switch (def.Kind)
            {
                case ItemKind.Tool:
                    if (def.Tool == ToolType.Bow) { Shoot(def); return; }
                    if (def.Tool == ToolType.Rod && TryCast()) return;
                    StartCoroutine(Swing(def)); break;
                case ItemKind.Food: case ItemKind.Drink: case ItemKind.Medical: Consume(Selected); break;
                case ItemKind.Placeable: GameManager.I.Build.TryPlace(); break;
                default: StartCoroutine(Swing(null)); break;
            }
        }

        public void Consume(int slot)
        {
            var s = _inv.Slots[slot];
            if (s.Empty) return;
            var def = s.Def;
            if (def.Kind != ItemKind.Food && def.Kind != ItemKind.Drink && def.Kind != ItemKind.Medical) return;
            if (def.Kind == ItemKind.Medical && _vitals.Health >= 100) { GameManager.I.Hud.Hint("You are not hurt."); return; }
            _cooldown = 0.7f;
            _vitals.Consume(def);
            AudioManager.I.Play(def.Kind == ItemKind.Drink || def.Id == "coconut" ? "drink" : def.Kind == ItemKind.Medical ? "craft" : "eat", 0.8f);
            _inv.RemoveAt(slot);
            if (def.ReturnsItem != null) _inv.Add(def.ReturnsItem, 1, -1, false);
            GameManager.I.Quests.Notify("consume", def.Id);
            if (_held != null && slot == Selected) Tween.Punch(_pc.Hand, 0.25f, 0.3f);
        }

        void Wear(int amount)
        {
            var s = Current;
            if (s.Empty || s.Def.Durability <= 0 || Camp.SaveWear) return;
            s.durability -= amount;
            if (s.durability <= 0)
            {
                GameManager.I.Hud.Hint(s.Def.Name + (s.Def.Tool == ToolType.Torch ? " burned out." : " broke."));
                AudioManager.I.Play("rock_break", 0.5f);
                _inv.RemoveAt(Selected);
            }
            else _inv.NotifyChanged();
        }

        IEnumerator Swing(ItemDef tool)
        {
            if (_swinging) yield break;
            _swinging = true; _cooldown = tool == null ? 0.5f : tool.Tool == ToolType.Knife ? 0.38f : 0.62f;
            _vitals.UseStamina(tool == null ? 2f : 4f);
            AudioManager.I.Play("swing", 0.45f, 0.15f);
            var hand = _pc.Hand; bool hitDone = false; float t = 0, dur = 0.42f;
            while (t < dur)
            {
                t += Time.deltaTime; float u = t / dur;
                float ang = u < 0.3f ? Mathf.Lerp(0, -38, u / 0.3f) : u < 0.55f ? Mathf.Lerp(-38, 62, (u - 0.3f) / 0.25f) : Mathf.Lerp(62, 0, (u - 0.55f) / 0.45f);
                hand.localRotation = tool != null && tool.Tool == ToolType.Spear
                    ? Quaternion.identity
                    : Quaternion.Euler(ang, -ang * 0.25f, 0);
                if (tool != null && tool.Tool == ToolType.Spear) hand.localPosition += Vector3.forward * (u < 0.45f ? 1 : -0.8f) * Time.deltaTime * 2.2f;
                if (!hitDone && u >= 0.5f) { hitDone = true; ApplyHit(tool); }
                yield return null;
            }
            hand.localRotation = Quaternion.identity; _swinging = false;
        }

        void ApplyHit(ItemDef tool)
        {
            var cam = _pc.Cam.transform;
            float range = tool != null && tool.Tool == ToolType.Spear ? 3.7f : 2.9f;
            if (!Physics.SphereCast(cam.position, 0.18f, cam.forward, out var hit, range, _mask, QueryTriggerInteraction.Ignore)) return;
            var h = hit.collider.GetComponentInParent<IHittable>();
            if (h == null)
            {
                if (hit.collider is TerrainCollider) FX.Burst(hit.point, new Color(0.8f, 0.72f, 0.5f), 5, 0.08f, 1.5f);
                return;
            }
            h.Hit(tool, tool != null ? tool.Damage : 5f, hit.point);
            Animal.Noise(hit.point, 18f);
            _pc.Shake(0.3f); Haptics.Pulse(18);
            Tween.Punch(_pc.Hand, 0.1f, 0.16f);
            if (tool != null) Wear(1);
        }

        void Shoot(ItemDef bow)
        {
            if (_inv.Count("arrow") < 1) { GameManager.I.Hud.Hint("No arrows. Craft some from sticks, flint and feathers."); _cooldown = 0.4f; return; }
            _inv.Remove("arrow", 1); _cooldown = 0.95f;
            AudioManager.I.Play("bow", 0.8f, 0.1f);
            var cam = _pc.Cam.transform;
            ArrowProjectile.Fire(cam.position + cam.forward * 0.6f + cam.right * 0.12f - cam.up * 0.08f, cam.forward, bow);
            Tween.Punch(_pc.Hand, 0.18f, 0.25f);
            Wear(1);
        }

        // ---------------------------------------------------------------- fishing
        bool TryCast()
        {
            var cam = _pc.Cam.transform;
            if (cam.forward.y > -0.03f) return false;
            float d = (cam.position.y - World.SeaLevel) / -cam.forward.y;
            if (d < 1.5f || d > 14f) return false;
            Vector3 p = cam.position + cam.forward * d;
            if (World.Height(p) > World.SeaLevel - 0.25f) return false;
            if (Physics.Raycast(cam.position, cam.forward, d - 0.3f, _mask, QueryTriggerInteraction.Ignore)) return false;
            _fish = 1; _fishTimer = UnityEngine.Random.Range(4f, 9f); _fishPos = p; _fishStart = transform.position; _cooldown = 0.6f;
            AudioManager.I.Play("cast", 0.8f);
            _bobber = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(_bobber.GetComponent<Collider>());
            _bobber.transform.position = p; _bobber.transform.localScale = Vector3.one * 0.14f;
            var mat = Resources.Load<Material>("Materials/Berry"); if (mat != null) _bobber.GetComponent<Renderer>().sharedMaterial = mat;
            GameManager.I.Hud.Hint("Line is out. Wait for a bite...", 3f);
            return true;
        }

        void UpdateFishing()
        {
            if (_fish == 0) return;
            if ((transform.position - _fishStart).sqrMagnitude > 4f || Current.Empty || Current.Def.Tool != ToolType.Rod) { CancelFishing(); return; }
            _fishTimer -= Time.deltaTime;
            if (_bobber != null)
            {
                float dip = _fish >= 2 ? Mathf.Sin(Time.time * 22f) * 0.09f - 0.08f : Mathf.Sin(Time.time * 2f) * 0.03f;
                _bobber.transform.position = _fishPos + Vector3.up * dip;
            }
            if (_fish == 1 && _fishTimer <= 0)
            {
                _fish = 2; _fishTimer = 1.7f;
                AudioManager.I.PlayAt("fish_bite", _fishPos, 1f);
                GameManager.I.Hud.Hint(GameInput.TouchMode ? "A BITE!  Tap USE" : "A BITE!  Click now", 1.7f, true);
                Haptics.Pulse(60);
            }
            else if (_fish == 2 && _fishTimer <= 0)
            {
                GameManager.I.Hud.Hint("It got away.");
                CancelFishing();
            }
            else if (_fish == 3)
            {
                // reeling: a marker sweeps the bar, land it inside the green zone
                _reel += Time.deltaTime * _reelSpeed;
                GameManager.I.Hud.FishBar(true, Mathf.PingPong(_reel, 1f), _zone, ZoneWidth);
                if (_fishTimer <= 0) { GameManager.I.Hud.Hint("The line went slack."); CancelFishing(); }
            }
        }

        void FishingUse()
        {
            _cooldown = 0.5f;
            if (_fish == 2)
            {
                // hooked: now reel it in
                _fish = 3; _fishTimer = 6f; _reel = UnityEngine.Random.value; _reelSpeed = UnityEngine.Random.Range(0.8f, 1.15f); _zone = UnityEngine.Random.Range(0.2f, 0.8f);
                GameManager.I.Hud.Hint(GameInput.TouchMode ? "Hooked!  Tap USE when the marker is in the green" : "Hooked!  Click when the marker is in the green", 3f, true);
                Haptics.Pulse(25);
                return;
            }
            if (_fish == 3)
            {
                float miss = Mathf.Abs(Mathf.PingPong(_reel, 1f) - _zone);
                if (miss <= ZoneWidth * 0.5f)
                {
                    bool perfect = miss < 0.04f;
                    _inv.Add("fish_raw", perfect ? 2 : 1);
                    if (perfect) GameManager.I.Hud.Hint("Perfect catch!  Two fish", 2f, true);
                    AudioManager.I.Play("splash", 0.6f); Haptics.Pulse(40);
                    FX.Burst(_fishPos, new Color(0.8f, 0.92f, 1f, 0.8f), 14, 0.09f, 3f);
                    Wear(1);
                }
                else GameManager.I.Hud.Hint("It slipped the hook.");
            }
            else GameManager.I.Hud.Hint("Reeled in too early.");
            CancelFishing();
        }

        void CancelFishing()
        {
            _fish = 0;
            if (_bobber != null) Destroy(_bobber);
            if (GameManager.I != null && GameManager.I.Hud != null) GameManager.I.Hud.FishBar(false, 0, 0, 0);
        }
    }

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
                if (UnityEngine.Random.value < 0.7f) Pickup.Spawn("arrow", 1, hit.point + hit.normal * 0.15f);
                Destroy(gameObject); return;
            }
            transform.position += step;
            transform.rotation = Quaternion.LookRotation(_vel) * Quaternion.Euler(90, 0, 0);
            if (_life <= 0 || transform.position.y < World.SeaLevel - 3f) Destroy(gameObject);
        }
    }
}
