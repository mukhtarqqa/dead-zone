using UnityEngine;
namespace DeadZone
{
    [System.Serializable] public sealed class CityDistrict
    {
        public string title;public Rect bounds;public Color color;
    }
    public sealed class CityArena : MonoBehaviour
    {
        public Rect Bounds = new(-56, -44, 112, 88);
        public Vector2 Entry=new(-49,-38);
        public CityDistrict[] Districts=System.Array.Empty<CityDistrict>();
        public Vector2[] SupplyPoints=System.Array.Empty<Vector2>();
        public Vector2 TeamSpawn(int slot)
        {
            Vector2 origin=Bounds.Contains(Entry)?Entry:Bounds.min+Vector2.one*7;
            return origin+new Vector2(slot%2,slot/2)*.9f;
        }
        public CityDistrict DistrictAt(Vector2 point){foreach(var district in Districts)if(district.bounds.Contains(point))return district;return null;}
        public string AreaName(Vector2 point)=>DistrictAt(point)?.title??"Центральный проспект";
        public Vector3 ClampCamera(Vector3 point, Camera camera)
        {
            float halfHeight = camera.orthographicSize, halfWidth = halfHeight * camera.aspect;
            point.x = Mathf.Clamp(point.x, Bounds.xMin + halfWidth, Bounds.xMax - halfWidth);
            point.y = Mathf.Clamp(point.y, Bounds.yMin + halfHeight, Bounds.yMax - halfHeight);
            return point;
        }
    }
}
