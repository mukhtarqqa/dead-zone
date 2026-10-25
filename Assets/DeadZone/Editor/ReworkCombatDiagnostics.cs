using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
namespace DeadZone.Editor
{
    public static class ReworkCombatDiagnostics
    {
        [MenuItem("Dead Zone/Rework/Check animation sheets and compressed worlds")]
        public static void AssetsAndCodec()
        {
            int count=0;for(int role=0;role<4;role++)
            {
                int expected=role==1?20:24;if(ZoneEnemyArt.PoseCount(role)!=expected)throw new Exception("Wrong pose count for role "+role);count+=expected;
                for(int facing=0;facing<4;facing++)for(int pose=0;pose<6;pose++){var sprite=ZoneEnemyArt.Frame(role,facing,pose);if(sprite==null||sprite.bounds.size.x<.5f||sprite.bounds.size.y<.2f||Mathf.Abs(sprite.bounds.min.y)>.01f)throw new Exception("Clipped/missing pose "+role+":"+facing+":"+pose);}
            }
            var objects=new ZoneObjectState[240];for(int i=0;i<objects.Length;i++)objects[i]=new ZoneObjectState{id=i,type=i%8+1,kind=i%4,facing=i%4,animation=i%4,position=new Vector2(i*.371f-40,i*.239f-30),velocity=new Vector2(i%7*.33f,-i%11*.17f),health=77+i,maxHealth=140,scale=1,remaining=.43f};
            string json=JsonUtility.ToJson(new ZoneWorld{sequence=123,notice="Кооператив · Қала",players=new ZonePlayerState[4],objects=objects});if(json.Length<=30000)throw new Exception("Snapshot test does not cross the old silent-drop limit");
            var wire=ZoneWorldCodec.Encode(json);if(ZoneWorldCodec.Decode(wire)!=json)throw new Exception("Compressed world roundtrip changed data");
            if(wire.Length>=json.Length*.65f)throw new Exception("World compression ineffective");
            bool oversizedRejected=false;try{ZoneWorldCodec.Encode(new string('x',ZoneWorldCodec.MaxDecodedBytes+1));}catch(System.IO.InvalidDataException){oversizedRejected=true;}if(!oversizedRejected)throw new Exception("World codec byte budget not enforced");
            Debug.Log("DZ_REWORK_ART_CODEC_PASS: "+count+" complete directional poses, feet pivots, world "+json.Length+" chars -> "+wire.Length+" bytes, bounded lossless UTF-8");
        }
        [MenuItem("Dead Zone/Rework/Check enemy tactics and personal caches")]
        public static void Combat()
        {
            var g=DeadZoneGame.Instance;if(!EditorApplication.isPlaying||g==null)throw new Exception("Enter Play Mode");g.selected=0;if(!g.running)g.Begin();
            typeof(DeadZoneGame).GetField("nextSpawn",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(g,float.MaxValue);
            foreach(var e in g.enemies.ToArray())UnityEngine.Object.Destroy(e.gameObject);g.enemies.Clear();g.player.IsLocal=false;g.player.maxHealth=g.player.health=5000;
            Vector2 origin=new(-36,-38);g.player.transform.position=origin;g.player.GetComponent<Rigidbody2D>().position=origin;
            ZoneEnemy Create(int role,Vector2 point)
            {
                g.SpawnEnemy(role,false);var e=g.enemies[g.enemies.Count-1];e.transform.position=point;e.GetComponent<Rigidbody2D>().position=point;e.ReadyAt=Time.time+.15f;e.health=10000;return e;
            }
            var walker=Create(0,origin+Vector2.left*6);var ranger=Create(1,origin+Vector2.right*8);var runner=Create(2,origin+new Vector2(-9,1));var brute=Create(3,origin+new Vector2(4,3));
            var protectedRanger=Create(1,origin+Vector2.up*7);protectedRanger.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;
            var wall=new GameObject("QA line of sight barrier");wall.layer=20;wall.transform.position=origin+Vector2.up*3.5f;wall.AddComponent<BoxCollider2D>().size=new Vector2(12,.5f);Physics2D.SyncTransforms();
            float started=Time.time,shotAt=Time.time+.2f,health=g.player.health;Vector2 walkerStart=walker.transform.position,runnerStart=runner.transform.position;int frames=0,staggered=0,hits=0,phase=0;bool attackPose=false;
            double deadline=EditorApplication.timeSinceStartup+40;
            void Poll()
            {
                if(!EditorApplication.isPlaying||g==null){EditorApplication.update-=Poll;return;}
                try
                {
                    if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Enemy verification timed out");
                    g.player.SetCommand(new ZoneCommand{aim=origin+Vector2.right*10});
                    if(phase==0)
                    {
                        frames++;if(walker.StaggerRemaining>0)staggered++;attackPose|=ranger.AnimationPose==3||brute.AnimationPose==3||walker.AnimationPose==3;
                        if(Time.time>=shotAt&&Time.time>=walker.ReadyAt){walker.Hit(13,g.player);hits++;shotAt=Time.time+.075f;}
                        if(Time.time-started<4)return;
                        if(hits<25||Vector2.Distance(walkerStart,walker.transform.position)<2||staggered>frames*.5f)throw new Exception("Automatic fire still permanently stuns movement: "+staggered+"/"+frames);
                        if(Vector2.Distance(runnerStart,runner.transform.position)<3)throw new Exception("Runner did not move/flank");
                        if(ranger.ShotsFired<3||protectedRanger.ShotsFired!=0)throw new Exception("Ranger burst/line of sight failed: "+ranger.ShotsFired+"/"+protectedRanger.ShotsFired);
                        if(!attackPose||g.player.health>=health)throw new Exception("Attack poses or enemy damage missing");
                        var cache=ZoneSupplyCache.Active[0];g.player.transform.position=cache.transform.position;g.player.GetComponent<Rigidbody2D>().position=cache.transform.position;cache.Open(g.player);
                        if(!cache.Opened(g.player)||cache.CanOpen(g.player))throw new Exception("Personal cache opens twice");
                        var ally=g.CreatePeer(778,1);ally.transform.position=cache.transform.position;if(!cache.CanOpen(ally))throw new Exception("Opening cache consumed ally loot");g.players.Remove(ally);UnityEngine.Object.Destroy(ally.gameObject);
                        walker.Hit(20000,g.player);if(ZoneCorpse.Active.Count==0)throw new Exception("Death pose missing");
                        started=Time.time;phase=1;return;
                    }
                    if(Time.time-started<.4f)return;
                    var corpse=ZoneCorpse.Active[0];if(corpse.GetComponent<SpriteRenderer>()?.sprite==null)throw new Exception("Corpse animation has no sprite");
                    UnityEngine.Object.Destroy(wall);g.player.IsLocal=true;EditorApplication.update-=Poll;Debug.Log("DZ_REWORK_COMBAT_PASS: "+hits+" machine-gun hits without permanent stun ("+staggered+"/"+frames+" frames), runner movement, warning poses, ranger burst/LOS, damage, personal cache/reconnect identity, corpse");
                }
                catch(Exception error){EditorApplication.update-=Poll;g.player.IsLocal=true;UnityEngine.Object.Destroy(wall);Debug.LogException(error);}
            }
            EditorApplication.update+=Poll;
        }
    }
}
