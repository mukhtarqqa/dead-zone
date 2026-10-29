using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DeadZone.Editor
{
    public static class ZombieAudioTools
    {
        const string Root = "Assets/Resources/Audio/Zombies";
        const string BankPath = "Assets/Resources/ZombieAudioBank.asset";

        [MenuItem("Dead Zone/Audio/Prepare approved zombie sounds")]
        public static void Prepare()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before preparing audio assets.");
            AssetDatabase.Refresh();
            var paths = AssetDatabase.FindAssets("t:AudioClip", new[] { Root })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p, StringComparer.Ordinal).ToArray();
            foreach (var path in paths)
            {
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                importer.forceToMono = true;
                importer.loadInBackground = false;
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.PCM;
                settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
            var bank = AssetDatabase.LoadAssetAtPath<ZoneZombieAudioBank>(BankPath);
            if (bank == null)
            {
                bank = ScriptableObject.CreateInstance<ZoneZombieAudioBank>();
                AssetDatabase.CreateAsset(bank, BankPath);
            }
            bank.growls = paths.Where(p => p.Contains("/Growls/")).Select(AssetDatabase.LoadAssetAtPath<AudioClip>).ToArray();
            bank.attacks = paths.Where(p => p.Contains("/Attacks/")).Select(AssetDatabase.LoadAssetAtPath<AudioClip>).ToArray();
            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssets();
            Validate(bank);
            Debug.Log($"DZ_ZOMBIE_AUDIO_IMPORT_PASS: {bank.growls.Length} growls, {bank.attacks.Length} attacks; mono PCM, preserved sample rate.");
        }

        static void Validate(ZoneZombieAudioBank bank)
        {
            if (bank == null || bank.growls.Length != 3 || bank.attacks.Length != 10)
                throw new InvalidOperationException("Expected the three approved growls and ten attack slices.");
            foreach (var clip in bank.growls.Concat(bank.attacks))
                if (clip == null || clip.channels != 1 || clip.length < .18f || clip.length > 5 || clip.frequency != 44100)
                    throw new InvalidOperationException("Unexpected zombie audio import: " + (clip != null ? clip.name : "missing clip"));
        }

        [MenuItem("Dead Zone/Audio/Check zombie playback")]
        public static void CheckPlayback()
        {
            var game = DeadZoneGame.Instance;
            if (!EditorApplication.isPlaying || game == null || game.player == null || game.running)
                throw new InvalidOperationException("Run this check from the Play mode main menu.");
            Validate(ZoneZombieAudio.Bank);
            if (UnityEngine.Object.FindObjectsByType<AudioListener>().Count(l => l.enabled) != 1)
                throw new InvalidOperationException("Exactly one active listener is required.");
            bool enabled = game.enabled;
            var music = game.GetComponent<ZoneMusic>();
            bool musicEnabled = music != null && music.enabled;
            float volume = ZoneFeedback.Volume;
            game.enabled = false;
            if (music != null) music.enabled = false;
            game.running = true;
            ZoneFeedback.Volume = .65f;
            var probes = new System.Collections.Generic.List<GameObject>();
            var tick = typeof(ZoneZombieVoice).GetMethod("LateUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var state = new ZoneObjectState { type = 1, health = 100, maxHealth = 100, animation = 3, floor = game.player.gameObject.layer, scale = 1, position = (Vector2)game.player.transform.position + Vector2.right };
            ZoneGhost first = null;
            ZoneZombieVoice emitter = null;
            AudioSource source = null;
            AudioClip firstClip = null;
            float started = 0;
            double startedDsp = 0;
            int frame = 0;
            ZoneGhost MakeGhost(int id)
            {
                var obj = new GameObject("Zombie audio probe " + id);
                probes.Add(obj);
                obj.AddComponent<SpriteRenderer>();
                var ghost = obj.AddComponent<ZoneGhost>();
                ghost.Apply(game, state);
                return ghost;
            }
            void Clean()
            {
                EditorApplication.update -= Verify;
                foreach (var obj in probes) if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
                game.running = false;
                game.paused = false;
                game.enabled = enabled;
                if (music != null) music.enabled = musicEnabled;
                ZoneFeedback.Volume = volume;
            }
            try
            {
                first = MakeGhost(0);
                emitter = first.GetComponent<ZoneZombieVoice>();
                tick.Invoke(emitter, null);
                var bus = UnityEngine.Object.FindAnyObjectByType<ZoneZombieAudio>();
                source = bus.GetComponents<AudioSource>().Single(s => s.isPlaying);
                firstClip = source.clip;
                started = Time.unscaledTime;
                startedDsp = AudioSettings.dspTime;
                frame = Time.frameCount;
            }
            catch { Clean(); throw; }
            void Verify()
            {
                if (Time.unscaledTime - started < .2f ||
                    source.isPlaying && source.timeSamples == 0 && AudioSettings.dspTime - startedDsp < 1) return;
                try
                {
                    double dspElapsed = AudioSettings.dspTime - startedDsp;
                    if (Time.frameCount <= frame || dspElapsed <= 0 || !emitter.Audible ||
                        source.isPlaying && source.timeSamples == 0 ||
                        !source.isPlaying && dspElapsed < firstClip.length / source.pitch - .05f)
                        throw new Exception($"Playback check: frames={Time.frameCount - frame}, audioTime={source.time:F3}, DSP={dspElapsed:F3}, playing={source.isPlaying}, audible={emitter.Audible}");
                    float before = source.time;
                    for (int i = 0; i < 8; i++) { first.Apply(game, state); tick.Invoke(emitter, null); }
                    if (source.clip != firstClip || source.time < before - .02f) throw new Exception("Repeated snapshots restarted the attack.");
                    state.position = (Vector2)game.player.transform.position + Vector2.right * 100;
                    var far = MakeGhost(1).GetComponent<ZoneZombieVoice>();
                    if (ZoneZombieAudio.Play(far, true)) throw new Exception("Far enemy was audible.");
                    state.position = (Vector2)game.player.transform.position + Vector2.left;
                    state.floor++;
                    var upstairs = MakeGhost(2).GetComponent<ZoneZombieVoice>();
                    if (ZoneZombieAudio.Play(upstairs, true)) throw new Exception("Other floor was audible.");
                    state.floor--;
                    state.health = 0;
                    var dead = MakeGhost(3).GetComponent<ZoneZombieVoice>();
                    if (ZoneZombieAudio.Play(dead, true)) throw new Exception("Dead enemy played audio.");
                    state.health = 100;
                    game.paused = true;
                    if (ZoneZombieAudio.Play(emitter, true)) throw new Exception("Paused game accepted new audio.");
                    game.paused = false;
                    if (!ZoneZombieAudio.Play(emitter, true)) throw new Exception("Near emitter failed to play.");
                    var dyingSource = UnityEngine.Object.FindAnyObjectByType<ZoneZombieAudio>().GetComponents<AudioSource>().Single(s => s.isPlaying);
                    UnityEngine.Object.DestroyImmediate(first.gameObject);
                    if (dyingSource.isPlaying) throw new Exception("Destroyed emitter kept playing.");
                    for (int i = 4; i < 16; i++)
                    {
                        state.position = (Vector2)game.player.transform.position + new Vector2(.1f * i, 0);
                        tick.Invoke(MakeGhost(i).GetComponent<ZoneZombieVoice>(), null);
                    }
                    var bus = UnityEngine.Object.FindAnyObjectByType<ZoneZombieAudio>();
                    if (bus.GetComponents<AudioSource>().Length != ZoneZombieAudio.Bank.voiceLimit || bus.GetComponents<AudioSource>().Count(s => s.isPlaying) > ZoneZombieAudio.Bank.voiceLimit)
                        throw new Exception("Zombie voice pool exceeded its limit.");
                    Debug.Log("DZ_ZOMBIE_AUDIO_PLAYBACK_PASS: audio advanced; duplicate snapshots, distance, floor, death, pause and voice cap checked.");
                }
                catch (Exception error) { Debug.LogError("DZ_ZOMBIE_AUDIO_PLAYBACK_FAIL: " + error); }
                finally { Clean(); }
            }
            EditorApplication.update += Verify;
        }
    }
}
