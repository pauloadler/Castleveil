using UnityEngine;

namespace Game.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMovement))]
    [DefaultExecutionOrder(110)]
    public sealed class PlayerPlaceholderVisual : MonoBehaviour
    {
        [SerializeField] private Transform facingMarker;
        [SerializeField, Min(0f)] private float markerDistance = 0.65f;
        private PlayerMovement movement;

        private void Awake() => movement = GetComponent<PlayerMovement>();

        private void LateUpdate()
        {
            if (facingMarker != null)
                facingMarker.localPosition = (Vector3)(movement.FacingDirection * markerDistance);
        }
    }
}
