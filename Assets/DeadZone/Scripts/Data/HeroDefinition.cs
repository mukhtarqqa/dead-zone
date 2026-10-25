using UnityEngine;

namespace DeadZone.Data
{
    public enum ActiveAbility { ReturnByDeath, OniRage, HollowPurple, MalevolentShrine }
    public enum PassiveAbility { Determination, OniBlood, Infinity, CurseKing }

    [CreateAssetMenu(menuName = "Dead Zone/Character")]
    public sealed class HeroDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public Sprite portrait;
        public GameObject visualPrefab;
        [Min(1)] public float maxHealth = 100;
        [Min(.1f)] public float moveSpeed = 3.5f;
        public ActiveAbility activeAbility;
        public PassiveAbility passiveAbility;
        [Min(1)] public float damageToCharge = 1000;
    }
}
