using System;
using UnityEditor;
using UnityEngine;

namespace DeadZone.Editor
{
    public static class RemOniAnimationDiagnostics
    {
        [MenuItem("Dead Zone/Rem Oni/Check ultimate animation playback")]
        public static void Check()
        {
            if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");
            var game=DeadZoneGame.Instance;if(game==null||game.player==null)throw new Exception("Wait for game initialization");
            if(!game.running)game.Begin();game.enabled=false;
            var actor=game.player;actor.enabled=false;actor.IsLocal=false;actor.ResetForMatch(1);
            actor.transform.position=new Vector2(400,400);actor.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;
            var sr=actor.GetComponent<SpriteRenderer>();
            if(!ZoneRemArt.Available||!actor.Oni.Activate())throw new Exception("Oni art/activation missing");
            int checkedFrames=0;
            Vector2[] directions={Vector2.up,Vector2.down,Vector2.left,Vector2.right};
            for(int s=0;s<4;s++)for(int d=0;d<4;d++)for(int f=0;f<ZoneRemArt.FrameCounts[s];f++)
            {
                var sprite=ZoneRemArt.Frame((RemOniMotion)s,directions[d],f);
                if(sprite==null||!sprite.name.EndsWith($"_{d}_{f:00}"))throw new Exception("Frame order mismatch");
                checkedFrames++;
            }
            actor.aimPoint=(Vector2)actor.transform.position+Vector2.right*10;
            actor.SendMessage("LateUpdate");Expect(sr,"Idle");
            actor.SetCommand(new ZoneCommand{move=Vector2.right,aim=actor.aimPoint});actor.SendMessage("Update");
            actor.SendMessage("LateUpdate");Expect(sr,"Run");
            actor.SetCommand(new ZoneCommand{fire=true,aim=actor.aimPoint});actor.SendMessage("Update");
            actor.SendMessage("LateUpdate");Expect(sr,"Attack");
            actor.SetCommand(new ZoneCommand{move=Vector2.up,dash=true,aim=actor.aimPoint});actor.SendMessage("Update");
            actor.SendMessage("LateUpdate");Expect(sr,"Dash");
            if(!sr.sprite.name.Contains("_0_"))throw new Exception("Dash followed aim instead of movement");
            var wire=JsonUtility.FromJson<ZonePlayerState>(JsonUtility.ToJson(new ZonePlayerState{dashRemaining=.12f,dashDirection=Vector2.left}));
            if(wire.dashRemaining!=.12f||wire.dashDirection!=Vector2.left)throw new Exception("Dash snapshot roundtrip");
            actor.AnimateWeapon();
            var gun=actor.transform.Find("WeaponAim/Gun").GetComponent<SpriteRenderer>();
            if(gun.enabled)throw new Exception("Gun visible during Oni form");
            actor.Oni.Cancel();actor.SendMessage("LateUpdate");actor.AnimateWeapon();
            if(sr.sprite.texture.name!="DeadZoneDirections"||!gun.enabled)throw new Exception("Base hero/weapon not restored");
            actor.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;
            actor.ResetForMatch(1);actor.transform.position=new Vector2(-3,2);actor.IsLocal=true;actor.enabled=true;game.enabled=true;
            Debug.Log($"DZ_REM_PLAYBACK_PASS: {checkedFrames} frames; idle/run/attack/dash transitions; movement-facing dash; optional network fields; gun hiding and base-form restoration");
        }

        static void Expect(SpriteRenderer renderer,string sheet)
        {
            if(renderer.sprite==null||renderer.sprite.texture.name!="RemOni"+sheet)throw new Exception("Expected Oni "+sheet);
        }
    }
}
