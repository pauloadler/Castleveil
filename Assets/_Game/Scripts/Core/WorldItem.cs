using System.Collections.Generic;
using Game.Player;
using UnityEngine;

namespace Game.Items
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class WorldItem : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform itemVisual;
        [SerializeField] private Transform arrowVisual;
        [SerializeField] private WorldItemHighlight highlight;
        [SerializeField] private WorldItemLabel lootLabel;
        [SerializeField] private Collider2D mouseCollider;
        [SerializeField] private Collider2D interactionTrigger;
        [SerializeField] private PlayerInputReader playerInput;
        [SerializeField] private Camera worldCamera;
        [SerializeField, Min(0f)] private float floatAmplitude = 0.08f;
        [SerializeField, Min(0f)] private float floatSpeed = 2f;
        [SerializeField, Min(0f)] private float arrowAmplitude = 0.08f;
        [SerializeField, Min(0f)] private float arrowSpeed = 3f;
        private readonly Dictionary<Collider2D, PlayerEquipment> overlaps = new Dictionary<Collider2D, PlayerEquipment>();
        private Vector3 visualOrigin;
        private Vector3 arrowOrigin;
        private Vector3 labelOrigin;
        private bool collected;
        public bool IsHovered { get; private set; }

        private void Awake()
        {
            if (itemVisual == transform || (itemVisual != null && !itemVisual.IsChildOf(transform))) itemVisual = null;
            if (arrowVisual == transform || (arrowVisual != null && !arrowVisual.IsChildOf(transform))) arrowVisual = null;
            if (itemVisual != null) visualOrigin = itemVisual.localPosition;
            if (arrowVisual != null) arrowOrigin = arrowVisual.localPosition;
            if (lootLabel != null && (lootLabel.transform.parent != transform))
            {
                Debug.LogError("WorldItem Loot Label must be on the direct child LootLabelRoot.", this);
                lootLabel = null;
            }
            if (lootLabel != null) labelOrigin = lootLabel.transform.localPosition;
            if (interactionTrigger == null || (lootLabel == null && mouseCollider == null))
                Debug.LogError("WorldItem needs an Interaction Trigger and a Loot Label (or optional Mouse Collider fallback).", this);
        }
        private void Start()
        {
            if (worldCamera == null) worldCamera = Camera.main;
            if (playerInput == null) playerInput = FindAnyObjectByType<PlayerInputReader>();
        }
        private void OnEnable()
        {
            if (arrowVisual != null) arrowVisual.gameObject.SetActive(!collected);
        }
        private void Update()
        {
            if (collected) return;
            Vector3 floatOffset = Vector3.up * (Mathf.Sin(Time.time * floatSpeed) * floatAmplitude);
            if (itemVisual != null) itemVisual.localPosition = visualOrigin + floatOffset;
            if (lootLabel != null) lootLabel.transform.localPosition = labelOrigin + floatOffset;
            if (arrowVisual != null)
            {
                Vector3 worldOffset = transform.TransformVector(Vector3.right * (Mathf.Sin(Time.time * arrowSpeed) * arrowAmplitude));
                arrowVisual.localPosition = arrowOrigin + arrowVisual.parent.InverseTransformVector(worldOffset);
            }
            IsHovered = false;
            if (lootLabel != null && playerInput != null && playerInput.HasPointer)
                IsHovered = lootLabel.ContainsPointer(playerInput.PointerScreenPosition, worldCamera);
            else if (lootLabel == null && playerInput != null && playerInput.HasPointer && worldCamera != null
                && mouseCollider != null && mouseCollider.enabled && mouseCollider.gameObject.activeInHierarchy)
            {
                Ray ray = worldCamera.ScreenPointToRay(playerInput.PointerScreenPosition);
                var plane = new Plane(Vector3.forward, mouseCollider.transform.position);
                if (plane.Raycast(ray, out float distance)) IsHovered = mouseCollider.OverlapPoint(ray.GetPoint(distance));
            }
            if (highlight != null) highlight.SetHovered(IsHovered);
            if (!IsHovered || Time.timeScale <= 0f || !playerInput.BasicAttackPressedThisFrame) return;
            PlayerEquipment player = playerInput.GetComponent<PlayerEquipment>();
            // Even an out-of-range loot attempt must not become an attack in LateUpdate.
            playerInput.ConsumePrimaryClick();
            Interact(player);
        }
        private void OnTriggerEnter2D(Collider2D other) => Track(other);
        private void OnTriggerStay2D(Collider2D other) => Track(other);
        private void Track(Collider2D other)
        {
            if (!isActiveAndEnabled || collected || other.isTrigger) return;
            PlayerEquipment player = other.GetComponentInParent<PlayerEquipment>();
            if (player != null) overlaps[other] = player;
        }
        private void OnTriggerExit2D(Collider2D other)
        {
            if (interactionTrigger == null || !interactionTrigger.IsTouching(other)) overlaps.Remove(other);
        }
        public bool CanInteract(PlayerEquipment player)
        {
            if (collected || !isActiveAndEnabled || player == null || !player.CanEquip
                || interactionTrigger == null || !interactionTrigger.enabled
                || !interactionTrigger.gameObject.activeInHierarchy) return false;
            foreach (var pair in overlaps)
            {
                Collider2D collider = pair.Key;
                if (pair.Value == player && collider != null && collider.enabled
                    && collider.gameObject.activeInHierarchy && interactionTrigger.Distance(collider).isOverlapped) return true;
            }
            return false;
        }
        public void Interact(PlayerEquipment player)
        {
            if (!CanInteract(player) || !player.EquipWeapon()) return;
            collected = true;
            gameObject.SetActive(false);
        }
        private void OnDisable()
        {
            overlaps.Clear();
            IsHovered = false;
            if (highlight != null) highlight.SetHovered(false);
            if (itemVisual != null) itemVisual.localPosition = visualOrigin;
            if (arrowVisual != null) arrowVisual.localPosition = arrowOrigin;
            if (lootLabel != null) lootLabel.transform.localPosition = labelOrigin;
        }
    }
}

