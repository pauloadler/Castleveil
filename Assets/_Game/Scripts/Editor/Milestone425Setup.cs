using System;
using Game.Combat;
using Game.Stats;
using Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Player.Editor
{
    public static class Milestone425Setup
    {
        [MenuItem("Tools/ARPG/Setup Milestone 4.25")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Exit Play Mode before setting up the HUD.");
                return;
            }
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != Milestone1Setup.ScenePath)
                throw new InvalidOperationException("Open the existing Milestone1 scene first.");

            PlayerMovement player = FindSingle<PlayerMovement>(scene);
            PlayerHud hud = FindSingle<PlayerHud>(scene);
            Health health = player != null ? player.GetComponent<Health>() : null;
            Stamina stamina = player != null ? player.GetComponent<Stamina>() : null;
            if (health == null || stamina == null)
                throw new InvalidOperationException("Run Setup Milestone 4 first; the player needs Health and Stamina.");

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                throw new InvalidOperationException("Unity's built-in LegacyRuntime font is unavailable.");
            if (hud == null)
                foreach (GameObject root in scene.GetRootGameObjects())
                    if (root.name == "Prototype HUD")
                        throw new InvalidOperationException("Prototype HUD already exists without PlayerHud. Rename it before setup.");

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Setup Milestone 4.25");
            try
            {
                if (hud == null)
                {
                    var root = new GameObject("Prototype HUD", typeof(RectTransform));
                    Undo.RegisterCreatedObjectUndo(root, "Create HUD");
                    var canvas = Undo.AddComponent<Canvas>(root);
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 10;
                    var scaler = Undo.AddComponent<CanvasScaler>(root);
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1280f, 720f);
                    scaler.matchWidthOrHeight = 0.5f;
                    hud = Undo.AddComponent<PlayerHud>(root);

                    RectTransform panel = MakeRect("Vitals", root.transform);
                    panel.anchorMin = panel.anchorMax = Vector2.zero;
                    panel.pivot = Vector2.zero;
                    panel.anchoredPosition = new Vector2(24f, 24f);
                    panel.sizeDelta = new Vector2(300f, 84f);
                    CreateBar(panel, "Health", 46f, new Color(0.75f, 0.16f, 0.16f), font,
                        "HP", health.MaxHealth > 0f ? health.MaxHealth : player.GetComponent<CharacterStats>().MaxHealth,
                        out RectTransform hpFill, out Text hpLabel);
                    CreateBar(panel, "Stamina", 0f, new Color(0.12f, 0.55f, 0.32f), font,
                        "Stamina", player.GetComponent<CharacterStats>().MaxStamina,
                        out RectTransform staminaFill, out Text staminaLabel);
                    SetReference(hud, "healthFill", hpFill);
                    SetReference(hud, "healthLabel", hpLabel);
                    SetReference(hud, "staminaFill", staminaFill);
                    SetReference(hud, "staminaLabel", staminaLabel);
                }
                // Re-running rebinds the player without duplicating or rebuilding the HUD.
                SetReference(hud, "health", health);
                SetReference(hud, "stamina", stamina);
                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(group);
                Selection.activeGameObject = hud.gameObject;
                Debug.Log("Prototype HUD ready. Save the scene (Ctrl+S), then press Play. Health appears above Stamina at the bottom-left.");
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                throw;
            }
        }

        private static void CreateBar(Transform parent, string name, float y, Color color, Font font,
            string title, float maximum, out RectTransform fill, out Text label)
        {
            RectTransform background = MakeRect(name, parent);
            background.anchorMin = background.anchorMax = Vector2.zero;
            background.pivot = Vector2.zero;
            background.anchoredPosition = new Vector2(0f, y);
            background.sizeDelta = new Vector2(300f, 38f);
            Image backgroundImage = Undo.AddComponent<Image>(background.gameObject);
            backgroundImage.color = new Color(0.035f, 0.035f, 0.045f, 0.95f);
            backgroundImage.raycastTarget = false;

            fill = MakeRect("Fill", background);
            Stretch(fill);
            fill.anchorMax = new Vector2(maximum > 0f ? 1f : 0f, 1f);
            Image fillImage = Undo.AddComponent<Image>(fill.gameObject);
            fillImage.color = color;
            fillImage.raycastTarget = false;

            RectTransform textRect = MakeRect("Value", background);
            Stretch(textRect);
            label = Undo.AddComponent<Text>(textRect.gameObject);
            label.font = font;
            label.fontSize = 20;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            label.text = $"{title}  {maximum:0.#} / {maximum:0.#}";
        }

        private static RectTransform MakeRect(string name, Transform parent)
        {
            var child = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(child, "Create HUD element");
            Undo.SetTransformParent(child.transform, parent, "Parent HUD element");
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;
            return (RectTransform)child.transform;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void SetReference(UnityEngine.Object target, string name, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(name).objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
        }

        private static T FindSingle<T>(Scene scene) where T : Component
        {
            T result = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (T candidate in root.GetComponentsInChildren<T>(true))
                {
                    if (result != null)
                        throw new InvalidOperationException("Expected at most one " + typeof(T).Name + "; no changes applied.");
                    result = candidate;
                }
            return result;
        }
    }
}
