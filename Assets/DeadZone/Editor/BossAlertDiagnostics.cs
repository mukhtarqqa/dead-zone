using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DeadZone.Editor
{
    public static class BossAlertDiagnostics
    {
        static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static readonly MethodInfo Advance=typeof(DeadZoneGame).GetMethod("AdvanceBossCamera",Private);
        static readonly MethodInfo Prune=typeof(DeadZoneGame).GetMethod("PruneBossAlerts",Private);
        static readonly MethodInfo Populate=typeof(ZoneMinimap).GetMethod("OnPopulateMesh",Private,null,new[]{typeof(UnityEngine.UI.VertexHelper)},null);
        public static string Result {get;private set;}="Not run";
        static void Require(bool value,string message){if(!value)throw new Exception(message);}
        static void Near(Vector3 a,Vector3 b,string message,float tolerance=.015f){Require(Vector3.Distance(a,b)<tolerance,$"{message}: {a} / {b}");}

        [MenuItem("Dead Zone/Check boss spawn alerts")]
        public static void Check()
        {
            var g=DeadZoneGame.Instance;
            if(!EditorApplication.isPlaying||g==null||g.running)throw new Exception("Use a fresh Play session at the menu");
            Result="Running";var city=UnityEngine.Object.FindAnyObjectByType<CityArena>();
            var map=UnityEngine.Object.FindAnyObjectByType<ZoneMinimap>(FindObjectsInactive.Include);
            Require(city!=null&&map!=null,"City/minimap unavailable");
            var created=new List<GameObject>();bool recording=ZoneProfile.SuppressRecording;
            ZoneProfile.SuppressRecording=true;g.Begin();g.enabled=false;
            var actor=g.player;actor.IsLocal=false;actor.enabled=false;
            float zoom=g.gameCamera.orthographicSize,z=g.gameCamera.transform.position.z;
            void NoWaves(){typeof(DeadZoneGame).GetField("pendingEnemies",Private).SetValue(g,0);typeof(DeadZoneGame).GetField("breakUntil",Private).SetValue(g,float.MaxValue);}
            NoWaves();
            ZoneEnemy Spawn(Vector2 position)
            {
                int count=g.enemies.Count;g.SpawnEnemy(0,true);
                Require(g.enemies.Count==count+1,"Actual boss spawn failed");
                var e=g.enemies[g.enemies.Count-1];created.Add(e.gameObject);e.transform.position=position;
                e.enabled=false;e.GetComponent<ZoneBossBrain>().enabled=false;e.GetComponent<Rigidbody2D>().simulated=false;e.ReadyAt=0;
                return e;
            }
            Vector3 Follow(Vector2 p)
            {
                actor.transform.position=p;actor.aimPoint=p;
                return city.ClampCamera(new Vector3(p.x,p.y,z),g.gameCamera);
            }
            Vector3 Tick(float dt,Vector3 follow,ZonePlayer tracked=null)
            {
                Prune.Invoke(g,null);
                var point=(Vector3)Advance.Invoke(g,new object[]{follow,tracked!=null?tracked:actor,dt});
                g.gameCamera.transform.position=city.ClampCamera(point,g.gameCamera);return g.gameCamera.transform.position;
            }
            int Vertices()
            {
                using var vh=new UnityEngine.UI.VertexHelper();Populate.Invoke(map,new object[]{vh});return vh.currentVertCount;
            }
            void Clear()
            {
                g.ResetBossAlerts();
                foreach(var obj in created)if(obj!=null){var e=obj.GetComponent<ZoneEnemy>();if(e!=null)g.enemies.Remove(e);UnityEngine.Object.DestroyImmediate(obj);}
                created.Clear();
            }
            void Cleanup()
            {
                Clear();g.gameCamera.orthographicSize=zoom;var cp=g.gameCamera.transform.position;cp.z=z;g.gameCamera.transform.position=cp;
                actor.Dead=false;actor.Downed=false;actor.enabled=true;actor.IsLocal=true;actor.SetCommand(default);
                actor.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;g.enabled=true;g.running=false;g.paused=false;
                ZoneProfile.SuppressRecording=recording;
            }
            try
            {
                // Same duration and easing for nearby and distant targets, preserving boundaries and Z.
                foreach(var destination in new[]{new Vector2(.1f,-11),new Vector2(36,27),new Vector2(55,43)})
                {
                    Clear();var follow=Follow(new Vector2(0,-11));g.gameCamera.transform.position=follow;
                    int baseline=Vertices();var e=Spawn(destination);g.NotifyBossSpawned(e);g.NotifyBossSpawned(e);
                    map.BossMarkerPosition(destination,out bool atEdge);
                    Require(g.BossTargets.Count==1&&Vertices()==baseline+(atEdge?15:16),"Duplicate/missing boss marker");
                    var aim=city.ClampCamera(new Vector3(destination.x,destination.y,z),g.gameCamera);
                    Near(Tick(.25f,follow),Vector3.Lerp(follow,aim,.5f),"Smooth approach midpoint");
                    Near(Tick(.25f,follow),aim,"Arrival at 0.5s");
                    Require(g.BossFocusPhase==DeadZoneGame.BossCameraPhase.Hold,"No hold phase");
                    e.transform.position+=(Vector3)Vector2.left;
                    aim=city.ClampCamera(new Vector3(e.transform.position.x,e.transform.position.y,z),g.gameCamera);
                    Near(Tick(.5f,follow),aim,"Moving boss hold");Tick(.5f,follow);
                    Require(g.BossFocusPhase==DeadZoneGame.BossCameraPhase.Return,"Hold must last 1s");
                    follow=Follow(new Vector2(2,-12));Near(Tick(.25f,follow),Vector3.Lerp(aim,follow,.5f),"Smooth return midpoint");
                    Near(Tick(.25f,follow),follow,"Return at 2s");
                    Require(g.BossFocusPhase==DeadZoneGame.BossCameraPhase.Follow,"Follow did not resume");
                    Near(Tick(.01f,follow),follow,"No snap after return");
                    Require(g.gameCamera.orthographicSize==zoom&&g.gameCamera.transform.position.z==z,"Zoom/Z changed");
                    var center=map.BossMarkerPosition(city.Bounds.center,out bool edge);Near(center,map.rectTransform.rect.center,"Minimap center conversion");Require(!edge,"Center clamped");
                    var moved=map.BossMarkerPosition(city.Bounds.center+new Vector2(4,2),out edge);
                    float scale=Mathf.Min(map.rectTransform.rect.width/city.Bounds.width,map.rectTransform.rect.height/city.Bounds.height);
                    Near(moved-center,new Vector2(4,2)*scale,"Minimap tracking conversion");
                    var outside=map.BossMarkerPosition(city.Bounds.max+Vector2.one*100,out edge);Require(edge&&map.rectTransform.rect.Contains(outside),"Missing edge indicator");
                    e.Hit(e.health+1,actor);Require(g.BossTargets.Count==0&&Vertices()==baseline,"Death did not remove marker");
                }
                // Destroy during approach; dead entries in the queue are skipped without snapping.
                Clear();var home=Follow(new Vector2(0,-11));g.gameCamera.transform.position=home;
                var first=Spawn(new Vector2(25,15));var second=Spawn(new Vector2(-25,15));
                Tick(.25f,home);Require(g.BossFocusTarget==first.transform,"FIFO queue order");
                var before=g.gameCamera.transform.position;g.enemies.Remove(first);UnityEngine.Object.DestroyImmediate(first.gameObject);
                Near(Tick(0,home),before,"Destroyed boss caused snap");Require(g.BossFocusPhase==DeadZoneGame.BossCameraPhase.Return,"Destroyed boss not cancelled");
                Tick(.5f,home);Tick(0,home);Require(g.BossFocusTarget==second.transform,"Second boss never introduced");
                // Death with a surviving boss returns to the active spectator; respawn does not replay.
                actor.Dead=true;before=g.gameCamera.transform.position;Near(Tick(0,home),before,"Player death snap");Tick(.5f,home);
                Require(g.BossFocusPhase==DeadZoneGame.BossCameraPhase.Follow,"Dead player left camera locked");
                actor.Dead=false;g.NotifyBossSpawned(second);Tick(.01f,home);Require(g.BossFocusPhase==DeadZoneGame.BossCameraPhase.Follow,"Repeated intro after respawn");
                // Moving spectator target during a live introduction also cancels safely.
                Clear();first=Spawn(new Vector2(20,20));Tick(.2f,home);
                var alternateObject=new GameObject("Boss alert spectator test");created.Add(alternateObject);
                var alternate=alternateObject.AddComponent<ZonePlayer>();alternate.enabled=false;
                Tick(0,home,alternate);Require(g.BossFocusPhase==DeadZoneGame.BossCameraPhase.Return,"Spectator switch did not cancel");Tick(.5f,home,alternate);
                Clear();first=Spawn(new Vector2(20,20));Tick(.2f,home);
                g.player=null;
                try{typeof(DeadZoneGame).GetMethod("LateUpdate",Private).Invoke(g,null);}finally{g.player=actor;}
                Require(g.BossFocusPhase==DeadZoneGame.BossCameraPhase.Return,"Missing player did not release boss focus");Tick(.5f,home);
                // A pending introduction whose boss was removed is never started.
                Clear();first=Spawn(new Vector2(20,20));g.enemies.Remove(first);Tick(.01f,home);
                Require(g.BossTargets.Count==0&&g.BossFocusPhase==DeadZoneGame.BossCameraPhase.Follow,"Removed queued boss shown");
                // Network snapshots reuse the same registration, with no protocol change.
                Clear();var obj=new GameObject("Boss alert network test");created.Add(obj);obj.AddComponent<SpriteRenderer>();var ghost=obj.AddComponent<ZoneGhost>();
                var state=new ZoneObjectState{type=1,boss=true,health=1000,maxHealth=1000,scale=1.6f,floor=20,position=new Vector2(10,12)};
                for(int i=0;i<20;i++)ghost.Apply(g,state);
                Require(g.BossTargets.Count==1,"Repeated snapshot duplicated marker");Tick(.5f,home);Tick(1,home);Tick(.5f,home);
                ghost.Apply(g,state);Tick(.01f,home);Require(g.BossFocusPhase==DeadZoneGame.BossCameraPhase.Follow,"Snapshot replayed introduction");
                state.health=0;ghost.Apply(g,state);Require(g.BossTargets.Count==0,"Network death retained marker");
                Clear();g.running=false;g.Begin();NoWaves();Require(g.BossTargets.Count==0&&g.BossFocusPhase==DeadZoneGame.BossCameraPhase.Follow,"Restart retained alerts");
                Debug.Log("DZ_BOSS_ALERT_LOGIC_PASS: near/far/boundary 0.5+1+0.5s, moving targets, one marker, projection/edge, death/destroy/removal, FIFO queue, player death/spectator/respawn, snapshots and restart");

                // Real player loop: normal camera LateUpdate, movement and firing remain active.
                Clear();actor.ResetForMatch(0);actor.IsLocal=false;actor.enabled=true;
                home=Follow(new Vector2(0,-11));g.gameCamera.transform.position=home;
                g.enabled=true;NoWaves();float started=0,holdAt=-1,returnAt=-1;
                int settleAt=Time.frameCount+3,initialFrame=0,shots=0;Vector2 origin=actor.transform.position;float health=actor.health;
                bool liveStarted=false;
                double deadline=EditorApplication.timeSinceStartup+8;
                void Poll()
                {
                    if(!EditorApplication.isPlaying||g==null){EditorApplication.update-=Poll;ZoneProfile.SuppressRecording=recording;return;}
                    try
                    {
                        // Heavy deterministic checks share one frame. Let its large delta pass before timing live play.
                        if(!liveStarted)
                        {
                            if(Time.frameCount<settleAt)return;
                            first=Spawn(new Vector2(30,20));started=Time.unscaledTime;initialFrame=Time.frameCount;
                            shots=actor.ShotsFired;origin=actor.transform.position;health=actor.health;liveStarted=true;
                        }
                        actor.SetCommand(new ZoneCommand{move=Vector2.down,aim=(Vector2)actor.transform.position+Vector2.down*8,fire=true});
                        float elapsed=Time.unscaledTime-started;
                        if(g.BossFocusPhase==DeadZoneGame.BossCameraPhase.Hold&&holdAt<0)holdAt=elapsed;
                        if(g.BossFocusPhase==DeadZoneGame.BossCameraPhase.Return&&returnAt<0)returnAt=elapsed;
                        if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Live introduction timed out");
                        if(elapsed<1.8f||g.BossFocusPhase!=DeadZoneGame.BossCameraPhase.Follow)return;
                        Require(holdAt>=.4f&&holdAt<=.75f&&returnAt>=1.35f&&returnAt<=1.8f&&elapsed<=2.4f,$"Live intro timing: hold={holdAt:F2}, return={returnAt:F2}, done={elapsed:F2}");
                        Require(Time.frameCount>initialFrame+10&&Vector2.Distance(origin,actor.transform.position)>.2f&&actor.ShotsFired>shots,"Movement/fire blocked");
                        Require(actor.health==health&&!g.InputBlocked&&g.gameCamera.orthographicSize==zoom,"Gameplay/camera settings modified");
                        Result=$"PASS: hold={holdAt:F2}s return={returnAt:F2}s complete={elapsed:F2}s frames={Time.frameCount-initialFrame} shots={actor.ShotsFired-shots}";
                        EditorApplication.update-=Poll;Cleanup();Debug.Log("DZ_BOSS_ALERT_LIVE_PASS: "+Result);
                    }
                    catch(Exception error){EditorApplication.update-=Poll;Cleanup();Result="FAIL: "+error.Message;Debug.LogException(error);}
                }
                EditorApplication.update+=Poll;
            }
            catch(Exception error){Cleanup();Result="FAIL: "+error.Message;Debug.LogException(error);}
        }
    }
}
