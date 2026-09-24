using UnityEngine;

namespace Game.Items
{
    [DisallowMultipleComponent]
    public sealed class WorldItemSparkles : MonoBehaviour
    {
        [SerializeField] private Transform followVisual;
        [SerializeField] private Sprite[] sparkleSprites = System.Array.Empty<Sprite>();
        [SerializeField] private SpriteRenderer[] sparkleRenderers = System.Array.Empty<SpriteRenderer>();
        [SerializeField, Min(0.01f)] private float cycleDuration = 1.2f;
        [SerializeField, Range(0f, 1f)] private float minAlpha = 0f;
        [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.85f;
        [SerializeField, Min(0f)] private float minScale = 0.8f;
        [SerializeField, Min(0f)] private float maxScale = 1f;
        [SerializeField] private Color sparkleColor = Color.white;

        private SpriteRenderer[] renderers;
        private Vector3[] originalScales;
        private Vector3 followOffset;
        private double startedAt;

        private void Awake()
        {
            if (followVisual == transform || (followVisual != null && followVisual.parent != transform.parent))
            {
                Debug.LogError("WorldItemSparkles Follow Visual must be a sibling of Sparkles.", this);
                followVisual = null;
            }
            if (followVisual != null) followOffset = transform.localPosition - followVisual.localPosition;
            renderers = sparkleRenderers == null ? System.Array.Empty<SpriteRenderer>()
                : (SpriteRenderer[])sparkleRenderers.Clone();
            originalScales = new Vector3[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (renderer == null) continue;
                if (renderer.transform == transform || !renderer.transform.IsChildOf(transform))
                {
                    Debug.LogError("Assign only dedicated child SpriteRenderers to WorldItemSparkles.", this);
                    renderers[i] = null;
                    continue;
                }
                // Ignore duplicate references so a renderer has just one pulse and base scale.
                for (int j = 0; j < i; j++)
                    if (renderers[j] == renderer) { renderers[i] = null; break; }
                if (renderers[i] == null) continue;
                originalScales[i] = renderer.transform.localScale;
                if (sparkleSprites != null && sparkleSprites.Length > 0)
                {
                    Sprite sprite = sparkleSprites[i % sparkleSprites.Length];
                    if (sprite != null) renderer.sprite = sprite;
                }
            }
        }

        private void OnEnable() => startedAt = Time.timeAsDouble;
        public void SetColor(Color color) => sparkleColor = color;

        private void LateUpdate()
        {
            if (followVisual != null) transform.localPosition = followVisual.localPosition + followOffset;
            float duration = cycleDuration > 0f && !float.IsInfinity(cycleDuration) ? cycleDuration : 1.2f;
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (renderer == null) continue;
                double phase = ((Time.timeAsDouble - startedAt) / (duration * (1f + 0.13f * i))
                    + i * 0.37d) % 1d;
                float pulse = Mathf.Max(0f, Mathf.Sin((float)phase * Mathf.PI * 2f));
                float alpha = Mathf.Lerp(Mathf.Clamp01(minAlpha), Mathf.Clamp01(maxAlpha), pulse);
                Color color = sparkleColor;
                color.a *= alpha;
                renderer.color = color;
                renderer.enabled = renderer.sprite != null && color.a > 0f;
                renderer.transform.localScale = originalScales[i]
                    * Mathf.Lerp(Mathf.Max(0f, minScale), Mathf.Max(0f, maxScale), pulse);
            }
        }

        private void OnDisable()
        {
            if (renderers == null) return;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                renderers[i].enabled = false;
                renderers[i].transform.localScale = originalScales[i];
            }
        }
    }
}
