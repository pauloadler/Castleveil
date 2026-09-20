using UnityEngine;

namespace Game.Combat
{
    public readonly struct DamageInfo
    {
        /// <summary>Final damage to remove from health, after any caller-side modifiers.</summary>
        public float Amount { get; }

        /// <summary>The originating object, or null for damage without an object source.</summary>
        public GameObject Source { get; }

        public DamageInfo(float amount, GameObject source = null)
        {
            Amount = float.IsNaN(amount) || float.IsInfinity(amount)
                ? 0f : Mathf.Max(0f, amount);
            Source = source;
        }
    }
}
