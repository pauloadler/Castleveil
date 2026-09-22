using UnityEngine;

namespace Game.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public sealed class ImpactVfxSpawner : MonoBehaviour
    {
        [SerializeField] private ImpactVfx impactPrefab;
        [Tooltip("Prefer the target's Hurtbox collider; used only to approximate the impact position.")]
        [SerializeField] private Collider2D impactCollider;
        [SerializeField] private bool orientToAttack = true;
        [SerializeField] private float rotationOffset;

        private Health health;

        private void Awake()
        {
            health = GetComponent<Health>();
            if (impactCollider == null) impactCollider = GetComponent<Collider2D>();
        }

        private void OnEnable() => health.DamageTaken += OnDamageTaken;

        private void OnDisable()
        {
            if (health != null) health.DamageTaken -= OnDamageTaken;
        }

        private void OnDamageTaken(DamageInfo damage, float appliedDamage)
        {
            if (!isActiveAndEnabled || !(appliedDamage > 0f) || impactPrefab == null) return;

            bool hasCollider = impactCollider != null && impactCollider.enabled
                && impactCollider.gameObject.activeInHierarchy;
            Vector3 position = hasCollider ? impactCollider.bounds.center : transform.position;
            Vector2 direction = Vector2.zero;
            if (damage.SourcePosition.HasValue)
            {
                Vector2 source = damage.SourcePosition.Value;
                if (!float.IsNaN(source.x) && !float.IsNaN(source.y)
                    && !float.IsInfinity(source.x) && !float.IsInfinity(source.y))
                {
                    direction = (Vector2)position - source;
                    if (hasCollider)
                    {
                        Vector2 closest = impactCollider.ClosestPoint(source);
                        position.x = closest.x;
                        position.y = closest.y;
                    }
                }
            }

            Quaternion rotation = impactPrefab.transform.rotation;
            if (orientToAttack && direction.sqrMagnitude > 0.0001f)
                rotation = Quaternion.Euler(0f, 0f,
                    Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + rotationOffset);
            // Spawn independently so knockback/death of the target cannot move or remove the effect.
            ImpactVfx instance = Instantiate(impactPrefab, position, rotation);
            if (!instance.gameObject.activeSelf) instance.gameObject.SetActive(true);
        }
    }
}
