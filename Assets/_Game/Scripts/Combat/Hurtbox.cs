using UnityEngine;

namespace Game.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class Hurtbox : MonoBehaviour
    {
        [Tooltip("A component implementing IDamageable, normally Health.")]
        [SerializeField] private MonoBehaviour damageReceiver;

        public IDamageable Receiver => damageReceiver as IDamageable;
        public GameObject Owner => damageReceiver != null ? damageReceiver.gameObject : gameObject;
        public bool CanReceiveDamage => isActiveAndEnabled && damageReceiver != null
            && damageReceiver.isActiveAndEnabled && Receiver != null;

        private void Awake()
        {
            if (damageReceiver == null)
                damageReceiver = GetComponentInParent<Health>();
            if (Receiver == null)
                Debug.LogError("Hurtbox needs a component implementing IDamageable.", this);
        }
    }
}
