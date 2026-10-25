using UnityEngine;
using UnityEngine.EventSystems;
namespace DeadZone
{
    public sealed class ZoneHeroCard : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        public bool Selected;public Color Accent;bool hovered;UnityEngine.UI.Image background;UnityEngine.UI.Outline border;
        public static Color HeroColor(int hero)=>hero==1?new Color(.96f,.59f,.8f):hero==2?new Color(.81f,.78f,.95f):hero==3?new Color(.98f,.46f,.5f):ZonePalette.Accent;
        void Start(){background=GetComponent<UnityEngine.UI.Image>();border=gameObject.AddComponent<UnityEngine.UI.Outline>();border.effectDistance=new Vector2(1,-1);}
        void Update()
        {
            float t=1-Mathf.Exp(-14*Time.unscaledDeltaTime);var color=Color.Lerp(ZonePalette.Graphite,Accent,Selected?.18f:hovered?.11f:.025f);background.color=Color.Lerp(background.color,color,t);
            border.effectColor=new Color(Accent.r,Accent.g,Accent.b,Selected?.85f:hovered?.55f:.15f);transform.localScale=Vector3.Lerp(transform.localScale,Vector3.one*(hovered?1.015f:Selected?1.005f:1),t);
        }
        public void OnPointerEnter(PointerEventData data){hovered=true;}public void OnPointerExit(PointerEventData data){hovered=false;}
    }
}
