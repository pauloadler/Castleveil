using Game.Stats;
using UnityEngine;

namespace Game.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterStats), typeof(Health))]
    public sealed class TargetDummy : MonoBehaviour
    {
        private Health health;

        private void Awake() => health = GetComponent<Health>();

        private void OnEnable()
        {
            health.DamageTaken += OnDamageTaken;
            health.Died += OnDeath;
        }

        private void OnDisable()
        {
            health.DamageTaken -= OnDamageTaken;
            health.Died -= OnDeath;
        }

        private void OnDamageTaken(DamageInfo damage, float appliedDamage)
        {
            string source = damage.Source != null ? damage.Source.name : "unknown source";
            Debug.Log($"{name} received {appliedDamage} damage from {source}. HP: {health.CurrentHealth}/{health.MaxHealth}", this);
        }

        private void OnDeath(DamageInfo damage) => Debug.Log($"{name} died.", this);
    }
}
