using UnityEngine;

namespace Game.Items
{
    [DisallowMultipleComponent]
    public sealed class WorldItemHighlight : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer sourceRenderer;
        [SerializeField] private Shader silhouetteShader;
        [SerializeField, Min(0f)] private float outlineOffset = 0.015f;
        [SerializeField] private Vector2 shadowOffset = new Vector2(0.015f, -0.015f);
        [SerializeField, Range(0f, 1f)] private float normalOutlineAlpha = 0.80f;
        [SerializeField, Range(0f, 1f)] private float hoverOutlineAlpha = 1f;
        [SerializeField] private Color outlineColor = Color.white;
        private readonly SpriteRenderer[] copies = new SpriteRenderer[9];
        private Material material;
        private bool hovered;

        private void Awake()
        {
            if (sourceRenderer == null || sourceRenderer.transform == transform
                || sourceRenderer.transform.parent != transform.parent || silhouetteShader == null)
            {
                Debug.LogError("WorldItemHighlight needs a sibling source SpriteRenderer and the Game/WorldItemSilhouette shader.", this);
                enabled = false;
                return;
            }
            material = new Material(silhouetteShader);
            for (int i = 0; i < copies.Length; i++)
            {
                var child = new GameObject(i == 8 ? "OutlineShadow" : "Outline" + i);
                child.layer = gameObject.layer;
                child.transform.SetParent(transform, false);
                copies[i] = child.AddComponent<SpriteRenderer>();
                copies[i].sharedMaterial = material;
            }
        }

        public void SetHovered(bool value) => hovered = value;
        public void SetColor(Color value) => outlineColor = value;

        private void LateUpdate()
        {
            if (sourceRenderer == null) { Hide(); return; }
            Transform source = sourceRenderer.transform;
            transform.localPosition = source.localPosition;
            transform.localRotation = source.localRotation;
            transform.localScale = source.localScale;
            int index = 0;
            for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                {
                    if (x == 0 && y == 0) continue;
                    UpdateCopy(index++, new Vector2(x, y) * outlineOffset,
                        new Color(outlineColor.r, outlineColor.g, outlineColor.b,
                            outlineColor.a * (hovered ? hoverOutlineAlpha : normalOutlineAlpha)), -1);
                }
            UpdateCopy(8, shadowOffset, Color.black, -2);
        }

        private void UpdateCopy(int index, Vector2 offset, Color color, int orderOffset)
        {
            SpriteRenderer copy = copies[index];
            copy.transform.localPosition = offset;
            copy.sprite = sourceRenderer.sprite;
            copy.flipX = sourceRenderer.flipX;
            copy.flipY = sourceRenderer.flipY;
            copy.sortingLayerID = sourceRenderer.sortingLayerID;
            copy.sortingOrder = sourceRenderer.sortingOrder + orderOffset;
            copy.color = color;
            copy.enabled = sourceRenderer.enabled && sourceRenderer.gameObject.activeInHierarchy;
        }

        private void Hide()
        {
            foreach (SpriteRenderer copy in copies) if (copy != null) copy.enabled = false;
        }
        private void OnDisable() { hovered = false; Hide(); }
        private void OnDestroy()
        {
            foreach (SpriteRenderer copy in copies) if (copy != null) Destroy(copy.gameObject);
            if (material != null) Destroy(material);
        }
    }
}
