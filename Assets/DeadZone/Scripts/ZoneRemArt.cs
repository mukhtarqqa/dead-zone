using UnityEngine;

namespace DeadZone
{
    public enum RemOniMotion { Idle, Run, Attack, Dash }

    // The imported sheets use Up, Down, Left, Right, independently of the base hero atlas.
    public static class ZoneRemArt
    {
        public const int CellSize = 192;
        public const float PixelsPerUnit = 56;
        public static readonly int[] FrameCounts = { 4, 6, 6, 4 };
        static readonly string[] Names = { "Idle", "Run", "Attack", "Dash" };
        static readonly Sprite[][] Sheets = new Sprite[4][];
        static bool loaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetCache()
        {
            loaded = false;
            System.Array.Clear(Sheets, 0, Sheets.Length);
        }

        public static bool Available { get { Load(); return Sheets[0] != null; } }
        static void Load()
        {
            if (loaded) return;
            loaded = true;
            // Treat the set atomically: a missing sheet must retain the old working presentation.
            for (int s = 0; s < 4; s++)
            {
                var sprites = Resources.LoadAll<Sprite>("RemOni" + Names[s]);
                if (sprites.Length != FrameCounts[s] * 4)
                {
                    for (int i = 0; i < 4; i++) Sheets[i] = null;
                    return;
                }
                System.Array.Sort(sprites, (a, b) => string.CompareOrdinal(a.name, b.name));
                Sheets[s] = sprites;
            }
        }

        public static int Direction(Vector2 direction)
        {
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y)) return direction.x < 0 ? 2 : 3;
            return direction.y > 0 ? 0 : 1;
        }

        public static Sprite Frame(RemOniMotion motion, Vector2 direction, int frame)
        {
            if (!Available) return null;
            int s = (int)motion;
            return Sheets[s][Direction(direction) * FrameCounts[s] + Mathf.Clamp(frame, 0, FrameCounts[s] - 1)];
        }

        public static int AttackFrame(float remaining)
        {
            float elapsed = ZoneRemRage.SwingDuration - remaining;
            // Pose 3 is the contact pose, synchronized to authoritative damage resolution.
            return elapsed < ZoneRemRage.ImpactDelay
                ? Mathf.Clamp(Mathf.FloorToInt(elapsed / ZoneRemRage.ImpactDelay * 3), 0, 2)
                : 3 + Mathf.Clamp(Mathf.FloorToInt((elapsed - ZoneRemRage.ImpactDelay) /
                    (ZoneRemRage.SwingDuration - ZoneRemRage.ImpactDelay) * 3), 0, 2);
        }

        public static Sprite ForActor(ZonePlayer actor, Vector2 look, bool moving)
        {
            if (actor.DashRemaining > 0)
                return Frame(RemOniMotion.Dash, actor.DashFacing,
                    Mathf.Clamp(Mathf.FloorToInt((1 - actor.DashRemaining / ZonePlayer.DashDuration) * 4), 0, 3));
            if (actor.Oni.SwingRemaining > 0)
                return Frame(RemOniMotion.Attack, actor.Oni.SwingDirection, AttackFrame(actor.Oni.SwingRemaining));
            return Frame(moving ? RemOniMotion.Run : RemOniMotion.Idle, look,
                Mathf.FloorToInt(Time.time * (moving ? 10 : 5)) % (moving ? 6 : 4));
        }
    }
}
