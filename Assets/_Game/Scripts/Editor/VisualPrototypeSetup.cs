using System;
using Game.Combat;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Game.Player.Editor
{
    public static class VisualPrototypeSetup
    {
        private const string TileFolder = "Assets/_Game/Art/Environment/Tiles";
        private const string ControllerPath = "Assets/_Game/Art/Characters/Animations/PlayerPrototype.controller";
        private const string MaterialPath = "Assets/_Game/Materials/VisualPrototypeLit.mat";
        private static readonly string[] Layers = { "Ground", "Environment", "Characters", "Foreground", "VFX" };

        [MenuItem("Tools/ARPG/Setup Visual Prototype")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before setting up the visual prototype.");
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != Milestone1Setup.ScenePath)
                throw new InvalidOperationException("Open the existing Milestone1 scene first.");
            PlayerMovement player = FindSingle<PlayerMovement>(scene);
            TargetDummy dummy = FindSingle<TargetDummy>(scene);
            if (player == null || dummy == null || player.GetComponent<PlayerCombat>() == null
                || player.GetComponent<PlayerDodge>() == null)
                throw new InvalidOperationException("Run the Milestone 1, 3 and 4 setup commands first.");
            Sprite square = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Characters/Milestone1Square.png");
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (square == null || shader == null)
                throw new InvalidOperationException("The Milestone 1 placeholder sprite and URP 2D Sprite-Lit shader are required.");

            Transform arena = FindRoot(scene, "Visual Prototype Arena");
            Vector3 center = new Vector3(Mathf.Floor(player.transform.position.x), Mathf.Floor(player.transform.position.y), 0f);
            Vector3 delta = dummy.transform.position - center;
            if (arena == null && (Mathf.Abs(delta.x) > 5f || Mathf.Abs(delta.y) > 3f))
                throw new InvalidOperationException("Place TargetDummy within 5 horizontal and 3 vertical units of Player before creating the small arena.");

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Setup Visual Prototype");
            try
            {
                EnsureSortingLayers();
                EnsureFolder(TileFolder);
                EnsureFolder("Assets/_Game/Art/Characters/Animations");
                EnsureFolder("Assets/_Game/Materials");
                Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
                if (material == null)
                {
                    RequireUnusedAssetPath(MaterialPath);
                    material = new Material(shader) { name = "VisualPrototypeLit" };
                    AssetDatabase.CreateAsset(material, MaterialPath);
                }
                ConfigurePlayer(player, square, material);
                foreach (SpriteRenderer renderer in dummy.GetComponentsInChildren<SpriteRenderer>(true))
                    ConfigureRenderer(renderer, "Characters", square, material);

                if (arena == null)
                {
                    arena = CreateChild("Visual Prototype Arena", null);
                    arena.position = center;
                }
                Transform gridTransform = Child(arena, "Grid");
                Grid grid = Ensure<Grid>(gridTransform.gameObject);
                Tile ground = GetTile("PrototypeGround", square, new Color(0.22f, 0.29f, 0.20f), false);
                Tile alternate = GetTile("PrototypeGroundAlternate", square, new Color(0.25f, 0.32f, 0.22f), false);
                Tile decoration = GetTile("PrototypeDecoration", square, new Color(0.34f, 0.38f, 0.25f), false);
                Tile wall = GetTile("PrototypeWall", square, new Color(0.43f, 0.44f, 0.47f), true);
                Tile obstacle = GetTile("PrototypeObstacle", square, new Color(0.48f, 0.33f, 0.20f), true);
                Tile foreground = GetTile("PrototypeForeground", square, new Color(0.22f, 0.40f, 0.26f), false);

                Tilemap groundMap = Map(gridTransform, "Ground Tilemap", "Ground", material, out bool newGround);
                Tilemap decorationMap = Map(gridTransform, "Decoration Tilemap", "Environment", material, out bool newDecoration);
                Tilemap collisionMap = Map(gridTransform, "Collision Tilemap", "Environment", material, out bool newCollision);
                Tilemap foregroundMap = Map(gridTransform, "Foreground Tilemap", "Foreground", material, out bool newForeground);

                // Collision configuration must exist even when recovering
                // from a partially completed previous setup.
                Ensure<TilemapCollider2D>(collisionMap.gameObject);

                // No Rigidbody2D is required here.
                // Without a Rigidbody2D the TilemapCollider2D behaves as static geometry.

                // Seed only newly created maps. Repeated setup preserves hand-painted tiles.
                for (int x = -8; x < 8; x++)
                    for (int y = -6; y < 6; y++)
                    {
                        var cell = new Vector3Int(x, y, 0);
                        if (newGround) groundMap.SetTile(cell, ((x + y) & 1) == 0 ? ground : alternate);
                        if (newCollision && (x == -8 || x == 7 || y == -6 || y == 5))
                            collisionMap.SetTile(cell, wall);
                    }
                if (newDecoration)
                    foreach (Vector3Int cell in new[] { new Vector3Int(-5, 2, 0), new Vector3Int(-3, 3, 0), new Vector3Int(4, -3, 0) })
                        decorationMap.SetTile(cell, decoration);
                if (newCollision)
                    foreach (Vector3Int cell in new[] { new Vector3Int(-4, -2, 0), new Vector3Int(-4, -1, 0), new Vector3Int(3, 2, 0) })
                    {
                        Vector3 world = grid.GetCellCenterWorld(cell);
                        if (Vector2.Distance(world, player.transform.position) > 1.5f
                            && Vector2.Distance(world, dummy.transform.position) > 1.5f)
                            collisionMap.SetTile(cell, obstacle);
                    }
                if (newForeground)
                {
                    foregroundMap.SetTile(new Vector3Int(-7, 4, 0), foreground);
                    foregroundMap.SetTile(new Vector3Int(6, 4, 0), foreground);
                }
                ConfigureLight(scene, arena);

                Transform oldGrid = FindRoot(scene, "Movement Reference Grid");
                if (oldGrid != null && oldGrid.gameObject.activeSelf)
                {
                    Undo.RecordObject(oldGrid.gameObject, "Hide reference grid");
                    oldGrid.gameObject.SetActive(false);
                }
                EditorSceneManager.MarkSceneDirty(scene);
                AssetDatabase.SaveAssets();
                Undo.CollapseUndoOperations(group);
                Selection.activeGameObject = player.gameObject;
                Debug.Log("Visual prototype ready. Save the scene (Ctrl+S). Existing gameplay, positions, HUD and camera were preserved. Re-running preserves painted tiles, sprite choices and Animator assets. Generated assets and project sorting layers are shared project data.");
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                throw;
            }
        }

        private static void ConfigurePlayer(PlayerMovement player, Sprite placeholder, Material material)
        {
            // Unity disallows reparenting original children inside a connected prefab instance.
            if (PrefabUtility.IsPartOfPrefabInstance(player.gameObject)
                && (player.transform.Find("Body") != null || player.transform.Find("Facing Marker") != null))
            {
                GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(player.gameObject);
                if (prefabRoot != player.gameObject)
                    throw new InvalidOperationException("Player is nested inside another prefab. Unpack its outer scene instance before visual setup.");
                PrefabUtility.UnpackPrefabInstance(prefabRoot, PrefabUnpackMode.OutermostRoot, InteractionMode.UserAction);
            }
            Transform visual = Child(player.transform, "VisualRoot");
            Transform sprite = visual.Find("CharacterSprite");
            if (sprite == null)
            {
                sprite = player.transform.Find("Body");
                if (sprite != null)
                {
                    if (sprite.GetComponent<Collider2D>() != null || sprite.GetComponent<Rigidbody2D>() != null)
                        throw new InvalidOperationException("Player/Body contains physics components. Separate these before migrating visuals.");
                    Undo.SetTransformParent(sprite, visual, "Move player sprite");
                    Undo.RecordObject(sprite.gameObject, "Name character sprite");
                    sprite.name = "CharacterSprite";
                }
                else
                {
                    sprite = CreateChild("CharacterSprite", visual);
                    var renderer = Ensure<SpriteRenderer>(sprite.gameObject);
                    renderer.sprite = placeholder;
                    renderer.color = new Color(0.25f, 0.8f, 0.95f);
                    sprite.localScale = new Vector3(0.8f, 0.8f, 1f);
                }
            }
            ConfigureRenderer(Ensure<SpriteRenderer>(sprite.gameObject), "Characters", placeholder, material);

            Transform marker = visual.Find("FacingMarker") ?? visual.Find("Facing Marker") ?? player.transform.Find("Facing Marker");

            if (marker != null && marker.name != "FacingMarker")
            {
                Undo.RecordObject(marker.gameObject, "Name facing marker");
                marker.name = "FacingMarker";
            }

            if (marker != null && marker.parent != visual)
            {
                Undo.SetTransformParent(marker, visual, "Move facing marker");
            }

            if (marker == null)
            {
                marker = player.transform.Find("Facing Marker");
                if (marker != null)
                {
                    Undo.SetTransformParent(marker, visual, "Move facing marker");
                    Undo.RecordObject(marker.gameObject, "Name facing marker");
                    marker.name = "FacingMarker";
                }
            }
            PlayerPlaceholderVisual oldPresentation = player.GetComponent<PlayerPlaceholderVisual>();
            PlayerPlaceholderVisual presentation = visual.GetComponent<PlayerPlaceholderVisual>();
            if (presentation == null && (oldPresentation != null || marker != null))
            {
                presentation = Undo.AddComponent<PlayerPlaceholderVisual>(visual.gameObject);
                if (oldPresentation != null) EditorUtility.CopySerialized(oldPresentation, presentation);
            }
            if (presentation != null)
            {
                SetReference(presentation, "movement", player);
                if (marker != null) SetReference(presentation, "facingMarker", marker);
            }
            if (oldPresentation != null) Undo.DestroyObjectImmediate(oldPresentation);
            if (marker != null)
                foreach (SpriteRenderer renderer in marker.GetComponentsInChildren<SpriteRenderer>(true))
                    ConfigureRenderer(renderer, "Characters", placeholder, material);

            SortingGroup sorting = Ensure<SortingGroup>(visual.gameObject);
            Undo.RecordObject(sorting, "Set character sorting");
            sorting.sortingLayerName = "Characters";
            Animator animator = Ensure<Animator>(visual.gameObject);
            Undo.RecordObject(animator, "Configure visual Animator");
            animator.applyRootMotion = false;
            if (animator.runtimeAnimatorController == null) animator.runtimeAnimatorController = GetController();
            PlayerAnimationController controller = Ensure<PlayerAnimationController>(visual.gameObject);
            SetReference(controller, "movement", player);
            SetReference(controller, "combat", player.GetComponent<PlayerCombat>());
            SetReference(controller, "dodge", player.GetComponent<PlayerDodge>());
            SetReference(controller, "health", player.GetComponent<Health>());
            SetReference(controller, "body", player.GetComponent<Rigidbody2D>());
            SetReference(controller, "animator", animator);
        }

        private static AnimatorController GetController()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller != null) return controller;
            RequireUnusedAssetPath(ControllerPath);
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("VisualState", AnimatorControllerParameterType.Int);
            controller.AddParameter("FacingDirection", AnimatorControllerParameterType.Int);
            foreach (string name in new[] { "IsMoving", "IsAttacking", "IsDodging" })
                controller.AddParameter(name, AnimatorControllerParameterType.Bool);
            foreach (string name in new[] { "MoveX", "MoveY", "FacingX", "FacingY" })
                controller.AddParameter(name, AnimatorControllerParameterType.Float);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            foreach (PlayerVisualState state in Enum.GetValues(typeof(PlayerVisualState)))
            {
                AnimatorState node = machine.AddState(state.ToString());
                node.writeDefaultValues = false;
                if (state == PlayerVisualState.Idle) machine.defaultState = node;
                AnimatorStateTransition transition = machine.AddAnyStateTransition(node);
                transition.hasExitTime = false;
                transition.duration = 0f;
                transition.canTransitionToSelf = false;
                transition.AddCondition(AnimatorConditionMode.Equals, (int)state, "VisualState");
            }
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void EnsureSortingLayers()
        {
            UnityEngine.Object manager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
            var serialized = new SerializedObject(manager);
            SerializedProperty layers = serialized.FindProperty("m_SortingLayers");
            foreach (string name in Layers)
            {
                bool exists = false;
                for (int i = 0; i < layers.arraySize; i++)
                    exists |= layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == name;
                if (exists) continue;
                int id;
                bool duplicate;
                do
                {
                    id = Guid.NewGuid().GetHashCode();
                    duplicate = id == 0;
                    for (int i = 0; i < layers.arraySize; i++)
                        duplicate |= layers.GetArrayElementAtIndex(i).FindPropertyRelative("uniqueID").intValue == id;
                } while (duplicate);
                int index = layers.arraySize;
                layers.InsertArrayElementAtIndex(index);
                SerializedProperty entry = layers.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("name").stringValue = name;
                entry.FindPropertyRelative("uniqueID").intValue = id;
                entry.FindPropertyRelative("locked").boolValue = false;
            }
            serialized.ApplyModifiedProperties();
        }

        private static void ConfigureLight(Scene scene, Transform arena)
        {
            Light2D light = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Light2D candidate in root.GetComponentsInChildren<Light2D>(true))
                    if (candidate.isActiveAndEnabled && candidate.lightType == Light2D.LightType.Global)
                    {
                        light = candidate;
                        break;
                    }
            if (light == null)
            {
                light = Ensure<Light2D>(Child(arena, "Global Light 2D").gameObject);
                light.lightType = Light2D.LightType.Global;
                light.intensity = 1f;
                light.color = Color.white;
            }
            // Newly added sorting layers must also be included in the global light's targets.
            var serialized = new SerializedObject(light);
            SerializedProperty targets = serialized.FindProperty("m_ApplyToSortingLayers");
            if (targets == null) throw new InvalidOperationException("Cannot configure Light2D sorting layers in this URP version.");
            foreach (string name in Layers)
            {
                int id = SortingLayer.NameToID(name);
                bool found = false;
                for (int i = 0; i < targets.arraySize; i++) found |= targets.GetArrayElementAtIndex(i).intValue == id;
                if (found) continue;
                int index = targets.arraySize;
                targets.InsertArrayElementAtIndex(index);
                targets.GetArrayElementAtIndex(index).intValue = id;
            }
            serialized.ApplyModifiedProperties();
        }

        private static Tile GetTile(string name, Sprite sprite, Color color, bool collision)
        {
            string path = TileFolder + "/" + name + ".asset";
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile != null) return tile;
            RequireUnusedAssetPath(path);
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = name;
            tile.sprite = sprite;
            tile.color = color;
            tile.colliderType = collision ? Tile.ColliderType.Grid : Tile.ColliderType.None;
            AssetDatabase.CreateAsset(tile, path);
            return tile;
        }

        private static Tilemap Map(Transform parent,
                                    string name,
                                    string layer,
                                    Material material,
                                    out bool created)
        {
            Transform existing = parent.Find(name);
            GameObject target;

            if (existing == null)
            {
                created = true;

                target = new GameObject(
                    name,
                    typeof(Tilemap),
                    typeof(TilemapRenderer));

                Undo.RegisterCreatedObjectUndo(target, $"Create {name}");

                target.transform.SetParent(parent, false);
            }
            else
            {
                created = false;
                target = existing.gameObject;
            }

            Tilemap map = Ensure<Tilemap>(target);
            TilemapRenderer renderer = Ensure<TilemapRenderer>(target);

            if (renderer == null)
            {
                Debug.LogError(
                    $"Could not create TilemapRenderer on '{target.name}'.",
                    target);

                return map;
            }

            Undo.RecordObject(renderer, $"Configure {name}");

            renderer.sortingLayerName = layer;
            renderer.sharedMaterial = material;
            renderer.mode = TilemapRenderer.Mode.Individual;

            EditorUtility.SetDirty(renderer);

            return map;
        }

        private static void ConfigureRenderer(SpriteRenderer renderer, string layer, Sprite placeholder, Material material)
        {
            Undo.RecordObject(renderer, "Configure sprite rendering");
            renderer.sortingLayerName = layer;
            renderer.spriteSortPoint = SpriteSortPoint.Pivot;
            Material defaultMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            if (renderer.sprite == placeholder && (renderer.sharedMaterial == null || renderer.sharedMaterial == defaultMaterial))
                renderer.sharedMaterial = material;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }

        private static void SetReference(UnityEngine.Object target, string name, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(name).objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
        }

        private static T Ensure<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();

            if (component != null)
                return component;

            Undo.AddComponent<T>(target);

            component = target.GetComponent<T>();

            if (component == null)
            {
                throw new InvalidOperationException(
                    $"Failed to add required component {typeof(T).Name} to '{target.name}'.");
            }

            return component;
        }
        private static Transform Child(Transform parent, string name) => parent.Find(name) ?? CreateChild(name, parent);

        private static Transform CreateChild(string name, Transform parent)
        {
            var child = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(child, "Create visual prototype object");
            if (parent != null) Undo.SetTransformParent(child.transform, parent, "Parent visual prototype object");
            child.transform.localPosition = Vector3.zero;
            return child.transform;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }

        private static void RequireUnusedAssetPath(string path)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                throw new InvalidOperationException("An asset of a different type already exists at " + path + ". No replacement was made.");
        }

        private static Transform FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects()) if (root.name == name) return root.transform;
            return null;
        }

        private static T FindSingle<T>(Scene scene) where T : Component
        {
            T result = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (T candidate in root.GetComponentsInChildren<T>(true))
                {
                    if (result != null) throw new InvalidOperationException("Expected one " + typeof(T).Name + " in this scene.");
                    result = candidate;
                }
            return result;
        }
    }
}
