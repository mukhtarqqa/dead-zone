using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
namespace DeadZone.Editor
{
    // Standalone preparation invoked through the live Editor; source JPGs remain untouched.
    public static class IntroArtBuilder
    {
        const string Generated="Assets/DeadZone/Cutscene/Generated/";
        static readonly string[] Names={"Subaru","Rem","Gojo","Sukuna","Zombie"};
        static readonly Rect[][] Regions={
            new[]{new Rect(20,65,900,270),new Rect(795,20,420,510),new Rect(1190,0,365,1143),new Rect(1560,0,480,1143)},
            new[]{new Rect(40,230,710,250),new Rect(800,75,410,440),new Rect(1300,15,275,570),new Rect(1740,30,230,570),new Rect(65,530,550,610),new Rect(715,525,390,618),new Rect(1265,540,330,603)},
            new[]{new Rect(15,0,385,1143),new Rect(375,0,370,1143),new Rect(745,0,285,1143),new Rect(1005,0,420,1143),new Rect(1365,0,330,1143),new Rect(1665,0,383,1143)},
            new[]{new Rect(15,0,430,1143),new Rect(420,0,300,1143),new Rect(710,0,355,1143),new Rect(1050,0,325,1143),new Rect(1310,0,335,1143),new Rect(1620,0,428,1143)},
            new[]{new Rect(165,15,265,560),new Rect(745,5,315,570),new Rect(1510,10,470,565),new Rect(185,575,290,568),new Rect(820,570,430,573),new Rect(1595,575,445,568)}
        };
        static Texture2D Read(string path)
        {
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
            if(!texture.LoadImage(File.ReadAllBytes(path)))throw new Exception("Cannot read "+path);return texture;
        }
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode");
            Directory.CreateDirectory(Generated);Directory.CreateDirectory("Assets/Resources/Intro");
            const string assetPath="Assets/Resources/Intro/IntroCutscene.asset";
            var definition=AssetDatabase.LoadAssetAtPath<ZoneIntroDefinition>(assetPath);
            bool fresh=definition==null;if(fresh)definition=ScriptableObject.CreateInstance<ZoneIntroDefinition>();
            definition.backgrounds=new Sprite[3];definition.actors=new IntroActorArt[5];
            for(int i=0;i<3;i++)
            {
                string path=Generated+$"Background{i+1}.png";
                var texture=Read($"Assets/CutscenePhoto/BG_Cutscene_{i+1:00}.jpg");
                File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
                definition.backgrounds[i]=Import(path);
            }
            for(int actor=0;actor<5;actor++)
            {
                string replacement=$"Assets/CutscenePhoto/cutscene{Names[actor]}.png";
                bool png=File.Exists(replacement);
                var texture=Read(png?replacement:$"Assets/CutscenePhoto/cutscene{Names[actor]}.jpg");
                var pixels=texture.GetPixels32();var background=BackgroundMask(texture,pixels);
                var regions=png&&actor==3?new[]{new Rect(0,0,470,1143),new Rect(470,0,370,1143),new Rect(840,0,355,1143),new Rect(1195,0,470,1143),new Rect(1600,180,448,963)}:Regions[actor];
                var poses=new Sprite[regions.Length];
                for(int pose=0;pose<poses.Length;pose++)
                {
                    var output=Extract(texture,pixels,background,regions[pose]);
                    string path=Generated+$"{Names[actor]}_{pose:00}.png";
                    File.WriteAllBytes(path,output.EncodeToPNG());UnityEngine.Object.DestroyImmediate(output);
                    poses[pose]=Import(path);
                }
                definition.actors[actor]=new IntroActorArt{id=Names[actor],poses=poses,referenceHeight=(actor==0?1500:actor==1?760:actor==4?720:1460)};
                UnityEngine.Object.DestroyImmediate(texture);
            }
            if(!definition.Ready)throw new Exception("Incomplete intro art");
            if(fresh)AssetDatabase.CreateAsset(definition,assetPath);else EditorUtility.SetDirty(definition);
            int count=0;foreach(var actor in definition.actors)count+=actor.poses.Length;
            AssetDatabase.SaveAssets();Debug.Log($"DZ_INTRO_ART_PASS: {count} isolated poses and 3 street backgrounds; PNG alpha preserved");
        }
        static bool[] BackgroundMask(Texture2D texture,Color32[] pixels)
        {
            int w=texture.width,h=texture.height;var mask=new bool[pixels.Length];var queue=new int[pixels.Length];int head=0,tail=0;
            // Supplied PNGs already have transparency: never key out white hair or clothing.
            if(Array.Exists(pixels,c=>c.a<32))
            {
                for(int i=0;i<pixels.Length;i++)mask[i]=pixels[i].a<32;
                return mask;
            }
            var bg=pixels[0];
            bool Match(int at){var c=pixels[at];return Mathf.Abs(c.r-bg.r)<45&&Mathf.Abs(c.g-bg.g)<45&&Mathf.Abs(c.b-bg.b)<45&&Mathf.Max(c.r,Mathf.Max(c.g,c.b))-Mathf.Min(c.r,Mathf.Min(c.g,c.b))<22;}
            void Add(int at){if(!mask[at]&&Match(at)){mask[at]=true;queue[tail++]=at;}}
            for(int x=0;x<w;x++){Add(x);Add((h-1)*w+x);}for(int y=0;y<h;y++){Add(y*w);Add(y*w+w-1);}
            while(head<tail)
            {
                int at=queue[head++],x=at%w,y=at/w;
                if(x>0)Add(at-1);if(x<w-1)Add(at+1);if(y>0)Add(at-w);if(y<h-1)Add(at+w);
            }
            return mask;
        }
        static Texture2D Extract(Texture2D texture,Color32[] pixels,bool[] background,Rect region)
        {
            float scale=texture.width/2048f;int x0=Mathf.RoundToInt(region.xMin*scale),x1=Mathf.Min(texture.width,Mathf.RoundToInt(region.xMax*scale));
            int y0=Mathf.Max(0,texture.height-Mathf.RoundToInt(region.yMax*scale)),y1=Mathf.Min(texture.height,texture.height-Mathf.RoundToInt(region.yMin*scale));
            var seen=new HashSet<int>();var best=new List<int>();
            for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)
            {
                int seed=y*texture.width+x;if(background[seed]||!seen.Add(seed))continue;
                var blob=new List<int>{seed};
                for(int head=0;head<blob.Count;head++)
                {
                    int at=blob[head],px=at%texture.width,py=at/texture.width;
                    for(int ny=py-1;ny<=py+1;ny++)for(int nx=px-1;nx<=px+1;nx++)
                    {
                        if(nx<x0||nx>=x1||ny<y0||ny>=y1)continue;int next=ny*texture.width+nx;
                        if(background[next]||!seen.Add(next))continue;blob.Add(next);
                    }
                }
                if(blob.Count>best.Count)best=blob;
            }
            if(best.Count<2000)throw new Exception("No figure in "+region);
            int l=x1,r=x0,b=y1,t=y0;foreach(int at in best){int x=at%texture.width,y=at/texture.width;l=Mathf.Min(l,x);r=Mathf.Max(r,x);b=Mathf.Min(b,y);t=Mathf.Max(t,y);}
            var output=new Texture2D(r-l+9,t-b+9,TextureFormat.RGBA32,false);var packed=new Color32[output.width*output.height];
            foreach(int at in best){var c=pixels[at];packed[(at/texture.width-b+4)*output.width+at%texture.width-l+4]=c;}
            output.SetPixels32(packed);output.Apply();return output;
        }
        static Sprite Import(string path)
        {
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Bilinear;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.npotScale=TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
            var factories=new SpriteDataProviderFactories();factories.Init();
            var provider=factories.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
            var edit=provider.GetDataProvider<ISpriteFrameEditCapability>();
            if(edit==null||!edit.GetEditCapability().HasCapability(EEditCapability.EditPivot))throw new Exception("Importer cannot safely edit pivot: "+path);
            var rects=provider.GetSpriteRects();foreach(var rect in rects){rect.alignment=SpriteAlignment.Custom;rect.pivot=new Vector2(.5f,0);}
            provider.SetSpriteRects(rects);provider.Apply();importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
