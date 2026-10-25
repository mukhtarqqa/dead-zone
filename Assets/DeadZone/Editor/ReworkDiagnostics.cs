using System;
using UnityEditor;
using UnityEngine;
namespace DeadZone.Editor
{
    public static class ReworkDiagnostics
    {
        [MenuItem("Dead Zone/Rework/Check city entry and routes")]
        public static void CityRoutes()
        {
            var g=DeadZoneGame.Instance;var map=ZoneMap.Current;var city=UnityEngine.Object.FindAnyObjectByType<CityArena>();if(!EditorApplication.isPlaying||g==null||map==null)throw new Exception("Enter Play Mode first");
            if(city.Bounds.width<110||city.Bounds.height<86||city.Districts.Length<4)throw new Exception("City dimensions/districts incorrect");
            for(int i=0;i<4;i++){Vector2 point=city.TeamSpawn(i);if(!map.Walkable(point,20)||!map.Reachable(point,20,g.player))throw new Exception("Blocked team entry "+i);if(point.magnitude<30)throw new Exception("Entry is near the center");for(int j=0;j<i;j++)if(Vector2.Distance(point,city.TeamSpawn(j))<.7f)throw new Exception("Team entry overlaps");}
            foreach(var point in city.SupplyPoints)if(!map.Walkable(point,20)||!map.Reachable(point,20,g.player))throw new Exception("Supply point blocked: "+point);
            int count=0;foreach(var node in map.WalkableNodes){if(!map.Reachable(map.WorldPoint(node),node.z,g.player))throw new Exception("Isolated city node: "+node);count++;}
            Debug.Log("DZ_CITY_ROUTES_PASS: "+count+" connected nodes, 4 edge entries, "+city.SupplyPoints.Length+" reachable supplies, field rebuild "+map.LastFieldMilliseconds.ToString("F2")+" ms");
        }
        [MenuItem("Dead Zone/Rework/Preview city overview")]
        public static void Overview()
        {
            var g=DeadZoneGame.Instance;if(!EditorApplication.isPlaying||g==null)throw new Exception("Enter Play Mode");g.running=false;g.enabled=false;foreach(var actor in g.players){actor.enabled=false;actor.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;}g.gameCamera.orthographicSize=46;g.gameCamera.transform.position=new Vector3(0,0,-10);
        }
    }
}
