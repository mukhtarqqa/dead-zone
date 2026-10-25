using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace DeadZone.Editor
{
    public static class QualityDiagnostics
    {
        [MenuItem("Dead Zone/Quality regression")]
        public static void Run()
        {
            var game=DeadZoneGame.Instance;var map=ZoneMap.Current;
            if(!EditorApplication.isPlaying||game==null||game.player==null||map==null)throw new InvalidOperationException("Wait for scene initialization in Play Mode");
            foreach(string path in AssetDatabase.GetDependencies(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,true))if(path.StartsWith("Assets/LoBitCity")||path.StartsWith("Assets/Cainos"))throw new Exception("Restricted asset dependency: "+path);
            int total=0,reachable=0;
            foreach(var node in map.WalkableNodes){total++;if(map.Reachable(map.WorldPoint(node),node.z,game.player))reachable++;}
            if(total<5000||reachable!=total)throw new Exception($"Map disconnected {reachable}/{total}");
            if(game.Balance.BossHealth(1,1)<=ZonePurple.Damage)throw new Exception("Boss dies to one ability");
            if(!game.running)game.Begin();int planned=game.PendingEnemies;
            if(planned<10||game.enemies.Count>2)throw new Exception("Whole wave spawned immediately");
            var options=game.GetComponent<ZoneOptions>();options.Show();if(!game.InputBlocked||Time.timeScale!=0)throw new Exception("Pause does not block input");options.Close();if(game.InputBlocked||Time.timeScale!=1)throw new Exception("Pause does not restore play");
            game.player.IsLocal=false;game.player.SetCommand(new ZoneCommand{aim=Vector2.right*8});
            var targets=new List<ZoneEnemy>();Vector2 origin=new Vector2(400,400);
            ZoneEnemy Target(Vector2 point)
            {
                var obj=new GameObject("Blast regression target");obj.layer=20;obj.transform.position=point;obj.AddComponent<SpriteRenderer>();obj.AddComponent<Rigidbody2D>().gravityScale=0;obj.AddComponent<CircleCollider2D>().radius=.2f;
                var enemy=obj.AddComponent<ZoneEnemy>();enemy.game=game;enemy.health=500;enemy.enabled=false;game.enemies.Add(enemy);targets.Add(enemy);return enemy;
            }
            var open=Target(origin+Vector2.up*2);var hidden=Target(origin+Vector2.right*2);
            var wall=new GameObject("Blast regression wall");wall.layer=20;wall.transform.position=origin+Vector2.right;wall.AddComponent<BoxCollider2D>().size=new Vector2(.1f,2);Physics2D.SyncTransforms();game.Blast(origin,3,50,game.player,20);
            bool clear=open.health==450&&hidden.health==500;foreach(var e in targets){game.enemies.Remove(e);UnityEngine.Object.Destroy(e.gameObject);}UnityEngine.Object.Destroy(wall);
            if(!clear)throw new Exception("Explosion bypassed wall");
            var dummy=game.CreatePeer(777,0);dummy.Hit(10000);
            if(dummy.health!=0||!dummy.Downed)throw new Exception("Player health did not clamp at zero");game.players.Remove(dummy);UnityEngine.Object.Destroy(dummy.gameObject);
            float after=Time.time+1.5f;int frames=Time.frameCount;
            void Poll()
            {
                if(!EditorApplication.isPlaying||game==null){EditorApplication.update-=Poll;return;}
                if(Time.time<after)return;EditorApplication.update-=Poll;
                bool pass=Time.frameCount>frames&&game.wave==1&&!game.BossPhase&&game.PendingEnemies<planned&&game.PendingEnemies>0&&game.enemies.Count>0;
                game.player.IsLocal=true;
                if(pass)Debug.Log($"DZ_QUALITY_PASS: {reachable}/{total} connected nodes, owned scene dependencies, staggered waves, boss balance, pause/input, blast wall, frames {frames}->{Time.frameCount}");
                else Debug.LogError("DZ_QUALITY_FAIL: wave spawning or frame advance");
            }
            EditorApplication.update+=Poll;
        }
        [MenuItem("Dead Zone/Configure source project")]
        public static void ConfigureSource()
        {
            var assets=AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            var data=new SerializedObject(assets[0]);var property=data.FindProperty("activeInputHandler")??data.FindProperty("m_ActiveInputHandler");
            if(property!=null){property.intValue=2;data.ApplyModifiedPropertiesWithoutUndo();}
            EditorSettings.serializationMode=SerializationMode.ForceText;AssetDatabase.SaveAssets();Debug.Log("DZ_SOURCE_SETTINGS_READY");
        }
    }
}
