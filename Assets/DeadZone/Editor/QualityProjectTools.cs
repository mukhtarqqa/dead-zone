using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
namespace DeadZone.Editor
{
    public static class QualityProjectTools
    {
        [MenuItem("Dead Zone/Build polished Windows game")]
        public static void BuildWindows()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play Mode first");
            string root=Directory.GetParent(Application.dataPath).FullName;
            string output=Path.Combine(root,"Builds","Quality","DeadZone.exe");Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report=UnityEditor.BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/DeadZoneCity.unity"},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.WriteAllText(Path.Combine(root,"quality-build-result.txt"),report.summary.result+" errors="+report.summary.totalErrors);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.Exception("Quality build failed");
            Debug.Log("DZ_QUALITY_BUILD_PASS");
        }
        // Can be used with -batchmode -executeMethod for clean-checkout validation.
        public static void ValidateCleanCheckout()
        {
            if(!File.Exists("Assets/Scenes/DeadZoneCity.unity"))throw new System.Exception("Main scene missing");
            foreach(string path in AssetDatabase.GetDependencies("Assets/Scenes/DeadZoneCity.unity",true))if(path.StartsWith("Assets/Cainos")||path.StartsWith("Assets/LoBitCity"))throw new System.Exception("Restricted dependency: "+path);
            EditorSceneManagerOpen();
            BuildWindows();Debug.Log("DZ_CLEAN_CHECKOUT_PASS");
        }
        static void EditorSceneManagerOpen(){UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/DeadZoneCity.unity");}
        static AddAndRemoveRequest request;
        [MenuItem("Dead Zone/Remove unused prototype packages")]
        public static void CleanPackages()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play Mode first");
            string[] remove={"com.unity.ai.assistant","com.unity.ai.inference","com.unity.purchasing","com.unity.analytics","com.unity.learn.iet-framework","com.unity.collab-proxy","com.unity.xr.legacyinputhelpers","com.unity.visualscripting","com.unity.multiplayer.center","com.unity.ai.navigation","com.unity.2d.psdimporter","com.unity.2d.aseprite"};
            request=Client.AddAndRemove(System.Array.Empty<string>(),remove);EditorApplication.update+=Poll;
        }
        static void Poll()
        {
            if(request==null||!request.IsCompleted)return;EditorApplication.update-=Poll;
            string result=request.Status==StatusCode.Success?"DZ_PACKAGES_CLEAN_PASS":"DZ_PACKAGES_CLEAN_FAIL: "+request.Error?.message;
            File.WriteAllText(Path.Combine(Directory.GetParent(Application.dataPath).FullName,"package-cleanup-result.txt"),result);
            if(request.Status==StatusCode.Success)Debug.Log(result);else Debug.LogError(result);request=null;
        }
    }
}
