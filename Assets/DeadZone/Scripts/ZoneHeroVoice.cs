using UnityEngine;

namespace DeadZone
{
    [DisallowMultipleComponent]
    public sealed class ZoneHeroVoice : MonoBehaviour
    {
        public const float ShrineRevealTime = 3.21f;
        static readonly AudioClip[] clips = new AudioClip[4];
        static readonly string[] paths = {
            "Audio/Heroes/Subaru/return_by_death", "Audio/Heroes/Rem/oni_fury", null,
            "Audio/Heroes/Sukuna/malevolent_shrine" };
        public ZonePlayer Actor;
        [field: SerializeField] public AudioSource Source { get; private set; }
        int ability;
        bool tracking, paused;

        public static AudioClip Clip(int kind)
        {
            if (kind < 0 || kind >= paths.Length || paths[kind] == null) return null;
            return clips[kind] != null ? clips[kind] : clips[kind] = Resources.Load<AudioClip>(paths[kind]);
        }

        public static void Play(ZonePlayer actor, int kind, float elapsed = 0)
        {
            var clip = Clip(kind);
            if (Application.isBatchMode || actor == null || clip == null || elapsed >= clip.length ||
                actor.Dead || actor.Downed || !actor.Connected || actor.game == null || !actor.game.running) return;
            var voice = actor.GetComponent<ZoneHeroVoice>();
            if (voice == null) voice = actor.gameObject.AddComponent<ZoneHeroVoice>();
            voice.Actor = actor; voice.ability = kind; voice.Stop();
            voice.Source.clip = clip; voice.Source.pitch = 1;
            voice.Source.timeSamples = Mathf.Clamp(Mathf.FloorToInt(Mathf.Max(0, elapsed) * clip.frequency), 0, clip.samples - 1);
            voice.SetLevel(); voice.Source.Play(); voice.tracking = true;
        }

        void Awake()
        {
            Source = gameObject.AddComponent<AudioSource>();
            Source.playOnAwake = false; Source.loop = false; Source.spatialBlend = 0;
            Source.dopplerLevel = 0; Source.pitch = 1; Source.priority = 65;
        }

        void SetLevel()
        {
            var listener = Actor.game.CameraActor;
            float level = 0;
            if (listener != null && listener.gameObject.layer == Actor.gameObject.layer)
            {
                float distance = Vector2.Distance(listener.transform.position, Actor.transform.position);
                level = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(6, 30, distance));
                Source.panStereo = Mathf.Clamp((Actor.transform.position.x - listener.transform.position.x) / 16, -.6f, .6f);
            }
            Source.volume = ZoneFeedback.Volume * .95f * level;
        }

        void LateUpdate()
        {
            if (!tracking) return;
            if (Actor == null || !Actor.isActiveAndEnabled || !Actor.Connected || Actor.Dead || Actor.Downed ||
                Actor.game == null || !Actor.game.running || ability == 1 && !Actor.Oni.Active || ability == 3 && Actor.ShrineRemaining <= 0)
            { Stop(); return; }
            SetLevel();
            if (Actor.game.paused)
            {
                if (!paused) { Source.Pause(); paused = true; }
                return;
            }
            if (paused) { Source.UnPause(); paused = false; }
            if (!Source.isPlaying) tracking = false;
        }

        public void Stop() { if (Source != null) Source.Stop(); tracking = paused = false; }
        void OnDisable() => Stop();
    }
}
