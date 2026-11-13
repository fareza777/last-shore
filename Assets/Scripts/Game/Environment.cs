using System;
using UnityEngine;

namespace LastShore
{
    /// <summary>Time of day: drives the sun/moon light, ambient colours, fog, sky exposure and stars.</summary>
    public class DayNight : MonoBehaviour
    {
        public float TimeOfDay = 0.3f;      // 0 = midnight, 0.25 = sunrise, 0.5 = noon, 0.75 = sunset
        public int Day = 1;
        public float DayLength = 840f;      // real seconds per full day
        public bool Frozen;                 // cinematics can stop the clock
        public float LightMul = 1f, Cloud;  // written by Weather
        public float Flash;                 // lightning
        public event Action<int> NewDay;

        public bool IsNight => TimeOfDay < 0.235f || TimeOfDay > 0.775f;
        public float DayFactor { get; private set; }

        Light _sun; Material _sky; ParticleSystem _stars; Material _starMat;

        static readonly Color NightSky = new Color(0.10f, 0.15f, 0.28f), DaySky = new Color(0.56f, 0.70f, 0.88f), DuskSky = new Color(0.78f, 0.5f, 0.42f);
        static readonly Color NightFog = new Color(0.03f, 0.05f, 0.09f), DayFog = new Color(0.68f, 0.82f, 0.92f), DuskFog = new Color(0.88f, 0.58f, 0.42f);

        void Start()
        {
            _sun = RenderSettings.sun != null ? RenderSettings.sun : FindFirstObjectByType<Light>();
            var gameSky = Resources.Load<Material>("Materials/SkyGame");
            if (gameSky != null) RenderSettings.skybox = gameSky;          // clouds, sun and moon, driven by the globals below
            else if (RenderSettings.skybox != null) { _sky = new Material(RenderSettings.skybox); RenderSettings.skybox = _sky; }
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            BuildStars();
            Apply();
        }

        void BuildStars()
        {
            var mat = FX.Additive; if (mat == null) return;
            var go = new GameObject("Stars"); go.transform.SetParent(transform, false);
            _stars = go.AddComponent<ParticleSystem>();
            _stars.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = _stars.main; main.loop = false; main.startLifetime = 100000f; main.startSpeed = 0; main.maxParticles = 700; main.playOnAwake = false;
            main.startSize = new ParticleSystem.MinMaxCurve(2.2f, 6.5f); main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.8f, 0.87f, 1f), Color.white);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var em = _stars.emission; em.rateOverTime = 0; em.SetBursts(new[] { new ParticleSystem.Burst(0, 650) });
            var sh = _stars.shape; sh.shapeType = ParticleSystemShapeType.Hemisphere; sh.radius = 700; sh.radiusThickness = 0; sh.rotation = new Vector3(-90, 0, 0);
            var r = go.GetComponent<ParticleSystemRenderer>(); _starMat = new Material(mat); r.sharedMaterial = _starMat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _stars.Play();
        }

        public void SkipTo(float t)
        {
            if (t <= TimeOfDay) { Day++; TimeOfDay = t; NewDay?.Invoke(Day); }
            else TimeOfDay = t;
            Apply();
        }

        void Update()
        {
            if (!Frozen)
            {
                TimeOfDay += Time.deltaTime / DayLength;
                if (TimeOfDay >= 1f) { TimeOfDay -= 1f; Day++; NewDay?.Invoke(Day); }
            }
            Flash = Mathf.MoveTowards(Flash, 0, Time.deltaTime * 3.5f);
            Apply();
        }

        public void Apply()
        {
            float ang = (TimeOfDay - 0.25f) * 360f;
            float elev = Mathf.Sin(ang * Mathf.Deg2Rad);
            DayFactor = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-0.08f, 0.3f, elev));
            float dusk = Mathf.Clamp01(1 - Mathf.Abs(elev) / 0.28f) * Mathf.Clamp01(elev * 6f + 0.9f);
            float mul = Mathf.Lerp(1f, 0.42f, Cloud) * LightMul;

            if (_sun != null)
            {
                bool sunUp = elev > -0.06f;
                _sun.transform.rotation = Quaternion.Euler(sunUp ? ang : ang - 180f, -38f, 0);
                Color c = sunUp ? Color.Lerp(new Color(1f, 0.52f, 0.26f), new Color(1f, 0.96f, 0.88f), Mathf.Clamp01(elev * 3f)) : new Color(0.55f, 0.68f, 1f);
                _sun.color = Color.Lerp(c, new Color(0.75f, 0.8f, 0.86f), Cloud * 0.8f);
                float inten = sunUp ? Mathf.Lerp(0.05f, 1.4f, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-0.06f, 0.35f, elev))) : Mathf.Lerp(0.08f, 0.5f, Mathf.InverseLerp(0.06f, 0.3f, -elev));
                _sun.intensity = inten * mul + Flash * 3f;
                _sun.shadowStrength = sunUp ? Mathf.Lerp(0.85f, 0.45f, Cloud) : 0.5f;
            }

            Color sky = Color.Lerp(Color.Lerp(NightSky, DaySky, DayFactor), DuskSky, dusk * 0.55f) * Mathf.Lerp(1f, 0.6f, Cloud);
            Color grey = new Color(0.5f, 0.54f, 0.58f);
            sky = Color.Lerp(sky, grey * Mathf.Lerp(0.2f, 1f, DayFactor), Cloud * 0.6f) + Color.white * Flash * 0.4f;
            RenderSettings.ambientSkyColor = sky * 1.05f;
            RenderSettings.ambientEquatorColor = Color.Lerp(sky, new Color(0.5f, 0.48f, 0.42f) * Mathf.Lerp(0.3f, 1f, DayFactor), 0.5f) * 0.8f;
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.27f, 0.22f) * Mathf.Lerp(0.3f, 1f, DayFactor) * 0.75f;
            Color fog = Color.Lerp(Color.Lerp(NightFog, DayFog, DayFactor), DuskFog, dusk * 0.6f);
            fog = Color.Lerp(fog, grey * Mathf.Lerp(0.12f, 0.9f, DayFactor), Cloud * 0.75f) + Color.white * Flash * 0.3f;
            RenderSettings.fogColor = fog;
            RenderSettings.fogDensity = Mathf.Lerp(0.0052f, 0.0026f, DayFactor) + Cloud * 0.006f;
            RenderSettings.reflectionIntensity = Mathf.Lerp(0.12f, 0.75f, DayFactor) * Mathf.Lerp(1f, 0.6f, Cloud);
            Shader.SetGlobalColor("_LS_Horizon", fog);
            Shader.SetGlobalFloat("_LS_Day", DayFactor * Mathf.Lerp(1f, 0.55f, Cloud));

            // ---- sky shader inputs
            Vector3 sunDir = Quaternion.Euler(ang, -38f, 0) * Vector3.back;
            Color top = Color.Lerp(Color.Lerp(new Color(0.012f, 0.028f, 0.085f), new Color(0.13f, 0.36f, 0.78f), DayFactor), new Color(0.30f, 0.27f, 0.50f), dusk * 0.5f);
            top = Color.Lerp(top, grey * Mathf.Lerp(0.1f, 0.72f, DayFactor), Cloud * 0.8f) + Color.white * Flash * 0.3f;
            Color sunCol = Color.Lerp(new Color(1f, 0.48f, 0.2f), new Color(1f, 0.95f, 0.82f), Mathf.Clamp01(elev * 3f)) * Mathf.Clamp01(elev * 8f + 0.6f) * Mathf.Lerp(1f, 0.12f, Cloud);
            Color moonCol = new Color(0.74f, 0.84f, 1f) * Mathf.Clamp01(-elev * 6f) * Mathf.Lerp(1f, 0.08f, Cloud);
            Color cloudCol = Color.Lerp(Color.Lerp(new Color(0.06f, 0.08f, 0.13f), new Color(0.97f, 0.98f, 1f), DayFactor), new Color(1f, 0.64f, 0.48f), dusk * 0.6f);
            cloudCol = Color.Lerp(cloudCol, grey * Mathf.Lerp(0.13f, 0.6f, DayFactor), Cloud * 0.85f) + Color.white * Flash * 0.5f;
            Shader.SetGlobalColor("_LS_SkyTop", top);
            Shader.SetGlobalVector("_LS_SunDir", sunDir); Shader.SetGlobalVector("_LS_MoonDir", -sunDir);
            Shader.SetGlobalColor("_LS_SunCol", sunCol); Shader.SetGlobalColor("_LS_MoonCol", moonCol);
            Shader.SetGlobalColor("_LS_CloudCol", cloudCol);
            Shader.SetGlobalFloat("_LS_CloudCover", Mathf.Lerp(0.2f, 1f, Cloud));

            if (_sky != null)
            {
                _sky.SetFloat("_Exposure", Mathf.Lerp(0.1f, 1.2f, DayFactor) * Mathf.Lerp(1f, 0.45f, Cloud) + Flash);
                _sky.SetFloat("_AtmosphereThickness", Mathf.Lerp(1f, 1.75f, dusk) + Cloud * 0.5f);
                _sky.SetColor("_SkyTint", Color.Lerp(new Color(0.5f, 0.5f, 0.5f), new Color(0.62f, 0.6f, 0.58f), Cloud));
            }
            if (_stars != null)
            {
                var cam = Camera.main;
                if (cam != null) _stars.transform.position = cam.transform.position;
                float a = (1 - DayFactor) * (1 - Cloud);
                _starMat.SetColor("_Color", new Color(1, 1, 1, a));
                _stars.transform.rotation = Quaternion.Euler(0, TimeOfDay * 40f, 12f);
            }
        }

        public string ClockText()
        {
            float h = TimeOfDay * 24f; int hh = Mathf.FloorToInt(h); int mm = Mathf.FloorToInt((h - hh) * 60f / 10f) * 10;
            return hh.ToString("00") + ":" + mm.ToString("00");
        }
    }

    public enum WeatherState { Clear, Overcast, Rain, Storm }

    /// <summary>Weather state machine plus the layered nature ambience (ocean, wind, birds, insects, rain).</summary>
    public class Weather : MonoBehaviour
    {
        public WeatherState State { get; private set; }
        public float RainAmount { get; private set; }
        public bool Raining => RainAmount > 0.35f;

        float _timer = 200f, _thunder = 10f, _gull = 20f;
        ParticleSystem _rain; DayNight _sky;

        void Start()
        {
            _sky = GetComponent<DayNight>();
            if (FX.Alpha != null) _rain = FX.Rain(transform);
        }

        public void Force(WeatherState s, float duration = 160f) { State = s; _timer = duration; }

        void Next()
        {
            int day = _sky.Day; var before = State;
            if (day <= 1) { State = WeatherState.Clear; _timer = 240f; return; }
            float storm = Mathf.Min(0.07f + day * 0.012f, 0.3f), rain = 0.2f, overcast = 0.2f;
            float r = UnityEngine.Random.value;
            State = r < storm ? WeatherState.Storm : r < storm + rain ? WeatherState.Rain : r < storm + rain + overcast ? WeatherState.Overcast : WeatherState.Clear;
            _timer = State == WeatherState.Clear ? UnityEngine.Random.Range(220f, 460f) : UnityEngine.Random.Range(100f, 230f);
            // a storm that blows itself out leaves something on the beach
            var gm = GameManager.I;
            if (before == WeatherState.Storm && State != WeatherState.Storm && gm != null && gm.Events != null) gm.Events.SpawnFlotsam();
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || gm.Player == null) return;
            float dt = Time.deltaTime;
            if (!_sky.Frozen) { _timer -= dt; if (_timer <= 0) Next(); }

            float targetRain = State == WeatherState.Storm ? 1f : State == WeatherState.Rain ? 0.7f : 0f;
            float targetCloud = State == WeatherState.Storm ? 1f : State == WeatherState.Rain ? 0.85f : State == WeatherState.Overcast ? 0.55f : 0f;
            RainAmount = Mathf.MoveTowards(RainAmount, targetRain, dt * 0.08f);
            _sky.Cloud = Mathf.MoveTowards(_sky.Cloud, targetCloud, dt * 0.05f);

            if (_rain != null)
            {
                var cam = gm.Player.Cam.transform;
                _rain.transform.position = cam.position + Vector3.up * 13f + new Vector3(cam.forward.x, 0, cam.forward.z) * 6f;
                var em = _rain.emission; em.rateOverTime = RainAmount * 2300f * (gm.Vitals.Sheltered ? 0.75f : 1f);
            }

            if (State == WeatherState.Storm && RainAmount > 0.8f)
            {
                if (!gm.Quests.StormSeen && !gm.Cinematic) { gm.Quests.StormSeen = true; AudioManager.I.Say("storm", Story.Line("storm")); }
                _thunder -= dt;
                if (_thunder <= 0) { _thunder = UnityEngine.Random.Range(9f, 24f); _sky.Flash = 1f; Tween.Delay(this, UnityEngine.Random.Range(0.3f, 1.4f), () => AudioManager.I.Play("thunder", 0.9f, 0.1f), false); }
            }

            // ---- ambience mix
            var a = AudioManager.I;
            float y = gm.Player.transform.position.y;
            float shore = 1 - Mathf.InverseLerp(World.SeaLevel + 1f, World.SeaLevel + 22f, y);
            a.SetLayer("ocean", Mathf.Lerp(0.12f, 0.85f, shore));
            a.SetLayer("wind", 0.18f + _sky.Cloud * 0.45f + Mathf.InverseLerp(20f, 40f, y) * 0.25f);
            a.SetLayer("rain", RainAmount * (gm.Vitals.Sheltered ? 0.6f : 0.95f));
            a.SetLayer("birds", _sky.DayFactor * (1 - RainAmount) * Mathf.Lerp(0.7f, 0.3f, shore));
            a.SetLayer("night", (1 - _sky.DayFactor) * (1 - RainAmount) * 0.65f);
            if (_sky.DayFactor > 0.6f && shore > 0.5f && RainAmount < 0.2f)
            {
                _gull -= dt;
                if (_gull <= 0) { _gull = UnityEngine.Random.Range(18f, 45f); a.Play("gull", 0.3f, 0.1f); }
            }
        }
    }
}
