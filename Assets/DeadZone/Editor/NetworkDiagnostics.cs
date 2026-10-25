using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DeadZone.Editor
{
    public static class NetworkDiagnostics
    {
        [MenuItem("Dead Zone/Host internet smoke test")]
        public static void HostInternetSmoke()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
            var game = DeadZoneGame.Instance;
            game.selected = 0; game.network.HostInternet();
            double deadline = EditorApplication.timeSinceStartup + 90;
            bool started = false, wroteCode = false;
            void Poll()
            {
                if (!EditorApplication.isPlaying || game == null) { EditorApplication.update -= Poll; return; }
                if (!wroteCode && !string.IsNullOrEmpty(game.network.RoomCode))
                {
                    wroteCode = true;
                    string folder = Path.Combine(Directory.GetParent(Application.dataPath).Parent.Parent.FullName, "work"); Directory.CreateDirectory(folder);
                    File.WriteAllText(Path.Combine(folder, "network-room-code.txt"), game.network.RoomCode);
                    Debug.Log("DZ_INTERNET_ROOM_READY");
                }
                if (game.network.MemberCount >= 2 && !started) { game.Begin(); started = true; Debug.Log("DZ_INTERNET_TWO_PLAYERS"); }
                if (!game.network.Busy && !game.network.Active && !wroteCode) { EditorApplication.update -= Poll; Debug.LogError("DZ_INTERNET_SETUP_FAIL: " + game.network.Status); }
                else if (EditorApplication.timeSinceStartup > deadline) { EditorApplication.update -= Poll; Debug.Log("DZ_INTERNET_TEST_FINISHED: " + game.network.Status); }
            }
            EditorApplication.update += Poll;
        }
        [MenuItem("Dead Zone/Build Windows test client")]
        public static void BuildClient()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before build.");
                string output = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds", "DeadZone.exe");
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                PlayerSettings.runInBackground = true;
                Debug.Log("DZ_NET_BUILD_STARTED");
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { Array.Find(EditorBuildSettings.scenes, scene => scene.enabled).path }, locationPathName = output, target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(output), "build-result.txt"), report.summary.result + " errors=" + report.summary.totalErrors);
                Debug.Log(report.summary.result == BuildResult.Succeeded ? "DZ_NET_BUILD_PASS" : "DZ_NET_BUILD_FAIL");
        }
        [MenuItem("Dead Zone/Host LAN smoke test")]
        public static void StartHostSmoke()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
            var game = DeadZoneGame.Instance;
            // Keep manual input from starting the match before the test client has joined.
            game.enabled = false; game.player.IsLocal = false;
            game.running = game.ended = false; game.kills = 0; game.damageDealt = 0;
            foreach (var enemy in game.enemies.ToArray()) UnityEngine.Object.Destroy(enemy.gameObject);
            game.enemies.Clear();
            game.selected = 2; game.network.HostLan();
            double deadline = EditorApplication.timeSinceStartup + 90;
            bool started = false, gojoTested = false, remRestored = false; float matchAt = 0;
            void Poll()
            {
                if (!EditorApplication.isPlaying || game == null) { EditorApplication.update -= Poll; return; }
                if (game.network.MemberCount >= 2 && !started) { game.Begin(); started = true; matchAt=Time.time; ZonePulse.Spawn(game.player.transform.position,game.player.gameObject.layer,.9f,3,12);
                    foreach(var actor in game.players) { actor.charge=100; actor.SetCommand(new ZoneCommand { ability=true, aim=(Vector2)actor.transform.position+Vector2.right*4 }); }
                    Debug.Log("DZ_NET_HOST_TWO_PLAYERS: match started."); }
                if(started && !gojoTested && Time.time-matchAt>1)
                {
                    gojoTested=true;game.player.Hit(1);
                }
                if(started && !remRestored && Time.time-matchAt>2){remRestored=true;foreach(var actor in game.players)if(actor.CharacterIndex==1)actor.Oni.Cancel();}
                if (started && game.network.MemberCount == 1) { EditorApplication.update -= Poll; Debug.Log("DZ_NET_HOST_DISCONNECT_PASS: disconnected peer was removed from active count."); }
                else if (EditorApplication.timeSinceStartup > deadline) { EditorApplication.update -= Poll; Debug.LogError("DZ_NET_HOST_TEST_TIMEOUT"); }
            }
            EditorApplication.update += Poll;
        }
    }
}
