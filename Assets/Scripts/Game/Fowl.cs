using UnityEngine;

namespace LastShore
{
    /// <summary>Jungle fowl: pecks about in a small flock and scatters, half flying, the moment it is startled.</summary>
    public class Fowl : Animal
    {
        Vector3 _target; float _timer, _legPhase, _hop; bool _fleeing;
        Transform[] _legs; Transform _model;

        void Start()
        {
            Health = 10f; _legs = new[] { Deep("LegL"), Deep("LegR") }; _model = transform.Find("Model");
            _target = WanderTarget(8f); _timer = Random.Range(2f, 5f);
        }

        protected override void OnAlert(Vector3 threat) { if (!Dead) Scatter(threat); }
        protected override void OnNoise(Vector3 pos) { if (!Dead) Scatter(pos); }

        void Scatter(Vector3 from)
        {
            Vector3 away = transform.position - from; away.y = 0;
            _target = transform.position + (Quaternion.Euler(0, Random.Range(-50f, 50f), 0) * away.normalized) * Random.Range(16f, 26f);
            _fleeing = true; _timer = 4f; Engaged = true;
        }

        void Update()
        {
            if (Dead || GM == null || GM.Player == null) return;
            float dist = PlayerDist;
            if (dist > 80f) { Speed = 0; return; }
            _timer -= Time.deltaTime;
            Vector3 d = _target - transform.position; d.y = 0;
            if (_fleeing)
            {
                if (!MoveSmart(d, 6.2f, 10f)) _target = transform.position + Quaternion.Euler(0, 90, 0) * d.normalized * 12f;
                _hop += Time.deltaTime * 9f;
                if (_timer <= 0 || d.magnitude < 1.5f) { _fleeing = false; Engaged = false; Home = transform.position; _target = WanderTarget(8f); _timer = 3f; }
            }
            else
            {
                if (Threat && (SeesPlayer(10f, 220f, 4f) || HearsPlayer(15f))) { AudioManager.I.PlayAt("fowl", transform.position, 0.8f, 0.2f); AlertKind(12f); Scatter(PlayerT.position); }
                else if (_timer <= 0 || d.magnitude < 0.5f) { Speed = 0; if (_timer < -Random.Range(1f, 3f)) { _target = WanderTarget(8f); _timer = Random.Range(2f, 5f); } }
                else if (!MoveSmart(d, 1.1f, 5f)) _target = WanderTarget(8f);
            }
            if (_model != null)
            {
                float y = _fleeing ? Mathf.Abs(Mathf.Sin(_hop)) * 0.45f : 0;
                float peck = !_fleeing && Speed < 0.1f ? Mathf.Max(0, Mathf.Sin(Time.time * 5f + Home.x)) * 26f : 0;
                _model.localPosition = new Vector3(0, y, 0); _model.localRotation = Quaternion.Euler(peck, 0, 0);
            }
            SwingLegs(_legs, ref _legPhase, Speed, 9f, 45f);
        }

        protected override void OnHit(ItemDef tool, float damage, Vector3 point)
        {
            Blood(point); AudioManager.I.PlayAt("fowl", transform.position); AlertKind(14f);
            StartCoroutine(DieRoutine(("meat_raw", 1), ("feather", 2)));
        }
    }
}
