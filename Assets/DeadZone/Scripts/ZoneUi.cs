using TMPro;
using UnityEngine;
namespace DeadZone
{
    public static class ZoneUi
    {
        public static readonly Color PanelColor=ZonePalette.Graphite,Muted=ZonePalette.Muted,Mint=ZonePalette.Sage;
        public static RectTransform Rect(Transform parent,string name,Vector2 anchor,Vector2 position,Vector2 size)
        {
            var obj=new GameObject(name,typeof(RectTransform));obj.transform.SetParent(parent,false);var rect=obj.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=anchor;rect.anchoredPosition=position;rect.sizeDelta=size;return rect;
        }
        public static UnityEngine.UI.Image Panel(Transform parent,string name,Vector2 anchor,Vector2 position,Vector2 size,Color? color=null)
        {
            var rect=Rect(parent,name,anchor,position,size);var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=ZonePalette.Surface(color??PanelColor);image.raycastTarget=false;var border=rect.gameObject.AddComponent<UnityEngine.UI.Outline>();border.effectDistance=new Vector2(1,-1);border.effectColor=new Color(.33f,.41f,.3f,.35f);ZoneQuarantineFrame.Attach(rect.gameObject,size);return image;
        }
        public static TextMeshProUGUI Text(Transform parent,string name,Vector2 anchor,Vector2 position,Vector2 size,TMP_FontAsset font,float fontSize)
        {
            var rect=Rect(parent,name,anchor,position,size);var text=rect.gameObject.AddComponent<TextMeshProUGUI>();text.font=font;text.fontSize=fontSize;text.enableAutoSizing=false;text.color=ZonePalette.Text;text.raycastTarget=false;text.textWrappingMode=TextWrappingModes.Normal;return text;
        }
    }
}
