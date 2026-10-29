using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace DeadZone.Editor
{
    public static class RemOniArtTools
    {
        public const string SourceRoot = "Assets/DeadZone/Art/RemOni/Source/";
        static readonly string[] Names = { "Idle", "Run", "Attack", "Dash" };
        // Source row gutters are measured from the generated art, not assumed to be exact cells.
        static readonly float[][] Rows = {
            new[] { 0f, 335f/1254, 635f/1254, 915f/1254, 1f },
            new[] { 0f, 263f/1024, 506f/1024, 744f/1024, 1f },
            new[] { 0f, 290f/1024, 520f/1024, 750f/1024, 1f },
            new[] { 0f, 340f/1254, 635f/1254, 910f/1254, 1f }
        };

        struct Cell
        {
            public RectInt rect;
            public float center, feet, height;
        }

        sealed class Blob
        {
            public readonly List<int> pixels = new List<int>();
            public int left=int.MaxValue, right, bottom=int.MaxValue, top;
            public int hairLeft=int.MaxValue, hairRight, hairTop, hairBottom=int.MaxValue, hairCount;
        }

        static List<Cell> Segment(Texture2D source, Color32[] pixels, int sheet, int columns, out int[] owners)
        {
            owners = new int[pixels.Length]; Array.Fill(owners, -1);
            var visited = new bool[pixels.Length];
            var cells = new List<Cell>();
            for (int row = 0; row < 4; row++)
            {
                int y0 = source.height - Mathf.RoundToInt(Rows[sheet][row+1] * source.height);
                int y1 = source.height - Mathf.RoundToInt(Rows[sheet][row] * source.height);
                var blobs = new List<Blob>();
                for (int seed=y0*source.width; seed<y1*source.width; seed++)
                {
                    if (visited[seed] || pixels[seed].a < 160) continue;
                    var blob = new Blob(); blob.pixels.Add(seed); visited[seed]=true;
                    for (int head=0; head<blob.pixels.Count; head++)
                    {
                        int at=blob.pixels[head], x=at%source.width, y=at/source.width;
                        blob.left=Mathf.Min(blob.left,x); blob.right=Mathf.Max(blob.right,x);
                        blob.bottom=Mathf.Min(blob.bottom,y); blob.top=Mathf.Max(blob.top,y);
                        if (Hair(pixels[at]))
                        {
                            blob.hairCount++; blob.hairLeft=Mathf.Min(blob.hairLeft,x); blob.hairRight=Mathf.Max(blob.hairRight,x);
                            blob.hairTop=Mathf.Max(blob.hairTop,y); blob.hairBottom=Mathf.Min(blob.hairBottom,y);
                        }
                        for (int ny=y-1; ny<=y+1; ny++) for (int nx=x-1; nx<=x+1; nx++)
                        {
                            if(nx<0||nx>=source.width||ny<y0||ny>=y1)continue;
                            int next=ny*source.width+nx;
                            if(visited[next]||pixels[next].a<160)continue;
                            visited[next]=true;blob.pixels.Add(next);
                        }
                    }
                    if(blob.pixels.Count>=4)blobs.Add(blob);
                }
                var bodies=new Blob[columns];
                foreach(var blob in blobs) if(blob.hairCount>80)
                {
                    int col=Mathf.Clamp(Mathf.FloorToInt((blob.hairLeft+blob.hairRight)*.5f/source.width*columns),0,columns-1);
                    if(bodies[col]==null||blob.hairCount>bodies[col].hairCount)bodies[col]=blob;
                }
                for(int col=0;col<columns;col++) if(bodies[col]==null)throw new Exception($"Missing body: {Names[sheet]} {row}/{col}");
                foreach(var blob in blobs)
                {
                    int owner=Array.IndexOf(bodies,blob);
                    if(owner<0)
                    {
                        float best=float.MaxValue;
                        for(int col=0;col<columns;col++)
                        {
                            var body=bodies[col];
                            float dx=Mathf.Max(0,Mathf.Max(body.left-blob.right,blob.left-body.right));
                            float dy=Mathf.Max(0,Mathf.Max(body.bottom-blob.top,blob.bottom-body.top));
                            float score=dx*dx+dy*dy+.001f*Mathf.Abs((blob.left+blob.right)-(body.hairLeft+body.hairRight));
                            if(score<best){best=score;owner=col;}
                        }
                        if(best>1600)continue;
                    }
                    foreach(int at in blob.pixels)owners[at]=row*columns+owner;
                }
                for(int col=0;col<columns;col++)
                {
                    var body=bodies[col];float center=(body.hairLeft+body.hairRight)*.5f, width=body.hairRight-body.hairLeft;
                    int feet=body.hairBottom;
                    for(int y=y0;y<body.hairBottom;y++)
                    {
                        int count=0;
                        for(int x=Mathf.Max(0,Mathf.RoundToInt(center-width*.22f));x<Mathf.Min(source.width,center+width*.22f);x++)
                        {
                            var c=pixels[y*source.width+x];
                            if(owners[y*source.width+x]==row*columns+col&&c.a>210&&c.r<100&&c.g<100&&c.b<120)count++;
                        }
                        if(count>=width*.08f){feet=y;break;}
                    }
                    cells.Add(new Cell{rect=new RectInt(0,y0,source.width,y1-y0),center=center,feet=feet,height=body.hairTop-feet+width*.14f});
                }
            }
            return cells;
        }

        [MenuItem("Dead Zone/Rem Oni/Import animation sheets")]
        public static void Import()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before importing art.");
            for (int s = 0; s < 4; s++) ImportSheet(s);
            ZoneRemArt.ResetCache();
            AssetDatabase.SaveAssets();
            Check();
        }

        static bool Hair(Color32 c) => c.a > 180 && c.b > 105 && c.b > c.r * 1.18f && c.g > c.r * 1.12f;

        static void ImportSheet(int sheet)
        {
            string sourcePath = SourceRoot + "RemOni" + Names[sheet] + ".png";
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!source.LoadImage(File.ReadAllBytes(sourcePath))) throw new Exception("Cannot read " + sourcePath);
            var pixels = source.GetPixels32();
            int columns = ZoneRemArt.FrameCounts[sheet], size = ZoneRemArt.CellSize;
            var cells = Segment(source, pixels, sheet, columns, out var owners);
            // A downward impact can put the dark flail below the shoes. Reject that anchor outlier.
            for (int row=0; row<4; row++)
            {
                var feet=new List<float>();
                for(int frame=0;frame<columns;frame++)feet.Add(cells[row*columns+frame].feet);
                feet.Sort();float baseline=feet[feet.Count/2];
                for(int frame=0;frame<columns;frame++)
                {
                    int index=row*columns+frame;var cell=cells[index];
                    if(Mathf.Abs(cell.feet-baseline)>cell.height*.05f)
                    { cell.height+=cell.feet-baseline;cell.feet=baseline;cells[index]=cell; }
                }
            }
            var heights = cells.ConvertAll(c => c.height);
            heights.Sort();
            // One scale per sheet avoids resizing the body between animation poses.
            float scale = 84 / heights[heights.Count / 2];
            var output = new Texture2D(columns * size, 4 * size, TextureFormat.RGBA32, false);
            var packed = new Color32[output.width * output.height];
            int clipped = 0;
            for (int row = 0; row < 4; row++) for (int frame = 0; frame < columns; frame++)
            {
                var cell = cells[row * columns + frame];
                int dx = frame * size, dy = (3-row) * size;
                // Point sampling preserves generated pixel clusters. All frames share a foot pivot (96,40).
                for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
                {
                    int sx = Mathf.RoundToInt(cell.center + (x - 96) / scale);
                    int sy = Mathf.RoundToInt(cell.feet + (y - 40) / scale);
                    if (!cell.rect.Contains(new Vector2Int(sx,sy))) continue;
                    if (owners[sy*source.width+sx] != row*columns+frame) continue;
                    var c = pixels[sy * source.width + sx];
                    // Binary alpha removes low-opacity matte fringes and soft generated haze.
                    c.a = (byte)(c.a >= 160 ? 255 : 0);
                    if (c.a == 0) c = new Color32(0,0,0,0);
                    packed[(dy+y)*output.width+dx+x] = c;
                    if (c.a > 0 && (x == 0 || y == 0 || x == size-1 || y == size-1)) clipped++;
                }
            }
            if (clipped > 0) throw new Exception("Increase cell padding: " + Names[sheet]);
            output.SetPixels32(packed); output.Apply();
            string path = "Assets/Resources/RemOni" + Names[sheet] + ".png";
            File.WriteAllBytes(path, output.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(source); UnityEngine.Object.DestroyImmediate(output);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = ZoneRemArt.PixelsPerUnit;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
            importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 2048; importer.isReadable = true;
            var factories = new SpriteDataProviderFactories(); factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
            var previous = provider.GetSpriteRects();
            var rects = new List<SpriteRect>();
            for (int row = 0; row < 4; row++) for (int frame = 0; frame < columns; frame++)
            {
                string name=$"RemOni{Names[sheet]}_{row}_{frame:00}";
                var existing=Array.Find(previous,r=>r.name==name);
                rects.Add(new SpriteRect {
                    name = name,
                    rect = new Rect(frame*size, (3-row)*size, size, size),
                    alignment = SpriteAlignment.Custom, pivot = new Vector2(.5f, 40f/size),
                    spriteID = existing != null ? existing.spriteID : GUID.Generate()
                });
            }
            provider.SetSpriteRects(rects.ToArray());
            var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            names.SetNameFileIdPairs(rects.ConvertAll(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply(); importer.SaveAndReimport();
            CreateClips(path, sheet);
        }

        static void CreateClips(string sheetPath, int sheet)
        {
            string directory="Assets/DeadZone/Art/RemOni/Animations";
            Directory.CreateDirectory(directory);
            var sprites=new List<Sprite>();
            foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(sheetPath))if(asset is Sprite sprite)sprites.Add(sprite);
            sprites.Sort((a,b)=>string.CompareOrdinal(a.name,b.name));
            int count=ZoneRemArt.FrameCounts[sheet];
            string[] directions={"Up","Down","Left","Right"};
            for(int row=0;row<4;row++)
            {
                string path=$"{directory}/RemOni{Names[sheet]}{directions[row]}.anim";
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                bool fresh=clip==null;if(fresh)clip=new AnimationClip();
                float fps=sheet==0?5:sheet==1?10:sheet==2?6/ZoneRemRage.SwingDuration:4/ZonePlayer.DashDuration;
                clip.frameRate=fps;
                var keys=new ObjectReferenceKeyframe[count+1];
                for(int frame=0;frame<=count;frame++)
                {
                    float at=frame/fps;
                    if(sheet==2)at=frame<3?frame*ZoneRemRage.ImpactDelay/3:ZoneRemRage.ImpactDelay+(frame-3)*(ZoneRemRage.SwingDuration-ZoneRemRage.ImpactDelay)/3;
                    keys[frame]=new ObjectReferenceKeyframe{time=at,value=sprites[row*count+Mathf.Min(frame,count-1)]};
                }
                var binding=new EditorCurveBinding{path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"};
                AnimationUtility.SetObjectReferenceCurve(clip,binding,keys);
                var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=sheet<2;
                AnimationUtility.SetAnimationClipSettings(clip,settings);
                if(fresh)AssetDatabase.CreateAsset(clip,path);else EditorUtility.SetDirty(clip);
            }
        }

        [MenuItem("Dead Zone/Rem Oni/Check animation sheets")]
        public static void Check()
        {
            int total = 0;
            for (int s = 0; s < 4; s++)
            {
                string path = "Assets/Resources/RemOni" + Names[s] + ".png";
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (importer == null || importer.filterMode != FilterMode.Point || importer.mipmapEnabled ||
                    importer.textureCompression != TextureImporterCompression.Uncompressed) throw new Exception("Invalid sprite import: " + path);
                int sheetCount=0;
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var pixels=texture.GetPixels32();
                foreach(var pixel in pixels)if(pixel.a!=0&&pixel.a!=255)throw new Exception("Non-pixel alpha: "+path);
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path)) if (asset is Sprite sprite)
                {
                    if (sprite.rect.size != Vector2.one * ZoneRemArt.CellSize ||
                        Vector2.Distance(sprite.pivot,new Vector2(96,40)) > .01f) throw new Exception("Unstable cell/pivot: " + sprite.name);
                    int opaque=0;var rect=sprite.rect;
                    for(int y=(int)rect.y;y<(int)rect.yMax;y++)for(int x=(int)rect.x;x<(int)rect.xMax;x++)
                    {
                        if(pixels[y*texture.width+x].a==0)continue;
                        if(x==rect.x||x==rect.xMax-1||y==rect.y||y==rect.yMax-1)throw new Exception("Clipped border: "+sprite.name);
                        opaque++;
                    }
                    if(opaque<500)throw new Exception("Incomplete character: "+sprite.name);
                    sheetCount++;total++;
                }
                if(sheetCount!=4*ZoneRemArt.FrameCounts[s])throw new Exception("Frame count: "+path);
                foreach(string direction in new[]{"Up","Down","Left","Right"})
                    if(AssetDatabase.LoadAssetAtPath<AnimationClip>($"Assets/DeadZone/Art/RemOni/Animations/RemOni{Names[s]}{direction}.anim")==null)
                        throw new Exception("Animation clip missing");
            }
            if (total != 80) throw new Exception("Expected exactly 80 sprites, got " + total);
            if (ZoneRemArt.Direction(Vector2.up)!=0 || ZoneRemArt.Direction(Vector2.down)!=1 ||
                ZoneRemArt.Direction(Vector2.left)!=2 || ZoneRemArt.Direction(Vector2.right)!=3) throw new Exception("Direction order");
            if (ZoneRemArt.AttackFrame(ZoneRemRage.SwingDuration-ZoneRemRage.ImpactDelay)!=3) throw new Exception("Impact timing");
            Debug.Log("DZ_REM_ART_PASS: 80 sprites; 192px cells; common feet pivots; point filtering; four directions; impact synchronized");
        }
    }
}
