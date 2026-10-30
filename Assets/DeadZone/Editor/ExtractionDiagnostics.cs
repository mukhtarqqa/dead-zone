using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DeadZone.Editor
{
    public static class ExtractionDiagnostics
    {
        static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        public static string Result {get;private set;}="Not run";
        static void Require(bool value,string message){if(!value)throw new Exception(message);}

        [MenuItem("Dead Zone/Extraction/Check objective and evacuation")]
        public static void Check()
        {
            var g=DeadZoneGame.Instance;
            if(!EditorApplication.isPlaying||g==null||g.running||g.IntroPlaying)throw new Exception("Use a fresh Play session at the main menu.");
            bool previousRecording=ZoneProfile.SuppressRecording;ZoneProfile.SuppressRecording=true;
            var peers=new List<ZonePlayer>();Result="Running";
            try
            {
                g.StartGameplay();g.enabled=false;g.player.enabled=false;
                typeof(DeadZoneGame).GetField("pendingEnemies",Private).SetValue(g,0);
                for(int i=1;i<4;i++){var p=g.CreatePeer((ulong)(700+i),i);p.enabled=false;peers.Add(p);}
                Require(g.BossesDefeated==0&&!g.Extracting&&g.ExtractionCrew==4,"Fresh match objective/crew incorrect");
                var camera=g.gameCamera;float zoom=camera.orthographicSize,z=camera.transform.position.z;
                for(int wave=1;wave<=5;wave++)
                {
                    g.wave=wave;int count=g.enemies.Count;g.SpawnEnemy(wave-1,true);
                    Require(g.enemies.Count==count+1,"Real boss spawn failed");var boss=g.enemies[g.enemies.Count-1];boss.ReadyAt=0;
                    if(wave==5){peers[1].Dead=true;peers[1].health=0;}
                    boss.Hit(boss.health+1,g.player);
                    Require(g.BossesDefeated==wave,"Boss objective did not progress");
                    g.EnemyKilled(boss);Require(g.BossesDefeated==wave,"Duplicate boss death counted twice");
                    if(wave<5)Require(!g.Extracting&&!g.ended,"Evacuation or victory before five bosses");
                }
                Require(g.ExtractionPhase==ZoneExtractionPhase.Arriving&&g.running&&!g.ended&&!g.Victory,"Fifth boss must begin evacuation rather than victory");
                Require(!peers[1].Dead&&peers[1].health>0,"Fallen teammate cannot reach evacuation");
                Require(g.Helicopter!=null&&g.Helicopter.MainRotor!=null&&g.Helicopter.TailRotor!=null,"Editable helicopter/rotors missing");
                Require(ZoneMap.Current.Walkable(g.ExtractionLanding,20),"Landing zone is not walkable");
                foreach(var actor in g.players)Require(ZoneMap.Current.Reachable(g.ExtractionLanding,20,actor),"Landing zone not reachable for crew");
                g.player.transform.position=g.ExtractionLanding;Require(!g.TryBoardHelicopter(g.player),"Boarding while airborne");
                g.paused=true;float frozen=g.ExtractionAge;g.TickExtraction(2);Require(g.ExtractionAge==frozen,"Pause advanced evacuation");g.paused=false;
                var view=g.Helicopter;var renderer=view.Body.GetComponentInChildren<Renderer>();
                g.TickExtraction(3.5f);Require(Vector2.Distance(view.transform.position,g.ExtractionLanding)>1,"Arrival skipped flight");
                var advance=typeof(DeadZoneGame).GetMethod("ExtractionCamera",Private);
                Vector3 follow=new Vector3(g.player.transform.position.x,g.player.transform.position.y,z);
                var cameraArgs=new object[]{follow,.1f,Vector3.zero};Require((bool)advance.Invoke(g,cameraArgs),"Arrival camera override absent");
                camera.transform.position=(Vector3)cameraArgs[2];Require(camera.transform.position.z==z,"Arrival camera changed Z");
                float away=Vector3.Distance(camera.transform.position,follow);g.TickExtraction(3.4f);
                cameraArgs=new object[]{follow,.1f,Vector3.zero};advance.Invoke(g,cameraArgs);camera.transform.position=(Vector3)cameraArgs[2];
                Require(Vector3.Distance(camera.transform.position,follow)<away,"Camera did not return to the player");
                g.TickExtraction(.11f);Require(g.ExtractionPhase==ZoneExtractionPhase.Boarding&&!g.InputBlocked,"Player control not restored after landing");
                Require(Vector2.Distance(view.transform.position,g.ExtractionLanding)<.001f,"Helicopter did not land at its marker");
                int order=renderer.sortingOrder;g.TickExtraction(.01f);g.TickExtraction(.01f);Require(renderer.sortingOrder==order,"Helicopter depth drifts between frames");
                g.player.transform.position=g.ExtractionLanding+Vector2.right*8;Require(!g.TryBoardHelicopter(g.player),"Boarding from outside radius");
                g.player.transform.position=g.ExtractionLanding;g.player.gameObject.layer=21;Require(!g.TryBoardHelicopter(g.player),"Boarding from a different floor");g.player.gameObject.layer=20;
                g.player.IsLocal=false;g.player.SetCommand(new ZoneCommand{interact=true,aim=g.ExtractionLanding});
                typeof(ZonePlayer).GetMethod("Update",Private).Invoke(g.player,null);g.player.IsLocal=true;
                Require(g.player.Boarded&&g.ExtractionSeated==1,"Authoritative E command did not seat first passenger");
                Require(!g.TryBoardHelicopter(g.player)&&g.ExtractionSeated==1,"Duplicate boarding counted twice");
                foreach(var peer in peers){peer.transform.position=g.ExtractionLanding;peer.SetCommand(new ZoneCommand{interact=true,aim=g.ExtractionLanding});typeof(ZonePlayer).GetMethod("Update",Private).Invoke(peer,null);Require(peer.Boarded,"Peer E command could not board");}
                Require(g.ExtractionPhase==ZoneExtractionPhase.Departing&&g.ExtractionSeated==4&&!g.ended,"Departure must wait for all four passengers");
                foreach(var actor in g.players)Require(!actor.GetComponent<Collider2D>().enabled&&!actor.GetComponent<Rigidbody2D>().simulated,"Boarded passenger still has movement/collision");
                var snapshot=(ZoneWorld)typeof(ZoneNetwork).GetMethod("Snapshot",Private).Invoke(g.network,new object[]{g.player.OwnerId});
                Require(snapshot.bossesDefeated==5&&snapshot.extractionPhase==(int)ZoneExtractionPhase.Departing,"Actual host snapshot omits evacuation state");
                snapshot.players=new[]{new ZonePlayerState{boarded=true,connected=true,hero=0,floor=20,health=100,maxHealth=100}};
                var decoded=JsonUtility.FromJson<ZoneWorld>(ZoneWorldCodec.Decode(ZoneWorldCodec.Encode(JsonUtility.ToJson(snapshot))));
                Require(decoded.players[0].boarded&&decoded.bossesDefeated==5&&decoded.extractionPhase==3&&decoded.extractionLanding==g.ExtractionLanding,"Compressed world lost crew or flight data");
                Require(new ZoneHello().protocol==10,"Evacuation requires matching protocol versions");
                cameraArgs=new object[]{follow,.1f,Vector3.zero};Require((bool)advance.Invoke(g,cameraArgs),"Takeoff camera focus absent");
                g.TickExtraction(2);Require(!g.ended&&Vector2.Distance(view.transform.position,g.ExtractionLanding)>1,"Victory before helicopter departure");
                g.TickExtraction(DeadZoneGame.HelicopterDepartureSeconds);
                Require(g.ExtractionPhase==ZoneExtractionPhase.Complete&&g.ended&&g.Victory&&!g.running,"Departure did not produce victory");
                Require(Vector2.Distance(view.transform.position,g.ExtractionExit)<.001f,"Helicopter never left the map");
                Require(camera.orthographicSize==zoom&&camera.transform.position.z==z,"Extraction changed camera zoom/Z");

                foreach(var peer in peers){g.players.Remove(peer);UnityEngine.Object.DestroyImmediate(peer.gameObject);}peers.Clear();
                g.StartGameplay();Require(g.BossesDefeated==0&&!g.Victory&&!g.Extracting&&!g.player.Boarded&&g.player.GetComponent<Collider2D>().enabled,"Restart retained evacuation state");
                typeof(DeadZoneGame).GetField("pendingEnemies",Private).SetValue(g,0);
                // Apply the actual serialized state as a late replica, without replaying arrival.
                decoded.extractionPhase=2;decoded.extractionAge=4;g.ApplyExtraction(decoded);
                Require(g.ExtractionPhase==ZoneExtractionPhase.Boarding&&Mathf.Abs(g.ExtractionAge-4)<.001f,"Late snapshot replayed arrival");
                g.ApplyExtraction(new ZoneWorld());Require(!g.Extracting&&g.BossesDefeated==0,"Remote restart did not clear evacuation");
                for(int wave=1;wave<=5;wave++){g.wave=wave;g.SpawnEnemy(wave-1,true);var boss=g.enemies[g.enemies.Count-1];boss.ReadyAt=0;boss.Hit(boss.health+1,g.player);}
                g.TickExtraction(DeadZoneGame.HelicopterArrivalSeconds);g.player.transform.position=g.ExtractionLanding;
                Require(g.ExtractionCrew==1&&g.TryBoardHelicopter(g.player)&&g.ExtractionPhase==ZoneExtractionPhase.Departing,"Solo evacuation cannot depart");
                var reconnecting=g.CreatePeer(799,1);reconnecting.enabled=false;reconnecting.Connected=false;peers.Add(reconnecting);
                Require(g.ExtractionCrew==1,"Disconnected teammate blocks departure");
                reconnecting.Connected=true;g.TickExtraction(.01f);
                Require(reconnecting.Boarded&&g.ExtractionSeated==2,"Reconnect during departure left its passenger behind");
                Result="PASS: five real boss deaths, duplicate guard, reachable landing, recovery, airborne/range/floor rejection, pause, four E seats, camera focus/return, snapshot compression, late replica/reset, solo departure, disconnect/reconnect, victory after flight";
                Debug.Log("DZ_EXTRACTION_PASS: "+Result);
            }
            catch(Exception error){Result="FAIL: "+error.Message;throw;}
            finally
            {
                foreach(var peer in peers){g.players.Remove(peer);if(peer!=null)UnityEngine.Object.DestroyImmediate(peer.gameObject);}
                foreach(var enemy in g.enemies.ToArray())if(enemy!=null)UnityEngine.Object.DestroyImmediate(enemy.gameObject);g.enemies.Clear();
                g.ApplyExtraction(new ZoneWorld());g.player.ResetForMatch(g.selected);g.player.enabled=true;g.player.IsLocal=true;
                g.running=false;g.ended=false;g.paused=false;g.enabled=true;Time.timeScale=1;ZoneProfile.SuppressRecording=previousRecording;
            }
        }

        [MenuItem("Dead Zone/Extraction/Preview landed helicopter")]
        public static void Preview()
        {
            var g=DeadZoneGame.Instance;if(!EditorApplication.isPlaying||g==null)throw new Exception("Play Mode required");
            ZoneProfile.SuppressRecording=true;if(!g.running)g.StartGameplay();g.enabled=false;
            foreach(var enemy in g.enemies.ToArray())if(enemy!=null)UnityEngine.Object.Destroy(enemy.gameObject);g.enemies.Clear();
            var arena=UnityEngine.Object.FindAnyObjectByType<CityArena>();Vector2 center=arena.Bounds.center;
            g.ApplyExtraction(new ZoneWorld{bossesDefeated=5,extractionPhase=2,extractionAge=1,extractionLanding=center,extractionEntry=center+Vector2.left*65,extractionExit=center+new Vector2(65,45)});
            g.player.transform.position=center+new Vector2(3,-2);g.player.aimPoint=center;g.player.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;g.player.enabled=false;
            g.gameCamera.transform.position=new Vector3(center.x,center.y,-10);g.gameCamera.orthographicSize=6.8f;
            typeof(DeadZoneGame).GetField("breakUntil",Private).SetValue(g,0f);
            typeof(DeadZoneGame).GetMethod("Update",Private).Invoke(g,null);
            ((GameObject)typeof(DeadZoneGame).GetField("menuRoot",Private).GetValue(g)).SetActive(false);
            ((GameObject)typeof(DeadZoneGame).GetField("hudRoot",Private).GetValue(g)).SetActive(true);
        }
    }
}
