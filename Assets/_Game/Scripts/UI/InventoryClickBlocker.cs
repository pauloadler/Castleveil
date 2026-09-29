using Game.Player;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UI
{
    // Pointer-down is consumed before PlayerCombat.LateUpdate, including clicks on empty slots.
    public sealed class InventoryClickBlocker : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private PlayerInputReader input;
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && input != null)
                input.ConsumePrimaryClick();
        }
    }
}
