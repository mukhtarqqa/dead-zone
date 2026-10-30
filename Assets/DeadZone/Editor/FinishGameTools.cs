using UnityEditor;
using UnityEngine;

namespace DeadZone.Editor
{
    public static class FinishGameTools
    {
        static DeadZoneGame pending;

        [MenuItem("Dead Zone/Finish Game", false, 20)]
        public static void FinishGame()
        {
            if(!CanFinishGame())return;
            var game=DeadZoneGame.Instance;
            if(game.IntroPlaying)
            {
                pending=game;
                game.GetComponent<ZoneIntro>().Skip();
                EditorApplication.update+=AfterIntro;
                return;
            }
            Complete(game);
        }

        [MenuItem("Dead Zone/Finish Game", true)]
        static bool CanFinishGame()
        {
            var game=DeadZoneGame.Instance;
            return EditorApplication.isPlaying&&!EditorApplication.isPaused&&game!=null&&game.IsAuthority&&!game.FinalePlaying&&!game.Restarting&&pending==null;
        }

        static void AfterIntro()
        {
            if(!EditorApplication.isPlaying||pending==null)
            {
                EditorApplication.update-=AfterIntro;pending=null;return;
            }
            if(pending.IntroPlaying)return;
            var game=pending;pending=null;EditorApplication.update-=AfterIntro;
            Complete(game);
        }

        static void Complete(DeadZoneGame game)
        {
            if(!game.running&&!game.ended)game.StartGameplay();
            // Preview the real victory flow without adding a developer win to the profile.
            bool suppressed=ZoneProfile.SuppressRecording;
            try
            {
                ZoneProfile.SuppressRecording=true;
                game.ApplyExtraction(new ZoneWorld
                {
                    bossesDefeated=DeadZoneGame.BossGoal,
                    extractionPhase=(int)ZoneExtractionPhase.Complete,
                    extractionLanding=game.ExtractionLanding,
                    extractionEntry=game.ExtractionEntry,
                    extractionExit=game.ExtractionExit
                });
                game.wave=5;game.ended=false;game.Finish(true);
                Debug.Log("Finish Game: victory cinematic started (test win is not saved).");
            }
            finally{ZoneProfile.SuppressRecording=suppressed;}
        }
    }
}
