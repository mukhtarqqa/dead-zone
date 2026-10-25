using UnityEngine;
namespace DeadZone
{
    public sealed class ZoneShrine : MonoBehaviour
    {
        public const float ChargeTime=.8f,Duration=8,Radius=5.2f,Damage=40,Interval=.65f;
        public ZonePlayer Owner {get;private set;}
        public int Floor {get;private set;}
        public float Age=>Time.time-started;
        public bool Replica {get;private set;}
        float started,nextTick;LineRenderer ring,roof,gate;readonly LineRenderer[] cuts=new LineRenderer[8];
        public static ZoneShrine Spawn(ZonePlayer actor)
        {
            var obj=new GameObject("Sukuna · Malevolent Shrine");obj.transform.position=actor.transform.position;
            var shrine=obj.AddComponent<ZoneShrine>();shrine.Owner=actor;shrine.Floor=actor.gameObject.layer;shrine.started=Time.time;shrine.nextTick=ChargeTime;shrine.Initialize();ZoneFeedback.Play(10);return shrine;
        }
        public void Apply(ZoneObjectState state)
        {
            Replica=true;Floor=state.floor;transform.position=state.position;started=Time.time-state.age;if(ring==null)Initialize();
        }
        LineRenderer Line(string name,int count,float width,Color color,int order,bool loop=false)
        {
            var obj=new GameObject(name);obj.transform.SetParent(transform,false);var line=obj.AddComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=count;line.startWidth=line.endWidth=width;line.sharedMaterial=ZoneImpact.Material;line.startColor=line.endColor=color;line.loop=loop;line.sortingLayerID=ZoneMap.Sorting(Floor);line.sortingOrder=order;return line;
        }
        void Initialize()
        {
            gameObject.layer=Floor;
            ring=Line("Cursed boundary",65,.06f,new Color(1,.15f,.25f,.55f),-1,true);
            roof=Line("Shrine roof",5,.16f,new Color(.8f,.15f,.2f),18);gate=Line("Shrine pillars",8,.13f,new Color(.6f,.06f,.12f),17);
            for(int i=0;i<cuts.Length;i++)cuts[i]=Line("Cleave "+i,3,.08f,new Color(1,.8f,.8f,.8f),115);
        }
        void Update()
        {
            var game=DeadZoneGame.Instance;
            if(game==null||!game.running||Age>ChargeTime+Duration){Destroy(gameObject);return;}
            if(!Replica)
            {
                if(Owner==null||Owner.Dead||Owner.Downed||Owner.CharacterIndex!=3||Owner.gameObject.layer!=Floor){Destroy(gameObject);return;}
                if(Age>=nextTick)
                {
                    nextTick+=Interval;
                    foreach(var enemy in game.enemies.ToArray())if(enemy!=null&&enemy.gameObject.layer==Floor&&Vector2.Distance(enemy.transform.position,transform.position)<=Radius&&ZoneCombat.Clear(transform.position,enemy.transform.position,Floor))enemy.Hit(Damage,Owner,false);
                }
            }
            if(ring==null)return;float buildup=Mathf.Clamp01(Age/ChargeTime),fade=Mathf.Clamp01((ChargeTime+Duration-Age)/.5f);
            for(int i=0;i<65;i++){float a=i*Mathf.PI*2/64;ring.SetPosition(i,new Vector3(Mathf.Cos(a),Mathf.Sin(a))*Radius*buildup);}
            roof.SetPosition(0,new Vector3(-1.2f,1.2f));roof.SetPosition(1,new Vector3(-.75f,1.35f));roof.SetPosition(2,new Vector3(0,1.8f));roof.SetPosition(3,new Vector3(.75f,1.35f));roof.SetPosition(4,new Vector3(1.2f,1.2f));
            Vector3[] pillars={new Vector3(-.8f,1.2f),new Vector3(-.8f,0),new Vector3(-.4f,0),new Vector3(-.4f,.8f),new Vector3(.4f,.8f),new Vector3(.4f,0),new Vector3(.8f,0),new Vector3(.8f,1.2f)};gate.SetPositions(pillars);
            ring.startColor=ring.endColor=new Color(1,.1f,.18f,(.35f+Mathf.Sin(Age*8)*.15f)*fade);
            float pulse=Mathf.Repeat(Mathf.Max(0,Age-ChargeTime)/Interval,1);
            for(int i=0;i<cuts.Length;i++)
            {
                cuts[i].enabled=Age>=ChargeTime;float angle=i*2.39996f+Mathf.Floor(Age/Interval)*.8f;Vector2 dir=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));Vector2 normal=new Vector2(-dir.y,dir.x);Vector2 center=normal*Mathf.Sin(i*8.7f)*Radius*.55f;
                cuts[i].SetPosition(0,center-dir*Radius*.75f);cuts[i].SetPosition(1,center+normal*.16f);cuts[i].SetPosition(2,center+dir*Radius*.75f);
                cuts[i].startColor=cuts[i].endColor=new Color(1,.65f,.7f,(1-pulse)*fade*.7f);cuts[i].startWidth=cuts[i].endWidth=.04f+(1-pulse)*.06f;
            }
            roof.transform.localScale=gate.transform.localScale=Vector3.one*buildup;
        }
        void OnDestroy(){if(!Replica&&Owner!=null)Owner.EndShrine();}
    }
}
