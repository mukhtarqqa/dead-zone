using UnityEngine;

namespace DeadZone
{
    // Small, deterministic pixel assets share the same palette and point filtering as the guns.
    public static class ZoneArt
    {
        static readonly Sprite[] flashes = new Sprite[2];
        public static Sprite MuzzleFlash(int frame)
        {
            frame &= 1; if(flashes[frame]!=null)return flashes[frame];
            var texture=new Texture2D(32,16){filterMode=FilterMode.Point}; var pixels=new Color[512];
            for(int y=0;y<16;y++)for(int x=0;x<32;x++)
            {
                int dy=Mathf.Abs(y-8); bool core=x<24 && dy<2; bool flare=x<20 && dy<(x<7?2:x<13?6:4);
                bool sparks=frame==0 ? (x>22 && x<27 && (dy==4||dy==5)) : (x>25 && dy<2);
                if(core||flare||sparks)pixels[y*32+x]=core?new Color(1,1,.8f):new Color(1,.65f,.12f);
            }
            texture.SetPixels(pixels);texture.Apply();
            return flashes[frame]=Sprite.Create(texture,new Rect(0,0,32,16),new Vector2(0,.5f),32);
        }
        static readonly Sprite[] cache = new Sprite[5];
        public static Sprite Item(int kind) // grenade, mine, supply crate, magazine, casing
        {
            if (cache[kind] != null) return cache[kind];
            var texture = new Texture2D(16, 16) { filterMode = FilterMode.Point, name = "DeadZone item " + kind };
            var pixels = new Color[256];
            Color edge = new(.08f, .12f, .12f), green = new(.3f, .42f, .2f), light = new(.6f, .67f, .37f), metal = new(.55f, .6f, .61f), gold = new(.92f, .65f, .22f);
            void R(int x, int y, int w, int h, Color c) { for (int j = y; j < y + h; j++) for (int i = x; i < x + w; i++) pixels[j * 16 + i] = c; }
            if (kind == 0) { R(4, 2, 8, 10, edge); R(3, 4, 10, 6, edge); R(5, 3, 6, 8, green); R(4, 5, 8, 4, green); R(5, 8, 2, 3, light); R(8, 3, 1, 8, edge); R(4, 6, 8, 1, edge); R(6, 12, 4, 2, metal); R(10, 10, 2, 5, metal); }
            if (kind == 1) { R(2, 4, 12, 7, edge); R(1, 5, 14, 4, edge); R(3, 5, 10, 5, green); R(4, 9, 8, 2, light); R(5, 10, 6, 2, edge); R(6, 11, 4, 2, metal); R(7, 7, 2, 2, Color.red); R(2, 5, 2, 2, metal); R(12, 5, 2, 2, metal); }
            if (kind == 2) { R(1, 2, 14, 12, edge); R(2, 3, 12, 10, green); R(2, 10, 12, 2, light); R(4, 3, 1, 7, edge); R(11, 3, 1, 7, edge); R(6, 6, 4, 3, gold); R(7, 5, 2, 5, gold); }
            if (kind == 3) { R(5, 2, 6, 12, edge); R(6, 3, 4, 10, metal); R(6, 5, 4, 1, edge); R(6, 8, 4, 1, edge); R(6, 12, 4, 2, gold); }
            if (kind == 4) { R(6, 4, 4, 8, edge); R(7, 5, 2, 7, gold); R(6, 4, 4, 2, light); }
            texture.SetPixels(pixels); texture.Apply();
            return cache[kind] = Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(.5f, .5f), 16);
        }
    }

    public sealed class ZonePulse : MonoBehaviour
    {
        public int Floor, Kind;
        public float Radius = 1, Duration = .5f;
        public Vector2 Direction = Vector2.right;
        public float Age => Mathf.Max(0, Time.time - born);
        public void SetClock(float duration, float remaining) { Duration = duration; born = Time.time - Mathf.Max(0, duration - remaining); }
        public float Remaining => Mathf.Max(0, Duration - (Time.time - born));
        float born;
        LineRenderer detail;
        LineRenderer line;
        static Material material;
        public static void Spawn(Vector2 point, int floor, float radius, int kind, float duration, Vector2 direction = default)
        {
            var obj = new GameObject("Zone effect"); obj.transform.position = point;
            var pulse = obj.AddComponent<ZonePulse>(); pulse.Floor = floor; pulse.Radius = radius; pulse.Kind = kind; pulse.Duration = duration; pulse.Direction = direction == default ? Vector2.right : direction;
        }
        void Start()
        {
            if (born == 0) born = Time.time;
            if (material == null) material = new Material(Shader.Find("Sprites/Default"));
            line = gameObject.AddComponent<LineRenderer>(); line.sharedMaterial = material; line.useWorldSpace = false; line.loop = true; line.positionCount = 40; line.startWidth = line.endWidth = .055f;
            line.sortingLayerID = ZoneMap.Sorting(Floor); line.sortingOrder = 50;
            var obj = new GameObject("Ability detail"); obj.transform.SetParent(transform,false);
            detail = obj.AddComponent<LineRenderer>(); detail.sharedMaterial = material; detail.useWorldSpace = false;
            detail.sortingLayerID = line.sortingLayerID; detail.sortingOrder = 51; detail.startWidth = detail.endWidth = .035f;
            detail.positionCount = 0;
        }
        void Update()
        {
            float t = (Time.time - born) / Duration;
            if (t >= 1) { Destroy(gameObject); return; }
            Color color = Kind == 1 ? new Color(1,.2f,.3f) : Kind == 2 || Kind == 9 ? new Color(.35f,1,.7f) : Kind == 3 || Kind == 7 || Kind == 12 ? new Color(.7f,.3f,1) : Kind == 4 || Kind == 6 || Kind == 8 ? new Color(.25f,.8f,1) : Kind == 5 || Kind == 11 ? new Color(1,.12f,.3f) : new Color(1,.65f,.16f);
            color.a = Kind == 3 ? Mathf.Min(1,Remaining) * (.65f + .2f * Mathf.Sin(Time.time*5)) : 1-t;
            line.startColor = line.endColor = detail.startColor = detail.endColor = color;
            float r = Radius * (Kind == 1 ? 1-t*.4f : Kind == 3 ? 1 : Kind == 7 ? 1-t : .25f+.75f*t);
            line.positionCount = 80;
            for (int i=0;i<80;i++)
            {
                float a=i*Mathf.PI*2/80;
                Vector3 p=new Vector3(Mathf.Cos(a),Mathf.Sin(a))*r;
                if (Kind == 4) p *= i%4 == 0 ? 1 : .83f;
                if (Kind == 6) p=new Vector3(Mathf.Cos(a),Mathf.Sin(2*a)*.5f)*r;
                if (Kind == 3) p.y *= .65f;
                line.SetPosition(i,p);
            }
            if (Kind == 3)
            {
                detail.positionCount=5; float a=Age*2;
                detail.SetPosition(0,new Vector3(Mathf.Cos(a),Mathf.Sin(a)*.65f)*r*.8f);
                detail.SetPosition(1,Vector3.zero);
                detail.SetPosition(2,new Vector3(Mathf.Cos(a/12),Mathf.Sin(a/12)*.65f)*r*.5f);
                detail.SetPosition(3,Vector3.zero); detail.SetPosition(4,Vector3.up*r*.15f);
            }
            else if (Kind == 11 || Kind == 13)
            {
                line.enabled=false; detail.positionCount=9; detail.startWidth=detail.endWidth=(Kind==13?.16f:.12f)*(1-t);
                Vector2 normal=new Vector2(-Direction.y,Direction.x);
                for(int i=0;i<9;i++) detail.SetPosition(i,Direction*Radius*i/8+normal*(i==0||i==8?0:Kind==13?0:Mathf.Sin(i*3+Age*45)*.14f));
            }
            else if (Kind == 4 || Kind == 5 || Kind == 7 || Kind == 9 || Kind == 12)
            {
                detail.positionCount=49;
                for(int i=0;i<49;i++) { float a=i*Mathf.PI*2/48+Age*(Kind==5?6:-3); float rr=i%2==0?r*.65f:r*(Kind==4?.35f:.9f); detail.SetPosition(i,new Vector3(Mathf.Cos(a),Mathf.Sin(a))*rr); }
            }

        }
    }

    public sealed class ZoneDebris : MonoBehaviour
    {
        public Vector2 Velocity;
        float lifetime;
        public static void Spawn(Vector2 point, int floor, bool shell, int weapon = 0)
        {
            var obj = new GameObject(shell ? "Ejected casing" : "Dropped magazine"); obj.transform.position = point; obj.transform.localScale = Vector3.one * (shell ? .25f : .35f);
            var sr = obj.AddComponent<SpriteRenderer>(); sr.sprite = ZoneArt.Item(shell ? 4 : weapon == 2 ? 2 : 3); sr.sortingLayerID = ZoneMap.Sorting(floor); sr.sortingOrder = 40;
            obj.AddComponent<ZoneDebris>().Velocity = new Vector2(Random.Range(-1f, 1f), Random.Range(.5f, 1.2f));
        }
        void Update() { lifetime += Time.deltaTime; if (lifetime < .3f) { transform.position += (Vector3)Velocity * Time.deltaTime; transform.Rotate(0, 0, 360 * Time.deltaTime); } if (lifetime > 1) Destroy(gameObject); }
    }

    public sealed class ZoneGrenade : MonoBehaviour
    {
        public ZonePlayer owner;
        public Vector2 origin, destination;
        float age;
        void Start()
        {
            var sr = gameObject.AddComponent<SpriteRenderer>(); sr.sprite = ZoneArt.Item(0); sr.sortingLayerID = ZoneMap.Sorting(gameObject.layer); sr.sortingOrder = 100;
            transform.localScale = Vector3.one * .45f; transform.position = origin;
        }
        void Update()
        {
            var game = DeadZoneGame.Instance;
            if (game == null || !game.running) { Destroy(gameObject); return; }
            age += Time.deltaTime; float t = Mathf.Clamp01(age / .8f);
            transform.position = Vector2.Lerp(origin, destination, t) + Vector2.up * Mathf.Sin(t * Mathf.PI) * 1.2f;
            transform.Rotate(0, 0, 360 * Time.deltaTime);
            if (age >= .8f) { game.Blast(destination, 2, 100, owner, gameObject.layer); Destroy(gameObject); }
        }
    }
}
