using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
namespace DeadZone.Editor
{
    public static class HurtboxDiagnostics
    {
        [MenuItem("Dead Zone/Rework/Check visible body hits and wall cover")]
        public static void Run()
        {
            var g=DeadZoneGame.Instance;if(!EditorApplication.isPlaying||g==null)throw new Exception("Enter Play Mode");if(!g.running)g.Begin();typeof(DeadZoneGame).GetField("nextSpawn",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(g,float.MaxValue);
            foreach(var e in g.enemies.ToArray())UnityEngine.Object.Destroy(e.gameObject);g.enemies.Clear();g.player.IsLocal=false;g.SpawnEnemy(0,false);var enemy=g.enemies[g.enemies.Count-1];enemy.transform.position=new Vector2(-39,-38);enemy.GetComponent<Rigidbody2D>().position=enemy.transform.position;enemy.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;enemy.health=500;enemy.ReadyAt=Time.time;
            int phase=0;float after=Time.time+.3f;ZoneHurtbox box=null;GameObject wall=null;ZonePlayer ally=null;double deadline=EditorApplication.timeSinceStartup+25;
            void Fire(){g.Shoot(enemy.DamageCenter+Vector2.left*4,Vector2.right,25,false,20,g.player,10,20);}
            void Poll()
            {
                if(!EditorApplication.isPlaying||g==null){EditorApplication.update-=Poll;return;}
                try
                {
                    if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Body hit verification timed out");if(Time.time<after)return;
                    if(phase==0){box=enemy.GetComponentInChildren<ZoneHurtbox>();if(box==null)throw new Exception("Visible-body hit area absent");box.enabled=false;box.GetComponent<Collider2D>().enabled=false;Physics2D.SyncTransforms();Debug.Log("DZ_BODY_BASELINE: root="+enemy.transform.position+" center="+enemy.DamageCenter+" circle="+enemy.GetComponent<Collider2D>().bounds+" bodyEnabled="+box.GetComponent<Collider2D>().enabled);foreach(var hit in Physics2D.RaycastAll(enemy.DamageCenter+Vector2.left*4,Vector2.right,6))Debug.Log("DZ_BODY_RAY: "+hit.collider.name+" / "+hit.collider.GetType().Name+" @ "+hit.point);Fire();phase=1;after=Time.time+.35f;return;}
                    if(phase==1){if(enemy.health!=500)throw new Exception("Baseline feet collider unexpectedly catches a torso shot: health="+enemy.health+" root="+enemy.transform.position+" center="+enemy.DamageCenter+" body="+box.GetComponent<Collider2D>().enabled);box.enabled=true;box.GetComponent<Collider2D>().enabled=true;Physics2D.SyncTransforms();Fire();phase=2;after=Time.time+.35f;return;}
                    if(phase==2){if(enemy.health!=475)throw new Exception("Torso shot missed visible body: "+enemy.health);wall=new GameObject("Body hit wall");wall.layer=20;wall.transform.position=enemy.DamageCenter+Vector2.left*2;wall.AddComponent<BoxCollider2D>().size=new Vector2(.2f,2);Physics2D.SyncTransforms();Fire();phase=3;after=Time.time+.35f;return;}
                    if(phase==3){if(enemy.health!=475)throw new Exception("Shot bypassed wall");UnityEngine.Object.Destroy(wall);ally=g.CreatePeer(779,1);ally.transform.position=enemy.transform.position+Vector3.left*2;ally.GetComponent<Rigidbody2D>().position=ally.transform.position;phase=4;after=Time.time+.15f;return;}
                    if(phase==4){Physics2D.SyncTransforms();Fire();phase=5;after=Time.time+.35f;return;}
                    if(enemy.health!=450||ally.health!=ally.maxHealth)throw new Exception("Friendly body blocked a shot or took friendly fire");
                    g.players.Remove(ally);UnityEngine.Object.Destroy(ally.gameObject);g.player.IsLocal=true;EditorApplication.update-=Poll;Debug.Log("DZ_HURTBOX_PASS: locomotion-only collider misses torso; visible-body shot deals25; wall blocks; ally takes0 and does not absorb friendly bullets");
                }
                catch(Exception error){EditorApplication.update-=Poll;g.player.IsLocal=true;if(wall!=null)UnityEngine.Object.Destroy(wall);Debug.LogException(error);}
            }
            EditorApplication.update+=Poll;
        }
    }
}
