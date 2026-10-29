using System.Collections.Generic;
using TMPro;
using UnityEngine;
namespace DeadZone
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ZoneMinimap : UnityEngine.UI.MaskableGraphic
    {
        DeadZoneGame game;CityArena arena;readonly List<Rect> obstacles=new();TextMeshProUGUI area,clock;float next;
        public void Initialize(DeadZoneGame owner,Transform parent,TMP_FontAsset font)
        {
            game=owner;arena=FindAnyObjectByType<CityArena>();raycastTarget=false;if(arena==null)return;
            foreach(var collider in FindObjectsByType<Collider2D>())if(!collider.isTrigger&&collider.GetComponentInParent<ZonePlayer>()==null&&collider.GetComponentInParent<ZoneEnemy>()==null){var b=collider.bounds;obstacles.Add(new Rect(b.min.x,b.min.y,b.size.x,b.size.y));}
            area=ZoneUi.Text(parent,"District name",new Vector2(0,1),new Vector2(14,-12),new Vector2(248,26),font,17);area.alignment=TextAlignmentOptions.Center;
            clock=ZoneUi.Text(parent,"Match clock",Vector2.zero,new Vector2(14,10),new Vector2(248,24),font,15);clock.alignment=TextAlignmentOptions.Center;clock.color=ZoneUi.Muted;
        }
        void Update()
        {
            if(game==null||arena==null)return;
            if(Time.unscaledTime<next)return;
            SetVerticesDirty();next=Time.unscaledTime+.1f;
            if(game.CameraActor==null)return;
            area.text=ZoneL.Get(arena.AreaName(game.CameraActor.transform.position));float seconds=Mathf.Max(0,game.Elapsed);clock.text=$"{Mathf.FloorToInt(seconds/60):00}:{Mathf.FloorToInt(seconds%60):00}"+"  ·  "+ZoneL.Format("ИГРОКИ {0}/4",game.TeamSize);
        }
        Vector2 Map(Vector2 world)
        {
            var rect=rectTransform.rect;float scale=Mathf.Min(rect.width/arena.Bounds.width,rect.height/arena.Bounds.height);return rect.center+(world-arena.Bounds.center)*scale;
        }
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();if(arena==null||game==null)return;Box(vh,arena.Bounds,ZonePalette.Panel);foreach(var district in arena.Districts)Box(vh,district.bounds,Color.Lerp(ZonePalette.Panel,district.color,.32f));
            Road(vh,new[]{new Vector2(-54,0),new Vector2(25,0),new Vector2(40,7),new Vector2(54,7)},8);Road(vh,new[]{new Vector2(0,-42),new Vector2(0,-17),new Vector2(5,-3),new Vector2(5,17),new Vector2(-4,29),new Vector2(-4,42)},6.5f);Road(vh,new[]{new Vector2(-49,-38),new Vector2(48,-38)},4);
            foreach(var obstacle in obstacles)Box(vh,obstacle,new Color(.035f,.045f,.033f));
            if(game.IsAuthority){foreach(var cache in ZoneSupplyCache.Active)if(cache!=null&&!cache.Opened(game.player))Dot(vh,cache.transform.position,2,ZonePalette.Accent);}
            else foreach(var ghost in ZoneGhost.Active)if(ghost!=null&&ghost.ObjectType==10)Dot(vh,ghost.transform.position,2,ZonePalette.Accent);
            foreach(var actor in game.players)
            {
                if(actor==null)continue;
                bool connected=game.IsAuthority?game.network==null||game.network.IsParticipantConnected(actor.OwnerId):actor.Connected;Color c=actor.Dead||!connected?ZoneUi.Muted:actor.Downed?new Color(1,.4f,.3f):ZoneHeroCard.HeroColor(actor.CharacterIndex);Dot(vh,actor.transform.position,actor==game.player?4.2f:3.3f,c);
            }
            if(game.running)foreach(var target in game.BossTargets)if(game.BossIsAlive(target))BossMarker(vh,target.position);
        }
        public Vector2 BossMarkerPosition(Vector2 world,out bool edge)
        {
            if(arena==null){edge=false;return Vector2.zero;}
            Vector2 min=Map(arena.Bounds.min)+Vector2.one*10,max=Map(arena.Bounds.max)-Vector2.one*10;
            Vector2 center=(min+max)*.5f,half=(max-min)*.5f,delta=Map(world)-center;
            float ratio=Mathf.Max(Mathf.Abs(delta.x)/Mathf.Max(1,half.x),Mathf.Abs(delta.y)/Mathf.Max(1,half.y));
            edge=ratio>1;return center+delta/Mathf.Max(1,ratio);
        }
        void BossMarker(UnityEngine.UI.VertexHelper vh,Vector2 world)
        {
            var p=BossMarkerPosition(world,out bool edge);float pulse=.5f+.5f*Mathf.Sin(Time.unscaledTime*5);
            Diamond(vh,p,8+pulse,new Color(1,.10f,.15f,.18f+pulse*.16f));
            Diamond(vh,p,6,new Color(.10f,.02f,.03f));
            if(edge)
            {
                Vector2 direction=(Map(world)-rectTransform.rect.center).normalized,normal=new Vector2(-direction.y,direction.x);
                int start=vh.currentVertCount;var v=UnityEngine.UIVertex.simpleVert;v.color=new Color(1,.16f,.20f);
                v.position=p+direction*6;vh.AddVert(v);v.position=p-direction*4+normal*4;vh.AddVert(v);v.position=p-direction*4-normal*4;vh.AddVert(v);vh.AddTriangle(start,start+1,start+2);
            }
            else Diamond(vh,p,4.5f,new Color(1,.16f,.20f));
            Diamond(vh,p,1.4f,new Color(1,.88f,.84f));
        }
        void Diamond(UnityEngine.UI.VertexHelper vh,Vector2 p,float radius,Color c)
        {Quad(vh,p+Vector2.up*radius,p+Vector2.right*radius,p+Vector2.down*radius,p+Vector2.left*radius,c);}
        void Box(UnityEngine.UI.VertexHelper vh,Rect world,Color c){Quad(vh,Map(world.min),Map(new Vector2(world.xMax,world.yMin)),Map(world.max),Map(new Vector2(world.xMin,world.yMax)),c);}
        void Dot(UnityEngine.UI.VertexHelper vh,Vector2 point,float radius,Color c){var p=Map(point);Quad(vh,p+new Vector2(-radius,-radius),p+new Vector2(radius,-radius),p+new Vector2(radius,radius),p+new Vector2(-radius,radius),c);}
        void Road(UnityEngine.UI.VertexHelper vh,Vector2[] points,float width)
        {
            float scale=Mathf.Min(rectTransform.rect.width/arena.Bounds.width,rectTransform.rect.height/arena.Bounds.height);
            for(int i=1;i<points.Length;i++){Vector2 a=Map(points[i-1]),b=Map(points[i]),d=(b-a).normalized,n=new Vector2(-d.y,d.x)*width*scale*.5f;Quad(vh,a-n,b-n,b+n,a+n,new Color(.40f,.37f,.31f));}
        }
        static void Quad(UnityEngine.UI.VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color color)
        {
            int start=vh.currentVertCount;var v=UnityEngine.UIVertex.simpleVert;v.color=color;v.position=a;vh.AddVert(v);v.position=b;vh.AddVert(v);v.position=c;vh.AddVert(v);v.position=d;vh.AddVert(v);vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }
    }
}
