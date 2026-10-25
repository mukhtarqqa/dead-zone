using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using TMPro;
namespace DeadZone.Editor
{
    public static class LocalizationBuilder
    {
        [Serializable] sealed class Dataset{public Entry[] entries;}
        [Serializable] sealed class Entry{public string ru,kk;}
        [MenuItem("Dead Zone/Build Russian and Kazakh tables")]
        public static void Build()
        {
            const string root="Assets/DeadZone/Localization";
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode");
            var settings=LocalizationEditorSettings.ActiveLocalizationSettings;
            if(settings==null){settings=ScriptableObject.CreateInstance<LocalizationSettings>();AssetDatabase.CreateAsset(settings,root+"/LocalizationSettings.asset");LocalizationEditorSettings.ActiveLocalizationSettings=settings;}
            foreach(string code in new[]{"ru","kk"})
            {
                bool exists=false;foreach(var locale in LocalizationEditorSettings.GetLocales())if(locale.Identifier.Code==code)exists=true;
                if(!exists){var locale=Locale.CreateLocale(code);AssetDatabase.CreateAsset(locale,root+"/"+code+".asset");LocalizationEditorSettings.AddLocale(locale);}
            }
            LocalizationSettings.StartupLocaleSelectors.Clear();LocalizationSettings.StartupLocaleSelectors.Add(new SpecificLocaleSelector{LocaleId=new LocaleIdentifier("ru")});EditorUtility.SetDirty(settings);
            var collection=LocalizationEditorSettings.GetStringTableCollection("DeadZone")??LocalizationEditorSettings.CreateStringTableCollection("DeadZone",root);
            var dataset=JsonUtility.FromJson<Dataset>(File.ReadAllText(root+"/Translations.json"));
            foreach(var table in collection.StringTables)
            {
                foreach(var entry in dataset.entries)table.AddEntry(ZoneL.Key(entry.ru),table.LocaleIdentifier.Code=="kk"?entry.kk:entry.ru);
                EditorUtility.SetDirty(table);
            }
            EditorUtility.SetDirty(collection.SharedData);EditorUtility.SetDirty(collection);LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(typeof(LocalizationBuilder),collection);
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(root+"/DeadZoneUIStatic.asset");
            if(font==null)
            {
                var source=AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
                font=TMP_FontAsset.CreateFontAsset(source,80,8,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDF16,2048,2048,AtlasPopulationMode.Dynamic,true);font.name="DeadZone UI RU KK Static";font.isMultiAtlasTexturesEnabled=true;
                AssetDatabase.CreateAsset(font,root+"/DeadZoneUIStatic.asset");foreach(var texture in font.atlasTextures)AssetDatabase.AddObjectToAsset(texture,font);AssetDatabase.AddObjectToAsset(font.material,font);font.material.mainTexture=font.atlasTexture;
            }
            var glyphs=new HashSet<char>("ЁёӘәҒғҚқҢңӨөҰұҮүҺһІі∞→·—×−↑↓←".ToCharArray());for(char c=' ';c<='~';c++)glyphs.Add(c);for(char c='А';c<='я';c++)glyphs.Add(c);
            foreach(var entry in dataset.entries)foreach(char c in entry.ru+entry.kk)if(!char.IsControl(c))glyphs.Add(c);
            var requested=new char[glyphs.Count];glyphs.CopyTo(requested);string chars=new string(requested);font.atlasPopulationMode=AtlasPopulationMode.Dynamic;
            if(!font.HasCharacters(chars)&&!font.TryAddCharacters(chars,out string missing))throw new Exception("UI font glyphs missing: "+missing);
            font.atlasPopulationMode=AtlasPopulationMode.Static;foreach(var texture in font.atlasTextures)texture.filterMode=FilterMode.Bilinear;
            font.material.mainTexture=font.atlasTexture;font.material.SetFloat("_OutlineWidth",0);font.material.SetFloat("_FaceDilate",0);font.material.SetFloat("_GradientScale",font.atlasPadding+1);
            EditorUtility.SetDirty(font);EditorUtility.SetDirty(font.material);foreach(var texture in font.atlasTextures)EditorUtility.SetDirty(texture);
            var tmp=new SerializedObject(TMP_Settings.instance);tmp.FindProperty("m_defaultFontAsset").objectReferenceValue=font;tmp.ApplyModifiedPropertiesWithoutUndo();
            var assets=LocalizationEditorSettings.GetAssetTableCollection("DeadZoneAssets")??LocalizationEditorSettings.CreateAssetTableCollection("DeadZoneAssets",root);
            string guid=AssetDatabase.AssetPathToGUID(root+"/DeadZoneUIStatic.asset");var addressables=AddressableAssetSettingsDefaultObject.GetSettings(true);addressables.RemoveAssetEntry(AssetDatabase.AssetPathToGUID(root+"/DeadZoneUI.asset"));addressables.CreateOrMoveEntry(guid,addressables.DefaultGroup);
            foreach(var table in assets.AssetTables){table.AddEntry("UI.Font",guid);EditorUtility.SetDirty(table);}EditorUtility.SetDirty(assets.SharedData);EditorUtility.SetDirty(assets);
            LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(typeof(LocalizationBuilder),assets);
            AssetDatabase.SaveAssets();bool hasMaterial=false;foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(root+"/DeadZoneUIStatic.asset"))if(asset is Material)hasMaterial=true;
            if(!hasMaterial)throw new Exception("Font material not persisted");
            if(!UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes())throw new Exception("Could not save open scenes before localization content build");
            AddressableAssetSettings.BuildPlayerContent(out UnityEditor.AddressableAssets.Build.AddressablesPlayerBuildResult build);if(build!=null&&!string.IsNullOrEmpty(build.Error))throw new Exception("Localization content build failed: "+build.Error);Debug.Log("DZ_LOCALIZATION_TABLES_READY: RU/KK, "+dataset.entries.Length+" strings, static SDF16 primary font / scale "+font.faceInfo.scale+" / baked glyphs "+glyphs.Count);
        }
    }
}
