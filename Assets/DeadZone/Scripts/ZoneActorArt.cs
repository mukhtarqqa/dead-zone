using UnityEngine;
namespace DeadZone
{
    // Six columns, six rows: each hero occupies two rows, with three frames per direction.
    public static class ZoneActorArt
    {
        static Sprite[,,] frames;
        static bool loaded;
        static Sprite[,] castFrames;
        public static Sprite Cast(DeadZoneGame game, Vector2 look, float remaining)
        {
            if(castFrames==null)
            {
                var atlas=Resources.Load<Texture2D>("GojoCast");if(atlas==null)return Frame(game,2,look,false);
                var pixels=atlas.GetPixels32();int w=atlas.width/4,h=atlas.height/4;castFrames=new Sprite[4,4];
                for(int d=0;d<4;d++)for(int f=0;f<4;f++)castFrames[d,f]=Extract(atlas,pixels,w,h,d,f,1.65f);
            }
            float progress=1-Mathf.Clamp01(remaining/ZonePurple.ChargeTime);
            int pose=progress<.3f?0:progress<.65f?1:progress<.94f?2:3;
            return castFrames[Facing(look),pose];
        }
        static Sprite Extract(Texture2D atlas,Color32[] pixels,int w,int h,int col,int row,float size)
        {
            int ox=col*w,oy=(atlas.height/h-1-row)*h,minX=w,minY=h,maxX=0,maxY=0,best=0;
            var visited=new bool[w*h]; var queue=new int[w*h];
            for(int seed=0;seed<w*h;seed++)
            {
                if(visited[seed] || pixels[(oy+seed/w)*atlas.width+ox+seed%w].a<=64) continue;
                int head=0,tail=1,l=w,b=h,r=0,t=0;queue[0]=seed;visited[seed]=true;
                while(head<tail)
                {
                    int at=queue[head++],x=at%w,y=at/w;l=Mathf.Min(l,x);r=Mathf.Max(r,x);b=Mathf.Min(b,y);t=Mathf.Max(t,y);
                    for(int n=0;n<4;n++)
                    {
                        int nx=x+(n==0?1:n==1?-1:0),ny=y+(n==2?1:n==3?-1:0);
                        if(nx<0||nx>=w||ny<0||ny>=h)continue;int next=ny*w+nx;
                        if(visited[next]||pixels[(oy+ny)*atlas.width+ox+nx].a<=64)continue;
                        visited[next]=true;queue[tail++]=next;
                    }
                }
                if(tail>best){best=tail;minX=l;minY=b;maxX=r;maxY=t;}
            }
            return Sprite.Create(atlas,new Rect(ox+minX,oy+minY,maxX-minX+1,maxY-minY+1),new Vector2(.5f,0),h/size);
        }
        public static int Facing(Vector2 look) => Mathf.Abs(look.x) > Mathf.Abs(look.y) ? look.x > 0 ? 1 : 3 : look.y > 0 ? 2 : 0;
        public static bool Flip(int hero, Vector2 look) => false;
        public static Sprite Frame(DeadZoneGame game, int hero, Vector2 look, bool moving)
        {
            if (!loaded)
            {
                loaded = true;
                var atlas = Resources.Load<Texture2D>("DeadZoneDirections");
                if (atlas != null)
                {
                    atlas.filterMode = FilterMode.Point; frames = new Sprite[3,4,3];
                    int w=atlas.width/6,h=atlas.height/6;
                    var pixels = atlas.GetPixels32();
                    for(int c=0;c<3;c++) for(int d=0;d<4;d++) for(int f=0;f<3;f++)
                    {
                        int artDirection = c == 1 && (d == 1 || d == 3) ? 4-d : d;
                        int row=c*2+artDirection/2, col=(artDirection%2)*3+f;
                        frames[c,d,f] = Extract(atlas,pixels,w,h,col,row,1.5f);
                    }
                }
            }
            int frame=moving ? 1+Mathf.FloorToInt(Time.time*8)%2 : 0;
            return frames != null && hero < 3 ? frames[Mathf.Clamp(hero,0,2),Facing(look),frame] : game.CharacterFrame(hero,frame);
        }
    }
}
// Directional atlas revision 2026-10-22.
