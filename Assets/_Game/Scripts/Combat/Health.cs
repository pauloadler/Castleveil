using System;
using Game.Stats;
using UnityEngine;

namespace Game.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterStats))]
    public sealed class Health : MonoBehaviour, IDamageable
    {
        public float CurrentHealth { get; private set; }
        public float MaxHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0f;

        public event Action<DamageInfo> Died;

        private void Awake()
        {
            // Snapshot the base maximum once; re-enabling the component must not revive it.
            MaxHealth = GetComponent<CharacterStats>().MaxHealth;
            CurrentHealth = MaxHealth;
        }

        public void TakeDamage(DamageInfo damage)
        {
            if (IsDead || damage.Amount <= 0f)
                return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage.Amount);
            if (IsDead)
                Died?.Invoke(damage);
        }
    }
}
