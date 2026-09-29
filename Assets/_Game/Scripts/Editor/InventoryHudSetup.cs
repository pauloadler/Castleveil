using System;
using System.IO;
using System.Linq;
using Game.Player;
using Game.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.EditorTools
{
    public static class InventoryHudSetup
    {
        private static Font font;
        private static PlayerInputReader input;
        private static readonly Color Border = new Color(0.30f, 0.34f, 0.40f);

        [MenuItem("Tools/Castleveil/Setup Inventory HUD")]
        public static void Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                scene.path != "Assets/_Game/Scenes/Milestone1.unity")
                throw new InvalidOperationException("Open Milestone1 outside Play Mode.");
            if (scene.isDirty)
                throw new InvalidOperationException("Save your current scene changes before adding the inventory.");
            if (Find<InventoryUI>(scene) != null)
            {
                Validate(scene);
                Debug.Log("Inventory already exists; preserved without rebuilding.");
                return;
            }
            var hud = Find<PlayerHud>(scene);
            var equipment = Find<PlayerEquipment>(scene);
            input = equipment != null ? equipment.GetComponent<PlayerInputReader>() : null;
            var canvas = hud != null ? hud.GetComponentInParent<Canvas>() : null;
            if (canvas == null || equipment == null || input == null)
                throw new InvalidOperationException("Existing HUD, PlayerEquipment and input are required.");
            var actions = new SerializedObject(input).FindProperty("inputActions").objectReferenceValue as InputActionAsset;
            if (actions == null || actions.FindActionMap("Player") == null)
                throw new InvalidOperationException("Player Input Actions missing.");
            var sword = AssetDatabase.LoadAllAssetsAtPath("Assets/_Game/Art/Weapons/Sword_Weapon_Inicial.png")
                .OfType<Sprite>().FirstOrDefault(s => s.name == "Sword_Weapon_Inicial_0");
            if (sword == null) throw new InvalidOperationException("Existing sword sprite not found.");
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Edit only a copy of the existing input asset; retain all existing IDs and bindings.
            var copy = UnityEngine.Object.Instantiate(actions);
            var map = copy.FindActionMap("Player", true);
            var action = map.FindAction("Inventory") ?? map.AddAction("Inventory", InputActionType.Button);
            if (!action.bindings.Any(b => b.path == "<Keyboard>/i")) action.AddBinding("<Keyboard>/i");
            File.WriteAllText(AssetDatabase.GetAssetPath(actions), copy.ToJson());
            UnityEngine.Object.DestroyImmediate(copy);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(actions));

            if (canvas.GetComponent<GraphicRaycaster>() == null) Undo.AddComponent<GraphicRaycaster>(canvas.gameObject);
            if (Find<EventSystem>(scene) == null)
            {
                var events = new GameObject("Inventory EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                Undo.RegisterCreatedObjectUndo(events, "Create inventory EventSystem");
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }

            var root = Rect("InventoryUI", canvas.transform, Vector2.zero, Vector2.zero);
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = root.offsetMax = Vector2.zero;
            var ui = Undo.AddComponent<InventoryUI>(root.gameObject);
            var panel = Box("InventoryPanel", root, Vector2.zero, new Vector2(600, 620), new Color(0.025f, 0.04f, 0.065f, 0.98f));
            panel.GetComponent<Image>().raycastTarget = true;
            Block(panel);
            Label("Title", panel, "INVENTÁRIO", new Vector2(0, 268), new Vector2(420, 42), 27);
            var close = Button("CloseButton", panel, "X", new Vector2(263, 269), new Vector2(38, 38));
            UnityEventTools.AddPersistentListener(close.onClick, ui.Toggle);
            var equipmentArea = Rect("EquipmentArea", panel, new Vector2(0, 90), new Vector2(520, 265));
            Slot(equipmentArea, "HeadSlot", "CABEÇA", new Vector2(0, 76));
            Slot(equipmentArea, "ChestSlot", "TORSO", Vector2.zero);
            Slot(equipmentArea, "BootsSlot", "BOTAS", new Vector2(0, -76));
            var weapon = Slot(equipmentArea, "WeaponSlot", "ARMA", new Vector2(-150, 0));
            Slot(equipmentArea, "AccessorySlot1", "ACESSÓRIO", new Vector2(150, 38));
            Slot(equipmentArea, "AccessorySlot2", "ACESSÓRIO", new Vector2(150, -48));
            var iconRect = Rect("WeaponIcon", weapon, Vector2.zero, new Vector2(48, 48));
            var icon = Undo.AddComponent<Image>(iconRect.gameObject);
            icon.sprite = sword; icon.preserveAspect = true; icon.raycastTarget = false; icon.enabled = false;
            Label("GridTitle", panel, "MOCHILA", new Vector2(0, -87), new Vector2(480, 30), 16);
            var grid = Rect("InventoryGrid", panel, new Vector2(0, -203), new Vector2(512, 192));
            for (int row = 0; row < 3; row++)
                for (int col = 0; col < 8; col++)
                    Box("EmptySlot_" + row + "_" + col, grid, new Vector2((col - 3.5f) * 62, (1 - row) * 62),
                        new Vector2(55, 55), new Color(0.045f, 0.06f, 0.085f));
            var toggle = Button("InventoryButton", root, "I", new Vector2(-44, -44), new Vector2(48, 48));
            var toggleRect = (RectTransform)toggle.transform;
            toggleRect.anchorMin = toggleRect.anchorMax = Vector2.one;
            UnityEventTools.AddPersistentListener(toggle.onClick, ui.Toggle);
            Set(ui, "input", input); Set(ui, "equipment", equipment); Set(ui, "panel", panel.gameObject);
            Set(ui, "weaponIcon", icon); Set(ui, "weaponSprite", sword);
            panel.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            Validate(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Inventory HUD added and saved. I / HUD button toggles the panel. Play Mode awaits manual validation.");
        }

        [MenuItem("Tools/Castleveil/Update Equipment Layout")]
        public static void UpdateEquipmentLayout()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/_Game/Scenes/Milestone1.unity" || scene.isDirty)
                throw new InvalidOperationException("Open and save Milestone1 outside Play Mode first.");
            var ui = Find<InventoryUI>(scene);
            if (ui == null) throw new InvalidOperationException("Existing inventory required.");
            var data = new SerializedObject(ui);
            var panel = ((GameObject)data.FindProperty("panel").objectReferenceValue).transform;
            var area = panel.Find("EquipmentArea");
            var grid = (RectTransform)panel.Find("InventoryGrid");
            var sprite = (Sprite)data.FindProperty("weaponSprite").objectReferenceValue;
            if (area == null || grid == null || sprite == null) throw new InvalidOperationException("Existing inventory references missing.");
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Update equipment layout");
            try
            {
                // Only the equipment presentation is replaced. Grid, controls and panel remain intact.
                for (int i = area.childCount - 1; i >= 0; i--) Undo.DestroyObjectImmediate(area.GetChild(i).gameObject);
                var names = new[] { "MainWeaponSlot", "ChestSlot", "CapeSlot", "HelmetSlot", "BeltSlot",
                    "GlovesSlot", "NecklaceSlot", "Ring1Slot", "Ring2Slot", "BootsSlot", "SecondarySlot" };
                var positions = new[] { new Vector2(-212, 0), new Vector2(-115, 70), new Vector2(-35, 70),
                    new Vector2(55, 85), new Vector2(135, 85), new Vector2(-115, -60),
                    new Vector2(-35, -60), new Vector2(45, -60), new Vector2(115, -60),
                    new Vector2(90, 0), new Vector2(212, 0) };
                for (int i = 0; i < names.Length; i++)
                {
                    bool large = i == 0 || i == 10;
                    var slot = Box(names[i], area, positions[i], large ? new Vector2(78, 200) : new Vector2(62, 58),
                        new Color(0.07f, 0.075f, 0.105f));
                    slot.GetComponent<Outline>().effectColor = new Color(0.15f, 0.16f, 0.205f);
                    var silhouette = Rect("EmptySilhouette", slot, Vector2.zero, new Vector2(44, 44));
                    DrawSilhouette(silhouette, names[i]);
                    var icon = Undo.AddComponent<Image>(Rect("EquippedIcon", slot, Vector2.zero,
                        large ? new Vector2(60, 150) : new Vector2(48, 44)).gameObject);
                    icon.raycastTarget = false; icon.preserveAspect = true; icon.enabled = false;
                    if (i == 0)
                    {
                        icon.sprite = sprite;
                        Set(ui, "weaponIcon", icon);
                        Set(ui, "weaponSilhouette", silhouette.gameObject);
                    }
                }
                if (area.childCount != 11 || area.GetComponentsInChildren<Text>(true).Length != 0)
                    throw new InvalidOperationException("Expected 11 equipment slots without labels.");
                Vector3[] corners = new Vector3[4];
                grid.GetWorldCorners(corners);
                float gridTop = panel.InverseTransformPoint(corners[1]).y;
                foreach (RectTransform slot in area)
                {
                    slot.GetWorldCorners(corners);
                    if (panel.InverseTransformPoint(corners[0]).y <= gridTop)
                        throw new InvalidOperationException("Equipment overlaps inventory grid.");
                    if (slot.Find("EmptySilhouette") == null || slot.Find("EquippedIcon") == null)
                        throw new InvalidOperationException("Slot children missing.");
                }
                Validate(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Undo.CollapseUndoOperations(group);
                Debug.Log("Equipment layout verified: 11 slots, no text, no grid overlap, references valid, no Missing Scripts.");
            }
            catch { Undo.RevertAllDownToGroup(group); throw; }
        }

        // Small UI-only silhouettes; no imported/generated artwork or equipment gameplay.
        private static void DrawSilhouette(RectTransform root, string kind)
        {
            if (kind == "MainWeaponSlot") { Shape(root, 0, 8, 5, 36); Shape(root, 0, -11, 23, 4); Shape(root, 0, -20, 5, 14); }
            else if (kind == "HelmetSlot") { Shape(root, 0, 9, 28, 17); Shape(root, -11, -7, 6, 18); Shape(root, 11, -7, 6, 18); Shape(root, 0, -2, 4, 13); }
            else if (kind == "ChestSlot") { Shape(root, 0, -3, 23, 30); Shape(root, -16, 8, 12, 13); Shape(root, 16, 8, 12, 13); }
            else if (kind == "CapeSlot") { Shape(root, 0, 14, 16, 8); Shape(root, 0, 3, 24, 16); Shape(root, 0, -12, 34, 16); }
            else if (kind == "GlovesSlot") { Shape(root, 0, -6, 21, 24); for (int i=0;i<4;i++) Shape(root, -8+i*5, 12, 4, 16-i*2); Shape(root, -14, -2, 8, 8); }
            else if (kind == "BeltSlot") { Shape(root, -12, 0, 14, 9); Shape(root, 12, 0, 14, 9); Ring(root, 0, 0, 7, 5); }
            else if (kind == "BootsSlot") { Shape(root, -4, 7, 15, 25); Shape(root, 3, -10, 29, 12); }
            else if (kind == "SecondarySlot") { Shape(root, 0, 9, 32, 25); Shape(root, 0, -8, 24, 12); Shape(root, 0, -18, 12, 8); }
            else if (kind == "NecklaceSlot") { Ring(root, 0, 4, 13, 14); Shape(root, 0, -14, 9, 9); }
            else { Ring(root, 0, -2, 11, 11); Shape(root, 0, 11, 8, 7); }
        }
        private static void Ring(RectTransform root, float x, float y, float rx, float ry)
        {
            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI * 2 / 12;
                Shape(root, x + Mathf.Cos(angle)*rx, y + Mathf.Sin(angle)*ry, 4, 4);
            }
        }
        private static void Shape(RectTransform root, float x, float y, float w, float h)
        {
            var image = Undo.AddComponent<Image>(Rect("Shape", root, new Vector2(x,y), new Vector2(w,h)).gameObject);
            image.color = new Color(0.19f, 0.20f, 0.255f, 0.65f);
            image.raycastTarget = false;
        }
        public static void Validate(Scene scene)
        {
            var ui = Find<InventoryUI>(scene);
            if (ui == null) throw new InvalidOperationException("Inventory UI missing.");
            var data = new SerializedObject(ui);
            foreach (var field in new[] { "input", "equipment", "panel", "weaponIcon", "weaponSprite" })
                if (data.FindProperty(field).objectReferenceValue == null)
                    throw new InvalidOperationException("Inventory reference missing: " + field);
            if (((GameObject)data.FindProperty("panel").objectReferenceValue).activeSelf)
                throw new InvalidOperationException("Panel must start closed.");
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) != 0)
                        throw new InvalidOperationException("Missing script: " + t.name);
            var reader = (PlayerInputReader)data.FindProperty("input").objectReferenceValue;
            var actions = (InputActionAsset)new SerializedObject(reader).FindProperty("inputActions").objectReferenceValue;
            if (!actions.FindAction("Player/Inventory", true).bindings.Any(b => b.path == "<Keyboard>/i"))
                throw new InvalidOperationException("Inventory I binding missing.");
            var buttons = ui.GetComponentsInChildren<Button>(true);
            if (buttons.Length != 2 || buttons.Any(b => b.onClick.GetPersistentEventCount() != 1 ||
                b.onClick.GetPersistentTarget(0) != ui || b.onClick.GetPersistentMethodName(0) != "Toggle"))
                throw new InvalidOperationException("Inventory button binding invalid.");
        }

        private static T Find<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).SingleOrDefault();

        private static void Set(UnityEngine.Object obj, string field, UnityEngine.Object value)
        {
            var data = new SerializedObject(obj);
            data.FindProperty(field).objectReferenceValue = value;
            data.ApplyModifiedProperties();
        }
        private static void Block(RectTransform rect)
        {
            var block = Undo.AddComponent<InventoryClickBlocker>(rect.gameObject);
            Set(block, "input", input);
        }
        private static RectTransform Rect(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create inventory UI");
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size; rect.anchoredPosition = pos;
            return rect;
        }
        private static RectTransform Box(string name, Transform parent, Vector2 pos, Vector2 size, Color color)
        {
            var rect = Rect(name, parent, pos, size);
            var image = Undo.AddComponent<Image>(rect.gameObject);
            image.color = color; image.raycastTarget = false;
            var border = Undo.AddComponent<Outline>(rect.gameObject);
            border.effectColor = Border; border.effectDistance = new Vector2(1, -1);
            return rect;
        }
        private static void Label(string name, Transform parent, string value, Vector2 pos, Vector2 size, int fontSize)
        {
            var rect = Rect(name, parent, pos, size);
            var text = Undo.AddComponent<Text>(rect.gameObject);
            text.font = font; text.fontSize = fontSize; text.text = value;
            text.color = new Color(0.88f, 0.88f, 0.84f); text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
        }
        private static Button Button(string name, Transform parent, string text, Vector2 pos, Vector2 size)
        {
            var rect = Box(name, parent, pos, size, new Color(0.10f, 0.13f, 0.18f));
            var image = rect.GetComponent<Image>(); image.raycastTarget = true;
            var button = Undo.AddComponent<Button>(rect.gameObject); button.targetGraphic = image;
            Label("Text", rect, text, Vector2.zero, size, 22); Block(rect);
            return button;
        }
        private static RectTransform Slot(Transform parent, string name, string title, Vector2 pos)
        {
            var rect = Box(name, parent, pos, new Vector2(60, 60), new Color(0.045f, 0.06f, 0.085f));
            Label("Label", rect, title, new Vector2(0, -39), new Vector2(115, 18), 11);
            return rect;
        }
    }
}
