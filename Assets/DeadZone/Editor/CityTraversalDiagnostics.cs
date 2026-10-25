using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
namespace DeadZone.Editor
{
    public static class CityTraversalDiagnostics
    {
        [MenuItem("Dead Zone/Rework/Traverse all districts with four heroes")]
        public static void Run()
        {
            var g=DeadZoneGame.Instance;var city=UnityEngine.Object.FindAnyObjectByType<CityArena>();if(!EditorApplication.isPlaying||g==null||city==null)throw new Exception("Fresh Play Mode required");
            ZoneProfile.SuppressRecording=true;g.selected=0;while(g.players.Count<4)g.CreatePeer((ulong)(900+g.players.Count),g.players.Count);if(!g.running)g.Begin();
            typeof(DeadZoneGame).GetField("nextSpawn",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(g,float.MaxValue);foreach(var e in g.enemies.ToArray())UnityEngine.Object.Destroy(e.gameObject);g.enemies.Clear();
            var points=new[]{new Vector2(-37,-17),new Vector2(33,-26),new Vector2(34,25),new Vector2(-36,27),new Vector2(0,-11)};var goals=new ZonePlayer[4];var steps=new int[4];var last=new Vector2[4];var travelled=new float[4];var stuck=new float[4];int startFrame=Time.frameCount;float previous=Time.time;double deadline=EditorApplication.timeSinceStartup+100;
            for(int i=0;i<4;i++){var actor=g.players[i];actor.IsLocal=false;actor.health=actor.maxHealth;last[i]=actor.transform.position;var obj=new GameObject("Traversal target "+i);obj.layer=20;goals[i]=obj.AddComponent<ZonePlayer>();goals[i].enabled=false;}
            Time.timeScale=4;
            void Poll()
            {
                if(!EditorApplication.isPlaying||g==null){EditorApplication.update-=Poll;return;}
                try
                {
                    if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Physical traversal timed out");float dt=Time.time-previous;previous=Time.time;bool complete=true;
                    for(int i=0;i<4;i++)
                    {
                        var actor=g.players[i];Vector2 position=actor.transform.position;float moved=Vector2.Distance(position,last[i]);travelled[i]+=moved;last[i]=position;
                        if(steps[i]>points.Length){actor.SetCommand(default);continue;}complete=false;
                        Vector2 destination=steps[i]==points.Length?city.TeamSpawn(i):points[steps[i]]+new Vector2((i%2-.5f)*1.5f,(i/2-.5f)*1.5f);
                        if(!ZoneMap.Current.Walkable(destination,20))throw new Exception("Blocked physical checkpoint "+steps[i]+" hero "+i+" "+destination);goals[i].transform.position=destination;
                        if(Vector2.Distance(position,destination)<.65f){steps[i]++;stuck[i]=0;Debug.Log("DZ_TRAVERSE_CHECKPOINT: hero "+i+" stage "+steps[i]);continue;}
                        Vector2 direction=ZoneMap.Current.Direction(position,20,goals[i]);actor.SetCommand(new ZoneCommand{move=direction,aim=position+direction*4});stuck[i]=moved<.01f?stuck[i]+dt:0;
                        if(stuck[i]>8)throw new Exception("Hero "+i+" stuck at "+position+" toward "+destination+" direction "+direction);
                        if(ZoneMap.Obstacle(position,.13f,20))throw new Exception("Hero inside level collider at "+position);
                    }
                    if(!complete)return;for(int i=0;i<4;i++)if(travelled[i]<240)throw new Exception("Traversal too short "+travelled[i]);
                    EditorApplication.update-=Poll;Time.timeScale=1;foreach(var target in goals)UnityEngine.Object.Destroy(target.gameObject);g.player.IsLocal=true;
                    Debug.Log("DZ_CITY_PHYSICAL_PASS: 4 hero rigidbodies, 24 checkpoints, distances "+string.Join(",",Array.ConvertAll(travelled,d=>d.ToString("F1")))+"m, no wall penetration/stuck, frames "+startFrame+"->"+Time.frameCount);
                }
                catch(Exception error){EditorApplication.update-=Poll;Time.timeScale=1;g.player.IsLocal=true;foreach(var target in goals)if(target!=null)UnityEngine.Object.Destroy(target.gameObject);Debug.LogException(error);}
            }
            EditorApplication.update+=Poll;
        }
    }
}
