using System;
using System.Collections.Generic;
using UnityEngine;
namespace DeadZone
{
    public static class ZoneShrineArt
    {
        static Sprite[] frames;
        public static Sprite Frame(int index)
        {
            if(frames==null)Load();return frames==null?null:frames[Mathf.Clamp(index,0,3)];
        }
        static void Load()
        {
            var texture=Resources.Load<Texture2D>("SukunaShrineStages");if(texture==null)return;var pixels=texture.GetPixels32();int w=texture.width,h=texture.height;var seen=new bool[pixels.Length];var queue=new int[pixels.Length];var boxes=new List<RectInt>();
            for(int seed=0;seed<pixels.Length;seed++)
            {
                if(seen[seed]||pixels[seed].a<=96)continue;int head=0,tail=1,l=w,b=h,r=0,t=0;queue[0]=seed;seen[seed]=true;
                while(head<tail){int at=queue[head++],x=at%w,y=at/w;l=Mathf.Min(l,x);r=Mathf.Max(r,x);b=Mathf.Min(b,y);t=Mathf.Max(t,y);for(int d=0;d<4;d++){int nx=x+(d==0?1:d==1?-1:0),ny=y+(d==2?1:d==3?-1:0);if(nx<0||ny<0||nx>=w||ny>=h)continue;int next=ny*w+nx;if(seen[next]||pixels[next].a<=96)continue;seen[next]=true;queue[tail++]=next;}}
                if(tail>10000)boxes.Add(new RectInt(l,b,r-l+1,t-b+1));
            }
            if(boxes.Count!=4)throw new InvalidOperationException("Shrine art expected four connected stages, found "+boxes.Count);
            boxes.Sort((a,b)=>b.center.y.CompareTo(a.center.y));for(int row=0;row<2;row++)boxes.Sort(row*2,2,Comparer<RectInt>.Create((a,b)=>a.center.x.CompareTo(b.center.x)));
            float ppu=(boxes[0].width+boxes[1].width)*.5f/4.8f;frames=new Sprite[4];for(int i=0;i<4;i++){var rect=boxes[i];frames[i]=Sprite.Create(texture,new Rect(rect.x,rect.y,rect.width,rect.height),new Vector2(.5f,0),ppu);frames[i].name="Malevolent Shrine stage "+i;}
        }
    }
}
