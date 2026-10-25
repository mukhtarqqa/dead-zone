using UnityEngine;
namespace DeadZone
{
    public sealed class ZoneShrineView : MonoBehaviour
    {
        public ZoneShrine Shrine;SpriteRenderer temple;LineRenderer boundary,glow,inner;Mesh floor,cuts,embers;MeshRenderer floorRenderer;
        readonly Vector3[] cutVertices=new Vector3[192],emberVertices=new Vector3[112];readonly Color32[] cutColors=new Color32[192],emberColors=new Color32[112];int lastBeat=-1;bool awakened;
        void Start()
        {
            if(Shrine.Replica&&Shrine.Age<ZoneShrine.ChargeTime)ZoneFeedback.Play(12);
            var obj=new GameObject("Manifested bone shrine");obj.transform.SetParent(transform,false);temple=obj.AddComponent<SpriteRenderer>();temple.sprite=ZoneShrineArt.Frame(0);temple.sortingLayerID=ZoneMap.Sorting(Shrine.Floor);temple.sortingOrder=350;
            boundary=Ring("Domain edge",.035f,450);glow=Ring("Crimson edge glow",.18f,449);inner=Ring("Inner seal",.018f,451);
            floor=MeshObject("Cursed floor",300,out floorRenderer);var vertices=new Vector3[97];var colors=new Color32[97];var triangles=new int[96*3];colors[0]=new Color(.08f,0,.012f,.25f);
            for(int i=0;i<96;i++){float angle=i*Mathf.PI*2/96;vertices[i+1]=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*ZoneShrine.Radius;colors[i+1]=new Color(.12f,0,.01f,0);triangles[i*3]=0;triangles[i*3+1]=i+1;triangles[i*3+2]=(i+1)%96+1;}floor.vertices=vertices;floor.colors32=colors;floor.triangles=triangles;floor.RecalculateBounds();
            cuts=MeshObject("Sweeping cleave ribbons",3200,out _);cuts.MarkDynamic();cuts.vertices=cutVertices;cuts.colors32=cutColors;cuts.triangles=Quads(48);cuts.bounds=new Bounds(Vector3.zero,new Vector3(14,14,1));
            embers=MeshObject("Cursed embers",2650,out _);embers.MarkDynamic();embers.vertices=emberVertices;embers.colors32=emberColors;embers.triangles=Quads(28);embers.bounds=new Bounds(Vector3.zero,new Vector3(14,16,1));
        }
        Mesh MeshObject(string name,int order,out MeshRenderer renderer)
        {
            var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(transform,false);renderer=obj.GetComponent<MeshRenderer>();renderer.sharedMaterial=ZoneImpact.Material;renderer.sortingLayerID=ZoneMap.Sorting(Shrine.Floor);renderer.sortingOrder=order;var mesh=new Mesh{name=name};obj.GetComponent<MeshFilter>().sharedMesh=mesh;return mesh;
        }
        static int[] Quads(int count){var t=new int[count*6];for(int i=0;i<count;i++){int n=i*6,v=i*4;t[n]=v;t[n+1]=v+1;t[n+2]=v+2;t[n+3]=v;t[n+4]=v+2;t[n+5]=v+3;}return t;}
        LineRenderer Ring(string name,float width,int order)
        {
            var obj=new GameObject(name);obj.transform.SetParent(transform,false);var line=obj.AddComponent<LineRenderer>();line.sharedMaterial=ZoneImpact.Material;line.useWorldSpace=false;line.loop=true;line.positionCount=96;line.startWidth=line.endWidth=width;line.sortingLayerID=ZoneMap.Sorting(Shrine.Floor);line.sortingOrder=order;return line;
        }
        void Update()
        {
            if(temple==null||Shrine==null)return;float age=Shrine.Age,build=Mathf.SmoothStep(0,1,age/ZoneShrine.ChargeTime),left=ZoneShrine.ChargeTime+ZoneShrine.Duration-age,fade=Mathf.SmoothStep(0,1,left/.65f),active=Mathf.Max(0,age-ZoneShrine.ChargeTime),phase=Mathf.Repeat(active,ZoneShrine.Interval);int beat=Mathf.FloorToInt(active/ZoneShrine.Interval);
            if(!awakened&&age>=ZoneShrine.ChargeTime){awakened=true;if(age<ZoneShrine.ChargeTime+.2f){ZoneFeedback.Play(11);if(!Shrine.Replica&&Shrine.Owner!=null&&Shrine.Owner.IsLocal)ZoneFeedback.Kick(.12f);}}
            if(age>=ZoneShrine.ChargeTime&&beat!=lastBeat){lastBeat=beat;ZoneFeedback.Play(13);}
            temple.sprite=ZoneShrineArt.Frame(left<.3f?3:left<.65f?2:age>=ZoneShrine.ChargeTime&&phase<.13f?1:0);temple.transform.localPosition=new Vector3(0,1.25f-(1-build)*.65f);temple.transform.localScale=new Vector3(1+(1-build)*.15f,Mathf.Max(.02f,build),1)*(1+Mathf.Exp(-phase*18)*.012f);temple.color=new Color(1,1,1,.86f*build*fade);
            float radius=ZoneShrine.Radius*build;for(int i=0;i<96;i++){float angle=i*Mathf.PI*2/96;var p=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle));boundary.SetPosition(i,p*radius);glow.SetPosition(i,p*radius);inner.SetPosition(i,p*(radius*.94f+.035f*Mathf.Sin(angle*12+age*2)));}
            float flash=age>=ZoneShrine.ChargeTime?Mathf.Exp(-phase*13):build;boundary.startColor=boundary.endColor=new Color(.95f,.14f,.18f,(.35f+flash*.3f)*fade);glow.startColor=glow.endColor=new Color(1,.04f,.1f,(.09f+flash*.12f)*fade);inner.startColor=inner.endColor=new Color(.9f,.7f,.54f,.2f*fade);floorRenderer.transform.localScale=Vector3.one*build;
            for(int i=0;i<8;i++)
            {
                float local=phase-i*.018f,life=Mathf.Clamp01(local/.35f),alpha=age<ZoneShrine.ChargeTime||local<0||local>.35f?0:(1-life)*fade;
                float angle=i*2.39996f+beat*.73f;Vector2 d=new(Mathf.Cos(angle),Mathf.Sin(angle)),n=new(-d.y,d.x),center=n*Mathf.Sin(i*8.7f+beat)*ZoneShrine.Radius*.55f;float half=Mathf.Sqrt(Mathf.Max(0,radius*radius-center.sqrMagnitude))*.94f;
                float end=Mathf.Clamp01(local/.13f),start=Mathf.Max(0,end-.68f);Vector2 a=center+d*Mathf.Lerp(-half,half,start),b=center+d*Mathf.Lerp(-half,half,end);
                for(int layer=0;layer<2;layer++)for(int s=0;s<3;s++)
                {
                    float t0=s/3f,t1=(s+1)/3f;Vector2 from=Vector2.Lerp(a,b,t0)+n*Mathf.Sin(t0*Mathf.PI)*.1f,to=Vector2.Lerp(a,b,t1)+n*Mathf.Sin(t1*Mathf.PI)*.1f;float width=(layer==0?.13f:.025f)*(1-life*.7f);Color c=layer==0?new Color(1,.05f,.12f,alpha*.25f):new Color(1,.88f,.76f,alpha*.9f);Quad(cutVertices,cutColors,((i*2+layer)*3+s)*4,from,to,n*width,c);
                }
            }
            cuts.vertices=cutVertices;cuts.colors32=cutColors;
            for(int i=0;i<28;i++){float life=Mathf.Repeat(age*(.38f+i%4*.06f)+i*.137f,1),angle=i*2.39996f+age*.07f;Vector2 p=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius*(.3f+i%7*.1f)+Vector2.up*life*1.7f;float alpha=Mathf.Sin(life*Mathf.PI)*fade*build;Quad(emberVertices,emberColors,i*4,p,p+new Vector2(.02f,.06f),new Vector2(.018f,-.005f),new Color(1,.25f,.08f,alpha*.65f));}embers.vertices=emberVertices;embers.colors32=emberColors;
        }
        static void Quad(Vector3[] v,Color32[] c,int index,Vector2 a,Vector2 b,Vector2 n,Color color){v[index]=a-n;v[index+1]=a+n;v[index+2]=b+n;v[index+3]=b-n;for(int i=0;i<4;i++)c[index+i]=color;}
        void OnDestroy(){if(floor!=null)Destroy(floor);if(cuts!=null)Destroy(cuts);if(embers!=null)Destroy(embers);}
    }
}
