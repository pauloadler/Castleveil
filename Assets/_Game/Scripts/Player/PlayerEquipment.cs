using Game.Combat;

using UnityEngine;

namespace Game.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputReader), typeof(Health))]
    public sealed class PlayerEquipment : MonoBehaviour
    {
        [SerializeField] private GameObject weaponVisual;

        private Health health;

        public bool HasWeapon { get; private set; }
        public bool CanEquip => isActiveAndEnabled && health != null && !health.IsDead && !HasWeapon;

        private void Awake()
        {

            health = GetComponent<Health>();
            if (weaponVisual != null && (weaponVisual == gameObject
                || !weaponVisual.transform.IsChildOf(transform)))
            {
                Debug.LogError("PlayerEquipment Weapon Visual must be a dedicated child of Player.", this);
                weaponVisual = null;
            }
            if (weaponVisual != null) weaponVisual.SetActive(false);
        }

        public bool EquipWeapon()
        {
            if (!CanEquip) return false;
            HasWeapon = true;
            if (weaponVisual != null) weaponVisual.SetActive(true);
            return true;
        }

    }
}


