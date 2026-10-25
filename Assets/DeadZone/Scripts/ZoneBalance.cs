using UnityEngine;
namespace DeadZone
{
    [CreateAssetMenu(menuName="Dead Zone/Game balance")]
    public sealed class ZoneBalance : ScriptableObject
    {
        [Header("Waves")][Min(1)] public int enemiesBase=12,enemiesPerWave=8,maxAlive=18;
        [Min(.1f)] public float spawnInterval=1.1f;
        [Min(1)] public float bossHealthBase=600,bossHealthPerWave=260;
        [Min(1)] public float enemyHealthBase=36,enemyHealthPerWave=14;
        [Range(0,1)]public float lootChance=.32f;
        [Min(1)]public float breakSeconds=12;
        public int WaveSize(int wave,int players)=>Mathf.RoundToInt((enemiesBase+enemiesPerWave*wave)*(1+.55f*(Mathf.Clamp(players,1,4)-1)));
        public float BossHealth(int wave,int players)=>(bossHealthBase+bossHealthPerWave*wave)*(1+.65f*(Mathf.Clamp(players,1,4)-1));
    }
}
