using System;
using UnityEditor;
using UnityEngine;
namespace DeadZone.Editor
{
    public static class OutbreakUiArtTools
    {
        [MenuItem("Dead Zone/Rework/Import quarantine interface art")]
        public static void Import()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode");
            const string path="Assets/Resources/DeadZoneUiBackdrop.png";var importer=AssetImporter.GetAtPath(path)as TextureImporter;if(importer==null)throw new Exception("Missing quarantine backdrop");
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=false;importer.isReadable=false;importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.alphaIsTransparency=false;importer.SaveAndReimport();
            if(AssetDatabase.LoadAssetAtPath<Sprite>(path)==null)throw new Exception("Backdrop Sprite import failed");Debug.Log("DZ_OUTBREAK_UI_ART_PASS");
        }
    }
}
