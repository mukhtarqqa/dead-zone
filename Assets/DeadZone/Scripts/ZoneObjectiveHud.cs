using TMPro;
using UnityEngine;

namespace DeadZone
{
    public sealed class ZoneObjectiveHud : MonoBehaviour
    {
        DeadZoneGame game;CanvasGroup gameHud;GameObject panel,cinema;
        TextMeshProUGUI heading,objective,counter,subtitle,direction;
        UnityEngine.UI.Image top,bottom,fade,notice;
        RectTransform hudRect;

        public void Initialize(DeadZoneGame owner,Transform parent,TMP_FontAsset font)
        {
            game=owner;hudRect=parent.GetComponent<RectTransform>();gameHud=parent.GetComponent<CanvasGroup>();
            if(gameHud==null)gameHud=parent.gameObject.AddComponent<CanvasGroup>();
            panel=ZoneUi.Panel(parent,"Mission objective",new Vector2(0,1),new Vector2(28,-20),new Vector2(330,58)).gameObject;
            heading=ZoneUi.Text(panel.transform,"Objective category",new Vector2(0,1),new Vector2(12,-4),new Vector2(305,17),font,12);heading.color=ZonePalette.Sage;
            objective=ZoneUi.Text(panel.transform,"Current objective",new Vector2(0,1),new Vector2(12,-25),new Vector2(236,28),font,19);
            counter=ZoneUi.Text(panel.transform,"Objective progress",new Vector2(1,1),new Vector2(-12,-24),new Vector2(52,30),font,22);counter.alignment=TextAlignmentOptions.Right;
            direction=ZoneUi.Text(parent,"Evacuation direction",new Vector2(.5f,.5f),Vector2.zero,new Vector2(300,65),font,20);direction.alignment=TextAlignmentOptions.Center;direction.color=ZonePalette.Sage;

            cinema=new GameObject("Extraction cinematic overlay",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler));
            cinema.transform.SetParent(parent.parent,false);var stretch=cinema.GetComponent<RectTransform>();stretch.anchorMin=Vector2.zero;stretch.anchorMax=Vector2.one;stretch.offsetMin=stretch.offsetMax=Vector2.zero;
            var canvas=cinema.GetComponent<Canvas>();canvas.overrideSorting=true;canvas.sortingOrder=200;
            top=Solid(cinema.transform,"Upper letterbox",new Vector2(0,1),new Vector2(1,1),new Vector2(0,68),Color.black);
            bottom=Solid(cinema.transform,"Lower letterbox",Vector2.zero,new Vector2(1,0),new Vector2(0,96),Color.black);
            notice=ZoneUi.Panel(cinema.transform,"Evacuation announcement",new Vector2(.5f,0),new Vector2(0,164),new Vector2(1050,68),ZonePalette.Graphite);
            subtitle=ZoneUi.Text(cinema.transform,"Extraction subtitle",new Vector2(.5f,0),new Vector2(0,23),new Vector2(1300,54),font,27);subtitle.alignment=TextAlignmentOptions.Center;
            fade=Solid(cinema.transform,"Departure fade",Vector2.zero,Vector2.one,Vector2.zero,Color.clear);
        }

        static UnityEngine.UI.Image Solid(Transform parent,string name,Vector2 min,Vector2 max,Vector2 size,Color color)
        {
            var obj=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));obj.transform.SetParent(parent,false);
            var rect=obj.GetComponent<RectTransform>();rect.anchorMin=min;rect.anchorMax=max;rect.pivot=min;rect.anchoredPosition=Vector2.zero;rect.sizeDelta=size;
            var image=obj.GetComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=false;return image;
        }

        void LateUpdate()
        {
            if(game==null||game.player==null)return;
            bool extracting=game.Extracting;var phase=game.ExtractionPhase;float age=game.ExtractionAge;
            heading.text=ZoneL.Get(extracting?"ЭВАКУАЦИЯ · БОССЫ 5/5":"ЦЕЛЬ");
            objective.text=ZoneL.Get(!extracting?"Уничтожьте 5 боссов":phase==ZoneExtractionPhase.Arriving?"Дождитесь посадки":phase==ZoneExtractionPhase.Boarding?"Сядьте в вертолёт":"Команда эвакуирована");
            counter.text=extracting?$"{game.ExtractionSeated}/{game.ExtractionCrew}":$"{game.BossesDefeated}/{DeadZoneGame.BossGoal}";
            panel.SetActive(game.running);counter.color=extracting||game.BossesDefeated>0?ZonePalette.Sage:ZonePalette.Text;
            bool cinematic=game.ExtractionCinematic;
            gameHud.alpha=cinematic?0:1;
            top.gameObject.SetActive(cinematic);bottom.gameObject.SetActive(cinematic);
            subtitle.gameObject.SetActive(cinematic||game.running&&phase==ZoneExtractionPhase.Boarding&&(age<5||game.player.Boarded));
            notice.gameObject.SetActive(subtitle.gameObject.activeSelf&&!cinematic);
            subtitle.rectTransform.anchoredPosition=new Vector2(0,cinematic?23:175);
            subtitle.text=ZoneL.Get(phase==ZoneExtractionPhase.Arriving?"Вертолёт прибыл. Направляйтесь к точке эвакуации.":"Все на борту. Покидаем Dead Zone.");
            if(phase==ZoneExtractionPhase.Boarding)subtitle.text=game.player.Boarded?ZoneL.Format("Ждём остальных: {0}/{1} на борту.",game.ExtractionSeated,game.ExtractionCrew):ZoneL.Get("Вертолёт прибыл");
            float alpha=phase==ZoneExtractionPhase.Departing?Mathf.SmoothStep(0,1,Mathf.InverseLerp(DeadZoneGame.HelicopterDepartureSeconds-.85f,DeadZoneGame.HelicopterDepartureSeconds,age)):
                phase==ZoneExtractionPhase.Complete?1-Mathf.SmoothStep(0,1,age/.7f):0;
            fade.color=new Color(.015f,.022f,.018f,alpha);fade.gameObject.SetActive(alpha>.001f);
            PresentDirection();
        }

        void PresentDirection()
        {
            bool show=game.running&&game.ExtractionPhase==ZoneExtractionPhase.Boarding&&!game.player.Boarded&&!game.OptionsVisible&&!game.paused;
            if(!show){direction.gameObject.SetActive(false);return;}
            Vector3 point=game.gameCamera.WorldToViewportPoint(game.ExtractionLanding);
            bool outside=point.x<.13f||point.x>.87f||point.y<.16f||point.y>.82f;
            direction.gameObject.SetActive(outside);if(!outside)return;
            Vector2 delta=new Vector2(point.x-.5f,point.y-.5f);float ratio=Mathf.Max(Mathf.Abs(delta.x)/.33f,Mathf.Abs(delta.y)/.30f);
            delta/=Mathf.Max(1,ratio);direction.rectTransform.anchoredPosition=Vector2.Scale(delta,hudRect.rect.size);
            string arrow=Mathf.Abs(delta.x)>Mathf.Abs(delta.y)?delta.x>0?"→":"←":delta.y>0?"↑":"↓";
            direction.text=arrow+"  "+ZoneL.Format("На борту {0}/{1} · {2} м",game.ExtractionSeated,game.ExtractionCrew,Mathf.CeilToInt(Vector2.Distance(game.player.transform.position,game.ExtractionLanding)));
        }

        void OnDestroy(){if(gameHud!=null)gameHud.alpha=1;if(cinema!=null)Destroy(cinema);}
    }
}
