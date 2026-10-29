using UnityEngine;
namespace DeadZone
{
    [System.Serializable]
    public sealed class IntroActorArt
    {
        public string id;
        public Sprite[] poses;
        public float referenceHeight;
    }
    public sealed class ZoneIntroDefinition : ScriptableObject
    {
        public Sprite[] backgrounds;
        public IntroActorArt[] actors;
        public bool Ready
        {
            get
            {
                if(backgrounds==null||backgrounds.Length<3||actors==null||actors.Length<5)return false;
                foreach(var bg in backgrounds)if(bg==null)return false;
                foreach(var actor in actors){if(actor==null||actor.poses==null||actor.poses.Length==0)return false;foreach(var pose in actor.poses)if(pose==null)return false;}
                return true;
            }
        }
    }
}
