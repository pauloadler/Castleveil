using UnityEngine;

namespace Game.Enemies
{
    [DisallowMultipleComponent]
    public sealed class AlertIndicatorVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Sprite[] frames = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] private float cycleDuration = 0.20f;

        private double startedAt;

        private void OnEnable()
        {
            if (targetRenderer == null) targetRenderer = GetComponent<SpriteRenderer>();
            if (targetRenderer == null)
            {
                Debug.LogError("AlertIndicatorVisual needs a SpriteRenderer on this object or assigned in the Inspector.", this);
                enabled = false;
                return;
            }
            startedAt = Time.timeAsDouble;
            ShowFrame(0);
        }

        private void Update()
        {
            if (frames == null || frames.Length == 0 || targetRenderer == null) return;
            float duration = cycleDuration > 0f && !float.IsInfinity(cycleDuration)
                ? cycleDuration : 0.20f;
            double progress = ((Time.timeAsDouble - startedAt) % duration) / duration;
            int index = Mathf.Min((int)(progress * frames.Length), frames.Length - 1);
            ShowFrame(index);
        }

        private void ShowFrame(int index)
        {
            if (frames == null || frames.Length == 0 || targetRenderer == null) return;
            Sprite frame = frames[index];
            if (frame != null && targetRenderer.sprite != frame) targetRenderer.sprite = frame;
        }
    }
}
