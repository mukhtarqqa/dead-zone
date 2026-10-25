using System;
using UnityEditor;
using UnityEngine;
using TMPro;
namespace DeadZone.Editor
{
    public static class EnhancementDiagnostics
    {
        [MenuItem("Dead Zone/Enhancement regression")]
        public static void Run()
        {
            var g=DeadZoneGame.Instance;if(!EditorApplication.isPlaying||g==null)throw new Exception("Enter Play Mode");
            int initialFrames=Time.frameCount;double timeout=EditorApplication.timeSinceStartup+20;int phase=0;float after=0;ZoneEnemy boss=null;ZoneBossBrain brain=null;
            string language=PlayerPrefs.GetString("dz.language","ru");
            void Poll()
            {
                if(!EditorApplication.isPlaying||g==null){EditorApplication.update-=Poll;return;}
                try
                {
                    if(EditorApplication.timeSinceStartup>timeout)throw new Exception("Enhancement verification timed out");
                    if(phase==0)
                    {
                        if(!ZoneL.Ready)return;ZoneL.Select("kk");after=Time.unscaledTime+.7f;phase=1;return;
                    }
                    if(phase==1)
                    {
                        if(Time.unscaledTime<after)return;
                        if(ZoneL.Get("НАСТРОЙКИ")!="БАПТАУЛАР")return;
                        if(!TMP_Settings.defaultFontAsset.HasCharacters("ӘәҒғҚқҢңӨөҰұҮүҺһІі",out uint[] missing,true,true))throw new Exception("Missing Kazakh glyphs: "+string.Join(",",missing));
                        var stats=new ZoneHeroStatistics();stats.Add(true,3,75,1,200);stats.Add(false,2,25,1,100);stats.Add(true,1,10,0,180);
                        if(stats.matches!=3||stats.wins!=2||stats.kills!=6||stats.damage!=110||stats.revives!=2||stats.bestWinSeconds!=180)throw new Exception("Statistics aggregation failed");
                        ZoneL.Select(language);if(!g.running)g.Begin();g.player.IsLocal=false;
                        foreach(var enemy in g.enemies.ToArray())UnityEngine.Object.Destroy(enemy.gameObject);g.enemies.Clear();
                        g.SpawnEnemy(1,true);boss=g.enemies[g.enemies.Count-1];boss.transform.position=g.player.transform.position+Vector3.up*4;boss.ReadyAt=Time.time+100;brain=boss.GetComponent<ZoneBossBrain>();
                        brain.Tick(g.player);if(brain.Drive(Vector2.one)!=Vector2.zero)throw new Exception("Boss shot has no telegraph stop");
                        if(UnityEngine.Object.FindObjectsByType<ZoneBullet>().Length!=0)throw new Exception("Boss shot before warning");
                        boss.health*=.4f;brain.Tick(g.player);if(!brain.Enraged)throw new Exception("Boss second phase did not start");
                        after=Time.time+.72f;phase=2;return;
                    }
                    if(Time.time<after)return;brain.Tick(g.player);
                    if(UnityEngine.Object.FindObjectsByType<ZoneBullet>().Length<7)throw new Exception("Boss did not fire after telegraph");
                    g.enemies.Remove(boss);UnityEngine.Object.Destroy(boss.gameObject);foreach(var bullet in UnityEngine.Object.FindObjectsByType<ZoneBullet>())UnityEngine.Object.Destroy(bullet.gameObject);
                    g.player.IsLocal=true;EditorApplication.update-=Poll;
                    if(Time.frameCount<=initialFrames)throw new Exception("Frames frozen");
                    Debug.Log("DZ_ENHANCEMENT_PASS: Kazakh table/font, statistics totals and best time, boss warning/delayed volley/phase 2, frames "+initialFrames+"->"+Time.frameCount);
                }
                catch(Exception error){EditorApplication.update-=Poll;ZoneL.Select(language);g.player.IsLocal=true;Debug.LogException(error);}
            }
            EditorApplication.update+=Poll;
        }
        [MenuItem("Dead Zone/Preview Kazakh options")]
        public static void PreviewKazakh(){ZoneL.Select("kk");DeadZoneGame.Instance.GetComponent<ZoneOptions>().Show();}
        [MenuItem("Dead Zone/Preview statistics")]
        public static void PreviewStats(){DeadZoneGame.Instance.GetComponent<ZoneStatisticsPanel>().Show();}
        [MenuItem("Dead Zone/Restore Russian")]
        public static void RestoreRussian(){ZoneL.Select("ru");}
    }
}
