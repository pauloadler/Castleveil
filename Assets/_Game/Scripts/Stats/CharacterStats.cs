using UnityEngine;

namespace Game.Stats
{
    [DisallowMultipleComponent]
    public sealed class CharacterStats : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField, Min(0f)] private float attackDamage = 10f;
        [SerializeField, Min(0f)] private float armor = 0f;
        [SerializeField, Min(0f)] private float moveSpeed = 5f;
        [SerializeField, Range(0f, 1f)] private float criticalChance = 0.05f;
        [Tooltip("Damage multiplier for a critical hit; 1.5 means 150% damage.")]
        [SerializeField, Min(1f)] private float criticalDamage = 1.5f;
        [SerializeField, Min(0f)] private float maxStamina = 100f;
        [SerializeField, Min(0f)] private float staminaRegeneration = 10f;

        public float MaxHealth => Mathf.Max(1f, maxHealth);
        public float AttackDamage => Mathf.Max(0f, attackDamage);
        public float Armor => Mathf.Max(0f, armor);
        public float MoveSpeed => Mathf.Max(0f, moveSpeed);
        public float CriticalChance => Mathf.Clamp01(criticalChance);
        public float CriticalDamage => Mathf.Max(1f, criticalDamage);
        public float MaxStamina => Mathf.Max(0f, maxStamina);
        public float StaminaRegeneration => Mathf.Max(0f, staminaRegeneration);
    }
}
