using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace DeadZone
{
    public sealed class ZoneIntro : MonoBehaviour
    {
        public DeadZoneGame Game;
        public bool Playing {get;private set;}
        public bool ShowingGame=>handed;
        public float Clock {get;private set;}
        public int BeatIndex {get;private set;}=-1;
        ZoneIntroDefinition art;GameObject root;RectTransform stage;Image background,black;
        readonly Image[] actors=new Image[4];readonly Image[] zombies=new Image[14];
        TextMeshProUGUI subtitle,chapter;AudioSource audioSource;Action completed;
        bool finishing,handed;float fade;int lastScene=-1;bool cursor;
        public bool Play(Action onComplete)
        {
            if(Playing)return false;art=Resources.Load<ZoneIntroDefinition>("Intro/IntroCutscene");
            if(art==null||!art.Ready){Debug.LogWarning("Intro art not prepared: run IntroArtBuilder.Build");return false;}
            completed=onComplete;Playing=true;Clock=0;BeatIndex=-1;lastScene=-1;finishing=handed=false;fade=0;cursor=Cursor.visible;Cursor.visible=true;
            root=new GameObject("Intro cinematic",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=1000;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
            stage=Rect(root.transform,"Stage",Vector2.zero,new Vector2(1600,900));
            background=Picture(stage,"Ruined street");background.rectTransform.sizeDelta=new Vector2(1700,970);
            for(int i=0;i<zombies.Length;i++)zombies[i]=Picture(stage,"Zombie "+i);
            for(int i=0;i<4;i++)actors[i]=Picture(stage,ZoneIntroScript.Speakers[i]);
            Bar(new Vector2(0,410),new Vector2(1700,90));Bar(new Vector2(0,-355),new Vector2(1700,190));
            chapter=Text(root.transform,"Chapter",new Vector2(-150,400),new Vector2(1100,45),22);chapter.alignment=TextAlignmentOptions.Left;
            subtitle=Text(root.transform,"Dialogue",new Vector2(0,-350),new Vector2(1360,140),30);subtitle.alignment=TextAlignmentOptions.Center;subtitle.enableAutoSizing=true;subtitle.fontSizeMin=24;subtitle.fontSizeMax=30;
            black=Picture(root.transform,"Fade");black.color=Color.black;black.rectTransform.anchorMin=Vector2.zero;black.rectTransform.anchorMax=Vector2.one;black.rectTransform.sizeDelta=Vector2.zero;
            var skip=Picture(root.transform,"Skip");skip.rectTransform.anchoredPosition=new Vector2(650,400);skip.rectTransform.sizeDelta=new Vector2(250,50);skip.color=new Color(.05f,.07f,.08f,.9f);skip.raycastTarget=true;
            var button=skip.gameObject.AddComponent<Button>();button.onClick.AddListener(Skip);
            var label=Text(skip.transform,"Label",Vector2.zero,new Vector2(245,45),21);label.text="Пропустить · Esc";label.alignment=TextAlignmentOptions.Center;
            audioSource=root.AddComponent<AudioSource>();audioSource.playOnAwake=false;audioSource.spatialBlend=0;
            Present(0);return true;
        }
        static RectTransform Rect(Transform parent,string name,Vector2 at,Vector2 size)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=at;r.sizeDelta=size;return r;}
        static Image Picture(Transform p,string name){var im=Rect(p,name,Vector2.zero,Vector2.zero).gameObject.AddComponent<Image>();im.raycastTarget=false;im.preserveAspect=true;return im;}
        static TextMeshProUGUI Text(Transform p,string name,Vector2 at,Vector2 size,int fontSize)
        {var t=Rect(p,name,at,size).gameObject.AddComponent<TextMeshProUGUI>();t.font=TMP_Settings.defaultFontAsset;t.fontSize=fontSize;t.color=Color.white;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;return t;}
        void Bar(Vector2 at,Vector2 size){var im=Picture(root.transform,"Letterbox");im.rectTransform.anchoredPosition=at;im.rectTransform.sizeDelta=size;im.color=new Color(.015f,.02f,.025f,.94f);}
        public void Skip(){if(Playing&&!finishing){finishing=true;fade=0;}}
        public void Seek(float time){if(!Playing||finishing)return;Clock=Mathf.Clamp(time,0,ZoneIntroScript.Duration);Present(Clock);}
        void Update()
        {
            if(!Playing)return;
            if(Input.GetKeyDown(KeyCode.Escape))Skip();
            if(finishing)
            {
                fade+=Time.unscaledDeltaTime;black.color=new Color(0,0,0,handed?1-Mathf.Clamp01((fade-.35f)/.35f):Mathf.Clamp01(fade/.35f));
                if(fade>=.35f&&!handed)
                {
                    handed=true;var done=completed;completed=null;
                    foreach(Transform child in root.transform)if(child!=black.transform)child.gameObject.SetActive(false);
                    audioSource.Stop();done?.Invoke();
                }
                if(fade>=.7f){Playing=false;Destroy(root);root=null;Cursor.visible=cursor;}
                return;
            }
            Clock+=Time.unscaledDeltaTime;Present(Clock);if(Clock>=ZoneIntroScript.Duration)Skip();
        }
        void Present(float clock)
        {
            int index=ZoneIntroScript.At(clock,out float local);var beat=ZoneIntroScript.Beats[index];
            if(index!=BeatIndex)
            {
                BeatIndex=index;subtitle.text=beat.speaker<0?"":"<color=#AFC6D0><b>"+ZoneIntroScript.Speakers[beat.speaker]+"</b></color>\n"+beat.text;
                chapter.text=ZoneIntroScript.Chapters[beat.scene];
                if(lastScene!=beat.scene){lastScene=beat.scene;}
                if(beat.cue>0){var clip=Resources.Load<AudioClip>(beat.cue==1?"Audio/sfx_mine_arm":beat.cue>=3?"Audio/sfx_grenade_pin":"Audio/sfx_heavy_reload");if(clip!=null)audioSource.PlayOneShot(clip,.25f);}
            }
            background.sprite=art.backgrounds[Mathf.Min(beat.scene,2)];
            float progress=local/beat.duration;
            float guardAge=PhaseAge(clock,3,3);
            float guard=Mathf.SmoothStep(0,1,Mathf.Clamp01(guardAge/.7f));
            float zoom=beat.stage==5?Mathf.Lerp(1.12f,1,progress):1.04f+Mathf.Sin(clock*.04f)*.025f;
            background.rectTransform.localScale=Vector3.one*zoom;
            background.rectTransform.anchoredPosition=new Vector2(Mathf.Sin(clock*.07f)*25,0);
            for(int i=0;i<4;i++)
            {
                bool visible=beat.scene>=2||beat.scene==0&&i<2||beat.scene==1&&i>=2;
                actors[i].gameObject.SetActive(visible);if(!visible)continue;
                int pose=i==beat.speaker?beat.pose:i==0?(beat.scene==0&&index==0?0:3):i==1?(beat.scene==0&&index<4?0:2):0;
                if(beat.scene==3&&i==0)pose=2;
                if(beat.scene==3&&i==1&&beat.stage>=3)pose=guardAge<1.2f?4:beat.stage>=4?5:6;
                var sprite=art.actors[i].poses[Mathf.Clamp(pose,0,art.actors[i].poses.Length-1)];actors[i].sprite=sprite;
                float height=beat.scene<2?550:460;
                if(i<2&&pose==0)height=180;
                float width=height*sprite.rect.width/sprite.rect.height;
                actors[i].rectTransform.sizeDelta=new Vector2(width,height);
                float x=beat.scene==0?(i==0?-300:300):beat.scene==1?(i==2?-300:300):i==0?-300:i==1?-40:i==2?260:550;
                if(beat.stage==1&&beat.scene==0)x+=Mathf.Lerp(0,100,progress);
                if(beat.scene==3&&i==0)x=Mathf.Lerp(-300,-40,guard);
                if(beat.scene==3&&i==1)x=Mathf.Lerp(-40,-260,guard);
                float feet=-245+(beat.scene==3&&i==0?guard*40:0);
                actors[i].rectTransform.anchoredPosition=new Vector2(x,feet+height*.5f+Mathf.Sin(clock*2+i)*2);
                float emphasis=i==beat.speaker?1.035f:1;
                // The supplied reach pose faces right; mirror Rem to meet the attacker on the left.
                actors[i].rectTransform.localScale=new Vector3(beat.scene==3&&i==1&&beat.stage>=3?-emphasis:emphasis,emphasis,1);
                actors[i].color=i==beat.speaker?Color.white:new Color(.78f,.82f,.85f,1);
            }
            for(int i=0;i<zombies.Length;i++)
            {
                bool visible=beat.scene>=2&&beat.stage>=1&&(i==0||beat.stage>=4);zombies[i].gameObject.SetActive(visible);if(!visible)continue;
                zombies[i].sprite=art.actors[4].poses[i==0&&beat.scene==3?2:i%art.actors[4].poses.Length];
                float h=i==0?420:180+(i%3)*40;
                zombies[i].rectTransform.sizeDelta=new Vector2(h*zombies[i].sprite.rect.width/zombies[i].sprite.rect.height,h);
                float threatAge=PhaseAge(clock,2,1);
                float threatX=Mathf.Lerp(-730,-500,Mathf.SmoothStep(0,1,Mathf.Clamp01(threatAge/2)));
                if(beat.scene==3&&beat.stage>=3)threatX=Mathf.Lerp(-500,-590,guard);
                zombies[i].rectTransform.anchoredPosition=new Vector2(i==0?threatX:(i%7-3)*210, i==0?-245+h*.5f:80+(i/7)*80-Mathf.Repeat(clock*9+i*12,45));
            }
            float start=1-Mathf.Clamp01((clock-1)/2);float cut=Mathf.Min(local/.25f,(beat.duration-local)/.25f);
            black.color=new Color(0,0,0,Mathf.Max(start,beat.speaker<0?0:Mathf.Clamp01(1-cut)*.25f));
        }
        static float PhaseAge(float clock,int scene,int stage)
        {
            float start=0;
            foreach(var beat in ZoneIntroScript.Beats)
            {
                if(beat.scene==scene&&beat.stage>=stage)return Mathf.Max(0,clock-start);
                start+=beat.duration;
            }
            return 0;
        }
        void OnDestroy(){completed=null;Playing=false;if(root!=null)Destroy(root);}
    }
}
