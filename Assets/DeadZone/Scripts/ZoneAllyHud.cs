using TMPro;
using UnityEngine;
namespace DeadZone
{
    public sealed class ZoneAllyHud : MonoBehaviour
    {
        DeadZoneGame game;RectTransform root;
        readonly TextMeshProUGUI[] names=new TextMeshProUGUI[3],arrows=new TextMeshProUGUI[3];
        readonly RectTransform[] health=new RectTransform[3],revive=new RectTransform[3];
        public void Initialize(DeadZoneGame owner,Transform parent,TMP_FontAsset font)
        {
            game=owner;root=parent.GetComponent<RectTransform>();
            for(int i=0;i<3;i++)
            {
                names[i]=Text(parent,"Ally "+i,new Vector2(0,1),new Vector2(28,-88-i*70),new Vector2(380,45),font,20);
                health[i]=Bar(names[i].transform,"Health",new Vector2(0,-7),new Vector2(300,4),new Color(.35f,.9f,.76f));
                revive[i]=Bar(names[i].transform,"Revive",new Vector2(0,-14),new Vector2(300,4),new Color(1,.8f,.3f));
                arrows[i]=Text(parent,"Downed ally direction",new Vector2(.5f,.5f),Vector2.zero,new Vector2(240,55),font,20);arrows[i].alignment=TextAlignmentOptions.Center;
            }
        }
        void LateUpdate()
        {
            int index=0;
            foreach(var actor in game.players)
            {
                if(actor==game.player||index>=3)continue;
                bool connected=game.IsAuthority?game.network.IsParticipantConnected(actor.OwnerId):actor.Connected;
                var label=names[index];label.gameObject.SetActive(game.running);
                string state=!connected?ZoneL.Get("НЕТ СВЯЗИ"):actor.Dead?ZoneL.Get("НАБЛЮДАЕТ"):actor.Downed?ZoneL.Format("ПОДНЯТЬ · {0} с",Mathf.CeilToInt(actor.DownRemaining)):$"HP {Mathf.CeilToInt(actor.health)}/{actor.maxHealth}";
                label.text=game.HeroName(actor.CharacterIndex)+" · "+state;label.color=actor.Downed?new Color(1,.55f,.3f):Color.white;
                health[index].localScale=new Vector3(Mathf.Clamp01(actor.health/actor.maxHealth),1,1);
                revive[index].gameObject.SetActive(actor.Downed);revive[index].localScale=new Vector3(actor.ReviveProgress,1,1);
                var view=game.gameCamera.WorldToViewportPoint(actor.transform.position);bool off=view.x<.05f||view.x>.95f||view.y<.07f||view.y>.93f;
                var arrow=arrows[index];arrow.gameObject.SetActive(game.running&&connected&&actor.Downed&&off);
                if(arrow.gameObject.activeSelf)
                {
                    Vector2 dir=new Vector2(view.x-.5f,view.y-.5f);float extent=Mathf.Max(Mathf.Abs(dir.x)/.40f,Mathf.Abs(dir.y)/.35f);dir/=Mathf.Max(1,extent);
                    arrow.rectTransform.anchoredPosition=Vector2.Scale(dir,root.rect.size);
                    arrow.text="SOS · "+game.HeroName(actor.CharacterIndex)+"\n"+Mathf.CeilToInt(Vector2.Distance(actor.transform.position,game.player.transform.position))+" м";arrow.color=new Color(1,.6f,.3f);
                }
                index++;
            }
            for(;index<3;index++){names[index].gameObject.SetActive(false);arrows[index].gameObject.SetActive(false);}
        }
        static RectTransform Bar(Transform parent,string name,Vector2 point,Vector2 size,Color color)
        {
            var obj=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));obj.transform.SetParent(parent,false);var rect=obj.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,0);rect.anchoredPosition=point;rect.sizeDelta=size;obj.GetComponent<UnityEngine.UI.Image>().color=color;obj.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;return rect;
        }
        static TextMeshProUGUI Text(Transform parent,string name,Vector2 anchor,Vector2 point,Vector2 size,TMP_FontAsset font,int fontSize)
        {
            var obj=new GameObject(name,typeof(RectTransform));obj.transform.SetParent(parent,false);var rect=obj.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=anchor;rect.anchoredPosition=point;rect.sizeDelta=size;var text=obj.AddComponent<TextMeshProUGUI>();text.font=font;text.fontSize=fontSize;text.raycastTarget=false;return text;
        }
    }
}
