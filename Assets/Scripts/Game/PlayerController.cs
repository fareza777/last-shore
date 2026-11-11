using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LastShore
{
    /// <summary>Unified keyboard/mouse + touch input. Touch controls write the Touch* fields; Poll() merges both once per frame.</summary>
    public static class GameInput
    {
        public static Vector2 Move, Look;
        public static bool Sprint, Jump, UseDown, UseHeld, Interact, Inventory, Pause, Rotate;
        public static int HotbarKey = -1, HotbarScroll;

        public static Vector2 TouchMove, TouchLook;
        public static bool TouchSprint, TouchJump, TouchUse, TouchUseHeld, TouchInteract, TouchInventory, TouchPause, TouchRotate;
        public static int TouchHotbar = -1;
        public static bool ForceTouch;
        public static bool TouchMode => Application.isMobilePlatform || ForceTouch;

        public static void Poll()
        {
            Vector2 kb = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            Move = Vector2.ClampMagnitude(kb + TouchMove, 1f);
            Vector2 mouse = TouchMode ? Vector2.zero : new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 2.2f;
            Look = mouse + TouchLook * 0.13f; TouchLook = Vector2.zero;
            Sprint = Input.GetKey(KeyCode.LeftShift) || TouchSprint;
            Jump = Input.GetKeyDown(KeyCode.Space) || TouchJump; TouchJump = false;
            UseDown = (!TouchMode && Input.GetMouseButtonDown(0)) || TouchUse; TouchUse = false;
            UseHeld = (!TouchMode && Input.GetMouseButton(0)) || TouchUseHeld;
            Interact = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F) || TouchInteract; TouchInteract = false;
            Inventory = Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I) || TouchInventory; TouchInventory = false;
            Pause = Input.GetKeyDown(KeyCode.Escape) || TouchPause; TouchPause = false;
            Rotate = Input.GetKeyDown(KeyCode.R) || TouchRotate; TouchRotate = false;
            HotbarKey = TouchHotbar; TouchHotbar = -1;
            for (int i = 0; i < Inventory_HotbarSize; i++) if (Input.GetKeyDown(KeyCode.Alpha1 + i)) HotbarKey = i;
            float sc = TouchMode ? 0 : Input.mouseScrollDelta.y;
            HotbarScroll = sc > 0.01f ? -1 : sc < -0.01f ? 1 : 0;
        }
        const int Inventory_HotbarSize = 6;
    }

    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public Camera Cam { get; private set; }
        public Transform Hand { get; private set; }
        public bool Swimming { get; private set; }
        public bool Sprinting { get; private set; }
        public float Yaw => _yaw;
        public Vector3 Velocity => _cc != null ? _cc.velocity : Vector3.zero;

        const float EyeHeight = 1.62f, WalkSpeed = 4.3f, SprintSpeed = 6.8f, SwimSpeed = 2.4f, JumpSpeed = 5.4f, Gravity = -19f;
        CharacterController _cc; PlayerVitals _vitals;
        float _yaw, _pitch, _vy, _bob, _stepDist, _shake, _fovKick;
        Vector3 _camBase, _handBase; Vector2 _sway;
        bool _wasInWater;

        public static PlayerController Create(Vector3 pos, float yaw)
        {
            var go = new GameObject("Player") { tag = "Player" };
            go.transform.position = pos;
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.35f; cc.center = new Vector3(0, 0.9f, 0); cc.slopeLimit = 48; cc.stepOffset = 0.4f;
            var pc = go.AddComponent<PlayerController>();
            pc._yaw = yaw; go.transform.rotation = Quaternion.Euler(0, yaw, 0);

            var camGo = new GameObject("PlayerCamera") { tag = "MainCamera" };
            camGo.transform.SetParent(go.transform, false);
            camGo.transform.localPosition = new Vector3(0, EyeHeight, 0);
            pc.Cam = camGo.AddComponent<Camera>();
            pc.Cam.nearClipPlane = 0.08f; pc.Cam.farClipPlane = 1800f; pc.Cam.fieldOfView = GameSettings.Fov;
            camGo.AddComponent<AudioListener>();
            var data = pc.Cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true; data.antialiasing = Application.isMobilePlatform ? AntialiasingMode.FastApproximateAntialiasing : AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            // cull small props early, keep big silhouettes far

            var hand = new GameObject("Hand");
            hand.transform.SetParent(camGo.transform, false);
            hand.transform.localPosition = pc._handBase = new Vector3(0.42f, -0.46f, 0.72f);
            pc.Hand = hand.transform;
            pc._camBase = camGo.transform.localPosition;

            go.AddComponent<PlayerVitals>();
            go.AddComponent<PlayerInteraction>();
            return pc;
        }

        void Awake() { _cc = GetComponent<CharacterController>(); }
        void Start() { _vitals = GetComponent<PlayerVitals>(); }

        public void Teleport(Vector3 pos, float yaw)
        {
            _cc.enabled = false; transform.position = pos; _yaw = yaw; _pitch = 0;
            transform.rotation = Quaternion.Euler(0, yaw, 0); _cc.enabled = true;
        }

        public void Shake(float amount) { _shake = Mathf.Max(_shake, amount); }

        void Update()
        {
            var gm = GameManager.I;
            bool locked = gm.InputLocked;
            float dt = Time.deltaTime;

            if (!locked)
            {
                float sens = GameSettings.Sensitivity;
                _yaw += GameInput.Look.x * sens;
                _pitch = Mathf.Clamp(_pitch - GameInput.Look.y * sens * (GameSettings.InvertY ? -1 : 1), -86f, 86f);
                _sway = Vector2.Lerp(_sway, GameInput.Look, dt * 10f);
            }
            else _sway = Vector2.Lerp(_sway, Vector2.zero, dt * 10f);
            if (gm.Dead || _lying) return;
            transform.rotation = Quaternion.Euler(0, _yaw, 0);

            // ---- water
            float feet = transform.position.y;
            float depth = World.SeaLevel - feet;
            Swimming = depth > 1.25f;
            bool inWater = depth > 0.35f;
            if (inWater && !_wasInWater && _vy < -2f) AudioManager.I.Play("splash", 0.7f);
            _wasInWater = inWater;

            // ---- movement
            Vector2 m = locked ? Vector2.zero : GameInput.Move;
            Vector3 wish = transform.right * m.x + transform.forward * m.y;
            Sprinting = !locked && GameInput.Sprint && m.y > 0.2f && _vitals.Stamina > 1f && !Swimming && !_vitals.Exhausted;
            float speed = Swimming ? SwimSpeed : Sprinting ? SprintSpeed : WalkSpeed;
            if (inWater && !Swimming) speed *= Mathf.Lerp(1f, 0.55f, Mathf.InverseLerp(0.35f, 1.25f, depth));
            if (_vitals.Hunger <= 0 || _vitals.Thirst <= 0) speed *= 0.8f;

            if (Swimming)
            {
                float target = World.SeaLevel - 1.38f;                   // float with the head above water
                _vy = Mathf.Lerp(_vy, (target - feet) * 3f, dt * 4f);
            }
            else if (_cc.isGrounded)
            {
                _vy = -2f;
                if (!locked && GameInput.Jump && _vitals.Stamina > 6f) { _vy = JumpSpeed; _vitals.UseStamina(6f); }
            }
            else _vy += Gravity * dt;

            Vector3 vel = wish * speed + Vector3.up * _vy;
            _cc.Move(vel * dt);

            // ---- feel: bob, footsteps, fov
            float planar = new Vector2(_cc.velocity.x, _cc.velocity.z).magnitude;
            bool walking = planar > 0.5f && (_cc.isGrounded || Swimming);
            if (walking)
            {
                _bob += dt * (Sprinting ? 12.5f : 9f);
                _stepDist += planar * dt;
                if (_stepDist > (Sprinting ? 2.5f : 2.0f) && !Swimming)
                {
                    _stepDist = 0;
                    AudioManager.I.Play(inWater ? "splash" : feet < World.SeaLevel + 2.3f ? "step_sand" : "step_grass", inWater ? 0.25f : 0.5f, 0.12f);
                }
            }
            float bobAmt = GameSettings.HeadBob && walking ? (Sprinting ? 0.055f : 0.035f) : 0f;
            Vector3 bob = new Vector3(Mathf.Cos(_bob * 0.5f) * bobAmt * 0.6f, Mathf.Sin(_bob) * bobAmt, 0);
            if (Swimming) bob.y += Mathf.Sin(Time.time * 1.6f) * 0.05f;
            _shake = Mathf.MoveTowards(_shake, 0, dt * 2.2f);
            Vector3 shake = Random.insideUnitSphere * _shake * 0.06f;
            Cam.transform.localPosition = Vector3.Lerp(Cam.transform.localPosition, _camBase + bob, dt * 14f) + shake;
            Cam.transform.localRotation = Quaternion.Euler(_pitch + shake.y * 40f, 0, shake.x * 60f);
            _fovKick = Mathf.Lerp(_fovKick, Sprinting ? 7f : 0f, dt * 6f);
            Cam.fieldOfView = GameSettings.Fov + _fovKick;

            Hand.localPosition = Vector3.Lerp(Hand.localPosition, _handBase + new Vector3(-_sway.x * 0.004f, -_sway.y * 0.004f + bob.y * 0.6f, 0) + new Vector3(bob.x, 0, 0), dt * 12f);
        }

        // ---------------------------------------------------------------- scripted moments
        bool _lying;

        /// <summary>Opening shot: lying on the sand, eyes open, slowly getting up.</summary>
        public IEnumerator WakeUp()
        {
            _lying = true;
            Cam.transform.localPosition = new Vector3(0, 0.28f, 0);
            Cam.transform.localRotation = Quaternion.Euler(-62f, 0, 24f);
            Hand.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.8f);
            SceneFader.FadeIn(3.2f);
            float t = 0;
            while (t < 2.6f) { t += Time.deltaTime; Cam.transform.localRotation = Quaternion.Euler(-62f + Mathf.Sin(t * 0.9f) * 3f, Mathf.Sin(t * 0.6f) * 5f, 24f); yield return null; }
            Vector3 p0 = Cam.transform.localPosition; Quaternion r0 = Cam.transform.localRotation;
            t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime / 3.0f;
                float e = Tween.Evaluate(Ease.InOutCubic, t);
                Cam.transform.localPosition = Vector3.Lerp(p0, _camBase, e) + Vector3.up * Mathf.Sin(e * Mathf.PI) * -0.12f;
                Cam.transform.localRotation = Quaternion.Slerp(r0, Quaternion.identity, e);
                yield return null;
            }
            _pitch = 0; Hand.gameObject.SetActive(true); _lying = false;
        }

        public void FallOver()
        {
            _lying = true;
            Hand.gameObject.SetActive(false);
            Vector3 p0 = Cam.transform.localPosition; Quaternion r0 = Cam.transform.localRotation;
            Tween.To(this, 0, 1, 1.4f, v =>
            {
                Cam.transform.localPosition = Vector3.Lerp(p0, new Vector3(0.2f, 0.3f, 0), v);
                Cam.transform.localRotation = Quaternion.Slerp(r0, Quaternion.Euler(-30f, 0, 70f), v);
            }, Ease.InQuad, 0, null, false);
        }
    }
}
