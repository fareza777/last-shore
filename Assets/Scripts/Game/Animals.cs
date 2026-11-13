using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastShore
{
    /// <summary>Anything burning that animals respect.</summary>
    public static class Fires
    {
        public static bool Near(Vector3 pos, float radius)
        {
            if (Campfire.AnyLitNear(pos, radius)) return true;
            foreach (var t in TorchStand.All) if ((t.transform.position - pos).sqrMagnitude < radius * radius * 0.5f) return true;
            foreach (var p in SignalPyre.All) if (p.Lit && (p.transform.position - pos).sqrMagnitude < radius * radius * 4f) return true;
            return false;
        }

        public static Vector3 Nearest(Vector3 pos)
        {
            Vector3 best = pos + Vector3.forward; float bd = float.MaxValue;
            foreach (var c in Campfire.All) if (c.Lit) { float d = (c.transform.position - pos).sqrMagnitude; if (d < bd) { bd = d; best = c.transform.position; } }
            foreach (var t in TorchStand.All) { float d = (t.transform.position - pos).sqrMagnitude; if (d < bd) { bd = d; best = t.transform.position; } }
            return best;
        }
    }

    /// <summary>
    /// Shared senses and movement for animals. No NavMesh: they sample the terrain, probe around obstacles,
    /// hear noise, see within a view cone with line of sight, and warn others of their kind.
    /// </summary>
    public abstract class Animal : MonoBehaviour, IHittable
    {
        public static readonly List<Animal> All = new List<Animal>();
        public float Health = 30f;
        public Vector3 Home;
        public System.Action<Animal> DiedEvent;
        public bool Engaged { get; protected set; }      // busy with the player: do not despawn
        public virtual bool Hostile => false;             // true for animals that attack
        protected bool Dead;
        protected float Speed;                             // current planar speed, for animation
        protected ItemDef LastTool;
        int _side = 1;

        protected const int ObstacleMask = (1 << 9) | (1 << 10) | (1 << 11);
        static readonly float[] Probe = { 40f, 80f, 125f };

        protected virtual void OnEnable() { All.Add(this); }
        protected virtual void OnDisable() { All.Remove(this); }

        protected GameManager GM => GameManager.I;
        protected Transform PlayerT => GameManager.I.Player.transform;
        protected float PlayerDist => Vector3.Distance(transform.position, PlayerT.position);
        protected bool Threat => !GM.Dead && !GM.Cinematic;

        protected Transform Deep(string name)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        // ---------------------------------------------------------------- senses
        /// <summary>Sight: inside range and view cone (or very close), with nothing solid in between.</summary>
        protected bool SeesPlayer(float range, float fovDegrees = 150f, float closeRange = 4f)
        {
            Vector3 to = PlayerT.position - transform.position; float d = to.magnitude;
            if (d > range) return false;
            if (d > closeRange && Vector3.Angle(transform.forward, new Vector3(to.x, 0, to.z)) > fovDegrees * 0.5f) return false;
            return !Physics.Linecast(transform.position + Vector3.up * 0.6f, PlayerT.position + Vector3.up * 1.3f, ObstacleMask, QueryTriggerInteraction.Ignore);
        }

        protected bool HearsPlayer(float range) => PlayerDist < range && (GM.Player.Sprinting || PlayerDist < range * 0.3f);

        protected bool PlayerLookingAtMe(float minDot = 0.5f)
        {
            Vector3 to = (transform.position - PlayerT.position); to.y = 0;
            return Vector3.Dot(PlayerT.forward, to.normalized) > minDot;
        }

        /// <summary>Loud actions (chopping, mining, fighting) are heard by everything nearby.</summary>
        public static void Noise(Vector3 pos, float radius)
        {
            for (int i = All.Count - 1; i >= 0; i--)
                if ((All[i].transform.position - pos).sqrMagnitude < radius * radius) All[i].OnNoise(pos);
        }

        protected virtual void OnNoise(Vector3 pos) { }
        protected virtual void OnAlert(Vector3 threat) { }
        public void Alert(Vector3 threat) { OnAlert(threat); }

        protected void AlertKind(float radius)
        {
            foreach (var a in All)
                if (a != this && a.GetType() == GetType() && (a.transform.position - transform.position).sqrMagnitude < radius * radius) a.OnAlert(PlayerT.position);
        }

        // ---------------------------------------------------------------- movement
        protected virtual bool CanStand(Vector3 p) => World.IsLand(p, 0.3f) && World.Slope(p) < 40f;

        /// <summary>Moves along dir if the ground ahead is walkable. Returns false when blocked.</summary>
        protected bool Step(Vector3 dir, float speed, float turn = 6f)
        {
            dir.y = 0; if (dir.sqrMagnitude < 0.0001f) { Speed = 0; return true; }
            dir.Normalize();
            Vector3 next = transform.position + dir * speed * Time.deltaTime;
            if (!CanStand(next + dir * 0.6f) || Physics.Raycast(transform.position + Vector3.up * 0.5f, dir, 1.1f, ObstacleMask, QueryTriggerInteraction.Ignore)) { Speed = 0; return false; }
            next.y = World.Height(next);
            transform.position = next;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * turn);
            Speed = speed; return true;
        }

        /// <summary>Like Step, but probes left and right around whatever is in the way and remembers the side that worked.</summary>
        protected bool MoveSmart(Vector3 dir, float speed, float turn = 6f)
        {
            if (Step(dir, speed, turn)) return true;
            foreach (float a in Probe)
            {
                if (Step(Quaternion.Euler(0, a * _side, 0) * dir, speed, turn)) return true;
                if (Step(Quaternion.Euler(0, -a * _side, 0) * dir, speed, turn)) { _side = -_side; return true; }
            }
            return false;
        }

        protected void Face(Vector3 worldPos, float turn = 6f)
        {
            Vector3 d = worldPos - transform.position; d.y = 0;
            if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), Time.deltaTime * turn);
        }

        protected Vector3 WanderTarget(float radius)
        {
            for (int i = 0; i < 6; i++)
            {
                Vector2 r = Random.insideUnitCircle * radius; Vector3 p = Home + new Vector3(r.x, 0, r.y);
                if (CanStand(p)) return p;
            }
            return Home;
        }

        protected static void SwingLegs(Transform[] legs, ref float phase, float speed, float stride = 3.6f, float maxSwing = 32f)
        {
            phase += speed * Time.deltaTime * stride;
            float swing = Mathf.Clamp01(speed / 2f) * maxSwing;
            for (int i = 0; i < legs.Length; i++)
                if (legs[i] != null) legs[i].localRotation = Quaternion.Euler(Mathf.Sin(phase + (i % 2 == 0 ? 0 : Mathf.PI)) * swing, 0, 0);
        }

        // ---------------------------------------------------------------- damage
        public void Hit(ItemDef tool, float damage, Vector3 point)
        {
            if (Dead) return;
            LastTool = tool;
            OnHit(tool, damage, point);
        }

        protected abstract void OnHit(ItemDef tool, float damage, Vector3 point);

        protected void Blood(Vector3 point, string sfx = "hit")
        {
            AudioManager.I.PlayAt(sfx, point);
            FX.Burst(point, new Color(0.45f, 0.1f, 0.08f), 8, 0.08f, 2f);
        }

        protected IEnumerator DieRoutine(params (string id, int n)[] drops)
        {
            Dead = true; Speed = 0; Engaged = false;
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            Quaternion r0 = transform.rotation; float t = 0;
            while (t < 1f) { t += Time.deltaTime / 0.5f; transform.rotation = r0 * Quaternion.Euler(0, 0, Tween.Evaluate(Ease.OutQuad, t) * 88f); yield return null; }
            bool knife = LastTool != null && LastTool.Tool == ToolType.Knife;
            foreach (var d in drops)
                Pickup.Spawn(d.id, d.n + (knife && (d.id.Contains("raw") || d.id == "hide") ? 1 : 0), transform.position + Vector3.up * 0.4f + Random.insideUnitSphere * 0.5f);
            DiedEvent?.Invoke(this);
            yield return new WaitForSeconds(10f);
            Tween.Scale(transform, Vector3.zero, 0.6f, Ease.InCubic, 0, () => { if (this != null) Destroy(gameObject); });
        }
    }

    /// <summary>Ambient birds circling (gulls over the coast, parrots over the jungle).</summary>
    public class Gull : MonoBehaviour
    {
        public Vector3 Center; public float Radius = 40f, Height = 26f, Speed = 0.16f, Phase;
        Transform _l, _r;
        void Start() { foreach (var t in GetComponentsInChildren<Transform>(true)) { if (t.name == "WingL") _l = t; else if (t.name == "WingR") _r = t; } }
        void Update()
        {
            float a = Time.time * Speed + Phase;
            Vector3 p = Center + new Vector3(Mathf.Cos(a) * Radius, Height + Mathf.Sin(a * 2.3f) * 3f, Mathf.Sin(a) * Radius * 0.8f);
            Vector3 fwd = new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a) * 0.8f) * Mathf.Sign(Speed);
            transform.SetPositionAndRotation(p, Quaternion.LookRotation(fwd) * Quaternion.Euler(0, 0, -14f * Mathf.Sign(Speed)));
            float flap = Mathf.Sin(Time.time * 5f + Phase * 3f) * (Mathf.Sin(Time.time * 0.4f + Phase) > 0.2f ? 32f : 5f);
            if (_l != null) _l.localRotation = Quaternion.Euler(0, 0, -flap);
            if (_r != null) _r.localRotation = Quaternion.Euler(0, 0, flap);
        }
    }

    /// <summary>A butterfly drifting around a patch of ground.</summary>
    public class Flutter : MonoBehaviour
    {
        public Vector3 Anchor; float _seed; Transform _l, _r;
        void Start() { _seed = Random.value * 100f; foreach (var t in GetComponentsInChildren<Transform>(true)) { if (t.name == "WingL") _l = t; else if (t.name == "WingR") _r = t; } }
        void Update()
        {
            float t = Time.time * 0.5f + _seed;
            Vector3 p = Anchor + new Vector3((Mathf.PerlinNoise(t, _seed) - 0.5f) * 7f, 0.6f + Mathf.PerlinNoise(_seed, t) * 1.6f, (Mathf.PerlinNoise(t + 9, _seed + 4) - 0.5f) * 7f);
            Vector3 d = p - transform.position;
            if (d.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(new Vector3(d.x, 0, d.z) + Vector3.forward * 0.001f), Time.deltaTime * 5f);
            transform.position = p;
            float flap = Mathf.Abs(Mathf.Sin(Time.time * 16f + _seed)) * 70f;
            if (_l != null) _l.localRotation = Quaternion.Euler(0, 0, flap);
            if (_r != null) _r.localRotation = Quaternion.Euler(0, 0, -flap);
        }
    }

    /// <summary>Spawns animal groups around the player and puts distant ones to sleep, so a big island stays cheap.</summary>
    public class AnimalSpawner : MonoBehaviour
    {
        class Slot { public Vector3 Pos; public string Prefab; public int Count; public readonly List<Animal> Alive = new List<Animal>(); public bool Active; public int Dead; public float RespawnAt = -1; }
        readonly List<Slot> _slots = new List<Slot>();
        readonly List<Flutter> _butterflies = new List<Flutter>();
        ParticleSystem _fireflies;
        float _t, _lifeT;
        const float WakeRange = 150f, SleepRange = 215f;

        static readonly (string prefix, string prefab, int count)[] Kinds =
        {
            ("Boar", "Boar", 2), ("Deer", "Deer", 3), ("Monkey", "Monkey", 2), ("Snake", "Snake", 1), ("Turtle", "Turtle", 1), ("Fowl", "Fowl", 3), ("Crab", "Crab", 1), ("Shark", "Shark", 1),
        };

        void Start()
        {
            var root = World.Find("Spawns");
            if (root != null)
                foreach (Transform t in root)
                    foreach (var k in Kinds)
                        if (t.name.StartsWith(k.prefix)) { _slots.Add(new Slot { Pos = t.position, Prefab = k.prefab, Count = k.count }); break; }

            Birds("Gull", 12, World.Center, World.Size * 0.36f, 18f, 36f, 1.6f, true);
            Birds("Parrot", 10, World.Center, World.Size * 0.2f, 30f, 46f, 1.2f, false);

            var mat = FX.Additive;
            if (mat != null)
            {
                var go = new GameObject("Fireflies"); go.transform.SetParent(transform, false);
                _fireflies = go.AddComponent<ParticleSystem>(); _fireflies.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = _fireflies.main; main.loop = true; main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f); main.startSpeed = 0.15f; main.maxParticles = 70; main.playOnAwake = false;
                main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.11f); main.startColor = new Color(0.8f, 1f, 0.45f, 0.9f); main.simulationSpace = ParticleSystemSimulationSpace.World;
                var sh = _fireflies.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(36, 3, 36);
                var noise = _fireflies.noise; noise.enabled = true; noise.strength = 0.5f; noise.frequency = 0.35f;
                var col = _fireflies.colorOverLifetime; col.enabled = true; var g = new Gradient();
                g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.2f), new GradientAlphaKey(0.2f, 0.5f), new GradientAlphaKey(1, 0.8f), new GradientAlphaKey(0, 1) });
                col.color = g;
                var em = _fireflies.emission; em.rateOverTime = 0;
                go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
                _fireflies.Play();
            }
        }

        void Birds(string model, int count, Vector3 center, float spread, float minH, float maxH, float scale, bool coast)
        {
            var src = Resources.Load<GameObject>("Models/" + model);
            if (src == null) return;
            for (int i = 0; i < count; i++)
            {
                var g = Instantiate(src).AddComponent<Gull>();
                float a = i * 2.39996f;
                float r = coast ? spread : spread * Mathf.Sqrt((i + 1f) / count);
                g.Center = new Vector3(center.x + Mathf.Cos(a) * r, World.SeaLevel + (coast ? 0 : 14f), center.z + Mathf.Sin(a) * r);
                g.Radius = Random.Range(22f, 55f); g.Height = Random.Range(minH, maxH); g.Speed = Random.Range(0.11f, 0.22f) * (i % 2 == 0 ? 1 : -1); g.Phase = i * 1.7f;
                g.transform.localScale = Vector3.one * scale;
            }
        }

        void Spawn(Slot s)
        {
            var src = Resources.Load<GameObject>("Prefabs/" + s.Prefab);
            if (src == null) return;
            for (int i = s.Alive.Count + s.Dead; i < s.Count; i++)
            {
                Vector2 off = Random.insideUnitCircle * (s.Count > 1 ? 5f : 0.5f);
                Vector3 p = s.Pos + new Vector3(off.x, 0, off.y);
                p.y = s.Prefab == "Shark" ? World.SeaLevel - 0.45f : World.Height(p);
                var go = Instantiate(src, p, Quaternion.Euler(0, Random.Range(0, 360f), 0));
                var a = go.GetComponent<Animal>(); a.Home = s.Pos; s.Alive.Add(a);
                a.DiedEvent += dead => { s.Alive.Remove(dead); s.Dead++; if (s.RespawnAt < 0) s.RespawnAt = GameManager.I.DayFloat + 2f; };
            }
            s.Active = true;
        }

        void Update()
        {
            var gm = GameManager.I; if (gm == null || gm.Player == null) return;
            Vector3 pp = gm.Player.transform.position;
            _t -= Time.deltaTime;
            if (_t <= 0)
            {
                _t = 1.5f;
                foreach (var s in _slots)
                {
                    float d = (pp - s.Pos).magnitude;
                    if (s.RespawnAt >= 0 && gm.DayFloat >= s.RespawnAt && d > 70f) { s.Dead = 0; s.RespawnAt = -1; if (s.Active) Spawn(s); }
                    if (!s.Active && d < WakeRange) Spawn(s);
                    else if (s.Active && d > SleepRange)
                    {
                        bool busy = false;
                        foreach (var a in s.Alive) if (a != null && a.Engaged) busy = true;
                        if (busy) continue;
                        foreach (var a in s.Alive) if (a != null) Destroy(a.gameObject);
                        s.Alive.Clear(); s.Active = false;
                    }
                }
            }

            // ---- small life around the player: butterflies by day, fireflies at night
            _lifeT -= Time.deltaTime;
            bool inland = pp.y > World.SeaLevel + 2.5f; bool day = gm.Sky.DayFactor > 0.6f && !gm.Weather.Raining;
            if (_lifeT <= 0)
            {
                _lifeT = 2f;
                for (int i = _butterflies.Count - 1; i >= 0; i--)
                    if (_butterflies[i] == null || !day || (_butterflies[i].Anchor - pp).sqrMagnitude > 45 * 45) { if (_butterflies[i] != null) Destroy(_butterflies[i].gameObject); _butterflies.RemoveAt(i); }
                if (day && inland && _butterflies.Count < 9)
                {
                    var src = Resources.Load<GameObject>("Models/Butterfly");
                    if (src != null)
                    {
                        Vector2 r = Random.insideUnitCircle.normalized * Random.Range(8f, 26f);
                        Vector3 anchor = World.OnGround(pp + new Vector3(r.x, 0, r.y));
                        if (World.IsLand(anchor, 1.5f)) { var f = Instantiate(src, anchor, Quaternion.identity).AddComponent<Flutter>(); f.Anchor = anchor; _butterflies.Add(f); }
                    }
                }
            }
            if (_fireflies != null)
            {
                _fireflies.transform.position = pp + Vector3.up * 1.2f;
                var em = _fireflies.emission; em.rateOverTime = inland && gm.Sky.DayFactor < 0.25f && !gm.Weather.Raining ? 9f : 0f;
            }
        }
    }
}
