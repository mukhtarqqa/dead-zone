using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DeadZone
{
    public sealed class ZoneRunFlow : MonoBehaviour
    {
        public DeadZoneGame Game;public bool Restarting{get;private set;}
        public void Restart()
        {
            if(Restarting||Game==null||!Game.ended||Game.FinalePlaying||!Game.IsAuthority)return;StartCoroutine(Prepare());
        }
        IEnumerator Prepare()
        {
            Restarting=true;Game.GetComponent<ZoneOptions>().Close();Time.timeScale=1;
            Clear<ZoneEnemy>();Clear<ZoneBullet>();Clear<ZonePickup>();Clear<ZoneMine>();Clear<ZoneGrenade>();Clear<ZonePulse>();Clear<ZonePurple>();Clear<ZoneShrine>();Clear<ZoneCorpse>();Clear<ZoneSupplyCache>();Clear<ZoneDebris>();Clear<ZoneImpact>();Clear<ZoneFloatingNumber>();Game.enemies.Clear();
            yield return null;Game.Begin();Restarting=false;
        }
        static void Clear<T>()where T:Component{foreach(var item in FindObjectsByType<T>())Destroy(item.gameObject);}
        public void Menu(){Time.timeScale=1;if(Game.network.Active)Game.network.Leave();else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);}
    }
}
