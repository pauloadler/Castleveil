using System;
using Game.Combat;
using Game.Stats;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Game.Player.Editor
{
    public static class Milestone3Setup
    {
        [MenuItem("Tools/ARPG/Setup Milestone 3")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Exit Play Mode before setting up Milestone 3.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != Milestone1Setup.ScenePath)
            {
                Debug.LogError("Open the Milestone1 scene first, using Tools > ARPG > Setup Milestone 1 if needed.");
                return;
            }

            PlayerMovement movement = FindSingleInScene<PlayerMovement>(scene);
            TargetDummy dummy = FindSingleInScene<TargetDummy>(scene);
            if (movement == null || movement.GetComponent<Rigidbody2D>() == null
                || movement.GetComponent<Collider2D>() == null)
                throw new InvalidOperationException("The scene needs the Milestone 1 player with its Rigidbody2D and Collider2D.");

            PlayerInputReader reader = movement.GetComponent<PlayerInputReader>();
            if (reader == null)
                throw new InvalidOperationException("The player needs its Milestone 1 PlayerInputReader.");
            var readerData = new SerializedObject(reader);
            var actions = readerData.FindProperty("inputActions").objectReferenceValue as InputActionAsset;
            if (actions == null || actions.FindAction("Player/BasicAttack") == null)
                throw new InvalidOperationException("Import the updated Milestone1.inputactions asset with Player/BasicAttack, bound to <Mouse>/leftButton.");

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Characters/Milestone1Square.png");
            if (dummy == null && sprite == null)
                throw new InvalidOperationException("The Milestone 1 placeholder sprite is missing. Run Setup Milestone 1 first.");

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Setup Milestone 3");
            try
            {
                GameObject player = movement.gameObject;
                EnsureComponent<CharacterStats>(player);
                Health playerHealth = EnsureComponent<Health>(player);
                AssignIfEmpty(EnsureComponent<Hurtbox>(player), "damageReceiver", playerHealth);
                PlayerCombat combat = EnsureComponent<PlayerCombat>(player);
                var combatData = new SerializedObject(combat);
                var hitbox = combatData.FindProperty("attackHitbox").objectReferenceValue as Hitbox;
                if (hitbox == null)
                {
                    Transform existing = player.transform.Find("Attack Hitbox");
                    if (existing != null)
                    {
                        hitbox = existing.GetComponent<Hitbox>();
                        if (hitbox == null)
                            throw new InvalidOperationException("An object named Attack Hitbox already exists without a Hitbox component. Rename it before setup.");
                    }
                    else
                    {
                        var hitboxObject = new GameObject("Attack Hitbox");
                        Undo.RegisterCreatedObjectUndo(hitboxObject, "Create attack hitbox");
                        Undo.SetTransformParent(hitboxObject.transform, player.transform, "Parent attack hitbox");
                        hitboxObject.transform.localPosition = Vector3.right * 0.8f;
                        hitboxObject.transform.localRotation = Quaternion.identity;
                        hitboxObject.transform.localScale = Vector3.one;
                        var shape = Undo.AddComponent<BoxCollider2D>(hitboxObject);
                        shape.size = new Vector2(1f, 0.8f);
                        shape.isTrigger = true;
                        shape.enabled = false;
                        hitbox = Undo.AddComponent<Hitbox>(hitboxObject);
                    }
                    AssignIfEmpty(combat, "attackHitbox", hitbox);
                }
                if (hitbox.transform == player.transform || !hitbox.transform.IsChildOf(player.transform))
                    throw new InvalidOperationException("Attack Hitbox must be a child of the player, not the player's body collider.");

                if (dummy == null)
                {
                    var dummyObject = new GameObject("TargetDummy");
                    Undo.RegisterCreatedObjectUndo(dummyObject, "Create target dummy");
                    SceneManager.MoveGameObjectToScene(dummyObject, scene);
                    dummyObject.transform.position = player.transform.position + Vector3.right * 1.4f;
                    Undo.AddComponent<Rigidbody2D>(dummyObject).bodyType = RigidbodyType2D.Static;
                    Undo.AddComponent<BoxCollider2D>(dummyObject).size = new Vector2(0.8f, 0.8f);
                    EnsureComponent<CharacterStats>(dummyObject);
                    Health dummyHealth = EnsureComponent<Health>(dummyObject);
                    AssignIfEmpty(EnsureComponent<Hurtbox>(dummyObject), "damageReceiver", dummyHealth);
                    dummy = Undo.AddComponent<TargetDummy>(dummyObject);

                    var visual = new GameObject("Body");
                    Undo.RegisterCreatedObjectUndo(visual, "Create dummy visual");
                    Undo.SetTransformParent(visual.transform, dummyObject.transform, "Parent dummy visual");
                    visual.transform.localPosition = Vector3.zero;
                    visual.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
                    var renderer = Undo.AddComponent<SpriteRenderer>(visual);
                    renderer.sprite = sprite;
                    renderer.color = new Color(1f, 0.45f, 0.2f);
                    renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
                }

                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(undoGroup);
                Selection.activeGameObject = player;
                Debug.Log("Milestone 3 configured. Save the scene (Ctrl+S). Aim at the orange TargetDummy and click LMB. Tune CharacterStats.AttackDamage and PlayerCombat timings in the Inspector. Existing values and prefab assets were preserved.");
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                throw;
            }
        }

        private static T FindSingleInScene<T>(Scene scene) where T : Component
        {
            T result = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (T component in root.GetComponentsInChildren<T>(true))
                {
                    if (result != null)
                        throw new InvalidOperationException("Expected at most one " + typeof(T).Name + " in the prototype scene; no changes applied.");
                    result = component;
                }
            }
            return result;
        }

        private static T EnsureComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(target);
        }

        private static void AssignIfEmpty(UnityEngine.Object target, string property, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty reference = serialized.FindProperty(property);
            if (reference.objectReferenceValue != null)
                return;
            reference.objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
        }
    }
}
