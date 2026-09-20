using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class Hitbox : MonoBehaviour
    {
        private readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();
        private BoxCollider2D shape;
        private DamageInfo damage;
        private float endTime;

        public bool IsActive { get; private set; }

        private void Awake()
        {
            shape = GetComponent<BoxCollider2D>();
            shape.isTrigger = true;
            shape.enabled = false;
        }

        public void BeginAttack(DamageInfo attackDamage, float duration)
        {
            if (!isActiveAndEnabled || IsActive || attackDamage.Source == null)
                return;

            hitTargets.Clear();
            damage = attackDamage;
            // Give even a very short attack at least one physics simulation step.
            endTime = Time.fixedTime + Mathf.Max(Time.fixedDeltaTime, duration);
            IsActive = true;
            shape.enabled = true;
        }

        public void EndAttack()
        {
            IsActive = false;
            if (shape != null)
                shape.enabled = false;
            hitTargets.Clear();
        }

        private void FixedUpdate()
        {
            if (IsActive && Time.fixedTime >= endTime)
                EndAttack();
        }

        private void OnTriggerEnter2D(Collider2D other) => TryHit(other);
        private void OnTriggerStay2D(Collider2D other) => TryHit(other);
        private void OnDisable() => EndAttack();

        private void TryHit(Collider2D other)
        {
            if (!IsActive || damage.Source == null)
                return;

            Hurtbox hurtbox = other.GetComponent<Hurtbox>();
            if (hurtbox == null || !hurtbox.CanReceiveDamage
                || hurtbox.Owner == damage.Source
                || hurtbox.transform.IsChildOf(damage.Source.transform))
                return;

            IDamageable receiver = hurtbox.Receiver;
            if (hitTargets.Add(receiver))
                receiver.TakeDamage(damage);
        }
    }
}
