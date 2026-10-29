using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace DeadZone.Editor
{
    public static class RemDiagnostics
    {
        static ZoneEnemy Target(DeadZoneGame g,Vector2 point,int floor=20)
        {
            var obj=new GameObject("Rem test target");obj.layer=floor;obj.transform.position=point;obj.AddComponent<SpriteRenderer>();obj.AddComponent<Rigidbody2D>().gravityScale=0;obj.AddComponent<CircleCollider2D>().radius=.22f;
            var enemy=obj.AddComponent<ZoneEnemy>();enemy.game=g;enemy.health=1000;enemy.enabled=false;g.enemies.Add(enemy);return enemy;
        }
        [MenuItem("Dead Zone/Check Rem Oni")]
        public static void Check()
        {
            var g=DeadZoneGame.Instance;if(!EditorApplication.isPlaying||g==null||g.player==null)throw new Exception("Wait for Play Mode initialization");
            if(!g.running)g.Begin();g.enabled=false;var actor=g.player;actor.enabled=false;actor.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;actor.ResetHero(1);actor.transform.position=new Vector2(400,400);
            int ammo=actor.ammo;actor.charge=0;var form=actor.Oni;
            if(!form.Activate()||form.Remaining<39.9f||form.Remaining>40||form.Activate())throw new Exception("40-second activation or extension guard failed");
            var targets=new List<ZoneEnemy>();Vector2 origin=actor.transform.position;
            targets.Add(Target(g,origin+new Vector2(2,0)));targets.Add(Target(g,origin+new Vector2(2,1)));
            targets.Add(Target(g,origin+new Vector2(-2,0)));targets.Add(Target(g,origin+new Vector2(4,0)));targets.Add(Target(g,origin+new Vector2(2,0),21));
            targets.Add(Target(g,origin+new Vector2(0,2)));
            var wall=new GameObject("Rem test wall");wall.layer=20;wall.transform.position=origin+Vector2.up;wall.AddComponent<BoxCollider2D>().size=new Vector2(1,.12f);Physics2D.SyncTransforms();
            if(!form.TrySwing(origin+Vector2.right*10)||form.TrySwing(origin+Vector2.right*10))throw new Exception("Swing rate limit failed");
            float after=Time.time+.38f;int stage=0;
            void Poll()
            {
                if(!EditorApplication.isPlaying||actor==null){EditorApplication.update-=Poll;return;}
                if(Time.time<after)return;
                try
                {
                    if(stage==0)
                    {
                        if(targets[0].health!=890||targets[1].health!=890||targets[2].health!=1000||targets[3].health!=1000||targets[4].health!=1000)throw new Exception($"Cone/range/floor/damage: {targets[0].health}, {targets[1].health}, {targets[2].health}, {targets[3].health}, {targets[4].health}");
                        after=Time.time+.38f;stage=1;return;
                    }
                    if(stage==1)
                    {
                        if(targets[0].health!=890||actor.ammo!=ammo||actor.charge!=0)throw new Exception("Repeated damage, ammunition or self charge failed");
                        if(!form.TrySwing(origin+Vector2.up*10))throw new Exception("Next swing blocked");
                        after=Time.time+.38f;stage=2;return;
                    }
                    if(targets[5].health!=1000)throw new Exception("Mace passed through wall");
                    // A short remote duration exercises the same expiry clock without a forty-second wait.
                    form.Apply(new ZonePlayerState{oniRemaining=.08f,maceDirection=Vector2.right});
                    after=Time.time+.2f;stage=3;
                    EditorApplication.update-=Poll;EditorApplication.update+=Expire;
                }
                catch(Exception e){Debug.LogError("DZ_REM_FAIL: "+e.Message);Cleanup();}
            }
            void Expire()
            {
                if(!EditorApplication.isPlaying||actor==null){EditorApplication.update-=Expire;return;}
                if(Time.time<after)return;
                if(form.Active||form.Remaining>0)Debug.LogError("DZ_REM_FAIL: expiry failed");
                else Debug.Log("DZ_REM_PASS: 40 seconds, no extension, cone/multiple targets, 110 damage once, rate limit, no ammo use/self charge, wall/floor isolation, expiry");
                EditorApplication.update-=Expire;Cleanup();
            }
            void Cleanup()
            {
                EditorApplication.update-=Poll;form.Cancel();foreach(var enemy in targets){g.enemies.Remove(enemy);if(enemy!=null)UnityEngine.Object.Destroy(enemy.gameObject);}if(wall!=null)UnityEngine.Object.Destroy(wall);actor.transform.position=new Vector2(-3,2);actor.enabled=true;g.enabled=true;
            }
            EditorApplication.update+=Poll;
        }
        [MenuItem("Dead Zone/Preview Rem Oni")]
        public static void Preview()
        {
            var g=DeadZoneGame.Instance;if(g==null||g.player==null)throw new Exception("Wait for scene");if(!g.running)g.Begin();g.enabled=false;
            foreach(var enemy in g.enemies.ToArray())UnityEngine.Object.Destroy(enemy.gameObject);g.enemies.Clear();foreach(var loot in UnityEngine.Object.FindObjectsByType<ZonePickup>())UnityEngine.Object.Destroy(loot.gameObject);
            var actor=g.player;actor.enabled=false;actor.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;actor.ResetHero(1);actor.transform.position=new Vector2(-3,-5);actor.aimPoint=new Vector2(5,-5);
            actor.GetComponent<SpriteRenderer>().sprite=ZoneActorArt.Frame(g,1,Vector2.right,false);actor.GetComponent<SpriteRenderer>().flipX=false;actor.Oni.Activate();actor.SendMessage("LateUpdate");actor.AnimateWeapon();
            g.gameCamera.transform.position=new Vector3(-2,-5,-10);g.gameCamera.orthographicSize=3;
        }
    }
}
