using UnityEngine;
namespace DeadZone
{
    public sealed class ZoneBossBrain : MonoBehaviour
    {
        public ZoneEnemy Enemy;
        float attackAt, summonAt, burstAt, dashAt, dashEnd;Vector2 dashDirection;
        public Vector2 Drive(Vector2 fallback)
        {
            if(dashAt>Time.time)return Vector2.zero;
            if(Time.time<dashEnd)return dashDirection*8;
            return fallback;
        }
        public void Tick(ZonePlayer target)
        {
            var g=Enemy.game;Vector2 origin=transform.position,delta=(Vector2)target.transform.position-origin;
            bool same=target.gameObject.layer==gameObject.layer;
            if(!same)return;
            if(delta.magnitude<.85f)target.Hit(Enemy.kind==0&&Time.time<dashEnd?32:22);
            if(burstAt>0&&Time.time>=burstAt)
            {
                burstAt=0;foreach(var player in g.players)if(player.gameObject.layer==gameObject.layer&&Vector2.Distance(origin,player.transform.position)<3&&ZoneCombat.Clear(origin,player.transform.position,gameObject.layer))player.Hit(32);
                ZonePulse.Spawn(origin,gameObject.layer,3,0,.45f);
            }
            if(Enemy.kind==3&&Time.time>=summonAt)
            {
                summonAt=Time.time+7;
                if(g.enemies.Count<g.Balance.maxAlive+5)for(int i=0;i<3;i++)g.SpawnEnemy(i%3,false);
                ZonePulse.Spawn(origin,gameObject.layer,1.5f,7,.8f);
            }
            if(Time.time<attackAt||delta.magnitude>12)return;
            attackAt=Time.time+(Enemy.kind==0?4:Enemy.kind==2?3.5f:2);
            if(Enemy.kind==0)
            {
                dashDirection=delta.normalized;dashAt=Time.time+.8f;dashEnd=dashAt+.55f;
                ZonePulse.Spawn(origin,gameObject.layer,5,13,.8f,dashDirection);return;
            }
            if(Enemy.kind==2&&delta.magnitude<5)
            {
                burstAt=Time.time+1;ZonePulse.Spawn(origin,gameObject.layer,3,1,1);return;
            }
            int count=Enemy.kind==1?7:Enemy.kind==4?16:5;
            for(int i=0;i<count;i++)
            {
                Vector2 dir=Enemy.kind==4?Quaternion.Euler(0,0,i*360f/count+Time.time*17)*Vector2.right:Quaternion.Euler(0,0,(i-(count-1)*.5f)*11)*delta.normalized;
                g.Shoot(origin+dir*.5f,dir,Enemy.kind==4?13:15,true,Enemy.kind==1?7:5,floor:gameObject.layer);
            }
        }
    }
}
