using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace DeadZone.Editor
{
    public static class CityV2Art
    {
        const string Root="Assets/DeadZone/CityV2";static CityPropSet props;
        public static Sprite Prop(int index)
        {
            if(props==null)props=AssetDatabase.LoadAssetAtPath<CityPropSet>(Root+"/CityPropSet.asset");
            if(props==null||props.Sprites==null||props.Sprites.Length!=12)throw new Exception("Import district props first");return props.Sprites[index];
        }
        [MenuItem("Dead Zone/Rework/Import district props")]
        public static void Import()
        {
            if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode");string path=Root+"/CityProps.png";var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType=TextureImporterType.Default;importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.isReadable=true;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);int width=texture.width,height=texture.height;var pixels=texture.GetPixels32();var visited=new bool[pixels.Length];var queue=new int[pixels.Length];var boxes=new List<RectInt>();
            for(int seed=0;seed<pixels.Length;seed++)
            {
                if(visited[seed]||pixels[seed].a<=96)continue;int head=0,tail=1,minX=width,minY=height,maxX=0,maxY=0;queue[0]=seed;visited[seed]=true;
                while(head<tail)
                {
                    int at=queue[head++],x=at%width,y=at/width;minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);
                    for(int d=0;d<4;d++){int nx=x+(d==0?1:d==1?-1:0),ny=y+(d==2?1:d==3?-1:0);if(nx<0||ny<0||nx>=width||ny>=height)continue;int next=ny*width+nx;if(visited[next]||pixels[next].a<=96)continue;visited[next]=true;queue[tail++]=next;}
                }
                if(tail>1800)boxes.Add(new RectInt(minX,minY,maxX-minX+1,maxY-minY+1));
            }
            if(boxes.Count!=12)throw new Exception("Expected 12 city props, found "+boxes.Count);boxes.Sort((a,b)=>b.center.y.CompareTo(a.center.y));for(int r=0;r<3;r++)boxes.Sort(r*4,4,Comparer<RectInt>.Create((a,b)=>a.center.x.CompareTo(b.center.x)));
            props=AssetDatabase.LoadAssetAtPath<CityPropSet>(Root+"/CityPropSet.asset");if(props==null){props=ScriptableObject.CreateInstance<CityPropSet>();AssetDatabase.CreateAsset(props,Root+"/CityPropSet.asset");}
            var sprites=new Sprite[12];float[] widths={3.2f,3.2f,4.6f,3.6f,2.9f,2,2.5f,.9f,3.7f,1.7f,7.3f,2.2f};
            for(int i=0;i<12;i++)
            {
                foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(Root+"/CityPropSet.asset"))if(asset is Sprite old&&old.name=="District prop "+i)sprites[i]=old;
                if(sprites[i]!=null)continue;var rect=boxes[i];sprites[i]=Sprite.Create(texture,new Rect(rect.x,rect.y,rect.width,rect.height),new Vector2(.5f,0),rect.width/widths[i]);sprites[i].name="District prop "+i;AssetDatabase.AddObjectToAsset(sprites[i],props);
            }
            props.Sprites=sprites;EditorUtility.SetDirty(props);AssetDatabase.SaveAssets();Debug.Log("DZ_CITY_PROPS_READY: 12 whole silhouettes, persistent editable sprites");
        }
    }
}
