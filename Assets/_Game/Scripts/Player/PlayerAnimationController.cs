using System;
using Game.Combat;
using UnityEngine;

namespace Game.Player
{
    public enum PlayerVisualState { Idle, Walk, Attack, Dodge, Hit, Death }

    [DisallowMultipleComponent]
    [DefaultExecutionOrder(150)]
    public sealed class PlayerAnimationController : MonoBehaviour
    {
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private PlayerDodge dodge;
        [SerializeField] private Health health;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Animator animator;
        [SerializeField, Min(0f)] private float movingThreshold = 0.05f;
        [SerializeField, Min(0f)] private float hitPresentationDuration = 0.15f;

        private float hitEndsAt;
        private RuntimeAnimatorController cachedController;
        private AnimatorControllerParameter[] parameters = Array.Empty<AnimatorControllerParameter>();

        public Vector2 MovementDirection { get; private set; }
        public Vector2 FacingDirection { get; private set; } = Vector2.down;
        public Direction8 Facing { get; private set; } = Direction8.South;
        public bool IsMoving { get; private set; }
        public bool IsAttacking => combat != null && combat.IsAttacking;
        public bool IsDodging => dodge != null && dodge.IsDodging;
        public PlayerVisualState State { get; private set; }
        public event Action<PlayerVisualState> StateChanged;

        private void Awake()
        {
            if (movement == null) movement = GetComponentInParent<PlayerMovement>();
            if (combat == null) combat = GetComponentInParent<PlayerCombat>();
            if (dodge == null) dodge = GetComponentInParent<PlayerDodge>();
            if (health == null) health = GetComponentInParent<Health>();
            if (body == null) body = GetComponentInParent<Rigidbody2D>();
            if (animator == null) animator = GetComponent<Animator>();
        }

        private void OnEnable()
        {
            if (health != null) health.DamageTaken += OnDamage;
        }

        private void OnDisable()
        {
            if (health != null) health.DamageTaken -= OnDamage;
            hitEndsAt = 0f;
        }

        private void OnDamage(DamageInfo damage, float amount)
        {
            if (amount > 0f) hitEndsAt = Time.time + hitPresentationDuration;
        }

        private void LateUpdate()
        {
            Vector2 velocity = body != null ? body.linearVelocity : Vector2.zero;
            IsMoving = velocity.sqrMagnitude > movingThreshold * movingThreshold;
            MovementDirection = IsMoving ? velocity.normalized : Vector2.zero;
            if (movement != null) FacingDirection = movement.FacingDirection;
            Facing = DirectionResolver.Resolve(FacingDirection, Facing);

            PlayerVisualState next = health != null && health.IsDead ? PlayerVisualState.Death
                : Time.time < hitEndsAt ? PlayerVisualState.Hit
                : IsDodging ? PlayerVisualState.Dodge
                : IsAttacking ? PlayerVisualState.Attack
                : IsMoving ? PlayerVisualState.Walk : PlayerVisualState.Idle;
            if (State != next)
            {
                State = next;
                StateChanged?.Invoke(State);
            }
            UpdateAnimator();
        }

        private void UpdateAnimator()
        {
            if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
                return;
            if (cachedController != animator.runtimeAnimatorController)
            {
                cachedController = animator.runtimeAnimatorController;
                parameters = animator.parameters;
            }
            // Optional parameters let future controllers adopt this contract incrementally.
            foreach (AnimatorControllerParameter parameter in parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Int)
                {
                    if (parameter.name == "VisualState") animator.SetInteger(parameter.nameHash, (int)State);
                    else if (parameter.name == "FacingDirection") animator.SetInteger(parameter.nameHash, (int)Facing);
                }
                else if (parameter.type == AnimatorControllerParameterType.Bool)
                {
                    if (parameter.name == "IsMoving") animator.SetBool(parameter.nameHash, IsMoving);
                    else if (parameter.name == "IsAttacking") animator.SetBool(parameter.nameHash, IsAttacking);
                    else if (parameter.name == "IsDodging") animator.SetBool(parameter.nameHash, IsDodging);
                }
                else if (parameter.type == AnimatorControllerParameterType.Float)
                {
                    if (parameter.name == "MoveX") animator.SetFloat(parameter.nameHash, MovementDirection.x);
                    else if (parameter.name == "MoveY") animator.SetFloat(parameter.nameHash, MovementDirection.y);
                    else if (parameter.name == "FacingX") animator.SetFloat(parameter.nameHash, FacingDirection.x);
                    else if (parameter.name == "FacingY") animator.SetFloat(parameter.nameHash, FacingDirection.y);
                }
            }
        }
    }
}
