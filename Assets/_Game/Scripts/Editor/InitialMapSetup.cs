using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Player;
using Game.Enemies;
using Game.Items;
using Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace Game.EditorTools
{
    // Owns only the initial-area environment, never gameplay objects or source sprites.
    public static class InitialMapSetup
    {
        private const string ScenePath = "Assets/_Game/Scenes/Milestone1.unity";
        private const string Folder = "Assets/_Game/Art/Environment/InitialArea";
        private const string TerrainPath = "Assets/ThirdParty/EpicRPGWorld/AncientRuins/Tilesets/Tileset-Terrain2.png";
        private const string PropsPath = "Assets/ThirdParty/EpicRPGWorld/AncientRuins/Props/Atlas-Props.png";
        private const string Marker = "Initial Area M7.2A";
        private static readonly Vector3Int[] Route = {
            new Vector3Int(-6,-5,0), new Vector3Int(1,-5,0),
            new Vector3Int(3,-3,0), new Vector3Int(3,2,0) };
        private static readonly HashSet<Vector3Int> routeCells = new HashSet<Vector3Int>();
        private static Dictionary<string, Sprite> terrain, props;
        private static Material lit;
        private static Tilemap ground, decoration, collision, foreground;
        private static Tile solid;
        private static Transform environment;

        [MenuItem("Tools/Castleveil/Setup Initial Area")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode first.");
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                throw new InvalidOperationException("Open Milestone1 before running Setup Initial Area.");
            if (scene.GetRootGameObjects().Any(g => g.name == Marker))
            {
                Validate();
                Debug.Log("Initial area already exists; existing edits were preserved.");
                return;
            }
            ground = Find<Tilemap>(scene, "Ground Tilemap");
            decoration = Find<Tilemap>(scene, "Decoration Tilemap");
            collision = Find<Tilemap>(scene, "Collision Tilemap");
            foreground = Find<Tilemap>(scene, "Foreground Tilemap");
            Grid grid = ground.GetComponentInParent<Grid>();
            if (grid == null || grid.cellSize != Vector3.one || grid.transform.lossyScale != Vector3.one)
                throw new InvalidOperationException("Expected the existing 1-unit Grid / 32 px logical tiles.");
            var player = One<PlayerMovement>(scene);
            var dog = One<FeralDogAI>(scene);
            var sword = One<WorldItem>(scene);
            if (Vector2.Distance(player.transform.position, new Vector2(-6,-5)) > 0.1f
                || Vector2.Distance(dog.transform.position, new Vector2(3,2)) > 0.1f
                || Vector2.Distance(sword.transform.position, new Vector2(1,-4)) > 0.1f)
                throw new InvalidOperationException("Layout expects validated Player (-6,-5), sword (1,-4), dog (3,2). No gameplay object was moved.");
            foreach (string layer in new[] { "Ground", "Environment", "Characters", "Foreground" })
                if (!SortingLayer.layers.Any(l => l.name == layer))
                    throw new InvalidOperationException("Missing sorting layer: " + layer);
            terrain = Sprites(TerrainPath);
            props = Sprites(PropsPath);
            lit = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Materials/VisualPrototypeLit.mat");
            if (lit == null) throw new InvalidOperationException("Existing URP 2D lit material is missing.");
            var snapshots = ProtectedSnapshot(scene);
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog("Setup Initial Area",
                "Replace the four environment Tilemaps with the initial-area blockout and save Milestone1? Gameplay objects and their positions are preserved. Undo is available.", "Build and save", "Cancel")) return;
            Undo.IncrementCurrentGroup();
            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Setup Initial Area M7.2A");
            foreach (var root in scene.GetRootGameObjects()) Undo.RegisterFullObjectHierarchyUndo(root, "Initial area");
            try
            {
                if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/_Game/Art/Environment", "InitialArea");
                environment = new GameObject(Marker).transform;
                Undo.RegisterCreatedObjectUndo(environment.gameObject, "Create initial area");
                ConfigureMap(ground, "Ground"); ConfigureMap(decoration, "Environment");
                ConfigureMap(collision, "Environment"); ConfigureMap(foreground, "Foreground");
                if (collision.GetComponent<TilemapCollider2D>() == null) Undo.AddComponent<TilemapCollider2D>(collision.gameObject);
                collision.GetComponent<TilemapCollider2D>().isTrigger = false;
                solid = Tile("SolidFootprint", terrain["Tileset-Terrain2_97"], Color.clear, true);
                var darkGrass = Sprites("Assets/_Game/Art/Tilesets/Castleveil_DarkForest/Castleveil_DarkForest_Ground_2.png");
                Tile[] grass = new[] { 93,94,95 }.Select(i => Tile("DarkGrass" + i, darkGrass["Castleveil_DarkForest_Ground_2_" + i], Color.white)).ToArray();
                // Painted margin covers the current 6-unit camera, including ultrawide views.
                for (int y=-37; y<=36; y++) for (int x=-43; x<=46; x++)
                    ground.SetTile(new Vector3Int(x,y,0), grass[(int)((uint)(x*73+y*29) % (uint)grass.Length)]);
                BuildRoute();
                Tile[] paving = new[] {247,248,249}.Select(i => Tile("OldPaving" + i, terrain["Tileset-Terrain2_" + i], new Color(0.55f,0.64f,0.70f))).ToArray();
                foreach (var cell in routeCells) ground.SetTile(cell, paving[Math.Abs(cell.x+cell.y)%paving.Length]);
                // Clear, open reserve for the future cabin; no building obstructs its exit.
                for(int x=-10;x<=-3;x++) for(int y=-8;y<=-3;y++)
                    ground.SetTile(new Vector3Int(x,y,0), paving[Math.Abs(x+y)%paving.Length]);
                Reserve("Cabin Reserve - 8x6", new Vector2(-6.5f,-5.5f));
                Reserve("Sword Clearing", sword.transform.position);
                Reserve("First Combat Clearing", dog.transform.position);
                Reserve("Northeast Ruins", new Vector2(12,7));
                // A solid perimeter keeps movement inside the painted, camera-safe area.
                for(int x=-18;x<=21;x++) { Block(x,-12); Block(x,12); }
                for(int y=-11;y<=11;y++) { Block(-18,y); Block(21,y); }
                Tile treeA = PropTile("CanopyA",0,new Color(0.68f,0.76f,0.72f));
                Tile treeB = PropTile("CanopyB",1,new Color(0.66f,0.75f,0.73f));
                for(int x=-18;x<=21;x+=3) { TreeCell(x,-15-Math.Abs(x%2),treeA); TreeCell(x,12+Math.Abs(x%2),treeB); }
                for(int y=-9;y<=9;y+=3) { TreeCell(-18,y,treeB); TreeCell(21,y,treeA); }
                // Extra visible forest beyond the perimeter avoids an abrupt map edge.
                for(int x=-24;x<=27;x+=4) { TreeCell(x,-20,treeB); TreeCell(x,18,treeA); }
                for(int y=-12;y<=12;y+=4) { TreeCell(-22,y,treeA); TreeCell(25,y,treeB); }
                foreach(var p in new[] { new Vector2(-14,-5),new Vector2(-13,-2),new Vector2(-15,2),
                    new Vector2(-10,7),new Vector2(-7,8),new Vector2(-8,10),new Vector2(15,-7),
                    new Vector2(18,-6),new Vector2(17,-3),new Vector2(-2,9),new Vector2(10,-8),new Vector2(17,8) })
                    Prop("Tree", (int)p.x%2==0?0:1, p, "Foreground", new Vector2(0.65f,0.6f));
                foreach(var p in new[] { new Vector2Int(-14,-6),new Vector2Int(-12,-3),new Vector2Int(-15,1),
                    new Vector2Int(-11,6),new Vector2Int(-7,7),new Vector2Int(-9,9),new Vector2Int(14,-7),
                    new Vector2Int(16,-6),new Vector2Int(18,-4),new Vector2Int(16,7),new Vector2Int(13,9),
                    new Vector2Int(8,8),new Vector2Int(10,9),new Vector2Int(-12,-10),new Vector2Int(8,-10) })
                    decoration.SetTile(new Vector3Int(p.x,p.y,0),PropTile("Bush",5,new Color(0.67f,0.78f,0.63f)));
                foreach(var p in new[] { new Vector2Int(-13,-8),new Vector2Int(-12,-9),new Vector2Int(13,-5),
                    new Vector2Int(14,-4),new Vector2Int(-11,4),new Vector2Int(-12,5),new Vector2Int(10,6),new Vector2Int(15,9) })
                {
                    decoration.SetTile(new Vector3Int(p.x,p.y,0),PropTile("Rock",19,new Color(0.80f,0.83f,0.80f)));
                    Block(p.x,p.y);
                }
                foreach(var p in new[] {new Vector2Int(-11,-7),new Vector2Int(-2,-7),new Vector2Int(6,-5),new Vector2Int(7,6),new Vector2Int(-4,5),new Vector2Int(12,-6)})
                    decoration.SetTile(new Vector3Int(p.x,p.y,0),PropTile("SmallStones",11,Color.white));
                Prop("Broken Wall A",21,new Vector2(10,9),"Environment",new Vector2(2.5f,0.65f));
                Prop("Broken Wall B",21,new Vector2(14,9),"Environment",new Vector2(2.5f,0.65f));
                Prop("Broken Pillar",20,new Vector2(8.5f,7),"Foreground",new Vector2(0.65f,0.6f));
                Prop("Old Altar",9,new Vector2(15,6),"Environment",new Vector2(1.2f,0.7f));
                var arch = Prop("Ruined Gateway",15,new Vector2(12,7),"Foreground",Vector2.zero);
                Box(arch, new Vector2(-1.15f,0.5f), new Vector2(0.65f,1f));
                Box(arch, new Vector2(1.15f,0.5f), new Vector2(0.65f,1f));
                // Branch toward the ruins; it does not cross the primary combat clearing.
                for(int x=3;x<=14;x++) for(int y=4;y<=5;y++) ground.SetTile(new Vector3Int(x,y,0),paving[(x+y)%3]);
                ConfigureHud(scene);
                var light = Find<Light2D>(scene,"Global Light 2D");
                light.intensity = 0.72f;
                light.color = new Color(0.72f,0.80f,1f);
                foreach(var pair in snapshots)
                    if (EditorJsonUtility.ToJson(pair.Key) != pair.Value)
                        throw new InvalidOperationException("Protected component changed: " + pair.Key.name + " / " + pair.Key.GetType().Name);
                collision.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
                Validate();
                AssetDatabase.SaveAssets();
                if(!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save Milestone1.");
                Undo.CollapseUndoOperations(undo);
                Debug.Log("M7.2A saved: 40x25 playable cells, painted margin, 3-cell route, protected objects unchanged. Play Mode remains manual.");
            }
            catch { Undo.RevertAllDownToGroup(undo); throw; }
        }

        // Batch entry point intended for a separate verification copy when the main Editor is open.
        public static void BuildBatch()
        {
            EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            Setup();
        }

        [MenuItem("Tools/Castleveil/Validate Initial Area")]
        public static void Validate()
        {
            Scene scene = SceneManager.GetActiveScene();
            if(scene.path != ScenePath) throw new InvalidOperationException("Open Milestone1 first.");
            foreach(var root in scene.GetRootGameObjects()) foreach(var t in root.GetComponentsInChildren<Transform>(true))
            {
                if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)
                    throw new InvalidOperationException("Missing script: " + t.name);
                foreach(var component in t.GetComponents<Component>())
                {
                    if(component == null) continue;
                    var so = new SerializedObject(component);
                    var property = so.GetIterator();
                    while(property.NextVisible(true))
                        if(property.propertyType == SerializedPropertyType.ObjectReference
                            && property.objectReferenceValue == null && property.objectReferenceEntityIdValue != EntityId.None)
                            throw new InvalidOperationException("Broken reference: " + t.name + "/" + property.propertyPath);
                }
            }
            var g=Find<Tilemap>(scene,"Ground Tilemap");
            var c=Find<Tilemap>(scene,"Collision Tilemap");
            for(int y=-12;y<=12;y++) for(int x=-18;x<=21;x++)
                if(!g.HasTile(new Vector3Int(x,y,0))) throw new InvalidOperationException("Ground hole.");
            BuildRoute();
            foreach(var cell in routeCells)
                if(c.HasTile(cell)) throw new InvalidOperationException("Primary route blocked at " + cell);
            var collider=c.GetComponent<TilemapCollider2D>();
            if(collider == null || collider.isTrigger || !collider.enabled) throw new InvalidOperationException("Collision Tilemap must be solid.");
            for(int x=-18;x<=21;x++)
                if(!c.HasTile(new Vector3Int(x,-12,0)) || !c.HasTile(new Vector3Int(x,12,0))) throw new InvalidOperationException("Open map boundary.");
            for(int y=-12;y<=12;y++)
                if(!c.HasTile(new Vector3Int(-18,y,0)) || !c.HasTile(new Vector3Int(21,y,0))) throw new InvalidOperationException("Open map boundary.");
            foreach(string layer in new[]{"Ground","Environment","Foreground"})
                if(!SortingLayer.layers.Any(l=>l.name==layer)) throw new InvalidOperationException("Missing sorting layer: "+layer);
            foreach(string name in new[]{"Ground Tilemap","Decoration Tilemap","Collision Tilemap","Foreground Tilemap"})
            {
                var renderer=Find<Tilemap>(scene,name).GetComponent<TilemapRenderer>();
                if(renderer==null || renderer.sharedMaterial==null || !renderer.enabled) throw new InvalidOperationException("Missing map renderer/material: "+name);
            }
            var dog=One<FeralDogAI>(scene);
            float detection=new SerializedObject(dog).FindProperty("detectionRadius").floatValue;
            if(Vector2.Distance(One<PlayerMovement>(scene).transform.position,dog.transform.position)<=detection
                || Vector2.Distance(One<WorldItem>(scene).transform.position,dog.transform.position)<=detection)
                throw new InvalidOperationException("Initial spawn or sword is inside the detection radius.");
            var light=Find<Light2D>(scene,"Global Light 2D");
            if(!light.enabled || light.lightType!=Light2D.LightType.Global || light.intensity<=0) throw new InvalidOperationException("Global Light 2D missing or disabled.");
            var hud=One<PlayerHud>(scene);
            if(hud.GetComponent<Canvas>().renderMode != RenderMode.ScreenSpaceOverlay || hud.transform.localScale != Vector3.one)
                throw new InvalidOperationException("HUD must use Overlay and unit scale.");
            var hudData=new SerializedObject(hud);
            foreach(string field in new[]{"health","stamina","healthFill","staminaFill","healthLabel","staminaLabel"})
                if(hudData.FindProperty(field).objectReferenceValue==null) throw new InvalidOperationException("Missing HUD reference: "+field);
            Debug.Log("M7.2A validation OK: no Missing Scripts/broken serialized references; ground continuous; route clear; collision and HUD configured.");
        }

        private static Dictionary<Component,string> ProtectedSnapshot(Scene scene)
        {
            var result=new Dictionary<Component,string>();
            foreach(var root in scene.GetRootGameObjects()) foreach(var c in root.GetComponentsInChildren<Component>(true))
                if(c != null && ((c is MonoBehaviour && c.GetType().Namespace != null && c.GetType().Namespace.StartsWith("Game."))
                    || (root.name != "Visual Prototype Arena" && root.name != "Prototype HUD")))
                    result[c]=EditorJsonUtility.ToJson(c);
            return result;
        }
        private static void ConfigureHud(Scene scene)
        {
            var hud=One<PlayerHud>(scene);
            var canvas=hud.GetComponent<Canvas>();
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera=null;
            canvas.sortingOrder=10;
            var rect=(RectTransform)hud.transform;
            rect.localScale=Vector3.one;
            var panel=Find<RectTransform>(scene,"Vitals");
            panel.anchorMin=panel.anchorMax=panel.pivot=Vector2.zero;
            panel.anchoredPosition=new Vector2(24,24);
            panel.localScale=Vector3.one;
        }
        private static void ConfigureMap(Tilemap map,string layer)
        {
            map.ClearAllTiles();
            var renderer=map.GetComponent<TilemapRenderer>();
            renderer.sharedMaterial=lit;
            renderer.sortingLayerName=layer;
            renderer.sortingOrder=0;
            map.color=Color.white;
        }
        private static void BuildRoute()
        {
            routeCells.Clear();
            for(int i=1;i<Route.Length;i++)
            {
                Vector3Int from=Route[i-1],to=Route[i];
                int steps=Mathf.Max(Mathf.Abs(to.x-from.x),Mathf.Abs(to.y-from.y));
                for(int s=0;s<=steps;s++)
                {
                    var cell=Vector3Int.RoundToInt(Vector3.Lerp(from,to,s/(float)steps));
                    for(int dx=-1;dx<=1;dx++) for(int dy=-1;dy<=1;dy++) routeCells.Add(cell+new Vector3Int(dx,dy,0));
                }
            }
        }
        private static Dictionary<string,Sprite> Sprites(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s=>s.name);
        private static Tile Tile(string name,Sprite sprite,Color color,bool blocks=false)
        {
            string path=Folder+"/"+name+".asset";
            var tile=AssetDatabase.LoadAssetAtPath<Tile>(path);
            if(tile != null) return tile;
            tile=ScriptableObject.CreateInstance<Tile>();
            tile.name=name; tile.sprite=sprite; tile.color=color;
            tile.colliderType=blocks?UnityEngine.Tilemaps.Tile.ColliderType.Grid:UnityEngine.Tilemaps.Tile.ColliderType.None;
            AssetDatabase.CreateAsset(tile,path);
            return tile;
        }
        private static Tile PropTile(string name,int index,Color color)
        {
            string path=Folder+"/"+name+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<Tile>(path);
            if(existing != null) return existing;
            Sprite sprite=props["Atlas-Props_"+index];
            var tile=Tile(name,sprite,color);
            float scale=sprite.pixelsPerUnit/32f;
            tile.transform=Matrix4x4.TRS(new Vector3(-sprite.bounds.center.x*scale,-sprite.bounds.min.y*scale,0),Quaternion.identity,Vector3.one*scale);
            EditorUtility.SetDirty(tile);
            return tile;
        }
        private static void Block(int x,int y) => collision.SetTile(new Vector3Int(x,y,0),solid);
        private static void TreeCell(int x,int y,Tile tree) { foreground.SetTile(new Vector3Int(x,y,0),tree); Block(x,y); }
        private static Transform Prop(string name,int spriteIndex,Vector2 position,string layer,Vector2 size)
        {
            var root=new GameObject(name).transform; root.SetParent(environment,false); root.position=position;
            var child=new GameObject("Visual",typeof(SpriteRenderer)).transform; child.SetParent(root,false);
            var renderer=child.GetComponent<SpriteRenderer>(); var sprite=props["Atlas-Props_"+spriteIndex];
            float scale=sprite.pixelsPerUnit/32f;
            child.localScale=Vector3.one*scale;
            child.localPosition=new Vector3(-sprite.bounds.center.x*scale,-sprite.bounds.min.y*scale,0);
            renderer.sprite=sprite; renderer.sharedMaterial=lit; renderer.sortingLayerName=layer;
            renderer.color=name=="Tree"?new Color(0.68f,0.76f,0.72f):new Color(0.8f,0.82f,0.78f);
            if(size!=Vector2.zero) Box(root,new Vector2(0,size.y*0.5f),size);
            return root;
        }
        private static void Box(Transform root,Vector2 offset,Vector2 size)
        { var box=root.gameObject.AddComponent<BoxCollider2D>(); box.offset=offset; box.size=size; }
        private static void Reserve(string name,Vector2 position)
        { var marker=new GameObject(name).transform; marker.SetParent(environment,false); marker.position=position; }
        private static T Find<T>(Scene scene,string name) where T:Component => scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).Single(c=>c.name==name);
        private static T One<T>(Scene scene) where T:Component => scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).Single();
    }
}
