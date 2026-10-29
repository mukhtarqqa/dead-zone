using System.Collections.Generic;
using TMPro;
using UnityEngine;

using UnityEngine.SceneManagement;

namespace DeadZone
{
    // Host owns combat and wave progression; clients render validated world snapshots.
    public sealed partial class DeadZoneGame : MonoBehaviour
    {
        public GameObject playerVisual;
        public Camera gameCamera;
        public DeadZone.Data.HeroCatalog roster;
        public int HeroCount => roster != null && roster.characters.Length > 0 ? roster.characters.Length : Heroes.Length;
        public string HeroName(int index) => roster != null && index < roster.characters.Length ? roster.characters[index].displayName : Heroes[index];
        public static DeadZoneGame Instance { get; private set; }
        public readonly List<ZoneEnemy> enemies = new();
        public ZonePlayer player;
        public readonly List<ZonePlayer> players = new();
        public ZoneNetwork network;
        public bool IsAuthority => network == null || !network.ClientOnly;
        public int TeamSize {get{int count=0;foreach(var actor in players)if(network==null||(network.ClientOnly?actor.Connected:network.IsParticipantConnected(actor.OwnerId)))count++;return Mathf.Max(1,count);}}
        public bool BossPhase => bossPhase;
        public bool Victory { get; private set; }
        public float BreakRemaining => Mathf.Max(0, breakUntil - Time.time);
        public float Elapsed => ended?finishedElapsed:Time.time-startedAt;
        public float RemoteBossHealth;
        public int wave, kills;
        public float damageDealt;
        public bool running, ended, paused;
        public int selected;
        public string message;
        float breakUntil, startedAt, finishedElapsed;
        bool bossPhase,statisticsRecorded;
        public ZoneBalance Balance {get;private set;}
        int pendingEnemies,groupRemaining;float nextSpawn;Vector2? spawnCluster;
        public int PendingEnemies => pendingEnemies;
        public int RemoteEnemyCount;
        public int RemainingEnemies => (IsAuthority?enemies.Count:RemoteEnemyCount)+pendingEnemies;
        public float CurrentBossHealth => IsAuthority?(enemies.Find(e=>e!=null&&e.boss)?.health??0):RemoteBossHealth;
        public bool OptionsVisible {get;set;}
        public bool IntroPlaying => intro!=null&&intro.Playing;
        ZoneIntro intro;
        public bool InputBlocked => IntroPlaying||paused||Restarting||OptionsVisible||(network!=null&&network.PanelVisible);
        CityArena arena;ZonePlayer spectated;ZoneRunFlow runFlow;
        public bool Restarting=>runFlow!=null&&runFlow.Restarting;
        public ZonePlayer CameraActor
        {
            get
            {
                if(!running||player==null||!player.Dead)return player;
                bool Eligible(ZonePlayer p)=>p!=null&&!p.Dead&&!p.Downed&&(network==null||(network.ClientOnly?p.Connected:network.IsParticipantConnected(p.OwnerId)));
                if(!Eligible(spectated)){spectated=null;foreach(var actor in players)if(Eligible(actor)){spectated=actor;break;}}
                return spectated!=null?spectated:player;
            }
        }
        public void CycleSpectator(int step)
        {
            if(player==null||!player.Dead)return;int start=players.IndexOf(CameraActor);for(int i=1;i<=players.Count;i++){int index=(start+step*i+players.Count*2)%players.Count;var actor=players[index];if(!actor.Dead&&!actor.Downed&&(network.ClientOnly?actor.Connected:network.IsParticipantConnected(actor.OwnerId))){spectated=actor;return;}}
        }
        public void RestartRun()=>runFlow.Restart();public void ReturnToMenu()=>runFlow.Menu();
        TMPro.TextMeshProUGUI hud, center, help;
        TMP_FontAsset font;
        GameObject menuRoot, hudRoot, resultShade;
        UnityEngine.UI.Image[] heroCards;
        RectTransform healthFill;UnityEngine.UI.Image healthPortrait;
        static readonly Color Accent = ZonePalette.Accent;
        public Sprite marker;
        Sprite[] enemyFrames;
        public Sprite EnemyFrame(int kind, bool boss) => enemyFrames == null ? CharacterFrame(3, 0) : enemyFrames[boss ? Mathf.Clamp(kind + 1, 1, 5) : Mathf.Clamp(kind,0,2)];
        [System.NonSerialized] public Sprite[,] characterFrames;
        public Sprite CharacterFrame(int character, int frame) => character==3&&ZoneActorArt.Sukuna(Vector2.down,frame)!=null?ZoneActorArt.Sukuna(Vector2.down,frame): characterFrames == null ? playerVisual.GetComponent<SpriteRenderer>().sprite : characterFrames[Mathf.Clamp(character, 0, 3), Mathf.Clamp(frame, 0, 2)];
        public static readonly string[] Heroes = { "Субару Нацуки", "Рем", "Годжо Сатору", "Рёмен Сукуна" };
        public static readonly string[] Bosses = { "Охотник", "Стрелок", "Разрушитель", "Призыватель", "Хранитель зоны" };
        public static readonly string[] Weapons = { "Пистолет", "АК-47", "Пулемёт", "Дробовик" };

        void Awake()
        {
            Instance = this; Balance=Resources.Load<ZoneBalance>("DeadZoneBalance");if(Balance==null)Balance=ScriptableObject.CreateInstance<ZoneBalance>();
            arena = FindAnyObjectByType<CityArena>();
            var texture = new Texture2D(8, 8) { filterMode = FilterMode.Point };
            var pixels = new Color[64];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            texture.SetPixels(pixels); texture.Apply();
            marker = Sprite.Create(texture, new Rect(0, 0, 8, 8), new Vector2(.5f, .5f), 16);
            var enemyAtlas = Resources.Load<Texture2D>("DeadZoneEnemies");
            if (enemyAtlas != null)
            {
                enemyAtlas.filterMode = FilterMode.Point; enemyFrames = new Sprite[6]; int w = enemyAtlas.width / 3, h = enemyAtlas.height / 2;
                for (int i = 0; i < 6; i++) enemyFrames[i] = Sprite.Create(enemyAtlas, new Rect(i % 3 * w, (1 - i / 3) * h, w, h), new Vector2(.5f, .12f), h / 1.65f);
            }
            var atlas = Resources.Load<Texture2D>("DeadZoneCharacters");
            if (atlas != null)
            {
                atlas.filterMode = FilterMode.Point; characterFrames = new Sprite[4, 3];
                int width = atlas.width / 4, height = atlas.height / 3;
                for (int column = 0; column < 4; column++) for (int row = 0; row < 3; row++)
                    characterFrames[column, row] = Sprite.Create(atlas, new Rect(column * width, (2 - row) * height, width, height), new Vector2(.5f, .12f), height / 1.5f);
                var visual = playerVisual.GetComponent<SpriteRenderer>(); visual.drawMode = SpriteDrawMode.Simple; visual.sprite = characterFrames[0, 0]; visual.sharedMaterial = new Material(Shader.Find("Sprites/Default")); visual.sortingOrder = 20;
                var animation = playerVisual.GetComponent<Animator>(); if (animation != null) animation.enabled = false;
                foreach (var child in playerVisual.GetComponentsInChildren<SpriteRenderer>()) if (child != visual) child.enabled = false;
            }
            foreach (var behaviour in playerVisual.GetComponentsInChildren<MonoBehaviour>())
                if (behaviour.GetType().Namespace == "Cainos.PixelArtTopDown_Basic") behaviour.enabled = false;
            foreach (var behaviour in gameCamera.GetComponents<MonoBehaviour>())
                if (behaviour.GetType().Name == "CameraFollow") behaviour.enabled = false;
            ZoneActorArt.Frame(this,2,Vector2.down,false); ZoneActorArt.Cast(this,Vector2.down,ZonePurple.ChargeTime);
            player = playerVisual.AddComponent<ZonePlayer>();
            player.game = this;
            gameObject.AddComponent<ZoneMap>();
            players.Add(player);
            gameCamera.orthographic = true;
            gameCamera.orthographicSize = arena != null ? (arena.Bounds.width>=100?6.8f:7.5f) : 5.625f;
            runFlow=gameObject.AddComponent<ZoneRunFlow>();runFlow.Game=this;BuildHud();
            network = gameObject.AddComponent<ZoneNetwork>();
            network.Initialize(this, menuRoot.transform.parent, font);
            gameObject.AddComponent<ZoneOptions>().Initialize(this,menuRoot.transform.parent,font);
            gameObject.AddComponent<ZoneCombatHud>().Initialize(this,hudRoot.transform,font);
            gameObject.AddComponent<ZoneAllyHud>().Initialize(this,hudRoot.transform,font);gameObject.AddComponent<ZoneMusic>();gameObject.AddComponent<ZoneLanguageStartup>();gameObject.AddComponent<ZoneStatisticsPanel>().Initialize(this,menuRoot.transform.parent,font);gameObject.AddComponent<ZoneResultsPanel>().Initialize(this,menuRoot.transform.parent,font);gameObject.AddComponent<ZoneRecoveryHud>().Initialize(this,hudRoot.transform,font);
        }

        void Update()
        {
            if(IntroPlaying){menuRoot.SetActive(false);hudRoot.SetActive(intro.ShowingGame);return;}
            if(IsAuthority&&running&&!paused&&pendingEnemies>0&&Time.time>=nextSpawn&&enemies.Count<Balance.AliveCap(wave,TeamSize))
            {
                if(groupRemaining<=0){groupRemaining=Mathf.Min(pendingEnemies,Balance.GroupSize(wave,TeamSize));spawnCluster=null;}
                int before=enemies.Count;SpawnEnemy(Balance.EnemyRole(wave,pendingEnemies),false);bool spawned=enemies.Count>before;
                if(spawned){pendingEnemies--;groupRemaining--;if(!spawnCluster.HasValue)spawnCluster=enemies[enemies.Count-1].transform.position;}
                nextSpawn=Time.time+(spawned?(groupRemaining>0?Balance.groupMemberInterval:Balance.GroupRest(wave,TeamSize)):.5f);if(groupRemaining<=0)spawnCluster=null;
            }
            var keys = ZoneKeyboard.Current;
            if (keys == null) return;
            menuRoot.SetActive(!running && !ended);
            hudRoot.SetActive(running);center.fontSize=32;resultShade.SetActive(false);if(ended)center.text="";
            healthFill.localScale = new Vector3(Mathf.Lerp(healthFill.localScale.x,Mathf.Clamp01(player.health/player.maxHealth),1-Mathf.Exp(-14*Time.unscaledDeltaTime)),1,1);
            if (!running && !ended)
            {
                if (!network.Active && !network.PanelVisible && !OptionsVisible)
                {
                    if (keys.digit1Key.wasPressedThisFrame) selected = 0;
                    if (keys.digit2Key.wasPressedThisFrame) selected = 1;
                    if (keys.digit3Key.wasPressedThisFrame) selected = 2;
                    if (keys.digit4Key.wasPressedThisFrame && HeroCount > 3) selected = 3;
                }
                for(int i=0;i<heroCards.Length;i++){var card=heroCards[i].GetComponent<ZoneHeroCard>();if(card!=null)card.Selected=i==selected;}
                center.text = "";
                if (keys.enterKey.wasPressedThisFrame && !network.PanelVisible && !OptionsVisible) Begin();
            }
            else if (ended)
            {
                if(keys.enterKey.wasPressedThisFrame&&!OptionsVisible&&!network.PanelVisible)RestartRun();
            }
            else
            {
                if (keys.escapeKey.wasPressedThisFrame && !OptionsVisible) { if (network.Active) network.TogglePanel(); }
                if (paused) center.text = ZoneL.Get("ПАУЗА\n\nEsc — продолжить");
                else
                {
                    center.text = Time.time < breakUntil ? (message.StartsWith("Волна пройдена")?ZoneL.Get("Волна пройдена · собери припасы"):bossPhase?ZoneL.Format("Босс: {0}",ZoneL.Get(Bosses[Mathf.Max(0,wave-1)])):ZoneL.Format("Волна {0}",wave))+"\n"+Mathf.CeilToInt(breakUntil-Time.time) : "";
                    if (IsAuthority && pendingEnemies==0 && enemies.Count == 0 && Time.time >= breakUntil)
                    {
                        if (!bossPhase) SpawnBoss();
                        else if (wave == 5) Finish(true);
                        else { wave++; bossPhase = false; SpawnWave(); }
                    }
                }
            }
            if (player != null)
            {
                hud.text="<size=16><color=#CEC4B3>"+HeroName(player.CharacterIndex)+"</color></size>\n<b>"+Mathf.CeilToInt(player.health)+"</b><size=17> / "+player.maxHealth+" HP</size>";
                if (network != null && network.Active) hud.text += "\n<size=14><color=#94A86B>PING: " + network.CurrentPingMs + " ms</color></size>";
                if(healthPortrait!=null)healthPortrait.sprite=ZoneActorArt.Frame(this,player.CharacterIndex,Vector2.down,false);
                healthFill.GetComponent<UnityEngine.UI.Image>().color=player.health<=player.maxHealth*.3f?new Color(1,.38f,.32f):ZoneUi.Mint;
                help.text=ended?ZoneL.Get(Victory?"ЗОНА ЗАЧИЩЕНА":"ВЫЛАЗКА ЗАВЕРШЕНА"):bossPhase&&CurrentBossHealth>0?ZoneL.Get(Bosses[Mathf.Max(0,wave-1)]):bossPhase?ZoneL.Get("ПЕРЕДЫШКА"):ZoneL.Format("ВОЛНА {0:00} / 05",wave);
                if(running&&!bossPhase)help.text+="\n<size=16>"+ZoneL.Format("ОСТАЛОСЬ {0} · УБИЙСТВА {1}",RemainingEnemies,kills)+"</size>";
                if(running&&player.Dead&&Application.isFocused&&!OptionsVisible){if(Input.GetKeyDown(KeyCode.LeftArrow))CycleSpectator(-1);if(Input.GetKeyDown(KeyCode.RightArrow))CycleSpectator(1);}
            }
        }

        void LateUpdate()
        {
            PruneBossAlerts();
            if (gameCamera == null) return;
            var tracked=CameraActor;
            var target=hasCameraFollowPoint?lastCameraFollowPoint:gameCamera.transform.position;
            if(tracked!=null)
            {
                var offset=Vector2.ClampMagnitude(tracked.aimPoint-(Vector2)tracked.transform.position,4)*.22f;
                target=tracked.transform.position+(Vector3)offset;
                lastCameraFollowPoint=target;hasCameraFollowPoint=true;
            }
            target.z = gameCamera.transform.position.z;
            if (arena != null) target = arena.ClampCamera(target, gameCamera);
            gameCamera.transform.position = AdvanceBossCamera(target, tracked, Time.unscaledDeltaTime) + ZoneFeedback.CameraOffset();
            if (arena != null) gameCamera.transform.position = arena.ClampCamera(gameCamera.transform.position, gameCamera);
        }

        public void Begin()
        {
            if(IntroPlaying)return;
            if(!running&&IsAuthority&&(network==null||!network.Active))
            {
                if(intro==null){intro=gameObject.AddComponent<ZoneIntro>();intro.Game=this;}
                if(intro.Play(StartGameplay))return;
            }
            StartGameplay();
        }
        public void StartGameplay()
        {
            if (!IsAuthority || running) return;
            ResetBossAlerts();
            statisticsRecorded=false;running = true; ended=false; paused=false;OptionsVisible=false;Time.timeScale=1;wave = 1;kills=0;damageDealt=0; startedAt = Time.time;
            for(int i=0;i<players.Count;i++){var actor=players[i];if(network!=null&&!network.IsParticipantConnected(actor.OwnerId))continue;actor.ResetForMatch(actor.IsLocal?selected:actor.CharacterIndex);if(arena!=null){actor.transform.position=arena.TeamSpawn(i);actor.GetComponent<Rigidbody2D>().position=actor.transform.position;}}
            spectated=null;if(arena!=null)gameCamera.transform.position=arena.ClampCamera(new Vector3(player.transform.position.x,player.transform.position.y,-10),gameCamera);center.text = ""; network?.MatchStarted();
            foreach (var actor in players)
            {
                // Legacy arenas keep their original starting racks. The larger city uses exploration caches.
                if(arena!=null&&arena.Bounds.width>=100)continue;
                for (int kind = 1; kind <= 3; kind++)
                {
                    Vector2 origin = actor.transform.position, point = origin;
                    for (int attempt = 0; attempt < 24; attempt++)
                    {
                        float angle = (kind * 120 + attempt * 15) * Mathf.Deg2Rad;
                        Vector2 candidate = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.8f;
                        bool clear = true;
                        for (int step = 1; step <= 6; step++) if (Blocked(Vector2.Lerp(origin, candidate, step / 6f), .24f)) { clear = false; break; }
                        if (clear) { point = candidate; break; }
                    }
                    ZonePickup.Spawn(this, point, kind, actor.OwnerId);
                }
            }
            ZoneSupplyCache.Populate(arena);
            SpawnWave();
        }

        void SpawnWave()
        {
            foreach (var actor in players) if ((actor.Dead || actor.Downed) && (network == null || network.IsParticipantConnected(actor.OwnerId))) actor.Revive(true);
            pendingEnemies=Balance.WaveSize(wave,TeamSize);groupRemaining=0;spawnCluster=null;nextSpawn=Time.time+.8f;
            message = "Волна " + wave;
            breakUntil = Time.time + 2;
        }

        void SpawnBoss()
        {
            bossPhase = true;
            SpawnEnemy(wave - 1, true);
            message = "Босс: " + Bosses[wave - 1]; breakUntil = Time.time + 2;
        }

        public void SpawnEnemy(int kind, bool boss)
        {
            var position = FindSpawn(boss);
            if (!position.HasValue) return;
            var obj = new GameObject(boss ? Bosses[wave - 1] : "Enemy");
            obj.layer = player.gameObject.layer;
            obj.transform.position = position.Value;
            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = EnemyFrame(kind, boss);
            sr.sortingLayerID = playerVisual.GetComponentInChildren<SpriteRenderer>().sortingLayerID;
            sr.sortingOrder = 20;
            sr.color = boss ? Color.white : kind == 1 ? new Color(1, .85f, .6f) : kind == 2 ? new Color(.85f, .7f, 1) : Color.white;
            obj.transform.localScale = Vector3.one * (boss ? 1.6f : 1);
            var body = obj.AddComponent<Rigidbody2D>(); body.gravityScale = 0; body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var feet=obj.AddComponent<CircleCollider2D>();feet.radius=.22f;feet.offset=Vector2.zero;
            var enemy = obj.AddComponent<ZoneEnemy>();
            enemy.game = this; enemy.kind = kind; enemy.boss = boss; enemy.ReadyAt = Time.time + (boss ? 1.8f : .75f);
            ZonePulse.Spawn(position.Value, obj.layer, boss ? 1.3f : .5f, 1, boss ? 1.8f : .75f);
            enemy.health = boss ? Balance.BossHealth(wave,TeamSize) : Balance.EnemyHealth(wave,TeamSize,kind);
            enemies.Add(enemy);if(boss){var brain=obj.AddComponent<ZoneBossBrain>();brain.Enemy=enemy;NotifyBossSpawned(enemy);}
        }

        Vector2? FindSpawn(bool boss)
        {
            if(ZoneMap.Current==null)return null;
            var active=players.FindAll(p=>!p.Dead&&!p.Downed&&(network==null||network.IsParticipantConnected(p.OwnerId)));if(active.Count==0)return null;
            float halfWidth=gameCamera.orthographicSize*Mathf.Max(gameCamera.aspect,16f/9)+2.1f,halfHeight=gameCamera.orthographicSize+2.1f;
            bool Safe(Vector2 point)=>active.TrueForAll(p=>{Vector2 d=point-(Vector2)p.transform.position;return d.sqrMagnitude>100&&(Mathf.Abs(d.x)>halfWidth||Mathf.Abs(d.y)>halfHeight);});
            bool Empty(Vector2 point){foreach(var e in enemies)if(e!=null&&((Vector2)e.transform.position-point).sqrMagnitude<.7f*.7f)return false;return true;}
            for(int attempt=0;attempt<180;attempt++)
            {
                var anchor=active[Random.Range(0,active.Count)];Vector2 point=!boss&&spawnCluster.HasValue&&attempt<60?spawnCluster.Value+Random.insideUnitCircle*3.5f:(Vector2)anchor.transform.position+Random.insideUnitCircle.normalized*Random.Range(14f,boss?21f:24f);
                if(Empty(point)&&Safe(point)&&ZoneMap.Current.Walkable(point,anchor.gameObject.layer)&&!ZoneMap.Obstacle(point,boss?.37f:.28f,anchor.gameObject.layer)&&ZoneMap.Current.Reachable(point,anchor.gameObject.layer,anchor))return point;
            }
            for(int attempt=0;attempt<180;attempt++)
            {
                Vector2 point=ZoneMap.Current.RandomPoint();var nearest=NearestTarget(point);
                if(nearest!=null&&Empty(point)&&Safe(point)&&!ZoneMap.Obstacle(point,boss?.37f:.28f,nearest.gameObject.layer)&&ZoneMap.Current.Reachable(point,nearest.gameObject.layer,nearest))return point;
            }
            return null;
        }

        public static bool Blocked(Vector2 point, float radius)
        {
            return ZoneMap.Obstacle(point, radius, Instance != null && Instance.player != null ? Instance.player.gameObject.layer : 20);
        }

        public void EnemyKilled(ZoneEnemy enemy)
        {
            if(enemy.boss)RemoveBossAlert(enemy.transform);
            if(enemy.boss){ZonePulse.Spawn(enemy.transform.position,enemy.gameObject.layer,3,5,1);ZoneFeedback.Play(11);}
            enemies.Remove(enemy); kills++;
            if (!enemy.boss && Random.value < Balance.lootChance) foreach (var actor in players) { int loot = Random.Range(0, 5); ZonePickup.Spawn(this, enemy.transform.position, loot == 0 ? -1 : loot, actor.OwnerId, floor: enemy.gameObject.layer); }
            if (enemy.boss)
            {
                foreach(var minion in enemies.ToArray())if(minion!=null&&!minion.boss){enemies.Remove(minion);Destroy(minion.gameObject);}
                foreach (var actor in players) ZonePickup.Spawn(this, enemy.transform.position, Random.Range(1, 4), actor.OwnerId, floor: enemy.gameObject.layer);
                if(wave==5){Finish(true);return;}breakUntil = Time.time + Balance.breakSeconds; message = "Волна пройдена · собери припасы";
            }
        }

        public void Finish(bool victory)
        {
            ResetBossAlerts();
            finishedElapsed=Time.time-startedAt;running = false; ended = true;GetComponent<ZoneOptions>().Close();paused=false;Time.timeScale=1;
            Victory = victory;
            if(!statisticsRecorded&&player!=null){ZoneProfile.Record(player,victory,Elapsed);statisticsRecorded=true;}
            center.text = "";
        }

        public string ResultText(bool victory)
        {
            string result=ZoneL.Get(victory?"<color=#94A86B>ПОБЕДА</color>":"<color=#F16E7A>ПОРАЖЕНИЕ</color>")+"\n"+ZoneL.Format("Время: {0:00}:{1:00}",Mathf.FloorToInt(Elapsed/60),Mathf.FloorToInt(Elapsed%60))+"\n";
            foreach(var actor in players)result+="\n"+ZoneL.Format("{0} · Убийства {1} · Урон {2} · Поднято {3}",HeroName(actor.CharacterIndex),actor.PersonalKills,Mathf.RoundToInt(actor.PersonalDamage),actor.Revives);
            return result+"\n\n"+ZoneL.Get("Enter — заново");
        }

        public void Shoot(Vector2 origin, Vector2 direction, float damage, bool hostile, float speed = 16, ZonePlayer owner = null, float range = 32, int floor = -1)
        {
            var obj = new GameObject(hostile ? "EnemyBullet" : "Bullet"); obj.transform.position = origin;
            obj.layer = floor >= 20 ? floor : owner != null ? owner.gameObject.layer : player.gameObject.layer;
            var sr = obj.AddComponent<SpriteRenderer>(); sr.sprite = marker; sr.color = hostile ? Color.red : new Color(1, .9f, .4f); sr.sortingOrder = 3000; sr.sortingLayerID = ZoneMap.Sorting(obj.layer);
            obj.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            obj.transform.localScale = new Vector3(.22f, .12f, 1);
            if(!hostile&&!Application.isBatchMode)obj.AddComponent<ZoneTracer>();
            var bullet = obj.AddComponent<ZoneBullet>(); bullet.direction = direction; bullet.damage = damage; bullet.hostile = hostile; bullet.speed = speed; bullet.owner = owner; bullet.range = range;
        }

        public void Blast(Vector2 point, float radius, float damage, ZonePlayer owner = null, int floor = -1, int effect = 0, bool isMine = false)
        {
            floor = floor >= 20 ? floor : owner != null ? owner.gameObject.layer : player.gameObject.layer;
            foreach (var enemy in enemies.ToArray())
                if (enemy != null && enemy.gameObject.layer == floor && Vector2.Distance(enemy.transform.position, point) < radius && ZoneCombat.Clear(point,enemy.transform.position,floor)) enemy.Hit(damage, owner);
            ZoneExplosiveAudio.Explode(point, floor, isMine);ZoneFeedback.Kick(.15f);
            ZonePulse.Spawn(point, floor, radius, effect, effect == 0 ? .5f : .85f).ExplosionSound = isMine ? 2 : 1;
        }

        void BuildHud()
        {
            font = TMP_Settings.defaultFontAsset;
            var obj = new GameObject("DeadZoneHUD", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            var canvas = obj.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = obj.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1600, 900); scaler.screenMatchMode=UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            hudRoot = new GameObject("GameHUD", typeof(RectTransform)); hudRoot.transform.SetParent(obj.transform, false); Stretch(hudRoot.GetComponent<RectTransform>());
            resultShade=Panel(hudRoot.transform,"Result shade",Vector2.zero,Vector2.zero,Vector2.zero,new Color(.015f,.025f,.04f,.88f));Stretch(resultShade.GetComponent<RectTransform>());resultShade.SetActive(false);
            var vitals=Panel(hudRoot.transform,"VitalsPanel",Vector2.zero,new Vector2(28,28),new Vector2(365,128),ZoneUi.PanelColor);
            healthPortrait=Panel(vitals.transform,"Hero portrait",new Vector2(0,1),new Vector2(22,-12),new Vector2(62,80),Color.white).GetComponent<UnityEngine.UI.Image>();healthPortrait.preserveAspect=true;
            hud=Label(vitals.transform,"Player",Vector2.zero,new Vector2(102,45),new Vector2(240,72),30);
            var track=Panel(vitals.transform,"HealthTrack",Vector2.zero,new Vector2(22,20),new Vector2(320,7),new Color(.15f,.23f,.29f));healthFill=Panel(track.transform,"Health",Vector2.zero,Vector2.zero,new Vector2(320,7),Accent).GetComponent<RectTransform>();
            var wavePanel=Panel(hudRoot.transform,"WavePanel",new Vector2(.5f,1),new Vector2(0,-28),new Vector2(460,78),ZoneUi.PanelColor);
            help=Label(wavePanel.transform,"Wave",new Vector2(.5f,1),new Vector2(0,-12),new Vector2(430,58),22);help.alignment=TextAlignmentOptions.Center;
            center = Label(hudRoot.transform, "Message", new Vector2(.5f, .5f), Vector2.zero, new Vector2(1050, 500), 32); center.alignment = TextAlignmentOptions.Center;
            menuRoot = Panel(obj.transform, "CharacterSelect", Vector2.zero, Vector2.zero, Vector2.zero, new Color(.02f, .035f, .055f, .985f)); Stretch(menuRoot.GetComponent<RectTransform>());
            var backdrop=Panel(menuRoot.transform,"Quarantine checkpoint",Vector2.zero,Vector2.zero,Vector2.zero,Color.white);Stretch(backdrop.GetComponent<RectTransform>());var background=backdrop.GetComponent<UnityEngine.UI.Image>();background.sprite=Resources.Load<Sprite>("DeadZoneUiBackdrop");background.color=new Color(.8f,.86f,.76f,.7f);ZoneQuarantineFrame.Attach(menuRoot,Vector2.zero,true);
            if (arena != null)
            {
                var credits = Label(menuRoot.transform, "Map credits", new Vector2(1,0), new Vector2(-30,20), new Vector2(700,28), 15);
                credits.alignment = TextAlignmentOptions.BottomRight; credits.text = "DEAD ZONE · CITY DISTRICT";
            }
            Label(menuRoot.transform, "Brand", new Vector2(0, 1), new Vector2(75, -40), new Vector2(1000, 100), 76).text = "<b>DEAD <color=#CB5546>ZONE</color></b>";
            Label(menuRoot.transform, "Subtitle", new Vector2(0, 1), new Vector2(78, -145), new Vector2(1100, 38), 22).text = "Пять волн. Пять боссов. Один шанс выбраться. ";
            Label(menuRoot.transform, "Section", new Vector2(0, 1), new Vector2(78, -195), new Vector2(1000, 34), 18).text = "ВЫБЕРИ ПЕРСОНАЖА  /  " + HeroCount.ToString("00");
            var quarantine=Label(menuRoot.transform,"Quarantine stamp",new Vector2(1,1),new Vector2(-75,-92),new Vector2(280,42),19);quarantine.alignment=TextAlignmentOptions.Right;quarantine.color=ZonePalette.Rust;quarantine.text="КАРАНТИН · СЕКТОР 04";
            heroCards = new UnityEngine.UI.Image[HeroCount];
            string[] roles = { "ВЫЖИВАНИЕ", "БЛИЖНИЙ БОЙ", "КОНТРОЛЬ", "ПРОКЛЯТИЕ" };
            string[] descriptions = { "Упорство\nМеньше урона при низком HP\n\nВозвращение · Q\nСтраховка от смертельного удара", "Кровь они\nВыносливость при низком HP\n\nФорма они · Q\nРог и булава на 40 секунд", "Бесконечность\nПериодическая блокировка удара\n\nФиолетовый · Q\nДальний пробивающий снаряд", "Король проклятий\n+4 HP за своё убийство\n\nЗловещее святилище · Q\nРубящие атаки в области на 8 секунд" };
            for (int i = 0; i < HeroCount; i++)
            {
                int index = i;
                float width=Mathf.Min(345,(1450-(HeroCount-1)*22f)/HeroCount),total=HeroCount*width+(HeroCount-1)*22;
                var card=Panel(menuRoot.transform,"Hero "+i,new Vector2(.5f,.5f),new Vector2(-total*.5f+i*(width+22),-15),new Vector2(width,450),new Color(.045f,.075f,.115f));
                card.GetComponent<RectTransform>().pivot=new Vector2(0,.5f);var image=card.GetComponent<UnityEngine.UI.Image>();image.raycastTarget=true;heroCards[i]=image;
                Color accent=ZoneHeroCard.HeroColor(i);var focus=card.AddComponent<ZoneHeroCard>();focus.Accent=accent;focus.Selected=i==selected;
                var button=card.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=image;button.transition=UnityEngine.UI.Selectable.Transition.None;button.onClick.AddListener(()=>{if(network==null||!network.Active){selected=index;ZoneFeedback.Play(7);}});
                Panel(card.transform,"Hero accent",new Vector2(0,1),Vector2.zero,new Vector2(width,4),accent);
                var number=Label(card.transform,"Number",new Vector2(0,1),new Vector2(22,-22),new Vector2(width-44,30),14);number.color=accent;number.text="0"+(i+1)+"  /  "+(i<4?roles[i]:"НОВЫЙ ГЕРОЙ");
                Panel(card.transform,"Portrait backdrop",new Vector2(.5f,1),new Vector2(0,-66),new Vector2(180,180),new Color(accent.r,accent.g,accent.b,.045f));
                var portrait=Panel(card.transform,"Portrait",new Vector2(.5f,1),new Vector2(0,-62),new Vector2(136,184),Color.white);var picture=portrait.GetComponent<UnityEngine.UI.Image>();picture.sprite=ZoneActorArt.Frame(this,i,Vector2.down,false);picture.preserveAspect=true;
                Label(card.transform,"Name",new Vector2(0,1),new Vector2(22,-265),new Vector2(width-44,42),25).text="<b>"+HeroName(i)+"</b>";
                var abilities=Label(card.transform,"Abilities",new Vector2(0,1),new Vector2(22,-308),new Vector2(width-44,136),17);abilities.color=ZonePalette.Muted;abilities.text=i<4?descriptions[i]:"Способности из каталога персонажей";
            }
            var start = Panel(menuRoot.transform, "Start", new Vector2(1, 0), new Vector2(-75, 58), new Vector2(420, 72), Accent);
            var startButton = start.AddComponent<UnityEngine.UI.Button>(); start.GetComponent<UnityEngine.UI.Image>().raycastTarget = true; startButton.targetGraphic = start.GetComponent<UnityEngine.UI.Image>(); startButton.onClick.AddListener(Begin);
            var startText = Label(start.transform, "Label", new Vector2(.5f, .5f), Vector2.zero, new Vector2(400, 65), 24); startText.alignment = TextAlignmentOptions.Center; startText.color = new Color(.025f,.045f,.018f); startText.text = "НАЧАТЬ ВЫЛАЗКУ   →";
            Label(menuRoot.transform, "BuildInfo", Vector2.zero, new Vector2(78, 96), new Vector2(580, 54), 17).text = "1–4 ИГРОКА · 5 ВОЛН · 5 БОССОВ\nВыбор: мышь или 1–4 · Старт: Enter";
            var coop = Panel(menuRoot.transform, "Coop", new Vector2(1, 0), new Vector2(-520, 58), new Vector2(250, 72), new Color(.12f, .2f, .25f));
            var coopButton = coop.AddComponent<UnityEngine.UI.Button>(); coopButton.targetGraphic = coop.GetComponent<UnityEngine.UI.Image>(); coopButton.targetGraphic.raycastTarget = true; coopButton.onClick.AddListener(() => network.TogglePanel());
            var coopText = Label(coop.transform, "Label", new Vector2(.5f, .5f), Vector2.zero, new Vector2(240, 65), 24); coopText.alignment = TextAlignmentOptions.Center; coopText.text = "КООПЕРАТИВ";
        }

        public ZonePlayer CreatePeer(ulong owner, int character)
        {
            var obj = new GameObject("Player " + owner); obj.layer = playerVisual.layer;
            Vector2 origin = player.transform.position;
            Vector2 spawn = origin;
            for (int i = 0; i < 16; i++) { float angle = i * Mathf.PI / 8; Vector2 point = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * .9f; if (!Blocked(point, .25f)) { spawn = point; break; } }
            obj.transform.position = spawn;
            var sr = obj.AddComponent<SpriteRenderer>(); sr.sprite = CharacterFrame(character, 0); sr.sortingLayerID = playerVisual.GetComponent<SpriteRenderer>().sortingLayerID; sr.sortingOrder = 20;
            var rb = obj.AddComponent<Rigidbody2D>(); rb.gravityScale = 0; rb.freezeRotation = true; var feet=obj.AddComponent<CircleCollider2D>();feet.radius=.22f;feet.offset=Vector2.zero;
            var actor = obj.AddComponent<ZonePlayer>(); actor.game = this; actor.IsLocal = false; actor.OwnerId = owner; actor.ResetHero(character); players.Add(actor); return actor;
        }
        public ZonePlayer NearestTarget(Vector2 position)
        {
            ZonePlayer result = null; float distance = float.MaxValue;
            foreach (var actor in players) if (!actor.Dead && !actor.Downed && (network == null || network.IsParticipantConnected(actor.OwnerId))) { float d = Vector2.SqrMagnitude((Vector2)actor.transform.position - position); if (d < distance) { distance = d; result = actor; } }
            return result;
        }
        public void CheckTeamDefeat() { foreach (var actor in players) if (!actor.Dead && !actor.Downed && (network == null || network.IsParticipantConnected(actor.OwnerId))) return; Finish(false); }
        public void ApplyRemoteMatch(bool active, bool finished, bool victory, int currentWave, int currentKills, float damage, bool boss, float bossHealth, float remaining, string notice, int pending=0,int alive=0,float elapsed=0)
        {
            if(active&&!running)ResetBossAlerts();
            if(active)statisticsRecorded=false;pendingEnemies=pending;RemoteEnemyCount=alive;startedAt=Time.time-elapsed;wave = currentWave; kills = currentKills; damageDealt = damage; bossPhase = boss; RemoteBossHealth = bossHealth; breakUntil = Time.time + remaining; message = notice;
            running = active; if (finished && !ended) Finish(victory); ended = finished;
        }

        static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        GameObject Panel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image)); obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = anchor; rect.anchoredPosition = position; rect.sizeDelta = size;
            var image = obj.GetComponent<UnityEngine.UI.Image>(); image.color = ZonePalette.Surface(color); image.raycastTarget = false; ZoneQuarantineFrame.Attach(obj,size); return obj;
        }

        TextMeshProUGUI Label(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize)
        {
            var obj = new GameObject(name, typeof(RectTransform)); obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = anchor; rect.anchoredPosition = pos; rect.sizeDelta = size;
            var text = obj.AddComponent<TextMeshProUGUI>(); text.font = font; text.fontSize = fontSize; text.color = ZonePalette.Text; text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.Normal;
            ZoneL.Attach(text);text.enableAutoSizing=false;return text;
        }

        void OnDestroy() { ResetBossAlerts();Time.timeScale = 1; if (Instance == this) Instance = null; }
    }

    public sealed class ZonePlayer : MonoBehaviour
    {
        public DeadZoneGame game;
        public bool IsLocal = true;
        public ulong OwnerId;
        public int CharacterIndex;
        public bool Downed, Dead;
        public int PersonalKills, Revives;
        public bool Connected=true;
        public float ReviveProgress => Mathf.Clamp01(reviveProgress/2);
        public float DownRemaining => game.IsAuthority?Mathf.Max(0,downUntil-Time.time):Mathf.Max(0,remoteDownRemaining-(Time.time-remoteStateAt));
        float remoteDownRemaining;
        public float PersonalDamage;
        ZoneCommand remoteCommand;
        float remoteInputAt, downUntil, reviveProgress, lastRevive;
        public float health = 100, maxHealth = 100, charge;
        public int weapon, ammo = 12, reserve = 90, grenades = 3, mines = 2;
        public Vector2 aimPoint;
        float remoteReloadRemaining, remoteStateAt;
        float castUntil;
        Vector2 castAimPoint;
        ZoneRemRage oni;
        public ZoneRemRage Oni { get { if(oni==null){oni=GetComponent<ZoneRemRage>();if(oni==null)oni=gameObject.AddComponent<ZoneRemRage>();oni.Actor=this;}return oni; } }
        float shrineUntil;
        public float ShrineRemaining => Mathf.Max(0,shrineUntil-Time.time);
        public void EndShrine(){shrineUntil=0;}
        public void OnEnemyKilled(){if(passive==3&&!Downed&&!Dead)health=Mathf.Min(maxHealth,health+4);}
        public float MoveSpeed => game.roster!=null&&CharacterIndex<game.roster.characters.Length?game.roster.characters[CharacterIndex].moveSpeed:3.5f;
        public float CastRemaining => Mathf.Max(0, castUntil - Time.time);
        public const float DashDuration = .18f;
        public float DashRemaining => Mathf.Max(0, dashUntil - Time.time);
        public Vector2 DashFacing => dashDirection;
        public int CastKind { get; private set; }
        public Vector2 MuzzlePosition => muzzle != null ? (Vector2)muzzle.transform.position : (Vector2)transform.position;
        public Vector2 EjectionPosition => gunSprite != null ? (Vector2)gunSprite.transform.TransformPoint(new Vector3(0, gunSprite.flipY ? -.06f : .06f)) : (Vector2)transform.position;
        public float ReloadRemaining => game != null && !game.IsAuthority ? Mathf.Max(0, remoteReloadRemaining - (Time.time - remoteStateAt)) : Mathf.Max(0, reloadUntil - Time.time);
        public bool Reloading => ReloadRemaining > 0;
        Rigidbody2D body; Animator animator; Vector2 move, dashDirection;
        Transform gun;
        SpriteRenderer reloadMagazine;
        int visualShots;
        bool wasReloading, dropping;
        float nextDrop;
        SpriteRenderer muzzle, gunSprite;
        float flashUntil;
        float nextShot, reloadUntil, dashUntil, nextDash, protectedUntil, hurtAt = -10, infinityAt, returnUntil;
        Vector2 returnPoint; float returnHealth;
        bool revived; int hero, passive;
        Vector2 networkTarget;
        bool hasNetworkPosition;
        int[] owned = { 0, -1 }; int slot;
        readonly int[] magazines = { ZoneArsenal.Stats(0).Magazine, ZoneArsenal.Stats(1).Magazine, ZoneArsenal.Stats(2).Magazine, ZoneArsenal.Stats(3).Magazine };
        readonly int[] loaded = { 12, 30, 70, 6 };
        readonly int[] reserves = { 0, 90, 140, 24 };
        public int ShotsFired { get; private set; }
        public int AbilitySequence { get; private set; }
        public int AbilityKind { get; private set; }
        float abilityAt;
        public float AbilityAge => AbilitySequence > 0 ? Mathf.Max(0, Time.time - abilityAt) : 0;
        public int ActiveSlot=>slot;public int WeaponInSlot(int index)=>owned[Mathf.Clamp(index,0,1)];
        public int StoredAmmo(int kind) => kind == weapon ? ammo : loaded[kind];
        public int StoredReserve(int kind) => kind == weapon ? reserve : reserves[kind];

        void Start()
        {
            gameObject.AddComponent<ZoneWeaponReloadAudio>().Actor = this;
            body = GetComponent<Rigidbody2D>(); body.gravityScale = 0; body.freezeRotation = true; body.collisionDetectionMode = CollisionDetectionMode2D.Continuous; body.interpolation = RigidbodyInterpolation2D.Interpolate; animator = GetComponent<Animator>();GetComponent<Collider2D>().offset=Vector2.zero;ZoneHurtbox.Add(this);
            foreach(var ally in game.players)if(ally!=this){var collider=ally.GetComponent<Collider2D>();if(collider!=null)Physics2D.IgnoreCollision(GetComponent<Collider2D>(),collider);}
            gun = new GameObject("WeaponAim").transform; gun.SetParent(transform, false);
            var visual = new GameObject("Gun"); visual.transform.SetParent(gun, false); visual.transform.localPosition = new Vector3(.35f, .25f);
            var sr = visual.AddComponent<SpriteRenderer>(); gunSprite = sr; sr.sprite = ZoneArsenal.Sprite(weapon); sr.sortingOrder = 100; sr.sortingLayerID = GetComponentInChildren<SpriteRenderer>().sortingLayerID;
            var tip = new GameObject("MuzzleFlash"); tip.transform.SetParent(gun, false); tip.transform.localPosition = new Vector3(.83f, .31f); tip.transform.localScale = new Vector3(.35f, .25f, 1);
            muzzle = tip.AddComponent<SpriteRenderer>(); muzzle.sprite = ZoneArt.MuzzleFlash(0); muzzle.color = Color.white; muzzle.sortingOrder = 110; muzzle.sortingLayerID = sr.sortingLayerID; muzzle.enabled = false;
            var magazine = new GameObject("Reload magazine"); magazine.transform.SetParent(gun, false); magazine.transform.localScale = Vector3.one * .3f;
            reloadMagazine = magazine.AddComponent<SpriteRenderer>(); reloadMagazine.sprite = ZoneArt.Item(3); reloadMagazine.sortingOrder = 101; reloadMagazine.sortingLayerID = sr.sortingLayerID; reloadMagazine.enabled = false;
        }
        public void ResetHero(int index) { GetComponent<ZoneHeroVoice>()?.Stop();AbilitySequence=0; foreach(var shrine in FindObjectsByType<ZoneShrine>())if(shrine.Owner==this)Destroy(shrine.gameObject);shrineUntil=0;if(oni!=null)oni.Cancel(); PersonalKills=Revives=0;PersonalDamage=0;revived=false;CharacterIndex = index; hero = passive = index; maxHealth = index == 1 ? 120 : 100; if (game.roster != null && index < game.roster.characters.Length) { var definition = game.roster.characters[index]; hero = (int)definition.activeAbility; passive = (int)definition.passiveAbility; maxHealth = definition.maxHealth; } health = maxHealth; Downed = Dead = false; }
        public void ResetForMatch(int index)
        {
            StopAllCoroutines();ResetHero(index);charge=0;grenades=3;mines=2;owned[0]=0;owned[1]=-1;slot=weapon=0;
            for(int i=0;i<4;i++){loaded[i]=ZoneArsenal.Stats(i).Magazine;reserves[i]=ZoneArsenal.Stats(i).Reserve;}ammo=loaded[0];reserve=0;
            ShotsFired=visualShots=0;wasReloading=dropping=false;move=dashDirection=Vector2.zero;nextShot=Time.time+.25f;reloadUntil=dashUntil=nextDash=protectedUntil=flashUntil=castUntil=returnUntil=shrineUntil=nextDrop=0;infinityAt=Time.time;hurtAt=Time.time;CastKind=0;reviveProgress=0;remoteCommand=default;
            GetComponent<Collider2D>().enabled=true;var rb=GetComponent<Rigidbody2D>();if(game.IsAuthority)rb.simulated=true;rb.linearVelocity=Vector2.zero;
        }
        public void SetCommand(ZoneCommand command) { if (!command.IsFinite) return; command.move = Vector2.ClampMagnitude(command.move, 1); remoteCommand.Merge(command); remoteInputAt = Time.time; }
        void Update()
        {
            var command = IsLocal ? ZoneCommand.Read(game.gameCamera) : (Time.time - remoteInputAt < .5f ? remoteCommand : default);
            remoteCommand.ClearEdges();
            if(IsLocal&&game.InputBlocked)command=new ZoneCommand {aim=aimPoint};
            if (IsLocal || game.IsAuthority) aimPoint = CastKind == 2 && CastRemaining > 0 ? castAimPoint : command.aim;
            if (!game.IsAuthority) { if (hasNetworkPosition) transform.position = Vector2.Lerp(transform.position, networkTarget, 1 - Mathf.Exp((IsLocal ? -36f : -28f) * Time.unscaledDeltaTime)); return; }
            GetComponent<SpriteRenderer>().color = Dead ? new Color(1, 1, 1, .2f) : Downed ? new Color(1, .5f, .5f) : Color.white;
            if (!game.running || game.paused || Dead || (IsLocal && game.network != null && game.network.PanelVisible)) { move = Vector2.zero; return; }
            move = command.move;

            if (Downed) { move *= .2f; if (Time.time - lastRevive > .2f) reviveProgress = 0; if (Time.time > downUntil) { Downed = false; Dead = true; GetComponent<Collider2D>().enabled = false; game.CheckTeamDefeat(); } return; }
            if (dropping || GetComponent<ZoneStairTravel>()?.Busy == true) { move = Vector2.zero; return; }
            if (command.drop && Time.time >= nextDrop && ZoneMap.Current != null && ZoneMap.Current.DropDestination(transform.position, gameObject.layer, move.sqrMagnitude > .1f ? move : aimPoint - (Vector2)transform.position, out var landing)) StartCoroutine(Drop(landing));
            if (Time.time - lastRevive > .2f) reviveProgress = 0;
            if (command.interactHeld) foreach (var ally in game.players) if (ally != this && ally.Downed && Vector2.Distance(transform.position, ally.transform.position) < 1.5f) { ally.lastRevive = Time.time; ally.reviveProgress += Time.deltaTime; if (ally.reviveProgress >= 2) { ally.Revive(false); Revives++; } break; }
            if (animator != null) { animator.SetBool("IsMoving", move.sqrMagnitude > 0); if (move.sqrMagnitude > 0) animator.SetInteger("Direction", Mathf.Abs(move.x) > Mathf.Abs(move.y) ? (move.x > 0 ? 2 : 3) : (move.y > 0 ? 1 : 0)); }
            if (reloadUntil > 0 && Time.time >= reloadUntil) { int count = weapon == 0 ? magazines[0] - ammo : Mathf.Min(magazines[weapon] - ammo, reserve); ammo += count; if (weapon != 0) reserve -= count; loaded[weapon] = ammo; reserves[weapon] = reserve; reloadUntil = 0; }
            if (command.swap) SwitchWeapon();
            if (!Oni.Active && (command.reload || command.fire && ammo == 0) && !Reloading && ammo < magazines[weapon] && (weapon == 0 || reserve > 0)) reloadUntil = Time.time + ZoneArsenal.Stats(weapon).Reload;
            if (command.dash && CastRemaining <= 0 && Time.time >= nextDash && move.sqrMagnitude > 0) { ZonePulse.Spawn(transform.position, gameObject.layer, .7f, 8, .35f); dashDirection = move; dashUntil = Time.time + DashDuration; nextDash = Time.time + 2; }
            if (command.ability && charge >= 100 && !Oni.Active && ShrineRemaining<=0) UseAbility();
            if (command.fire && Oni.Active && CastRemaining <= 0) Oni.TrySwing(aimPoint);
            if (command.fire && !Oni.Active && CastRemaining <= 0 && !Reloading && Time.time >= nextShot && ammo > 0)
            {
                AnimateWeapon();
                var origin = SafeMuzzlePosition();
                var direction = (aimPoint - origin).normalized; if (direction.sqrMagnitude < .01f) direction = gun.right;
                var stats = ZoneArsenal.Stats(weapon);
                for (int i = 0; i < stats.Pellets; i++) { float angle = Random.Range(-stats.Spread, stats.Spread); Vector2 dir = Quaternion.Euler(0, 0, angle) * direction; game.Shoot(origin, dir, stats.Damage, false, stats.Speed, this, stats.Range); }
                ZoneFeedback.PlayShot(weapon,this);if(IsLocal)ZoneFeedback.Kick(weapon==3?.045f:.015f);
                ammo--; ShotsFired++; flashUntil = Time.time + .055f; nextShot = Time.time + stats.Interval;
            }
            if (command.interact) {var pickup=NearestPickup();if(pickup!=null)pickup.Take(this);else ZoneSupplyCache.Nearest(this)?.Open(this);}
            if (command.grenade && CastRemaining <= 0 && grenades > 0) { castUntil = Time.time + .25f; CastKind = 3; grenades--; var obj = new GameObject("ThrownGrenade"); var grenade = obj.AddComponent<ZoneGrenade>(); grenade.owner = this; grenade.origin = transform.position; grenade.destination = Vector2.ClampMagnitude(aimPoint - (Vector2)transform.position, 6) + (Vector2)transform.position; ZoneMap.SetFloor(obj, gameObject.layer); }
            if (command.mine && CastRemaining <= 0 && mines > 0) { castUntil = Time.time + .35f; CastKind = 4; mines--; var obj = new GameObject("Mine"); obj.layer = gameObject.layer; obj.transform.position = transform.position; var sr = obj.AddComponent<SpriteRenderer>(); sr.sprite = ZoneArt.Item(1); sr.sortingLayerID = ZoneMap.Sorting(obj.layer); sr.sortingOrder = ZoneDepth.Order(transform.position.y)-3; obj.transform.localScale = Vector3.one * .65f; obj.AddComponent<ZoneMine>().owner = this; }
            if (Time.time - hurtAt > 6) health = Mathf.Min(maxHealth, health + 4.5f * Time.deltaTime);
        }
        void LateUpdate()
        {
            AnimateWeapon();
            var sr = GetComponent<SpriteRenderer>();
            Vector2 look = aimPoint - (Vector2)transform.position;
            bool walking = game.IsAuthority ? move.sqrMagnitude > .01f : networkMoving;
            sr.sprite = Oni.Active && ZoneRemArt.Available ? ZoneRemArt.ForActor(this, look, walking)
                : CharacterIndex == 2 && CastKind == 2 && CastRemaining > 0 ? ZoneActorArt.Cast(game,look,CastRemaining) : ZoneActorArt.Frame(game, CharacterIndex, look, walking && !Dead);
            sr.flipX = ZoneActorArt.Flip(CharacterIndex,look);
            float cast = Mathf.Clamp01(CastRemaining / .6f);
            // Renderer-only motion never moves the collider, feet or weapon parent.
            sr.color = Dead ? new Color(1,1,1,.2f) : Downed ? new Color(1,.55f,.55f) : CastRemaining > 0 ? Color.Lerp(Color.white, CastKind == 1 ? Color.cyan : CastKind == 5 ? new Color(1,.2f,.25f) : CastKind == 2 ? new Color(1,.4f,.5f) : new Color(.75f,.5f,1), cast * .45f) : Color.white;
            sr.sortingOrder = ZoneDepth.Order(transform.position.y);
        }
        bool networkMoving;
        public Vector2 SafeMuzzlePosition()
        {
            Vector2 start = transform.position, end = MuzzlePosition;
            int steps = Mathf.CeilToInt(Vector2.Distance(start,end) / .04f);
            Vector2 safe = start;
            for (int i=1; i<=steps; i++) { Vector2 p=Vector2.Lerp(start,end,(float)i/steps); if (ZoneMap.Obstacle(p,.025f,gameObject.layer)) return safe; safe=p; }
            return end;
        }
        public void AnimateWeapon()
        {
            if (gun == null || gunSprite == null || muzzle == null || reloadMagazine == null) return;
            Vector2 hand = (Vector2)transform.position + Vector2.up * .55f;
            var direction = aimPoint - hand; if (direction.sqrMagnitude < .001f) direction = Vector2.right;
            float sign = direction.x < 0 ? -1 : 1;
            float progress = Reloading ? Mathf.Clamp01(1 - ReloadRemaining / ZoneArsenal.Stats(weapon).Reload) : 0;
            float reload = Reloading ? Mathf.Sin(progress * Mathf.PI) : 0;
            float recoil = Mathf.Clamp01((flashUntil - Time.time) / .055f);
            float reloadTilt = weapon == 0 ? 50 : weapon == 1 ? 30 : weapon == 2 ? 12 : 18;
            gun.localPosition = new Vector3(0, .55f + (move.sqrMagnitude > .1f ? Mathf.Sin(Time.time * 12) * .018f : 0));
            gun.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - reload * reloadTilt * sign);
            // Barrel pixels differ by weapon. Put their centre on the aim axis even after flipping.
            Vector2 tip = ZoneArsenal.Muzzle(weapon);
            gunSprite.transform.localPosition = new Vector3(.28f - recoil * (weapon == 3 ? .14f : .065f), -tip.y * sign);
            gunSprite.sprite = ZoneArsenal.Sprite(weapon); gunSprite.flipY = sign < 0;
            gunSprite.enabled = game.running && !Dead && !Downed && CastRemaining <= 0 && !Oni.Active;
            gunSprite.sortingOrder = ZoneDepth.Order(transform.position.y)+(direction.y>Mathf.Abs(direction.x)?-2:2);
            muzzle.sortingOrder = gunSprite.sortingOrder + 1; muzzle.sprite = ZoneArt.MuzzleFlash(ShotsFired);
            muzzle.transform.position = gunSprite.transform.TransformPoint(new Vector3(tip.x, tip.y * sign));
            muzzle.transform.localScale = new Vector3(weapon == 3 ? .65f : .35f, .20f + recoil * .13f, 1);
            muzzle.enabled = gunSprite.enabled && Time.time < flashUntil && Vector2.Distance(SafeMuzzlePosition(), MuzzlePosition) < .05f;
            reloadMagazine.enabled = gunSprite.enabled && Reloading && progress > .1f && progress < .88f;
            reloadMagazine.sortingOrder = gunSprite.sortingOrder + 1;
            reloadMagazine.sprite = ZoneArt.Item(weapon == 3 ? 4 : weapon == 2 ? 2 : 3);
            float insert = weapon == 3 ? Mathf.Sin(Mathf.Repeat(progress * 6, 1) * Mathf.PI) : reload;
            if (weapon == 1 && Reloading)
            {
                float elapsed = ZoneArsenal.Stats(1).Reload - ReloadRemaining;
                insert = elapsed < .82f ? Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.1f, .6f, elapsed))
                    : 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.82f, 1.15f, elapsed));
                reloadMagazine.enabled = gunSprite.enabled && elapsed > .1f && elapsed < 1.16f;
            }
            else if (Reloading && weapon == 0)
            {
                float elapsed = ZoneArsenal.Stats(0).Reload - ReloadRemaining;
                insert = elapsed < .4f ? Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.04f, .30f, elapsed))
                    : 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.4f, .82f, elapsed));
                reloadMagazine.enabled = gunSprite.enabled && elapsed > .04f && elapsed < .84f;
            }
            else if (Reloading && weapon == 2)
            {
                float elapsed = ZoneArsenal.Stats(2).Reload - ReloadRemaining;
                insert = elapsed < .64f ? Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.35f, .58f, elapsed))
                    : 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.64f, 1.42f, elapsed));
                reloadMagazine.enabled = gunSprite.enabled && elapsed > .35f && elapsed < 1.43f;
            }
            else if (Reloading && weapon == 3)
            {
                float elapsed = ZoneArsenal.Stats(3).Reload - ReloadRemaining;
                insert = Mathf.Sin(Mathf.Repeat((elapsed + .17f) / .27f, 1) * Mathf.PI);
                reloadMagazine.enabled = gunSprite.enabled && elapsed < 1.73f;
            }
            reloadMagazine.transform.localPosition = new Vector3(weapon == 3 ? .25f + insert * .18f : .28f, (-.07f - insert * (weapon == 0 ? .3f : weapon == 1 ? .5f : weapon == 2 ? .35f : .17f)) * sign);
            reloadMagazine.transform.localRotation = Quaternion.Euler(0, 0, weapon == 1 ? reload * 25 * sign : weapon == 3 ? 90 : 0);
            if (ShotsFired > visualShots) { ZoneDebris.Spawn(EjectionPosition, gameObject.layer, true); if(!game.IsAuthority)ZoneFeedback.PlayShot(weapon,this); }
            if (Reloading && !wasReloading && weapon != 3) ZoneDebris.Spawn(gun.TransformPoint(new Vector3(.28f,-.1f*sign)), gameObject.layer, false, weapon);
            visualShots = ShotsFired; wasReloading = Reloading;
        }
        System.Collections.IEnumerator Drop(Vector2 landing)
        {
            dropping = true; nextDrop = Time.time + 1; protectedUntil = Time.time + .6f; body.linearVelocity = Vector2.zero; body.simulated = false;
            Vector2 origin = transform.position; float elapsed = 0; int floor = gameObject.layer - 1;
            while (elapsed < .45f && game.running) { elapsed += Time.deltaTime; float t = Mathf.Clamp01(elapsed / .45f); transform.position = Vector2.Lerp(origin, landing, t) + Vector2.up * Mathf.Sin(t * Mathf.PI) * .5f; yield return null; }
            transform.position = landing; body.position = landing; ZoneMap.SetFloor(gameObject, floor); body.simulated = true; dropping = false;
            ZonePulse.Spawn(landing, floor, .45f, 2, .3f);
        }
        void FixedUpdate()
        {
            if (body == null || !game.IsAuthority || dropping) return;
            bool stopped = !game.running || game.paused || Dead || CastKind == 2 && CastRemaining > 0 || (IsLocal && game.network != null && game.network.PanelVisible);
            if (!stopped && !Downed && ZoneStairTravel.Step(gameObject, move)) return;
            body.linearVelocity = stopped ? Vector2.zero : !Downed && Time.time < dashUntil ? dashDirection * 15 : move * MoveSpeed;
        }
        void Equip(int kind) { weapon = kind; ammo = loaded[kind]; reserve = reserves[kind]; reloadUntil = 0; }
        public void SwitchWeapon()
        {
            if (owned[1] < 0) return;
            loaded[weapon] = ammo; reserves[weapon] = reserve; slot = 1 - slot; Equip(owned[slot]);
        }
        public void Pickup(int kind, int pickupAmmo = -1, int pickupReserve = -1)
        {
            if (kind < 0) { reserves[weapon] = reserve; foreach (int carried in owned) if (carried > 0) reserves[carried] += magazines[carried] * 2; reserve = reserves[weapon]; grenades = Mathf.Min(5, grenades + 1); return; }
            if (kind == 4) { mines = Mathf.Min(4, mines + 1); return; }
            if (kind < 0 || kind > 3) return;
            int incomingAmmo = pickupAmmo < 0 ? ZoneArsenal.Stats(kind).Magazine : pickupAmmo;
            int incomingReserve = pickupReserve < 0 ? ZoneArsenal.Stats(kind).Reserve : pickupReserve;
            loaded[weapon] = ammo; reserves[weapon] = reserve;
            if (owned[0] == kind || owned[1] == kind) { if (kind > 0) reserves[kind] += incomingAmmo + incomingReserve; reserve = reserves[weapon]; return; }
            if (owned[1] < 0) slot = 1;
            else ZonePickup.Spawn(game, transform.position, weapon, OwnerId, ammo, reserve, gameObject.layer);
            loaded[kind] = incomingAmmo; reserves[kind] = incomingReserve; owned[slot] = kind; Equip(kind);
        }
        public ZonePickup NearestPickup()
        {
            ZonePickup nearest = null; float distance = 1.4f;
            foreach (var pickup in ZonePickup.Active) if (pickup!=null&&pickup.Owner == OwnerId && pickup.gameObject.layer == gameObject.layer&&ZoneCombat.Clear(transform.position,pickup.transform.position,gameObject.layer))
            { float next = Vector2.Distance(transform.position, pickup.transform.position); if (next < distance) { distance = next; nearest = pickup; } }
            return nearest;
        }
        public string PickupHint()
        {
            if (!game.running || Dead || Downed) return "";
            if (game.IsAuthority) { var pickup = NearestPickup();if(pickup!=null)return "E — "+ZoneL.Get(ZoneArsenal.LootName(pickup.kind));return ZoneSupplyCache.Nearest(this)!=null?ZoneL.Get("E — открыть тайник с припасами"):""; }
            ZoneGhost nearest = null; float distance = 1.4f;
            foreach (var ghost in FindObjectsByType<ZoneGhost>()) if ((ghost.ObjectType == 3 || ghost.ObjectType==10) && ghost.gameObject.layer == gameObject.layer&&ZoneCombat.Clear(transform.position,ghost.transform.position,gameObject.layer))
            { float next = Vector2.Distance(transform.position, ghost.transform.position); if (next < distance) { nearest = ghost; distance = next; } }
            return nearest == null ? "" : nearest.ObjectType==10?ZoneL.Get("E — открыть тайник с припасами"):"E — " + ZoneL.Get(ZoneArsenal.LootName(nearest.Kind));
        }
        void UseAbility()
        {
            charge = 0; castAimPoint = aimPoint; castUntil = Time.time + .6f; CastKind = hero;
            if (hero == 0) { returnPoint = transform.position; returnHealth = health; returnUntil = Time.time + 12; ZonePulse.Spawn(returnPoint, gameObject.layer, .9f, 3, 12); }
            else if (hero == 1) { reloadUntil=0; Oni.Activate(); }
            else if(hero==3){CastKind=5;castUntil=Time.time+ZoneShrine.ChargeTime;shrineUntil=Time.time+ZoneShrine.ChargeTime+ZoneShrine.Duration;ZoneShrine.Spawn(this);}
            else { castUntil = Time.time + ZonePurple.ChargeTime; ZonePurple.Spawn(this, aimPoint - ((Vector2)transform.position + Vector2.up * .55f)); }
            AbilitySequence++; AbilityKind = hero; abilityAt = Time.time;
            ZoneHeroVoice.Play(this, AbilityKind);
        }
        public void Hit(float damage)
        {
            if (!game.IsAuthority || !game.running || game.paused || Downed || Dead || Time.time < dashUntil || Time.time < protectedUntil || Time.time - hurtAt < .4f) return;
            if (passive == 2 && Time.time >= infinityAt) { infinityAt = Time.time + 10; ZonePulse.Spawn((Vector2)transform.position + Vector2.up * .5f, gameObject.layer, .85f, 6, .65f); return; }
            if (passive == 0 && health < maxHealth * .3f) damage *= .7f;
            if (passive == 1 && health < maxHealth * .4f) damage *= .8f;
            ZoneFeedback.Hit(transform.position,Mathf.Min(damage,health),gameObject.layer,true);health = Mathf.Max(0,health-damage); hurtAt = Time.time;
            if (health > 0) return;
            if (Time.time < returnUntil) { ZonePulse.Spawn(transform.position, gameObject.layer, 1.1f, 7, .8f); transform.position = returnPoint; body.position = returnPoint; ZonePulse.Spawn(returnPoint, gameObject.layer, 1.3f, 7, .8f); health = Mathf.Max(35, returnHealth); returnUntil = 0; }
            else if (game.players.Count == 1 && !revived) { revived = true; ZonePulse.Spawn(transform.position, gameObject.layer, 1, 9, 1); health = maxHealth * .5f; protectedUntil = Time.time + 2; }
            else if (game.players.Count > 1) { health = 0; Downed = true; downUntil = Time.time + 20; game.CheckTeamDefeat(); }
            else {health=0;Dead=true;GetComponent<Collider2D>().enabled=false;game.Finish(false);}
        }
        public void Revive(bool newWave) { ZonePulse.Spawn(transform.position, gameObject.layer, .8f, 9, .7f); Downed = Dead = false; health = newWave ? maxHealth : maxHealth * .4f; reviveProgress = 0; GetComponent<Collider2D>().enabled = true; protectedUntil = Time.time + 1; dashUntil = 0; }
        public void ApplyNetworkState(ZonePlayerState state)
        {
            dashUntil = Time.time + Mathf.Clamp(state.dashRemaining, 0, DashDuration);
            dashDirection = state.dashDirection.sqrMagnitude > .001f ? state.dashDirection.normalized : Vector2.down;
            shrineUntil=Time.time+state.shrineRemaining;Connected=state.connected;PersonalKills=state.kills;Revives=state.revives;PersonalDamage=state.damage;reviveProgress=state.reviveProgress*2;remoteDownRemaining=state.downRemaining;OwnerId = state.id; CharacterIndex = state.hero; health = state.health; maxHealth = state.maxHealth; charge = state.charge; weapon = state.weapon; ammo = state.ammo; reserve = state.reserve;owned[0]=state.weaponA;owned[1]=state.weaponB;slot=state.activeSlot; grenades = state.grenades; mines = state.mines; Downed = state.downed; Dead = state.dead; remoteReloadRemaining = state.reloadRemaining; remoteStateAt = Time.time; castUntil = Time.time + state.castRemaining; CastKind = state.castKind; networkMoving = state.moving; Oni.Apply(state); ZoneMap.SetFloor(gameObject, state.floor);
            networkTarget = state.position; if (!hasNetworkPosition || Vector2.Distance(transform.position, networkTarget) > 3) transform.position = networkTarget; hasNetworkPosition = true; if (!IsLocal || state.castKind == 2 && state.castRemaining > 0) aimPoint = state.aim; castAimPoint = state.aim;
            if(state.shots<ShotsFired){visualShots=0;wasReloading=false;}if (state.shots > ShotsFired) flashUntil = Time.time + .06f; ShotsFired = state.shots;
            GetComponent<Rigidbody2D>().simulated = false;
            var sr = GetComponent<SpriteRenderer>(); sr.sprite = game.CharacterFrame(state.hero, state.moving ? 1 + (Mathf.FloorToInt(Time.time * 8) % 2) : 0); sr.color = Dead ? new Color(1, 1, 1, .2f) : Downed ? new Color(1, .5f, .5f) : Color.white;
            bool newAbility = state.abilitySequence > 0 && state.abilitySequence != AbilitySequence;
            AbilitySequence = state.abilitySequence; AbilityKind = state.abilityKind; abilityAt = Time.time - state.abilityAge;
            if (newAbility) ZoneHeroVoice.Play(this, AbilityKind, state.abilityAge);
        }
    }

    public sealed class ZoneBullet : MonoBehaviour
    {
        public Vector2 direction; public float damage, speed, range = 32; public bool hostile; public ZonePlayer owner; float travelled;
        void Update()
        {
            var game = DeadZoneGame.Instance; if (game == null || !game.running) { Destroy(gameObject); return; }if(game.paused)return;
            float step = Mathf.Min(speed * Time.deltaTime, range - travelled);
            foreach (var hit in Physics2D.RaycastAll(transform.position, direction, step))
            {
                if (hit.collider.isTrigger && hit.collider.GetComponent<ZoneHurtbox>()==null) continue;
                var player = hit.collider.GetComponentInParent<ZonePlayer>(); var enemy = hit.collider.GetComponentInParent<ZoneEnemy>();
                int floor = hit.collider.gameObject.layer;
                if (floor >= 20 && floor <= 22 && floor != gameObject.layer) continue;
                if (hostile ? enemy != null : player != null) continue;
                if (hostile && player != null) player.Hit(damage);
                else if (!hostile && enemy != null) enemy.Hit(damage, owner);
                ZoneImpact.Spawn(hit.point,hit.normal,gameObject.layer,enemy!=null);
                Destroy(gameObject); return;
            }
            transform.position += (Vector3)direction * step; travelled += step; if (travelled >= range) Destroy(gameObject);
        }
    }
    public sealed class ZonePickup : MonoBehaviour
    {
        public static readonly List<ZonePickup> Active=new();float born;SpriteRenderer sprite;
        public int kind;
        public int ammo = -1, reserve = -1;
        public ulong Owner;
        public static void Spawn(DeadZoneGame game, Vector2 point, int kind, ulong owner = 0, int ammo = -1, int reserve = -1, int floor = -1)
        {
            int count=0;ZonePickup oldest=null;foreach(var item in Active)if(item!=null&&item.Owner==owner){count++;if(oldest==null||item.born<oldest.born)oldest=item;}if(count>=72&&oldest!=null){Active.Remove(oldest);Destroy(oldest.gameObject);}
            var obj = new GameObject("Pickup " + ZoneArsenal.LootName(kind)); obj.transform.position = point; obj.transform.localScale = Vector3.one * (kind >= 0 && kind < 4 ? 1.2f : .6f);
            obj.layer = floor >= 20 ? floor : game.player.gameObject.layer;
            var sr = obj.AddComponent<SpriteRenderer>(); sr.sprite = kind >= 0 && kind < 4 ? ZoneArsenal.Sprite(kind) : ZoneArt.Item(kind == 4 ? 1 : 2); sr.sortingOrder = ZoneDepth.Order(point.y)-3; sr.sortingLayerID = ZoneMap.Sorting(obj.layer); sr.color = Color.white;
            var pickup = obj.AddComponent<ZonePickup>(); pickup.kind = kind; pickup.Owner = owner; pickup.ammo = ammo; pickup.reserve = reserve;pickup.born=Time.time;pickup.sprite=sr;Active.Add(pickup);
            if (game.network != null && game.network.Active && owner != game.player.OwnerId) sr.enabled = false;
        }
        void Update(){float remaining=120-(Time.time-born);if(remaining<=0){Destroy(gameObject);return;}if(remaining<5&&sprite!=null){var c=sprite.color;c.a=Mathf.Clamp01(remaining/5);sprite.color=c;}}
        void OnDestroy(){Active.Remove(this);}
        public void Take(ZonePlayer player) { if (player.OwnerId != Owner) return; player.Pickup(kind, ammo, reserve); Destroy(gameObject); }
    }
    public sealed class ZoneMine : MonoBehaviour
    {
        public ZonePlayer owner;
        float armedAt;
        public float Age => armedAt > 0 ? Mathf.Max(0, Time.time - armedAt + 1) : 0;
        void Start() { armedAt = Time.time + 1; ZoneExplosiveAudio.Attach(gameObject, true); }
        void Update() { var game = DeadZoneGame.Instance; if (game == null || !game.IsAuthority || !game.running || game.paused || Time.time < armedAt) return; GetComponent<SpriteRenderer>().color = Mathf.Sin(Time.time * 7) > 0 ? Color.white : new Color(.8f, 1, .8f); foreach (var enemy in game.enemies.ToArray()) if (enemy != null && enemy.gameObject.layer == gameObject.layer && Vector2.Distance(enemy.transform.position, transform.position) < 1) { game.Blast(transform.position, 2.5f, 150, owner, gameObject.layer, 0, true); Destroy(gameObject); break; } }
    }
}




