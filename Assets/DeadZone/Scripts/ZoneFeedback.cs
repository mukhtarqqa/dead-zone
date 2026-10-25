using UnityEngine;
namespace DeadZone
{
    public static class ZoneCombat
    {
        public static bool Clear(Vector2 from,Vector2 to,int floor)
        {
            Vector2 delta=to-from;
            foreach(var hit in Physics2D.RaycastAll(from,delta.normalized,delta.magnitude))
            {
                var c=hit.collider;if(c.isTrigger||c.GetComponentInParent<ZonePlayer>()!=null||c.GetComponentInParent<ZoneEnemy>()!=null)continue;
                int layer=c.gameObject.layer;if((layer>=20&&layer<=22&&layer!=floor)||Physics2D.GetIgnoreLayerCollision(floor,layer))continue;
                return false;
            }
            return true;
        }
    }
    public sealed class ZoneFeedback : MonoBehaviour
    {
        static ZoneFeedback instance;
        static AudioClip[] sounds;
        readonly AudioSource[] voices=new AudioSource[12];int voice;
        static readonly System.Random audioRandom=new System.Random(73);
        public static float Volume {get=>PlayerPrefs.GetFloat("dz.volume",.5f);set{PlayerPrefs.SetFloat("dz.volume",Mathf.Clamp01(value));}}
        public static bool Shake {get=>PlayerPrefs.GetInt("dz.shake",1)!=0;set{PlayerPrefs.SetInt("dz.shake",value?1:0);PlayerPrefs.Save();}}
        public static void Play(int kind)
        {
            if(Application.isBatchMode||Volume<=0)return;
            if(instance==null){var obj=new GameObject("Combat audio");instance=obj.AddComponent<ZoneFeedback>();for(int i=0;i<instance.voices.Length;i++){var source=obj.AddComponent<AudioSource>();source.spatialBlend=0;source.playOnAwake=false;instance.voices[i]=source;}}
            if(sounds==null)
            {
                sounds=new AudioClip[12];var random=new System.Random(19);
                for(int k=0;k<sounds.Length;k++)
                {
                    float duration=k==0?.13f:k==1?.10f:k==2?.17f:k==3?.32f:k==5?.6f:k>=10?.7f:.12f;
                    int count=Mathf.RoundToInt(22050*duration);var data=new float[count];
                    for(int i=0;i<count;i++)
                    {
                        float t=(float)i/22050,x=(float)i/count,env=Mathf.Pow(1-x,k==5?2:4),noise=(float)random.NextDouble()*2-1;
                        float hz=k==0?190:k==1?125:k==2?85:k==3?65:k==8?1100:k==9?650:k==10?320:70;
                        float body=Mathf.Sin(2*Mathf.PI*hz*t*(1-x*.45f));
                        float attack=Mathf.Min(1,t*1800);
                        float signal=k<4?noise*(k==3?.7f:.4f)+body*.6f:k==5?noise*.8f+body*.4f:k>=10?body*.5f+Mathf.Sin(t*2*Mathf.PI*(hz*1.5f))*.25f:noise*.3f+body*.35f;
                        data[i]=Mathf.Clamp(signal*env*attack*.45f,-1,1);
                    }
                    sounds[k]=AudioClip.Create("Dead Zone effect "+k,count,1,22050,false);sounds[k].SetData(data,0);
                }
            }
            var selected=instance.voices[instance.voice++%instance.voices.Length];selected.pitch=.96f+(float)audioRandom.NextDouble()*.08f;
            selected.PlayOneShot(sounds[Mathf.Clamp(kind,0,sounds.Length-1)],Volume);
        }
        static float kick;
        public static void Kick(float amount){if(Shake)kick=Mathf.Max(kick,amount);}
        public static Vector3 CameraOffset(){float value=kick;kick=Mathf.MoveTowards(kick,0,Time.unscaledDeltaTime*1.7f);return new Vector3(Mathf.Sin(Time.unscaledTime*73),Mathf.Cos(Time.unscaledTime*89))*value;}
        public static void Hit(Vector2 point,float damage,int floor,bool player=false)
        {
            if(Application.isBatchMode)return;
            Play(player?6:4);if(player)Kick(.12f);
            var obj=new GameObject("Damage number");obj.transform.position=point+Vector2.up*.9f;
            var text=obj.AddComponent<TMPro.TextMeshPro>();text.text=Mathf.CeilToInt(damage).ToString();text.fontSize=3;text.alignment=TMPro.TextAlignmentOptions.Center;text.color=player?new Color(1,.3f,.4f):new Color(1,.86f,.4f);var renderer=obj.GetComponent<MeshRenderer>();renderer.sortingLayerID=ZoneMap.Sorting(floor);renderer.sortingOrder=220;text.rectTransform.sizeDelta=new Vector2(2,1);obj.AddComponent<ZoneFloatingNumber>();
        }
    }
    public sealed class ZoneFloatingNumber : MonoBehaviour
    {
        float age;void Update(){age+=Time.deltaTime;transform.position+=Vector3.up*Time.deltaTime*.65f;var text=GetComponent<TMPro.TextMeshPro>();var c=text.color;c.a=1-age/.65f;text.color=c;if(age>.65f)Destroy(gameObject);}
    }
}
