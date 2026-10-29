using UnityEngine;

namespace DeadZone
{
    [DisallowMultipleComponent]
    public sealed class ZoneExplosiveAudio : MonoBehaviour
    {
        static AudioClip grenadeThrow, mineArm, mineExplosion;
        static AudioClip[] grenadeExplosions;
        static int lastExplosion = -1;
        [field: SerializeField] public AudioSource Source { get; private set; }
        bool ownsObject, paused, finished;
        float gain;

        public static AudioClip GrenadeThrow => grenadeThrow != null ? grenadeThrow : grenadeThrow = Resources.Load<AudioClip>("Audio/Explosives/HE/he_pin_throw");
        public static AudioClip MineArm => mineArm != null ? mineArm : mineArm = Resources.Load<AudioClip>("Audio/Explosives/C4/mine_place_arm");
        public static AudioClip NextExplosion(bool mine)
        {
            if (mine) return mineExplosion != null ? mineExplosion : mineExplosion = Resources.Load<AudioClip>("Audio/Explosives/C4/mine_explosion");
            if (grenadeExplosions == null) grenadeExplosions = new[] {
                Resources.Load<AudioClip>("Audio/Explosives/HE/he_explosion_01"),
                Resources.Load<AudioClip>("Audio/Explosives/HE/he_explosion_02") };
            int index = lastExplosion < 0 ? Random.Range(0, grenadeExplosions.Length) : (lastExplosion + 1) % grenadeExplosions.Length;
            lastExplosion = index;
            return grenadeExplosions[index];
        }

        // Owned by the physical object: deleting a mine also cancels its ready beep.
        public static void Attach(GameObject emitter, bool mine, float elapsed = 0)
        {
            if (Application.isBatchMode || emitter == null || emitter.GetComponent<ZoneExplosiveAudio>() != null) return;
            var clip = mine ? MineArm : GrenadeThrow;
            if (clip == null || elapsed >= clip.length) return;
            emitter.AddComponent<ZoneExplosiveAudio>().Begin(clip, elapsed, .95f, false);
        }

        // The tail survives destruction of the grenade or mine that caused the blast.
        public static void Explode(Vector2 point, int floor, bool mine, float elapsed = 0)
        {
            if (Application.isBatchMode) return;
            var clip = NextExplosion(mine);
            if (clip == null || elapsed >= clip.length) return;
            var obj = new GameObject(mine ? "C4 explosion audio" : "HE explosion audio");
            obj.layer = floor; obj.transform.position = point;
            obj.AddComponent<ZoneExplosiveAudio>().Begin(clip, elapsed, 1, true);
        }

        void Begin(AudioClip clip, float elapsed, float volume, bool independent)
        {
            ownsObject = independent; gain = volume;
            Source = gameObject.AddComponent<AudioSource>();
            Source.playOnAwake = false; Source.loop = false; Source.spatialBlend = 0;
            Source.dopplerLevel = 0; Source.pitch = 1; Source.priority = independent ? 70 : 100;
            Source.clip = clip;
            Source.timeSamples = Mathf.Clamp(Mathf.FloorToInt(Mathf.Max(0, elapsed) * clip.frequency), 0, clip.samples - 1);
            SetLevel(); Source.Play();
        }

        void SetLevel()
        {
            var actor = DeadZoneGame.Instance != null ? DeadZoneGame.Instance.CameraActor : null;
            float level = 0;
            if (actor != null && actor.gameObject.layer == gameObject.layer)
            {
                float distance = Vector2.Distance(actor.transform.position, transform.position);
                float t = Mathf.InverseLerp(6, ownsObject ? 40 : 24, distance);
                level = 1 - Mathf.SmoothStep(0, 1, t);
                Source.panStereo = Mathf.Clamp((transform.position.x - actor.transform.position.x) / 16, -.65f, .65f);
            }
            Source.volume = ZoneFeedback.Volume * gain * level;
        }

        void Update()
        {
            if (Source == null || finished) return;
            var game = DeadZoneGame.Instance;
            if (game == null || !game.running) { Finish(); return; }
            SetLevel();
            if (game.paused)
            {
                if (!paused) { Source.Pause(); paused = true; }
                return;
            }
            if (paused) { Source.UnPause(); paused = false; }
            if (!Source.isPlaying) Finish();
        }

        void Finish()
        {
            Source.Stop(); finished = true;
            if (ownsObject) Destroy(gameObject);
        }
        void OnDisable() { if (Source != null) Source.Stop(); }
    }
}
