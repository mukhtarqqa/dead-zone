using UnityEngine;
namespace DeadZone
{
    public sealed class ZoneOccluder : MonoBehaviour
    {
        public float FeetY;public bool Fade=true;SpriteRenderer sprite;Color original;
        void Start(){sprite=GetComponent<SpriteRenderer>();original=sprite.color;sprite.sortingOrder=ZoneDepth.Order(FeetY);}
        void LateUpdate()
        {
            if(sprite==null||!Fade)return;var game=DeadZoneGame.Instance;if(game==null)return;
            bool overlaps=false;var bounds=sprite.bounds;
            foreach(var actor in game.players)if(!actor.Dead&&actor.transform.position.y>FeetY+.15f&&bounds.Contains(actor.transform.position+Vector3.up*.6f)){overlaps=true;break;}
            if(!overlaps)foreach(var enemy in game.enemies)if(enemy!=null&&enemy.transform.position.y>FeetY+.15f&&bounds.Contains(enemy.transform.position+Vector3.up*.6f)){overlaps=true;break;}
            if(!overlaps&&game.network!=null&&game.network.ClientOnly)foreach(var ghost in ZoneGhost.Active)if(ghost!=null&&ghost.ObjectType==1&&ghost.transform.position.y>FeetY+.15f&&bounds.Contains(ghost.transform.position+Vector3.up*.6f)){overlaps=true;break;}
            var color=original;color.a=Mathf.Lerp(sprite.color.a,overlaps?.32f:original.a,1-Mathf.Exp(-10*Time.unscaledDeltaTime));sprite.color=color;
        }
    }
}
