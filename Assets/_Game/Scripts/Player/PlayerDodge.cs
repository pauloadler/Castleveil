using Game.Combat;
using Game.Stats;
using UnityEngine;

namespace Game.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerMovement), typeof(PlayerInputReader))]
    [RequireComponent(typeof(Stamina), typeof(Health))]
    [DefaultExecutionOrder(90)]
    public sealed class PlayerDodge : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float dodgeDistance = 3f;
        [SerializeField, Min(0.01f)] private float dodgeDuration = 0.25f;
        [SerializeField, Min(0f)] private float staminaCost = 25f;
        [Tooltip("Seconds between dodge starts.")]
        [SerializeField, Min(0f)] private float cooldown = 0.75f;
        [SerializeField, Min(0f)] private float invulnerabilityDuration = 0.15f;

        private Rigidbody2D body;
        private PlayerMovement movement;
        private PlayerInputReader input;
        private Stamina stamina;
        private Health health;
        private bool dodgeRequested;
        private Vector2 direction;
        private float speed;
        private float remainingDuration;
        private float nextDodgeTime;
        private float protectionEndsAt;
        private bool hasProtection;

        public bool IsDodging { get; private set; }
        public float CooldownRemaining => Mathf.Max(0f, nextDodgeTime - Time.time);

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            movement = GetComponent<PlayerMovement>();
            input = GetComponent<PlayerInputReader>();
            stamina = GetComponent<Stamina>();
            health = GetComponent<Health>();
        }

        private void OnEnable() => health.Died += OnDeath;

        private void LateUpdate()
        {
            if (input.DodgePressedThisFrame && !IsDodging && !health.IsDead
                && Time.time >= nextDodgeTime)
                dodgeRequested = true;
        }

        private void FixedUpdate()
        {
            if (health.IsDead || !movement.isActiveAndEnabled || !input.isActiveAndEnabled)
            {
                CancelDodge();
                return;
            }

            if (hasProtection && Time.fixedTime >= protectionEndsAt)
                ReleaseProtection();
            if (IsDodging && remainingDuration <= 0.000001f)
                EndMovement();

            if (dodgeRequested)
            {
                dodgeRequested = false;
                TryBeginDodge();
            }

            if (!IsDodging)
                return;

            // A partial final step preserves configured distance at different physics rates.
            float step = Mathf.Min(Time.fixedDeltaTime, remainingDuration);
            body.linearVelocity = direction * speed * (step / Time.fixedDeltaTime);
            remainingDuration -= step;
        }

        private void TryBeginDodge()
        {
            if (IsDodging || Time.fixedTime < nextDodgeTime
                || !movement.TryAcquireMovementControl(this))
                return;
            if (!stamina.TrySpend(staminaCost))
            {
                movement.ReleaseMovementControl(this);
                return;
            }

            Vector2 move = input.MoveInput;
            direction = (move.sqrMagnitude > 0.0001f ? move : movement.FacingDirection).normalized;
            remainingDuration = Mathf.Max(0.01f, dodgeDuration);
            speed = Mathf.Max(0f, dodgeDistance) / remainingDuration;
            nextDodgeTime = Time.fixedTime + Mathf.Max(0f, cooldown);
            IsDodging = true;

            if (invulnerabilityDuration > 0f)
            {
                protectionEndsAt = Mathf.Max(protectionEndsAt, Time.fixedTime + invulnerabilityDuration);
                hasProtection = true;
                health.SetInvulnerable(this, true);
            }
        }

        private void EndMovement()
        {
            if (!IsDodging)
                return;
            IsDodging = false;
            body.linearVelocity = Vector2.zero;
            movement.ReleaseMovementControl(this);
        }

        private void ReleaseProtection()
        {
            if (health != null)
                health.SetInvulnerable(this, false);
            hasProtection = false;
            protectionEndsAt = 0f;
        }

        private void CancelDodge()
        {
            dodgeRequested = false;
            EndMovement();
            ReleaseProtection();
        }

        private void OnDeath(DamageInfo damage) => CancelDodge();

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
                CancelDodge();
        }

        private void OnDisable()
        {
            if (health != null)
                health.Died -= OnDeath;
            CancelDodge();
        }
    }
}
