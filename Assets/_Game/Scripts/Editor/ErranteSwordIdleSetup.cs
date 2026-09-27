using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Game.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.EditorTools
{
    public static class ErranteSwordIdleSetup
    {
        [MenuItem("Tools/Castleveil/Configure Errante Sword Idle")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            const string scenePath="Assets/_Game/Scenes/Milestone1.unity";
            var sprites=AssetDatabase.LoadAllAssetsAtPath("Assets/_Game/Art/Characters/Errante/Errante_Idle_8Dir_Sword.png").OfType<Sprite>().ToArray();
            if(sprites.Length!=8) throw new InvalidOperationException("Expected exactly 8 Sword sprites.");
            var ordered=Enumerable.Range(0,8).Select(i=>sprites.Single(s=>s.name=="Errante_Idle_8Dir_Sword_"+i)).ToArray();
            Scene scene=SceneManager.GetSceneByPath(scenePath);
            bool opened=!scene.IsValid() || !scene.isLoaded;
            if(opened) scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);
            try
            {
                if(scene.isDirty) throw new InvalidOperationException("Save your pending Milestone1 changes before running this command.");
                var visual=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ErranteDirectionalSprite>(true)).Single();
                var equipment=visual.GetComponentInParent<PlayerEquipment>();
                if(equipment==null) throw new InvalidOperationException("Missing PlayerEquipment.");
                var data=new SerializedObject(visual);
                string[] names={"North","NorthEast","East","SouthEast","South","SouthWest","West","NorthWest"};
                var unarmed=new Sprite[8];
                for(int i=0;i<8;i++)
                {
                    unarmed[i]=data.FindProperty(char.ToLowerInvariant(names[i][0])+names[i].Substring(1)).objectReferenceValue as Sprite;
                    if(unarmed[i]==null) throw new InvalidOperationException("Missing existing unarmed reference.");
                    data.FindProperty("sword"+names[i]).objectReferenceValue=ordered[i];
                }
                data.FindProperty("equipment").objectReferenceValue=equipment;
                data.ApplyModifiedProperties();
                var equipmentData=new SerializedObject(equipment);
                var weapon=equipmentData.FindProperty("weaponVisual").objectReferenceValue as GameObject;
                if(weapon!=null)
                {
                    if(!weapon.transform.IsChildOf(equipment.transform)) throw new InvalidOperationException("Unexpected WeaponVisual hierarchy.");
                    Undo.RecordObject(weapon,"Hide separate weapon"); weapon.SetActive(false);
                }
                // Presentation reference only: gameplay code and EquipmentChanged stay untouched.
                equipmentData.FindProperty("weaponVisual").objectReferenceValue=null;
                equipmentData.ApplyModifiedProperties();
                foreach(var controller in equipment.GetComponentsInChildren<EquipmentVisualController>(true))
                { Undo.RecordObject(controller,"Disable separate equipment presentation"); controller.enabled=false; }
                Transform root=equipment.transform.Find("VisualRoot/EquipmentRoot");
                if(root!=null) { Undo.RecordObject(root.gameObject,"Hide old EquipmentRoot"); root.gameObject.SetActive(false); }
                var select=typeof(ErranteDirectionalSprite).GetMethod("GetIdleSprite",BindingFlags.NonPublic|BindingFlags.Instance);
                for(int i=0;i<8;i++)
                {
                    if((Sprite)select.Invoke(visual,new object[]{(Direction8)i,false})!=unarmed[i]
                        || (Sprite)select.Invoke(visual,new object[]{(Direction8)i,true})!=ordered[i])
                        throw new InvalidOperationException("Idle selection mismatch: "+names[i]);
                }
                EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save Milestone1.");
                Debug.Log("SWORD_IDLE_PASS: 8 existing unarmed references preserved; 8 Sword references mapped N,NE,E,SE,S,SW,W,NW. Both selections checked. Separate presentation disabled. Play Mode pending.");
            }
            finally { if(opened) EditorSceneManager.CloseScene(scene,true); }
        }
    }
}
