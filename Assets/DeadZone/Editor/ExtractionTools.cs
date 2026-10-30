using System;
using System.IO;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace DeadZone.Editor
{
    public static class ExtractionTools
    {
        [Serializable] sealed class Dataset { public Entry[] entries; }
        [Serializable] sealed class Entry { public string ru,kk; }
        [MenuItem("Dead Zone/Extraction/Prepare helicopter and objective text")]
        public static void Prepare()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
            Folder("Assets/DeadZone","Extraction");Folder("Assets/Resources","Extraction");
            const string prefabPath="Assets/Resources/Extraction/RescueHelicopter.prefab";
            if(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)==null)
            {
                const string materialPath="Assets/DeadZone/Extraction/Helicopter.mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if(material==null){material=new Material(Shader.Find("Sprites/Default")){name="Helicopter unlit vertex colours"};AssetDatabase.CreateAsset(material,materialPath);}
                var previous=SceneManager.GetActiveScene();var temporary=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
                SceneManager.SetActiveScene(temporary);GameObject obj=null;
                try
                {
                    obj=ZoneHelicopterView.CreateModel(material);
                    var meshes=obj.GetComponentsInChildren<MeshFilter>();
                    for(int i=0;i<meshes.Length;i++)
                    {
                        if(i==0)AssetDatabase.CreateAsset(meshes[i].sharedMesh,"Assets/DeadZone/Extraction/HelicopterModel.asset");
                        else AssetDatabase.AddObjectToAsset(meshes[i].sharedMesh,"Assets/DeadZone/Extraction/HelicopterModel.asset");
                    }
                    if(PrefabUtility.SaveAsPrefabAsset(obj,prefabPath)==null)throw new Exception("Could not save editable helicopter prefab.");
                }
                finally{if(obj!=null)UnityEngine.Object.DestroyImmediate(obj);SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(temporary,true);}
            }
            var collection=LocalizationEditorSettings.GetStringTableCollection("DeadZone");
            if(collection==null)throw new Exception("Existing DeadZone localization collection missing.");
            var dataset=JsonUtility.FromJson<Dataset>(File.ReadAllText("Assets/DeadZone/Localization/Translations.json"));
            foreach(var table in collection.StringTables)
            {
                foreach(var entry in dataset.entries)table.AddEntry(ZoneL.Key(entry.ru),table.LocaleIdentifier.Code=="kk"?entry.kk:entry.ru);
                EditorUtility.SetDirty(table);
            }
            EditorUtility.SetDirty(collection.SharedData);AssetDatabase.SaveAssets();
            LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(typeof(ExtractionTools),collection);
            Debug.Log("DZ_EXTRACTION_ASSETS_READY: editable helicopter, RU/KK objectives; no player or content build.");
        }
        static void Folder(string parent,string name){if(!AssetDatabase.IsValidFolder(parent+"/"+name))AssetDatabase.CreateFolder(parent,name);}
    }
}
