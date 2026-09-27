using System;
using UnityEngine;

namespace Game.Player
{
    /// <summary>Presentation only. Equipment and combat remain the gameplay authorities.</summary>
    [DisallowMultipleComponent]
    public sealed class EquipmentVisualController : MonoBehaviour
    {
        [Serializable]
        public struct WeaponPose
        {
            public Vector2 offset;
            public float rotation;
            public bool flipX;
            public int sortingOffset;

            public WeaponPose(float x, float y, float angle, bool flip, int order)
            {
                offset = new Vector2(x, y);
                rotation = angle;
                flipX = flip;
                sortingOffset = order;
            }
        }

        [SerializeField] private PlayerEquipment equipment;
        [SerializeField] private PlayerAnimationController facingSource;
        [SerializeField] private SpriteRenderer characterRenderer;
        [Tooltip("Assign the existing dedicated WeaponVisual renderer, also referenced by PlayerEquipment.")]
        [SerializeField] private SpriteRenderer weaponRenderer;
        [SerializeField] private Sprite weaponSprite;
        [SerializeField, Min(0.01f)] private float weaponScale = 0.06f;
        [Header("Local poses relative to VisualRoot; angle assumes an upward pointing sprite")]
        [SerializeField] private WeaponPose north = new WeaponPose(.18f, .25f, 0f, false, -1);
        [SerializeField] private WeaponPose northEast = new WeaponPose(.25f, .18f, -45f, false, -1);
        [SerializeField] private WeaponPose east = new WeaponPose(.28f, .05f, -90f, false, 1);
        [SerializeField] private WeaponPose southEast = new WeaponPose(.23f, -.10f, -135f, false, 1);
        [SerializeField] private WeaponPose south = new WeaponPose(.16f, -.15f, 180f, false, 1);
        [SerializeField] private WeaponPose southWest = new WeaponPose(-.23f, -.10f, 135f, true, 1);
        [SerializeField] private WeaponPose west = new WeaponPose(-.28f, .05f, 90f, true, 1);
        [SerializeField] private WeaponPose northWest = new WeaponPose(-.25f, .18f, 45f, true, -1);

        private Transform mainHand;
        private PlayerAnimationController subscribedSource;

        private void Awake()
        {
            if (equipment == null) equipment = GetComponentInParent<PlayerEquipment>();
            if (facingSource == null) facingSource = GetComponent<PlayerAnimationController>();
            if (characterRenderer == null)
            {
                Transform character = transform.Find("CharacterSprite");
                if (character != null) characterRenderer = character.GetComponent<SpriteRenderer>();
            }
            // Never move a gameplay object or the character renderer when a reference is misassigned.
            if (equipment == null || facingSource == null || characterRenderer == null
                || weaponRenderer == null || weaponRenderer == characterRenderer
                || !weaponRenderer.transform.IsChildOf(equipment.transform)
                || weaponRenderer.transform == equipment.transform
                || transform.IsChildOf(weaponRenderer.transform)
                || weaponRenderer.transform.childCount != 0)
            {
                Debug.LogError("EquipmentVisualController: attach to VisualRoot and assign a dedicated leaf WeaponVisual SpriteRenderer, CharacterSprite, PlayerEquipment and facing source.", this);
                enabled = false;
                return;
            }
            weaponRenderer.enabled = false;
            Transform root = GetOrCreateChild(transform, "EquipmentRoot");
            mainHand = GetOrCreateChild(root, "MainHand");
            // Reuse the same GameObject PlayerEquipment toggles; no second weapon is created.
            weaponRenderer.transform.SetParent(mainHand, false);
            weaponRenderer.transform.localPosition = Vector3.zero;
            weaponRenderer.transform.localRotation = Quaternion.identity;
        }

        private static Transform GetOrCreateChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null) return child;
            child = new GameObject(childName).transform;
            child.SetParent(parent, false);
            return child;
        }

        private void OnEnable()
        {
            if (mainHand == null || facingSource == null) return;

            subscribedSource = facingSource;
            subscribedSource.LocomotionVisualUpdated += Refresh;

            if (equipment != null)
                equipment.EquipmentChanged += OnEquipmentChanged;

            Refresh(0f);
        }

        private void OnDisable()
        {
            if (subscribedSource != null)
                subscribedSource.LocomotionVisualUpdated -= Refresh;

            if (equipment != null)
                equipment.EquipmentChanged -= OnEquipmentChanged;

            subscribedSource = null;

            if (mainHand != null && weaponRenderer != null)
                weaponRenderer.enabled = false;
        }

        private void OnEquipmentChanged()
        {
            Refresh(0f);
        }

        private void Refresh(float unusedDeltaTime)
        {
            if (weaponRenderer == null || mainHand == null || characterRenderer == null) return;
            bool visible = equipment != null && equipment.HasWeapon && weaponSprite != null;
            weaponRenderer.enabled = visible;
            if (!visible) return;
            Direction8 direction = facingSource.IsAttacking ? facingSource.AttackFacing : facingSource.Facing;
            WeaponPose pose = GetPose(direction);
            weaponRenderer.sprite = weaponSprite;
            weaponRenderer.flipX = pose.flipX;
            weaponRenderer.sortingLayerID = characterRenderer.sortingLayerID;
            weaponRenderer.sortingOrder = characterRenderer.sortingOrder + pose.sortingOffset;
            mainHand.localPosition = new Vector3(pose.offset.x, pose.offset.y, 0f);
            mainHand.localRotation = Quaternion.Euler(0f, 0f, pose.rotation);
            float scale = float.IsNaN(weaponScale) || float.IsInfinity(weaponScale) || weaponScale <= 0f ? 1f : weaponScale;
            weaponRenderer.transform.localScale = Vector3.one * scale;
        }

        private WeaponPose GetPose(Direction8 direction)
        {
            switch (direction)
            {
                case Direction8.North: return north;
                case Direction8.NorthEast: return northEast;
                case Direction8.East: return east;
                case Direction8.SouthEast: return southEast;
                case Direction8.SouthWest: return southWest;
                case Direction8.West: return west;
                case Direction8.NorthWest: return northWest;
                default: return south;
            }
        }
    }
}
