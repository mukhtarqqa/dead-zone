using System;
using UnityEditor;
using UnityEngine;

namespace DeadZone.Editor
{
    public static class HeroVoiceTools
    {
        [MenuItem("Dead Zone/Audio/Prepare approved hero voices")]
        public static void Prepare()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before importing.");
            AssetDatabase.Refresh();
            int count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Resources/Audio/Heroes" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                importer.forceToMono = true; importer.loadInBackground = false;
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.PCM;
                settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                importer.defaultSampleSettings = settings; importer.SaveAndReimport();
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null || clip.channels != 1 || clip.frequency != 44100 || clip.length <= 1)
                    throw new InvalidOperationException("Invalid hero voice: " + path);
                count++;
            }
            if (count != 3 || ZoneHeroVoice.Clip(0) == null || ZoneHeroVoice.Clip(1) == null || ZoneHeroVoice.Clip(3) == null || ZoneHeroVoice.Clip(2) != null)
                throw new InvalidOperationException("Expected only the approved Subaru, Rem A and Sukuna voices.");
            Debug.Log("DZ_HERO_VOICE_IMPORT_PASS: Subaru, Rem A, Sukuna; Gojo excluded; mono PCM 44100.");
        }
    }
}
