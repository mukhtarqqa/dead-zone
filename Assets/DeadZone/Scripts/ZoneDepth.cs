using UnityEngine;
namespace DeadZone
{
    public static class ZoneDepth
    {
        public static int Order(float feetY)=>1000-Mathf.RoundToInt(feetY*10);
    }
}
