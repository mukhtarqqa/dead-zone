using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DeadZone.Editor
{
    public static class PublicArenaBuilder
    {
        const string Root="Assets/DeadZone/CityArt";
        static Texture2D atlas;
        static readonly Dictionary<int,Tile> tiles=new();
        [MenuItem("Dead Zone/Rebuild arena (owned art)")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode before rebuilding");
            AssetDatabase.Refresh();
            var importer=(TextureImporter)AssetImporter.GetAtPath(Root+"/CityMaterials.png");
            importer.textureType=TextureImporterType.Default;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.SaveAndReimport();
            atlas=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/CityMaterials.png");tiles.Clear();
            if(!AssetDatabase.IsValidFolder(Root+"/Tiles"))AssetDatabase.CreateFolder(Root,"Tiles");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var city=new GameObject("Dead Zone · City District");city.AddComponent<CityArena>();
            var grid=new GameObject("Grid",typeof(Grid));grid.transform.SetParent(city.transform);
            var groundObject=new GameObject("Ground",typeof(Tilemap),typeof(TilemapRenderer));groundObject.layer=20;groundObject.transform.SetParent(grid.transform,false);
            var map=groundObject.GetComponent<Tilemap>();var render=groundObject.GetComponent<TilemapRenderer>();render.sortingLayerID=ZoneMap.Sorting(20);render.sortingOrder=-30;
            for(int x=-28;x<28;x++)for(int y=-22;y<22;y++)
            {
                bool avenue=Mathf.Abs(x)<4||Mathf.Abs(y)<4;
                bool plaza=Mathf.Abs(x)<6&&Mathf.Abs(y)<6;
                bool border=Mathf.Abs(x)>=24||Mathf.Abs(y)>=19;
                bool park=Mathf.Abs(y)>=16&&Mathf.Abs(y)<19&&Mathf.Abs(x)>=6&&Mathf.Abs(x)<23;
                int index=plaza?4:avenue||border?((x*13+y*7)%23==0?1:0):park?6:((x*5+y*7)%29==0?3:2);
                map.SetTile(new Vector3Int(x,y,0),Tile(index));
            }
            var structures=new GameObject("Buildings · solid footprints");structures.transform.SetParent(city.transform);
            Color[] palettes={new Color(.31f,.38f,.45f),new Color(.42f,.30f,.28f),new Color(.28f,.4f,.4f),new Color(.45f,.4f,.3f)};
            int count=0;
            foreach(int sy in new[]{-1,1})foreach(int sx in new[]{-1,1})for(int n=0;n<3;n++)
            {
                int k=count++;float x=sx*(8+n*6),y=sy>0?7:-15;
                var building=new GameObject("Block "+count);building.layer=20;building.transform.SetParent(structures.transform);building.transform.position=new Vector2(x,y);
                var box=building.AddComponent<BoxCollider2D>();box.offset=new Vector2(2,3);box.size=new Vector2(4,6);
                Rect(building.transform,"Building shadow",new Vector2(2.14f,2.82f),new Vector2(4.3f,6.35f),new Color(.03f,.04f,.06f,.7f),-12);
                var roof=SpriteObject(building.transform,"Roof",Tile(8+k%4).sprite,new Vector2(2,4));roof.transform.localScale=new Vector3(4,4,1);roof.GetComponent<SpriteRenderer>().sortingOrder=-5;
                var facade=SpriteObject(building.transform,"Facade",Facade(k,palettes[k%4]),new Vector2(2,1));facade.GetComponent<SpriteRenderer>().sortingOrder=-4;
                Rect(building.transform,"Roof ledge",new Vector2(2,2),new Vector2(4.15f,.14f),new Color(.1f,.14f,.2f),-3);
                Rect(building.transform,"Vent",new Vector2(1,4.6f),new Vector2(.65f,.5f),new Color(.15f,.21f,.26f),-2);
                for(int vent=0;vent<4;vent++)Rect(building.transform,"Vent grille",new Vector2(.78f+vent*.14f,4.6f),new Vector2(.045f,.4f),new Color(.42f,.49f,.52f),-1);
            }
            var details=new GameObject("Street furniture");details.transform.SetParent(city.transform);
            for(int x=-25;x<=25;x+=3)if(Mathf.Abs(x)>6)Rect(details.transform,"Road dash",new Vector2(x,0),new Vector2(1.2f,.10f),new Color(.77f,.67f,.36f),-22);
            for(int y=-20;y<=20;y+=3)if(Mathf.Abs(y)>6)Rect(details.transform,"Road dash",new Vector2(0,y),new Vector2(.10f,1.2f),new Color(.77f,.67f,.36f),-22);
            foreach(int sy in new[]{-1,1})for(int i=-3;i<=3;i++)Rect(details.transform,"Crosswalk",new Vector2(i*.65f,sy*5),new Vector2(.35f,1),new Color(.64f,.67f,.62f),-21);
            foreach(int sx in new[]{-1,1})for(int i=-3;i<=3;i++)Rect(details.transform,"Crosswalk",new Vector2(sx*5,i*.65f),new Vector2(1,.35f),new Color(.64f,.67f,.62f),-21);
            foreach(int sy in new[]{-1,1})foreach(int sx in new[]{-1,1})
            {
                var car=SpriteObject(details.transform,"Abandoned car",Car(sx==sy?new Color(.55f,.22f,.24f):new Color(.22f,.39f,.5f)),new Vector2(sx*13,sy*3.1f));car.GetComponent<SpriteRenderer>().sortingOrder=5;
                car.AddComponent<BoxCollider2D>().size=new Vector2(2.2f,1);car.layer=20;
                for(int i=0;i<5;i++)
                {
                    var tree=SpriteObject(details.transform,"Park tree",Tree(),new Vector2(sx*(6+i*4),sy*17.4f));tree.GetComponent<SpriteRenderer>().sortingOrder=6;tree.layer=20;
                    var collider=tree.AddComponent<CircleCollider2D>();collider.radius=.38f;collider.offset=new Vector2(0,-.3f);
                }
            }
            Rect(details.transform,"Plaza platform",Vector2.zero,new Vector2(2.8f,2.8f),new Color(.15f,.22f,.27f),-10);
            Rect(details.transform,"Fountain basin",Vector2.zero,new Vector2(2.4f,2.4f),new Color(.2f,.42f,.51f),-9);
            Rect(details.transform,"Fountain water",Vector2.zero,new Vector2(1.9f,1.9f),new Color(.28f,.66f,.75f),-8);
            var fountain=new GameObject("Fountain collider");fountain.layer=20;fountain.transform.SetParent(details.transform);fountain.AddComponent<BoxCollider2D>().size=Vector2.one*2.7f;
            Boundary(city.transform,new Vector2(-28,0),new Vector2(.4f,44));Boundary(city.transform,new Vector2(28,0),new Vector2(.4f,44));Boundary(city.transform,new Vector2(0,-22),new Vector2(56,.4f));Boundary(city.transform,new Vector2(0,22),new Vector2(56,.4f));
            var cameraObject=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));cameraObject.tag="MainCamera";var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=7.5f;camera.backgroundColor=new Color(.035f,.055f,.08f);camera.allowHDR=camera.allowMSAA=false;cameraObject.transform.position=new Vector3(-3,2,-10);
            var player=new GameObject("Player",typeof(SpriteRenderer),typeof(Rigidbody2D),typeof(CircleCollider2D));player.layer=20;player.transform.position=new Vector2(-3,2);player.GetComponent<SpriteRenderer>().sprite=Tile(2).sprite;player.GetComponent<SpriteRenderer>().sortingLayerID=ZoneMap.Sorting(20);var body=player.GetComponent<Rigidbody2D>();body.gravityScale=0;body.freezeRotation=true;body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;player.GetComponent<CircleCollider2D>().radius=.22f;
            var game=new GameObject("DeadZoneGame").AddComponent<DeadZoneGame>();game.playerVisual=player;game.gameCamera=camera;game.roster=AssetDatabase.LoadAssetAtPath<DeadZone.Data.HeroCatalog>("Assets/DeadZone/Content/HeroCatalog.asset");
            if(AssetDatabase.LoadAssetAtPath<ZoneBalance>("Assets/Resources/DeadZoneBalance.asset")==null)AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ZoneBalance>(),"Assets/Resources/DeadZoneBalance.asset");
            PlayerSettings.runInBackground=true;QualitySettings.antiAliasing=0;EditorSettings.serializationMode=SerializationMode.ForceText;
            const string path="Assets/Scenes/DeadZoneCity.unity";EditorSceneManager.SaveScene(scene,path);EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(path,true)};AssetDatabase.SaveAssets();
            Debug.Log("DZ_PUBLIC_ARENA_READY: owned art, 12 blocks, connected streets, editable balance and Unity scene");
        }
        static Tile Tile(int index)
        {
            if(tiles.TryGetValue(index,out var tile))return tile;string path=Root+"/Tiles/material_"+index+".asset";tile=AssetDatabase.LoadAssetAtPath<Tile>(path);if(tile!=null)return tiles[index]=tile;
            tile=ScriptableObject.CreateInstance<Tile>();tile.colliderType=UnityEngine.Tilemaps.Tile.ColliderType.None;int w=atlas.width/4,h=atlas.height/4;
            var sprite=Sprite.Create(atlas,new Rect(index%4*w,(3-index/4)*h,w,h),new Vector2(.5f,.5f),w,0,SpriteMeshType.FullRect);sprite.name="Material "+index;AssetDatabase.CreateAsset(tile,path);AssetDatabase.AddObjectToAsset(sprite,tile);tile.sprite=sprite;EditorUtility.SetDirty(tile);tiles[index]=tile;return tile;
        }
        static Sprite PixelSprite(string name,int width,int height,Color[] pixels,float ppu)
        {
            string path=Root+"/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(old!=null){foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path))if(asset is Sprite sprite)return sprite;}
            var tex=new Texture2D(width,height){filterMode=FilterMode.Point,name=name};tex.SetPixels(pixels);tex.Apply();AssetDatabase.CreateAsset(tex,path);var result=Sprite.Create(tex,new Rect(0,0,width,height),new Vector2(.5f,.5f),ppu);AssetDatabase.AddObjectToAsset(result,tex);return result;
        }
        static Sprite Facade(int kind,Color color)
        {
            var p=new Color[64*32];void R(int x,int y,int w,int h,Color c){for(int yy=y;yy<y+h;yy++)for(int xx=x;xx<x+w;xx++)p[yy*64+xx]=c;}
            R(0,0,64,32,color);for(int y=0;y<32;y+=6)R(0,y,64,1,color*.7f);
            for(int x=6;x<58;x+=14){R(x,14,9,12,new Color(.07f,.11f,.17f));R(x+1,16,6,9,new Color(.24f,.43f,.52f));R(x+4,16,1,9,new Color(.12f,.2f,.26f));R(x,13,10,1,new Color(.65f,.64f,.55f));}
            R(26,0,12,12,new Color(.07f,.10f,.14f));R(28,1,8,10,new Color(.24f,.22f,.2f));R(34,5,1,2,new Color(.8f,.7f,.4f));return PixelSprite("facade_"+kind,64,32,p,16);
        }
        static Sprite Car(Color color)
        {
            var p=new Color[64*32];void R(int x,int y,int w,int h,Color c){for(int yy=y;yy<y+h;yy++)for(int xx=x;xx<x+w;xx++)p[yy*64+xx]=c;}
            R(7,3,49,26,new Color(.05f,.08f,.1f));R(10,5,43,22,color);R(21,6,19,20,color*1.2f);R(19,8,6,16,new Color(.11f,.22f,.29f));R(39,8,6,16,new Color(.11f,.22f,.29f));R(7,8,4,5,new Color(.8f,.72f,.38f));R(7,21,4,4,new Color(.8f,.72f,.38f));return PixelSprite("car_"+ColorUtility.ToHtmlStringRGB(color),64,32,p,24);
        }
        static Sprite Tree()
        {
            var p=new Color[32*48];for(int y=1;y<17;y++)for(int x=14;x<18;x++)p[y*32+x]=new Color(.27f,.19f,.13f);
            for(int y=11;y<47;y++)for(int x=1;x<31;x++){float dx=(x-16)/14f,dy=(y-28)/18f;if(dx*dx+dy*dy<1)p[y*32+x]=(x*3+y*7)%9<3?new Color(.2f,.36f,.25f):new Color(.12f,.26f,.2f);}
            return PixelSprite("park_tree",32,48,p,20);
        }
        static GameObject SpriteObject(Transform parent,string name,Sprite sprite,Vector2 point){var obj=new GameObject(name);obj.layer=20;obj.transform.SetParent(parent,false);obj.transform.localPosition=point;var sr=obj.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sortingLayerID=ZoneMap.Sorting(20);return obj;}
        static void Rect(Transform parent,string name,Vector2 point,Vector2 size,Color color,int order){var obj=SpriteObject(parent,name,White(),point);obj.transform.localScale=new Vector3(size.x,size.y,1);var sr=obj.GetComponent<SpriteRenderer>();sr.color=color;sr.sortingOrder=order;}
        static Sprite White(){return PixelSprite("solid",1,1,new[]{Color.white},1);}
        static void Boundary(Transform parent,Vector2 point,Vector2 size){var obj=new GameObject("Arena boundary");obj.layer=20;obj.transform.SetParent(parent);obj.transform.position=point;obj.AddComponent<BoxCollider2D>().size=size;}
    }
}
