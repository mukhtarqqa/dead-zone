using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace DeadZone.Editor
{
    public static class CityV2Roads
    {
        const string Root="Assets/DeadZone/CityV2/RoadGeometry";static Transform parent;static Texture2D atlas;
        public static void Draw(Transform city,Texture2D materials)
        {
            parent=new GameObject("Continuous roads and curbs").transform;parent.SetParent(city,false);atlas=materials;if(!AssetDatabase.IsValidFolder(Root))AssetDatabase.CreateFolder("Assets/DeadZone/CityV2","RoadGeometry");
            var avenue=Smooth(new[]{new Vector2(-54,0),new Vector2(25,0),new Vector2(40,7),new Vector2(54,7)});
            var spine=Smooth(new[]{new Vector2(0,-42),new Vector2(0,-17),new Vector2(5,-3),new Vector2(5,17),new Vector2(-4,29),new Vector2(-4,42)});
            var south=Smooth(new[]{new Vector2(-49,-38),new Vector2(-12,-38),new Vector2(48,-38)});
            var trail=Smooth(new[]{new Vector2(-50,-39),new Vector2(-41,-28),new Vector2(-37,-16),new Vector2(-23,-8),new Vector2(-14,-3),new Vector2(1,0)});
            Strip("Avenue curb",avenue,9.2f,2,-990);Strip("Spine curb",spine,7.7f,2,-990);Strip("South curb",south,5.2f,2,-990);
            Strip("Park trail",trail,4.2f,5,-989);Strip("Avenue",avenue,8,0,-988);Strip("Spine",spine,6.5f,0,-988);Strip("South road",south,4,0,-988);
            Disk("Plaza curb",new Vector2(2,0),9.4f,2,-987);Disk("Plaza",new Vector2(2,0),8.8f,4,-986);
        }
        static List<Vector2> Smooth(Vector2[] control)
        {
            var result=new List<Vector2>();for(int s=0;s<control.Length-1;s++)
            {
                Vector2 a=control[Mathf.Max(0,s-1)],b=control[s],c=control[s+1],d=control[Mathf.Min(control.Length-1,s+2)];
                for(int i=0;i<12;i++){float t=i/12f;result.Add(.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t));}
            }
            result.Add(control[control.Length-1]);return result;
        }
        static Material Surface(int index)
        {
            string path=Root+"/Surface_"+index+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material!=null)return material;
            int cell=atlas.width/4;var texture=new Texture2D(cell,cell){name="Road material "+index,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Repeat};texture.SetPixels(atlas.GetPixels(index%4*cell,(3-index/4)*cell,cell,cell));texture.Apply();AssetDatabase.CreateAsset(texture,Root+"/Surface_"+index+".asset");material=new Material(Shader.Find("Sprites/Default")){mainTexture=texture};AssetDatabase.CreateAsset(material,path);return material;
        }
        static void Save(string name,Vector3[] vertices,int[] triangles,int material,int order)
        {
            string path=Root+"/"+name.Replace(" ","_")+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh==null){mesh=new Mesh{name=name};AssetDatabase.CreateAsset(mesh,path);}mesh.Clear();mesh.vertices=vertices;mesh.triangles=triangles;var uv=new Vector2[vertices.Length];var colors=new Color[vertices.Length];for(int i=0;i<uv.Length;i++){uv[i]=new Vector2(vertices[i].x,vertices[i].y);colors[i]=Color.white;}mesh.uv=uv;mesh.colors=colors;mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.layer=20;obj.transform.SetParent(parent,false);obj.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=obj.GetComponent<MeshRenderer>();renderer.sharedMaterial=Surface(material);renderer.sortingLayerID=ZoneMap.Sorting(20);renderer.sortingOrder=order;
        }
        static void Strip(string name,List<Vector2> points,float width,int material,int order)
        {
            var vertices=new Vector3[points.Count*2];var triangles=new int[(points.Count-1)*6];
            for(int i=0;i<points.Count;i++)
            {
                Vector2 before=(points[i]-points[Mathf.Max(0,i-1)]).normalized,after=(points[Mathf.Min(points.Count-1,i+1)]-points[i]).normalized;if(before==Vector2.zero)before=after;if(after==Vector2.zero)after=before;
                Vector2 n1=new(-before.y,before.x),n2=new(-after.y,after.x),miter=(n1+n2).normalized;float extent=width*.5f/Mathf.Max(.5f,Vector2.Dot(miter,n2));vertices[i*2]=points[i]+miter*extent;vertices[i*2+1]=points[i]-miter*extent;
                if(i==points.Count-1)continue;int t=i*6,v=i*2;triangles[t]=v;triangles[t+1]=v+2;triangles[t+2]=v+1;triangles[t+3]=v+1;triangles[t+4]=v+2;triangles[t+5]=v+3;
            }
            Save(name,vertices,triangles,material,order);
        }
        static void Disk(string name,Vector2 center,float radius,int material,int order)
        {
            const int count=96;var v=new Vector3[count+1];var t=new int[count*3];v[0]=center;for(int i=0;i<count;i++){float angle=i*Mathf.PI*2/count;v[i+1]=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;t[i*3]=0;t[i*3+1]=i+1;t[i*3+2]=(i+1)%count+1;}Save(name,v,t,material,order);
        }
    }
}
