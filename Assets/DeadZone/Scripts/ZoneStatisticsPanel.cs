using TMPro;
using UnityEngine;
namespace DeadZone
{
    public sealed class ZoneStatisticsPanel : MonoBehaviour
    {
        DeadZoneGame game;GameObject root;TextMeshProUGUI details;int hero;
        public void Initialize(DeadZoneGame owner,Transform canvas,TMP_FontAsset font)
        {
            game=owner;root=Panel(canvas,"Statistics",Vector2.zero,Vector2.zero,Vector2.zero);var r=root.GetComponent<RectTransform>();r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
            var card=Panel(root.transform,"Statistics card",new Vector2(.5f,.5f),Vector2.zero,new Vector2(1150,700));
            Label(card.transform,"СТАТИСТИКА",new Vector2(40,-25),new Vector2(1060,65),font,38);
            for(int i=0;i<game.HeroCount;i++){int index=i;Button(card.transform,game.HeroName(i),new Vector2(40+i*270,-115),new Vector2(250,55),font,()=>hero=index);}
            details=Label(card.transform,"",new Vector2(40,-205),new Vector2(1060,390),font,28);
            Button(card.transform,"Назад",new Vector2(40,-605),new Vector2(1060,55),font,()=>{root.SetActive(false);game.OptionsVisible=false;});
            var menu=canvas.Find("CharacterSelect");Button(menu,"Статистика",new Vector2(350,54),new Vector2(250,42),font,Show,true);
            root.SetActive(false);
        }
        public void Show(){hero=game.selected;root.transform.SetAsLastSibling();root.SetActive(true);game.OptionsVisible=true;}
        void Update()
        {
            if(!root.activeSelf)return;
            var s=ZoneProfile.Read(hero);string best=s.bestWinSeconds<=0?"—":$"{Mathf.FloorToInt(s.bestWinSeconds/60):00}:{Mathf.FloorToInt(s.bestWinSeconds%60):00}";
            details.text=game.HeroName(hero)+"\n\n"+ZoneL.Format("Матчи: {0}\nПобеды: {1}\nУбийства: {2}\nУрон: {3}\nПоднято союзников: {4}\nВремя в игре: {5} мин\nЛучшее прохождение: {6}",s.matches,s.wins,s.kills,Mathf.RoundToInt(s.damage),s.revives,Mathf.FloorToInt(s.seconds/60),best);
            if(Input.GetKeyDown(KeyCode.Escape)){root.SetActive(false);game.OptionsVisible=false;}
        }
        static GameObject Panel(Transform parent,string name,Vector2 anchor,Vector2 position,Vector2 size)
        {
            var obj=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));obj.transform.SetParent(parent,false);var r=obj.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=anchor;r.anchoredPosition=position;r.sizeDelta=size;obj.GetComponent<UnityEngine.UI.Image>().color=ZonePalette.Graphite;ZoneQuarantineFrame.Attach(obj,size);return obj;
        }
        static TextMeshProUGUI Label(Transform parent,string value,Vector2 position,Vector2 size,TMP_FontAsset font,int fontSize)
        {
            var obj=new GameObject("Label",typeof(RectTransform));obj.transform.SetParent(parent,false);var r=obj.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=position;r.sizeDelta=size;var text=obj.AddComponent<TextMeshProUGUI>();text.font=font;text.fontSize=fontSize;text.color=ZonePalette.Text;text.raycastTarget=false;text.text=value;ZoneL.Attach(text);return text;
        }
        static void Button(Transform parent,string label,Vector2 position,Vector2 size,TMP_FontAsset font,UnityEngine.Events.UnityAction action,bool bottom=false)
        {
            var obj=Panel(parent,label,new Vector2(0,1),position,size);if(bottom){var r=obj.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=Vector2.zero;r.anchoredPosition=new Vector2(position.x,12);}
            obj.GetComponent<UnityEngine.UI.Image>().color=ZonePalette.Button;var button=obj.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=obj.GetComponent<UnityEngine.UI.Image>();button.onClick.AddListener(action);
            var text=Label(obj.transform,label,Vector2.zero,size,font,22);text.alignment=TextAlignmentOptions.Center;
        }
    }
}
