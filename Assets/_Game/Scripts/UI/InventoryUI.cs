using Game.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    [DisallowMultipleComponent]
    public sealed class InventoryUI : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerEquipment equipment;
        [SerializeField] private GameObject panel;
        [SerializeField] private Image weaponIcon;
        [SerializeField] private Sprite weaponSprite;
        [SerializeField] private GameObject weaponSilhouette;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
        }

        private void OnEnable()
        {
            if (input != null) input.InventoryRequested += Toggle;
            if (equipment != null) equipment.EquipmentChanged += RefreshWeapon;
            RefreshWeapon();
        }

        private void OnDisable()
        {
            if (input != null) input.InventoryRequested -= Toggle;
            if (equipment != null) equipment.EquipmentChanged -= RefreshWeapon;
        }

        public void Toggle()
        {
            if (panel != null) panel.SetActive(!panel.activeSelf);
        }

        private void RefreshWeapon()
        {
            if (weaponIcon == null) return;
            weaponIcon.sprite = weaponSprite;
            weaponIcon.enabled = equipment != null && equipment.HasWeapon && weaponSprite != null;
            if (weaponSilhouette != null) weaponSilhouette.SetActive(equipment == null || !equipment.HasWeapon);
        }
    }
}
