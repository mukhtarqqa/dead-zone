using UnityEngine;

namespace DeadZone.Data
{
    [CreateAssetMenu(menuName = "Dead Zone/Character Roster")]
    public sealed class HeroCatalog : ScriptableObject
    {
        public HeroDefinition[] characters = System.Array.Empty<HeroDefinition>();
    }
}
