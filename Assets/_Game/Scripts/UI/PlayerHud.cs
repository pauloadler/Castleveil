using Game.Combat;
using Game.Stats;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(200)]
    public sealed class PlayerHud : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private Stamina stamina;
        [SerializeField] private RectTransform healthFill;
        [SerializeField] private RectTransform staminaFill;
        [SerializeField] private Text healthLabel;
        [SerializeField] private Text staminaLabel;

        private void OnEnable()
        {
            if (health != null)
                health.ValuesChanged += RefreshHealth;
            if (stamina != null)
                stamina.ValuesChanged += RefreshStamina;
            Refresh();
        }

        private void Start()
        {
            if (health == null || stamina == null || healthFill == null || staminaFill == null
                || healthLabel == null || staminaLabel == null)
                Debug.LogError("PlayerHud references are incomplete. Run Tools > ARPG > Setup Milestone 4.25.", this);
            Refresh();
        }

        private void OnDisable()
        {
            if (health != null)
                health.ValuesChanged -= RefreshHealth;
            if (stamina != null)
                stamina.ValuesChanged -= RefreshStamina;
        }

        private void Refresh()
        {
            RefreshHealth(health != null ? health.CurrentHealth : 0f, health != null ? health.MaxHealth : 0f);
            RefreshStamina(stamina != null ? stamina.CurrentStamina : 0f, stamina != null ? stamina.MaxStamina : 0f);
        }

        private void RefreshHealth(float current, float maximum) => SetBar(healthFill, healthLabel, "HP", current, maximum);
        private void RefreshStamina(float current, float maximum) => SetBar(staminaFill, staminaLabel, "Stamina", current, maximum);

        private static void SetBar(RectTransform fill, Text label, string title, float current, float maximum)
        {
            if (fill != null)
                fill.anchorMax = new Vector2(maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f, 1f);
            if (label != null)
                label.text = $"{title}  {current:0.#} / {maximum:0.#}";
        }
    }
}
