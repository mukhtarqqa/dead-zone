using UnityEngine;

namespace DeadZone
{
    [DisallowMultipleComponent]
    public sealed class ZoneWeaponReloadAudio : MonoBehaviour
    {
        public ZonePlayer Actor;
        [field: SerializeField] public AudioSource Source { get; private set; }
        [field: SerializeField] public AudioSource PumpSource { get; private set; }
        bool tracking, paused;
        float previousRemaining, pumpAt = -1;
        int reloadWeapon = -1;

        void Awake()
        {
            Source = gameObject.AddComponent<AudioSource>();
            Source.playOnAwake = false;
            Source.loop = false;
            Source.spatialBlend = 0;
            Source.dopplerLevel = 0;
            Source.pitch = 1;
            Source.priority = 120;
            PumpSource = gameObject.AddComponent<AudioSource>();
            PumpSource.playOnAwake = false;
            PumpSource.loop = false;
            PumpSource.spatialBlend = 0;
            PumpSource.dopplerLevel = 0;
            PumpSource.pitch = 1;
            PumpSource.priority = 110;
        }
        public void QueuePump() { if (Actor != null && Actor.weapon == 3 && !Actor.Reloading) pumpAt = Time.time + .42f; }

        void LateUpdate()
        {
            if (Actor == null || !Actor.isActiveAndEnabled || Actor.game == null || !Actor.game.running ||
                !Actor.Connected || Actor.Dead || Actor.Downed || Actor.Oni.Active)
            { Stop(); return; }
            Source.volume = ZoneFeedback.Volume * .9f;
            PumpSource.volume = ZoneFeedback.Volume * 2;
            if (Actor.game.paused)
            {
                if (!paused) { Source.Pause(); PumpSource.Pause(); paused = true; }
                return;
            }
            if (paused) { Source.UnPause(); PumpSource.UnPause(); paused = false; }
            if (Actor.weapon != 3 || Actor.Reloading) { pumpAt = -1; if (PumpSource.isPlaying) PumpSource.Stop(); }
            else if (pumpAt >= 0 && Time.time >= pumpAt)
            {
                pumpAt = -1;
                PumpSource.clip = ZoneWeaponAudio.NovaPump;
                if (PumpSource.clip != null) PumpSource.Play();
            }
            if (!Actor.Reloading) { StopReload(); return; }
            float remaining = Actor.ReloadRemaining;
            // A replica may enter an already running reload; repeated snapshots keep it playing.
            if (!tracking || reloadWeapon != Actor.weapon || remaining > previousRemaining + .12f)
            {
                var clip = ZoneWeaponAudio.Reload(Actor.weapon);
                if (clip == null) return;
                Source.Stop();
                Source.clip = clip;
                Source.pitch = 1;
                float elapsed = Mathf.Clamp(ZoneArsenal.Stats(Actor.weapon).Reload - remaining, 0, clip.length);
                Source.timeSamples = Mathf.Clamp(Mathf.FloorToInt(elapsed * clip.frequency), 0, clip.samples - 1);
                Source.Play();
                tracking = true;
                reloadWeapon = Actor.weapon;
            }
            previousRemaining = remaining;
        }

        void StopReload()
        {
            if (Source != null && (tracking || Source.isPlaying)) Source.Stop();
            tracking = false;
            previousRemaining = 0;
            reloadWeapon = -1;
        }
        public void Stop() { StopReload(); if (PumpSource != null) PumpSource.Stop(); pumpAt = -1; paused = false; }

        void OnDisable() => Stop();
    }
}
