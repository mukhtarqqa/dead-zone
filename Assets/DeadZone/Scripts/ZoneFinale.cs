using TMPro;
using UnityEngine;
using UnityEngine.Video;

namespace DeadZone
{
    public sealed class ZoneFinale : MonoBehaviour
    {
        public const string Epilogue="После всего пережитого...\nПосле бесконечной борьбы за жизнь...\nНаконец появился шанс.\nШанс начать всё сначала.\n<b>ОНИ ВЫЖИЛИ.</b>";
        public bool Playing {get;private set;}
        public VideoPlayer Player=>video;
        DeadZoneGame game;TMP_FontAsset font;
        GameObject root;VideoPlayer video;RenderTexture texture;UnityEngine.UI.RawImage picture;
        float openedAt,finishedAt=-1;bool prepared,wasCursorVisible;

        public void Initialize(DeadZoneGame owner,TMP_FontAsset typeface){game=owner;font=typeface;}
        public void Play()
        {
            if(Playing||Application.isBatchMode)return;
            Playing=true;openedAt=Time.unscaledTime;finishedAt=-1;prepared=false;
            wasCursorVisible=Cursor.visible;Cursor.visible=true;
            root=new GameObject("Victory cinematic",typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=1000;
            var scaler=root.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
            var shade=Rect(root.transform,"Black background",Vector2.zero,Vector2.one);shade.gameObject.AddComponent<UnityEngine.UI.Image>().color=Color.black;
            var frame=Rect(root.transform,"Video area",new Vector2(.04f,.34f),new Vector2(.96f,.96f));
            var screen=Rect(frame,"Final cutscene",Vector2.zero,Vector2.one);
            picture=screen.gameObject.AddComponent<UnityEngine.UI.RawImage>();picture.color=Color.black;picture.raycastTarget=false;
            var fit=screen.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();fit.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=16f/9;
            var text=Label(root.transform,"Epilogue",new Vector2(.06f,.08f),new Vector2(.94f,.32f),29);text.text=Epilogue;
            text.enableAutoSizing=true;text.fontSizeMin=18;text.fontSizeMax=29;
            var next=Rect(root.transform,"Continue to results",new Vector2(.69f,.015f),new Vector2(.97f,.065f));
            var image=next.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=new Color(.12f,.16f,.1f,.95f);
            var button=next.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=image;button.onClick.AddListener(Continue);
            Label(next,"Label",Vector2.zero,Vector2.one,19).text="К результатам · Esc";
            video=root.AddComponent<VideoPlayer>();video.playOnAwake=false;video.isLooping=false;video.waitForFirstFrame=true;
            video.timeUpdateMode=VideoTimeUpdateMode.UnscaledGameTime;
            video.audioOutputMode=VideoAudioOutputMode.None;video.renderMode=VideoRenderMode.RenderTexture;
            video.prepareCompleted+=Prepared;video.loopPointReached+=Finished;video.errorReceived+=Failed;
            video.clip=Resources.Load<VideoClip>("Cinematics/FinalCutscene");
            if(video.clip==null){Failed(video,"FinalCutscene is missing");return;}
            video.Prepare();
        }
        void Prepared(VideoPlayer player)
        {
            if(!Playing)return;prepared=true;
            float scale=Mathf.Min(1,1920f/Mathf.Max(1,player.width));
            int width=Mathf.Max(2,Mathf.RoundToInt(player.width*scale)),height=Mathf.Max(2,Mathf.RoundToInt(player.height*scale));
            texture=new RenderTexture(width,height,0,RenderTextureFormat.ARGB32);texture.Create();
            player.targetTexture=texture;picture.texture=texture;picture.color=Color.white;
            picture.GetComponent<UnityEngine.UI.AspectRatioFitter>().aspectRatio=(float)width/height;player.Play();
        }
        void Finished(VideoPlayer player){if(finishedAt<0)finishedAt=Time.unscaledTime;}
        void Failed(VideoPlayer player,string error){Debug.LogWarning("Final cinematic: "+error);finishedAt=Time.unscaledTime;}
        void Update()
        {
            if(!Playing)return;
            if(!game.ended||!game.Victory){Stop();return;}
            if(!prepared&&finishedAt<0&&Time.unscaledTime-openedAt>20)Failed(video,"Video preparation timed out");
            if(Application.isFocused&&Input.GetKeyDown(KeyCode.Escape))Continue();
            if(finishedAt>=0&&Time.unscaledTime-finishedAt>=6)Stop();
        }
        public void Continue(){if(Playing&&Time.unscaledTime-openedAt>.5f)Stop();}
        public void Stop()
        {
            if(!Playing)return;Playing=false;
            if(video!=null){video.prepareCompleted-=Prepared;video.loopPointReached-=Finished;video.errorReceived-=Failed;video.Stop();video.targetTexture=null;}
            if(root!=null){root.SetActive(false);Destroy(root);}root=null;video=null;picture=null;
            if(texture!=null){texture.Release();Destroy(texture);}texture=null;Cursor.visible=wasCursorVisible;
        }
        void OnDestroy(){Stop();}
        static RectTransform Rect(Transform parent,string name,Vector2 min,Vector2 max)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;return rect;
        }
        TextMeshProUGUI Label(Transform parent,string name,Vector2 min,Vector2 max,int size)
        {
            var text=Rect(parent,name,min,max).gameObject.AddComponent<TextMeshProUGUI>();text.font=font;text.fontSize=size;
            text.color=Color.white;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;text.textWrappingMode=TextWrappingModes.Normal;return text;
        }
    }
}
