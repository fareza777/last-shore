using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LastShore
{
    /// <summary>Opening: the Meridian is lost in a night storm.</summary>
    public class IntroCinematic : MonoBehaviour
    {
        Transform _boat, _cam; Light _light; ParticleSystem _rain;
        int _shot; float _sink, _flash, _nextFlash = 2.5f, _t; bool _sinking, _finished; Vector3 _boatPos;

        IEnumerator Start()
        {
            GameSettings.Load();
            Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            _boat = World.Find("Boat"); _cam = Camera.main.transform;
            var lg = GameObject.Find("StormLight"); if (lg != null) _light = lg.GetComponent<Light>();
            _boatPos = _boat.position;
            _rain = FX.Rain(transform);
            var em = _rain.emission; em.rateOverTime = 2600;
            var a = AudioManager.I; a.StopAllLayers(); a.SetLayer("storm", 1f); a.SetLayer("rain", 0.5f);
            CinemaOverlay.Create(Finish);

            yield return new WaitForSeconds(1.6f);
            for (int i = 0; i < 4; i++)
            {
                _shot = i;
                if (i == 1) a.Play("wave_crash", 0.9f, 0);
                if (i == 3) { _sinking = true; a.Play("wave_crash", 1f, 0); }
                a.Say("intro_" + (i + 1), Story.Intro[i]);
                yield return null;
                float min = 0;
                while (a.VoicePlaying || min < (i == 0 ? 5.5f : 4.2f)) { min += Time.deltaTime; yield return null; }
            }
            bool black = false;
            SceneFader.FadeOut(2.2f, () => black = true);
            a.SetLayer("storm", 0); a.SetLayer("rain", 0);
            while (!black) yield return null;
            a.SetLayer("ocean", 0.6f);
            yield return new WaitForSeconds(1.2f);
            a.Say("intro_5", Story.Intro[4]);
            yield return null;
            while (a.VoicePlaying) yield return null;
            yield return new WaitForSeconds(0.6f);
            Finish();
        }

        void Finish()
        {
            if (_finished) return;
            _finished = true;
            StopAllCoroutines();
            AudioManager.I.StopVoice(); AudioManager.I.SetLayer("storm", 0); AudioManager.I.SetLayer("rain", 0);
            GameManager.StartNewGame = true;
            SceneFader.Load("Game", 0.8f);
        }

        void Update()
        {
            if (_boat == null || _finished) return;
            _t += Time.deltaTime; float t = _t;
            if (_sinking) _sink += Time.deltaTime;
            float roll = Mathf.Sin(t * 1.1f) * 16f + Mathf.Sin(t * 2.3f) * 6f + _sink * 9f;
            float pitch = Mathf.Sin(t * 0.8f + 1f) * 9f - _sink * 5f;
            _boat.position = _boatPos + new Vector3(0, Mathf.Sin(t * 0.9f) * 0.9f - _sink * 0.5f, 0);
            _boat.rotation = Quaternion.Euler(pitch, 20f + Mathf.Sin(t * 0.3f) * 8f, roll);

            Vector3 b = _boatPos + Vector3.up * 1.5f; Vector3 pos; Vector3 look = b + Vector3.up * 1.5f;
            switch (_shot)
            {
                case 0: { float a = 0.5f + t * 0.035f; pos = b + new Vector3(Mathf.Cos(a) * 19f, 4.5f, Mathf.Sin(a) * 19f); break; }
                case 1: pos = b + new Vector3(5.5f, 1.6f + Mathf.Sin(t * 0.9f) * 0.5f, -4.5f); look = b + Vector3.up * 2.5f; break;
                case 2: pos = b + new Vector3(-8f, -0.4f + Mathf.Sin(t * 1.3f) * 0.35f, 6.5f); look = b + Vector3.up * 3.5f; break;
                default: pos = b + new Vector3(11f, 7f + _sink * 0.3f, -13f); look = b; break;
            }
            Vector3 shake = new Vector3(Mathf.PerlinNoise(t * 2.2f, 0) - 0.5f, Mathf.PerlinNoise(0, t * 2.2f) - 0.5f, 0) * 0.5f;
            _cam.position = Vector3.Lerp(_cam.position, pos + shake, Time.deltaTime * (_t < 0.3f ? 100f : 3f));
            _cam.rotation = Quaternion.Slerp(_cam.rotation, Quaternion.LookRotation(look - _cam.position) * Quaternion.Euler(shake.y * 3f, shake.x * 3f, Mathf.Sin(t * 0.7f) * 3.5f), Time.deltaTime * 4f);
            if (_rain != null) _rain.transform.position = _cam.position + Vector3.up * 12f + _cam.forward * 6f;

            _nextFlash -= Time.deltaTime; _flash = Mathf.MoveTowards(_flash, 0, Time.deltaTime * 3f);
            if (_nextFlash <= 0)
            {
                _nextFlash = Random.Range(2.5f, 6.5f); _flash = 1f;
                Tween.Delay(this, Random.Range(0.3f, 1.1f), () => AudioManager.I.Play("thunder", 0.85f, 0.12f), false);
            }
            float flicker = _flash * (0.6f + Mathf.PerlinNoise(t * 30f, 3f) * 0.6f);
            if (_light != null) _light.intensity = 0.22f + flicker * 3.2f;
            RenderSettings.ambientLight = new Color(0.05f, 0.065f, 0.09f) + Color.white * flicker * 0.3f;
            RenderSettings.fogColor = new Color(0.035f, 0.05f, 0.07f) + new Color(0.5f, 0.55f, 0.65f) * flicker * 0.5f;
            Shader.SetGlobalColor("_LS_Horizon", RenderSettings.fogColor);
            Shader.SetGlobalFloat("_LS_Day", 0.1f + flicker * 0.5f);
        }
    }
}
