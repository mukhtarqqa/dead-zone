using UnityEngine;
namespace DeadZone.Data
{
    public enum WeaponKind { Pistol, AssaultRifle, MachineGun, Shotgun, Grenade, Mine }

    [CreateAssetMenu(menuName = "Dead Zone/Weapon")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public WeaponKind kind;
        public Sprite icon;
        [Min(0)] public float damage = 15;
        [Min(0.01f)] public float shotsPerSecond = 4;
        [Min(1)] public int magazineSize = 12;
        [Min(0)] public int reserveAmmo = 60;
        public bool infiniteReserve;
        [Min(0.1f)] public float reloadSeconds = 1.5f;
        [Min(1)] public int pellets = 1;
        [Range(0, 90)] public float spreadDegrees;
        [Min(0.1f)] public float range = 20;
        [Min(0)] public float explosionRadius;
    }
}





