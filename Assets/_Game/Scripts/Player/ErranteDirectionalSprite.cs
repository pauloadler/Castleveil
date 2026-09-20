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
        [SerializeField] private Sprite north;
        [SerializeField] private Sprite northEast;
        [SerializeField] private Sprite east;
        [SerializeField] private Sprite southEast;
        [SerializeField] private Sprite south;
        [SerializeField] private Sprite southWest;
        [SerializeField] private Sprite west;
        [SerializeField] private Sprite northWest;

        private PlayerAnimationController subscribedSource;

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
            // Synchronize immediately, including when re-enabled without a direction change.
            SetDirection(subscribedSource.Facing);
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
                subscribedSource.FacingChanged -= SetDirection;
            subscribedSource = null;
        }

        public void SetDirection(Direction8 direction)
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
