using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace DeadZone.Editor
{
    public static class SukunaDiagnostics
    {
        [MenuItem("Dead Zone/Check Sukuna Shrine")]
        public static void Run()
        {
            var g=DeadZoneGame.Instance;if(!EditorApplication.isPlaying||g==null)throw new Exception("Enter Play Mode");
            if(g.HeroCount!=4)throw new Exception("Fourth hero missing");
            foreach(var direction in new[]{Vector2.down,Vector2.right,Vector2.up,Vector2.left})for(int frame=0;frame<3;frame++){var sprite=ZoneActorArt.Sukuna(direction,frame);if(sprite==null||sprite.rect.width<10||sprite.rect.height<10)throw new Exception("Sukuna pose missing");}
            if(!g.running)g.Begin();g.enabled=false;var actor=g.player;actor.enabled=false;actor.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;actor.ResetHero(3);actor.transform.position=new Vector2(400,400);actor.charge=0;
            actor.health=40;actor.OnEnemyKilled();if(actor.health!=44)throw new Exception("Curse King heal failed");actor.health=actor.maxHealth-1;actor.OnEnemyKilled();if(actor.health!=actor.maxHealth)throw new Exception("Heal exceeded maximum");
            var targets=new List<ZoneEnemy>();Vector2 origin=actor.transform.position;
            ZoneEnemy Target(Vector2 point,int floor=20)
            {
                var obj=new GameObject("Shrine test target");obj.layer=floor;obj.transform.position=point;obj.AddComponent<SpriteRenderer>();obj.AddComponent<Rigidbody2D>().gravityScale=0;obj.AddComponent<CircleCollider2D>().radius=.2f;
                var enemy=obj.AddComponent<ZoneEnemy>();enemy.game=g;enemy.health=1000;enemy.enabled=false;g.enemies.Add(enemy);targets.Add(enemy);return enemy;
            }
            Target(origin+Vector2.right*2);Target(origin+new Vector2(2,1));Target(origin+Vector2.up*2);Target(origin+Vector2.right*6.5f);Target(origin+Vector2.right*2,21);
            var wall=new GameObject("Shrine wall");wall.layer=20;wall.transform.position=origin+Vector2.up;wall.AddComponent<BoxCollider2D>().size=new Vector2(1,.12f);Physics2D.SyncTransforms();
            var shrine=ZoneShrine.Spawn(actor);float after=Time.time+.35f;int phase=0;
            void Clean(){EditorApplication.update-=Poll;foreach(var target in targets){g.enemies.Remove(target);if(target!=null)UnityEngine.Object.Destroy(target.gameObject);}UnityEngine.Object.Destroy(wall);if(shrine!=null)UnityEngine.Object.Destroy(shrine.gameObject);actor.Downed=false;actor.transform.position=new Vector2(-3,2);actor.enabled=true;g.enabled=true;}
            void Poll()
            {
                if(!EditorApplication.isPlaying||actor==null){EditorApplication.update-=Poll;return;}if(Time.time<after)return;
                try
                {
                    if(phase==0){if(targets[0].health!=1000)throw new Exception("Damage before cast finished");after=Time.time+.6f;phase=1;return;}
                    if(phase==1)
                    {
                        if(targets[0].health!=960||targets[1].health!=960||targets[2].health!=1000||targets[3].health!=1000||targets[4].health!=1000)throw new Exception("Shrine damage, radius, wall or floor isolation failed");
                        if(actor.charge!=0)throw new Exception("Shrine charged itself");after=Time.time+.7f;phase=2;return;
                    }
                    if(phase==2){if(targets[0].health!=920)throw new Exception("Shrine tick interval failed");actor.Downed=true;after=Time.time+.2f;phase=3;return;}
                    if(shrine!=null)throw new Exception("Shrine did not cancel when downed");
                    Clean();Debug.Log("DZ_SUKUNA_PASS: four heroes, 12 poses, heal/cap, cast delay, 40 damage per .65s, area/multiple targets, walls/floors/range, no self charge, downed cancel");
                }
                catch(Exception error){Clean();Debug.LogException(error);}
            }
            EditorApplication.update+=Poll;
        }
        [MenuItem("Dead Zone/Preview Sukuna Shrine")]
        public static void Preview()
        {
            var g=DeadZoneGame.Instance;if(!g.running)g.Begin();g.enabled=false;g.selected=3;g.player.ResetHero(3);g.player.IsLocal=false;g.player.transform.position=new Vector2(-3,-5);g.player.SetCommand(new ZoneCommand{ability=true,aim=new Vector2(2,-5)});g.player.charge=100;
            foreach(var enemy in g.enemies.ToArray())UnityEngine.Object.Destroy(enemy.gameObject);g.enemies.Clear();g.gameCamera.transform.position=new Vector3(-3,-5,-10);g.gameCamera.orthographicSize=7;
        }
    }
}
