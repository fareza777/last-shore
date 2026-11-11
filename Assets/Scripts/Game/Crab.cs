using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastShore
{
    public class Crab : Animal
    {
        Vector3 _target; float _timer;

        void Start() { Health = 8f; Pick(); }
        protected override bool CanStand(Vector3 p) { float h = World.Height(p); return h > World.SeaLevel - 0.1f && h < World.SeaLevel + 2.6f; }
        void Pick() { Vector2 r = Random.insideUnitCircle * 6f; _target = Home + new Vector3(r.x, 0, r.y); _timer = Random.Range(2f, 6f); }

        void Update()
        {
            if (Dead || GameManager.I == null || GameManager.I.Player == null) return;
            float dist = PlayerDist;
            if (dist > 60f) return;
            _timer -= Time.deltaTime;
            Vector3 d;
            if (dist < 4.5f) { d = transform.position - PlayerT.position; if (!Move(d, 3.4f)) Move(Quaternion.Euler(0, 80, 0) * d, 3.4f); }
            else
            {
                d = _target - transform.position; d.y = 0;
                if (_timer <= 0 || d.magnitude < 0.4f) { if (_timer < -2f || d.magnitude >= 0.4f) Pick(); Speed = 0; }
                else if (!Move(d, 0.9f)) Pick();
            }
            var m = transform.Find("Model");
            if (m != null) m.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(Time.time * 14f)) * 0.025f * Mathf.Clamp01(Speed), 0);
        }

        // crabs walk sideways
        bool Move(Vector3 dir, float speed)
        {
            dir.y = 0; if (dir.sqrMagnitude < 0.001f) return true; dir.Normalize();
            Vector3 next = transform.position + dir * speed * Time.deltaTime;
            if (!CanStand(next + dir * 0.4f)) { Speed = 0; return false; }
            next.y = World.Height(next); transform.position = next;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(Vector3.Cross(Vector3.up, dir)), Time.deltaTime * 5f);
            Speed = speed; return true;
        }

        protected override void OnHit(ItemDef tool, float damage, Vector3 point)
        {
            AudioManager.I.PlayAt("hit", point, 0.7f);
            FX.Burst(point, new Color(0.85f, 0.35f, 0.2f), 6, 0.06f, 2f);
            StartCoroutine(DieRoutine(("meat_raw", 1)));
        }
    }
}
