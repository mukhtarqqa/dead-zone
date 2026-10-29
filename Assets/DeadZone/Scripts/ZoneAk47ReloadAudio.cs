using UnityEngine;

namespace DeadZone
{
    [DisallowMultipleComponent]
    public sealed class ZoneAk47ReloadAudio : MonoBehaviour
    {
        public ZonePlayer Actor;
        [field: SerializeField] public AudioSource Source { get; private set; }
        static AudioClip clip;
        bool tracking, paused;
        float previousRemaining;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache() => clip = null;

        void Awake()
        {
            Source = gameObject.AddComponent<AudioSource>();
            Source.playOnAwake = false;
            Source.loop = false;
            Source.spatialBlend = 0;
            Source.dopplerLevel = 0;
            Source.pitch = 1;
            Source.priority = 120;
        }

        void LateUpdate()
        {
            if (Actor == null || !Actor.isActiveAndEnabled || Actor.game == null || !Actor.game.running ||
                Actor.weapon != 1 || !Actor.Connected || Actor.Dead || Actor.Downed || Actor.Oni.Active || !Actor.Reloading)
            { Stop(); return; }
            Source.volume = ZoneFeedback.Volume * .9f;
            if (Actor.game.paused)
            {
                if (tracking && !paused) { Source.Pause(); paused = true; }
                return;
            }
            if (paused) { Source.UnPause(); paused = false; }
            float remaining = Actor.ReloadRemaining;
            // A replica may enter an already running reload; repeated snapshots keep it playing.
            if (!tracking || remaining > previousRemaining + .12f)
            {
                if (clip == null) clip = Resources.Load<AudioClip>("Audio/Weapons/AK47/ak47_reload");
                if (clip == null) return;
                Source.Stop();
                Source.clip = clip;
                Source.pitch = 1;
                float elapsed = Mathf.Clamp(ZoneArsenal.Stats(1).Reload - remaining, 0, clip.length);
                Source.timeSamples = Mathf.Clamp(Mathf.FloorToInt(elapsed * clip.frequency), 0, clip.samples - 1);
                Source.Play();
                tracking = true;
            }
            previousRemaining = remaining;
        }

        public void Stop()
        {
            if (Source != null && (tracking || Source.isPlaying)) Source.Stop();
            tracking = paused = false;
            previousRemaining = 0;
        }

        void OnDisable() => Stop();
    }
}
