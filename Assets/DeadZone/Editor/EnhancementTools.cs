using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
namespace DeadZone.Editor
{
    public static class EnhancementTools
    {
        static AddRequest request;
        [MenuItem("Dead Zone/Install localization")]
        public static void Install()
        {
            request=Client.Add("com.unity.localization");
            EditorApplication.update+=Poll;
        }
        static void Poll()
        {
            if(request==null||!request.IsCompleted)return;
            EditorApplication.update-=Poll;
            if(request.Status==StatusCode.Success)Debug.Log("DZ_LOCALIZATION_INSTALLED");
            else Debug.LogError(request.Error.message);
        }
        [MenuItem("Dead Zone/Open city")]
        public static void OpenCity(){UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/DeadZoneCity.unity");}
    }
}
