using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace Game.EditorTools
{
    public static class CampfireBonfireSetup
    {
        private const string Root = "Assets/_Game/Art/UI/MainMenu/";
        [MenuItem("Tools/Castleveil/Use 12 Bonfire Frames")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var available = AssetDatabase.LoadAllAssetsAtPath(Root+"Fire/bonfire.png").OfType<Sprite>().ToArray();
            var frames = Enumerable.Range(0,12).Select(i=>available.Single(s=>s.name=="bonfire_"+i)).ToArray();
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+"Animation/Campfire.anim");
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Root+"Animation/Campfire.controller");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/MainMenuCampfire.prefab");
            if (clip==null || controller==null || prefab==null) throw new InvalidOperationException("Existing campfire assets missing.");
            if (prefab.GetComponent<Image>()==null || prefab.GetComponent<Animator>()==null) throw new InvalidOperationException("Campfire must have Image and Animator.");
            var state=controller.layers[0].stateMachine.defaultState;
            if (state==null || state.motion!=clip) throw new InvalidOperationException("Existing controller must use Campfire.anim as default.");
            Undo.RecordObject(clip,"Use manual bonfire frames");
            foreach(var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip)) AnimationUtility.SetObjectReferenceCurve(clip,binding,null);
            clip.frameRate=10f;
            var spriteBinding=new EditorCurveBinding{path="",type=typeof(Image),propertyName="m_Sprite"};
            AnimationUtility.SetObjectReferenceCurve(clip,spriteBinding,frames.Select((s,i)=>new ObjectReferenceKeyframe{time=i/10f,value=s}).ToArray());
            var settings=AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime=true; settings.startTime=0; settings.stopTime=1.2f;
            AnimationUtility.SetAnimationClipSettings(clip,settings);
            EditorUtility.SetDirty(clip);
            // Only replace the obsolete initial sprite on the existing prefab. No RectTransform edits.
            GameObject contents=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(prefab));
            try
            {
                contents.GetComponent<Image>().sprite=frames[0];
                contents.GetComponent<Animator>().runtimeAnimatorController=controller;
                PrefabUtility.SaveAsPrefabAsset(contents,AssetDatabase.GetAssetPath(prefab));
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            var probe=new GameObject("BonfireValidation",typeof(RectTransform),typeof(Image),typeof(Animator));
            try
            {
                var animator=probe.GetComponent<Animator>(); animator.runtimeAnimatorController=controller;
                animator.cullingMode=AnimatorCullingMode.AlwaysAnimate; animator.Rebind();
                for(int cycle=0;cycle<2;cycle++) for(int i=0;i<12;i++)
                {
                    animator.Play(state.name,0,cycle+(i+.25f)/12f); animator.Update(0f);
                    if(probe.GetComponent<Image>().sprite!=frames[i]) throw new InvalidOperationException("Unexpected frame "+i+" in cycle "+cycle);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(probe); }
            AssetDatabase.SaveAssetIfDirty(clip);
            Debug.Log("BONFIRE_PASS: bonfire_0..bonfire_11, 12 frames, Image.m_Sprite, 10 FPS, 1.2 seconds, loop true; Animator sampled across two cycles. No scene/layout changes.");
        }
    }
}
