using UnityEngine;

namespace DeadZone
{
    [CreateAssetMenu(menuName = "Dead Zone/Zombie audio bank")]
    public sealed class ZoneZombieAudioBank : ScriptableObject
    {
        public AudioClip[] growls = System.Array.Empty<AudioClip>();
        public AudioClip[] attacks = System.Array.Empty<AudioClip>();
        [Range(0, 1)] public float growlVolume = .32f;
        [Range(0, 1)] public float attackVolume = .55f;
        [Min(0)] public float fullVolumeDistance = 2;
        [Min(1)] public float audibleDistance = 22;
        [Min(1)] public float minGrowlInterval = 5;
        [Min(1)] public float maxGrowlInterval = 9;
        [Range(2, 12)] public int voiceLimit = 6;
        [Range(1, 4)] public int growlLimit = 2;
    }
}
