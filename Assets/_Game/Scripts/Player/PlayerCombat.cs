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
        [SerializeField, Min(0f)] private float attackCooldown = 0.6f;
        [SerializeField, Min(0.01f)] private float attackDuration = 0.6f;
        [Tooltip("Hitbox opens after this delay. The full attack ending always closes it.")]
        [SerializeField, Min(0f)] private float hitboxDelay = 0.25f;
        [SerializeField, Min(0.01f)] private float activeDuration = 0.15f;
        [SerializeField, Min(0f)] private float attackReach = 0.8f;

        private PlayerInputReader input;
        private PlayerMovement movement;
        private CharacterStats stats;
        private Health health;
        private PlayerDodge dodge;
        private PlayerEquipment equipment;
        private bool attackRequested;
        private Vector2 attackDirection;
        private float nextAttackTime;

        public bool IsAttacking { get; private set; }
        public Direction8 AttackFacing { get; private set; } = Direction8.South;
        private float attackStartedAt;
        private float currentAttackDuration;
        private float currentHitboxDelay;
        private bool hitboxOpened;
        public float AttackProgress => currentAttackDuration > 0f
            ? Mathf.Clamp01((Time.time - attackStartedAt) / currentAttackDuration) : 0f;

        private void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            movement = GetComponent<PlayerMovement>();
            stats = GetComponent<CharacterStats>();
            health = GetComponent<Health>();
            dodge = GetComponent<PlayerDodge>();
            equipment = GetComponent<PlayerEquipment>();
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
            if (equipment != null && equipment.HasWeapon && input.BasicAttackPressedThisFrame && !health.IsDead
                && (dodge == null || !dodge.IsDodging)
                && !IsAttacking && !attackHitbox.IsActive && Time.time >= nextAttackTime)
            {
                attackRequested = true;
                attackDirection = movement.FacingDirection;
            }
        }

        private void FixedUpdate()
        {
            if (equipment == null || !equipment.HasWeapon || health.IsDead || (dodge != null && dodge.IsDodging))
            {
                CancelAttack();
                return;
            }
            if (IsAttacking)
            {
                AdvanceAttack();
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
            AttackFacing = DirectionResolver.Resolve(attackDirection);
            attackStartedAt = Time.fixedTime;
            currentAttackDuration = Mathf.Max(0.01f, attackDuration);
            currentHitboxDelay = Mathf.Max(0f, hitboxDelay);
            hitboxOpened = false;
            IsAttacking = true;
            nextAttackTime = attackStartedAt + Mathf.Max(0f, attackCooldown);
            AdvanceAttack();
        }

        private void AdvanceAttack()
        {
            float elapsed = Time.fixedTime - attackStartedAt;
            // Ending wins over opening: an oversized delay cannot create a late hitbox.
            if (elapsed >= currentAttackDuration)
            {
                EndAttack();
                return;
            }
            if (!hitboxOpened && elapsed >= currentHitboxDelay)
            {
                hitboxOpened = true;
                attackHitbox.BeginAttack(new DamageInfo(stats.AttackDamage, gameObject), activeDuration);
            }
        }

        private void EndAttack()
        {
            IsAttacking = false;
            attackRequested = false;
            hitboxOpened = false;
            currentHitboxDelay = 0f;
            if (attackHitbox != null)
                attackHitbox.EndAttack();
        }

        private void OnDeath(DamageInfo damage) => CancelAttack();

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
                CancelAttack();
        }

        private void CancelAttack()
        {
            EndAttack();
            attackStartedAt = 0f;
            currentAttackDuration = 0f;
        }
    }
}
