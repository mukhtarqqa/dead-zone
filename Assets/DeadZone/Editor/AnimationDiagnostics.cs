using System;
using UnityEditor;
using UnityEngine;
namespace DeadZone.Editor
{
    public static class AnimationDiagnostics
    {
        [MenuItem("Dead Zone/Configure animation art")]
        public static void Configure()
        {
            AssetDatabase.Refresh();
            var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/Resources/DeadZoneDirections.png");
            importer.textureType=TextureImporterType.Default; importer.isReadable=true; importer.alphaIsTransparency=true;
            importer.filterMode=FilterMode.Point; importer.mipmapEnabled=false; importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.npotScale=TextureImporterNPOTScale.None; importer.maxTextureSize=2048; importer.SaveAndReimport();
            Debug.Log("DZ_ANIMATION_ART_READY");
        }
        [MenuItem("Dead Zone/Run animation check")]
        public static void Check()
        {
            if(!EditorApplication.isPlaying) throw new InvalidOperationException("Play Mode required");
            var g=DeadZoneGame.Instance; if(!g.running)g.Begin();
            g.enabled=false; var actor=g.player; actor.IsLocal=false; actor.enabled=false;
            var saved=actor.transform.position; int gun=actor.weapon; Vector2 aim=actor.aimPoint;
            actor.transform.position=new Vector3(-3,2); Physics2D.SyncTransforms();
            int checks=0;
            for(int kind=0;kind<4;kind++) for(int d=0;d<16;d++)
            {
                float a=d*Mathf.PI/8; var dir=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                actor.weapon=kind; actor.aimPoint=(Vector2)actor.transform.position+Vector2.up*.55f+dir*8; actor.AnimateWeapon();
                var gunView=actor.transform.Find("WeaponAim/Gun"); var renderer=gunView.GetComponent<SpriteRenderer>();
                var tip=ZoneArsenal.Muzzle(kind); if(renderer.flipY)tip.y=-tip.y;
                if(Vector2.Distance(actor.MuzzlePosition,gunView.TransformPoint(tip))>.001f)throw new Exception("Muzzle mismatch");
                if(Mathf.Abs(Vector2.Dot(actor.MuzzlePosition-((Vector2)actor.transform.position+Vector2.up*.55f),new Vector2(-dir.y,dir.x)))>.025f)throw new Exception("Barrel off aim axis");
                if(Vector2.Distance(actor.SafeMuzzlePosition(),actor.MuzzlePosition)>.01f)throw new Exception("Clear muzzle clamped");
                checks++;
            }
            // Real wall between actor and barrel must prevent spawning a projectile through it.
            actor.weapon=1; actor.aimPoint=(Vector2)actor.transform.position+Vector2.right*8; actor.AnimateWeapon();
            var wall=new GameObject("Animation test wall");wall.layer=actor.gameObject.layer;
            wall.transform.position=Vector2.Lerp(actor.transform.position,actor.MuzzlePosition,.55f);
            wall.AddComponent<BoxCollider2D>().size=new Vector2(.12f,2);Physics2D.SyncTransforms();
            if(Vector2.Distance(actor.SafeMuzzlePosition(),actor.MuzzlePosition)<.1f)throw new Exception("Muzzle bypassed wall");
            UnityEngine.Object.DestroyImmediate(wall);
            for(int h=0;h<3;h++)for(int d=0;d<4;d++)
            {
                var direction=d==0?Vector2.down:d==1?Vector2.right:d==2?Vector2.up:Vector2.left;
                var frame=ZoneActorArt.Frame(g,h,direction,false);
                if(frame==null || frame.texture.name!="DeadZoneDirections")throw new Exception("Directional atlas missing");
                checks++;
            }
            actor.weapon=gun;actor.aimPoint=aim;actor.transform.position=saved;actor.enabled=true;actor.IsLocal=true;g.enabled=true;
            var ghostObject=new GameObject("Pulse anchor check");ghostObject.AddComponent<SpriteRenderer>();
            var ghost=ghostObject.AddComponent<ZoneGhost>(); Vector2 fixedPoint=new Vector2(-4,2);
            ghost.Apply(g,new ZoneObjectState { type=6,kind=4,floor=20,position=fixedPoint,scale=1,health=2,remaining=2 });
            double deadline=EditorApplication.timeSinceStartup+.35;
            void VerifyPulse()
            {
                if(!EditorApplication.isPlaying || ghostObject==null){EditorApplication.update-=VerifyPulse;return;}
                if(EditorApplication.timeSinceStartup<deadline)return;
                EditorApplication.update-=VerifyPulse;
                bool stable=Vector2.Distance(ghostObject.transform.position,fixedPoint)<.001f;UnityEngine.Object.Destroy(ghostObject);
                if(stable)Debug.Log("DZ_ANIMATION_PULSE_ANCHOR_PASS");else Debug.LogError("DZ_ANIMATION_PULSE_ANCHOR_FAIL");
            }
            EditorApplication.update+=VerifyPulse;
            Debug.Log("DZ_ANIMATION_PASS: "+checks+" gun/direction checks; obstruction blocked; 12 directional sets loaded");
        }
        [MenuItem("Dead Zone/Preview animation lineup")]
        public static void Preview()
        {
            var g=DeadZoneGame.Instance; if(!g.running)g.Begin(); g.enabled=false;
            foreach(var enemy in g.enemies.ToArray()) UnityEngine.Object.Destroy(enemy.gameObject);g.enemies.Clear();
            foreach(var loot in UnityEngine.Object.FindObjectsByType<ZonePickup>()) UnityEngine.Object.Destroy(loot.gameObject);
            g.player.enabled=false;g.player.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;
            Vector2 origin=new Vector2(-3,2);g.player.transform.position=origin;
            for(int h=0;h<3;h++)for(int d=0;d<4;d++)
            {
                var obj=new GameObject("Animation preview");obj.transform.position=origin+new Vector2((d-1.5f)*2.5f,(1-h)*2.5f);
                var sr=obj.AddComponent<SpriteRenderer>();sr.sortingLayerID=ZoneMap.Sorting(20);sr.sortingOrder=30;
                sr.flipX=ZoneActorArt.Flip(h,d==0?Vector2.down:d==1?Vector2.right:d==2?Vector2.up:Vector2.left);
                sr.sprite=ZoneActorArt.Frame(g,h,d==0?Vector2.down:d==1?Vector2.right:d==2?Vector2.up:Vector2.left,true);
            }
            g.gameCamera.transform.position=new Vector3(-3,2,-10);g.gameCamera.orthographicSize=5;
            ZonePulse.Spawn(origin+new Vector2(-3,2.5f),20,.8f,3,12);
            ZonePulse.Spawn(origin,20,1.1f,4,12);
            ZonePulse.Spawn(origin+new Vector2(3,-2.5f),20,1,5,12);
        }
    }
}
