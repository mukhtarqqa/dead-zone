using System;
using System.IO;
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
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(root+"/DeadZoneUI.asset");
            if(font==null)
            {
                var source=AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
                font=TMP_FontAsset.CreateFontAsset(source,64,8,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);font.name="DeadZone UI RU KK";font.isMultiAtlasTexturesEnabled=true;
                AssetDatabase.CreateAsset(font,root+"/DeadZoneUI.asset");foreach(var texture in font.atlasTextures)AssetDatabase.AddObjectToAsset(texture,font);AssetDatabase.AddObjectToAsset(font.material,font);font.material.mainTexture=font.atlasTexture;
            }
            string chars="ӘәҒғҚқҢңӨөҰұҮүҺһІі";if(!font.HasCharacters(chars)&&!font.TryAddCharacters(chars,out string missing))throw new Exception("Kazakh font glyphs missing: "+missing);
            EditorUtility.SetDirty(font);EditorUtility.SetDirty(font.material);foreach(var texture in font.atlasTextures)EditorUtility.SetDirty(texture);
            var tmp=new SerializedObject(TMP_Settings.instance);tmp.FindProperty("m_defaultFontAsset").objectReferenceValue=font;tmp.ApplyModifiedPropertiesWithoutUndo();
            var assets=LocalizationEditorSettings.GetAssetTableCollection("DeadZoneAssets")??LocalizationEditorSettings.CreateAssetTableCollection("DeadZoneAssets",root);
            string guid=AssetDatabase.AssetPathToGUID(root+"/DeadZoneUI.asset");var addressables=AddressableAssetSettingsDefaultObject.GetSettings(true);addressables.CreateOrMoveEntry(guid,addressables.DefaultGroup);
            foreach(var table in assets.AssetTables){table.AddEntry("UI.Font",guid);EditorUtility.SetDirty(table);}EditorUtility.SetDirty(assets.SharedData);EditorUtility.SetDirty(assets);
            LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(typeof(LocalizationBuilder),assets);
            AssetDatabase.SaveAssets();bool hasMaterial=false;foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(root+"/DeadZoneUI.asset"))if(asset is Material)hasMaterial=true;
            if(!hasMaterial)throw new Exception("Font material not persisted");
            AddressableAssetSettings.BuildPlayerContent();Debug.Log("DZ_LOCALIZATION_TABLES_READY: RU/KK, "+dataset.entries.Length+" strings, Kazakh glyphs and persisted font material");
        }
    }
}
