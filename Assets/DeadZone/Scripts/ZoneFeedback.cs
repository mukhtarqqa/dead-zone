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
        static AudioClip[] weaponShots;
        static AudioClip[] weaponReloads;
        readonly AudioSource[] voices=new AudioSource[12];int voice;
        // PlayOneShot creates an extra mixer voice on every call, even on a reused
        // AudioSource. Long Negev tails otherwise exhaust the 32 real voices.
        public const int ShotVoiceLimit=12;
        readonly AudioSource[] shotVoices=new AudioSource[ShotVoiceLimit];int shotVoice;
        static readonly System.Random audioRandom=new System.Random(73);
        public static float Volume {get=>PlayerPrefs.GetFloat("dz.volume",.5f);set{PlayerPrefs.SetFloat("dz.volume",Mathf.Clamp01(value));}}
        public static bool Shake {get=>PlayerPrefs.GetInt("dz.shake",1)!=0;set{PlayerPrefs.SetInt("dz.shake",value?1:0);PlayerPrefs.Save();}}

        static void EnsureClips()
        {
            if (weaponShots == null)
            {
                weaponShots = new AudioClip[4];
                weaponShots[0] = Resources.Load<AudioClip>("Audio/Weapons/Glock18/glock18_shot") ?? Resources.Load<AudioClip>("Audio/sfx_pistol_shot");
                weaponShots[1] = Resources.Load<AudioClip>("Audio/Weapons/AK47/ak47_01") ?? Resources.Load<AudioClip>("Audio/sfx_ak47_shot");
                weaponShots[2] = Resources.Load<AudioClip>("Audio/Weapons/Negev/negev_shot_01") ?? Resources.Load<AudioClip>("Audio/sfx_heavy_shot");
                weaponShots[3] = Resources.Load<AudioClip>("Audio/Weapons/Nova/nova_shot") ?? Resources.Load<AudioClip>("Audio/sfx_shotgun_shot");
            }
            if (weaponReloads == null)
            {
                weaponReloads = new AudioClip[4];
                weaponReloads[0] = ZoneWeaponAudio.Reload(0) ?? Resources.Load<AudioClip>("Audio/sfx_pistol_reload");
                weaponReloads[1] = Resources.Load<AudioClip>("Audio/Weapons/AK47/ak47_reload") ?? Resources.Load<AudioClip>("Audio/sfx_ak47_reload");
                weaponReloads[2] = ZoneWeaponAudio.Reload(2) ?? Resources.Load<AudioClip>("Audio/sfx_heavy_reload");
                weaponReloads[3] = ZoneWeaponAudio.Reload(3) ?? Resources.Load<AudioClip>("Audio/sfx_shotgun_reload");
            }
        }

        public static void PlayShot(int weaponKind, ZonePlayer owner = null)
        {
            if(Application.isBatchMode||Volume<=0)return;
            EnsureInstance();
            EnsureClips();
            int index = Mathf.Clamp(weaponKind, 0, 3);
            var approved = ZoneWeaponAudio.NextShot(index);
            if (approved != null)
            {
                PlayShotClip(approved,Volume * (index == 1 ? .82f : 2),1);
                if (index == 3 && owner != null) owner.GetComponent<ZoneWeaponReloadAudio>()?.QueuePump();
                return;
            }
            if (weaponShots != null && weaponShots[index] != null)
            {
                PlayShotClip(weaponShots[index],Volume,.98f + (float)audioRandom.NextDouble() * .04f);
                return;
            }
            Play(weaponKind);
        }

        static void PlayShotClip(AudioClip clip,float gain,float pitch)
        {
            var source=instance.shotVoices[instance.shotVoice];
            instance.shotVoice=(instance.shotVoice+1)%instance.shotVoices.Length;
            source.Stop();source.clip=clip;source.pitch=pitch;source.volume=1;
            source.PlayOneShot(clip,gain);
        }

        public static void PlayReload(int weaponKind)
        {
            if(Application.isBatchMode||Volume<=0)return;
            EnsureInstance();
            EnsureClips();
            int index = Mathf.Clamp(weaponKind, 0, 3);
            if (weaponReloads != null && weaponReloads[index] != null)
            {
                var voice = instance.voices[instance.voice++%instance.voices.Length];
                voice.pitch = 1f;
                voice.PlayOneShot(weaponReloads[index], Volume * .9f);
                return;
            }
            Play(8);
        }

        static void EnsureInstance()
        {
            if(instance==null)
            {
                var obj=new GameObject("Combat audio");
                instance=obj.AddComponent<ZoneFeedback>();
                for(int i=0;i<instance.voices.Length;i++)
                {
                    var source=obj.AddComponent<AudioSource>();
                    source.spatialBlend=0;
                    source.playOnAwake=false;
                    instance.voices[i]=source;
                }
                var shots=new GameObject("Weapon shot channels");shots.transform.SetParent(obj.transform,false);
                for(int i=0;i<instance.shotVoices.Length;i++)
                {
                    var source=shots.AddComponent<AudioSource>();source.playOnAwake=false;
                    source.spatialBlend=0;source.dopplerLevel=0;source.loop=false;source.priority=80;
                    instance.shotVoices[i]=source;
                }
            }
        }

        public static void Play(int kind)
        {
            if(Application.isBatchMode||Volume<=0)return;
            EnsureInstance();
            EnsureClips();

            if (kind >= 0 && kind <= 3 && weaponShots != null && weaponShots[kind] != null)
            {
                PlayShot(kind);
                return;
            }
            if(sounds==null)
            {
                sounds=new AudioClip[14];var random=new System.Random(19);
                for(int k=0;k<sounds.Length;k++)
                {
                    float duration=k==12?1.1f:k==13?.18f:k==0?.13f:k==1?.10f:k==2?.17f:k==3?.32f:k==5?.6f:k>=10?.7f:.12f;
                    int count=Mathf.RoundToInt(22050*duration);var data=new float[count];
                    for(int i=0;i<count;i++)
                    {
                        float t=(float)i/22050,x=(float)i/count,env=Mathf.Pow(1-x,k==5?2:4),noise=(float)random.NextDouble()*2-1;
                        float hz=k==0?190:k==1?125:k==2?85:k==3?65:k==8?1100:k==9?650:k==10?320:70;
                        float body=Mathf.Sin(2*Mathf.PI*hz*t*(1-x*.45f));
                        float attack=Mathf.Min(1,t*1800);
                        float signal=k<4?noise*(k==3?.7f:.4f)+body*.6f:k==5?noise*.8f+body*.4f:k>=10?body*.5f+Mathf.Sin(t*2*Mathf.PI*(hz*1.5f))*.25f:noise*.3f+body*.35f;
                        if(k==12){env=Mathf.Sin(x*Mathf.PI)*(.5f+.5f*x);signal=Mathf.Sin(2*Mathf.PI*(48+35*x)*t)*.65f+Mathf.Sin(t*2*Mathf.PI*96)*.2f+noise*.12f;}
                        if(k==13){env=Mathf.Pow(1-x,3);signal=noise*.65f+Mathf.Sin(t*2*Mathf.PI*(1800-1300*x))*.25f+body*.18f;}
                        data[i]=Mathf.Clamp(signal*env*attack*.45f,-1,1);
                    }
                    sounds[k]=AudioClip.Create("Dead Zone effect "+k,count,1,22050,false);sounds[k].SetData(data,0);
                }
            }
            if(kind>=0&&kind<=3){PlayShotClip(sounds[kind],Volume,.96f+(float)audioRandom.NextDouble()*.08f);return;}
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
            var text=obj.AddComponent<TMPro.TextMeshPro>();text.text=Mathf.CeilToInt(damage).ToString();text.fontSize=3;text.alignment=TMPro.TextAlignmentOptions.Center;text.color=player?new Color(1,.3f,.4f):new Color(1,.86f,.4f);var renderer=obj.GetComponent<MeshRenderer>();renderer.sortingLayerID=ZoneMap.Sorting(floor);renderer.sortingOrder=3200;text.rectTransform.sizeDelta=new Vector2(2,1);obj.AddComponent<ZoneFloatingNumber>();
        }
    }
    public sealed class ZoneFloatingNumber : MonoBehaviour
    {
        float age;void Update(){age+=Time.deltaTime;transform.position+=Vector3.up*Time.deltaTime*.65f;var text=GetComponent<TMPro.TextMeshPro>();var c=text.color;c.a=1-age/.65f;text.color=c;if(age>.65f)Destroy(gameObject);}
    }
}
