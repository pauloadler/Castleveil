using System;
using System.Collections.Generic;
using Game.Stats;
using UnityEngine;

namespace Game.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterStats))]
    public sealed class Health : MonoBehaviour, IDamageable
    {
        private readonly HashSet<object> invulnerabilitySources = new HashSet<object>();

        public float CurrentHealth { get; private set; }
        public float MaxHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0f;
        public bool IsInvulnerable => invulnerabilitySources.Count > 0;

        public event Action<DamageInfo> Died;
        public event Action<DamageInfo, float> DamageTaken;
        public event Action<float, float> ValuesChanged;

        private void Awake()
        {
            // Snapshot the base maximum once; re-enabling the component must not revive it.
            MaxHealth = GetComponent<CharacterStats>().MaxHealth;
            CurrentHealth = MaxHealth;
            ValuesChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        // Each external system releases only its own protection, leaving other sources intact.
        public void SetInvulnerable(object source, bool active)
        {
            if (source == null)
                return;
            if (active)
                invulnerabilitySources.Add(source);
            else
                invulnerabilitySources.Remove(source);
        }

        public void TakeDamage(DamageInfo damage)
        {
            if (IsDead || IsInvulnerable || damage.Amount <= 0f)
                return;

            float previousHealth = CurrentHealth;
            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage.Amount);
            // Capture this transition before notifying listeners that may cause further damage.
            bool died = IsDead;
            float appliedDamage = previousHealth - CurrentHealth;
            ValuesChanged?.Invoke(CurrentHealth, MaxHealth);
            DamageTaken?.Invoke(damage, appliedDamage);
            if (died)
                Died?.Invoke(damage);
        }
    }
}
