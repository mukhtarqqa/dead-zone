using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using TMPro;
namespace DeadZone.Editor
{
    public static class CityV2Builder
    {
        const string Root="Assets/DeadZone/CityV2";
        public const string ScenePath="Assets/Scenes/DeadZoneCityV2.unity";
        static Texture2D atlas;static Transform city;static readonly Dictionary<int,Tile> tiles=new();
        static readonly Vector2[] avenue={new(-54,0),new(25,0),new(40,7),new(54,7)};
        static readonly Vector2[] spine={new(0,-42),new(0,-17),new(5,-3),new(5,17),new(-4,29),new(-4,42)};
        static readonly Vector2[] entryPath={new(-50,-39),new(-41,-28),new(-37,-16),new(-23,-8),new(-14,-3),new(1,0)};
        [MenuItem("Dead Zone/Rework/Build large district city")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode");
            AssetDatabase.Refresh();var importer=(TextureImporter)AssetImporter.GetAtPath(Root+"/CityMaterialsV2.png");
            importer.textureType=TextureImporterType.Default;importer.maxTextureSize=256;importer.npotScale=TextureImporterNPOTScale.None;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.isReadable=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            atlas=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/CityMaterialsV2.png");CityV2Art.Prop(0);tiles.Clear();if(!AssetDatabase.IsValidFolder(Root+"/Tiles"))AssetDatabase.CreateFolder(Root,"Tiles");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);city=new GameObject("Dead Zone · Four Districts").transform;
            var arena=city.gameObject.AddComponent<CityArena>();arena.Bounds=new Rect(-56,-44,112,88);arena.Entry=new Vector2(-49,-38);
            arena.Districts=new[]{District("Жилой квартал",new Rect(-54,7,44,35),new Color(.56f,.65f,.67f)),District("Промышленная зона",new Rect(12,9,42,33),new Color(.72f,.51f,.32f)),District("Мемориальный парк",new Rect(-54,-35,42,29),new Color(.35f,.63f,.4f)),District("Рынок и клиника",new Rect(12,-41,42,35),new Color(.77f,.48f,.4f)),District("Южный въезд",new Rect(-55,-43,24,10),new Color(.86f,.73f,.4f))};
            arena.SupplyPoints=new[]{new Vector2(-45,-34),new Vector2(-40,12),new Vector2(-19,31),new Vector2(-44,34),new Vector2(20,14),new Vector2(45,31),new Vector2(40,13),new Vector2(-21,-18),new Vector2(-44,-15),new Vector2(-17,-30),new Vector2(17,-24.5f),new Vector2(34,-33),new Vector2(45,-14),new Vector2(21,-15),new Vector2(10,3),new Vector2(-9,24)};
            var grid=new GameObject("Grid",typeof(Grid));grid.transform.SetParent(city,false);
            var groundObject=new GameObject("Ground",typeof(Tilemap),typeof(TilemapRenderer));groundObject.transform.SetParent(grid.transform,false);groundObject.layer=20;
            var ground=groundObject.GetComponent<Tilemap>();groundObject.GetComponent<TilemapRenderer>().sortingLayerID=ZoneMap.Sorting(20);groundObject.GetComponent<TilemapRenderer>().sortingOrder=-1000;
            for(int x=-56;x<56;x++)for(int y=-44;y<44;y++)
            {
                Vector2 p=new Vector2(x+.5f,y+.5f);int material=x<-10&&y>7?3:x>12&&y>9?12:x<-12&&y<-6?6:x>12&&y<-6?13:2;
                if(x>=-40&&x<=-35&&y>=7&&y<=28||y>=25&&y<=28&&x>=-51&&x<=-12)material=2;
                if(x>=-38&&x<=-33&&y>=17&&y<=23)material=6;
                if(Mathf.Abs(x)>53||Mathf.Abs(y)>41)material=7;
                ground.SetTile(new Vector3Int(x,y,0),Tile(material));
            }
            CityV2Roads.Draw(city,atlas);
            // Residential courtyards: asymmetrical footprints leave loops around each block.
            Building("West apartments",new Rect(-49,21,8,14),0,new Color(.52f,.42f,.35f));
            Building("North apartments",new Rect(-37,30,16,7),0,new Color(.42f,.5f,.55f));
            Building("Courtyard house",new Rect(-31,14,8,10),0,new Color(.55f,.39f,.34f));
            Building("Corner townhouse",new Rect(-50,9,8,7),0,new Color(.45f,.53f,.43f));
            Building("East townhouse",new Rect(-18,22,6,14),0,new Color(.4f,.44f,.56f));
            // Industrial: larger warehouses, loading yards and staggered cargo lanes.
            Building("Warehouse A",new Rect(17,27,18,10),1,new Color(.36f,.4f,.43f));
            Building("Warehouse B",new Rect(39,18,11,17),1,new Color(.48f,.39f,.29f));
            Building("Workshop",new Rect(17,11,11,8),1,new Color(.38f,.48f,.45f));
            foreach(var r in new[]{new Rect(31,12,2,7),new Rect(35,15,2,8),new Rect(15,22,8,2)})Container(r,new Color(.36f,.41f,.4f));
            // Market: shop fronts, short counters and several routes between stalls.
            Building("Market arcade",new Rect(34,-12,16,6),2,new Color(.53f,.33f,.25f));
            Building("Corner bakery",new Rect(15,-21,8,6),2,new Color(.58f,.46f,.3f));
            Building("Clinic",new Rect(12,-35,12,8),3,new Color(.5f,.66f,.64f));
            Building("South shop",new Rect(38,-35,12,5),2,new Color(.5f,.37f,.38f));
            Building("Substation",new Rect(6,29,5,8),1,new Color(.4f,.48f,.46f));
            Building("Maintenance office",new Rect(-13,10,6,9),1,new Color(.39f,.46f,.45f));
            Building("Park cafe",new Rect(-11,-27,7,7),2,new Color(.56f,.42f,.3f));
            Building("Pharmacy",new Rect(6,-20,6,8),3,new Color(.49f,.63f,.61f));
            for(int row=0;row<3;row++)for(int col=0;col<3;col++)
            {
                Vector2 p=new Vector2(28+col*7+(row==1?2:0),-18-row*5);
                Prop("Market stall",(row+col)%2,p-Vector2.up*.4f,new Vector2(2.7f,.9f),new Vector2(0,.45f));
            }
            // Park: a pond, clear trails, small groups of trees and low benches.
            Pond(new Vector2(-26,-26),new Vector2(8,4.5f));
            foreach(var p in new[]{new Vector2(-49,-15),new Vector2(-43,-10),new Vector2(-33,-12),new Vector2(-18,-12),new Vector2(-16,-24),new Vector2(-20,-33),new Vector2(-36,-32),new Vector2(-46,-32),new Vector2(-46,-23),new Vector2(-39,-34),new Vector2(-13,-16),new Vector2(-51,-28)})Tree(p);
            foreach(var p in new[]{new Vector2(-51,-10),new Vector2(-50,-20),new Vector2(-46,-13),new Vector2(-42,-12),new Vector2(-34,-10),new Vector2(-28,-11),new Vector2(-22,-10),new Vector2(-16,-10),new Vector2(-14,-22),new Vector2(-16,-29),new Vector2(-25,-33),new Vector2(-31,-34),new Vector2(-35,-30),new Vector2(-48,-30),new Vector2(-51,-32),new Vector2(-52,-23),new Vector2(-43,-31),new Vector2(-42,-20)})Tree(p);
            foreach(var p in new[]{new Vector2(-41,-17),new Vector2(-33,-33),new Vector2(-17,-20),new Vector2(-42,24),new Vector2(-26,27)})Prop("Bench",5,p-Vector2.up*.25f,new Vector2(1.8f,.45f),new Vector2(0,.25f));
            foreach(var p in new[]{new Vector2(-29,4.8f),new Vector2(19,-4.9f),new Vector2(8,20),new Vector2(45,11)})Car(p,new Color(.32f,.46f,.52f));
            foreach(var p in new[]{new Vector2(-42,4),new Vector2(-14,4),new Vector2(11,-5),new Vector2(34,3),new Vector2(-8,30),new Vector2(8,-27)})Lamp(p);
            foreach(var segment in new[]{avenue,spine})for(int s=1;s<segment.Length;s++)
            {
                Vector2 delta=segment[s]-segment[s-1];int count=Mathf.FloorToInt(delta.magnitude/4);
                for(int i=1;i<count;i++){var p=Vector2.Lerp(segment[s-1],segment[s],(float)i/count);if((p-new Vector2(2,0)).sqrMagnitude<100)continue;var mark=Block("Road marking",p,new Vector2(1.5f,.09f),new Color(.75f,.65f,.39f),false,-984);mark.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);}
            }
            foreach(int side in new[]{-1,1})for(int i=-3;i<=3;i++)Block("Crosswalk",new Vector2(i*.7f+2,side*8),new Vector2(.35f,1.7f),new Color(.69f,.7f,.64f),false,-984);
            Prop("Dry plaza fountain",3,new Vector2(2,-1.3f),new Vector2(2.9f,1.6f),new Vector2(0,1));
            foreach(var p in new[]{new Vector2(-4,-5),new Vector2(8,-5),new Vector2(-4,5),new Vector2(8,5)})Prop("Plaza planter",6,p-Vector2.up*.4f,new Vector2(2.15f,.8f),new Vector2(0,.4f));
            Sign("Residential sign",new Vector2(-30,31.2f),"NORTH · RESIDENCES",new Color(.62f,.77f,.83f));Sign("Industrial sign",new Vector2(28,28.2f),"EAST · WORKS",new Color(.88f,.63f,.35f));Sign("Market sign",new Vector2(42,-10.8f),"SOUTH · MARKET",new Color(.95f,.65f,.45f));Sign("Clinic sign",new Vector2(18,-33.8f),"+ CLINIC",new Color(.5f,.95f,.81f));
            Prop("South gate bus shelter",2,new Vector2(-52,-38),new Vector2(4.3f,.85f),new Vector2(0,.45f));Prop("Checkpoint planter",6,new Vector2(-43,-36.5f),new Vector2(2.15f,.8f),new Vector2(0,.4f));
            Boundary(new Vector2(-56,0),new Vector2(.4f,88));Boundary(new Vector2(56,0),new Vector2(.4f,88));Boundary(new Vector2(0,-44),new Vector2(112,.4f));Boundary(new Vector2(0,44),new Vector2(112,.4f));
            Physics2D.SyncTransforms();
            for(int i=0;i<arena.SupplyPoints.Length;i++)
            {
                Vector2 original=arena.SupplyPoints[i];if(!ZoneMap.Obstacle(original,.45f,20))continue;bool found=false;
                for(float radius=.5f;radius<=5&&!found;radius+=.5f)for(int angle=0;angle<16;angle++){float a=angle*Mathf.PI/8;Vector2 candidate=original+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;if(arena.Bounds.Contains(candidate)&&!ZoneMap.Obstacle(candidate,.45f,20)){arena.SupplyPoints[i]=candidate;found=true;break;}}
                if(!found)throw new Exception("Cannot place reachable supply near "+original);
            }
            var cameraObject=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));cameraObject.tag="MainCamera";var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=6.8f;camera.allowHDR=camera.allowMSAA=false;camera.backgroundColor=new Color(.055f,.075f,.085f);cameraObject.transform.position=new Vector3(-43,-37,-10);
            var player=new GameObject("Player",typeof(SpriteRenderer),typeof(Rigidbody2D),typeof(CircleCollider2D));player.layer=20;player.transform.position=arena.TeamSpawn(0);player.GetComponent<SpriteRenderer>().sprite=Tile(2).sprite;player.GetComponent<SpriteRenderer>().sortingLayerID=ZoneMap.Sorting(20);var body=player.GetComponent<Rigidbody2D>();body.gravityScale=0;body.freezeRotation=true;body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;player.GetComponent<CircleCollider2D>().radius=.22f;
            var game=new GameObject("DeadZoneGame").AddComponent<DeadZoneGame>();game.playerVisual=player;game.gameCamera=camera;game.roster=AssetDatabase.LoadAssetAtPath<DeadZone.Data.HeroCatalog>("Assets/DeadZone/Content/HeroCatalog.asset");
            PlayerSettings.runInBackground=true;EditorSettings.serializationMode=SerializationMode.ForceText;EditorSceneManager.SaveScene(scene,ScenePath);EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};AssetDatabase.SaveAssets();Debug.Log("DZ_CITY_V2_READY: 112x88, edge entry, four distinct districts, editable geometry");
        }
        static CityDistrict District(string title,Rect bounds,Color color)=>new CityDistrict{title=title,bounds=bounds,color=color};
        static float Distance(Vector2 p,Vector2[] points){float min=float.MaxValue;for(int i=1;i<points.Length;i++){Vector2 a=points[i-1],d=points[i]-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude);min=Mathf.Min(min,Vector2.Distance(p,a+d*t));}return min;}
        static Tile Tile(int index)
        {
            if(tiles.TryGetValue(index,out var tile))return tile;string path=Root+"/Tiles/material_"+index+".asset";tile=AssetDatabase.LoadAssetAtPath<Tile>(path);if(tile!=null)return tiles[index]=tile;
            int size=atlas.width/4;tile=ScriptableObject.CreateInstance<Tile>();tile.colliderType=UnityEngine.Tilemaps.Tile.ColliderType.None;AssetDatabase.CreateAsset(tile,path);var sprite=Sprite.Create(atlas,new Rect(index%4*size,(3-index/4)*size,size,size),new Vector2(.5f,.5f),size,0,SpriteMeshType.FullRect);sprite.name="District material "+index;AssetDatabase.AddObjectToAsset(sprite,tile);tile.sprite=sprite;EditorUtility.SetDirty(tile);return tiles[index]=tile;
        }
        static Sprite Pixel(string name,int w,int h,Color[] pixels,float ppu)
        {
            string path=Root+"/"+name+"_"+w+"x"+h+".asset";foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path))if(asset is Sprite old)return old;
            var texture=new Texture2D(w,h){name=name,filterMode=FilterMode.Point};texture.SetPixels(pixels);texture.Apply();AssetDatabase.CreateAsset(texture,path);var sprite=Sprite.Create(texture,new Rect(0,0,w,h),new Vector2(.5f,.5f),ppu);AssetDatabase.AddObjectToAsset(sprite,texture);return sprite;
        }
        static Sprite White()=>Pixel("solid",1,1,new[]{Color.white},1);
        static GameObject Block(string name,Vector2 point,Vector2 size,Color color,bool solid,int order)
        {
            var obj=new GameObject(name);obj.layer=20;obj.transform.SetParent(city);obj.transform.position=point;obj.transform.localScale=new Vector3(size.x,size.y,1);var sr=obj.AddComponent<SpriteRenderer>();sr.sprite=White();sr.color=color;sr.sortingLayerID=ZoneMap.Sorting(20);sr.sortingOrder=order;if(solid)obj.AddComponent<BoxCollider2D>();return obj;
        }
        static void Building(string name,Rect rect,int style,Color color)
        {
            var root=new GameObject(name);root.layer=20;root.transform.SetParent(city);root.transform.position=rect.min;var box=root.AddComponent<BoxCollider2D>();box.offset=rect.size*.5f;box.size=rect.size;
            Block(name+" shadow",rect.center+new Vector2(.15f,-.15f),rect.size+Vector2.one*.3f,new Color(.04f,.07f,.09f,.5f),false,-995);
            var grid=new GameObject("Roof grid",typeof(Grid));grid.transform.SetParent(root.transform,false);var roof=new GameObject("Roof",typeof(Tilemap),typeof(TilemapRenderer));roof.transform.SetParent(grid.transform,false);var map=roof.GetComponent<Tilemap>();var renderer=roof.GetComponent<TilemapRenderer>();renderer.sortingLayerID=ZoneMap.Sorting(20);renderer.sortingOrder=ZoneDepth.Order(rect.yMin);
            for(int x=0;x<rect.width;x++)for(int y=2;y<rect.height;y++)map.SetTile(new Vector3Int(x,y,0),Tile(style==0?8:style==1?9:style==3?10:11));
            int w=Mathf.RoundToInt(rect.width*16),h=40;var pixels=new Color[w*h];
            void R(int x,int y,int width,int height,Color c){for(int yy=Mathf.Max(0,y);yy<Mathf.Min(h,y+height);yy++)for(int xx=Mathf.Max(0,x);xx<Mathf.Min(w,x+width);xx++)pixels[yy*w+xx]=c;}
            R(0,0,w,h,color);for(int y=0;y<h;y+=style==1?5:8){R(0,y,w,1,color*.77f);if(style==0)for(int x=y%16;x<w;x+=16)R(x,y,1,8,color*.72f);}
            if(style==1){for(int x=12;x<w-25;x+=58){R(x,1,40,25,new Color(.18f,.23f,.25f));for(int y=3;y<25;y+=4)R(x+2,y,36,1,new Color(.42f,.48f,.46f));R(x+4,27,32,8,new Color(.24f,.42f,.47f));}}
            else for(int x=7;x<w-12;x+=22){R(x,16,12,17,new Color(.07f,.12f,.17f));R(x+1,18,10,13,new Color(.3f,.49f,.55f));R(x+6,18,1,13,new Color(.13f,.23f,.27f));R(x-1,15,14,2,new Color(.67f,.65f,.53f));}
            R(w/2-7,0,14,17,new Color(.09f,.15f,.17f));R(w/2-5,1,10,14,style==3?new Color(.31f,.67f,.65f):new Color(.31f,.25f,.2f));R(w/2+2,7,1,3,new Color(.9f,.77f,.4f));
            var facade=new GameObject("Facade");facade.layer=20;facade.transform.SetParent(root.transform,false);facade.transform.localPosition=new Vector2(rect.width*.5f,1.25f);var sr=facade.AddComponent<SpriteRenderer>();sr.sprite=Pixel(name.Replace(" ","_")+"_facade",w,h,pixels,16);sr.sortingLayerID=ZoneMap.Sorting(20);sr.sortingOrder=ZoneDepth.Order(rect.yMin)+1;
            Block(name+" ledge",new Vector2(rect.center.x,rect.yMin+2.5f),new Vector2(rect.width,.12f),color*.6f,false,ZoneDepth.Order(rect.yMin)+2);
        }
        static GameObject Prop(string name,int index,Vector2 feet,Vector2 footprint,Vector2 offset,float visualHeight=0)
        {
            var root=new GameObject(name);root.layer=20;root.transform.SetParent(city);root.transform.position=feet;
            var visual=new GameObject("Sprite");visual.layer=20;visual.transform.SetParent(root.transform,false);var sr=visual.AddComponent<SpriteRenderer>();sr.sprite=CityV2Art.Prop(index);sr.sortingLayerID=ZoneMap.Sorting(20);
            if(visualHeight>0)visual.transform.localScale=new Vector3(1,visualHeight/sr.sprite.bounds.size.y,1);
            float front=feet.y+offset.y-footprint.y*.5f;sr.sortingOrder=ZoneDepth.Order(front);var occluder=visual.AddComponent<ZoneOccluder>();occluder.FeetY=front;
            if(footprint.x>0){var box=root.AddComponent<BoxCollider2D>();box.size=footprint;box.offset=offset;}return root;
        }
        static void Container(Rect rect,Color color)
        {
            bool vertical=rect.height>rect.width;Prop("Freight container",vertical?11:10,new Vector2(rect.center.x,rect.yMin),rect.size,new Vector2(0,rect.height*.5f),rect.height+.8f);
        }
        static void Tree(Vector2 point)
        {
            var root=Prop("Park tree",4,point,Vector2.zero,Vector2.zero);var collider=root.AddComponent<CircleCollider2D>();collider.radius=.32f;collider.offset=new Vector2(0,.25f);
        }
        static void Pond(Vector2 point,Vector2 radius)
        {
            int w=128,h=72;var pixels=new Color[w*h];for(int y=0;y<h;y++)for(int x=0;x<w;x++){float d=Mathf.Pow((x-64)/63f,2)+Mathf.Pow((y-36)/35f,2);if(d<1)pixels[y*w+x]=d>.8f?new Color(.39f,.44f,.37f):(x+y*7)%41<2?new Color(.37f,.61f,.62f):new Color(.18f,.4f,.45f);}
            var obj=new GameObject("Park pond");obj.layer=20;obj.transform.SetParent(city);obj.transform.position=point;obj.transform.localScale=new Vector3(radius.x/8,radius.y/4.5f,1);var sr=obj.AddComponent<SpriteRenderer>();sr.sprite=Pixel("pond",w,h,pixels,8);sr.sortingLayerID=ZoneMap.Sorting(20);sr.sortingOrder=-980;var polygon=obj.AddComponent<PolygonCollider2D>();var points=new Vector2[24];for(int i=0;i<points.Length;i++){float angle=i*Mathf.PI*2/points.Length;points[i]=new Vector2(Mathf.Cos(angle)*7.4f,Mathf.Sin(angle)*4.1f);}polygon.points=points;
        }
        static void Car(Vector2 point,Color color)=>Prop("Abandoned car",8,point-Vector2.up*.6f,new Vector2(3.3f,1.1f),new Vector2(0,.6f));
        static void Lamp(Vector2 p)=>Prop("Streetlamp",7,p,new Vector2(.22f,.25f),new Vector2(0,.12f));
        static void Sign(string name,Vector2 point,string value,Color color){Block(name+" frame",point,new Vector2(5,.65f),new Color(.06f,.11f,.14f),false,ZoneDepth.Order(point.y-1.1f)+2);var obj=new GameObject(name);obj.transform.SetParent(city);obj.transform.position=point;var text=obj.AddComponent<TextMeshPro>();text.font=TMP_Settings.defaultFontAsset;text.text=value;text.fontSize=1.7f;text.color=color;text.alignment=TextAlignmentOptions.Center;text.rectTransform.sizeDelta=new Vector2(5,.65f);text.GetComponent<MeshRenderer>().sortingLayerID=ZoneMap.Sorting(20);text.GetComponent<MeshRenderer>().sortingOrder=ZoneDepth.Order(point.y-1.1f)+3;}
        static void Boundary(Vector2 point,Vector2 size){Block("City boundary",point,size,new Color(.18f,.23f,.19f),true,-970);}
    }
}
