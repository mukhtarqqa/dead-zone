using UnityEngine;
namespace DeadZone
{
    public sealed class CityArena : MonoBehaviour
    {
        public Rect Bounds = new(-28, -22, 56, 44);
        public Vector3 ClampCamera(Vector3 point, Camera camera)
        {
            float halfHeight = camera.orthographicSize, halfWidth = halfHeight * camera.aspect;
            point.x = Mathf.Clamp(point.x, Bounds.xMin + halfWidth, Bounds.xMax - halfWidth);
            point.y = Mathf.Clamp(point.y, Bounds.yMin + halfHeight, Bounds.yMax - halfHeight);
            return point;
        }
    }
}
