using UnityEngine;
namespace DeadZone
{
    [CreateAssetMenu(menuName="Dead Zone/Game balance")]
    public sealed class ZoneBalance : ScriptableObject
    {
        [Header("Waves")][Min(1)] public int enemiesBase=24,enemiesPerWave=20,maxAlive=22;
        [Min(.1f)] public float spawnInterval=2.2f;
        [Header("Pressure groups")][Min(1)]public int groupBase=4;
        [Min(.08f)]public float groupMemberInterval=.22f;
        [Min(1)] public float bossHealthBase=1100,bossHealthPerWave=380;
        [Min(1)] public float enemyHealthBase=52,enemyHealthPerWave=10;
        [Range(0,1)]public float lootChance=.32f;
        [Min(1)]public float breakSeconds=12;
        public int WaveSize(int wave,int players)=>Mathf.RoundToInt((enemiesBase+enemiesPerWave*wave)*(1+.8f*(Mathf.Clamp(players,1,4)-1)));
        public int GroupSize(int wave,int players)=>Mathf.RoundToInt((groupBase+Mathf.Max(0,wave-1)/2)*(1+.65f*(Mathf.Clamp(players,1,4)-1)));
        public float GroupRest(int wave,int players)=>Mathf.Max(1,(groupBase+Mathf.Max(0,wave-1)/2)*spawnInterval-(GroupSize(wave,players)-1)*groupMemberInterval);
        public float BossHealth(int wave,int players)=>(bossHealthBase+bossHealthPerWave*wave)*(1+.65f*(Mathf.Clamp(players,1,4)-1));
        public int AliveCap(int wave,int players)=>Mathf.Min(60,maxAlive+wave*2+(Mathf.Clamp(players,1,4)-1)*5);
        public float EnemyHealth(int wave,int players,int role)=>(enemyHealthBase+wave*enemyHealthPerWave)*(role==3?2.8f:role==2?.72f:role==1?.95f:1)*(1+.1f*(Mathf.Clamp(players,1,4)-1));
        public int EnemyRole(int wave,int sequence)
        {
            int n=Mathf.Abs(sequence*7+wave*3)%20;
            return wave==1?(n<4?2:0):n<3?3:n<7?1:n<12?2:0;
        }
    }
}
