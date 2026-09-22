using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health), typeof(Rigidbody2D))]
    public sealed class EnemyHitReaction : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Color hitFlashColor = Color.red;
        [SerializeField, Min(0f)] private float hitFlashDuration = 0.10f;
        [SerializeField, Min(0f)] private float knockbackDistance = 0.30f;
        [SerializeField, Min(0.01f)] private float knockbackDuration = 0.10f;
        [Tooltip("0 grants immunity; values between 0 and 1 reduce displacement.")]
        [SerializeField, Range(0f, 1f)] private float knockbackMultiplier = 1f;

        private Health health;
        private Rigidbody2D body;
        private Color originalColor;
        private bool flashing;
        private float flashEndsAt;
        private Vector2 knockbackVelocity;
        private float remainingKnockback;
        private readonly List<RaycastHit2D> knockbackHits = new List<RaycastHit2D>(16);

        // Future locomotion should yield Rigidbody2D control while this is true.
        public bool IsKnockedBack { get; private set; }

        private void Awake()
        {
            health = GetComponent<Health>();
            body = GetComponent<Rigidbody2D>();
            if (targetRenderer == null) targetRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void OnEnable()
        {
            health.DamageTaken += OnDamageTaken;
            health.Died += OnDeath;
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.DamageTaken -= OnDamageTaken;
                health.Died -= OnDeath;
            }
            RestoreColor();
            StopKnockback();
        }

        private void OnDamageTaken(DamageInfo damage, float appliedDamage)
        {
            if (!isActiveAndEnabled || appliedDamage <= 0f) return;
            if (targetRenderer != null && hitFlashDuration > 0f)
            {
                // Repeated hits extend the flash without capturing the temporary hit color.
                if (!flashing) originalColor = targetRenderer.color;
                flashing = true;
                targetRenderer.color = hitFlashColor;
                flashEndsAt = Time.time + hitFlashDuration;
            }

            StopKnockback();
            if (health.IsDead || !damage.SourcePosition.HasValue || body == null
                || !body.simulated || body.bodyType != RigidbodyType2D.Kinematic)
                return;
            Vector2 direction = body.position - damage.SourcePosition.Value;
            float distance = knockbackDistance * Mathf.Clamp01(knockbackMultiplier);
            if (direction.sqrMagnitude <= 0.0001f || distance <= 0f
                || float.IsNaN(direction.sqrMagnitude) || float.IsInfinity(direction.sqrMagnitude)
                || float.IsNaN(distance) || float.IsInfinity(distance))
                return;
            remainingKnockback = knockbackDuration > 0f && !float.IsInfinity(knockbackDuration)
                ? knockbackDuration : 0.10f;
            knockbackVelocity = direction.normalized * (distance / remainingKnockback);
            IsKnockedBack = true;
        }

        private void Update()
        {
            if (flashing && Time.time >= flashEndsAt) RestoreColor();
        }

        private void FixedUpdate()
        {
            if (!IsKnockedBack) return;
            if (health.IsDead || remainingKnockback <= 0f || !body.simulated
                || body.bodyType != RigidbodyType2D.Kinematic)
            {
                StopKnockback();
                return;
            }
            // A partial final step preserves distance in free space; collisions may shorten it.
            float step = Mathf.Min(Time.fixedDeltaTime, remainingKnockback);
            Vector2 displacement = knockbackVelocity * step;
            float distance = displacement.magnitude;
            Vector2 direction = displacement.normalized;
            var filter = new ContactFilter2D();
            filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
            filter.useTriggers = false;
            // Kinematic bodies need an explicit sweep to stop against static walls.
            float skin = Physics2D.defaultContactOffset;
            int count = body.Cast(direction, filter, knockbackHits, distance + skin);
            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = knockbackHits[i];
                if (Vector2.Dot(direction, hit.normal) >= 0f) continue;
                distance = Mathf.Min(distance, Mathf.Max(0f, hit.distance - skin));
            }
            body.MovePosition(body.position + direction * distance);
            remainingKnockback -= step;
        }

        private void OnDeath(DamageInfo damage) => StopKnockback();

        private void StopKnockback()
        {
            if (IsKnockedBack && body != null)
            {
                // Replace any pending MovePosition when death/disable cancels before simulation.
                if (body.simulated && body.bodyType == RigidbodyType2D.Kinematic)
                    body.MovePosition(body.position);
                body.linearVelocity = Vector2.zero;
            }
            IsKnockedBack = false;
            remainingKnockback = 0f;
            knockbackVelocity = Vector2.zero;
        }

        private void RestoreColor()
        {
            if (flashing && targetRenderer != null) targetRenderer.color = originalColor;
            flashing = false;
        }
    }
}
