using System;
using UnityEditor;
using UnityEngine;

namespace DeadZone.Editor
{
    public static class Ak47AudioTools
    {
        [MenuItem("Dead Zone/Audio/Prepare approved AK47 reload")]
        public static void PrepareReload()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before importing audio.");
            const string path = "Assets/Resources/Audio/Weapons/AK47/ak47_reload.wav";
            AssetDatabase.Refresh();
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) throw new InvalidOperationException("Missing approved reload recording.");
            importer.forceToMono = true;
            importer.loadInBackground = false;
            var settings = importer.defaultSampleSettings;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null || clip.channels != 1 || clip.frequency != 44100 || Mathf.Abs(clip.length - ZoneArsenal.Stats(1).Reload) > .001f)
                throw new InvalidOperationException("AK47 reload must match the existing 1.8-second reload.");
            Debug.Log("DZ_AK47_RELOAD_IMPORT_PASS: approved 1.8-second reload, mono PCM, 44100 Hz.");
        }

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
