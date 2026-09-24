using Game.Player;

namespace Game.Items
{
    public interface IInteractable
    {
        bool CanInteract(PlayerEquipment player);
        void Interact(PlayerEquipment player);
    }
}
