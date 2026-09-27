using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Player;
using Game.Items;
using Game.Enemies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Game.EditorTools
{
    public static class CabinTrailSetup
    {
        private const string ScenePath="Assets/_Game/Scenes/Milestone1.unity";
        private const string RootName="Cabin and Trail M7.2B";
        private const string Folder="Assets/_Game/Art/Environment/CabinTrail";
        private const string Source="Assets/RafaelMatos/ERW - Village(interiors)/assets/";
        private static readonly Vector2[] Trail={new Vector2(-3,-5),new Vector2(-1,-5.3f),new Vector2(1,-4.7f),new Vector2(2.7f,-2),new Vector2(3,1.2f),new Vector2(4.5f,3.7f),new Vector2(8,4.7f),new Vector2(12,5)};
        private static Material material;
        private static Transform cabin;

        [MenuItem("Tools/Castleveil/Setup Cabin and Dirt Trail")]
        public static void Setup()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var scene=SceneManager.GetActiveScene();
            if(scene.path!=ScenePath) throw new InvalidOperationException("Open Milestone1 first.");
            if(scene.GetRootGameObjects().Any(g=>g.name==RootName)) { Validate(); Debug.Log("M7.2B already exists; no changes applied."); return; }
            if(!scene.GetRootGameObjects().Any(g=>g.name=="Initial Area M7.2A")) throw new InvalidOperationException("The existing initial area is required.");
            var ground=Find<Tilemap>(scene,"Ground Tilemap");
            var decoration=Find<Tilemap>(scene,"Decoration Tilemap");
            var structure=AssetDatabase.LoadAllAssetsAtPath(Source+"Interiors_tilesets.png").OfType<Sprite>().ToDictionary(s=>s.name);
            var terrain=AssetDatabase.LoadAllAssetsAtPath("Assets/ThirdParty/EpicRPGWorld/AncientRuins/Tilesets/Tileset-Terrain2.png").OfType<Sprite>().ToDictionary(s=>s.name);
            material=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Materials/VisualPrototypeLit.mat");
            if(material==null) throw new InvalidOperationException("Existing lit material missing.");
            foreach(string sprite in new[]{"bed_4","cabinet_14","table_rect_0","bench_0","shelves_0","crates_0","barrels_0"})
                if(AssetDatabase.LoadAssetAtPath<Sprite>(Source+"furniture_and_props_sprites/"+sprite+".png")==null) throw new InvalidOperationException("Missing furniture sprite: "+sprite);
            if(!Application.isBatchMode && !EditorUtility.DisplayDialog("Cabin and dirt trail","Add the cabin and replace only InitialArea paving? Save Milestone1 after validation. Existing gameplay and forest remain unchanged.","Apply and save","Cancel")) return;
            // All pre-existing components except the two painted maps must stay identical.
            var before=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)).Where(c=>c!=null && c!=ground && c!=decoration).ToDictionary(c=>c,c=>EditorJsonUtility.ToJson(c));
            Undo.IncrementCurrentGroup(); int undo=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("M7.2B Cabin and trail");
            Undo.RegisterCompleteObjectUndo(ground,"Replace paving"); Undo.RegisterCompleteObjectUndo(decoration,"Trail edges");
            try
            {
                if(!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/_Game/Art/Environment","CabinTrail");
                cabin=new GameObject(RootName).transform; Undo.RegisterCreatedObjectUndo(cabin.gameObject,"Add cabin");
                var floor=Map("Cabin Floor","Environment",-20);
                var walls=Map("Cabin Walls","Environment",-10);
                var cutaway=Map("Cabin Cutaway Frame","Foreground",0);
                var wood=new[]{14,15,61}.Select(i=>MakeTile("WoodFloor"+i,structure["Interiors_tilesets_"+i],new Color(0.58f,0.62f,0.66f))).ToArray();
                var wall=MakeTile("TimberWall",structure["Interiors_tilesets_238"],new Color(0.64f,0.66f,0.69f));
                for(int x=-10;x<=-3;x++) for(int y=-8;y<=-3;y++) floor.SetTile(new Vector3Int(x,y,0),wood[Math.Abs(x+y)%3]);
                for(int x=-10;x<=-3;x++) for(int y=-3;y<=-2;y++) walls.SetTile(new Vector3Int(x,y,0),wall);
                for(int y=-8;y<=-4;y++) walls.SetTile(new Vector3Int(-10,y,0),wall);
                for(int y=-8;y<=-3;y++) if(y< -6 || y> -4) walls.SetTile(new Vector3Int(-3,y,0),wall);
                // Low front wall keeps the single-room interior readable without roof logic.
                var low=MakeTile("LowTimberFrame",structure["Interiors_tilesets_238"],new Color(0.48f,0.51f,0.55f),0.28f);
                for(int x=-10;x<=-3;x++) cutaway.SetTile(new Vector3Int(x,-8,0),low);
                Solid("North wall",new Vector2(-6,-2.7f),new Vector2(7.8f,0.3f));
                Solid("West wall",new Vector2(-9.7f,-5.3f),new Vector2(0.3f,5.5f));
                Solid("South low wall",new Vector2(-6,-7.9f),new Vector2(7.8f,0.25f));
                Solid("East lower jamb",new Vector2(-2.7f,-7.2f),new Vector2(0.3f,1.3f));
                Solid("East upper jamb",new Vector2(-2.7f,-3.1f),new Vector2(0.3f,0.8f));
                Furniture("Bed", "bed_4",new Vector2(-8.45f,-6.65f),1.25f,new Vector2(1.1f,1.65f));
                Furniture("Wardrobe", "cabinet_14",new Vector2(-8.2f,-3.9f),0.85f,new Vector2(0.8f,0.6f));
                Furniture("Small Table", "table_rect_0",new Vector2(-4.3f,-7.2f),1.4f,new Vector2(1.3f,0.55f));
                Furniture("Bench", "bench_0",new Vector2(-6.2f,-7.3f),1.1f,new Vector2(1f,0.4f));
                Furniture("Shelf", "shelves_0",new Vector2(-5.3f,-3.45f),1.3f,Vector2.zero);
                Furniture("Old Crates", "crates_0",new Vector2(-9,-7.4f),0.7f,new Vector2(0.6f,0.5f));
                Furniture("Barrel", "barrels_0",new Vector2(-3.65f,-3.6f),0.55f,new Vector2(0.5f,0.45f));
                Furniture("Dark Window", "windows4-_0",new Vector2(-6.7f,-2.65f),0.7f,Vector2.zero,"windows_and_doors_sprites/no_sunlight/");
                PaintTrail(ground,decoration,terrain);
                foreach(var pair in before) if(EditorJsonUtility.ToJson(pair.Key)!=pair.Value) throw new InvalidOperationException("Unexpected change to existing component: "+pair.Key.name+" / "+pair.Key.GetType().Name);
                Physics2D.SyncTransforms();
                Validate();
                AssetDatabase.SaveAssets();
                if(!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save Milestone1.");
                Undo.CollapseUndoOperations(undo);
                Debug.Log("M7.2B saved. Existing gameplay components preserved. Manual Play Mode validation pending.");
            }
            catch { Undo.RevertAllDownToGroup(undo); throw; }
        }

        private static void PaintTrail(Tilemap ground,Tilemap decoration,Dictionary<string,Sprite> terrain)
        {
            var grass=new[]{93,94,95}.Select(i=>AssetDatabase.LoadAssetAtPath<Tile>("Assets/_Game/Art/Environment/InitialArea/DarkGrass"+i+".asset")).ToArray();
            if(grass.Any(t=>t==null)) throw new InvalidOperationException("Existing grass tiles missing.");
            foreach(var cell in ground.cellBounds.allPositionsWithin)
                if(AssetDatabase.GetAssetPath(ground.GetTile(cell)).StartsWith("Assets/_Game/Art/Environment/InitialArea/OldPaving",StringComparison.Ordinal))
                    ground.SetTile(cell,grass[Math.Abs(cell.x+cell.y)%3]);
            var dirt=new[]{96,97,98}.Select(i=>MakeTile("PackedEarth"+i,terrain["Tileset-Terrain2_"+i],new Color(0.85f,0.38f,0.50f))).ToArray();
            var edges=new Tile[4];
            for(int i=0;i<4;i++) edges[i]=MakeTile("EarthEdge"+i,terrain["Tileset-Terrain2_7"],new Color(0.85f,0.38f,0.50f),1f,90*i);
            for(int y=-8;y<=7;y++) for(int x=-3;x<=15;x++)
            {
                var cell=new Vector3Int(x,y,0); var p=new Vector2(x+0.5f,y+0.5f);
                float best=float.MaxValue; Vector2 delta=Vector2.zero; float radius=1.8f;
                for(int i=1;i<Trail.Length;i++)
                {
                    Vector2 a=Trail[i-1],ab=Trail[i]-a;
                    float t=Mathf.Clamp01(Vector2.Dot(p-a,ab)/ab.sqrMagnitude);
                    Vector2 d=p-(a+ab*t);
                    if(d.sqrMagnitude<best) { best=d.sqrMagnitude; delta=d; radius=1.9f+0.25f*Mathf.Sin((i+t)*2.1f); }
                }
                float distance=Mathf.Sqrt(best);
                if(distance<radius-0.4f) ground.SetTile(cell,dirt[Math.Abs(x*7+y*3)%3]);
                else if(distance<radius+0.4f && !decoration.HasTile(cell))
                {
                    int angle=((Mathf.RoundToInt((Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg-90)/90)%4)+4)%4;
                    decoration.SetTile(cell,edges[angle]);
                }
            }
        }

        [MenuItem("Tools/Castleveil/Validate Cabin and Dirt Trail")]
        public static void Validate()
        {
            var scene=SceneManager.GetActiveScene();
            if(scene.path!=ScenePath) throw new InvalidOperationException("Open Milestone1 first.");
            var root=scene.GetRootGameObjects().Single(g=>g.name==RootName);
            foreach(string name in new[]{"Bed","Wardrobe","Small Table","Bench","Shelf","Old Crates","Barrel","Dark Window"})
                if(root.transform.Find(name)==null) throw new InvalidOperationException("Missing furniture: "+name);
            foreach(var go in scene.GetRootGameObjects()) foreach(var t in go.GetComponentsInChildren<Transform>(true))
            {
                if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0) throw new InvalidOperationException("Missing script: "+t.name);
                foreach(var c in t.GetComponents<Component>())
                {
                    if(c==null) continue;
                    var property=new SerializedObject(c).GetIterator();
                    while(property.NextVisible(true)) if(property.propertyType==SerializedPropertyType.ObjectReference && property.objectReferenceValue==null && property.objectReferenceEntityIdValue!=EntityId.None)
                        throw new InvalidOperationException("Broken reference: "+t.name+" / "+property.propertyPath);
                }
            }
            Physics2D.SyncTransforms();
            // Check the physical clearance from the unchanged player spawn through the east doorway.
            for(float x=-6;x<=-1.5f;x+=0.2f)
                foreach(var hit in Physics2D.OverlapCircleAll(new Vector2(x,-5),0.4f))
                    if(!hit.isTrigger && hit.transform.IsChildOf(root.transform)) throw new InvalidOperationException("Cabin exit blocked at "+x);
            var player=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PlayerMovement>(true)).Single();
            if(Vector2.Distance(player.transform.position,new Vector2(-6,-5))>0.1f) throw new InvalidOperationException("Unexpected player spawn.");
            foreach(var sr in root.GetComponentsInChildren<SpriteRenderer>()) if(sr.sprite==null || sr.sharedMaterial==null) throw new InvalidOperationException("Furniture sprite or material missing.");
            Debug.Log("M7.2B validation OK: required furniture, references, Missing Scripts and east exit clearance checked. Gameplay needs manual Play Mode validation.");
        }
        public static void BuildBatch() { EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single); Setup(); }
        private static Tilemap Map(string name,string layer,int order)
        {
            var go=new GameObject(name,typeof(Tilemap),typeof(TilemapRenderer)); go.transform.SetParent(cabin,false);
            if(cabin.GetComponent<Grid>()==null) cabin.gameObject.AddComponent<Grid>();
            var r=go.GetComponent<TilemapRenderer>();r.sharedMaterial=material;r.sortingLayerName=layer;r.sortingOrder=order;
            return go.GetComponent<Tilemap>();
        }
        private static Tile MakeTile(string name,Sprite sprite,Color color,float height=1f,float angle=0f)
        {
            string path=Folder+"/"+name+".asset"; var existing=AssetDatabase.LoadAssetAtPath<Tile>(path); if(existing!=null)return existing;
            var tile=ScriptableObject.CreateInstance<Tile>();tile.name=name;tile.sprite=sprite;tile.color=color;tile.colliderType=Tile.ColliderType.None;
            float s=sprite.pixelsPerUnit/32f;
            tile.transform=Matrix4x4.TRS(Vector3.zero,Quaternion.Euler(0,0,angle),new Vector3(s,s*height,1))*Matrix4x4.Translate(-sprite.bounds.center);
            AssetDatabase.CreateAsset(tile,path);return tile;
        }
        private static void Furniture(string name,string spriteName,Vector2 position,float width,Vector2 footprint,string subfolder="furniture_and_props_sprites/")
        {
            Sprite sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Source+subfolder+spriteName+".png");
            var root=new GameObject(name).transform;root.SetParent(cabin,false);root.position=position;
            var visual=new GameObject("Visual",typeof(SpriteRenderer)).transform;visual.SetParent(root,false);
            float scale=width/sprite.bounds.size.x;visual.localScale=Vector3.one*scale;visual.localPosition=new Vector3(-sprite.bounds.center.x*scale,-sprite.bounds.min.y*scale,0);
            var r=visual.GetComponent<SpriteRenderer>();r.sprite=sprite;r.sharedMaterial=material;r.sortingLayerName="Environment";r.sortingOrder=5;r.color=new Color(0.72f,0.76f,0.78f);
            if(footprint!=Vector2.zero) { var box=root.gameObject.AddComponent<BoxCollider2D>();box.offset=new Vector2(0,footprint.y/2);box.size=footprint; }
        }
        private static void Solid(string name,Vector2 position,Vector2 size)
        { var go=new GameObject(name,typeof(BoxCollider2D));go.transform.SetParent(cabin,false);go.transform.position=position;go.GetComponent<BoxCollider2D>().size=size; }
        private static T Find<T>(Scene scene,string name) where T:Component => scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).Single(c=>c.name==name);
    }
}
