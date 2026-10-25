using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
namespace DeadZone.Editor
{
    public static class OutcomeDiagnostics
    {
        [MenuItem("Dead Zone/Rework/Preview defeat report")]
        public static void PreviewDefeat()
        {
            var g=DeadZoneGame.Instance;if(!EditorApplication.isPlaying||g==null)throw new Exception("Fresh Play Mode");ZoneProfile.SuppressRecording=true;g.selected=3;if(!g.running)g.Begin();
            for(int hero=0;hero<4;hero++)if(!g.players.Exists(p=>p.CharacterIndex==hero))g.CreatePeer((ulong)(810+hero),hero);
            typeof(DeadZoneGame).GetField("nextSpawn",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(g,float.MaxValue);
            g.wave=4;typeof(DeadZoneGame).GetField("startedAt",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(g,Time.time-1428);
            for(int i=0;i<g.players.Count;i++){var actor=g.players[i];actor.IsLocal=false;actor.PersonalKills=23+i*11;actor.PersonalDamage=4180+i*630;actor.Revives=i%3;}
            g.Finish(false);Debug.Log("DZ_OUTCOME_PREVIEW_READY");
        }
        [MenuItem("Dead Zone/Rework/Check rematch reset")]
        public static void Rematch()
        {
            var g=DeadZoneGame.Instance;if(!EditorApplication.isPlaying||g==null||!g.ended)throw new Exception("Preview defeat report first");g.RestartRun();float after=Time.time+.4f;double deadline=EditorApplication.timeSinceStartup+20;
            void Poll()
            {
                if(!EditorApplication.isPlaying||g==null){EditorApplication.update-=Poll;return;}
                try
                {
                    if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Rematch timed out");if(Time.time<after)return;
                    if(!g.running||g.ended||g.Restarting||g.wave!=1||g.kills!=0||g.damageDealt!=0||g.Elapsed>1)throw new Exception("Match state did not reset");
                    foreach(var actor in g.players)if(actor.health!=actor.maxHealth||actor.Dead||actor.Downed||actor.charge!=0||actor.weapon!=0||actor.WeaponInSlot(1)!=-1||actor.ammo!=12||actor.PersonalKills!=0||actor.PersonalDamage!=0||actor.Revives!=0)throw new Exception("Actor inventory, health or statistics did not reset");
                    if(ZoneSupplyCache.Active.Count!=16||ZonePickup.Active.Count!=0||ZoneCorpse.Active.Count!=0||UnityEngine.Object.FindObjectsByType<ZoneShrine>().Length!=0)throw new Exception("Runtime objects not cleared/repopulated");
                    EditorApplication.update-=Poll;Debug.Log("DZ_REMATCH_PASS: four actors, hero choices, full HP, pistol-only inventory, charges/stats/timers, 16 fresh caches, wave1");
                }
                catch(Exception error){EditorApplication.update-=Poll;Debug.LogException(error);}
            }
            EditorApplication.update+=Poll;
        }
        [MenuItem("Dead Zone/Rework/Check spectator camera")]
        public static void Spectator()
        {
            var g=DeadZoneGame.Instance;if(!EditorApplication.isPlaying||g==null)throw new Exception("Play Mode");if(!g.running)g.Begin();while(g.players.Count<4)g.CreatePeer((ulong)(820+g.players.Count),g.players.Count);g.player.Dead=true;g.player.health=0;g.player.GetComponent<Collider2D>().enabled=false;
            var first=g.CameraActor;if(first==g.player||first.Dead)throw new Exception("Spectator did not choose living ally");g.CycleSpectator(1);if(g.CameraActor==first||g.CameraActor.Dead)throw new Exception("Spectator did not switch ally");g.player.Revive(true);if(g.CameraActor!=g.player)throw new Exception("Revived player did not regain camera");Debug.Log("DZ_SPECTATOR_PASS: living ally, cycling, own camera after revival");
        }
    }
}
