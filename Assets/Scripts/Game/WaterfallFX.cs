using UnityEngine;

namespace LastShore
{
    /// <summary>Falling water, mist and sound for the waterfall landmark.</summary>
    public class WaterfallFX : MonoBehaviour
    {
        void Start()
        {
            var mat = FX.Alpha; if (mat == null) return;
            var fall = new GameObject("FallingWater"); fall.transform.SetParent(transform, false); fall.transform.localPosition = new Vector3(0, 10.9f, -0.9f);
            var ps = fall.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.playOnAwake = false; main.startLifetime = new ParticleSystem.MinMaxCurve(1.35f, 1.6f); main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.1f); main.startColor = new Color(0.9f, 0.97f, 1f, 0.5f); main.gravityModifier = 1.1f; main.maxParticles = 260; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 120;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(2.4f, 0.2f, 0.4f); sh.rotation = new Vector3(60, 0, 0);
            var r = fall.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = mat; r.renderMode = ParticleSystemRenderMode.Stretch; r.lengthScale = 2.6f; r.velocityScale = 0.12f;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();

            var mist = new GameObject("Mist"); mist.transform.SetParent(transform, false); mist.transform.localPosition = new Vector3(0, 0.6f, 1.2f);
            var mp = mist.AddComponent<ParticleSystem>(); mp.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            main = mp.main; main.loop = true; main.playOnAwake = false; main.startLifetime = new ParticleSystem.MinMaxCurve(2.2f, 3.6f); main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(2.2f, 4.5f); main.startColor = new Color(1f, 1f, 1f, 0.16f); main.maxParticles = 60; main.simulationSpace = ParticleSystemSimulationSpace.World;
            em = mp.emission; em.rateOverTime = 14;
            sh = mp.shape; sh.shapeType = ParticleSystemShapeType.Hemisphere; sh.radius = 1.6f; sh.rotation = new Vector3(-90, 0, 0);
            var col = mp.colorOverLifetime; col.enabled = true; var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.25f), new GradientAlphaKey(0, 1) });
            col.color = g;
            mist.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
            mp.Play();

            var a = gameObject.AddComponent<AudioSource>();
            a.clip = AudioManager.I.Clip("Amb", "waterfall"); a.loop = true; a.spatialBlend = 1; a.rolloffMode = AudioRolloffMode.Linear; a.minDistance = 6; a.maxDistance = 75; a.volume = 0.9f * GameSettings.Ambience;
            if (a.clip != null) a.Play();
        }
    }
}
