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
        AudioSource source;
        public static float Volume {get=>PlayerPrefs.GetFloat("dz.volume",.5f);set{PlayerPrefs.SetFloat("dz.volume",Mathf.Clamp01(value));}}
        public static bool Shake {get=>PlayerPrefs.GetInt("dz.shake",1)!=0;set{PlayerPrefs.SetInt("dz.shake",value?1:0);PlayerPrefs.Save();}}
        public static void Play(int kind)
        {
            if(Application.isBatchMode||Volume<=0)return;
            if(instance==null){var obj=new GameObject("Combat audio");instance=obj.AddComponent<ZoneFeedback>();instance.source=obj.AddComponent<AudioSource>();instance.source.spatialBlend=0;instance.source.playOnAwake=false;}
            if(sounds==null)
            {
                sounds=new AudioClip[8];var random=new System.Random(19);
                for(int k=0;k<8;k++)
                {
                    int count=Mathf.RoundToInt(22050*(k==3?.20f:k==5?.27f:.08f));var data=new float[count];
                    for(int i=0;i<count;i++){float t=(float)i/22050,env=Mathf.Pow(1-(float)i/count,k==5?2:5);float noise=(float)random.NextDouble()*2-1;data[i]=(noise*(k<4?.55f:.18f)+Mathf.Sin(t*(k<4?150:70)*Mathf.PI*2)*.45f)*env*.45f;}
                    sounds[k]=AudioClip.Create("Dead Zone effect "+k,count,1,22050,false);sounds[k].SetData(data,0);
                }
            }
            instance.source.PlayOneShot(sounds[Mathf.Clamp(kind,0,7)],Volume);
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
