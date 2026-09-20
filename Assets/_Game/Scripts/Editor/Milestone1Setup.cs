using System;
using System.IO;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace Game.Player.Editor
{
    public static class Milestone1Setup
    {
        public const string ScenePath = "Assets/_Game/Scenes/Milestone1.unity";
        private const string PrefabPath = "Assets/_Game/Prefabs/Characters/Milestone1Player.prefab";
        private const string SpritePath = "Assets/_Game/Art/Characters/Milestone1Square.png";
        private const string InputPath = "Assets/_Game/Settings/Milestone1.inputactions";

        [MenuItem("Tools/ARPG/Setup Milestone 1")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before setting up Milestone 1.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // Existing scenes belong to the user after generation: never rebuild over edits.
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                EditorSceneManager.OpenScene(ScenePath);
                Debug.Log("Opened existing Milestone 1 scene; preserved all scene and prefab edits.");
                return;
            }

            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            if (actions == null || actions.FindAction("Player/Move") == null
                || actions.FindAction("Player/Pointer") == null)
                throw new InvalidOperationException("Import " + InputPath + " before running setup.");

            EnsureFolder("Assets/_Game/Art/Characters");
            EnsureFolder("Assets/_Game/Prefabs/Characters");
            EnsureFolder("Assets/_Game/Scenes");
            Sprite sprite = GetOrCreateSprite();
            GameObject prefab = GetOrCreatePlayer(sprite, actions);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            player.name = "Player";

            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.075f, 0.1f);
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
            CinemachineBrain brain = cameraObject.AddComponent<CinemachineBrain>();
            brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;

            var followCamera = new GameObject("Player Follow Camera").AddComponent<CinemachineCamera>();
            followCamera.transform.position = cameraObject.transform.position;
            followCamera.Follow = player.transform.Find("Follow Target");
            followCamera.Lens.OrthographicSize = camera.orthographicSize;
            var composer = followCamera.gameObject.AddComponent<CinemachinePositionComposer>();
            composer.CameraDistance = 10f;
            composer.Damping = new Vector3(0.15f, 0.15f, 0f);
            SetReference(player.GetComponent<PlayerMovement>(), "worldCamera", camera);
            PrefabUtility.RecordPrefabInstancePropertyModifications(player.GetComponent<PlayerMovement>());

            var grid = new GameObject("Movement Reference Grid");
            for (int offset = -10; offset <= 10; offset += 2)
            {
                AddSprite("Horizontal " + offset, grid.transform, sprite,
                    new Vector3(0f, offset, 0f), new Vector3(20f, 0.025f, 1f), -10,
                    new Color(0.13f, 0.18f, 0.22f));
                AddSprite("Vertical " + offset, grid.transform, sprite,
                    new Vector3(offset, 0f, 0f), new Vector3(0.025f, 20f, 1f), -10,
                    new Color(0.13f, 0.18f, 0.22f));
            }

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("Could not save " + ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = player;
            Debug.Log("Milestone 1 ready. Press Play; WASD moves and the mouse controls the gold facing marker.");
        }

        private static Sprite GetOrCreateSprite()
        {
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (existing != null)
                return existing;
            if (File.Exists(SpritePath))
                throw new InvalidOperationException("Existing placeholder is not imported as a Sprite: " + SpritePath);

            var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            var pixels = new Color32[16 * 16];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(255, 255, 255, 255);
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(SpritePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(SpritePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        }

        private static GameObject GetOrCreatePlayer(Sprite sprite, InputActionAsset actions)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null)
            {
                if (existing.GetComponent<PlayerMovement>() == null || existing.transform.Find("Follow Target") == null)
                    throw new InvalidOperationException("Existing player prefab is missing movement or Follow Target: " + PrefabPath);
                return existing;
            }

            var player = new GameObject("Player");
            try
            {
                var body = player.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Dynamic;
                body.gravityScale = 0f;
                body.constraints = RigidbodyConstraints2D.FreezeRotation;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                player.AddComponent<CircleCollider2D>().radius = 0.4f;
                var input = player.AddComponent<PlayerInputReader>();
                SetReference(input, "inputActions", actions);
                player.AddComponent<PlayerMovement>();
                AddSprite("Body", player.transform, sprite, Vector3.zero,
                    new Vector3(0.8f, 0.8f, 1f), 0, new Color(0.25f, 0.8f, 0.95f));
                Transform marker = AddSprite("Facing Marker", player.transform, sprite,
                    new Vector3(0f, -0.65f, 0f), new Vector3(0.18f, 0.18f, 1f), 1,
                    new Color(1f, 0.8f, 0.2f));
                SetReference(player.AddComponent<PlayerPlaceholderVisual>(), "facingMarker", marker);
                new GameObject("Follow Target").transform.SetParent(player.transform, false);
                return PrefabUtility.SaveAsPrefabAsset(player, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(player);
            }
        }

        private static Transform AddSprite(string name, Transform parent, Sprite sprite,
            Vector3 position, Vector3 scale, int order, Color color)
        {
            var visual = new GameObject(name, typeof(SpriteRenderer));
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = position;
            visual.transform.localScale = scale;
            var renderer = visual.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            return visual.transform;
        }

        private static void SetReference(UnityEngine.Object target, string property, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
