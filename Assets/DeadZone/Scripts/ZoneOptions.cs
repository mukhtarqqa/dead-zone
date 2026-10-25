using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadZone
{
    public sealed class ZoneOptions : MonoBehaviour
    {
        DeadZoneGame game;TMP_FontAsset font;GameObject root,card,controlsCard;bool pausedBefore;
        TextMeshProUGUI shakeLabel;
        public void Initialize(DeadZoneGame owner,Transform canvas,TMP_FontAsset typeface)
        {
            game=owner;font=typeface;
            root=Panel(canvas,"Options",Vector2.zero,Vector2.zero,Vector2.zero,new Color(.015f,.025f,.04f,.96f));
            var stretch=root.GetComponent<RectTransform>();stretch.anchorMin=Vector2.zero;stretch.anchorMax=Vector2.one;stretch.offsetMin=stretch.offsetMax=Vector2.zero;
            card=Panel(root.transform,"Options card",new Vector2(.5f,.5f),Vector2.zero,new Vector2(820,790),new Color(.045f,.075f,.105f));
            Text(card.transform,"Title",new Vector2(0,1),new Vector2(45,-35),new Vector2(720,65),40).text="НАСТРОЙКИ";
            Text(card.transform,"Volume",new Vector2(0,1),new Vector2(45,-100),new Vector2(720,40),24).text="Громкость эффектов";
            var track=Panel(card.transform,"Volume slider",new Vector2(0,1),new Vector2(45,-155),new Vector2(720,18),new Color(.12f,.2f,.25f));
            var handle=Panel(track.transform,"Handle",new Vector2(.5f,.5f),Vector2.zero,new Vector2(22,34),ZonePalette.Accent);
            var slider=track.AddComponent<UnityEngine.UI.Slider>();slider.minValue=0;slider.maxValue=1;slider.handleRect=handle.GetComponent<RectTransform>();slider.targetGraphic=handle.GetComponent<UnityEngine.UI.Image>();slider.value=ZoneFeedback.Volume;slider.onValueChanged.AddListener(v=>ZoneFeedback.Volume=v);
            Text(card.transform,"Music",new Vector2(0,1),new Vector2(45,-210),new Vector2(720,40),24).text="Громкость музыки";
            var musicTrack=Panel(card.transform,"Music slider",new Vector2(0,1),new Vector2(45,-265),new Vector2(720,18),new Color(.12f,.2f,.25f));
            var musicHandle=Panel(musicTrack.transform,"Handle",new Vector2(.5f,.5f),Vector2.zero,new Vector2(22,34),ZonePalette.Accent);
            var musicSlider=musicTrack.AddComponent<UnityEngine.UI.Slider>();musicSlider.minValue=0;musicSlider.maxValue=1;musicSlider.handleRect=musicHandle.GetComponent<RectTransform>();musicSlider.targetGraphic=musicHandle.GetComponent<UnityEngine.UI.Image>();musicSlider.value=ZoneMusic.Volume;musicSlider.onValueChanged.AddListener(v=>ZoneMusic.Volume=v);
            Button(card.transform,"Русский",new Vector2(0,1),new Vector2(45,-390),new Vector2(350,55),()=>ZoneL.Select("ru"));
            Button(card.transform,"Қазақша",new Vector2(0,1),new Vector2(415,-390),new Vector2(350,55),()=>ZoneL.Select("kk"));
            var shake=Button(card.transform,"",new Vector2(0,1),new Vector2(45,-315),new Vector2(720,55),()=>{ZoneFeedback.Shake=!ZoneFeedback.Shake;UpdateShake();});shakeLabel=shake.GetComponentInChildren<TextMeshProUGUI>();UpdateShake();
            Button(card.transform,"Продолжить",new Vector2(0,1),new Vector2(45,-475),new Vector2(350,60),Close);
            Button(card.transform,"В меню",new Vector2(0,1),new Vector2(415,-475),new Vector2(350,60),()=>{Close();if(game.network.Active)game.network.Leave();else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);});
            Button(card.transform,"Выйти из игры",new Vector2(0,1),new Vector2(45,-560),new Vector2(720,60),()=>{
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying=false;
#else
                Application.Quit();
#endif
            });
            Text(card.transform,"Hint",new Vector2(0,1),new Vector2(45,-650),new Vector2(720,55),18).text="Esc — закрыть · В кооперативе бой продолжается";
            controlsCard=Panel(root.transform,"Controls help",new Vector2(.5f,.5f),Vector2.zero,new Vector2(820,700),ZoneUi.PanelColor);
            Text(controlsCard.transform,"Title",new Vector2(0,1),new Vector2(45,-30),new Vector2(730,55),36).text="УПРАВЛЕНИЕ";
            Text(controlsCard.transform,"Bindings",new Vector2(0,1),new Vector2(45,-112),new Vector2(730,430),22).text=ControlsText;
            Text(controlsCard.transform,"Pause hint",new Vector2(0,1),new Vector2(45,-555),new Vector2(730,45),18).text="Esc — закрыть · В кооперативе бой продолжается";
            Button(controlsCard.transform,"Назад",new Vector2(0,1),new Vector2(45,-615),new Vector2(730,55),Close);controlsCard.SetActive(false);
            var menu=canvas.Find("CharacterSelect");if(menu!=null)Button(menu,"Настройки",Vector2.zero,new Vector2(75,12),new Vector2(250,42),Show);
            var hud=canvas.Find("GameHUD");if(hud!=null){Button(hud,"Настройки",new Vector2(0,1),new Vector2(28,-24),new Vector2(220,42),Show);Button(hud,"F1",new Vector2(0,1),new Vector2(260,-24),new Vector2(58,42),ShowControls);}
            root.SetActive(false);
        }
        void UpdateShake(){shakeLabel.text=ZoneL.Get(ZoneFeedback.Shake?"Тряска камеры: ВКЛ":"Тряска камеры: ВЫКЛ");}
        public void Show(){if(!root.activeSelf)pausedBefore=game.paused;card.SetActive(true);controlsCard.SetActive(false);game.OptionsVisible=true;if(game.running&&!game.network.Active){game.paused=true;Time.timeScale=0;}root.transform.SetAsLastSibling();root.SetActive(true);Cursor.visible=true;}
        public const string ControlsText="WASD — движение\nМышь — прицеливание · ЛКМ — огонь\nR — перезарядка · Tab — смена оружия\nShift — рывок с короткой неуязвимостью\nQ — активная способность\nG — граната · F — мина\nE — подобрать / открыть тайник\nУдерживать E — поднять союзника\n\nОружие: два слота, замена оружия в руках.\nСпособность заряжается за урон и убийства.";
        public void ShowControls(){if(!root.activeSelf)Show();card.SetActive(false);controlsCard.SetActive(true);}
        public void Close(){game.OptionsVisible=false;root.SetActive(false);if(!game.network.Active){game.paused=pausedBefore;Time.timeScale=game.paused?0:1;}PlayerPrefs.Save();}
        void Update(){if(Application.isFocused&&Input.GetKeyDown(KeyCode.F1)&&game.running){if(root.activeSelf&&controlsCard.activeSelf)Close();else if(!game.InputBlocked)ShowControls();}if(shakeLabel!=null)UpdateShake();if(root!=null&&Application.isFocused&&Input.GetKeyDown(KeyCode.Escape)){if(root.activeSelf)Close();else if(game.running&&!game.network.Active)Show();}}
        GameObject Panel(Transform parent,string name,Vector2 anchor,Vector2 pos,Vector2 size,Color color)
        {
            var obj=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));obj.transform.SetParent(parent,false);var rect=obj.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=anchor;rect.anchoredPosition=pos;rect.sizeDelta=size;obj.GetComponent<UnityEngine.UI.Image>().color=ZonePalette.Surface(color);ZoneQuarantineFrame.Attach(obj,size);return obj;
        }
        UnityEngine.UI.Button Button(Transform parent,string label,Vector2 anchor,Vector2 pos,Vector2 size,UnityEngine.Events.UnityAction action)
        {
            var obj=Panel(parent,label,anchor,pos,size,ZonePalette.Button);var button=obj.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=obj.GetComponent<UnityEngine.UI.Image>();button.onClick.AddListener(()=>{ZoneFeedback.Play(7);action();});var text=Text(obj.transform,"Label",new Vector2(.5f,.5f),Vector2.zero,size-Vector2.one*8,22);text.alignment=TextAlignmentOptions.Center;text.text=label;return button;
        }
        TextMeshProUGUI Text(Transform parent,string name,Vector2 anchor,Vector2 pos,Vector2 size,int fontSize)
        {
            var obj=new GameObject(name,typeof(RectTransform));obj.transform.SetParent(parent,false);var rect=obj.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=anchor;rect.anchoredPosition=pos;rect.sizeDelta=size;var text=obj.AddComponent<TextMeshProUGUI>();text.font=font;text.fontSize=fontSize;text.color=ZonePalette.Text;text.raycastTarget=false;ZoneL.Attach(text);return text;
        }
    }
}
