using UnityEngine;
namespace DeadZone
{
    public static class ZoneWeaponAudio
    {
        static readonly string[][] ShotPaths = {
            new[] { "Glock18/glock18_shot" },
            new[] { "AK47/ak47_01", "AK47/ak47_02", "AK47/ak47_04" },
            new[] { "Negev/negev_shot_01", "Negev/negev_shot_02" },
            new[] { "Nova/nova_shot" }
        };
        static readonly string[] ReloadPaths = { "Glock18/glock18_reload", "AK47/ak47_reload", "Negev/negev_reload", "Nova/nova_reload" };
        static AudioClip[][] shots = new AudioClip[4][];
        static AudioClip[] reloads = new AudioClip[4];
        static int[] last = { -1, -1, -1, -1 };
        static AudioClip pump;
        static readonly System.Random random = new System.Random(281);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache() { shots = new AudioClip[4][]; reloads = new AudioClip[4]; last = new[] { -1, -1, -1, -1 }; pump = null; }
        public static AudioClip NextShot(int kind)
        {
            kind = Mathf.Clamp(kind, 0, 3);
            if (shots[kind] == null)
            {
                shots[kind] = new AudioClip[ShotPaths[kind].Length];
                for (int i = 0; i < shots[kind].Length; i++) shots[kind][i] = Resources.Load<AudioClip>("Audio/Weapons/" + ShotPaths[kind][i]);
            }
            int count = shots[kind].Length;
            int index = count == 1 ? 0 : last[kind] < 0 ? random.Next(count) : (last[kind] + 1 + random.Next(count - 1)) % count;
            last[kind] = index;
            return shots[kind][index];
        }
        public static AudioClip Reload(int kind)
        {
            kind = Mathf.Clamp(kind, 0, 3);
            if (reloads[kind] == null) reloads[kind] = Resources.Load<AudioClip>("Audio/Weapons/" + ReloadPaths[kind]);
            return reloads[kind];
        }
        public static AudioClip NovaPump { get { if (pump == null) pump = Resources.Load<AudioClip>("Audio/Weapons/Nova/nova_pump"); return pump; } }
    }
}
