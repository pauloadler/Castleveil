using UnityEngine;

namespace Game.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    [DefaultExecutionOrder(160)]
    public sealed class ErranteDirectionalSprite : MonoBehaviour
    {
        [SerializeField] private PlayerAnimationController facingSource;
        [SerializeField] private SpriteRenderer targetRenderer;
        [Header("Idle")]
        [SerializeField] private Sprite north;
        [SerializeField] private Sprite northEast;
        [SerializeField] private Sprite east;
        [SerializeField] private Sprite southEast;
        [SerializeField] private Sprite south;
        [SerializeField] private Sprite southWest;
        [SerializeField] private Sprite west;
        [SerializeField] private Sprite northWest;

        [Header("Run frames (looping playback order)")]
        [SerializeField, Min(0.01f)] private float runFramesPerSecond = 8f;
        [SerializeField] private Sprite[] run_N = System.Array.Empty<Sprite>();
        [SerializeField] private Sprite[] run_NE = System.Array.Empty<Sprite>();
        [SerializeField] private Sprite[] run_E = System.Array.Empty<Sprite>();
        [SerializeField] private Sprite[] run_SE = System.Array.Empty<Sprite>();
        [SerializeField] private Sprite[] run_S = System.Array.Empty<Sprite>();
        [SerializeField] private Sprite[] run_SW = System.Array.Empty<Sprite>();
        [SerializeField] private Sprite[] run_W = System.Array.Empty<Sprite>();
        [SerializeField] private Sprite[] run_NW = System.Array.Empty<Sprite>();

        [Header("Attack frames (playback order; duration follows the real attack)")]
        [SerializeField] private Sprite[] attack_N = System.Array.Empty<Sprite>();
        [SerializeField] private Sprite[] attack_NE = System.Array.Empty<Sprite>();
        [SerializeField] private Sprite[] attack_E = System.Array.Empty<Sprite>();
        [SerializeField] private Sprite[] attack_SE = System.Array.Empty<Sprite>();
        [SerializeField] private Sprite[] attack_S = System.Array.Empty<Sprite>();
        [SerializeField] private Sprite[] attack_SW = System.Array.Empty<Sprite>();
        [SerializeField] private Sprite[] attack_W = System.Array.Empty<Sprite>();
        [SerializeField] private Sprite[] attack_NW = System.Array.Empty<Sprite>();

        private PlayerAnimationController subscribedSource;
        private bool showingAttack;
        private bool showingRun;
        private Direction8 runDirection;
        private float runFrame;

        private void Awake()
        {
            if (targetRenderer == null)
                targetRenderer = GetComponent<SpriteRenderer>();
            if (facingSource == null)
                facingSource = GetComponentInParent<PlayerAnimationController>();
        }

        private void OnEnable()
        {
            if (facingSource == null || targetRenderer == null)
            {
                Debug.LogError("ErranteDirectionalSprite needs a facing source and a target SpriteRenderer.", this);
                enabled = false;
                return;
            }

            subscribedSource = facingSource;
            subscribedSource.FacingChanged += SetDirection;
            subscribedSource.AttackVisualUpdated += RefreshAttack;
            subscribedSource.LocomotionVisualUpdated += RefreshLocomotion;
            // Synchronize immediately, including when re-enabled without a direction change.
            SetDirection(subscribedSource.Facing);
            if (subscribedSource.isActiveAndEnabled && subscribedSource.IsAttacking)
                RefreshAttack(true, subscribedSource.AttackFacing, subscribedSource.AttackProgress);
        }

        private void Start()
        {
            if (north == null || northEast == null || east == null || southEast == null
                || south == null || southWest == null || west == null || northWest == null)
                Debug.LogWarning("Assign all eight Errante sprites. Missing directions preserve the current sprite.", this);
        }

        private void OnDisable()
        {
            if (subscribedSource != null)
            {
                subscribedSource.FacingChanged -= SetDirection;
                subscribedSource.AttackVisualUpdated -= RefreshAttack;
                subscribedSource.LocomotionVisualUpdated -= RefreshLocomotion;
                showingAttack = false;
                ApplyFacing(subscribedSource.Facing);
            }
            showingAttack = false;
            showingRun = false;
            runFrame = 0f;
            subscribedSource = null;
        }

        public void SetDirection(Direction8 direction)
        {
            if (!showingAttack)
                ApplyLocomotion(direction, 0f);
        }

        private void RefreshLocomotion(float deltaTime)
        {
            if (showingAttack)
            {
                showingRun = false;
                runFrame = 0f;
                return;
            }
            if (subscribedSource != null)
                ApplyLocomotion(subscribedSource.Facing, deltaTime);
        }

        private void ApplyLocomotion(Direction8 direction, float deltaTime)
        {
            Sprite[] frames = null;
            if (subscribedSource != null && subscribedSource.isActiveAndEnabled && subscribedSource.IsMoving)
            {
                switch (direction)
                {
                    case Direction8.North: frames = run_N; break;
                    case Direction8.NorthEast: frames = run_NE; break;
                    case Direction8.East: frames = run_E; break;
                    case Direction8.SouthEast: frames = run_SE; break;
                    case Direction8.South: frames = run_S; break;
                    case Direction8.SouthWest: frames = run_SW; break;
                    case Direction8.West: frames = run_W; break;
                    case Direction8.NorthWest: frames = run_NW; break;
                }
            }

            bool valid = frames != null && frames.Length > 0;
            if (valid)
                foreach (Sprite frame in frames)
                    if (frame == null) { valid = false; break; }

            if (!valid)
            {
                showingRun = false;
                runFrame = 0f;
                ApplyFacing(direction);
                return;
            }

            // Restart on entering Run or changing direction; physics speed is independent.
            if (!showingRun || runDirection != direction)
                runFrame = 0f;
            else
                runFrame = (runFrame + deltaTime * Mathf.Max(0.01f, runFramesPerSecond)) % frames.Length;
            showingRun = true;
            runDirection = direction;
            Sprite selected = frames[Mathf.Min(Mathf.FloorToInt(runFrame), frames.Length - 1)];
            if (targetRenderer != null && targetRenderer.sprite != selected)
                targetRenderer.sprite = selected;
        }

        private void RefreshAttack(bool active, Direction8 direction, float progress)
        {
            showingAttack = active;
            if (!active)
            {
                RefreshLocomotion(0f);
                return;
            }

            Sprite[] frames;
            switch (direction)
            {
                case Direction8.North: frames = attack_N; break;
                case Direction8.NorthEast: frames = attack_NE; break;
                case Direction8.East: frames = attack_E; break;
                case Direction8.SouthEast: frames = attack_SE; break;
                case Direction8.South: frames = attack_S; break;
                case Direction8.SouthWest: frames = attack_SW; break;
                case Direction8.West: frames = attack_W; break;
                case Direction8.NorthWest: frames = attack_NW; break;
                default: return;
            }

            if (frames == null || frames.Length == 0)
            {
                ApplyFacing(direction);
                return;
            }
            // Give every frame an equal interval, including the last before progress reaches 1.
            int index = Mathf.Min(Mathf.FloorToInt(Mathf.Clamp01(progress) * frames.Length), frames.Length - 1);
            Sprite frame = frames[index];
            if (frame == null)
                ApplyFacing(direction);
            else if (targetRenderer != null && targetRenderer.sprite != frame)
                targetRenderer.sprite = frame;
        }

        private void ApplyFacing(Direction8 direction)
        {
            Sprite selected;
            switch (direction)
            {
                case Direction8.North: selected = north; break;
                case Direction8.NorthEast: selected = northEast; break;
                case Direction8.East: selected = east; break;
                case Direction8.SouthEast: selected = southEast; break;
                case Direction8.South: selected = south; break;
                case Direction8.SouthWest: selected = southWest; break;
                case Direction8.West: selected = west; break;
                case Direction8.NorthWest: selected = northWest; break;
                default: return;
            }

            if (targetRenderer != null && selected != null && targetRenderer.sprite != selected)
                targetRenderer.sprite = selected;
        }
    }
}
