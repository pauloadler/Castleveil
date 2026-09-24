using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;

        private InputActionAsset runtimeActions;
        private InputAction moveAction;
        private InputAction pointerAction;
        private InputAction basicAttackAction;
        private InputAction dodgeAction;
        private InputAction interactAction;
        private bool hasFocus = true;
        private int consumedAttackFrame = -1;
        public void ConsumePrimaryClick() => consumedAttackFrame = Time.frameCount;

        public Vector2 MoveInput => isActiveAndEnabled && hasFocus && moveAction != null
            ? Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f) : Vector2.zero;
        public Vector2 PointerScreenPosition => pointerAction != null
            ? pointerAction.ReadValue<Vector2>() : Vector2.zero;
        public bool HasPointer => isActiveAndEnabled && hasFocus && pointerAction != null
            && pointerAction.enabled && pointerAction.controls.Count > 0;
        public bool BasicAttackPressedThisFrame => isActiveAndEnabled && hasFocus
            && consumedAttackFrame != Time.frameCount && basicAttackAction != null && basicAttackAction.WasPressedThisFrame();
        public bool DodgePressedThisFrame => isActiveAndEnabled && hasFocus
            && dodgeAction != null && dodgeAction.WasPressedThisFrame();
        public bool InteractPressedThisFrame => isActiveAndEnabled && hasFocus
            && interactAction != null && interactAction.WasPressedThisFrame();

        private void OnEnable()
        {
            if (inputActions == null)
            {
                Debug.LogError("Assign the Milestone1 Input Actions asset to PlayerInputReader.", this);
                enabled = false;
                return;
            }

            // Each player owns its action state; disabling one cannot disable a shared asset.
            runtimeActions = Instantiate(inputActions);
            moveAction = runtimeActions.FindAction("Player/Move", true);
            pointerAction = runtimeActions.FindAction("Player/Pointer", true);
            basicAttackAction = runtimeActions.FindAction("Player/BasicAttack");
            dodgeAction = runtimeActions.FindAction("Player/Dodge");
            interactAction = runtimeActions.FindAction("Player/Interact");
            runtimeActions.Enable();
        }

        private void OnDisable()
        {
            if (runtimeActions != null)
            {
                runtimeActions.Disable();
                Destroy(runtimeActions);
            }
            runtimeActions = null;
            moveAction = null;
            pointerAction = null;
            basicAttackAction = null;
            dodgeAction = null;
            interactAction = null;
        }

        private void OnApplicationFocus(bool focused) => hasFocus = focused;
    }
}
