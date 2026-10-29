using System;
using UnityEditor;
using UnityEngine;
namespace DeadZone.Editor
{
    public static class WeaponAudioTools
    {
        [MenuItem("Dead Zone/Audio/Prepare approved Glock Negev Nova")]
        public static void Prepare()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before importing.");
            AssetDatabase.Refresh();
            foreach (var folder in new[] { "Glock18", "Negev", "Nova" })
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Resources/Audio/Weapons/" + folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                importer.forceToMono = path.Contains("reload") || path.Contains("pump");
                importer.loadInBackground = false;
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.PCM;
                settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
            foreach (int kind in new[] { 0, 2, 3 })
            {
                var shot = ZoneWeaponAudio.NextShot(kind);
                var reload = ZoneWeaponAudio.Reload(kind);
                if (shot == null || shot.channels != 2 || reload == null || reload.channels != 1 || Mathf.Abs(reload.length - ZoneArsenal.Stats(kind).Reload) > .001f)
                    throw new InvalidOperationException("Approved weapon audio import failed: " + kind);
            }
            if (ZoneWeaponAudio.NovaPump == null) throw new InvalidOperationException("Missing Nova pump.");
            Debug.Log("DZ_WEAPON_BATCH_IMPORT_PASS: Glock18, Negev, Nova shots and reloads; separate Nova pump; preserved reload times.");
        }
    }
}
