using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastShore
{
    /// <summary>
    /// Persistent audio hub: layered nature ambience (no music), pooled one-shot SFX and a voice channel with subtitles.
    /// Clips are loaded lazily from Resources/Audio/{Amb,SFX,VO}.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        static AudioManager _inst;
        public static AudioManager I
        {
            get
            {
                if (_inst == null)
                {
                    var go = new GameObject("[Audio]");
                    DontDestroyOnLoad(go);
                    _inst = go.AddComponent<AudioManager>();
                    _inst.Init();
                }
                return _inst;
            }
        }

        /// <summary>Raised when a voice line starts: (text, duration seconds).</summary>
        public static event Action<string, float> Subtitle;

        readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        readonly Dictionary<string, AudioSource> _layers = new Dictionary<string, AudioSource>();
        readonly Dictionary<string, float> _layerTarget = new Dictionary<string, float>();
        readonly List<AudioSource> _pool = new List<AudioSource>();
        readonly Queue<KeyValuePair<string, string>> _voiceQueue = new Queue<KeyValuePair<string, string>>();
        AudioSource _voice, _ui;
        bool _voiceBusy;

        void Init()
        {
            GameSettings.Load();
            _voice = gameObject.AddComponent<AudioSource>(); _voice.playOnAwake = false; _voice.spatialBlend = 0;
            _ui = gameObject.AddComponent<AudioSource>(); _ui.playOnAwake = false; _ui.spatialBlend = 0; _ui.ignoreListenerPause = true;
            for (int i = 0; i < 12; i++)
            {
                var g = new GameObject("sfx" + i); g.transform.SetParent(transform);
                var s = g.AddComponent<AudioSource>(); s.playOnAwake = false; s.rolloffMode = AudioRolloffMode.Linear; s.minDistance = 2; s.maxDistance = 45; s.dopplerLevel = 0;
                _pool.Add(s);
            }
        }

        public AudioClip Clip(string folder, string name)
        {
            string key = folder + "/" + name;
            if (!_clips.TryGetValue(key, out var c))
            {
                c = Resources.Load<AudioClip>("Audio/" + key);
                _clips[key] = c;
            }
            return c;
        }

        // ---------- ambience layers ----------
        public void SetLayer(string name, float volume)
        {
            _layerTarget[name] = Mathf.Clamp01(volume);
            if (!_layers.ContainsKey(name))
            {
                var clip = Clip("Amb", name);
                if (clip == null) return;
                var s = gameObject.AddComponent<AudioSource>();
                s.clip = clip; s.loop = true; s.volume = 0; s.spatialBlend = 0; s.playOnAwake = false;
                s.time = UnityEngine.Random.Range(0f, Mathf.Max(0.1f, clip.length - 0.5f));
                _layers[name] = s;
            }
        }

        public void StopAllLayers() { foreach (var k in new List<string>(_layerTarget.Keys)) _layerTarget[k] = 0; }

        void Update()
        {
            foreach (var kv in _layers)
            {
                float target = (_layerTarget.TryGetValue(kv.Key, out var t) ? t : 0) * GameSettings.Ambience;
                var s = kv.Value;
                s.volume = Mathf.MoveTowards(s.volume, target, Time.unscaledDeltaTime * 0.45f);
                if (s.volume > 0.001f && !s.isPlaying) s.Play();
                else if (s.volume <= 0.001f && s.isPlaying && target <= 0) s.Pause();
            }
        }

        // ---------- one shots ----------
        public void Play(string name, float volume = 1f, float pitchJitter = 0.06f)
        {
            var c = Clip("SFX", name);
            if (c == null) return;
            var s = Free();
            s.spatialBlend = 0; s.pitch = 1 + UnityEngine.Random.Range(-pitchJitter, pitchJitter);
            s.volume = volume * GameSettings.Sfx; s.clip = c; s.Play();
        }

        public void PlayAt(string name, Vector3 pos, float volume = 1f, float pitchJitter = 0.08f)
        {
            var c = Clip("SFX", name);
            if (c == null) return;
            var s = Free();
            s.transform.position = pos; s.spatialBlend = 1; s.pitch = 1 + UnityEngine.Random.Range(-pitchJitter, pitchJitter);
            s.volume = volume * GameSettings.Sfx; s.clip = c; s.Play();
        }

        public void UI(string name = "ui_click", float volume = 0.7f)
        {
            var c = Clip("SFX", name);
            if (c != null) _ui.PlayOneShot(c, volume * GameSettings.Sfx);
        }

        AudioSource Free()
        {
            foreach (var s in _pool) if (!s.isPlaying) return s;
            return _pool[0];
        }

        // ---------- voice ----------
        /// <summary>Queue a narrator line. The subtitle is shown even if the clip is missing.</summary>
        public void Say(string clipName, string text)
        {
            _voiceQueue.Enqueue(new KeyValuePair<string, string>(clipName, text));
            if (!_voiceBusy) StartCoroutine(VoiceLoop());
        }

        public void StopVoice()
        {
            _voiceQueue.Clear(); _voice.Stop();
            Subtitle?.Invoke("", 0);
        }

        public bool VoicePlaying => _voiceBusy;

        IEnumerator VoiceLoop()
        {
            _voiceBusy = true;
            while (_voiceQueue.Count > 0)
            {
                var line = _voiceQueue.Dequeue();
                var c = Clip("VO", line.Key);
                float dur = c != null ? c.length : Mathf.Max(2.2f, line.Value.Length * 0.065f);
                if (c != null) { _voice.clip = c; _voice.volume = GameSettings.Voice; _voice.Play(); }
                Subtitle?.Invoke(line.Value, dur);
                float t = 0;
                while (t < dur + 0.35f) { t += Time.unscaledDeltaTime; yield return null; }
            }
            _voiceBusy = false;
        }
    }
}
