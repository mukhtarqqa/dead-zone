using UnityEditor;
using UnityEngine;
namespace DeadZone.Editor
{
    public static class IntroDiagnostics
    {
        public static string Result="Not run";
        public static void Check()
        {
            var g=DeadZoneGame.Instance;ZoneProfile.SuppressRecording=true;g.selected=2;
            int test=0;bool waiting=false;float began=Time.unscaledTime;
            Result="Running";
            void Poll()
            {
                if(!EditorApplication.isPlaying){EditorApplication.update-=Poll;return;}
                try
                {
                    if(Time.unscaledTime-began>20)throw new System.Exception("Timeout");
                    if(waiting)
                    {
                        if(g.IntroPlaying)return;
                        if(!g.running||g.wave!=1||g.InputBlocked||g.player.CharacterIndex!=2)throw new System.Exception("Bad handoff");
                        test++;waiting=false;
                        if(test==4){Result="PASS: button, duplicate guard, skip beginning/middle/end, natural finish, replay, hero preserved, first wave";EditorApplication.update-=Poll;Debug.Log("DZ_INTRO_PASS: "+Result);return;}
                    }
                    g.running=false;g.ended=false;
                    foreach(var e in g.enemies.ToArray())if(e!=null)Object.Destroy(e.gameObject);g.enemies.Clear();
                    var button=Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include);
                    if(test==0){foreach(var b in button)if(b.name=="Start"){b.onClick.Invoke();break;}}else g.Begin();
                    var intro=g.GetComponent<ZoneIntro>();g.Begin();
                    if(!g.IntroPlaying||g.running||!g.InputBlocked||g.enemies.Count!=0||Object.FindObjectsByType<ZoneIntro>().Length!=1)throw new System.Exception("Start gate failed");
                    intro.Seek(test==0?0:test==1?65:test==2?ZoneIntroScript.Duration-2:ZoneIntroScript.Duration);
                    if(test<3)intro.Skip();waiting=true;
                }
                catch(System.Exception e){Result="FAIL: "+e.Message;EditorApplication.update-=Poll;Debug.LogException(e);}
            }
            EditorApplication.update+=Poll;
        }
    }
}
