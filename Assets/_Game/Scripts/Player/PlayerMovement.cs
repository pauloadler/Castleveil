using UnityEngine;

namespace Game.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerInputReader))]
    [DefaultExecutionOrder(100)]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float movementSpeed = 5f;
        [SerializeField] private Camera worldCamera;

        private Rigidbody2D body;
        private PlayerInputReader input;
        private MonoBehaviour externalMovementOwner;

        public Vector2 FacingDirection { get; private set; } = Vector2.down;
        public Vector2 MouseWorldPosition { get; private set; }
        public float MovementSpeed => movementSpeed;

        public bool TryAcquireMovementControl(MonoBehaviour owner)
        {
            if (owner == null || (externalMovementOwner != null && externalMovementOwner != owner))
                return false;
            externalMovementOwner = owner;
            return true;
        }

        public void ReleaseMovementControl(MonoBehaviour owner)
        {
            if (externalMovementOwner == owner)
                externalMovementOwner = null;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            input = GetComponent<PlayerInputReader>();
        }

        private void Start()
        {
            if (worldCamera == null)
                worldCamera = Camera.main;
            if (worldCamera == null)
                Debug.LogError("PlayerMovement needs a world camera for mouse facing.", this);
        }

        private void FixedUpdate()
        {
            if (externalMovementOwner != null)
                return;
            // Velocity is in units per second. The physics step applies delta time.
            body.linearVelocity = Vector2.ClampMagnitude(input.MoveInput, 1f) * movementSpeed;
        }

        private void LateUpdate()
        {
            if (worldCamera == null || !input.HasPointer)
                return;

            // Resolve against the gameplay plane after Cinemachine updates the camera.
            Ray ray = worldCamera.ScreenPointToRay(input.PointerScreenPosition);
            Plane plane = new Plane(Vector3.forward, transform.position);
            if (!plane.Raycast(ray, out float distance))
                return;

            MouseWorldPosition = ray.GetPoint(distance);
            Vector2 direction = MouseWorldPosition - (Vector2)transform.position;
            // Preserve the last unit direction when the pointer is at the player's center.
            if (direction.sqrMagnitude > 0.0001f)
                FacingDirection = direction.normalized;
        }

        private void OnDisable()
        {
            if (body != null && externalMovementOwner == null)
                body.linearVelocity = Vector2.zero;
        }

        private void OnValidate() => movementSpeed = Mathf.Max(0f, movementSpeed);
    }
}
