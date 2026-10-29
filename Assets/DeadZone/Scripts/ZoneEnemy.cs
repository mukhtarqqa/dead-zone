using UnityEngine;

namespace DeadZone
{
    public sealed class ZoneEnemy : MonoBehaviour
    {
        public DeadZoneGame game;public int kind;public bool boss;public float health,ReadyAt;
        Rigidbody2D body;Collider2D hitbox;SpriteRenderer sprite;Transform healthBar;ZoneBossBrain brain;
        ZonePlayer target;float retargetAt,checkSightAt,nextAttack,attackAt,attackPoseUntil,burstAt,staggerUntil,nextStagger,flashUntil,maxHealth,checkProgressAt,recoverUntil;
        int burstLeft,side;bool clearSight;Vector2 attackDirection=Vector2.down,look=Vector2.down,knockback,lastPosition,recovery,rangedAim;
        static readonly Collider2D[] neighbours=new Collider2D[32];
        public float MaxHealth=>maxHealth>0?maxHealth:health;
        public float StaggerRemaining=>Mathf.Max(0,staggerUntil-Time.time);
        public float HitFlashRemaining=>Mathf.Max(0,flashUntil-Time.time);
        public int ShotsFired{get;private set;}
        public int Facing=>ZoneActorArt.Facing(look);
        public int AnimationPose=>Time.time<attackPoseUntil||attackAt>0||boss&&brain!=null&&brain.Attacking?3:body!=null&&body.linearVelocity.sqrMagnitude>.12f?1+(Mathf.FloorToInt(Time.time*(kind==2?12:7)+(GetEntityId().GetHashCode()%97)*.13f)&1):0;
        public bool HasTarget=>target!=null;
        public Vector2 DamageCenter=>(Vector2)transform.position+Vector2.up*(ZoneEnemyArt.Role(kind,boss)==3?.95f:.78f)*(boss?1.6f:1);
        void OnDestroy(){if(game!=null&&boss)game.RemoveBossAlert(transform);}
        void Start()
        {
            body=GetComponent<Rigidbody2D>();hitbox=GetComponent<Collider2D>();hitbox.offset=Vector2.zero;sprite=GetComponent<SpriteRenderer>();brain=GetComponent<ZoneBossBrain>();maxHealth=health;side=(GetEntityId().GetHashCode()&1)==0?1:-1;ZoneHurtbox.Add(this);
            lastPosition=body.position;checkProgressAt=Time.time+1.2f;nextAttack=ReadyAt+.2f;
            gameObject.AddComponent<ZoneZombieVoice>().Bind(this);
            var bar=new GameObject("Health");bar.transform.SetParent(transform,false);bar.transform.localPosition=new Vector3(0,boss?2:kind==3?2.05f:1.8f);bar.transform.localScale=new Vector3(1,.065f,1);
            var sr=bar.AddComponent<SpriteRenderer>();sr.sprite=game.marker;sr.color=boss?new Color(1,.2f,.45f):new Color(.94f,.38f,.24f);sr.sortingOrder=3100;sr.sortingLayerID=sprite.sortingLayerID;healthBar=bar.transform;
        }
        void Update()
        {
            if(sprite==null)return;var frame=ZoneEnemyArt.Frame(kind,Facing,AnimationPose,boss);sprite.sprite=frame!=null?frame:game.EnemyFrame(kind,boss);sprite.flipX=false;sprite.sortingOrder=ZoneDepth.Order(transform.position.y);
            var color=ZoneEnemyArt.Tint(kind,boss);if(Time.time<flashUntil)color=Color.Lerp(color,new Color(1,.35f,.3f),.65f);
            color.a=Mathf.Clamp01(1-(ReadyAt-Time.time)/(boss?1.8f:.75f));sprite.color=color;
            if(healthBar!=null){healthBar.gameObject.SetActive(game.running&&Time.time>=ReadyAt&&(boss||health<maxHealth));healthBar.localScale=new Vector3(Mathf.Clamp01(health/maxHealth),.065f,1);}
        }
        void FixedUpdate()
        {
            hitbox.enabled=Time.time>=ReadyAt;
            if(!game.running||!game.IsAuthority||game.paused||Time.time<ReadyAt){body.linearVelocity=Vector2.zero;return;}
            if(target==null||target.Dead||target.Downed||game.network!=null&&!game.network.IsParticipantConnected(target.OwnerId)||Time.time>=retargetAt)
            {
                var candidate=game.NearestTarget(body.position);if(target==null||target.Dead||target.Downed||game.network!=null&&!game.network.IsParticipantConnected(target.OwnerId)||candidate==null||(candidate.transform.position-transform.position).sqrMagnitude<(target.transform.position-transform.position).sqrMagnitude*.72f)target=candidate;
                retargetAt=Time.time+.65f;
            }
            if(target==null){body.linearVelocity=Vector2.zero;return;}
            Vector2 delta=(Vector2)target.transform.position-body.position;float distance=delta.magnitude;bool same=target.gameObject.layer==gameObject.layer;
            if(Time.time>=checkSightAt){clearSight=same&&ZoneCombat.Clear(body.position,target.transform.position,gameObject.layer);checkSightAt=Time.time+.12f;}
            if(delta.sqrMagnitude>.01f)look=delta.normalized;
            if(attackAt>0&&Time.time>=attackAt){attackAt=0;ResolveAttack();}
            if(burstLeft>0&&Time.time>=burstAt){FireRound();burstLeft--;burstAt=Time.time+.13f;}
            if(Time.time<staggerUntil){body.linearVelocity=knockback;return;}
            Vector2 direction=ZoneMap.Current!=null?ZoneMap.Current.Direction(body.position,gameObject.layer,target):delta.normalized;
            if(ZoneStairTravel.Step(gameObject,direction))return;
            float speed=boss?2.1f+game.wave*.08f:kind==2?4.15f:kind==3?2:kind==1?2.75f:2.65f;
            if(boss){body.linearVelocity=brain!=null?brain.Drive(direction*speed):direction*speed;brain?.Tick(target);return;}
            if(same&&clearSight)
            {
                if(kind==1)
                {
                    Vector2 tangent=new Vector2(-delta.y,delta.x).normalized*side;
                    direction=distance<4.6f?(-delta.normalized+tangent*.4f).normalized:distance<7.1f?tangent*.6f:direction;
                }
                else if(kind==2&&distance>2&&distance<11)direction=(delta.normalized*.65f+new Vector2(-delta.y,delta.x).normalized*side*.75f).normalized;
                else if(distance<(kind==3?1.55f:.78f))direction=Vector2.zero;
            }
            if(attackAt>0||burstLeft>0)direction=Vector2.zero;
            if(same&&clearSight&&Time.time>=nextAttack&&attackAt<=0&&burstLeft==0&&distance<(kind==1?10:kind==3?1.9f:1.05f))BeginAttack(delta);
            if(attackAt>0)direction=Vector2.zero;
            if(Time.time>=checkProgressAt)
            {
                if(direction.sqrMagnitude>.1f&&Vector2.Distance(body.position,lastPosition)<.16f){side=-side;recovery=OpenDirection(direction);recoverUntil=Time.time+.65f;}
                lastPosition=body.position;checkProgressAt=Time.time+1.1f;
            }
            if(Time.time<recoverUntil&&attackAt<=0)direction=recovery;
            if(direction.sqrMagnitude>.05f)
            {
                direction=(direction+Separation()*.8f).normalized;
                if(!ZoneMap.Clearance(body.position,body.position+direction*.45f,gameObject.layer,.23f))direction=OpenDirection(direction);
            }
            body.linearVelocity=direction*speed;
        }
        Vector2 Separation()
        {
            int count=Physics2D.OverlapCircleNonAlloc(body.position,.85f,neighbours);Vector2 push=Vector2.zero;
            for(int i=0;i<count;i++){var other=neighbours[i].GetComponent<ZoneEnemy>();if(other==null||other==this||other.gameObject.layer!=gameObject.layer)continue;Vector2 away=body.position-(Vector2)other.transform.position;float d=away.magnitude;if(d>.02f)push+=away/d*(1-d/.85f);}
            return Vector2.ClampMagnitude(push,1);
        }
        Vector2 OpenDirection(Vector2 desired)
        {
            Vector2 best=Vector2.zero;float score=-2;
            for(int i=0;i<12;i++){Vector2 candidate=Quaternion.Euler(0,0,(i+side*.3f)*30)*Vector2.right;if(!ZoneMap.Clearance(body.position,body.position+candidate*.7f,gameObject.layer,.24f))continue;float value=Vector2.Dot(candidate,desired.normalized);if(value>score){score=value;best=candidate;}}
            return best;
        }
        void BeginAttack(Vector2 delta)
        {
            float warning=kind==3?.85f:kind==1?.6f:kind==2?.3f:.42f;attackAt=Time.time+warning;attackPoseUntil=attackAt+.23f;attackDirection=delta.normalized;
            rangedAim=target!=null?(Vector2)target.transform.position+target.GetComponent<Rigidbody2D>().linearVelocity*.12f:body.position+delta;
            nextAttack=attackAt+(kind==3?2:kind==1?1.6f:kind==2?.9f:1.1f);
            ZonePulse.Spawn(body.position,gameObject.layer,kind==3?1.9f:kind==1?6:.95f,kind==3?1:13,warning,attackDirection);
        }
        void ResolveAttack()
        {
            if(kind==1){burstLeft=3;burstAt=Time.time;return;}
            float radius=kind==3?2:1.2f;
            foreach(var actor in game.players)
            {
                Vector2 delta=(Vector2)actor.transform.position-body.position;
                if(actor.Dead||actor.Downed||actor.gameObject.layer!=gameObject.layer||delta.magnitude>radius||Vector2.Dot(delta.normalized,attackDirection)<(kind==3?-.1f:.2f)||!ZoneCombat.Clear(body.position,actor.transform.position,gameObject.layer))continue;
                actor.Hit(kind==3?34:kind==2?16:21);if(kind!=3)break;
            }
            if(kind==3){ZonePulse.Spawn(body.position+attackDirection*.65f,gameObject.layer,1.5f,0,.3f);ZoneFeedback.Play(10);}
        }
        void FireRound()
        {
            Vector2 origin=body.position+Vector2.up*.75f+attackDirection*.45f;if(!ZoneMap.Clearance(body.position,origin,gameObject.layer,.02f))origin=body.position;
            Vector2 direction=Quaternion.Euler(0,0,(burstLeft-2)*4)*(rangedAim-origin).normalized;
            game.Shoot(origin,direction,14,true,9.5f,range:14,floor:gameObject.layer);ShotsFired++;attackPoseUntil=Time.time+.22f;
        }
        public void Hit(float damage,ZonePlayer owner=null,bool grantCharge=true)
        {
            if(!game.IsAuthority||!game.running||game.paused||!game.enemies.Contains(this)||health<=0||damage<=0||Time.time<ReadyAt)return;float actual=Mathf.Min(damage,health);health-=damage;
            Vector2 impulse=owner!=null?((Vector2)transform.position-(Vector2)owner.transform.position).normalized:Vector2.up;
            // Automatic fire still feels reactive, but cannot hold an enemy permanently in hit stun.
            if(!boss&&Time.time>=nextStagger){knockback=impulse*(kind==3?.5f:1.8f);staggerUntil=Time.time+(kind==3?.035f:.075f);nextStagger=Time.time+(kind==3?.9f:.38f);}
            ZoneImpact.Spawn(transform.position+Vector3.up*.45f,impulse,gameObject.layer,true);ZoneFeedback.Hit(transform.position,actual,gameObject.layer);flashUntil=Time.time+.09f;game.damageDealt+=actual;
            owner??=game.player;owner.PersonalDamage+=actual;if(grantCharge)owner.charge=Mathf.Min(100,owner.charge+actual*.09f);
            if(health<=0){owner.PersonalKills++;owner.OnEnemyKilled();if(grantCharge)owner.charge=Mathf.Min(100,owner.charge+8);ZoneCorpse.Spawn(this);game.EnemyKilled(this);Destroy(gameObject);}
        }
    }
}
