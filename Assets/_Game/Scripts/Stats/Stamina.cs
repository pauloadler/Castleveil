using System;
using UnityEngine;

namespace Game.Stats
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterStats))]
    public sealed class Stamina : MonoBehaviour
    {
        [Tooltip("Runtime value, initialized from CharacterStats when the character awakens.")]
        [SerializeField] private float currentStamina;
        private CharacterStats stats;
        private float lastReportedCurrent = -1f;
        private float lastReportedMax = -1f;

        public event Action<float, float> ValuesChanged;

        public float CurrentStamina => currentStamina;
        public float MaxStamina => stats != null ? stats.MaxStamina : 0f;

        private void Awake()
        {
            stats = GetComponent<CharacterStats>();
            currentStamina = MaxStamina;
            NotifyIfChanged();
        }

        private void Update()
        {
            currentStamina = Mathf.Clamp(currentStamina
                + stats.StaminaRegeneration * Time.deltaTime, 0f, MaxStamina);
            NotifyIfChanged();
        }

        public bool TrySpend(float amount)
        {
            if (!isActiveAndEnabled || float.IsNaN(amount) || float.IsInfinity(amount)
                || amount < 0f || currentStamina < amount)
                return false;

            currentStamina -= amount;
            NotifyIfChanged();
            return true;
        }

        private void NotifyIfChanged()
        {
            float maximum = MaxStamina;
            if (currentStamina == lastReportedCurrent && maximum == lastReportedMax)
                return;
            lastReportedCurrent = currentStamina;
            lastReportedMax = maximum;
            ValuesChanged?.Invoke(currentStamina, maximum);
        }
    }
}
