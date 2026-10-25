using TMPro;
using UnityEngine;
namespace DeadZone
{
    public sealed class ZoneRecoveryHud : MonoBehaviour
    {
        DeadZoneGame game;GameObject root,previous,next;TextMeshProUGUI title,detail;RectTransform progress;
        public void Initialize(DeadZoneGame owner,Transform parent,TMP_FontAsset font)
        {
            game=owner;root=ZoneUi.Panel(parent,"Recovery and observation",new Vector2(.5f,0),new Vector2(0,185),new Vector2(620,100)).gameObject;
            title=ZoneUi.Text(root.transform,"State",new Vector2(.5f,1),new Vector2(0,-10),new Vector2(500,32),font,22);title.alignment=TextAlignmentOptions.Center;title.color=ZonePalette.Accent;
            detail=ZoneUi.Text(root.transform,"Help",new Vector2(.5f,.5f),new Vector2(0,-12),new Vector2(580,40),font,16);detail.alignment=TextAlignmentOptions.Center;detail.color=ZonePalette.Muted;
            progress=ZoneUi.Panel(root.transform,"Revive progress",Vector2.zero,new Vector2(20,12),new Vector2(580,4),ZonePalette.Accent).rectTransform;
            GameObject Arrow(string text,float x,int direction){var image=ZoneUi.Panel(root.transform,"Observe "+direction,new Vector2(x>0?0:1,1),new Vector2(x,-8),new Vector2(40,34),ZonePalette.Button);image.raycastTarget=true;var button=image.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=image;button.onClick.AddListener(()=>game.CycleSpectator(direction));var label=ZoneUi.Text(image.transform,"Arrow",new Vector2(.5f,.5f),Vector2.zero,new Vector2(35,30),font,24);label.text=text;label.alignment=TextAlignmentOptions.Center;return image.gameObject;}
            previous=Arrow("<",8,-1);next=Arrow(">",-8,1);root.SetActive(false);
        }
        void LateUpdate()
        {
            var p=game.player;bool show=game.running&&(p.Dead||p.Downed);root.SetActive(show);if(!show)return;
            title.text=p.Downed?ZoneL.Format("ВЫ РАНЕНЫ · {0} с",Mathf.CeilToInt(p.DownRemaining)):ZoneL.Format("НАБЛЮДЕНИЕ · {0}",game.HeroName(game.CameraActor.CharacterIndex));
            detail.text=ZoneL.Get(p.Downed?"Союзник может поднять вас, удерживая E":"Возвращение в начале следующей волны · ← / → сменить игрока");previous.SetActive(p.Dead);next.SetActive(p.Dead);progress.gameObject.SetActive(p.Downed);progress.localScale=new Vector3(p.ReviveProgress,1,1);
        }
    }
}
