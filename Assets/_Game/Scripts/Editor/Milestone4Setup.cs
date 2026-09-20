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
    public static class Milestone4Setup
    {
        [MenuItem("Tools/ARPG/Setup Milestone 4")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Exit Play Mode before setting up Milestone 4.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != Milestone1Setup.ScenePath)
            {
                Debug.LogError("Open the existing Milestone1 scene before running Setup Milestone 4.");
                return;
            }

            PlayerMovement movement = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (PlayerMovement candidate in root.GetComponentsInChildren<PlayerMovement>(true))
                {
                    if (movement != null)
                        throw new InvalidOperationException("Expected one player in the prototype scene; no changes applied.");
                    movement = candidate;
                }
            }
            if (movement == null || movement.GetComponent<Rigidbody2D>() == null)
                throw new InvalidOperationException("The scene needs the Milestone 1 player and its Rigidbody2D.");

            PlayerInputReader reader = movement.GetComponent<PlayerInputReader>();
            if (reader == null)
                throw new InvalidOperationException("The player needs PlayerInputReader.");
            var readerData = new SerializedObject(reader);
            var actions = readerData.FindProperty("inputActions").objectReferenceValue as InputActionAsset;
            if (actions == null || actions.FindAction("Player/Dodge") == null)
                throw new InvalidOperationException("Import the updated Milestone1.inputactions asset with Player/Dodge bound to Space.");

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Setup Milestone 4");
            try
            {
                GameObject player = movement.gameObject;
                EnsureComponent<CharacterStats>(player);
                EnsureComponent<Health>(player);
                EnsureComponent<Stamina>(player);
                EnsureComponent<PlayerDodge>(player);
                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(group);
                Selection.activeGameObject = player;
                Debug.Log("Milestone 4 configured. Save the scene (Ctrl+S). Space dodges. Tune PlayerDodge and CharacterStats stamina values in the Inspector. Existing scene, combat, camera and prefab settings were preserved.");
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                throw;
            }
        }

        private static void EnsureComponent<T>(GameObject player) where T : Component
        {
            if (player.GetComponent<T>() == null)
                Undo.AddComponent<T>(player);
        }
    }
}
