using Game.Combat;
using UnityEngine;

namespace Game.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public sealed class FeralDogVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite[] idleFrames = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] private float idleCycleDuration = 1.0f;
        [SerializeField] private Sprite[] runFrames = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] private float runCycleDuration = 0.55f;
        [SerializeField] private Sprite[] attackFrames = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] private float attackDuration = 0.45f;
        [SerializeField] private Sprite[] hitFrames = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] private float hitDuration = 0.15f;
        [SerializeField] private Sprite[] deathFrames = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] private float deathDuration = 0.40f;
        [Tooltip("Direction the original, unflipped artwork faces.")]
        [SerializeField] private bool spritesFaceRight = true;

        private Health health;
        private bool started;
        private bool hitting;
        private bool dying;
        private bool deathFinished;
        private float animationStartedAt;
        private float animationDuration;
        private float idleStartedAt;
        private bool running;
        private bool attacking;
        private float attackStartedAt;
        private float attackPlaybackDuration;

        private void Awake()
        {
            health = GetComponent<Health>();
            if (targetRenderer == null) targetRenderer = GetComponentInChildren<SpriteRenderer>(true);
            if (targetRenderer == null)
            {
                Debug.LogError("FeralDogVisual needs a SpriteRenderer assigned on its Visual child.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            health.DamageTaken += OnDamageTaken;
            health.Died += OnDeath;
            if (started) Synchronize();
        }

        private void Start()
        {
            // Health initializes in Awake; all Awake calls have completed before Start.
            started = true;
            Synchronize();
        }

        private void OnDisable()
        {
            if (health == null) return;
            health.DamageTaken -= OnDamageTaken;
            health.Died -= OnDeath;
            hitting = false;
            attacking = false;
        }

        private void Synchronize()
        {
            if (health.IsDead) BeginDeath();
            else if (!hitting) RestartLocomotion();
        }

        public void SetFacingRight(bool facingRight)
        {
            if (targetRenderer != null && !dying && !health.IsDead)
                targetRenderer.flipX = facingRight != spritesFaceRight;
        }

        public void PlayAttack()
        {
            if (!isActiveAndEnabled || health == null || health.IsDead || dying || hitting) return;
            if (attackFrames == null || attackFrames.Length == 0) return;
            attacking = true;
            attackStartedAt = Time.time;
            attackPlaybackDuration = SafeDuration(attackDuration, 0.45f);
            ShowFrame(attackFrames, 0f);
        }

        public void SetRunning(bool value)
        {
            if (running == value) return;
            running = value;
            if (isActiveAndEnabled && health != null && !health.IsDead
                && !dying && !hitting && !attacking)
                RestartLocomotion();
        }

        private void OnDamageTaken(DamageInfo damage, float appliedDamage)
        {
            if (!isActiveAndEnabled || dying || health.IsDead || appliedDamage <= 0f) return;
            hitting = true;
            // Hit cancels this visual swing; gameplay remains owned by the AI.
            attacking = false;
            animationStartedAt = Time.time;
            animationDuration = SafeDuration(hitDuration, 0.15f);
            ShowFrame(hitFrames, 0f);
        }

        private void OnDeath(DamageInfo damage)
        {
            if (isActiveAndEnabled) BeginDeath();
        }

        private void BeginDeath()
        {
            if (dying) return;
            dying = true;
            attacking = false;
            hitting = false;
            animationStartedAt = Time.time;
            animationDuration = SafeDuration(deathDuration, 0.40f);
            ShowFrame(deathFrames, 0f);
        }

        private void Update()
        {
            if (dying)
            {
                if (deathFinished) return;
                float progress = (Time.time - animationStartedAt) / animationDuration;
                ShowFrame(deathFrames, progress);
                if (progress >= 1f)
                {
                    // Keep the last valid death frame even when trailing slots are unassigned.
                    if (deathFrames != null)
                        for (int i = deathFrames.Length - 1; i >= 0; i--)
                            if (deathFrames[i] != null) { Show(deathFrames[i]); break; }
                    deathFinished = true;
                }
            }
            else if (hitting)
            {
                float progress = (Time.time - animationStartedAt) / animationDuration;
                if (progress >= 1f)
                {
                    hitting = false;
                    RestartLocomotion();
                }
                else ShowFrame(hitFrames, progress);
            }
            else if (attacking)
            {
                float progress = (Time.time - attackStartedAt) / attackPlaybackDuration;
                if (progress >= 1f)
                {
                    attacking = false;
                    RestartLocomotion();
                }
                else ShowFrame(attackFrames, progress);
            }
            else if (!health.IsDead)
            {
                bool useRun = running && runFrames != null && runFrames.Length > 0;
                float duration = useRun ? SafeDuration(runCycleDuration, 0.55f)
                    : SafeDuration(idleCycleDuration, 1f);
                float progress = Mathf.Repeat(Time.time - idleStartedAt, duration) / duration;
                ShowFrame(useRun ? runFrames : idleFrames, progress);
            }
        }

        private void RestartLocomotion()
        {
            idleStartedAt = Time.time;
            ShowFrame(running && runFrames != null && runFrames.Length > 0 ? runFrames : idleFrames, 0f);
        }

        private void ShowFrame(Sprite[] frames, float progress)
        {
            if (frames == null || frames.Length == 0) { Show(idleSprite); return; }
            int index = Mathf.Min(Mathf.FloorToInt(Mathf.Clamp01(progress) * frames.Length), frames.Length - 1);
            Show(frames[index] != null ? frames[index] : idleSprite);
        }

        private void Show(Sprite sprite)
        {
            if (targetRenderer != null && sprite != null) targetRenderer.sprite = sprite;
        }

        private static float SafeDuration(float value, float fallback)
            => value > 0f && !float.IsInfinity(value) ? value : fallback;
    }
}
