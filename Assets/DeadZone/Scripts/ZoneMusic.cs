using UnityEngine;
namespace DeadZone
{
    public sealed class ZoneMusic : MonoBehaviour
    {
        public static float Volume {get=>PlayerPrefs.GetFloat("dz.music",.25f);set=>PlayerPrefs.SetFloat("dz.music",Mathf.Clamp01(value));}
        readonly AudioClip[] clips=new AudioClip[3];AudioSource source;int current=-1;
        void Update()
        {
            if(Application.isBatchMode)return;var game=DeadZoneGame.Instance;if(game==null)return;
            if(source==null){source=gameObject.AddComponent<AudioSource>();source.loop=true;source.playOnAwake=false;source.volume=0;}
            if(game.IntroPlaying){source.volume=Mathf.MoveTowards(source.volume,0,Time.unscaledDeltaTime);return;}
            int desired=!game.running?0:game.BossPhase?2:1;
            if(current!=desired)
            {
                source.volume=Mathf.MoveTowards(source.volume,0,Time.unscaledDeltaTime*.7f);
                if(source.volume<=.001f){if(clips[desired]==null)clips[desired]=Compose(desired);source.clip=clips[desired];current=desired;source.Play();}
            }
            else source.volume=Mathf.MoveTowards(source.volume,Volume*(game.paused?.25f:1),Time.unscaledDeltaTime*.5f);
        }
        static AudioClip Compose(int mood)
        {
            const int rate=22050;float beat=60f/(mood==2?112:mood==1?94:72);int count=Mathf.RoundToInt(beat*16*rate);var data=new float[count];
            int[] roots={0,-3,-5,-2};int[] melody={0,7,12,10,7,3,5,7,0,3,7,12,10,7,5,3};
            for(int i=0;i<count;i++)
            {
                float t=(float)i/rate,b=t/beat;int step=Mathf.FloorToInt(b),root=roots[(step/4)%4];float part=b-step;
                float hz=55*Mathf.Pow(2,root/12f);float bass=Mathf.Sin(t*hz*Mathf.PI*2)*Mathf.Exp(-part*(mood==0?2:5))*.16f;
                float pad=(Mathf.Sin(t*hz*4*Mathf.PI*2)+Mathf.Sin(t*hz*4*1.189207f*Mathf.PI*2)+Mathf.Sin(t*hz*6*Mathf.PI*2))*.028f;
                float note=220*Mathf.Pow(2,(root+melody[step%16])/12f);float lead=Mathf.Sin(t*note*Mathf.PI*2)*Mathf.Exp(-part*5)*.035f;
                float kick=mood==0?0:Mathf.Sin((45*t+2.8f*(1-Mathf.Exp(-part*35)))*Mathf.PI*2)*Mathf.Exp(-part*20)*.13f;
                float hat=mood==0?0:Mathf.Sin(t*7201)*Mathf.Sin(t*8491)*Mathf.Exp(-Mathf.Repeat(b*2,1)*35)*.035f;
                float edge=Mathf.Min(1,Mathf.Min(t,(float)count/rate-t)*30);data[i]=(bass+pad+lead+kick+hat)*edge;
            }
            var clip=AudioClip.Create("Dead Zone original loop "+mood,count,1,rate,false);clip.SetData(data,0);return clip;
        }
        void OnDestroy(){foreach(var clip in clips)if(clip!=null)Destroy(clip);}
    }
}
