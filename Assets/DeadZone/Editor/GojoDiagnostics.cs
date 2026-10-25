using System;
using UnityEditor;
using UnityEngine;
namespace DeadZone.Editor
{
    public static class GojoDiagnostics
    {
        [MenuItem("Dead Zone/Import Gojo cast art")]
        public static void ImportArt()
        {
            AssetDatabase.Refresh();var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/Resources/GojoCast.png");
            importer.textureType=TextureImporterType.Default;importer.isReadable=true;importer.alphaIsTransparency=true;
            importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;importer.SaveAndReimport();Debug.Log("DZ_GOJO_CAST_ART_READY");
        }
        static ZoneEnemy Target(DeadZoneGame game, Vector2 point, int floor=20)
        {
            var obj=new GameObject("Purple test target");obj.layer=floor;obj.transform.position=point;
            obj.AddComponent<SpriteRenderer>();obj.AddComponent<Rigidbody2D>().gravityScale=0;obj.AddComponent<CircleCollider2D>().radius=.22f;
            var enemy=obj.AddComponent<ZoneEnemy>();enemy.game=game;enemy.health=1000;enemy.enabled=false;game.enemies.Add(enemy);return enemy;
        }
        [MenuItem("Dead Zone/Check Gojo Purple")]
        public static void Check()
        {
            if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");
            var game=DeadZoneGame.Instance;if(game==null||game.player==null)throw new InvalidOperationException("Wait for scene initialization");if(!game.running)game.Begin();
            bool previous=game.enabled;game.enabled=false;
            var targets=new System.Collections.Generic.List<ZoneEnemy>();
            var spells=new System.Collections.Generic.List<GameObject>();GameObject wall=null;Vector3 savedPosition=game.player.transform.position;
            try
            {
                Vector2 origin=new Vector2(400,400);game.player.charge=0;
                foreach(float x in new[]{4f,14f,21f})targets.Add(Target(game,origin+Vector2.right*x));
                targets.Add(Target(game,origin+new Vector2(14,3)));
                targets.Add(Target(game,origin+new Vector2(14,0),21));
                var spell=ZonePurple.Spawn(game.player,Vector2.right);spells.Add(spell.gameObject);spell.enabled=false;spell.Origin=origin;spell.transform.position=origin;
                Physics2D.SyncTransforms();spell.Tick(ZonePurple.ChargeTime-.01f);
                if(targets[0].health!=1000)throw new Exception("Damage during charge");
                spell.Tick(.02f+4f/ZonePurple.Speed);
                if(Mathf.Abs(targets[0].health-580)>.01f||targets[1].health!=1000)throw new Exception("First swept hit");
                spell.Tick(.8f);spell.Tick(.02f);
                if(Mathf.Abs(targets[0].health-580)>.01f||Mathf.Abs(targets[1].health-580)>.01f)throw new Exception("Duplicate damage or missing pierced hit");
                spell.Tick(5);
                if(Mathf.Abs(spell.Travelled-ZonePurple.Range)>.001f||targets[2].health!=580||targets[3].health!=1000||targets[4].health!=1000)throw new Exception("Range, lateral or floor isolation");
                if(game.player.charge!=0)throw new Exception("Ability refilled itself");
                origin+=Vector2.up*10;var hidden=Target(game,origin+Vector2.right*7);targets.Add(hidden);
                wall=new GameObject("Purple blocking wall");wall.layer=20;wall.transform.position=origin+Vector2.right*6;wall.AddComponent<BoxCollider2D>().size=new Vector2(.1f,4);Physics2D.SyncTransforms();
                var blocked=ZonePurple.Spawn(game.player,Vector2.right);spells.Add(blocked.gameObject);blocked.enabled=false;blocked.Origin=origin;blocked.transform.position=origin;blocked.Tick(5);
                if(blocked.Travelled>6||blocked.ImpactAge<0||hidden.health!=1000)throw new Exception("Wall or burst occlusion failed");
                UnityEngine.Object.DestroyImmediate(wall);wall=null;
                game.player.transform.position=origin;Physics2D.SyncTransforms();
                wall=new GameObject("Purple close wall");wall.layer=20;wall.transform.position=origin+new Vector2(.35f,.4f);wall.AddComponent<BoxCollider2D>().size=new Vector2(.1f,2);Physics2D.SyncTransforms();
                var close=ZonePurple.Spawn(game.player,Vector2.right);spells.Add(close.gameObject);close.enabled=false;
                if(close.Origin.x>origin.x+.3f)throw new Exception("Charge appeared behind adjacent wall");
                close.Tick(5);if(close.Travelled>.4f)throw new Exception("Close wall bypassed");
                UnityEngine.Object.DestroyImmediate(wall);wall=null;
                var replica=ZonePurple.Spawn(game.player,Vector2.right);spells.Add(replica.gameObject);replica.enabled=false;
                replica.Apply(new ZoneObjectState { floor=20,origin=origin,position=origin,velocity=Vector2.right,age=ZonePurple.ChargeTime+.5f,endAge=-1 });replica.Tick(.1f);
                if(Vector2.Distance(replica.transform.position,origin+Vector2.right*8.4f)>.01f)throw new Exception("Replica cast phase");
                if(ZoneActorArt.Flip(2,Vector2.right)||ZoneActorArt.Flip(2,Vector2.left))throw new Exception("Gojo unexpectedly mirrored");
                var poses=new System.Collections.Generic.HashSet<Sprite>();
                foreach(var direction in new[]{Vector2.down,Vector2.right,Vector2.up,Vector2.left})
                    foreach(float remaining in new[]{.9f,.55f,.2f,.02f})
                    {
                        var pose=ZoneActorArt.Cast(game,direction,remaining*ZonePurple.ChargeTime);
                        if(pose==null||pose.texture.name!="GojoCast")throw new Exception("Casting pose missing");poses.Add(pose);
                    }
                if(poses.Count!=16)throw new Exception("Casting poses duplicated");
                Debug.Log("DZ_GOJO_CAST_POSES_PASS: 16 poses across four directions");
                Debug.Log("DZ_GOJO_PASS: charge, 420 damage, three pierced targets, 22 range, no repeated damage, no self recharge, wall and floor isolation, replica phase, unmirrored profiles");
            }
            finally
            {
                foreach(var enemy in targets){game.enemies.Remove(enemy);if(enemy!=null)UnityEngine.Object.DestroyImmediate(enemy.gameObject);}
                foreach(var obj in spells)if(obj!=null)UnityEngine.Object.DestroyImmediate(obj);
                if(wall!=null)UnityEngine.Object.DestroyImmediate(wall);game.player.transform.position=savedPosition;Physics2D.SyncTransforms();game.enabled=previous;
            }
        }
        [MenuItem("Dead Zone/Preview Gojo Purple")]
        public static void Preview()
        {
            var game=DeadZoneGame.Instance;if(game==null||game.player==null)throw new InvalidOperationException("Wait for scene initialization");if(!game.running)game.Begin();game.enabled=false;
            foreach(var enemy in game.enemies.ToArray())UnityEngine.Object.Destroy(enemy.gameObject);game.enemies.Clear();
            game.player.ResetHero(2);game.player.enabled=false;game.player.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;
            game.player.transform.position=new Vector3(-4,-5);game.player.aimPoint=new Vector2(20,-5);
            game.player.GetComponent<SpriteRenderer>().sprite=ZoneActorArt.Cast(game,Vector2.right,.2f);game.player.GetComponent<SpriteRenderer>().flipX=ZoneActorArt.Flip(2,Vector2.right);
            game.player.AnimateWeapon();
            foreach(var child in game.player.GetComponentsInChildren<SpriteRenderer>())if(child.gameObject!=game.player.gameObject)child.enabled=false;
            foreach(var loot in UnityEngine.Object.FindObjectsByType<ZonePickup>())UnityEngine.Object.Destroy(loot.gameObject);
            game.gameCamera.transform.position=new Vector3(0,-5,-10);game.gameCamera.orthographicSize=5;
            // Separate snapshots of the charge, merge and flight for a reviewable camera capture.
            float[] ages={.3f,.85f,1.4f};
            for(int i=0;i<3;i++)
            {
                var spell=ZonePurple.Spawn(game.player,Vector2.right);spell.enabled=false;spell.Age=ages[i];spell.Origin=new Vector2(-3+i*4,-4);spell.transform.position=spell.Origin;spell.gameObject.AddComponent<ZonePurpleView>().Spell=spell;
            }
        }
    }
}
