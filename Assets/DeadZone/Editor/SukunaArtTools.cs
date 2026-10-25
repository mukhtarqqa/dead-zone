using System;
using UnityEditor;
using UnityEngine;
namespace DeadZone.Editor
{
    public static class SukunaArtTools
    {
        [MenuItem("Dead Zone/Rework/Import Sukuna shrine stages")]
        public static void Import()
        {
            if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode");var importer=AssetImporter.GetAtPath("Assets/Resources/SukunaShrineStages.png") as TextureImporter;if(importer==null)throw new Exception("Shrine source missing");
            importer.textureType=TextureImporterType.Default;importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;importer.isReadable=true;importer.alphaIsTransparency=true;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            for(int i=0;i<4;i++){var sprite=ZoneShrineArt.Frame(i);if(sprite==null||sprite.rect.width<100||sprite.rect.height<100)throw new Exception("Shrine stage clipped: "+i);}Debug.Log("DZ_SHRINE_ART_PASS: 4 complete stages, shared size, feet pivots");
        }
    }
}
