using UnityEngine;
namespace DeadZone
{
    public sealed class ZoneShrine : MonoBehaviour
    {
        public const float ChargeTime=ZoneHeroVoice.ShrineRevealTime,Duration=8,Radius=5.2f,Damage=40,Interval=.65f;
        public ZonePlayer Owner {get;private set;}
        public int Floor {get;private set;}
        public float Age=>Time.time-started;
        public bool Replica {get;private set;}
        float started,nextTick;ZoneShrineView view;
        public static ZoneShrine Spawn(ZonePlayer actor)
        {
            var obj=new GameObject("Sukuna · Malevolent Shrine");obj.transform.position=actor.transform.position;
            var shrine=obj.AddComponent<ZoneShrine>();shrine.Owner=actor;shrine.Floor=actor.gameObject.layer;shrine.started=Time.time;shrine.nextTick=ChargeTime;shrine.Initialize();ZoneFeedback.Play(12);return shrine;
        }
        public void Apply(ZoneObjectState state){Replica=true;Floor=state.floor;transform.position=state.position;started=Time.time-state.age;if(view==null)Initialize();}
        void Initialize(){gameObject.layer=Floor;view=gameObject.AddComponent<ZoneShrineView>();view.Shrine=this;}
        void Update()
        {
            var game=DeadZoneGame.Instance;if(game==null||!game.running||Age>ChargeTime+Duration){Destroy(gameObject);return;}if(game.paused)return;
            if(Replica)return;if(Owner==null||Owner.Dead||Owner.Downed||Owner.CharacterIndex!=3||Owner.gameObject.layer!=Floor){Destroy(gameObject);return;}
            if(Age<nextTick)return;nextTick+=Interval;
            foreach(var enemy in game.enemies.ToArray())if(enemy!=null&&enemy.gameObject.layer==Floor&&Vector2.Distance(enemy.transform.position,transform.position)<=Radius&&ZoneCombat.Clear(transform.position,enemy.transform.position,Floor))enemy.Hit(Damage,Owner,false);
        }
        void OnDestroy(){if(!Replica&&Owner!=null)Owner.EndShrine();}
    }
}
