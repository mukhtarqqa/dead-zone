using UnityEngine;
namespace DeadZone
{
    public static class ZonePalette
    {
        public static readonly Color Graphite=new(.035f,.045f,.038f,.97f),Panel=new(.075f,.092f,.075f),Button=new(.18f,.23f,.16f),Text=new(.87f,.89f,.83f),Muted=new(.66f,.71f,.62f),Accent=new(.58f,.66f,.42f),Sage=new(.53f,.72f,.42f),Crimson=new(.86f,.24f,.22f),Rust=new(.68f,.3f,.22f);
        public static Color Surface(Color source)
        {
            if(source.b>source.r*1.12f&&source.maxColorComponent<.5f){float value=source.maxColorComponent;return new Color(value*.73f,value*.9f,value*.7f,source.a);}return source;
        }
    }
}
