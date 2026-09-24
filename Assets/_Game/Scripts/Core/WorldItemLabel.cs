using TMPro;
using UnityEngine;

namespace Game.Items
{
    [DisallowMultipleComponent]
    public sealed class WorldItemLabel : MonoBehaviour
    {
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private string itemName = "Espada Enferrujada";
        [SerializeField] private Color textColor = Color.white;

        private void Awake()
        {
            if (labelText == null) labelText = GetComponentInChildren<TMP_Text>(true);
            if (labelText == null)
            {
                Debug.LogError("WorldItemLabel needs its TextMeshPro text assigned.", this);
                enabled = false;
                return;
            }
            labelText.text = itemName;
            labelText.color = textColor;
            labelText.alignment = TextAlignmentOptions.Center;
            // WorldItem performs rectangle hit testing with the existing Input System reader.
            labelText.raycastTarget = false;
        }

        public void SetText(string value)
        {
            itemName = value ?? string.Empty;
            if (labelText != null) labelText.text = itemName;
        }

        public void SetColor(Color value)
        {
            textColor = value;
            if (labelText != null) labelText.color = value;
        }

        public bool ContainsPointer(Vector2 screenPosition, Camera worldCamera)
        {
            if (!isActiveAndEnabled || labelText == null || !labelText.isActiveAndEnabled) return false;
            Canvas canvas = labelText.canvas;
            if (canvas == null || !canvas.isActiveAndEnabled) return false;
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null
                : canvas.worldCamera != null ? canvas.worldCamera : worldCamera;
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && camera == null) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(labelText.rectTransform, screenPosition, camera);
        }
    }
}
