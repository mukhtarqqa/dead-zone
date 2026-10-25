using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
namespace DeadZone
{
    // Cached, allocation-free flow fields with local line-of-sight smoothing.
    public sealed class ZoneMap : MonoBehaviour
    {
        public static ZoneMap Current {get;private set;}
        const float Step=.5f;
        readonly List<Tilemap> floors=new();
        readonly Dictionary<Vector3Int,int> nodes=new();
        readonly List<Vector3Int> keys=new();
        readonly Dictionary<ZonePlayer,Flow> flows=new();
        int[] adjacent;
        static readonly Collider2D[] obstacleHits=new Collider2D[64];
        static readonly RaycastHit2D[] lineHits=new RaycastHit2D[64];
        sealed class Flow
        {
            public int Goal=-1;public float Until;public readonly int[] Next,Queue;
            public Flow(int count){Next=new int[count];Queue=new int[count];}
        }
        public int NodeCount=>nodes.Count;
        public Vector2 RandomPoint()=>keys.Count==0?Vector2.zero:Point(keys[Random.Range(0,keys.Count)]);
        public int StairLinks{get;private set;}
        public int FieldBuilds{get;private set;}
        public double LastFieldMilliseconds{get;private set;}
        public readonly List<(Vector3Int lower,Vector3Int upper)> StairRoutes=new();
        public static int Sorting(int floor)=>SortingLayer.NameToID("Layer "+(Mathf.Clamp(floor,20,22)-19));
        public static void SetFloor(GameObject obj,int floor){obj.layer=floor;foreach(var sr in obj.GetComponentsInChildren<SpriteRenderer>())sr.sortingLayerID=Sorting(floor);}
        static bool Terrain(Collider2D c,int floor)
        {
            if(c==null||c.isTrigger||c.GetComponentInParent<ZonePlayer>()!=null||c.GetComponentInParent<ZoneEnemy>()!=null)return false;
            int layer=c.gameObject.layer;return !((layer>=20&&layer<=22&&layer!=floor)||Physics2D.GetIgnoreLayerCollision(floor,layer));
        }
        public static bool Obstacle(Vector2 point,float radius,int floor)
        {
            int count=Physics2D.OverlapCircleNonAlloc(point,radius,obstacleHits);
            for(int i=0;i<count;i++)if(Terrain(obstacleHits[i],floor))return true;return count==obstacleHits.Length;
        }
        public static bool Clearance(Vector2 from,Vector2 to,int floor,float radius=.27f)
        {
            Vector2 delta=to-from;if(delta.sqrMagnitude<.0001f)return !Obstacle(to,radius,floor);
            int count=Physics2D.CircleCastNonAlloc(from,radius,delta.normalized,lineHits,delta.magnitude);
            for(int i=0;i<count;i++)if(Terrain(lineHits[i].collider,floor))return false;return count<lineHits.Length;
        }
        bool TileAt(Vector2 point,int floor){foreach(var map in floors)if(map.gameObject.layer==floor&&map.HasTile(map.WorldToCell(point)))return true;return false;}
        public bool Walkable(Vector2 point,int floor)
        {
            if(!TileAt(point,floor)||Obstacle(point,.27f,floor))return false;
            for(int upper=floor+1;upper<=22;upper++)if(TileAt(point,upper))return false;return true;
        }
        static Vector3Int Key(Vector2 point,int floor)=>new(Mathf.RoundToInt(point.x/Step),Mathf.RoundToInt(point.y/Step),floor);
        static Vector2 Point(Vector3Int key)=>new(key.x*Step,key.y*Step);
        void Awake()
        {
            Current=this;
            foreach(var map in FindObjectsByType<Tilemap>())if(map.gameObject.layer>=20&&map.gameObject.layer<=22&&(map.name.Contains("Grass")||map.name.Contains("Ground")))floors.Add(map);
            Physics2D.SyncTransforms();
            foreach(var map in floors)
            {
                var bounds=map.cellBounds;Vector2 min=map.CellToWorld(bounds.min),max=map.CellToWorld(bounds.max);
                for(int x=Mathf.FloorToInt(min.x/Step);x<=Mathf.CeilToInt(max.x/Step);x++)for(int y=Mathf.FloorToInt(min.y/Step);y<=Mathf.CeilToInt(max.y/Step);y++)
                {
                    var key=new Vector3Int(x,y,map.gameObject.layer);if(!nodes.ContainsKey(key)&&Walkable(Point(key),key.z)){nodes.Add(key,keys.Count);keys.Add(key);}
                }
            }
            adjacent=new int[keys.Count*4];System.Array.Fill(adjacent,-1);
            var offsets=new[]{Vector3Int.left,Vector3Int.right,Vector3Int.up,Vector3Int.down};
            for(int i=0;i<keys.Count;i++)for(int d=0;d<4;d++){var next=keys[i]+offsets[d];if(nodes.TryGetValue(next,out int id)&&!Obstacle((Point(next)+Point(keys[i]))*.5f,.27f,next.z))adjacent[i*4+d]=id;}
            Debug.Log("DZ_MAP_READY: "+nodes.Count+" walkable nodes, buffered fields, smooth paths");
        }
        bool Nearest(Vector2 point,int floor,out int result,float radius=1)
        {
            var center=Key(point,floor);if(nodes.TryGetValue(center,out result))return true;
            float best=radius*radius;bool found=false;int count=Mathf.CeilToInt(radius/Step);
            for(int x=-count;x<=count;x++)for(int y=-count;y<=count;y++){var candidate=center+new Vector3Int(x,y,0);float distance=(Point(candidate)-point).sqrMagnitude;if(distance<=best&&nodes.TryGetValue(candidate,out int id)){best=distance;result=id;found=true;}}
            return found;
        }
        Flow Field(ZonePlayer target)
        {
            if(target==null||!Nearest(target.transform.position,target.gameObject.layer,out int goal,2))return null;
            if(!flows.TryGetValue(target,out var flow)){flow=new Flow(keys.Count);flows[target]=flow;}
            if(flow.Goal>=0&&Time.time<flow.Until)return flow;
            flow.Until=Time.time+.35f;if(flow.Goal==goal)return flow;
            long started=System.Diagnostics.Stopwatch.GetTimestamp();flow.Goal=goal;System.Array.Fill(flow.Next,-1);
            int head=0,tail=1;flow.Queue[0]=goal;flow.Next[goal]=goal;
            while(head<tail){int node=flow.Queue[head++];for(int d=0;d<4;d++){int next=adjacent[node*4+d];if(next<0||flow.Next[next]>=0)continue;flow.Next[next]=node;flow.Queue[tail++]=next;}}
            FieldBuilds++;LastFieldMilliseconds=(System.Diagnostics.Stopwatch.GetTimestamp()-started)*1000d/System.Diagnostics.Stopwatch.Frequency;return flow;
        }
        public bool Reachable(Vector2 point,int floor,ZonePlayer target)=>Nearest(point,floor,out int node,.8f)&&Field(target)?.Next[node]>=0;
        public Vector2 Direction(Vector2 point,int floor,ZonePlayer target)
        {
            if(target==null)return Vector2.zero;Vector2 destination=target.transform.position;
            if(target.gameObject.layer==floor&&(destination-point).sqrMagnitude<144&&Clearance(point,destination,floor))return (destination-point).normalized;
            var flow=Field(target);if(flow==null||!Nearest(point,floor,out int node,1.5f)||flow.Next[node]<0)return Vector2.zero;
            if(node==flow.Goal)return (destination-point).normalized;
            int next=flow.Next[node],look=next;
            for(int i=0;i<4;i++){int ahead=flow.Next[look];if(ahead<0||ahead==look||!Clearance(point,Point(keys[ahead]),floor))break;look=ahead;}
            return (Point(keys[look])-point).normalized;
        }
        public bool DropDestination(Vector2 origin, int floor, Vector2 direction, out Vector2 landing)
        {
            landing = origin;
            if (floor <= 20 || direction.sqrMagnitude < .1f) return false;
            direction.Normalize();
            // Must stand at a real ledge; an ordinary wall on the upper floor is not a drop.
            bool leftFloor = false;
            for (float d = .35f; d <= 2.5f; d += .15f)
            {
                Vector2 p = origin + direction * d;
                if (TileAt(p, floor)) { if (leftFloor) return false; continue; }
                if (!leftFloor && d > 1.1f) return false;
                leftFloor = true;
                if (Walkable(p, floor - 1) && Walkable(p + direction * .25f, floor - 1)) { landing = p + direction * .25f; return true; }
            }
            return false;
        }
        public bool RampDestination(Vector2 origin, int floor, Vector2 intention, out Vector2 landing, out int destinationFloor)
        {
            landing = origin; destinationFloor = floor;
            if (intention.sqrMagnitude < .01f) return false;
            foreach (var route in StairRoutes)
            {
                Vector3Int start, end;
                if (floor == route.lower.z) { start = route.lower; end = route.upper; }
                else if (floor == route.upper.z) { start = route.upper; end = route.lower; }
                else continue;
                Vector2 a = Point(start), b = Point(end);
                if (Vector2.Distance(origin, a) < .8f && Vector2.Dot(intention.normalized, (b - a).normalized) > .35f)
                { landing = b; destinationFloor = end.z; return true; }
            }
            return false;
        }
        void OnDestroy(){if(Current==this)Current=null;}
        public IEnumerable<Vector3Int> WalkableNodes=>nodes.Keys;
        public Vector2 WorldPoint(Vector3Int node)=>Point(node);
    }

    public sealed class ZoneStairTravel : MonoBehaviour
    {
        public bool Busy { get; private set; }
        public static bool Step(GameObject actor, Vector2 intention)
        {
            var travel = actor.GetComponent<ZoneStairTravel>();
            if (travel != null && travel.Busy) return true;
            if (ZoneMap.Current == null || !ZoneMap.Current.RampDestination(actor.transform.position, actor.layer, intention, out var landing, out var floor)) return false;
            travel = travel != null ? travel : actor.AddComponent<ZoneStairTravel>();
            travel.StartCoroutine(travel.Traverse(landing, floor)); return true;
        }
        System.Collections.IEnumerator Traverse(Vector2 landing, int floor)
        {
            Busy = true; var body = GetComponent<Rigidbody2D>(); body.linearVelocity = Vector2.zero; body.simulated = false;
            Vector2 origin = transform.position; float elapsed = 0, duration = Mathf.Max(.4f, Vector2.Distance(origin, landing) / 2.8f);
            while (elapsed < duration && DeadZoneGame.Instance != null && DeadZoneGame.Instance.running)
            { elapsed += Time.deltaTime; transform.position = Vector2.Lerp(origin, landing, Mathf.Clamp01(elapsed / duration)); yield return null; }
            transform.position = landing; body.position = landing; ZoneMap.SetFloor(gameObject, floor); body.simulated = true; Busy = false;
        }
    }
}
