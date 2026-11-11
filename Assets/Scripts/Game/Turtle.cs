using UnityEngine;

namespace LastShore
{
    /// <summary>Sea turtle hauled out on the sand. Harmless; it simply pulls in and waits when you come near.</summary>
    public class Turtle : Animal
    {
        Vector3 _target; float _timer, _phase; Transform[] _flip;

        void Start()
        {
            Health = 999f; _flip = new[] { Deep("FlipFL"), Deep("FlipFR"), Deep("FlipBL"), Deep("FlipBR") };
            _target = WanderTarget(9f); _timer = Random.Range(4f, 10f);
        }

        protected override bool CanStand(Vector3 p) { float h = World.Height(p); return h > World.SeaLevel - 0.3f && h < World.SeaLevel + 2.4f; }

        void Update()
        {
            if (Dead || GM == null || GM.Player == null) return;
            float dist = PlayerDist;
            if (dist > 70f) return;
            _timer -= Time.deltaTime;
            bool hiding = dist < 3.5f;
            Vector3 d = _target - transform.position; d.y = 0;
            if (hiding) Speed = 0;
            else if (_timer <= 0 || d.magnitude < 0.5f) { Speed = 0; if (_timer < -Random.Range(3f, 9f)) { _target = WanderTarget(9f); _timer = Random.Range(5f, 12f); } }
            else if (!MoveSmart(d, 0.45f, 1.5f)) _target = WanderTarget(9f);
            _phase += Speed * Time.deltaTime * 9f;
            for (int i = 0; i < _flip.Length; i++)
                if (_flip[i] != null)
                {
                    _flip[i].localRotation = Quaternion.Euler(0, Mathf.Sin(_phase + (i % 2) * Mathf.PI) * 28f * Mathf.Clamp01(Speed * 3f), 0);
                    _flip[i].localScale = Vector3.Lerp(_flip[i].localScale, hiding ? Vector3.one * 0.35f : Vector3.one, Time.deltaTime * 6f);
                }
        }

        protected override void OnHit(ItemDef tool, float damage, Vector3 point)
        {
            AudioManager.I.PlayAt("mine", point, 0.4f);
            GM.Hud.Hint("The turtle pulls into its shell and waits.");
        }
    }
}
