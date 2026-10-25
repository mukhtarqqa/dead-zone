using System.Collections.Generic;
using UnityEngine;
namespace DeadZone
{
    public sealed class ZoneCorpse : MonoBehaviour
    {
        public static readonly List<ZoneCorpse> Active=new();
        public int Kind,Facing;public bool Boss;float born;SpriteRenderer sprite;
        public float Age=>Time.time-born;
        public static void Spawn(ZoneEnemy enemy)
        {
            while(Active.Count>=24){var old=Active[0];Active.RemoveAt(0);if(old!=null)Destroy(old.gameObject);}
            var obj=new GameObject("Fallen enemy");obj.layer=enemy.gameObject.layer;obj.transform.position=enemy.transform.position;obj.transform.localScale=enemy.transform.localScale;
            var corpse=obj.AddComponent<ZoneCorpse>();corpse.Kind=enemy.kind;corpse.Facing=enemy.Facing;corpse.Boss=enemy.boss;corpse.born=Time.time;Active.Add(corpse);
        }
        public void Apply(ZoneObjectState state){Kind=state.kind;Facing=state.facing;Boss=state.boss;born=Time.time-state.age;transform.position=state.position;transform.localScale=Vector3.one*state.scale;}
        void Update()
        {
            if(sprite==null){sprite=GetComponent<SpriteRenderer>();if(sprite==null)sprite=gameObject.AddComponent<SpriteRenderer>();}
            sprite.sprite=ZoneEnemyArt.Frame(Kind,Facing,Age<.24f?4:5,Boss);sprite.sortingLayerID=ZoneMap.Sorting(gameObject.layer);sprite.sortingOrder=ZoneDepth.Order(transform.position.y)-2;
            var c=ZoneEnemyArt.Tint(Kind,Boss);c.a=Mathf.Clamp01((8-Age)/2);sprite.color=c;if(Age>=8)Destroy(gameObject);
        }
        void OnDestroy(){Active.Remove(this);}
    }
}
