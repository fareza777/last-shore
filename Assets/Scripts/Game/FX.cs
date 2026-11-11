using UnityEngine;

namespace LastShore
{
    /// <summary>Runtime-built particle effects (bursts, fire, smoke) so no prefab wiring is needed.</summary>
    public static class FX
    {
        static ParticleSystem _burst;
        static Material _add, _alpha;

        public static Material Additive => _add != null ? _add : (_add = Resources.Load<Material>("Materials/ParticleAdd"));
        public static Material Alpha => _alpha != null ? _alpha : (_alpha = Resources.Load<Material>("Materials/ParticleAlpha"));

        static ParticleSystem NewSystem(string name, Transform parent, Material mat, bool world = true)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.simulationSpace = world ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local; main.playOnAwake = false;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }

        /// <summary>Chips / leaves / dust puff at a hit point.</summary>
        public static void Burst(Vector3 pos, Color color, int count = 10, float size = 0.11f, float speed = 2.6f)
        {
            if (_burst == null)
            {
                _burst = NewSystem("[FXBurst]", null, Alpha);
                var main = _burst.main; main.maxParticles = 400; main.gravityModifier = 1.2f; main.loop = false;
                var em = _burst.emission; em.enabled = false;
                var sol = _burst.sizeOverLifetime; sol.enabled = true; sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0.2f));
            }
            for (int i = 0; i < count; i++)
            {
                var ep = new ParticleSystem.EmitParams
                {
                    position = pos + Random.insideUnitSphere * 0.12f,
                    velocity = (Random.onUnitSphere + Vector3.up * 0.9f) * speed * Random.Range(0.4f, 1f),
                    startColor = color * Random.Range(0.8f, 1.1f), startSize = size * Random.Range(0.6f, 1.3f), startLifetime = Random.Range(0.45f, 0.9f)
                };
                _burst.Emit(ep, 1);
            }
        }

        public static ParticleSystem Rain(Transform parent)
        {
            var rain = NewSystem("Rain", parent, Alpha);
            var main = rain.main; main.loop = true; main.startLifetime = 0.85f; main.startSpeed = 24f; main.maxParticles = 2800;
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.05f); main.startColor = new Color(0.85f, 0.9f, 1f, 0.38f);
            var sh = rain.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(34, 0.1f, 34); sh.rotation = new Vector3(96, 0, 0);
            var em = rain.emission; em.rateOverTime = 0;
            var r = rain.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Stretch; r.lengthScale = 9f; r.velocityScale = 0.02f;
            rain.Play();
            return rain;
        }

        public static GameObject Fire(Transform parent, Vector3 localPos, float scale = 1f, bool withSmoke = true, float lightRange = 13f)
        {
            var root = new GameObject("Fire");
            root.transform.SetParent(parent, false); root.transform.localPosition = localPos;

            var ps = NewSystem("Flames", root.transform, Additive);
            var main = ps.main; main.loop = true; main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.95f); main.startSpeed = new ParticleSystem.MinMaxCurve(0.9f * scale, 1.9f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f * scale, 0.7f * scale); main.maxParticles = 80; main.startRotation = new ParticleSystem.MinMaxCurve(0, 6.28f);
            var em = ps.emission; em.rateOverTime = 34;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 9; sh.radius = 0.2f * scale; sh.rotation = new Vector3(-90, 0, 0);
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.9f, 0.55f), 0), new GradientColorKey(new Color(1f, 0.5f, 0.12f), 0.45f), new GradientColorKey(new Color(0.75f, 0.14f, 0.04f), 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(0.9f, 0.15f), new GradientAlphaKey(0.5f, 0.6f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var sol = ps.sizeOverLifetime; sol.enabled = true; sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0.5f), new Keyframe(0.25f, 1f), new Keyframe(1, 0.15f)));
            ps.Play();

            var sparks = NewSystem("Sparks", root.transform, Additive);
            main = sparks.main; main.loop = true; main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.8f); main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f); main.startColor = new Color(1f, 0.7f, 0.3f); main.gravityModifier = -0.15f; main.maxParticles = 40;
            em = sparks.emission; em.rateOverTime = 7 * scale;
            sh = sparks.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 22; sh.radius = 0.15f * scale; sh.rotation = new Vector3(-90, 0, 0);
            var noise = sparks.noise; noise.enabled = true; noise.strength = 0.6f; noise.frequency = 0.8f;
            sparks.Play();

            if (withSmoke)
            {
                var smoke = NewSystem("Smoke", root.transform, Alpha);
                main = smoke.main; main.loop = true; main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f); main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.5f * scale, 0.9f * scale); main.startColor = new Color(0.25f, 0.25f, 0.27f, 0.22f); main.maxParticles = 60;
                em = smoke.emission; em.rateOverTime = 6;
                sh = smoke.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 6; sh.radius = 0.15f; sh.rotation = new Vector3(-90, 0, 0); sh.position = new Vector3(0, 0.8f * scale, 0);
                var scol = smoke.colorOverLifetime; scol.enabled = true;
                var sg = new Gradient();
                sg.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.2f), new GradientAlphaKey(0, 1) });
                scol.color = sg;
                var ssol = smoke.sizeOverLifetime; ssol.enabled = true; ssol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.6f, 1, 2.6f));
                var vel = smoke.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
                vel.x = new ParticleSystem.MinMaxCurve(0.5f); vel.y = new ParticleSystem.MinMaxCurve(0f); vel.z = new ParticleSystem.MinMaxCurve(0.2f);
                smoke.Play();
            }

            var lg = new GameObject("FireLight"); lg.transform.SetParent(root.transform, false); lg.transform.localPosition = new Vector3(0, 0.6f * scale, 0);
            var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.color = new Color(1f, 0.62f, 0.3f); l.range = lightRange; l.intensity = 2.4f; l.shadows = LightShadows.None;
            lg.AddComponent<FireFlicker>();
            return root;
        }
    }

    public class FireFlicker : MonoBehaviour
    {
        Light _l; float _base, _seed; Vector3 _pos;
        void Awake() { _l = GetComponent<Light>(); _base = _l.intensity; _seed = Random.value * 100f; _pos = transform.localPosition; }
        void Update()
        {
            float n = Mathf.PerlinNoise(_seed, Time.time * 6f);
            _l.intensity = _base * (0.75f + n * 0.5f);
            transform.localPosition = _pos + new Vector3(Mathf.PerlinNoise(_seed + 5, Time.time * 4f) - 0.5f, 0, Mathf.PerlinNoise(_seed + 9, Time.time * 4f) - 0.5f) * 0.15f;
        }
    }
}
