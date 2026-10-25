using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DeadZone.Data;
namespace DeadZone.Editor
{
    public static class SukunaBuilder
    {
        [MenuItem("Dead Zone/Add Sukuna to catalog")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new System.Exception("Exit Play Mode");
            const string path="Assets/Resources/SukunaDirections.png";
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;importer.isReadable=true;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.SaveAndReimport();
            var catalog=AssetDatabase.LoadAssetAtPath<HeroCatalog>("Assets/DeadZone/Content/HeroCatalog.asset");
            const string heroPath="Assets/DeadZone/Content/hero_sukuna.asset";var hero=AssetDatabase.LoadAssetAtPath<HeroDefinition>(heroPath);
            if(hero==null){hero=ScriptableObject.CreateInstance<HeroDefinition>();AssetDatabase.CreateAsset(hero,heroPath);}
            hero.id="sukuna";hero.displayName="Рёмен Сукуна";hero.maxHealth=110;hero.moveSpeed=3.5f;hero.damageToCharge=1100;hero.activeAbility=ActiveAbility.MalevolentShrine;hero.passiveAbility=PassiveAbility.CurseKing;
            if(hero.portrait==null){hero.portrait=ZoneActorArt.Sukuna(Vector2.down,0);AssetDatabase.AddObjectToAsset(hero.portrait,hero);}
            var characters=new List<HeroDefinition>(catalog.characters);characters.RemoveAll(item=>item!=null&&item.id=="sukuna");characters.Insert(Mathf.Min(3,characters.Count),hero);catalog.characters=characters.ToArray();
            if(characters.Count>2){characters[0].moveSpeed=3.7f;characters[1].moveSpeed=3.4f;characters[2].moveSpeed=3.65f;characters[2].maxHealth=90;for(int i=0;i<3;i++)EditorUtility.SetDirty(characters[i]);}
            EditorUtility.SetDirty(hero);EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();Debug.Log("DZ_SUKUNA_CATALOG_READY: fourth unique hero, 110 HP, 12 directional poses, Shrine and Curse King");
        }
    }
}
