using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DeadZone
{
    // The Cainos map is a layered XY world, not a 3D NavMesh surface.
    public sealed class ZoneMap : MonoBehaviour
    {
        public static ZoneMap Current { get; private set; }
        const float Step = .5f;
        readonly List<Tilemap> floors = new();
        readonly Dictionary<Vector3Int, List<Vector3Int>> nodes = new();
        readonly Dictionary<ZonePlayer, Flow> flows = new();
        sealed class Flow { public Vector3Int Goal; public float Until; public readonly Dictionary<Vector3Int, Vector3Int> Next = new(); }
        public int NodeCount => nodes.Count;
        public int StairLinks { get; private set; }
        public readonly List<(Vector3Int lower, Vector3Int upper)> StairRoutes = new();
        public static int Sorting(int floor) => SortingLayer.NameToID("Layer " + (Mathf.Clamp(floor, 20, 22) - 19));
        public static void SetFloor(GameObject obj, int floor)
        {
            obj.layer = floor;
            foreach (var sr in obj.GetComponentsInChildren<SpriteRenderer>()) sr.sortingLayerID = Sorting(floor);
        }
        public static bool Obstacle(Vector2 point, float radius, int floor)
        {
            foreach (var c in Physics2D.OverlapCircleAll(point, radius))
            {
                if (c.isTrigger || c.GetComponentInParent<ZonePlayer>() != null || c.GetComponentInParent<ZoneEnemy>() != null) continue;
                int layer = c.gameObject.layer;
                if ((layer >= 20 && layer <= 22 && layer != floor) || Physics2D.GetIgnoreLayerCollision(floor, layer)) continue;
                return true;
            }
            return false;
        }
        bool TileAt(Vector2 point, int floor)
        {
            foreach (var map in floors) if (map.gameObject.layer == floor && map.HasTile(map.WorldToCell(point))) return true;
            return false;
        }
        public bool Walkable(Vector2 point, int floor)
        {
            if (!TileAt(point, floor) || Obstacle(point, .2f, floor)) return false;
            // No underground spawns hidden beneath the raised floor artwork.
            for (int upper = floor + 1; upper <= 22; upper++) if (TileAt(point, upper)) return false;
            return true;
        }
        static Vector3Int Key(Vector2 p, int floor) => new(Mathf.RoundToInt(p.x / Step), Mathf.RoundToInt(p.y / Step), floor);
        static Vector2 Point(Vector3Int key) => new(key.x * Step, key.y * Step);
        void Awake()
        {
            Current = this;
            foreach (var map in FindObjectsByType<Tilemap>()) if (map.gameObject.layer >= 20 && map.gameObject.layer <= 22 && (map.name.Contains("Grass") || map.name.Contains("Ground"))) floors.Add(map);
            Physics2D.SyncTransforms();
            foreach (var map in floors)
            {
                var b = map.cellBounds;
                Vector2 min = map.CellToWorld(b.min), max = map.CellToWorld(b.max);
                for (int x = Mathf.FloorToInt(min.x / Step); x <= Mathf.CeilToInt(max.x / Step); x++)
                for (int y = Mathf.FloorToInt(min.y / Step); y <= Mathf.CeilToInt(max.y / Step); y++)
                { var key = new Vector3Int(x, y, map.gameObject.layer); if (Walkable(Point(key), key.z)) nodes.TryAdd(key, new List<Vector3Int>()); }
            }
            var offsets = new[] { Vector3Int.left, Vector3Int.right, Vector3Int.up, Vector3Int.down };
            foreach (var pair in nodes) foreach (var offset in offsets)
            { var next = pair.Key + offset; if (nodes.ContainsKey(next) && !Obstacle((Point(next) + Point(pair.Key)) * .5f, .2f, next.z)) pair.Value.Add(next); }
            Debug.Log($"DZ_MAP_READY: {nodes.Count} walkable nodes, {StairLinks} stair links.");
        }
        bool Nearest(Vector2 point, int floor, out Vector3Int result, float radius = 1)
        {
            var center = Key(point, floor); result = center;
            if (nodes.ContainsKey(center)) return true;
            float best = radius * radius; bool found = false; int count = Mathf.CeilToInt(radius / Step);
            for (int x = -count; x <= count; x++) for (int y = -count; y <= count; y++)
            { var candidate = center + new Vector3Int(x, y, 0); float d = (Point(candidate) - point).sqrMagnitude; if (d <= best && nodes.ContainsKey(candidate)) { result = candidate; best = d; found = true; } }
            return found;
        }
        Flow Field(ZonePlayer target)
        {
            if (!Nearest(target.transform.position, target.gameObject.layer, out var goal, 2)) return null;
            if (!flows.TryGetValue(target, out var flow)) { flow = new Flow(); flows[target] = flow; }
            if (flow.Next.Count > 0 && flow.Goal == goal && Time.time < flow.Until) return flow;
            flow.Next.Clear(); flow.Goal = goal; flow.Until = Time.time + .75f;
            var queue = new Queue<Vector3Int>(); queue.Enqueue(goal); flow.Next[goal] = goal;
            while (queue.Count > 0) { var node = queue.Dequeue(); foreach (var adjacent in nodes[node]) if (!flow.Next.ContainsKey(adjacent)) { flow.Next[adjacent] = node; queue.Enqueue(adjacent); } }
            return flow;
        }
        public bool Reachable(Vector2 point, int floor, ZonePlayer target) => Nearest(point, floor, out var node, .6f) && Field(target)?.Next.ContainsKey(node) == true;
        public Vector2 Direction(Vector2 point, int floor, ZonePlayer target)
        {
            var flow = Field(target);
            if (flow != null) foreach (var route in StairRoutes)
            {
                Vector2 a = Point(route.lower), b = Point(route.upper), ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / Mathf.Max(.01f, ab.sqrMagnitude));
                if (Vector2.Distance(point, a + ab * t) > .65f || floor != route.lower.z && floor != route.upper.z) continue;
                if (flow.Next.TryGetValue(route.lower, out var up) && up == route.upper && Vector2.Distance(point, b) > .25f) return (b - point).normalized;
                if (flow.Next.TryGetValue(route.upper, out var down) && down == route.lower && Vector2.Distance(point, a) > .25f) return (a - point).normalized;
            }
            if (flow == null || !Nearest(point, floor, out var node, 1.2f) || !flow.Next.TryGetValue(node, out var next)) return Vector2.zero;
            if (node == flow.Goal) return ((Vector2)target.transform.position - point).normalized;
            return (Point(next) - point).normalized;
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
        void OnDestroy() { if (Current == this) Current = null; }
        public IEnumerable<Vector3Int> WalkableNodes => nodes.Keys;
        public Vector2 WorldPoint(Vector3Int node) => Point(node);
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
