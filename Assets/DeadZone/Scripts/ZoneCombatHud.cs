using TMPro;
using UnityEngine;
namespace DeadZone
{
    public sealed class ZoneCombatHud : MonoBehaviour
    {
        DeadZoneGame game;RectTransform canvas,crosshair,charge,reload,boss;TextMeshProUGUI ability;
        readonly UnityEngine.UI.Image[] marks=new UnityEngine.UI.Image[4];
        public void Initialize(DeadZoneGame owner,Transform parent,TMP_FontAsset font)
        {
            game=owner;canvas=parent.parent.GetComponent<RectTransform>();
            crosshair=Rect(parent,"Aim",new Vector2(.5f,.5f),Vector2.zero,new Vector2(24,24));
            for(int i=0;i<4;i++){var r=Rect(crosshair,"Aim mark",new Vector2(.5f,.5f),i<2?new Vector2(i==0?-8:8,0):new Vector2(0,i==2?-8:8),i<2?new Vector2(6,2):new Vector2(2,6));marks[i]=r.gameObject.AddComponent<UnityEngine.UI.Image>();marks[i].raycastTarget=false;}
            var panel=Rect(parent,"Ability status",Vector2.zero,new Vector2(625,72),new Vector2(360,76));
            var textObject=new GameObject("Ability",typeof(RectTransform));textObject.transform.SetParent(panel,false);var tr=textObject.GetComponent<RectTransform>();tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=Vector2.zero;tr.offsetMax=Vector2.zero;
            ability=textObject.AddComponent<TextMeshProUGUI>();ability.font=font;ability.fontSize=22;ability.color=Color.white;ability.raycastTarget=false;
            var track=Rect(panel,"Charge track",Vector2.zero,new Vector2(0,10),new Vector2(340,6));track.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(.1f,.2f,.24f,.85f);
            charge=Rect(track,"Charge fill",Vector2.zero,Vector2.zero,new Vector2(340,6));charge.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(.4f,.95f,.8f);
            reload=Rect(panel,"Reload fill",Vector2.zero,new Vector2(0,0),new Vector2(340,3));reload.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(1,.8f,.3f);
            var bossTrack=Rect(parent,"Boss track",new Vector2(.5f,1),new Vector2(0,-35),new Vector2(500,8));bossTrack.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(.2f,.08f,.12f,.85f);
            boss=Rect(bossTrack,"Boss fill",Vector2.zero,Vector2.zero,new Vector2(500,8));boss.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(1,.3f,.45f);
        }
        void LateUpdate()
        {
            if(game==null||game.player==null)return;
            bool aiming=game.running&&!game.InputBlocked&&!game.player.Dead;crosshair.gameObject.SetActive(aiming);Cursor.visible=!aiming;
            if(aiming){RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,Input.mousePosition,null,out var point);crosshair.anchoredPosition=point;bool target=false;foreach(var c in Physics2D.OverlapPointAll(game.player.aimPoint))if(c.GetComponentInParent<ZoneEnemy>()!=null&&c.gameObject.layer==game.player.gameObject.layer)target=true;foreach(var mark in marks)mark.color=target?new Color(1,.35f,.4f):new Color(.7f,1,.93f);}
            var p=game.player;float value=p.Oni.Active?p.Oni.Remaining/ZoneRemRage.Duration:p.charge/100;
            ability.text=p.Oni.Active?$"РЕМ · ОНИ {Mathf.CeilToInt(p.Oni.Remaining)} с":p.CastRemaining>0?"ПОДГОТОВКА СПОСОБНОСТИ":p.charge>=100?"Q · СПОСОБНОСТЬ ГОТОВА":$"Q · ЗАРЯД {Mathf.FloorToInt(p.charge)}%";
            charge.localScale=new Vector3(Mathf.Clamp01(value),1,1);reload.gameObject.SetActive(p.Reloading);reload.localScale=new Vector3(Mathf.Clamp01(1-p.ReloadRemaining/ZoneArsenal.Stats(p.weapon).Reload),1,1);
            boss.parent.gameObject.SetActive(game.running&&game.BossPhase);
            float hp=game.RemoteBossHealth;if(game.IsAuthority){var e=game.enemies.Find(enemy=>enemy!=null&&enemy.boss);hp=e==null?0:e.health;}
            boss.localScale=new Vector3(Mathf.Clamp01(hp/game.Balance.BossHealth(game.wave,game.TeamSize)),1,1);
        }
        static RectTransform Rect(Transform parent,string name,Vector2 anchor,Vector2 pos,Vector2 size){var obj=new GameObject(name,typeof(RectTransform));obj.transform.SetParent(parent,false);var r=obj.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=anchor;r.anchoredPosition=pos;r.sizeDelta=size;return r;}
        void OnDestroy(){Cursor.visible=true;}
    }
}
