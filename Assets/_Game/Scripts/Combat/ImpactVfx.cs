using UnityEngine;

namespace Game.Combat
{
    [DisallowMultipleComponent]
    public sealed class ImpactVfx : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Sprite[] frames = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] private float duration = 0.12f;

        private double startedAt;
        private float playbackDuration;

        private void Awake()
        {
            if (targetRenderer == null) targetRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }

        private void OnEnable()
        {
            if (targetRenderer == null)
            {
                Debug.LogError("ImpactVfx needs a SpriteRenderer assigned on its prefab.", this);
                Finish();
                return;
            }
            if (frames == null || frames.Length == 0)
            {
                Finish();
                return;
            }
            startedAt = Time.timeAsDouble;
            playbackDuration = duration > 0f && !float.IsInfinity(duration) ? duration : 0.12f;
            ShowFrame(0);
        }

        private void Update()
        {
            double progress = (Time.timeAsDouble - startedAt) / playbackDuration;
            if (progress >= 1d) { Finish(); return; }
            int index = Mathf.Min((int)(progress * frames.Length), frames.Length - 1);
            ShowFrame(index);
        }

        private void ShowFrame(int index)
        {
            Sprite frame = frames[index];
            targetRenderer.enabled = frame != null;
            if (frame != null) targetRenderer.sprite = frame;
        }

        private void Finish()
        {
            if (targetRenderer != null) targetRenderer.enabled = false;
            enabled = false;
            // Each spawn is disposable; this intentionally destroys the whole VFX instance.
            Destroy(gameObject);
        }
    }
}
