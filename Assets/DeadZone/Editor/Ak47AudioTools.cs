using System;
using UnityEditor;
using UnityEngine;

namespace DeadZone.Editor
{
    public static class Ak47AudioTools
    {
        [MenuItem("Dead Zone/Audio/Prepare approved AK47 shots")]
        public static void Prepare()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before importing audio.");
            AssetDatabase.Refresh();
            foreach (var name in new[] { "ak47_01", "ak47_02", "ak47_04" })
            {
                string path = "Assets/Resources/Audio/Weapons/AK47/" + name + ".wav";
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null) throw new InvalidOperationException("Missing approved AK47 recording: " + path);
                importer.forceToMono = false;
                importer.loadInBackground = false;
                var settings = importer.defaultSampleSettings;
                settings.compressionFormat = AudioCompressionFormat.PCM;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null || clip.channels != 2 || clip.frequency != 44100 || clip.length < 1 || clip.length > 3)
                    throw new InvalidOperationException("Unexpected AK47 audio import: " + name);
            }
            Debug.Log("DZ_AK47_AUDIO_IMPORT_PASS: three approved CS2 shots; stereo PCM, 44100 Hz.");
        }
    }
}
