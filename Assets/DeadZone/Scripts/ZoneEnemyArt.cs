using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadZone
{
    // Alpha components keep the complete pose even when an illustrated sheet drifts across its nominal grid.
    public static class ZoneEnemyArt
    {
        static readonly string[] Names={"EnemyWalkerDirections","EnemyRangerDirections","EnemyRunnerDirections","EnemyBruteDirections"};
        static readonly int[] Rows={6,5,6,6};
        static readonly Sprite[][,] Sheets=new Sprite[4][,];
        static readonly int[] Counts=new int[4];
        public static int PoseCount(int role){Load(Mathf.Clamp(role,0,3));return Counts[Mathf.Clamp(role,0,3)];}
        public static int Role(int kind,bool boss)=>boss?(kind==0?2:kind==1?1:kind==2?3:kind==3?0:3):Mathf.Clamp(kind,0,3);
        public static Color Tint(int kind,bool boss)=>!boss?Color.white:kind==0?new Color(1,.8f,.82f):kind==1?new Color(.8f,.92f,1):kind==2?new Color(1,.8f,.65f):kind==3?new Color(.82f,.62f,1):new Color(.67f,.86f,1);
        public static Sprite Frame(int kind,int facing,int pose,bool boss=false)
        {
            int role=Role(kind,boss);Load(role);if(Sheets[role]==null)return null;
            // The guard's raised-rifle step also serves as its firing pose.
            int row=role==1?(pose>=4?pose-1:pose==3?2:pose):pose;
            return Sheets[role][Mathf.Clamp(row,0,Rows[role]-1),Mathf.Clamp(facing,0,3)];
        }
        static void Load(int role)
        {
            if(Sheets[role]!=null)return;var texture=Resources.Load<Texture2D>(Names[role]);if(texture==null)return;
            int width=texture.width,height=texture.height;var pixels=texture.GetPixels32();var visited=new bool[pixels.Length];var queue=new int[pixels.Length];var boxes=new List<RectInt>();
            for(int seed=0;seed<pixels.Length;seed++)
            {
                if(visited[seed]||pixels[seed].a<=96)continue;
                int head=0,tail=1,minX=width,minY=height,maxX=0,maxY=0;queue[0]=seed;visited[seed]=true;
                while(head<tail)
                {
                    int at=queue[head++],x=at%width,y=at/width;minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);
                    for(int d=0;d<4;d++)
                    {
                        int nx=x+(d==0?1:d==1?-1:0),ny=y+(d==2?1:d==3?-1:0);if(nx<0||ny<0||nx>=width||ny>=height)continue;
                        int next=ny*width+nx;if(visited[next]||pixels[next].a<=96)continue;visited[next]=true;queue[tail++]=next;
                    }
                }
                if(tail>Mathf.Max(900,pixels.Length/1600))boxes.Add(new RectInt(minX,minY,maxX-minX+1,maxY-minY+1));
            }
            Counts[role]=boxes.Count;
            if(boxes.Count!=Rows[role]*4)throw new InvalidOperationException(Names[role]+": expected "+Rows[role]*4+" complete silhouettes, found "+boxes.Count);
            boxes.Sort((a,b)=>b.center.y.CompareTo(a.center.y));
            for(int row=0;row<Rows[role];row++)boxes.Sort(row*4,4,Comparer<RectInt>.Create((a,b)=>a.center.x.CompareTo(b.center.x)));
            var standing=new List<int>();for(int i=0;i<12;i++)standing.Add(boxes[i].height);standing.Sort();
            float ppu=standing[standing.Count/2]/(role==3?1.95f:role==2?1.6f:1.65f);
            var sheet=new Sprite[Rows[role],4];
            for(int row=0;row<Rows[role];row++)for(int col=0;col<4;col++)
            {
                var b=boxes[row*4+col];int left=Mathf.Max(0,b.xMin-1),bottom=Mathf.Max(0,b.yMin-1),right=Mathf.Min(width,b.xMax+1),top=Mathf.Min(height,b.yMax+1);
                sheet[row,col]=Sprite.Create(texture,new Rect(left,bottom,right-left,top-bottom),new Vector2(.5f,0),ppu);sheet[row,col].name=Names[role]+" "+row+":"+col;
            }
            Sheets[role]=sheet;
        }
    }
}
