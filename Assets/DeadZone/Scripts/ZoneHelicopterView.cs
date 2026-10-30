using System.Collections.Generic;
using UnityEngine;

namespace DeadZone
{
    // A native, editable model: hull, glazing, skids, doors and independent rotors.
    public sealed class ZoneHelicopterView : MonoBehaviour
    {
        public Transform Body,MainRotor,TailRotor,Door,Shadow;
        public MeshRenderer RedLight,GreenLight,LandingLight;
        public MeshRenderer[] SeatLights;
        MaterialPropertyBlock block;
        readonly List<Mesh> ownedMeshes=new();
        Renderer[] parts;int[] offsets;
        public Vector3 FocusPoint=>Body!=null?Body.position+Vector3.up*.3f:transform.position;

        public static GameObject CreateModel(Material material=null)
        {
            var obj=new GameObject("Rescue helicopter");var view=obj.AddComponent<ZoneHelicopterView>();
            view.Build(material!=null?material:new Material(Shader.Find("Sprites/Default")));
            return obj;
        }

        MeshRenderer Polygon(Transform parent,string name,Vector2[] outline,Color color,Material material,int order)
        {
            var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(parent,false);
            var mesh=new Mesh{name=name};ownedMeshes.Add(mesh);var vertices=new Vector3[outline.Length+1];var colors=new Color[vertices.Length];
            Vector2 center=Vector2.zero;foreach(var point in outline)center+=point;center/=outline.Length;vertices[0]=center;colors[0]=Color.Lerp(color,Color.white,.12f);colors[0].a=color.a;
            var triangles=new int[outline.Length*3];for(int i=0;i<outline.Length;i++){vertices[i+1]=outline[i];colors[i+1]=color;triangles[i*3]=0;triangles[i*3+1]=i+1;triangles[i*3+2]=(i+1)%outline.Length+1;}
            mesh.vertices=vertices;mesh.colors=colors;mesh.triangles=triangles;mesh.RecalculateBounds();obj.GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=obj.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingLayerID=ZoneMap.Sorting(20);renderer.sortingOrder=order;return renderer;
        }
        MeshRenderer Box(Transform parent,string name,Vector2 center,Vector2 size,Color color,Material material,int order)
        {var h=size*.5f;return Polygon(parent,name,new[]{center+new Vector2(-h.x,-h.y),center+new Vector2(h.x,-h.y),center+h,center+new Vector2(-h.x,h.y)},color,material,order);}
        MeshRenderer Oval(Transform parent,string name,Vector2 center,Vector2 radius,Color color,Material material,int order)
        {var points=new Vector2[32];for(int i=0;i<points.Length;i++){float a=i*Mathf.PI*2/points.Length;points[i]=center+new Vector2(Mathf.Cos(a)*radius.x,Mathf.Sin(a)*radius.y);}return Polygon(parent,name,points,color,material,order);}

        void Build(Material material)
        {
            Shadow=Oval(transform,"Ground shadow",Vector2.zero,new Vector2(1.75f,1.1f),new Color(.02f,.025f,.025f,.30f),material,-20).transform;
            Body=new GameObject("Airframe").transform;Body.SetParent(transform,false);
            Color edge=new(.055f,.075f,.07f),hull=new(.24f,.29f,.25f),paint=new(.72f,.74f,.65f),glass=new(.14f,.35f,.39f);
            Box(Body,"Left landing skid",new Vector2(-1,-.15f),new Vector2(.11f,2.9f),edge,material,-3);
            Box(Body,"Right landing skid",new Vector2(1,-.15f),new Vector2(.11f,2.9f),edge,material,-3);
            for(int side=-1;side<=1;side+=2)for(int y=-1;y<=1;y+=2)Box(Body,"Skid strut",new Vector2(side*.75f,y*.75f),new Vector2(.55f,.1f),paint,material,-2);
            Polygon(Body,"Tail boom",new[]{new Vector2(-.42f,-.55f),new Vector2(.42f,-.55f),new Vector2(.15f,-3.45f),new Vector2(-.15f,-3.45f)},hull,material,-1);
            Box(Body,"Tailplane",new Vector2(0,-2.9f),new Vector2(1.65f,.23f),paint,material,0);
            Oval(Body,"Armoured hull",Vector2.zero,new Vector2(.85f,1.75f),edge,material,1);
            Oval(Body,"Cabin shell",new Vector2(0,.08f),new Vector2(.74f,1.52f),hull,material,2);
            Polygon(Body,"Left cockpit",new[]{new Vector2(-.63f,.58f),new Vector2(-.08f,.66f),new Vector2(-.08f,1.43f),new Vector2(-.40f,1.28f)},glass,material,3);
            Polygon(Body,"Right cockpit",new[]{new Vector2(.08f,.66f),new Vector2(.63f,.58f),new Vector2(.40f,1.28f),new Vector2(.08f,1.43f)},glass,material,3);
            Box(Body,"Cockpit reflection",new Vector2(-.30f,1.10f),new Vector2(.28f,.06f),new Color(.53f,.74f,.74f),material,4);
            Box(Body,"Rescue stripe",new Vector2(0,-.78f),new Vector2(1.40f,.26f),paint,material,3);
            Box(Body,"Rescue marking",new Vector2(0,-.78f),new Vector2(.23f,.53f),new Color(.65f,.16f,.12f),material,4);
            Door=Box(Body,"Sliding cabin door",new Vector2(.62f,-.13f),new Vector2(.24f,.76f),new Color(.10f,.15f,.13f),material,4).transform;
            Box(Body,"Door step",new Vector2(.89f,-.15f),new Vector2(.34f,.12f),paint,material,3);
            MainRotor=new GameObject("Main rotor").transform;MainRotor.SetParent(Body,false);MainRotor.localPosition=new Vector3(0,.17f);
            Oval(MainRotor,"Rotor blur",Vector2.zero,Vector2.one*2.8f,new Color(.35f,.40f,.35f,.065f),material,5);
            for(int blade=0;blade<2;blade++){var part=Box(MainRotor,"Rotor blade "+blade,Vector2.zero,new Vector2(5.65f,.14f),edge,material,6);part.transform.localRotation=Quaternion.Euler(0,0,blade*90);}
            Oval(MainRotor,"Rotor hub",Vector2.zero,Vector2.one*.19f,paint,material,7);
            TailRotor=new GameObject("Tail rotor").transform;TailRotor.SetParent(Body,false);TailRotor.localPosition=new Vector3(0,-3.23f);
            Box(TailRotor,"Tail blade",Vector2.zero,new Vector2(.9f,.10f),edge,material,3);
            Box(TailRotor,"Tail blade cross",Vector2.zero,new Vector2(.10f,.9f),edge,material,3);
            RedLight=Oval(Body,"Port navigation light",new Vector2(-.88f,.20f),Vector2.one*.10f,new Color(1,.16f,.12f),material,8);
            GreenLight=Oval(Body,"Starboard navigation light",new Vector2(.88f,.20f),Vector2.one*.10f,new Color(.22f,.95f,.55f),material,8);
            LandingLight=Polygon(Body,"Landing light",new[]{new Vector2(-.15f,1.45f),new Vector2(.15f,1.45f),new Vector2(1.4f,4.1f),new Vector2(-1.4f,4.1f)},new Color(.85f,.91f,.72f,.07f),material,-4);
            SeatLights=new MeshRenderer[4];for(int i=0;i<4;i++)SeatLights[i]=Box(Body,"Occupied seat "+(i+1),new Vector2(-.35f+i*.23f,-.45f),new Vector2(.13f,.09f),new Color(.43f,.79f,.55f),material,4);
        }

        public void Present(float altitude,float heading,bool landed,int seated)
        {
            if(Body==null)return;
            if(parts==null){parts=GetComponentsInChildren<Renderer>(true);offsets=new int[parts.Length];for(int i=0;i<parts.Length;i++)offsets[i]=parts[i].sortingOrder;}
            float rotorClock=Time.time;Body.localPosition=new Vector3(0,altitude*.45f);Body.localRotation=Quaternion.Euler(0,0,heading);
            MainRotor.localRotation=Quaternion.Euler(0,0,rotorClock*1223);TailRotor.localRotation=Quaternion.Euler(0,0,-rotorClock*1913);
            Door.localPosition=new Vector3(landed?.24f:0,0);LandingLight.enabled=!landed;
            int depth=landed?ZoneDepth.Order(transform.position.y)+10:3200;
            for(int i=0;i<parts.Length;i++)if(parts[i].transform!=Shadow)parts[i].sortingOrder=depth+offsets[i];
            for(int i=0;i<SeatLights.Length;i++)SeatLights[i].enabled=i<seated;
            Shadow.GetComponent<Renderer>().sortingOrder=ZoneDepth.Order(transform.position.y)-5;
            Shadow.localScale=Vector3.one*(1+altitude*.10f);Tint(Shadow.GetComponent<Renderer>(),new Color(1,1,1,1-altitude*.10f));
            Tint(RedLight,new Color(1,1,1,.35f+.65f*Mathf.Max(0,Mathf.Sin(rotorClock*5))));
            Tint(GreenLight,new Color(1,1,1,.6f+.4f*Mathf.Max(0,Mathf.Sin(rotorClock*5+.8f))));
        }
        void Tint(Renderer renderer,Color color){block??=new MaterialPropertyBlock();block.SetColor("_Color",color);renderer.SetPropertyBlock(block);}
        void OnDestroy(){if(Application.isPlaying)foreach(var mesh in ownedMeshes)if(mesh!=null)Destroy(mesh);}
    }
}
