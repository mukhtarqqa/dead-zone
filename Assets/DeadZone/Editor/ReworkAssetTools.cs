using System;
using UnityEditor;
using UnityEngine;
namespace DeadZone.Editor
{
    public static class ReworkAssetTools
    {
        [MenuItem("Dead Zone/Rework/Import art and combat tuning")]
        public static void Import()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode");
            foreach(string role in new[]{"Walker","Ranger","Runner","Brute"})
            {
                string path="Assets/Resources/Enemy"+role+"Directions.png";var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)throw new Exception("Missing "+path);
                importer.textureType=TextureImporterType.Default;importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.isReadable=true;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            }
            var balance=Resources.Load<ZoneBalance>("DeadZoneBalance");if(balance==null)throw new Exception("Missing balance asset");
            balance.enemiesBase=24;balance.enemiesPerWave=20;balance.maxAlive=22;balance.spawnInterval=2.2f;balance.groupBase=4;balance.groupMemberInterval=.22f;balance.bossHealthBase=1100;balance.bossHealthPerWave=380;balance.enemyHealthBase=52;balance.enemyHealthPerWave=10;balance.breakSeconds=14;
            EditorUtility.SetDirty(balance);AssetDatabase.SaveAssets();Debug.Log("DZ_REWORK_ASSETS_READY: 4 role sheets, revision 2 combat balance");
        }
    }
}
