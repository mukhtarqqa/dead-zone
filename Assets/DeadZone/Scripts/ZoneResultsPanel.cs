using TMPro;
using UnityEngine;
namespace DeadZone
{
    public sealed class ZoneResultsPanel : MonoBehaviour
    {
        DeadZoneGame game;GameObject root;CanvasGroup fade;float shownAt,nextRefresh;bool shown;string language;
        TextMeshProUGUI brand,title,subtitle,heroName,role,wave,retryLabel,footer,menuLabel,optionsLabel;UnityEngine.UI.Image portrait;UnityEngine.UI.Button retry;
        readonly TextMeshProUGUI[] statNames=new TextMeshProUGUI[4],statValues=new TextMeshProUGUI[4];readonly TextMeshProUGUI[,] team=new TextMeshProUGUI[5,4];
        public void Initialize(DeadZoneGame owner,Transform canvas,TMP_FontAsset font)
        {
            game=owner;var shade=ZoneUi.Panel(canvas,"Run results",Vector2.zero,Vector2.zero,Vector2.zero,new Color(.015f,.022f,.018f,.96f));root=shade.gameObject;shade.raycastTarget=true;var stretch=shade.rectTransform;stretch.anchorMin=Vector2.zero;stretch.anchorMax=Vector2.one;stretch.offsetMin=stretch.offsetMax=Vector2.zero;fade=root.AddComponent<CanvasGroup>();
            var card=ZoneUi.Panel(root.transform,"Results content",new Vector2(.5f,.5f),Vector2.zero,new Vector2(1400,760),ZonePalette.Graphite);
            ZoneUi.Panel(card.transform,"Outcome accent",new Vector2(0,1),Vector2.zero,new Vector2(1400,4),ZonePalette.Accent);
            brand=ZoneUi.Text(card.transform,"Brand",new Vector2(0,1),new Vector2(40,-24),new Vector2(1320,30),font,16);brand.text="DEAD ZONE";brand.color=ZonePalette.Muted;
            title=ZoneUi.Text(card.transform,"Outcome",new Vector2(0,1),new Vector2(40,-70),new Vector2(1320,76),font,56);
            subtitle=ZoneUi.Text(card.transform,"Outcome detail",new Vector2(0,1),new Vector2(43,-160),new Vector2(1300,38),font,20);subtitle.color=ZonePalette.Muted;
            var hero=ZoneUi.Panel(card.transform,"Hero summary",new Vector2(0,1),new Vector2(40,-222),new Vector2(310,360),ZonePalette.Panel);
            portrait=ZoneUi.Panel(hero.transform,"Portrait",new Vector2(.5f,1),new Vector2(0,-20),new Vector2(128,180),Color.white);portrait.preserveAspect=true;
            heroName=ZoneUi.Text(hero.transform,"Name",new Vector2(.5f,0),new Vector2(0,116),new Vector2(286,40),font,23);heroName.alignment=TextAlignmentOptions.Center;
            role=ZoneUi.Text(hero.transform,"Role",new Vector2(.5f,0),new Vector2(0,76),new Vector2(286,30),font,15);role.alignment=TextAlignmentOptions.Center;role.color=ZonePalette.Muted;
            wave=ZoneUi.Text(hero.transform,"Wave reached",new Vector2(.5f,0),new Vector2(0,20),new Vector2(286,32),font,19);wave.alignment=TextAlignmentOptions.Center;
            for(int i=0;i<4;i++)
            {
                var tile=ZoneUi.Panel(card.transform,"Metric "+i,new Vector2(0,1),new Vector2(385+i%2*480,-222-i/2*112),new Vector2(455,94),ZonePalette.Panel);
                statNames[i]=ZoneUi.Text(tile.transform,"Label",new Vector2(0,1),new Vector2(20,-12),new Vector2(415,24),font,14);statNames[i].color=ZonePalette.Muted;statValues[i]=ZoneUi.Text(tile.transform,"Value",Vector2.zero,new Vector2(20,10),new Vector2(415,46),font,34);
            }
            float[] widths={340,130,220,245};float x=385;
            for(int column=0;column<4;column++){for(int row=0;row<5;row++){team[row,column]=ZoneUi.Text(card.transform,"Team "+row+":"+column,new Vector2(0,1),new Vector2(x,-458-row*28),new Vector2(widths[column]-12,27),font,row==0?13:17);team[row,column].color=row==0?ZonePalette.Muted:ZonePalette.Text;}x+=widths[column];}
            retry=Button(card.transform,"Play again",new Vector2(1,0),new Vector2(-40,55),new Vector2(350,64),ZonePalette.Accent,font,game.RestartRun,out retryLabel);retryLabel.color=new Color(.035f,.055f,.025f);
            Button(card.transform,"Menu",new Vector2(1,0),new Vector2(-414,55),new Vector2(260,64),ZonePalette.Button,font,game.ReturnToMenu,out menuLabel);
            Button(card.transform,"Options",Vector2.zero,new Vector2(40,55),new Vector2(220,64),ZonePalette.Panel,font,()=>game.GetComponent<ZoneOptions>().Show(),out optionsLabel);
            footer=ZoneUi.Text(card.transform,"Shortcuts",new Vector2(.5f,0),new Vector2(0,13),new Vector2(1300,26),font,14);footer.alignment=TextAlignmentOptions.Center;footer.color=ZonePalette.Muted;root.SetActive(false);
        }
        void Update()
        {
            if(!game.ended||game.FinalePlaying){if(shown){root.SetActive(false);shown=false;}return;}
            if(!shown){shown=true;shownAt=Time.unscaledTime;root.transform.SetAsLastSibling();root.SetActive(true);language="";}
            fade.alpha=Mathf.SmoothStep(0,1,(Time.unscaledTime-shownAt)/.45f);fade.interactable=fade.alpha>.8f;
            string locale=PlayerPrefs.GetString("dz.language","ru");if(locale!=language||Time.unscaledTime>=nextRefresh){language=locale;nextRefresh=Time.unscaledTime+.5f;Refresh();}
            retry.interactable=game.IsAuthority&&!game.Restarting;retryLabel.text=ZoneL.Get(game.Restarting?"ПОДГОТОВКА...":game.IsAuthority?"ИГРАТЬ СНОВА":"ОЖИДАНИЕ ВЕДУЩЕГО");
            if(Application.isFocused&&!game.OptionsVisible&&Time.unscaledTime-shownAt>.5f&&Input.GetKeyDown(KeyCode.Escape))game.ReturnToMenu();
        }
        void Refresh()
        {
            brand.text="DEAD ZONE  /  "+ZoneL.Get("ИТОГИ ВЫЛАЗКИ");title.text=ZoneL.Get(game.Victory?"ЗОНА ЗАЧИЩЕНА":"ПОРАЖЕНИЕ");title.color=game.Victory?ZonePalette.Accent:ZonePalette.Crimson;subtitle.text=game.Victory?ZoneL.Get("Пять боссов уничтожены. Команда эвакуирована."):ZoneL.Format("Волна {0}/5 · Соберите команду и попробуйте снова.",game.wave);
            var actor=game.player;portrait.sprite=ZoneActorArt.Frame(game,actor.CharacterIndex,Vector2.down,false);heroName.text=game.HeroName(actor.CharacterIndex);role.text=ZoneL.Get(actor.CharacterIndex==0?"ВЫЖИВАНИЕ":actor.CharacterIndex==1?"БЛИЖНИЙ БОЙ":actor.CharacterIndex==2?"КОНТРОЛЬ":"ПРОКЛЯТИЕ");wave.text=ZoneL.Format("ВОЛНА {0:00} / 05",game.wave);
            string[] labels={"УБИЙСТВА","НАНЕСЁННЫЙ УРОН","ПОДНЯТЫЕ СОЮЗНИКИ","ВРЕМЯ ВЫЛАЗКИ"};string[] values={actor.PersonalKills.ToString(),Mathf.RoundToInt(actor.PersonalDamage).ToString(),actor.Revives.ToString(),$"{Mathf.FloorToInt(game.Elapsed/60):00}:{Mathf.FloorToInt(game.Elapsed%60):00}"};
            for(int i=0;i<4;i++){statNames[i].text=ZoneL.Get(labels[i]);statValues[i].text=values[i];}
            string[] headings={"КОМАНДА","УБИЙСТВА","УРОН","ПОМОЩЬ"};for(int c=0;c<4;c++)team[0,c].text=ZoneL.Get(headings[c]);
            for(int row=1;row<=4;row++)for(int c=0;c<4;c++)team[row,c].text="";
            for(int i=0;i<game.players.Count&&i<4;i++){var p=game.players[i];team[i+1,0].text=game.HeroName(p.CharacterIndex);team[i+1,1].text=p.PersonalKills.ToString();team[i+1,2].text=Mathf.RoundToInt(p.PersonalDamage).ToString();team[i+1,3].text=p.Revives.ToString();for(int c=0;c<4;c++)team[i+1,c].color=p==actor?ZonePalette.Accent:ZonePalette.Text;}
            menuLabel.text=ZoneL.Get("В МЕНЮ");optionsLabel.text=ZoneL.Get("НАСТРОЙКИ");footer.text=ZoneL.Get(game.IsAuthority?"Enter — ещё раз · Esc — в меню":"Повторную вылазку запускает ведущий · Esc — выйти");
        }
        static UnityEngine.UI.Button Button(Transform parent,string name,Vector2 anchor,Vector2 position,Vector2 size,Color color,TMP_FontAsset font,UnityEngine.Events.UnityAction action,out TextMeshProUGUI label)
        {
            var image=ZoneUi.Panel(parent,name,anchor,position,size,color);image.raycastTarget=true;var button=image.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=image;button.onClick.AddListener(()=>{ZoneFeedback.Play(7);action();});label=ZoneUi.Text(image.transform,"Label",new Vector2(.5f,.5f),Vector2.zero,size-Vector2.one*12,font,21);label.alignment=TextAlignmentOptions.Center;return button;
        }
    }
}
