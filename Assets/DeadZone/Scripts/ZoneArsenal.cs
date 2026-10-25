using UnityEngine;

namespace DeadZone
{
    // One source of combat values for firing, reloads, loot and presentation.
    public static class ZoneArsenal
    {
        public readonly struct Gun
        {
            public readonly int Magazine, Reserve, Pellets;
            public readonly float Damage, Interval, Reload, Spread, Speed, Range;
            public Gun(int magazine, int reserve, int pellets, float damage, float interval, float reload, float spread, float speed, float range)
            { Magazine = magazine; Reserve = reserve; Pellets = pellets; Damage = damage; Interval = interval; Reload = reload; Spread = spread; Speed = speed; Range = range; }
        }
        static readonly Gun[] Guns = {
            new(12, 0, 1, 22, .25f, 1.25f, 1.5f, 20, 15),
            new(30, 90, 1, 16, .115f, 1.8f, 3, 24, 19),
            new(70, 140, 1, 13, .075f, 3, 6, 22, 18),
            new(6, 24, 8, 10, .8f, 2.2f, 15, 19, 7)
        };
        // Pixel-space barrel tips relative to the actual Sprite pivot, at 32 pixels/unit.
        public static Vector2 Muzzle(int kind) => kind == 0 ? new Vector2(10f/32,1.5f/32) : kind == 1 ? new Vector2(14f/32,2f/32) : kind == 2 ? new Vector2(15f/32,2f/32) : new Vector2(15f/32,3f/32);
        static Sprite[] sprites;
        public static Gun Stats(int kind) => Guns[Mathf.Clamp(kind, 0, Guns.Length - 1)];
        public static string LootName(int kind) => kind < 0 ? "Припасы: патроны и граната" : kind == 4 ? "Мина" : DeadZoneGame.Weapons[Mathf.Clamp(kind, 0, 3)];
        public static Sprite Sprite(int kind)
        {
            if (sprites == null) sprites = new Sprite[4];
            kind = Mathf.Clamp(kind, 0, 3);
            if (sprites[kind] != null) return sprites[kind];
            var texture = new Texture2D(32, 16) { filterMode = FilterMode.Point, name = "DeadZone gun " + kind };
            var pixels = new Color[32 * 16];
            void Rect(int x, int y, int w, int h, Color c) { for (int yy = y; yy < y + h; yy++) for (int xx = x; xx < x + w; xx++) pixels[yy * 32 + xx] = c; }
            Color steel = new(.24f, .3f, .34f), light = new(.55f, .64f, .68f), wood = new(.64f, .32f, .14f), dark = new(.1f, .13f, .16f);
            if (kind == 0) { Rect(9, 7, 15, 5, steel); Rect(10, 3, 5, 5, dark); Rect(10, 11, 12, 1, light); Rect(23, 8, 3, 3, dark); }
            if (kind == 1) { Rect(2, 7, 8, 5, wood); Rect(9, 8, 13, 4, steel); Rect(21, 9, 9, 2, dark); Rect(14, 3, 4, 6, dark); Rect(10, 4, 3, 4, wood); Rect(19, 8, 5, 4, wood); Rect(10, 11, 8, 1, light); }
            if (kind == 2) { Rect(1, 7, 8, 5, dark); Rect(8, 7, 15, 6, steel); Rect(22, 9, 9, 2, dark); Rect(12, 2, 8, 5, new Color(.38f, .43f, .22f)); Rect(9, 4, 3, 4, dark); Rect(11, 12, 12, 1, light); Rect(25, 5, 2, 4, dark); }
            if (kind == 3) { Rect(1, 5, 9, 6, wood); Rect(9, 8, 11, 4, steel); Rect(19, 10, 12, 2, light); Rect(18, 7, 8, 3, wood); Rect(11, 5, 3, 4, dark); }
            texture.SetPixels(pixels); texture.Apply();
            return sprites[kind] = UnityEngine.Sprite.Create(texture, new Rect(0, 0, 32, 16), new Vector2(.5f, .5f), 32);
        }
    }
}
