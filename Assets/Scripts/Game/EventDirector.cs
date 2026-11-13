using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastShore
{
    /// <summary>A crate washed ashore by a world event.</summary>
    public class Flotsam : MonoBehaviour, IInteractable
    {
        static readonly (string id, int min, int max)[] Table =
        {
            ("rope", 1, 3), ("canvas", 1, 1), ("flask_empty", 1, 1), ("bandage", 1, 3), ("arrow", 4, 8), ("flint", 2, 4), ("bamboo", 2, 4), ("coconut", 2, 3), ("hide", 1, 2), ("antidote", 1, 1), ("fish_cooked", 1, 2),
        };
        public string Prompt() => "Break open the washed-up crate";
        public void Interact()
        {
            var gm = GameManager.I;
            AudioManager.I.PlayAt("rock_break", transform.position, 0.6f); AudioManager.I.Play("ui_open");
            FX.Burst(transform.position + Vector3.up * 0.4f, new Color(0.75f, 0.6f, 0.4f), 22, 0.16f, 3.5f);
            int n = Random.Range(2, 4);
            for (int i = 0; i < n; i++)
            {
                var t = Table[Random.Range(0, Table.Length)];
                int left = gm.Inv.Add(t.id, Random.Range(t.min, t.max + 1));
                if (left > 0) Pickup.Spawn(t.id, left, transform.position + Vector3.up * 0.5f);
            }
            if (gm.Events.Marker == transform) gm.Events.Marker = null;
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Unscripted moments that make the island feel alive: flotsam after rough nights, ships on the horizon,
    /// meteor showers, boars raiding the camp after dark, and the occasional tremor.
    /// </summary>
    public class EventDirector : MonoBehaviour
    {
        /// <summary>Extra compass marker for the current event (may be null).</summary>
        public Transform Marker;

        ParticleSystem _meteors; float _meteorTime, _nextMeteor, _tick;
        GameObject _ship; Vector3 _shipDir; float _shipTime;
        int _raidDay = -1, _shipDay = -1, _meteorDay = -1;
        float _quake;

        void Start()
        {
            var gm = GameManager.I;
            gm.Sky.NewDay += OnNewDay;
            var mat = FX.Additive;
            if (mat != null)
            {
                var go = new GameObject("Meteors"); go.transform.SetParent(transform, false);
                _meteors = go.AddComponent<ParticleSystem>(); _meteors.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = _meteors.main; main.loop = false; main.playOnAwake = false; main.maxParticles = 40; main.simulationSpace = ParticleSystemSimulationSpace.World;
                var em = _meteors.emission; em.enabled = false;
                var col = _meteors.colorOverLifetime; col.enabled = true; var g = new Gradient();
                g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(new Color(0.7f, 0.85f, 1f), 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.15f), new GradientAlphaKey(0, 1) });
                col.color = g;
                var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = mat; r.renderMode = ParticleSystemRenderMode.Stretch; r.lengthScale = 14f; r.velocityScale = 0.05f;
                _meteors.Play();
            }
        }

        // ---------------------------------------------------------------- flotsam
        void OnNewDay(int day)
        {
            if (day < 2 || day % 2 != 0) return;
            SpawnFlotsam();
        }

        /// <summary>Washes a crate up on the shore nearest the player and marks it on the compass.</summary>
        public void SpawnFlotsam()
        {
            var gm = GameManager.I; if (gm == null || gm.Player == null || Marker != null) return;
            Vector3 p = gm.Player.transform.position;
            Vector3 dir = p - World.Center; dir.y = 0; if (dir.sqrMagnitude < 1f) dir = Vector3.back;
            dir = Quaternion.Euler(0, Random.Range(-50f, 50f), 0) * dir.normalized;
            Vector3 q = World.Center;
            for (int i = 0; i < 700; i++) { q += dir; if (i > 40 && World.Height(q) < World.SeaLevel + 0.55f) break; }
            q -= dir * 2f; q.y = Mathf.Max(World.Height(q), World.SeaLevel + 0.1f);
            var src = Resources.Load<GameObject>("Models/Crate"); if (src == null) return;
            var go = new GameObject("Flotsam") { layer = 11 };
            go.transform.SetPositionAndRotation(q, Quaternion.Euler(Random.Range(-8f, 8f), Random.Range(0, 360f), Random.Range(-10f, 10f)));
            Instantiate(src, go.transform);
            var b = go.AddComponent<BoxCollider>(); b.size = new Vector3(0.85f, 0.6f, 0.65f); b.center = new Vector3(0, 0.3f, 0);
            go.AddComponent<Flotsam>();
            Marker = go.transform;
            gm.Hud.Toast("Something washed ashore");
            gm.SayOnce("flotsam");
        }

        // ---------------------------------------------------------------- per frame
        void Update()
        {
            var gm = GameManager.I; if (gm == null || gm.Player == null || gm.Cinematic || gm.Dead) return;
            float dt = Time.deltaTime; var sky = gm.Sky; float t = sky.TimeOfDay;
            _tick -= dt;
            if (_tick <= 0)
            {
                _tick = 2f;
                // meteor shower on clear nights
                if (sky.Day != _meteorDay && t > 0.88f && gm.Weather.State == WeatherState.Clear)
                {
                    _meteorDay = sky.Day;
                    if (Random.value < 0.4f) { _meteorTime = 110f; gm.SayOnce("meteor"); gm.Hud.Toast("Meteor shower"); }
                }
                // a ship on the horizon
                if (sky.Day >= 3 && sky.Day != _shipDay && sky.Day % 2 == 1 && t > 0.36f && t < 0.6f && _ship == null && !gm.Weather.Raining) { _shipDay = sky.Day; StartShip(); }
                // boars raid the camp after dark
                if (sky.Day >= 3 && sky.Day != _raidDay && t > 0.9f && t < 0.97f)
                {
                    _raidDay = sky.Day;
                    if (Random.value < 0.45f && Campfire.All.Count > 0) Raid();
                }
                // one tremor early in the stay
                if (sky.Day >= 3 && t > 0.55f && t < 0.7f && gm.Flags.Add("quake")) { _quake = 6.5f; AudioManager.I.Play("rumble", 1f, 0); gm.SayOnce("quake"); gm.Hud.Toast("Earth tremor"); }
            }

            if (_meteorTime > 0 && _meteors != null)
            {
                _meteorTime -= dt; _nextMeteor -= dt;
                if (_nextMeteor <= 0)
                {
                    _nextMeteor = Random.Range(0.6f, 3.2f);
                    Vector3 cam = gm.Player.Cam.transform.position; float a = Random.Range(0, 6.28f);
                    Vector3 start = cam + new Vector3(Mathf.Cos(a) * 420f, Random.Range(260f, 420f), Mathf.Sin(a) * 420f);
                    Vector3 vel = Quaternion.Euler(0, Random.Range(0, 360f), 0) * new Vector3(300f, -90f, 0);
                    _meteors.Emit(new ParticleSystem.EmitParams { position = start, velocity = vel, startLifetime = Random.Range(0.7f, 1.3f), startSize = Random.Range(2.5f, 5f), startColor = Color.white }, 1);
                }
            }

            if (_quake > 0) { _quake -= dt; gm.Player.Shake(0.35f + Mathf.Sin(_quake * 3f) * 0.25f); }

            if (_ship != null)
            {
                _shipTime -= dt;
                _ship.transform.position += _shipDir * 5.5f * dt + Vector3.up * (Mathf.Sin(Time.time * 0.6f) * 0.004f);
                if (_shipTime <= 0) { Destroy(_ship); _ship = null; }
            }
        }

        void StartShip()
        {
            var gm = GameManager.I;
            var src = Resources.Load<GameObject>("Models/Sailboat"); if (src == null) return;
            Vector3 p = gm.Player.transform.position; Vector3 outward = p - World.Center; outward.y = 0; outward.Normalize();
            if (outward.sqrMagnitude < 0.1f) outward = Vector3.back;
            _shipDir = Vector3.Cross(Vector3.up, outward);
            Vector3 start = World.Center + outward * (World.Size * 0.5f + 330f) - _shipDir * 380f; start.y = World.SeaLevel + 1.5f;
            _ship = Instantiate(src, start, Quaternion.LookRotation(_shipDir)); _ship.transform.localScale = Vector3.one * 6f; _ship.name = "PassingShip";
            _shipTime = 135f;
            AudioManager.I.Play("ship_horn", 0.7f, 0);
            gm.Hud.Toast("A ship on the horizon");
            gm.SayOnce("ship");
            if (gm.Flags.Contains("pyre_lit")) Tween.Delay(this, 9f, () => { AudioManager.I.Play("ship_horn", 0.9f, 0); GameManager.I.Hud.Hint("They sound the horn. They saw your fire, but the reef keeps them out.", 6f, true); }, false);
        }

        void Raid()
        {
            var gm = GameManager.I; Vector3 pp = gm.Player.transform.position;
            Campfire camp = null;
            foreach (var c in Campfire.All) if (!string.IsNullOrEmpty(c.Prefab) && (c.transform.position - pp).sqrMagnitude < 45 * 45) camp = c;
            if (camp == null) return;
            var src = Resources.Load<GameObject>("Prefabs/Boar"); if (src == null) return;
            float a = Random.Range(0, 6.28f); int spawned = 0;
            for (int i = 0; i < 3; i++)
            {
                Vector3 p = camp.transform.position + new Vector3(Mathf.Cos(a + i * 0.25f), 0, Mathf.Sin(a + i * 0.25f)) * (48f + i * 3f);
                if (!World.IsLand(p, 0.4f)) continue;
                var b = Instantiate(src, World.OnGround(p), Quaternion.identity).GetComponent<Animal>();
                b.Home = camp.transform.position + (p - camp.transform.position).normalized * 16f;
                b.Alert(pp); spawned++;
            }
            if (spawned == 0) return;
            AudioManager.I.PlayAt("boar_grunt", camp.transform.position + (pp - camp.transform.position).normalized * 20f, 1f);
            gm.Hud.Toast("Boars are circling the camp"); gm.SayOnce("raid");
        }
    }

    /// <summary>
    /// Keeps a large island cheap: scatter objects are bucketed into grid cells and only the cells near the player are active.
    /// Small things (pickups, plants) switch off first; trees and boulders stay visible much further.
    /// </summary>
    public class WorldStreamer : MonoBehaviour
    {
        class Cell { public Vector2 Center; public readonly List<GameObject>[] Tier = { new List<GameObject>(), new List<GameObject>(), new List<GameObject>() }; public readonly bool[] On = { true, true, true }; }
        readonly List<Cell> _cells = new List<Cell>(); readonly Dictionary<int, Cell> _map = new Dictionary<int, Cell>();
        const float CellSize = 50f;
        static readonly float[] Range = { 95f, 210f, 360f };
        float _t; int _cursor;

        void Start()
        {
            var world = World.Find("World"); if (world == null) return;
            foreach (Transform group in world)
            {
                int tier = group.name == "Pickups" || group.name == "Plants" || group.name == "Flowers" ? 0 : group.name == "Rocks" || group.name == "Decor" ? 1 : group.name == "Trees" || group.name == "Boulders" ? 2 : -1;
                if (tier < 0) continue;
                foreach (Transform t in group)
                {
                    int cx = Mathf.FloorToInt(t.position.x / CellSize), cz = Mathf.FloorToInt(t.position.z / CellSize); int key = cx * 1000 + cz;
                    if (!_map.TryGetValue(key, out var c)) { c = new Cell { Center = new Vector2((cx + 0.5f) * CellSize, (cz + 0.5f) * CellSize) }; _map[key] = c; _cells.Add(c); }
                    c.Tier[tier].Add(t.gameObject);
                }
            }
            Refresh(int.MaxValue);
        }

        void Update()
        {
            _t -= Time.deltaTime; if (_t > 0) return; _t = 0.12f;
            Refresh(40);
        }

        void Refresh(int budget)
        {
            var gm = GameManager.I; if (gm == null || gm.Player == null || _cells.Count == 0) return;
            Vector3 pp = gm.Player.Cam != null ? gm.Player.Cam.transform.position : gm.Player.transform.position; var p = new Vector2(pp.x, pp.z);
            float q = GameSettings.Quality == 0 ? 0.5f : GameSettings.Quality == 1 ? 0.78f : 1f;
            int n = Mathf.Min(budget, _cells.Count);
            for (int i = 0; i < n; i++)
            {
                _cursor = (_cursor + 1) % _cells.Count; var c = _cells[_cursor];
                float d = (c.Center - p).magnitude;
                for (int tier = 0; tier < 3; tier++)
                {
                    float r = Range[tier] * q;
                    bool want = c.On[tier] ? d < r + CellSize * 0.6f : d < r;      // hysteresis
                    if (want == c.On[tier]) continue;
                    c.On[tier] = want;
                    var list = c.Tier[tier];
                    for (int k = 0; k < list.Count; k++) if (list[k] != null) list[k].SetActive(want);
                }
            }
        }
    }
}
