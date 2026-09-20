using UnityEngine;

namespace Game.Player
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(110)]
    public sealed class PlayerPlaceholderVisual : MonoBehaviour
    {
        [SerializeField] private Transform facingMarker;
        [SerializeField, Min(0f)] private float markerDistance = 0.65f;
        [SerializeField] private PlayerMovement movement;

        private void Awake()
        {
            if (movement == null)
                movement = GetComponentInParent<PlayerMovement>();
        }

        private void LateUpdate()
        {
            if (facingMarker != null && movement != null)
            {
                Vector3 offset = (Vector3)(movement.FacingDirection * markerDistance);
                facingMarker.localPosition = facingMarker.parent != null
                    ? facingMarker.parent.InverseTransformVector(offset) : offset;
            }
        }
    }
}
