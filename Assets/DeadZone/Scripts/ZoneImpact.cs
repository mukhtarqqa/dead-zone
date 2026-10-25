using UnityEngine;
namespace DeadZone
{
    public sealed class ZoneImpact : MonoBehaviour
    {
        static int active;
        static Material material;
        public static Material Material => material!=null?material:(material=new Material(Shader.Find("Sprites/Default")));
        readonly SpriteRenderer[] sparks=new SpriteRenderer[7];
        readonly Vector2[] directions=new Vector2[7];
        float age;
        public static void Spawn(Vector2 point,Vector2 normal,int floor,bool flesh=false)
        {
            if(Application.isBatchMode||active>=32||DeadZoneGame.Instance==null)return;
            var obj=new GameObject(flesh?"Hit sparks":"Wall sparks");obj.transform.position=point;
            var effect=obj.AddComponent<ZoneImpact>();active++;
            for(int i=0;i<effect.sparks.Length;i++)
            {
                float angle=i*137.5f*Mathf.Deg2Rad;Vector2 d=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                effect.directions[i]=(d+normal*.6f).normalized*(.6f+i*.12f);
                var spark=new GameObject("Spark");spark.transform.SetParent(obj.transform,false);var sr=spark.AddComponent<SpriteRenderer>();
                sr.sprite=DeadZoneGame.Instance.marker;sr.sortingLayerID=ZoneMap.Sorting(floor);sr.sortingOrder=120;
                sr.color=flesh?new Color(1,.36f,.3f):new Color(1,.8f,.35f);spark.transform.localScale=new Vector3(.08f,.05f,1);effect.sparks[i]=sr;
            }
        }
        void Update()
        {
            age+=Time.deltaTime;
            for(int i=0;i<sparks.Length;i++)if(sparks[i]!=null){sparks[i].transform.localPosition=directions[i]*age+Vector2.down*age*age;var c=sparks[i].color;c.a=1-age/.28f;sparks[i].color=c;}
            if(age>.28f)Destroy(gameObject);
        }
        void OnDestroy(){active=Mathf.Max(0,active-1);}
    }
    public sealed class ZoneTracer : MonoBehaviour
    {
        TrailRenderer trail;
        void Start()
        {
            trail=gameObject.AddComponent<TrailRenderer>();trail.time=.055f;trail.minVertexDistance=.035f;trail.startWidth=.055f;trail.endWidth=0;
            trail.sharedMaterial=ZoneImpact.Material;trail.startColor=new Color(1,.86f,.45f,.7f);trail.endColor=new Color(1,.7f,.2f,0);
            trail.sortingLayerID=ZoneMap.Sorting(gameObject.layer);trail.sortingOrder=95;
        }
    }
}
