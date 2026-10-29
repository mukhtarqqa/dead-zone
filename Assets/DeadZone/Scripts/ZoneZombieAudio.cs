using UnityEngine;

namespace DeadZone
{
    // Uses planar distance from the current camera actor; camera Z never attenuates a 2D sound.
    public sealed class ZoneZombieAudio : MonoBehaviour
    {
        sealed class Voice
        {
            public AudioSource source;
            public ZoneZombieVoice emitter;
            public bool attack;
            public float gain;
        }

        static ZoneZombieAudio instance;
        static ZoneZombieAudioBank bank;
        static bool loaded;
        readonly System.Random random = new System.Random(137);
        Voice[] voices;
        int lastGrowl = -1, lastAttack = -1;
        float nextGrowlAt;
        bool paused;

        public static ZoneZombieAudioBank Bank
        {
            get
            {
                if (!loaded) { bank = Resources.Load<ZoneZombieAudioBank>("ZombieAudioBank"); loaded = true; }
                return bank;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache() { instance = null; bank = null; loaded = false; }

        public static bool Play(ZoneZombieVoice emitter, bool attack)
        {
            if (Application.isBatchMode || emitter == null || !emitter.Audible || ZoneFeedback.Volume <= 0 || Bank == null) return false;
            var clips = attack ? Bank.attacks : Bank.growls;
            if (clips == null || clips.Length == 0 || Attenuation(emitter) <= 0) return false;
            if (instance == null)
            {
                var obj = new GameObject("Zombie audio");
                instance = obj.AddComponent<ZoneZombieAudio>();
                instance.voices = new Voice[Mathf.Clamp(Bank.voiceLimit, 2, 12)];
                for (int i = 0; i < instance.voices.Length; i++)
                {
                    var source = obj.AddComponent<AudioSource>();
                    source.playOnAwake = false;
                    source.loop = false;
                    source.spatialBlend = 0;
                    source.dopplerLevel = 0;
                    instance.voices[i] = new Voice { source = source };
                }
            }
            return instance.StartVoice(emitter, attack, clips);
        }

        bool StartVoice(ZoneZombieVoice emitter, bool attack, AudioClip[] clips)
        {
            Voice free = null, quietGrowl = null, quietest = null;
            int growls = 0;
            foreach (var voice in voices)
            {
                if (!voice.source.isPlaying) { if (free == null) free = voice; continue; }
                if (voice.emitter == emitter)
                {
                    if (!attack) return false;
                    free = voice;
                    break;
                }
                if (!voice.attack)
                {
                    growls++;
                    if (quietGrowl == null || voice.source.volume < quietGrowl.source.volume) quietGrowl = voice;
                }
                if (quietest == null || voice.source.volume < quietest.source.volume) quietest = voice;
            }
            if (!attack && (Time.time < nextGrowlAt || growls >= Mathf.Clamp(Bank.growlLimit, 1, voices.Length))) return false;
            if (free == null)
            {
                if (!attack) return false;
                free = quietGrowl != null ? quietGrowl : quietest;
                if (free == null || quietGrowl == null && Attenuation(emitter) * Bank.attackVolume <= free.source.volume / Mathf.Max(.001f, ZoneFeedback.Volume)) return false;
            }
            int last = attack ? lastAttack : lastGrowl;
            int index = random.Next(clips.Length);
            if (clips.Length > 1 && index == last) index = (index + 1 + random.Next(clips.Length - 1)) % clips.Length;
            var clip = clips[index];
            if (clip == null) return false;
            free.source.Stop();
            free.emitter = emitter;
            free.attack = attack;
            free.gain = attack ? Bank.attackVolume : Bank.growlVolume;
            free.source.clip = clip;
            free.source.priority = attack ? 100 : 150;
            free.source.pitch = (emitter.Boss ? .9f : emitter.Kind == 3 ? .94f : emitter.Kind == 2 ? 1.04f : 1) * (.98f + (float)random.NextDouble() * .04f);
            SetLevel(free, 1);
            free.source.Play();
            if (attack) lastAttack = index;
            else { lastGrowl = index; nextGrowlAt = Time.time + .6f; }
            return true;
        }

        void Update()
        {
            var game = DeadZoneGame.Instance;
            bool shouldPause = game != null && game.paused;
            if (shouldPause != paused)
            {
                paused = shouldPause;
                foreach (var voice in voices) { if (paused) voice.source.Pause(); else voice.source.UnPause(); }
            }
            if (paused) return;
            int active = 0;
            foreach (var voice in voices)
            {
                if (voice.emitter == null || !voice.emitter.Audible || Attenuation(voice.emitter) <= 0 || ZoneFeedback.Volume <= 0)
                { voice.source.Stop(); voice.emitter = null; }
                else if (voice.source.isPlaying) active++;
            }
            float crowd = 1 / Mathf.Sqrt(Mathf.Max(1, active * .6f));
            foreach (var voice in voices) if (voice.source.isPlaying) SetLevel(voice, crowd);
        }

        static float Attenuation(ZoneZombieVoice emitter)
        {
            var actor = emitter.Game != null ? emitter.Game.CameraActor : null;
            if (actor == null || actor.gameObject.layer != emitter.gameObject.layer || Bank == null) return 0;
            float distance = Vector2.Distance(actor.transform.position, emitter.transform.position);
            float t = Mathf.InverseLerp(Bank.fullVolumeDistance, Mathf.Max(Bank.fullVolumeDistance + .1f, Bank.audibleDistance), distance);
            return 1 - Mathf.SmoothStep(0, 1, t);
        }

        static void SetLevel(Voice voice, float crowd)
        {
            voice.source.volume = ZoneFeedback.Volume * voice.gain * Attenuation(voice.emitter) * crowd;
            var actor = voice.emitter.Game.CameraActor;
            voice.source.panStereo = Mathf.Clamp((voice.emitter.transform.position.x - actor.transform.position.x) / 12, -.65f, .65f);
        }

        public static void Stop(ZoneZombieVoice emitter)
        {
            if (instance == null || instance.voices == null) return;
            foreach (var voice in instance.voices) if (voice.emitter == emitter) { voice.source.Stop(); voice.emitter = null; }
        }
    }
}
