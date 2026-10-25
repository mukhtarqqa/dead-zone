using System.Collections.Generic;
using UnityEngine;

namespace DeadZone
{
    // The host sweeps the entire path; replicas render the same cast age and impact point.
    public sealed class ZonePurple : MonoBehaviour
    {
        public const float ChargeTime=1.05f, Speed=14, Range=22, Radius=.8f, Damage=420, BurstDamage=210;
        public ZonePlayer Owner;
        public Vector2 Origin, Direction=Vector2.right;
        public float Age, ImpactAge=-1;
        public bool Replica;
        public int Floor;
        public float Travelled { get; private set; }
        readonly HashSet<ZoneEnemy> struck=new();
        ZonePurpleView view;
        public static ZonePurple Spawn(ZonePlayer owner, Vector2 direction)
        {
            var obj=new GameObject("Gojo Hollow Purple");obj.layer=owner.gameObject.layer;
            var spell=obj.AddComponent<ZonePurple>();spell.Owner=owner;spell.Floor=obj.layer;
            spell.Direction=direction.sqrMagnitude>.001f?direction.normalized:Vector2.right;
            spell.Origin=(Vector2)owner.transform.position+Vector2.up*.55f+spell.Direction*.65f;
            Vector2 root=owner.transform.position,desired=spell.Origin,safe=root;
            int steps=Mathf.CeilToInt(Vector2.Distance(root,desired)/.04f);
            for(int i=1;i<=steps;i++){Vector2 p=Vector2.Lerp(root,desired,(float)i/steps);if(ZoneMap.Obstacle(p,.06f,spell.Floor))break;safe=p;}
            spell.Origin=safe;obj.transform.position=spell.Origin;return spell;
        }
        public void Apply(ZoneObjectState state)
        {
            Replica=true;Floor=state.floor;Origin=state.origin;Direction=state.velocity.normalized;
            Age=state.age;ImpactAge=state.endAge;transform.position=state.position;
        }
        void Start(){view=gameObject.AddComponent<ZonePurpleView>();view.Spell=this;}
        void Update()
        {
            var g=DeadZoneGame.Instance;
            if(g==null||!g.running){Destroy(gameObject);return;}
            if(!Replica && Age<ChargeTime && Owner!=null && (Owner.Dead||Owner.Downed)){Destroy(gameObject);return;}
            if(g.paused)return;
            Tick(Time.deltaTime);
        }
        public void Tick(float delta)
        {
            Age+=delta;
            if(ImpactAge>=0){if(Age-ImpactAge>.4f)Destroy(gameObject);return;}
            if(Age<=ChargeTime)return;
            if(Replica)
            {
                Travelled=Mathf.Clamp((Age-ChargeTime)*Speed,0,Range);
                transform.position=Origin+Direction*Travelled;return;
            }
            Vector2 from=transform.position;
            float step=Mathf.Clamp((Age-ChargeTime)*Speed-Travelled,0,Range-Travelled);
            bool blocked=false;
            foreach(var hit in Physics2D.CircleCastAll(from,Radius*.7f,Direction,step))
            {
                var c=hit.collider;
                if(c.isTrigger||c.GetComponentInParent<ZonePlayer>()!=null||c.GetComponentInParent<ZoneEnemy>()!=null)continue;
                int layer=c.gameObject.layer;
                if((layer>=20&&layer<=22&&layer!=Floor)||Physics2D.GetIgnoreLayerCollision(Floor,layer))continue;
                if(hit.distance<=step){step=hit.distance;blocked=true;}
            }
            Vector2 to=from+Direction*step;
            HitPath(from,to,Radius,Damage);
            transform.position=to;Travelled+=step;
            if(blocked||Travelled>=Range-.001f)
            {
                ImpactAge=Age;HitPath(to,to,1.8f,BurstDamage);
                ZonePulse.Spawn(to,Floor,2,12,.6f);
            }
        }
        void HitPath(Vector2 from,Vector2 to,float radius,float damage)
        {
            var game=DeadZoneGame.Instance;if(game==null||!game.IsAuthority)return;
            Vector2 segment=to-from;float length=segment.sqrMagnitude;
            foreach(var enemy in game.enemies.ToArray())
            {
                if(enemy==null||enemy.health<=0||enemy.gameObject.layer!=Floor||struck.Contains(enemy)||Time.time<enemy.ReadyAt)continue;
                Vector2 p=enemy.transform.position;
                float t=length>.00001f?Mathf.Clamp01(Vector2.Dot(p-from,segment)/length):0;
                if(Vector2.Distance(p,from+segment*t)>radius+(enemy.boss?.4f:.22f))continue;
                // Impact bursts cannot leak through a wall at the end of the flight.
                bool occluded=false;
                foreach(var hit in Physics2D.RaycastAll(from+segment*t,(p-from-segment*t).normalized,Vector2.Distance(p,from+segment*t)))
                {
                    var c=hit.collider;if(c.isTrigger||c.GetComponentInParent<ZonePlayer>()!=null||c.GetComponentInParent<ZoneEnemy>()!=null)continue;
                    int layer=c.gameObject.layer;if((layer>=20&&layer<=22&&layer!=Floor)||Physics2D.GetIgnoreLayerCollision(Floor,layer))continue;
                    occluded=true;break;
                }
                if(occluded)continue;
                struck.Add(enemy);enemy.Hit(damage,Owner,false);
            }
        }
    }

    // Native geometry: layered glow discs, orbiting energy, lightning and a fading wake.
    public sealed class ZonePurpleView : MonoBehaviour
    {
        public ZonePurple Spell;
        static Material material;
        readonly Transform[] orbs=new Transform[3];
        readonly LineRenderer[] rings=new LineRenderer[7];
        readonly LineRenderer[] bolts=new LineRenderer[8];
        LineRenderer trail;
        readonly List<Mesh> meshes=new();
        void Start()
        {
            if(material==null)material=new Material(Shader.Find("Sprites/Default"));
            Color[] colors={new Color(.1f,.45f,1),new Color(1,.08f,.22f),new Color(.65f,.12f,1)};
            for(int i=0;i<3;i++)orbs[i]=Orb(colors[i]);
            for(int i=0;i<rings.Length;i++)rings[i]=Line("Energy orbit",true,96);
            for(int i=0;i<bolts.Length;i++)bolts[i]=Line("Purple lightning",false,12);
            trail=Line("Purple wake",false,24);
        }
        Transform Orb(Color color)
        {
            var obj=new GameObject("Energy sphere");obj.transform.SetParent(transform,false);
            var mesh=new Mesh();meshes.Add(mesh);
            const int n=64;var v=new Vector3[n*2+1];var c=new Color[v.Length];var tris=new int[n*9];
            v[0]=Vector3.zero;c[0]=new Color(1,.92f,1,1);
            for(int i=0;i<n;i++)
            {
                float a=i*Mathf.PI*2/n;var dir=new Vector3(Mathf.Cos(a),Mathf.Sin(a));
                v[1+i]=dir*.45f;v[1+n+i]=dir;c[1+i]=color;c[1+n+i]=new Color(color.r,color.g,color.b,0);
                int next=(i+1)%n,j=i*9;
                tris[j]=0;tris[j+1]=1+i;tris[j+2]=1+next;
                tris[j+3]=1+i;tris[j+4]=1+n+i;tris[j+5]=1+n+next;
                tris[j+6]=1+i;tris[j+7]=1+n+next;tris[j+8]=1+next;
            }
            mesh.vertices=v;mesh.colors=c;mesh.triangles=tris;mesh.RecalculateBounds();
            obj.AddComponent<MeshFilter>().sharedMesh=mesh;
            var sr=obj.AddComponent<MeshRenderer>();sr.sharedMaterial=material;sr.sortingLayerID=ZoneMap.Sorting(Spell.Floor);sr.sortingOrder=130;
            return obj.transform;
        }
        LineRenderer Line(string name,bool loop,int count)
        {
            var obj=new GameObject(name);obj.transform.SetParent(transform,false);
            var line=obj.AddComponent<LineRenderer>();line.sharedMaterial=material;line.useWorldSpace=false;line.loop=loop;line.positionCount=count;
            line.sortingLayerID=ZoneMap.Sorting(Spell.Floor);line.sortingOrder=131;return line;
        }
        void LateUpdate()
        {
            if(orbs[0]==null)return;
            float age=Spell.Age, charge=Mathf.Clamp01(age/ZonePurple.ChargeTime);
            bool preparing=age<ZonePurple.ChargeTime, impact=Spell.ImpactAge>=0;
            float fade=impact?Mathf.Clamp01(1-(age-Spell.ImpactAge)/.4f):1;
            float size=preparing?Mathf.SmoothStep(.12f,1,Mathf.InverseLerp(.45f,1,charge)):1;
            Vector2 dir=Spell.Direction,normal=new Vector2(-dir.y,dir.x);
            float separation=(1-Mathf.SmoothStep(0,1,charge))*1.15f;
            for(int i=0;i<3;i++)
            {
                orbs[i].gameObject.SetActive(i==2?charge>.48f:preparing&&charge<.94f);
                Vector2 p=normal*(i==0?separation:-separation);
                if(i<2)p+=dir*Mathf.Sin(age*9+(i==0?0:Mathf.PI))*.22f*(1-charge);
                orbs[i].localPosition=i==2?Vector3.zero:(Vector3)p;
                float radius=i==2?size*(2.5f+Mathf.Sin(age*35)*.1f):( .5f+charge*.3f);
                if(impact)radius*=1+(1-fade)*2;
                orbs[i].localScale=Vector3.one*radius*fade;
            }
            for(int i=0;i<rings.Length;i++)
            {
                var line=rings[i];float radius=(i<4?.7f+i*.17f:1.45f+(i-4)*.2f)*size;
                line.enabled=charge>.25f;line.startWidth=line.endWidth=(i<4?.045f:.018f)*fade;
                Color color=i%2==0?new Color(.75f,.35f,1,fade*.8f):new Color(.4f,.65f,1,fade*.65f);
                line.startColor=line.endColor=color;
                for(int k=0;k<96;k++)
                {
                    float a=k*Mathf.PI*2/96+age*(i%2==0?3:-4)+i;
                    Vector2 p=new Vector2(Mathf.Cos(a),Mathf.Sin(a)*(i<4?.55f:1))*radius;
                    p=Quaternion.Euler(0,0,i*37+age*70)*p;
                    line.SetPosition(k,p*fade*(impact?1+(1-fade)*2:1));
                }
            }
            for(int i=0;i<bolts.Length;i++)
            {
                var line=bolts[i];line.enabled=charge>.5f;
                line.startWidth=.045f*fade;line.endWidth=.008f*fade;
                line.startColor=new Color(.95f,.65f,1,fade);line.endColor=new Color(.5f,.2f,1,0);
                float a=i*Mathf.PI/4+Mathf.Floor(age*14)*.37f;
                Vector2 spoke=new Vector2(Mathf.Cos(a),Mathf.Sin(a)),side=new Vector2(-spoke.y,spoke.x);
                for(int k=0;k<12;k++)
                {
                    float t=k/11f;line.SetPosition(k,(spoke*(.5f+t*1.7f)+side*Mathf.Sin(k*5+i+Mathf.Floor(age*22))*.14f)*size*fade);
                }
            }
            trail.enabled=!preparing;trail.startWidth=1.15f*fade;trail.endWidth=.01f;
            trail.startColor=new Color(.75f,.3f,1,.6f*fade);trail.endColor=new Color(.3f,.1f,1,0);
            float length=Mathf.Min(6,Mathf.Max(0,(age-ZonePurple.ChargeTime)*ZonePurple.Speed));
            for(int k=0;k<24;k++){float t=k/23f;trail.SetPosition(k,-dir*length*t+normal*Mathf.Sin(t*15-age*20)*.13f*t);}
        }
        void OnDestroy(){foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);}
    }
}
