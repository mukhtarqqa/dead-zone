using System;
using UnityEditor;
using UnityEngine;

namespace DeadZone.Editor
{
    public static class ExplosiveAudioTools
    {
        [MenuItem("Dead Zone/Audio/Prepare approved HE and C4")]
        public static void Prepare()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before importing.");
            AssetDatabase.Refresh();
            int count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Resources/Audio/Explosives" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                bool mono = path.Contains("pin_throw") || path.Contains("place_arm");
                importer.forceToMono = mono;
                importer.loadInBackground = false;
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.PCM;
                settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null || clip.channels != (mono ? 1 : 2) || clip.frequency != 44100 || clip.length <= .5f)
                    throw new InvalidOperationException("Approved explosive audio import failed: " + path);
                count++;
            }
            if (count != 5 || ZoneExplosiveAudio.GrenadeThrow == null || ZoneExplosiveAudio.MineArm == null ||
                ZoneExplosiveAudio.NextExplosion(false) == null || ZoneExplosiveAudio.NextExplosion(false) == null || ZoneExplosiveAudio.NextExplosion(true) == null)
                throw new InvalidOperationException("Expected five approved HE and C4 clips.");
            Debug.Log("DZ_EXPLOSIVE_IMPORT_PASS: approved HE pin/throw, two HE explosions, C4 placement/ready, C4 explosion; PCM 44100.");
        }
    }
}
