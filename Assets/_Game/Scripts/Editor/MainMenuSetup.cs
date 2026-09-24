using System;
using System.Linq;
using Game.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.EditorTools
{
    public static class MainMenuSetup
    {
        private const string MenuPath = "Assets/_Game/Scenes/MainMenu.unity";
        private const string GameplayPath = "Assets/_Game/Scenes/Milestone1.unity";

        [MenuItem("Tools/ARPG/Setup Main Menu")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before creating Main Menu.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GameplayPath) == null)
                throw new InvalidOperationException("Milestone1 scene was not found.");
            // Never silently replace a custom profile's scene list.
            if (BuildProfile.GetActiveBuildProfile() != null)
                throw new InvalidOperationException("Select the shared platform profile before running Setup Main Menu.");
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (font == null) throw new InvalidOperationException("TMP LiberationSans font is missing.");

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuPath) == null)
            {
                Scene previous = SceneManager.GetActiveScene();
                if (!Application.isBatchMode && string.IsNullOrEmpty(previous.path))
                    throw new InvalidOperationException("Save the untitled scene before creating Main Menu.");
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                    Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
                try
                {
                    SceneManager.SetActiveScene(scene);
                    var root = new GameObject("MainMenu");
                    var controller = root.AddComponent<MainMenuController>();
                    var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas),
                        typeof(CanvasScaler), typeof(GraphicRaycaster));
                    canvasObject.transform.SetParent(root.transform, false);
                    canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                    var scaler = canvasObject.GetComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    scaler.matchWidthOrHeight = 0.5f;
                    var background = Rect("Background", canvasObject.transform, Vector2.zero, Vector2.zero);
                    background.anchorMin = Vector2.zero;
                    background.anchorMax = Vector2.one;
                    var backgroundImage = background.gameObject.AddComponent<Image>();
                    backgroundImage.color = new Color(0.035f, 0.042f, 0.055f);
                    backgroundImage.raycastTarget = false;
                    Text("Title", canvasObject.transform, "CASTLEVEIL", font, 72,
                        new Vector2(0, 230), new Vector2(900, 120));
                    var panel = Rect("MenuPanel", canvasObject.transform, new Vector2(0, -40), new Vector2(420, 360));
                    Button first = Button(panel, "NewGameButton", "Novo Jogo", 135, font, controller.NewGame);
                    Button resume = Button(panel, "ContinueButton", "Continuar", 45, font, null);
                    resume.interactable = false;
                    Button(panel, "OptionsButton", "Opções", -45, font, controller.OpenOptions);
                    Button(panel, "ExitButton", "Sair", -135, font, controller.ExitGame);
                    var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                    events.transform.SetParent(root.transform, false);
                    events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
                    events.GetComponent<EventSystem>().firstSelectedGameObject = first.gameObject;
                    if (!EditorSceneManager.SaveScene(scene, MenuPath))
                        throw new InvalidOperationException("Could not save MainMenu.");
                }
                finally
                {
                    if (!Application.isBatchMode) EditorSceneManager.CloseScene(scene, true);
                    if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                }
            }
            var preserved = EditorBuildSettings.scenes.Where(s => s.path != MenuPath && s.path != GameplayPath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MenuPath, true),
                new EditorBuildSettingsScene(GameplayPath, true) }.Concat(preserved).ToArray();
            Debug.Log("MainMenu ready. Build order: MainMenu, Milestone1, then existing scenes. Existing menu is never overwritten.");
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static void Text(string name, Transform parent, string value, TMP_FontAsset font,
            float size, Vector2 position, Vector2 dimensions)
        {
            var text = Rect(name, parent, position, dimensions).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.86f, 0.80f, 0.65f);
            text.raycastTarget = false;
        }

        private static Button Button(Transform parent, string name, string label, float y,
            TMP_FontAsset font, UnityAction action)
        {
            var rect = Rect(name, parent, new Vector2(0, y), new Vector2(400, 72));
            var image = rect.gameObject.AddComponent<Image>();
            image.color = Color.white;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = new Color(0.13f, 0.14f, 0.17f);
            colors.highlightedColor = colors.selectedColor = new Color(0.25f, 0.24f, 0.21f);
            colors.pressedColor = new Color(0.09f, 0.10f, 0.12f);
            colors.disabledColor = new Color(0.065f, 0.07f, 0.08f);
            button.colors = colors;
            Text("Text", rect, label, font, 30, Vector2.zero, rect.sizeDelta);
            if (action != null) UnityEventTools.AddPersistentListener(button.onClick, action);
            return button;
        }
    }
}
