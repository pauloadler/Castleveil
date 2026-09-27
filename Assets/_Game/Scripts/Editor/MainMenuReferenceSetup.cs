using System;
using System.IO;
using System.Linq;
using Game.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.EditorTools
{
    public static class MainMenuReferenceSetup
    {
        private const string Art = "Assets/_Game/Art/UI/MainMenu/";
        private const string ScenePath = "Assets/_Game/Scenes/MainMenu.unity";
        private const string GeneratedName = "ReferenceMenuCanvas";
        [Serializable] private sealed class Part { public string path; public int x, y, width, height; }
        [Serializable] private sealed class Layout { public int width, height, fireFrameCount; public float fireFramesPerSecond; public Part[] parts; }

        [MenuItem("Tools/Castleveil/Apply Reference Main Menu")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var layout = JsonUtility.FromJson<Layout>(File.ReadAllText(Art + "layout.json"));
            if (layout == null || layout.parts == null || layout.parts.Length != 7) throw new InvalidOperationException("Extract the reference assets first.");
            AssetDatabase.Refresh();
            string[] paths = layout.parts.Select(p => p.path).Concat(new[]{"Background/menu_background.png"})
                .Concat(Enumerable.Range(0, layout.fireFrameCount).Select(i => $"Fire/campfire_{i:00}.png")).Distinct().ToArray();
            foreach (string path in paths) ImportSprite(Art + path);
            Directory.CreateDirectory(Art + "Animation");
            Directory.CreateDirectory(Art + "Prefabs");
            AssetDatabase.Refresh();
            AnimatorController animation = CreateAnimation(layout);
            GameObject prefab = CreateFirePrefab(layout, animation);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var controller = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MainMenuController>(true)).Single();
            Transform root = controller.transform;
            Transform old = root.Find(GeneratedName);
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
            // Keep the previous presentation intact for a manual rollback.
            foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                Undo.RecordObject(canvas.gameObject, "Preserve old menu presentation");
                canvas.gameObject.SetActive(false);
            }
            RectTransform canvasRect = Rect(GeneratedName, root, Vector2.zero, new Vector2(layout.width, layout.height));
            Undo.RegisterCreatedObjectUndo(canvasRect.gameObject, "Reference menu");
            Canvas newCanvas = canvasRect.gameObject.AddComponent<Canvas>();
            newCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            newCanvas.sortingOrder = 100;
            var scaler = canvasRect.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(layout.width, layout.height);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            canvasRect.gameObject.AddComponent<GraphicRaycaster>();
            RectTransform matte = Rect("AspectRatioMatte", canvasRect, Vector2.zero, Vector2.zero);
            matte.anchorMin = Vector2.zero; matte.anchorMax = Vector2.one;
            var matteImage = matte.gameObject.AddComponent<Image>(); matteImage.color = Color.black; matteImage.raycastTarget = false;
            RectTransform content = Rect("ReferenceComposition", canvasRect, Vector2.zero, new Vector2(layout.width, layout.height));
            AddImage("Background", content, new Part {path="Background/menu_background.png",width=layout.width,height=layout.height}, layout);
            Button first = null, options = null, exit = null;
            foreach (Part part in layout.parts)
            {
                if (part.path.StartsWith("Fire/"))
                {
                    var fire = (GameObject)PrefabUtility.InstantiatePrefab(prefab, content);
                    Place(fire.GetComponent<RectTransform>(), part, layout);
                    continue;
                }
                Image image = AddImage(Path.GetFileNameWithoutExtension(part.path), content, part, layout);
                if (!part.path.StartsWith("Buttons/")) continue;
                image.raycastTarget = true;
                Button button = image.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                button.transition = Selectable.Transition.None; // The source already contains the intended button colors.
                if (part.path.EndsWith("button_start.png"))
                { first=button; UnityEventTools.AddPersistentListener(button.onClick, controller.NewGame); }
                else if (part.path.EndsWith("button_options.png"))
                { options=button; UnityEventTools.AddPersistentListener(button.onClick, controller.OpenOptions); }
                else if (part.path.EndsWith("button_exit.png"))
                { exit=button; UnityEventTools.AddPersistentListener(button.onClick, controller.ExitGame); }
                else button.interactable = false;
            }
            if (first == null || options == null || exit == null) throw new InvalidOperationException("Missing menu button.");
            SetNavigation(first, exit, options); SetNavigation(options, first, exit); SetNavigation(exit, options, first);
            var eventSystem = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<EventSystem>(true)).Single();
            eventSystem.firstSelectedGameObject = first.gameObject;
            if (!eventSystem.isActiveAndEnabled) throw new InvalidOperationException("Existing EventSystem must be active.");
            Validate(scene, layout, newCanvas, animation);
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Cannot save MainMenu.");
            Debug.Log("Reference MainMenu saved. Original canvas preserved disabled; gameplay/build settings unchanged. Play Mode still requires manual validation.");
        }

        private static void ImportSprite(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Texture importer missing: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static AnimatorController CreateAnimation(Layout layout)
        {
            string path = Art + "Animation/Campfire.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
            clip.frameRate = layout.fireFramesPerSecond;
            var keys = new ObjectReferenceKeyframe[layout.fireFrameCount];
            for (int i=0;i<keys.Length;i++) keys[i] = new ObjectReferenceKeyframe {
                time=i/layout.fireFramesPerSecond, value=Sprite($"Fire/campfire_{i % layout.fireFrameCount:00}.png")};
            AnimationUtility.SetObjectReferenceCurve(clip, new EditorCurveBinding {path="",type=typeof(Image),propertyName="m_Sprite"}, keys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime=true; settings.startTime=0f; settings.stopTime=layout.fireFrameCount/layout.fireFramesPerSecond;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            string controllerPath = Art + "Animation/Campfire.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var machine = controller.layers[0].stateMachine;
            var state = machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="Campfire") ?? machine.AddState("Campfire");
            state.motion=clip; machine.defaultState=state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static GameObject CreateFirePrefab(Layout layout, AnimatorController controller)
        {
            var obj = new GameObject("MainMenuCampfire", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Animator));
            try
            {
                Part part = layout.parts.Single(p=>p.path.StartsWith("Fire/"));
                obj.GetComponent<RectTransform>().sizeDelta=new Vector2(part.width,part.height);
                var image=obj.GetComponent<Image>(); image.sprite=Sprite(part.path); image.raycastTarget=false;
                var animator=obj.GetComponent<Animator>(); animator.runtimeAnimatorController=controller;
                animator.updateMode=AnimatorUpdateMode.UnscaledTime; animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                return PrefabUtility.SaveAsPrefabAsset(obj, Art+"Prefabs/MainMenuCampfire.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(obj); }
        }
        private static Sprite Sprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(Art+path)
            ?? throw new InvalidOperationException("Missing sprite: "+path);
        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent,false); rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
            rect.anchoredPosition=position; rect.sizeDelta=size; return rect;
        }
        private static void Place(RectTransform rect, Part part, Layout layout)
        {
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
            rect.anchoredPosition=new Vector2(part.x+part.width*.5f-layout.width*.5f,layout.height*.5f-part.y-part.height*.5f);
            rect.sizeDelta=new Vector2(part.width,part.height);
        }
        private static Image AddImage(string name, Transform parent, Part part, Layout layout)
        {
            RectTransform rect=Rect(name,parent,Vector2.zero,Vector2.zero); Place(rect,part,layout);
            var image=rect.gameObject.AddComponent<Image>(); image.sprite=Sprite(part.path); image.raycastTarget=false; return image;
        }
        private static void SetNavigation(Button button, Button up, Button down)
        { button.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=up,selectOnDown=down}; }

        private static void Validate(Scene scene, Layout layout, Canvas canvas, AnimatorController controller)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject)>0)
                    throw new InvalidOperationException("Missing script: "+transform.name);
            foreach (Image image in canvas.GetComponentsInChildren<Image>())
                if (image.name!="AspectRatioMatte" && image.sprite==null) throw new InvalidOperationException("Missing UI sprite.");
            var buttons=canvas.GetComponentsInChildren<Button>();
            if (buttons.Length!=4 || buttons.Count(b=>b.interactable)!=3) throw new InvalidOperationException("Button state mismatch.");
            foreach (Button button in buttons.Where(b=>b.interactable))
                if (button.onClick.GetPersistentEventCount()!=1 || button.onClick.GetPersistentTarget(0)==null)
                    throw new InvalidOperationException("Missing menu action.");
            AnimationClip clip=controller.animationClips.Single();
            if (!AnimationUtility.GetAnimationClipSettings(clip).loopTime) throw new InvalidOperationException("Fire must loop.");
            var binding=AnimationUtility.GetObjectReferenceCurveBindings(clip).Single();
            var keys=AnimationUtility.GetObjectReferenceCurve(clip,binding);
            if (keys.Length!=layout.fireFrameCount || keys.Any(k=>k.value==null))
                throw new InvalidOperationException("Fire frame references/loop mismatch.");
            // Sample actual clip binding in Edit Mode, without entering Play Mode.
            var sample = new GameObject("CampfireValidation", typeof(RectTransform),typeof(Image),typeof(Animator));
            try { var animator=sample.GetComponent<Animator>(); animator.runtimeAnimatorController=controller; animator.cullingMode=AnimatorCullingMode.AlwaysAnimate; animator.Rebind(); for (int i=0;i<layout.fireFrameCount;i++) { animator.Play("Campfire",0,(i+.25f)/layout.fireFrameCount); animator.Update(0f); if (sample.GetComponent<Image>().sprite!=keys[i].value) throw new InvalidOperationException("Fire Animator sample mismatch at frame "+i+" actual="+sample.GetComponent<Image>().sprite+" expected="+keys[i].value); } }
            finally { UnityEngine.Object.DestroyImmediate(sample); }
            Debug.Log("Reference menu validation PASS: sprites, scripts, 4 buttons/3 actions, Continue disabled, loop and sampled animation binding.");
        }

        public static void ApplyBatch() { Apply(); RenderPreview(); }
        private static void RenderPreview()
        {
            var canvas=UnityEngine.Object.FindObjectsByType<Canvas>().Single(c=>c.name==GeneratedName);
            var cameraObject=new GameObject("MenuPreviewCamera",typeof(Camera));
            var camera=cameraObject.GetComponent<Camera>(); camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.black;
            var texture=new RenderTexture(1448,1086,24); texture.Create(); camera.targetTexture=texture;
            try
            {
                canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=1;
                Canvas.ForceUpdateCanvases();
                var request=new UniversalRenderPipeline.SingleCameraRequest {destination=texture};
                RenderPipeline.SubmitRenderRequest(camera,request);
                RenderTexture.active=texture;
                var pixels=new Texture2D(1448,1086,TextureFormat.RGBA32,false); pixels.ReadPixels(new Rect(0,0,1448,1086),0,0); pixels.Apply();
                File.WriteAllBytes(Art+"Animation/unity_menu_preview.png",pixels.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(pixels);
                RenderTexture.active=null;
            }
            finally { canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.worldCamera=null; UnityEngine.Object.DestroyImmediate(cameraObject); texture.Release(); UnityEngine.Object.DestroyImmediate(texture); }
        }
    }
}
