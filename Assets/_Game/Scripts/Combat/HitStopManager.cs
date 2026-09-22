using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat
{
    [DisallowMultipleComponent]
    public sealed class HitStopManager : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float defaultDuration = 0.04f;
        [Tooltip("Health components whose accepted damage requests hit stop. Configure before enabling.")]
        [SerializeField] private Health[] damageSources = System.Array.Empty<Health>();

        private static HitStopManager activeManager;
        private readonly HashSet<Health> subscribedHealth = new HashSet<Health>();
        private bool controllingTimeScale;
        private float previousTimeScale;
        private double endsAt;

        private void OnEnable()
        {
            if (activeManager != null && activeManager != this)
            {
                Debug.LogError("Only one active HitStopManager is allowed. Assign all Health references to that manager.", this);
                enabled = false;
                return;
            }
            activeManager = this;
            if (damageSources == null) return;
            foreach (Health health in damageSources)
                if (health != null && subscribedHealth.Add(health))
                    health.DamageTaken += OnDamageTaken;
        }

        private void OnDamageTaken(DamageInfo damage, float appliedDamage)
        {
            if (appliedDamage > 0f) RequestHitStop();
        }

        public void RequestHitStop() => RequestHitStop(defaultDuration);

        public void RequestHitStop(float duration)
        {
            if (!isActiveAndEnabled || activeManager != this
                || duration <= 0f || float.IsNaN(duration) || float.IsInfinity(duration))
                return;

            double requestedEnd = Time.unscaledTimeAsDouble + duration;
            if (!controllingTimeScale)
            {
                previousTimeScale = Time.timeScale;
                controllingTimeScale = true;
                endsAt = requestedEnd;
                Time.timeScale = 0f;
            }
            else
            {
                // Never capture our own zero as the value to restore or shorten an active stop.
                endsAt = System.Math.Max(endsAt, requestedEnd);
            }
        }

        private void Update()
        {
            if (controllingTimeScale && Time.unscaledTimeAsDouble >= endsAt)
                RestoreTimeScale();
        }

        private void RestoreTimeScale()
        {
            if (!controllingTimeScale) return;
            Time.timeScale = previousTimeScale;
            controllingTimeScale = false;
            endsAt = 0d;
        }

        private void OnDisable() => Release();
        private void OnDestroy() => Release();
        private void OnApplicationQuit() => RestoreTimeScale();

        private void Release()
        {
            foreach (Health health in subscribedHealth)
                if (health != null) health.DamageTaken -= OnDamageTaken;
            subscribedHealth.Clear();
            RestoreTimeScale();
            if (activeManager == this) activeManager = null;
        }
    }
}
