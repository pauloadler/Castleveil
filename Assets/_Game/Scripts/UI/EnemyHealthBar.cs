using Game.Combat;
using UnityEngine;

namespace Game.UI
{
    [DisallowMultipleComponent]
    public sealed class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private SpriteRenderer background;
        [SerializeField] private SpriteRenderer fill;
        [SerializeField] private Vector3 offset = new Vector3(0f, 1f, 0f);
        [SerializeField, Min(0.01f)] private float width = 0.8f;
        [SerializeField] private bool showOnlyAfterDamage = true;

        private Health subscribedHealth;
        private bool hasTakenDamage;

        private void Awake()
        {
            if (health == null) health = GetComponentInParent<Health>();
            if (health == null || background == null || fill == null || background == fill
                || background.sprite == null || fill.sprite == null
                || background.transform.parent != transform || fill.transform.parent != transform
                || transform.parent != health.transform)
            {
                Debug.LogError("EnemyHealthBar needs a Health on its parent and two distinct direct children with SpriteRenderers and sprites assigned.", this);
                enabled = false;
                return;
            }
            transform.localPosition = offset;
        }

        private void OnEnable()
        {
            subscribedHealth = health;
            subscribedHealth.ValuesChanged += Refresh;
            subscribedHealth.DamageTaken += OnDamageTaken;
            subscribedHealth.Died += OnDeath;
            Synchronize();
        }

        // Synchronize again after every object's Awake has initialized Health.
        private void Start() => Synchronize();

        private void OnDisable()
        {
            if (subscribedHealth != null)
            {
                subscribedHealth.ValuesChanged -= Refresh;
                subscribedHealth.DamageTaken -= OnDamageTaken;
                subscribedHealth.Died -= OnDeath;
                subscribedHealth = null;
            }
            SetVisible(false);
        }

        private void Synchronize()
        {
            if (health == null) return;
            hasTakenDamage |= health.CurrentHealth < health.MaxHealth;
            Refresh(health.CurrentHealth, health.MaxHealth);
        }

        private void OnDamageTaken(DamageInfo damage, float appliedDamage)
        {
            if (appliedDamage <= 0f) return;
            hasTakenDamage = true;
            Refresh(health.CurrentHealth, health.MaxHealth);
        }

        private void OnDeath(DamageInfo damage) => SetVisible(false);

        private void Refresh(float current, float maximum)
        {
            float ratio = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;
            float barWidth = width > 0f && !float.IsInfinity(width) ? width : 0.8f;
            SetWidth(background, barWidth, 0f);
            SetWidth(fill, barWidth * ratio, barWidth * (ratio - 1f) * 0.5f);
            SetVisible(current > 0f && (!showOnlyAfterDamage || hasTakenDamage));
        }

        private static void SetWidth(SpriteRenderer renderer, float visualWidth, float centerX)
        {
            if (renderer == null || renderer.sprite == null) return;
            Bounds bounds = renderer.sprite.bounds;
            if (bounds.size.x <= 0f) return;
            Vector3 scale = renderer.transform.localScale;
            scale.x = visualWidth / bounds.size.x;
            renderer.transform.localScale = scale;
            Vector3 position = renderer.transform.localPosition;
            position.x = centerX - bounds.center.x * scale.x;
            renderer.transform.localPosition = position;
        }

        private void SetVisible(bool visible)
        {
            if (background != null) background.enabled = visible;
            if (fill != null) fill.enabled = visible;
        }
    }
}
