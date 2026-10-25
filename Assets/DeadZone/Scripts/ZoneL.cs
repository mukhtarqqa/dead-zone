using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
namespace DeadZone
{
    public static class ZoneL
    {
        public static string Key(string source){uint hash=2166136261;foreach(char c in source){hash^=c;hash*=16777619;}return "s"+hash.ToString("x8");}
        public static bool Ready=>LocalizationSettings.InitializationOperation.IsDone;
        public static string Get(string source)
        {
            if(string.IsNullOrEmpty(source)||!Ready)return source;
            var handle=LocalizationSettings.StringDatabase.GetTableAsync("DeadZone");if(!handle.IsDone||handle.Result==null)return source;
            string key=Key(source);if(handle.Result.GetEntry(key)==null)return source;
            return LocalizationSettings.StringDatabase.GetLocalizedString("DeadZone",key);
        }
        public static string Format(string source,params object[] args)=>string.Format(Get(source),args);
        public static void Attach(TMP_Text text){if(text.GetComponent<ZoneLocalizedLabel>()==null)text.gameObject.AddComponent<ZoneLocalizedLabel>();}
        public static void Select(string code)
        {
            var locale=LocalizationSettings.AvailableLocales.GetLocale(code);if(locale==null)return;
            LocalizationSettings.SelectedLocale=locale;PlayerPrefs.SetString("dz.language",code);PlayerPrefs.Save();
        }
    }
    public sealed class ZoneLanguageStartup : MonoBehaviour
    {
        System.Collections.IEnumerator Start(){yield return LocalizationSettings.InitializationOperation;ZoneL.Select(PlayerPrefs.GetString("dz.language","ru"));}
    }
    public sealed class ZoneLocalizedLabel : MonoBehaviour
    {
        bool bound;
        void LateUpdate()
        {
            if(bound||!ZoneL.Ready)return;var text=GetComponent<TMP_Text>();if(text==null||string.IsNullOrEmpty(text.text))return;
            var table=LocalizationSettings.StringDatabase.GetTableAsync("DeadZone");if(!table.IsDone||table.Result==null||table.Result.GetEntry(ZoneL.Key(text.text))==null)return;
            bound=true;var localizer=gameObject.AddComponent<LocalizeStringEvent>();localizer.StringReference.TableReference="DeadZone";localizer.StringReference.TableEntryReference=ZoneL.Key(text.text);localizer.OnUpdateString.AddListener(value=>{if(text!=null)text.text=value;});localizer.RefreshString();
        }
    }
}
