using UnityEngine;
namespace DeadZone
{
    // Feet collide with the level; this trigger receives shots across the visible body.
    public sealed class ZoneHurtbox : MonoBehaviour
    {
        public ZoneEnemy Enemy;public ZonePlayer Player;CapsuleCollider2D shape;
        public static ZoneHurtbox Add(ZoneEnemy enemy)
        {
            var box=Create(enemy.gameObject,ZoneEnemyArt.Role(enemy.kind,enemy.boss)==3);box.Enemy=enemy;return box;
        }
        public static ZoneHurtbox Add(ZonePlayer player){var box=Create(player.gameObject,false);box.Player=player;return box;}
        static ZoneHurtbox Create(GameObject actor,bool heavy)
        {
            var existing=actor.GetComponentInChildren<ZoneHurtbox>();if(existing!=null)return existing;
            var obj=new GameObject("Body hit area");obj.layer=actor.layer;obj.transform.SetParent(actor.transform,false);var capsule=obj.AddComponent<CapsuleCollider2D>();capsule.isTrigger=true;capsule.direction=CapsuleDirection2D.Vertical;capsule.offset=new Vector2(0,heavy?.95f:.78f);capsule.size=new Vector2(heavy?1.05f:.72f,heavy?1.8f:1.45f);capsule.enabled=false;
            var box=obj.AddComponent<ZoneHurtbox>();box.shape=capsule;return box;
        }
        void Update()
        {
            bool live=Enemy!=null?Enemy.game.running&&Enemy.health>0&&Time.time>=Enemy.ReadyAt:Player!=null&&Player.game.running&&!Player.Dead&&!Player.Downed;
            if(shape.enabled!=live)shape.enabled=live;if(gameObject.layer!=transform.parent.gameObject.layer)gameObject.layer=transform.parent.gameObject.layer;
        }
    }
}
