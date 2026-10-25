using TMPro;
using UnityEngine;
namespace DeadZone
{
    public sealed class ZoneCombatHud : MonoBehaviour
    {
        DeadZoneGame game;RectTransform canvas,crosshair,charge,reload,boss;TextMeshProUGUI ability,weaponName,ammo,equipment,prompt;
        readonly UnityEngine.UI.Image[] marks=new UnityEngine.UI.Image[4],slotIcons=new UnityEngine.UI.Image[2],slotFrames=new UnityEngine.UI.Image[2];
        public void Initialize(DeadZoneGame owner,Transform parent,TMP_FontAsset font)
        {
            game=owner;canvas=parent.parent.GetComponent<RectTransform>();crosshair=ZoneUi.Rect(parent,"Aim",new Vector2(.5f,.5f),Vector2.zero,new Vector2(24,24));
            for(int i=0;i<4;i++){var r=ZoneUi.Rect(crosshair,"Aim mark",new Vector2(.5f,.5f),i<2?new Vector2(i==0?-8:8,0):new Vector2(0,i==2?-8:8),i<2?new Vector2(6,2):new Vector2(2,6));marks[i]=r.gameObject.AddComponent<UnityEngine.UI.Image>();marks[i].raycastTarget=false;}
            var panel=ZoneUi.Panel(parent,"Ability status",new Vector2(.5f,0),new Vector2(0,28),new Vector2(320,84));
            ability=ZoneUi.Text(panel.transform,"Ability",new Vector2(0,1),new Vector2(20,-12),new Vector2(280,42),font,19);ability.alignment=TextAlignmentOptions.Center;
            var track=ZoneUi.Panel(panel.transform,"Charge track",Vector2.zero,new Vector2(20,17),new Vector2(280,5),new Color(.12f,.22f,.27f));charge=ZoneUi.Panel(track.transform,"Charge fill",Vector2.zero,Vector2.zero,new Vector2(280,5),ZoneUi.Mint).rectTransform;
            var weapon=ZoneUi.Panel(parent,"Weapon status",new Vector2(1,0),new Vector2(-28,28),new Vector2(434,128));
            weaponName=ZoneUi.Text(weapon.transform,"Weapon name",new Vector2(0,1),new Vector2(22,-13),new Vector2(390,30),font,17);weaponName.color=ZoneUi.Muted;
            ammo=ZoneUi.Text(weapon.transform,"Ammunition",Vector2.zero,new Vector2(22,28),new Vector2(215,62),font,42);ammo.alignment=TextAlignmentOptions.BottomLeft;
            equipment=ZoneUi.Text(weapon.transform,"Grenades and mines",new Vector2(1,0),new Vector2(-20,12),new Vector2(185,23),font,16);equipment.alignment=TextAlignmentOptions.BottomRight;equipment.color=ZoneUi.Muted;
            reload=ZoneUi.Panel(weapon.transform,"Reload fill",Vector2.zero,new Vector2(22,14),new Vector2(190,4),ZonePalette.Accent).rectTransform;
            for(int i=0;i<2;i++){slotFrames[i]=ZoneUi.Panel(weapon.transform,"Weapon slot "+i,new Vector2(1,.5f),new Vector2(i==0?-104:-20,4),new Vector2(72,36));slotIcons[i]=ZoneUi.Panel(slotFrames[i].transform,"Weapon icon",new Vector2(.5f,.5f),Vector2.zero,new Vector2(54,27),Color.white);slotIcons[i].preserveAspect=true;}
            prompt=ZoneUi.Text(parent,"Context prompt",new Vector2(.5f,.5f),new Vector2(0,-112),new Vector2(700,42),font,22);prompt.alignment=TextAlignmentOptions.Center;
            var bossTrack=ZoneUi.Panel(parent,"Boss track",new Vector2(.5f,1),new Vector2(0,-112),new Vector2(440,7),new Color(.24f,.08f,.13f));boss=ZoneUi.Panel(bossTrack.transform,"Boss fill",Vector2.zero,Vector2.zero,new Vector2(440,7),new Color(1,.3f,.45f)).rectTransform;
            var mapPanel=ZoneUi.Panel(parent,"District map",new Vector2(1,1),new Vector2(-28,-28),new Vector2(276,262));var mapRect=ZoneUi.Rect(mapPanel.transform,"Map",new Vector2(0,1),new Vector2(10,-42),new Vector2(256,190));mapRect.gameObject.AddComponent<ZoneMinimap>().Initialize(game,mapPanel.transform,font);
            var hint=ZoneUi.Text(parent,"Help shortcut",Vector2.zero,new Vector2(28,5),new Vector2(365,20),font,14);hint.color=ZoneUi.Muted;hint.text="F1 — управление";ZoneL.Attach(hint);
        }
        void LateUpdate()
        {
            if(game==null||game.player==null)return;var p=game.player;bool aiming=Application.isFocused&&game.running&&!game.InputBlocked&&!p.Dead;crosshair.gameObject.SetActive(aiming);Cursor.visible=!aiming;
            if(aiming)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,Input.mousePosition,null,out var point);crosshair.anchoredPosition=point;bool target=false;
                if(game.IsAuthority){foreach(var collider in Physics2D.OverlapPointAll(p.aimPoint))if(collider.GetComponentInParent<ZoneEnemy>()!=null&&collider.gameObject.layer==p.gameObject.layer)target=true;}
                else foreach(var ghost in ZoneGhost.Active)if(ghost!=null&&ghost.ObjectType==1&&ghost.GetComponent<SpriteRenderer>().bounds.Contains(new Vector3(p.aimPoint.x,p.aimPoint.y)))target=true;
                foreach(var mark in marks)mark.color=target?new Color(1,.35f,.4f):ZonePalette.Text;
            }
            float value=p.Oni.Active?p.Oni.Remaining/ZoneRemRage.Duration:p.ShrineRemaining>0?p.ShrineRemaining/(ZoneShrine.Duration+ZoneShrine.ChargeTime):p.charge/100;
            ability.color=ZoneHeroCard.HeroColor(p.CharacterIndex);ability.text=p.ShrineRemaining>0?ZoneL.Format("СВЯТИЛИЩЕ · {0} с",Mathf.CeilToInt(p.ShrineRemaining)):p.Oni.Active?ZoneL.Format("ФОРМА ОНИ · {0} с",Mathf.CeilToInt(p.Oni.Remaining)):p.CastRemaining>0?ZoneL.Get("ПОДГОТОВКА СПОСОБНОСТИ"):p.charge>=100?ZoneL.Get("Q · СПОСОБНОСТЬ ГОТОВА"):ZoneL.Format("Q · ЗАРЯД {0}%",Mathf.FloorToInt(p.charge));charge.localScale=new Vector3(Mathf.Clamp01(value),1,1);charge.GetComponent<UnityEngine.UI.Image>().color=ability.color;
            weaponName.text=p.Oni.Active?ZoneL.Get("БУЛАВА ОНИ · ЛКМ УДАР"):ZoneL.Get(DeadZoneGame.Weapons[p.weapon])+"  ·  "+ZoneL.Get(p.Reloading?"ПЕРЕЗАРЯДКА":p.ammo==0?(p.weapon==0||p.reserve>0?"R · ПЕРЕЗАРЯДИТЬ":"НЕТ ПАТРОНОВ"):"TAB · СМЕНА");
            ammo.color=p.ammo==0&&!p.Oni.Active?new Color(1,.48f,.4f):Color.white;ammo.text=p.Oni.Active?"<size=27>"+ZoneL.Get("БЛИЖНИЙ БОЙ")+"</size>":$"<b>{p.ammo:00}</b><size=21><color=#A5B09B> / {(p.weapon==0?"∞":p.reserve.ToString())}</color></size>";equipment.text=$"G  {p.grenades}    ·    F  {p.mines}";
            for(int i=0;i<2;i++){int kind=p.WeaponInSlot(i);slotIcons[i].enabled=kind>=0;if(kind>=0)slotIcons[i].sprite=ZoneArsenal.Sprite(kind);slotFrames[i].color=i==p.ActiveSlot?ZonePalette.Button:ZonePalette.Panel;}
            reload.gameObject.SetActive(p.Reloading);reload.localScale=new Vector3(Mathf.Clamp01(1-p.ReloadRemaining/ZoneArsenal.Stats(p.weapon).Reload),1,1);prompt.text=p.PickupHint();
            float hp=game.CurrentBossHealth,max=game.Balance.BossHealth(game.wave,game.TeamSize);if(game.IsAuthority){var e=game.enemies.Find(enemy=>enemy!=null&&enemy.boss);if(e!=null)max=e.MaxHealth;}else foreach(var ghost in ZoneGhost.Active)if(ghost!=null&&ghost.ObjectType==1&&ghost.IsBoss){max=ghost.MaxHealth;break;}
            boss.parent.gameObject.SetActive(game.running&&game.BossPhase&&hp>0);boss.localScale=new Vector3(Mathf.Clamp01(hp/Mathf.Max(1,max)),1,1);
        }
        void OnDestroy(){Cursor.visible=true;}
    }
}
