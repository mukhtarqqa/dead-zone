using UnityEngine;

namespace DeadZone
{
    // Rem's timed form owns attacks; presentation reads exactly the same swing phase on peers.
    public sealed class ZoneRemRage : MonoBehaviour
    {
        public const float Duration=40, Reach=2.8f, Damage=110, Interval=.65f, SwingDuration=.52f, ImpactDelay=.22f;
        public ZonePlayer Actor;
        float endAt, nextSwing, swingAt=-100;
        bool resolved;
        public int Swings { get; private set; }
        public Vector2 SwingDirection { get; private set; }=Vector2.right;
        public float Remaining => Mathf.Max(0,endAt-Time.time);
        public bool Active => Actor!=null && Actor.CharacterIndex==1 && !Actor.Dead && !Actor.Downed && Actor.game.running && Remaining>0;
        public float SwingRemaining => Active?Mathf.Max(0,SwingDuration-(Time.time-swingAt)):0;
        public bool Activate()
        {
            if(Actor==null||!Actor.game.IsAuthority||Actor.CharacterIndex!=1||Actor.Dead||Actor.Downed||!Actor.game.running||Active)return false;
            Swings=0;endAt=Time.time+Duration;nextSwing=Time.time;resolved=true;
            ZonePulse.Spawn(transform.position,gameObject.layer,1.3f,4,.8f);return true;
        }
        public void Cancel(){endAt=0;swingAt=-100;resolved=true;}
        public bool TrySwing(Vector2 target)
        {
            if(!Active||!Actor.game.IsAuthority||Actor.CastRemaining>0||Time.time<nextSwing)return false;
            SwingDirection=(target-(Vector2)transform.position).normalized;if(SwingDirection.sqrMagnitude<.01f)SwingDirection=Vector2.right;
            swingAt=Time.time;nextSwing=Time.time+Interval;resolved=false;Swings++;return true;
        }
        public void Apply(ZonePlayerState state)
        {
            endAt=Time.time+state.oniRemaining;Swings=state.maceSwings;SwingDirection=state.maceDirection;
            swingAt=state.maceRemaining>0?Time.time-(SwingDuration-state.maceRemaining):-100;
        }
        void Update()
        {
            if(Actor==null||!Actor.game.IsAuthority)return;
            if(!Actor.game.running||Actor.Downed||Actor.Dead||Actor.CharacterIndex!=1){Cancel();return;}
            if(!Active||resolved||Time.time-swingAt<ImpactDelay)return;
            resolved=true;
            ResolveHit();
        }
        void ResolveHit()
        {
            Vector2 origin=transform.position;
            foreach(var enemy in Actor.game.enemies.ToArray())
            {
                if(enemy==null||enemy.health<=0||enemy.gameObject.layer!=gameObject.layer||Time.time<enemy.ReadyAt)continue;
                Vector2 delta=(Vector2)enemy.transform.position-origin;
                if(delta.magnitude>Reach+.22f || delta.magnitude>.65f && Vector2.Dot(delta.normalized,SwingDirection)<.42f)continue;
                bool blocked=false;
                foreach(var hit in Physics2D.RaycastAll(origin,delta.normalized,delta.magnitude))
                {
                    var c=hit.collider;if(c.isTrigger||c.GetComponentInParent<ZonePlayer>()!=null||c.GetComponentInParent<ZoneEnemy>()!=null)continue;
                    int floor=c.gameObject.layer;
                    if((floor>=20&&floor<=22&&floor!=gameObject.layer)||Physics2D.GetIgnoreLayerCollision(gameObject.layer,floor))continue;
                    blocked=true;break;
                }
                if(!blocked)enemy.Hit(Damage,Actor,false);
            }
            Vector2 point=origin+SwingDirection*1.9f;
            if(!ZoneMap.Obstacle(point,.1f,gameObject.layer))ZonePulse.Spawn(point,gameObject.layer,.85f,4,.35f);
        }
        ZoneRemRageView view;
        void LateUpdate(){if(view==null&&Active){view=gameObject.AddComponent<ZoneRemRageView>();view.Form=this;}}
    }

    public sealed class ZoneRemRageView : MonoBehaviour
    {
        public ZoneRemRage Form;
        static Material material;
        Transform root,horn,head;
        LineRenderer chain,handle,arc,aura;
        readonly System.Collections.Generic.List<Mesh> meshes=new();
        void Start()
        {
            if(material==null)material=new Material(Shader.Find("Sprites/Default"));
            root=new GameObject("Rem Oni visuals").transform;root.SetParent(transform,false);
            horn=Polygon("Oni horn",new[]{new Vector2(-.10f,0),new Vector2(.09f,0),new Vector2(.13f,.17f),new Vector2(.04f,.49f),new Vector2(-.015f,.2f)},new Color(1,.78f,.92f));
            horn.localScale=Vector3.one*.7f;
            var points=new Vector2[32];for(int i=0;i<32;i++){float a=i*Mathf.PI/16;points[i]=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(i%2==0?.44f:.26f);}
            head=Polygon("Spiked mace",points,new Color(.45f,.49f,.6f));
            var corePoints=new Vector2[12];for(int i=0;i<12;i++){float a=i*Mathf.PI/6;corePoints[i]=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.23f;}
            var core=Polygon("Mace steel core",corePoints,new Color(.18f,.22f,.3f));core.SetParent(head,false);core.GetComponent<MeshRenderer>().sortingOrder=40;
            var glint=Polygon("Mace highlight",new[]{new Vector2(-.12f,.02f),new Vector2(-.04f,.17f),new Vector2(.09f,.13f),new Vector2(.01f,0)},new Color(.85f,.9f,1));glint.SetParent(head,false);glint.GetComponent<MeshRenderer>().sortingOrder=41;
            chain=Line("Mace chain",20,.035f);handle=Line("Mace grip",2,.09f);arc=Line("Mace swing trail",32,.11f);aura=Line("Oni aura",64,.025f);aura.loop=true;
        }
        Transform Polygon(string name,Vector2[] points,Color color)
        {
            var obj=new GameObject(name);obj.transform.SetParent(root,false);
            var mesh=new Mesh();meshes.Add(mesh);var vertices=new Vector3[points.Length+1];var colors=new Color[vertices.Length];var triangles=new int[points.Length*3];colors[0]=Color.Lerp(color,Color.white,.25f);
            for(int i=0;i<points.Length;i++){vertices[i+1]=points[i];colors[i+1]=color;triangles[i*3]=0;triangles[i*3+1]=i+1;triangles[i*3+2]=(i+1)%points.Length+1;}
            mesh.vertices=vertices;mesh.colors=colors;mesh.triangles=triangles;mesh.RecalculateBounds();obj.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingLayerID=ZoneMap.Sorting(gameObject.layer);renderer.sortingOrder=39;return obj.transform;
        }
        LineRenderer Line(string name,int count,float width)
        {
            var obj=new GameObject(name);obj.transform.SetParent(root,false);var line=obj.AddComponent<LineRenderer>();line.sharedMaterial=material;line.positionCount=count;line.useWorldSpace=false;line.startWidth=line.endWidth=width;line.sortingLayerID=ZoneMap.Sorting(gameObject.layer);line.sortingOrder=37;line.startColor=line.endColor=new Color(.65f,.7f,.8f);return line;
        }
        void LateUpdate()
        {
            if(root==null||Form==null)return;
            root.gameObject.SetActive(Form.Active);if(!Form.Active)return;
            var actor=Form.Actor;int depth=ZoneDepth.Order(actor.transform.position.y);
            foreach(var renderer in root.GetComponentsInChildren<Renderer>())renderer.sortingOrder=depth+8;
            Vector2 look=actor.aimPoint-(Vector2)actor.transform.position;
            int facing=ZoneActorArt.Facing(look);float side=facing==1?1:facing==3?-1:0;
            // The horn extends above the hair in every direction, with rear views drawn behind it.
            horn.localPosition=new Vector3(side*.12f+.025f,1.05f);horn.localRotation=Quaternion.Euler(0,0,-side*12);
            horn.GetComponent<MeshRenderer>().sortingOrder=depth+(facing==2?-1:10);
            Vector2 dir=Form.SwingRemaining>0?Form.SwingDirection:look.normalized;if(dir.sqrMagnitude<.1f)dir=Vector2.right;
            float phase=1-Form.SwingRemaining/ZoneRemRage.SwingDuration;
            float angle=Form.SwingRemaining>0?Mathf.Lerp(-95,85,Mathf.SmoothStep(0,1,phase)): -30;
            Vector2 rotation=Quaternion.Euler(0,0,angle)*dir;
            Vector2 grip=new Vector2(0,.5f),tip=grip+rotation*(Form.SwingRemaining>0?2.3f:1.05f);
            head.localPosition=tip;head.localRotation=Quaternion.Euler(0,0,Time.time*160);
            handle.SetPosition(0,grip);handle.SetPosition(1,grip+rotation*.3f);
            for(int i=0;i<20;i++){float t=i/19f;Vector2 normal=new Vector2(-rotation.y,rotation.x);chain.SetPosition(i,Vector2.Lerp(grip+rotation*.3f,tip,t)+normal*Mathf.Sin(t*Mathf.PI)*.12f+normal*(i%2==0?.015f:-.015f));}
            arc.enabled=Form.SwingRemaining>0 && phase>.12f && phase<.85f;
            arc.startColor=new Color(.4f,.85f,1,.05f);arc.endColor=new Color(1,.35f,.7f,.65f);
            for(int i=0;i<32;i++){float a=angle-70+i*70f/31;Vector2 p=Quaternion.Euler(0,0,a)*dir;arc.SetPosition(i,grip+p*2.3f);}
            aura.startColor=aura.endColor=new Color(1,.3f,.6f,.45f);
            for(int i=0;i<64;i++){float a=i*Mathf.PI*2/64;float r=.43f+Mathf.Sin(Time.time*7+i*.7f)*.025f;aura.SetPosition(i,new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r*.45f));}
        }
        void OnDestroy(){if(root!=null)Destroy(root.gameObject);foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);}
    }
}
