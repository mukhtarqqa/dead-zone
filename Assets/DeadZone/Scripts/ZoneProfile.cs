using System;
using UnityEngine;
namespace DeadZone
{
    [Serializable] public sealed class ZoneHeroStatistics
    {
        public int matches,wins,kills,revives;
        public float damage,seconds,bestWinSeconds;
        public void Add(bool victory,int matchKills,float matchDamage,int matchRevives,float duration)
        {
            matches++;if(victory)wins++;kills+=Mathf.Max(0,matchKills);damage+=Mathf.Max(0,matchDamage);revives+=Mathf.Max(0,matchRevives);seconds+=Mathf.Max(0,duration);
            if(victory&&duration>0&&(bestWinSeconds<=0||duration<bestWinSeconds))bestWinSeconds=duration;
        }
    }
    public static class ZoneProfile
    {
        public static ZoneHeroStatistics Read(int hero)
        {
            try{return JsonUtility.FromJson<ZoneHeroStatistics>(PlayerPrefs.GetString("dz.stats.v1."+hero,"{}"))??new ZoneHeroStatistics();}
            catch(ArgumentException){return new ZoneHeroStatistics();}
        }
        public static void Record(ZonePlayer actor,bool victory,float duration)
        {
            var stats=Read(actor.CharacterIndex);stats.Add(victory,actor.PersonalKills,actor.PersonalDamage,actor.Revives,duration);
            PlayerPrefs.SetString("dz.stats.v1."+actor.CharacterIndex,JsonUtility.ToJson(stats));PlayerPrefs.Save();
        }
    }
}
