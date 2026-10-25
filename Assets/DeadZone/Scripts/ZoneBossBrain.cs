using UnityEngine;
namespace DeadZone
{
    public sealed class ZoneBossBrain : MonoBehaviour
    {
        public ZoneEnemy Enemy;
        float attackAt, summonAt, burstAt, dashAt, dashEnd,shootAt,initialHealth;Vector2 dashDirection,shootDirection;
        public bool Enraged {get;private set;}
        public Vector2 Drive(Vector2 fallback)
        {
            if(dashAt>Time.time||burstAt>Time.time||shootAt>Time.time)return Vector2.zero;
            if(Time.time<dashEnd)return dashDirection*(Enraged?10:8);
            return fallback;
        }
        public void Tick(ZonePlayer target)
        {
            var g=Enemy.game;Vector2 origin=transform.position,delta=(Vector2)target.transform.position-origin;
            if(initialHealth<=0)initialHealth=Enemy.MaxHealth;
            if(!Enraged&&Enemy.health<=initialHealth*.5f){Enraged=true;attackAt=Mathf.Max(attackAt,Time.time+1);ZonePulse.Spawn(origin,gameObject.layer,2,5,1);ZoneFeedback.Play(11);}
            bool same=target.gameObject.layer==gameObject.layer;
            if(!same)return;
            if(delta.magnitude<.85f)target.Hit(Enemy.kind==0&&Time.time<dashEnd?32:22);
            if(burstAt>0&&Time.time>=burstAt)
            {
                burstAt=0;foreach(var player in g.players)if(player.gameObject.layer==gameObject.layer&&Vector2.Distance(origin,player.transform.position)<3&&ZoneCombat.Clear(origin,player.transform.position,gameObject.layer))player.Hit(32);
                ZonePulse.Spawn(origin,gameObject.layer,3,0,.45f);
            }
            if(shootAt>0&&Time.time>=shootAt){shootAt=0;Fire(origin,shootDirection);}
            if(Enemy.kind==3&&Time.time>=summonAt)
            {
                summonAt=Time.time+(Enraged?5.5f:7);
                if(g.enemies.Count<g.Balance.maxAlive+5)for(int i=0;i<3;i++)g.SpawnEnemy(i%3,false);
                ZonePulse.Spawn(origin,gameObject.layer,1.5f,7,.8f);
            }
            if(Time.time<attackAt||delta.magnitude>12)return;
            attackAt=Time.time+(Enemy.kind==0?4:Enemy.kind==2?3.5f:2)*(Enraged?.78f:1);
            if(Enemy.kind==0)
            {
                dashDirection=delta.normalized;dashAt=Time.time+.8f;dashEnd=dashAt+.55f;
                ZonePulse.Spawn(origin,gameObject.layer,5,13,.8f,dashDirection);return;
            }
            if(Enemy.kind==2&&delta.magnitude<5)
            {
                burstAt=Time.time+1;ZonePulse.Spawn(origin,gameObject.layer,3,1,1);return;
            }
            shootDirection=delta.normalized;shootAt=Time.time+.65f;
            ZonePulse.Spawn(origin,gameObject.layer,Enemy.kind==4?2:7,Enemy.kind==4?5:13,.65f,shootDirection);
        }
        void Fire(Vector2 origin,Vector2 direction)
        {
            var g=Enemy.game;
            int count=Enemy.kind==1?(Enraged?9:7):Enemy.kind==4?(Enraged?20:16):5;
            for(int i=0;i<count;i++)
            {
                Vector2 dir=Enemy.kind==4?Quaternion.Euler(0,0,i*360f/count+Time.time*17)*Vector2.right:Quaternion.Euler(0,0,(i-(count-1)*.5f)*11)*direction;
                g.Shoot(origin+dir*.5f,dir,Enemy.kind==4?13:15,true,Enemy.kind==1?7:5,floor:gameObject.layer);
            }
        }
    }
}
