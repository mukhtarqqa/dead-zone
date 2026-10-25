using System.Collections.Generic;
using UnityEngine;
namespace DeadZone
{
    public sealed class ZoneSupplyCache : MonoBehaviour
    {
        public static readonly List<ZoneSupplyCache> Active=new();
        public int Index;int openedHeroes;SpriteRenderer sprite;
        public int WeaponKind=>1+Index%3;
        public bool Opened(ZonePlayer actor)=>actor!=null&&(openedHeroes&(1<<actor.CharacterIndex))!=0;
        public bool CanOpen(ZonePlayer actor)=>actor!=null&&!actor.Dead&&!actor.Downed&&!Opened(actor)&&actor.gameObject.layer==gameObject.layer&&Vector2.Distance(actor.transform.position,transform.position)<1.5f&&ZoneCombat.Clear(actor.transform.position,transform.position,gameObject.layer);
        public static void Populate(CityArena arena)
        {
            if(arena==null||arena.Bounds.width<100||Active.Count>0)return;
            for(int i=0;i<arena.SupplyPoints.Length;i++)
            {
                var obj=new GameObject("Supply cache "+i);obj.layer=20;obj.transform.position=arena.SupplyPoints[i];obj.transform.localScale=Vector3.one*1.25f;
                var cache=obj.AddComponent<ZoneSupplyCache>();cache.Index=i;Active.Add(cache);
            }
        }
        public void Open(ZonePlayer actor)
        {
            var game=actor.game;if(!game.IsAuthority||!game.running||!CanOpen(actor))return;openedHeroes|=1<<actor.CharacterIndex;
            Vector2 origin=transform.position;
            Vector2 Drop(Vector2 offset){var point=origin+offset;return ZoneMap.Current!=null&&ZoneMap.Current.Walkable(point,gameObject.layer)&&ZoneMap.Clearance(origin,point,gameObject.layer,.16f)?point:origin;}
            ZonePickup.Spawn(game,Drop(Vector2.right*.7f),WeaponKind,actor.OwnerId,floor:gameObject.layer);
            ZonePickup.Spawn(game,Drop(Vector2.left*.7f),-1,actor.OwnerId,floor:gameObject.layer);
            if(Index%4==0)ZonePickup.Spawn(game,Drop(Vector2.down*.7f),4,actor.OwnerId,floor:gameObject.layer);
            ZonePulse.Spawn(origin,gameObject.layer,.9f,2,.45f);ZoneFeedback.Play(9);
        }
        public static ZoneSupplyCache Nearest(ZonePlayer actor)
        {
            ZoneSupplyCache result=null;float distance=1.5f;
            foreach(var cache in Active)if(cache!=null&&cache.CanOpen(actor)){float d=Vector2.Distance(actor.transform.position,cache.transform.position);if(d<distance){result=cache;distance=d;}}
            return result;
        }
        void LateUpdate()
        {
            if(sprite==null){sprite=GetComponent<SpriteRenderer>();if(sprite==null)sprite=gameObject.AddComponent<SpriteRenderer>();sprite.sprite=ZoneArt.Item(2);sprite.sortingLayerID=ZoneMap.Sorting(gameObject.layer);sprite.sortingOrder=ZoneDepth.Order(transform.position.y)-3;}
            var game=DeadZoneGame.Instance;sprite.enabled=game!=null&&game.running&&!Opened(game.player);sprite.color=Color.Lerp(Color.white,new Color(.65f,1,.85f),.3f+.15f*Mathf.Sin(Time.time*2+Index));
        }
        void OnDestroy(){Active.Remove(this);}
    }
}
