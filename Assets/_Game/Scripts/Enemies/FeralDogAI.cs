using System.Collections.Generic;
using Game.Combat;
using UnityEngine;

namespace Game.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health), typeof(Rigidbody2D), typeof(EnemyHitReaction))]
    [DefaultExecutionOrder(-10)]
    public sealed class FeralDogAI : MonoBehaviour
    {
        public enum State { Idle, Alert, Chase, Attack, Recover, Dead }

        [SerializeField] private Health player;
        [SerializeField] private GameObject alertIndicator;
        [SerializeField, Min(0f)] private float detectionRadius = 4f;
        [SerializeField, Min(0f)] private float loseAggroRadius = 6f;
        [SerializeField, Min(0f)] private float alertDuration = 0.35f;
        [SerializeField, Min(0f)] private float moveSpeed = 2.5f;
        [SerializeField, Min(0f)] private float attackRange = 0.9f;
        [SerializeField, Min(0f)] private float attackDamage = 10f;
        [SerializeField, Min(0f)] private float attackWindup = 0.20f;
        [SerializeField, Min(0f)] private float recoverDuration = 0.70f;

        private Health health;
        private Rigidbody2D body;
        private EnemyHitReaction hitReaction;
        private FeralDogVisual visual;
        private float stateRemaining;
        private readonly List<RaycastHit2D> movementHits = new List<RaycastHit2D>(16);
        public State CurrentState { get; private set; }
        private float LoseRadius => Mathf.Max(detectionRadius + 0.01f, loseAggroRadius);

        private void Awake()
        {
            health = GetComponent<Health>();
            body = GetComponent<Rigidbody2D>();
            hitReaction = GetComponent<EnemyHitReaction>();
            visual = GetComponent<FeralDogVisual>();
            if (alertIndicator != null && (alertIndicator == gameObject
                || !alertIndicator.transform.IsChildOf(transform)))
            {
                Debug.LogError("FeralDogAI Alert Indicator must be a dedicated child of the dog.", this);
                alertIndicator = null;
            }
            if (body.bodyType != RigidbodyType2D.Kinematic)
            {
                Debug.LogError("FeralDogAI requires a Kinematic Rigidbody2D.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            health.Died += OnDeath;
            SetState(CurrentState == State.Dead ? State.Dead : State.Idle);
        }

        private void Start()
        {
            if (visual != null) visual.SetFacingRight(false);
            if (player == null)
            {
                GameObject candidate = GameObject.FindGameObjectWithTag("Player");
                if (candidate != null) player = candidate.GetComponent<Health>();
            }
            if (player == health) player = null;
            if (player == null)
                Debug.LogWarning("Assign Player Health to FeralDogAI, or tag the Player root 'Player'. No repeated scene searches are performed.", this);
        }

        private void OnDisable()
        {
            if (health != null) health.Died -= OnDeath;
            SetIndicator(false);
            StopMovement();
            stateRemaining = 0f;
        }

        private bool HasTarget => player != null && player != health
            && player.isActiveAndEnabled && !player.IsDead;

        private void FixedUpdate()
        {
            if (health.IsDead || CurrentState == State.Dead)
            {
                SetState(State.Dead);
                return;
            }
            if (!HasTarget)
            {
                SetState(State.Idle);
                return;
            }
            // Run before EnemyHitReaction: never overwrite even its final movement step.
            if (hitReaction.IsKnockedBack) return;
            if (!body.simulated || body.bodyType != RigidbodyType2D.Kinematic) return;

            Vector2 toPlayer = (Vector2)player.transform.position - body.position;
            float distance = toPlayer.magnitude;
            switch (CurrentState)
            {
                case State.Idle:
                    StopMovement();
                    if (distance <= detectionRadius) SetState(State.Alert);
                    break;
                case State.Alert:
                    StopMovement();
                    stateRemaining -= Time.fixedDeltaTime;
                    if (stateRemaining <= 0f) SetState(State.Chase);
                    break;
                case State.Chase:
                    Face(toPlayer);
                    if (distance > LoseRadius) SetState(State.Idle);
                    else if (distance <= attackRange) SetState(State.Attack);
                    else MoveTowards(toPlayer, Mathf.Min(moveSpeed * Time.fixedDeltaTime,
                        Mathf.Max(0f, distance - attackRange)));
                    break;
                case State.Attack:
                    StopMovement();
                    Face(toPlayer);
                    stateRemaining -= Time.fixedDeltaTime;
                    if (stateRemaining <= 0f)
                    {
                        // Transition first: damage events cannot cause this swing to apply twice.
                        SetState(State.Recover);
                        if (HasTarget && distance <= attackRange)
                            player.TakeDamage(new DamageInfo(attackDamage, gameObject, body.position));
                    }
                    break;
                case State.Recover:
                    StopMovement();
                    stateRemaining -= Time.fixedDeltaTime;
                    if (stateRemaining <= 0f)
                        SetState(distance <= LoseRadius ? State.Chase : State.Idle);
                    break;
            }
        }

        private void SetState(State next)
        {
            CurrentState = next;
            stateRemaining = next == State.Alert ? alertDuration
                : next == State.Attack ? attackWindup
                : next == State.Recover ? recoverDuration : 0f;
            SetIndicator(next == State.Alert);
            StopMovement();
        }

        private void SetIndicator(bool visible)
        {
            if (alertIndicator != null && alertIndicator.activeSelf != visible)
                alertIndicator.SetActive(visible);
        }

        private void Face(Vector2 direction)
        {
            if (visual != null && Mathf.Abs(direction.x) > 0.0001f)
                visual.SetFacingRight(direction.x > 0f);
        }

        private void MoveTowards(Vector2 direction, float distance)
        {
            direction.Normalize();
            var filter = new ContactFilter2D();
            filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
            filter.useTriggers = false;
            float skin = Physics2D.defaultContactOffset;
            int count = body.Cast(direction, filter, movementHits, distance + skin);
            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = movementHits[i];
                if (Vector2.Dot(direction, hit.normal) >= 0f) continue;
                distance = Mathf.Min(distance, Mathf.Max(0f, hit.distance - skin));
            }
            body.MovePosition(body.position + direction * Mathf.Max(0f, distance));
        }

        private void StopMovement()
        {
            if (body == null || (hitReaction != null && hitReaction.IsKnockedBack)) return;
            if (body.simulated && body.bodyType == RigidbodyType2D.Kinematic)
                body.MovePosition(body.position);
            body.linearVelocity = Vector2.zero;
        }

        private void OnDeath(DamageInfo damage) => SetState(State.Dead);

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(0f, detectionRadius));
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, LoseRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(0f, attackRange));
        }
    }
}
