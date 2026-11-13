using UnityEngine;

namespace LastShore
{
    /// <summary>Lies coiled and hard to see. Hisses a warning when you come close, strikes if you ignore it, then slips away.</summary>
    public class Snake : Animal
    {
        public override bool Hostile => true;
        enum S { Coiled, Warn, Strike, Retreat }
        S _state; float _timer, _wave; Vector3 _target; bool _hissed;
        Transform[] _segs;

        void Start()
        {
            Health = 12f;
            var list = new System.Collections.Generic.List<Transform>();
            for (int i = 1; i < 10; i++) { var s = Deep("Seg" + i); if (s != null) list.Add(s); }
            _segs = list.ToArray();
        }

        protected override bool CanStand(Vector3 p) => World.IsLand(p, 0.4f) && World.Slope(p) < 35f;

        void Update()
        {
            if (Dead || GM == null || GM.Player == null) return;
            float dt = Time.deltaTime, dist = PlayerDist;
            if (dist > 60f) return;
            _timer -= dt;
            Engaged = _state != S.Coiled;

            switch (_state)
            {
                case S.Coiled:
                    Speed = 0;
                    if (Threat && dist < 6f) { _state = S.Warn; _timer = 3.5f; if (!_hissed) { _hissed = true; AudioManager.I.PlayAt("snake_hiss", transform.position, 0.9f); } }
                    else if (dist > 12f) _hissed = false;
                    break;
                case S.Warn:
                    Speed = 0; Face(PlayerT.position, 10f);
                    if (dist > 8f) { _state = S.Coiled; break; }
                    if (dist < 2.3f && Threat) { _state = S.Strike; _timer = 0.28f; AudioManager.I.PlayAt("snake_hiss", transform.position, 1f, 0.2f); }
                    break;
                case S.Strike:
                    {
                        Vector3 d = PlayerT.position - transform.position; d.y = 0;
                        Step(d, 7f, 20f);
                        if (_timer <= 0)
                        {
                            if (PlayerDist < 1.6f && Threat) { GM.Vitals.Damage(7f, "a snake bite", true, transform.position); GM.Vitals.Poison(50f); GM.SayOnce("snake"); }
                            _target = transform.position - d.normalized * 14f + new Vector3(Random.Range(-4f, 4f), 0, Random.Range(-4f, 4f));
                            _state = S.Retreat; _timer = 6f;
                        }
                        break;
                    }
                case S.Retreat:
                    {
                        Vector3 d = _target - transform.position; d.y = 0;
                        if (_timer <= 0 || d.magnitude < 1f || !MoveSmart(d, 3.4f, 8f)) { Home = transform.position; _state = S.Coiled; _hissed = false; }
                        break;
                    }
            }
        }

        // body follows the head: a tight coil at rest, a travelling wave when moving
        void LateUpdate()
        {
            if (Dead || _segs == null) return;
            _wave += Time.deltaTime * (2f + Speed * 3f);
            bool coiled = _state == S.Coiled || _state == S.Warn;
            for (int i = 0; i < _segs.Length; i++)
            {
                Vector3 p;
                if (coiled)
                {
                    float a = (i + 1) * 0.95f; float r = 0.07f + (i + 1) * 0.026f;
                    p = transform.position + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r) - transform.forward * 0.05f;
                }
                else
                    p = transform.position - transform.forward * ((i + 1) * 0.13f) + transform.right * (Mathf.Sin(_wave * 4f - i * 0.9f) * 0.06f);
                p.y = World.Height(p);
                _segs[i].position = Vector3.Lerp(_segs[i].position, p, Time.deltaTime * 14f);
                Vector3 look = (i == 0 ? transform.position : _segs[i - 1].position) - _segs[i].position;
                if (look.sqrMagnitude > 0.0001f) _segs[i].rotation = Quaternion.LookRotation(-look);
            }
        }

        protected override void OnHit(ItemDef tool, float damage, Vector3 point)
        {
            Health -= damage; Blood(point);
            if (Health <= 0) { foreach (var s in _segs) if (s != null) s.gameObject.SetActive(false); StartCoroutine(DieRoutine(("meat_raw", 1))); }
            else { _target = transform.position + (transform.position - PlayerT.position).normalized * 12f; _state = S.Retreat; _timer = 5f; }
        }
    }
}
