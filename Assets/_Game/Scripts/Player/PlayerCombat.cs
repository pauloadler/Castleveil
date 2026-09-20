using Game.Combat;
using Game.Stats;
using UnityEngine;

namespace Game.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputReader), typeof(PlayerMovement))]
    [RequireComponent(typeof(CharacterStats), typeof(Health))]
    [DefaultExecutionOrder(120)]
    public sealed class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private Hitbox attackHitbox;
        [SerializeField, Min(0f)] private float attackCooldown = 0.5f;
        [SerializeField, Min(0.01f)] private float activeDuration = 0.15f;
        [SerializeField, Min(0f)] private float attackReach = 0.8f;

        private PlayerInputReader input;
        private PlayerMovement movement;
        private CharacterStats stats;
        private Health health;
        private PlayerDodge dodge;
        private bool attackRequested;
        private Vector2 attackDirection;
        private float nextAttackTime;

        private void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            movement = GetComponent<PlayerMovement>();
            stats = GetComponent<CharacterStats>();
            health = GetComponent<Health>();
            dodge = GetComponent<PlayerDodge>();
            if (attackHitbox == null)
            {
                Debug.LogError("Assign the player's Attack Hitbox to PlayerCombat.", this);
                enabled = false;
            }
        }

        private void OnEnable() => health.Died += OnDeath;

        private void OnDisable()
        {
            if (health != null)
                health.Died -= OnDeath;
            CancelAttack();
        }

        private void LateUpdate()
        {
            // Movement has already resolved this frame's mouse-facing direction.
            if (input.BasicAttackPressedThisFrame && !health.IsDead
                && (dodge == null || !dodge.IsDodging)
                && !attackHitbox.IsActive && Time.time >= nextAttackTime)
            {
                attackRequested = true;
                attackDirection = movement.FacingDirection;
            }
        }

        private void FixedUpdate()
        {
            if (dodge != null && dodge.IsDodging)
            {
                CancelAttack();
                return;
            }
            if (!attackRequested)
                return;
            attackRequested = false;
            if (health.IsDead || !input.isActiveAndEnabled || attackHitbox.IsActive
                || Time.time < nextAttackTime)
                return;

            // Position the child collider before simulation; facing is locked for this swing.
            attackHitbox.transform.position = transform.position + (Vector3)(attackDirection * attackReach);
            attackHitbox.transform.rotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg);
            attackHitbox.BeginAttack(new DamageInfo(stats.AttackDamage, gameObject), activeDuration);
            nextAttackTime = Time.time + Mathf.Max(0f, attackCooldown);
        }

        private void OnDeath(DamageInfo damage) => CancelAttack();

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
                CancelAttack();
        }

        private void CancelAttack()
        {
            attackRequested = false;
            if (attackHitbox != null)
                attackHitbox.EndAttack();
        }
    }
}
