using System.Collections.Generic;
using TMPro;
using UnityEngine;

using UnityEngine.SceneManagement;

namespace DeadZone
{
    // Host owns combat and wave progression; clients render validated world snapshots.
    public sealed class DeadZoneGame : MonoBehaviour
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
        public int TeamSize => Mathf.Max(1, players.FindAll(p => network == null || network.IsParticipantConnected(p.OwnerId)).Count);
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
        int pendingEnemies;float nextSpawn;
        public int PendingEnemies => pendingEnemies;
        public int RemoteEnemyCount;
        public int RemainingEnemies => (IsAuthority?enemies.Count:RemoteEnemyCount)+pendingEnemies;
        public float CurrentBossHealth => IsAuthority?(enemies.Find(e=>e!=null&&e.boss)?.health??0):RemoteBossHealth;
        public bool OptionsVisible {get;set;}
        public bool InputBlocked => paused||OptionsVisible||(network!=null&&network.PanelVisible);
        CityArena arena;
        TMPro.TextMeshProUGUI hud, center, help;
        TMP_FontAsset font;
        GameObject menuRoot, hudRoot, resultShade;
        UnityEngine.UI.Image[] heroCards;
        RectTransform healthFill;
        static readonly Color Accent = new(.35f, .9f, .76f);
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
            gameCamera.orthographicSize = arena != null ? 7.5f : 5.625f;
            BuildHud();
            network = gameObject.AddComponent<ZoneNetwork>();
            network.Initialize(this, menuRoot.transform.parent, font);
            gameObject.AddComponent<ZoneOptions>().Initialize(this,menuRoot.transform.parent,font);
            gameObject.AddComponent<ZoneCombatHud>().Initialize(this,hudRoot.transform,font);
            gameObject.AddComponent<ZoneAllyHud>().Initialize(this,hudRoot.transform,font);gameObject.AddComponent<ZoneMusic>();gameObject.AddComponent<ZoneLanguageStartup>();gameObject.AddComponent<ZoneStatisticsPanel>().Initialize(this,menuRoot.transform.parent,font);
        }

        void Update()
        {
            if(IsAuthority&&running&&!paused&&pendingEnemies>0&&Time.time>=nextSpawn&&enemies.Count<Balance.maxAlive+wave*2)
            {
                int before=enemies.Count;SpawnEnemy((pendingEnemies+wave)%Mathf.Min(3,wave),false);
                if(enemies.Count>before)pendingEnemies--;nextSpawn=Time.time+Balance.spawnInterval/Mathf.Sqrt(TeamSize);
            }
            var keys = ZoneKeyboard.Current;
            if (keys == null) return;
            menuRoot.SetActive(!running && !ended);
            hudRoot.SetActive(running || ended);if(ended){center.fontSize=26;center.text=ResultText(Victory);}else center.fontSize=32; resultShade.SetActive(ended);
            healthFill.localScale = new Vector3(Mathf.Clamp01(player.health / player.maxHealth), 1, 1);
            if (!running && !ended)
            {
                if (!network.Active && !network.PanelVisible && !OptionsVisible)
                {
                    if (keys.digit1Key.wasPressedThisFrame) selected = 0;
                    if (keys.digit2Key.wasPressedThisFrame) selected = 1;
                    if (keys.digit3Key.wasPressedThisFrame) selected = 2;
                    if (keys.digit4Key.wasPressedThisFrame && HeroCount > 3) selected = 3;
                }
                for (int i = 0; i < heroCards.Length; i++) heroCards[i].color = i == selected ? new Color(.12f, .29f, .3f) : new Color(.075f, .1f, .14f);
                center.text = "";
                if (keys.enterKey.wasPressedThisFrame && !network.PanelVisible && !OptionsVisible) Begin();
            }
            else if (ended)
            {
                if (keys.enterKey.wasPressedThisFrame && !OptionsVisible && !network.PanelVisible) { if (network.Active) network.Leave(); else { Time.timeScale = 1; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); } }
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
                hud.text = $"<b>{HeroName(selected)}</b>   HP {Mathf.CeilToInt(player.health)}/{player.maxHealth}\n" +
                    (player.Oni.Active ? ZoneL.Format("<color=#FF75BC>БУЛАВА ОНИ · {0} с</color>",Mathf.CeilToInt(player.Oni.Remaining)) : $"{ZoneL.Get(Weapons[player.weapon])}   {player.ammo} / {(player.weapon == 0 ? "∞" : player.reserve.ToString())}") +
                    (player.Oni.Active ? ZoneL.Get("   ЛКМ — УДАР") : player.Reloading ? ZoneL.Get("   ПЕРЕЗАРЯДКА") : player.ammo == 0 ? ZoneL.Get("   НЕТ ПАТРОНОВ") : "") + ZoneL.Format("\nСпособность: {0}%   Гранаты: {1}   Мины: {2}",Mathf.FloorToInt(player.charge),player.grenades,player.mines) + "\n" + player.PickupHint();
                help.text = ZoneL.Format("<color=#74E5D0>ВОЛНА {0:00} / 05</color>     УБИЙСТВА {1}\n",wave,kills) + (bossPhase ? ZoneL.Format("БОСС: {0}   HP {1}",ZoneL.Get(Bosses[Mathf.Max(0,wave-1)]),Mathf.CeilToInt(CurrentBossHealth)) : ZoneL.Get("Уничтожьте врагов зоны"));
                if (running && !bossPhase && !paused) help.text += ZoneL.Format("\nОСТАЛОСЬ: {0}",RemainingEnemies);
                if (running && player.Dead) center.text = ZoneL.Get("ВЫ ПОГИБЛИ\nВозвращение в начале следующей волны");
                else if (running && player.Downed) center.text = ZoneL.Get("ВЫ РАНЕНЫ\nСоюзник может поднять вас клавишей E");
            }
        }

        void LateUpdate()
        {
            if (player == null) return;
            var offset = Vector2.ClampMagnitude(player.aimPoint - (Vector2)player.transform.position, 4) * .22f;
            var target = player.transform.position + (Vector3)offset;
            target.z = -10;
            if (arena != null) target = arena.ClampCamera(target, gameCamera);
            gameCamera.transform.position = Vector3.Lerp(gameCamera.transform.position, target, 1 - Mathf.Exp(-8 * Time.unscaledDeltaTime)) + ZoneFeedback.CameraOffset();
            if (arena != null) gameCamera.transform.position = arena.ClampCamera(gameCamera.transform.position, gameCamera);
        }

        public void Begin()
        {
            if (!IsAuthority || running) return;
            statisticsRecorded=false;running = true; ended=false; paused=false;OptionsVisible=false;Time.timeScale=1;wave = 1;kills=0;damageDealt=0; startedAt = Time.time;
            foreach (var actor in players) actor.ResetHero(actor.IsLocal ? selected : actor.CharacterIndex);
            center.text = ""; network?.MatchStarted();
            foreach (var actor in players)
            {
                // Guaranteed personal choices before relying on random enemy drops.
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
            SpawnWave();
        }

        void SpawnWave()
        {
            foreach (var actor in players) if ((actor.Dead || actor.Downed) && (network == null || network.IsParticipantConnected(actor.OwnerId))) actor.Revive(true);
            pendingEnemies=Balance.WaveSize(wave,TeamSize);nextSpawn=Time.time+.8f;
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
            var position = FindSpawn();
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
            obj.AddComponent<CircleCollider2D>().radius = .22f;
            var enemy = obj.AddComponent<ZoneEnemy>();
            enemy.game = this; enemy.kind = kind; enemy.boss = boss; enemy.ReadyAt = Time.time + (boss ? 1.8f : .75f);
            ZonePulse.Spawn(position.Value, obj.layer, boss ? 1.3f : .5f, 1, boss ? 1.8f : .75f);
            enemy.health = boss ? Balance.BossHealth(wave,TeamSize) : (Balance.enemyHealthBase+wave*Balance.enemyHealthPerWave)*(kind==2?1.6f:1);
            enemies.Add(enemy);if(boss){var brain=obj.AddComponent<ZoneBossBrain>();brain.Enemy=enemy;}
        }

        Vector2? FindSpawn()
        {
            Vector2 origin = player.transform.position;
            for (int i = 0; i < 80; i++)
            {
                Vector2 point = origin + Random.insideUnitCircle.normalized * Random.Range(6f, 12f);
                if (ZoneMap.Current != null && ZoneMap.Current.Walkable(point, player.gameObject.layer) && ZoneMap.Current.Reachable(point, player.gameObject.layer, player) && players.TrueForAll(p=>p.Dead||Vector2.Distance(p.transform.position,point)>3.5f)) return point;
            }
            if(ZoneMap.Current!=null)
            {
                foreach(var node in ZoneMap.Current.WalkableNodes)
                {
                    Vector2 point=ZoneMap.Current.WorldPoint(node);
                    if(node.z==player.gameObject.layer&&players.TrueForAll(p=>p.Dead||Vector2.Distance(p.transform.position,point)>7)&&ZoneMap.Current.Reachable(point,node.z,player))return point;
                }
            }
            return null;
        }

        public static bool Blocked(Vector2 point, float radius)
        {
            return ZoneMap.Obstacle(point, radius, Instance != null && Instance.player != null ? Instance.player.gameObject.layer : 20);
        }

        public void EnemyKilled(ZoneEnemy enemy)
        {
            if(enemy.boss){ZonePulse.Spawn(enemy.transform.position,enemy.gameObject.layer,3,5,1);ZoneFeedback.Play(11);}
            enemies.Remove(enemy); kills++;
            if (!enemy.boss && Random.value < Balance.lootChance) foreach (var actor in players) { int loot = Random.Range(0, 5); ZonePickup.Spawn(this, enemy.transform.position, loot == 0 ? -1 : loot, actor.OwnerId, floor: enemy.gameObject.layer); }
            if (enemy.boss)
            {
                foreach(var minion in enemies.ToArray())if(minion!=null&&!minion.boss){enemies.Remove(minion);Destroy(minion.gameObject);}
                foreach (var actor in players) ZonePickup.Spawn(this, enemy.transform.position, Random.Range(1, 4), actor.OwnerId, floor: enemy.gameObject.layer);
                breakUntil = Time.time + Balance.breakSeconds; message = "Волна пройдена · собери припасы";
            }
        }

        public void Finish(bool victory)
        {
            finishedElapsed=Time.time-startedAt;running = false; ended = true;
            Victory = victory;
            if(!statisticsRecorded&&player!=null){ZoneProfile.Record(player,victory,Elapsed);statisticsRecorded=true;}
            center.text = ResultText(victory);
        }

        public string ResultText(bool victory)
        {
            string result=ZoneL.Get(victory?"<color=#74E5D0>ПОБЕДА</color>":"<color=#F16E7A>ПОРАЖЕНИЕ</color>")+"\n"+ZoneL.Format("Время: {0:00}:{1:00}",Mathf.FloorToInt(Elapsed/60),Mathf.FloorToInt(Elapsed%60))+"\n";
            foreach(var actor in players)result+="\n"+ZoneL.Format("{0} · Убийства {1} · Урон {2} · Поднято {3}",HeroName(actor.CharacterIndex),actor.PersonalKills,Mathf.RoundToInt(actor.PersonalDamage),actor.Revives);
            return result+"\n\n"+ZoneL.Get("Enter — заново");
        }

        public void Shoot(Vector2 origin, Vector2 direction, float damage, bool hostile, float speed = 16, ZonePlayer owner = null, float range = 32, int floor = -1)
        {
            var obj = new GameObject(hostile ? "EnemyBullet" : "Bullet"); obj.transform.position = origin;
            obj.layer = floor >= 20 ? floor : owner != null ? owner.gameObject.layer : player.gameObject.layer;
            var sr = obj.AddComponent<SpriteRenderer>(); sr.sprite = marker; sr.color = hostile ? Color.red : new Color(1, .9f, .4f); sr.sortingOrder = 200; sr.sortingLayerID = ZoneMap.Sorting(obj.layer);
            obj.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            obj.transform.localScale = new Vector3(.22f, .12f, 1);
            if(!hostile&&!Application.isBatchMode)obj.AddComponent<ZoneTracer>();
            var bullet = obj.AddComponent<ZoneBullet>(); bullet.direction = direction; bullet.damage = damage; bullet.hostile = hostile; bullet.speed = speed; bullet.owner = owner; bullet.range = range;
        }

        public void Blast(Vector2 point, float radius, float damage, ZonePlayer owner = null, int floor = -1, int effect = 0)
        {
            floor = floor >= 20 ? floor : owner != null ? owner.gameObject.layer : player.gameObject.layer;
            foreach (var enemy in enemies.ToArray())
                if (enemy != null && enemy.gameObject.layer == floor && Vector2.Distance(enemy.transform.position, point) < radius && ZoneCombat.Clear(point,enemy.transform.position,floor)) enemy.Hit(damage, owner);
            ZoneFeedback.Play(5);ZoneFeedback.Kick(.15f);
            ZonePulse.Spawn(point, floor, radius, effect, effect == 0 ? .5f : .85f);
        }

        void BuildHud()
        {
            font = TMP_Settings.defaultFontAsset;
            var obj = new GameObject("DeadZoneHUD", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            var canvas = obj.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = obj.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = .5f;
            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            hudRoot = new GameObject("GameHUD", typeof(RectTransform)); hudRoot.transform.SetParent(obj.transform, false); Stretch(hudRoot.GetComponent<RectTransform>());
            resultShade=Panel(hudRoot.transform,"Result shade",Vector2.zero,Vector2.zero,Vector2.zero,new Color(.015f,.025f,.04f,.88f));Stretch(resultShade.GetComponent<RectTransform>());resultShade.SetActive(false);
            Panel(hudRoot.transform, "VitalsPanel", Vector2.zero, new Vector2(28, 56), new Vector2(550, 160), new Color(.025f, .04f, .065f, .9f));
            hud = Label(hudRoot.transform, "Player", Vector2.zero, new Vector2(50, 70), new Vector2(510, 125), 23);
            var track = Panel(hudRoot.transform, "HealthTrack", Vector2.zero, new Vector2(50, 194), new Vector2(500, 5), new Color(.2f, .25f, .3f));
            var fill = Panel(track.transform, "Health", Vector2.zero, Vector2.zero, new Vector2(500, 5), Accent); healthFill = fill.GetComponent<RectTransform>();
            Panel(hudRoot.transform, "WavePanel", Vector2.one, new Vector2(-28, -28), new Vector2(510, 100), new Color(.025f, .04f, .065f, .9f));
            help = Label(hudRoot.transform, "Wave", Vector2.one, new Vector2(-50, -45), new Vector2(465, 75), 24); help.alignment = TextAlignmentOptions.TopRight;
            center = Label(hudRoot.transform, "Message", new Vector2(.5f, .5f), Vector2.zero, new Vector2(1050, 500), 32); center.alignment = TextAlignmentOptions.Center;
            Label(hudRoot.transform, "Controls", Vector2.zero, new Vector2(32, 12), new Vector2(1530, 32), 17).text = "WASD движение   ЛКМ огонь   R перезарядка   SHIFT рывок   Q способность   E подбор   TAB оружие   G граната   F мина" + (arena == null ? "   ПРОБЕЛ спрыгнуть" : "");
            menuRoot = Panel(obj.transform, "CharacterSelect", Vector2.zero, Vector2.zero, Vector2.zero, new Color(.025f, .04f, .065f, .96f)); Stretch(menuRoot.GetComponent<RectTransform>());
            if (arena != null)
            {
                var credits = Label(menuRoot.transform, "Map credits", new Vector2(1,0), new Vector2(-30,20), new Vector2(700,28), 15);
                credits.alignment = TextAlignmentOptions.BottomRight; credits.text = "DEAD ZONE · CITY DISTRICT";
            }
            Label(menuRoot.transform, "Brand", new Vector2(0, 1), new Vector2(70, -45), new Vector2(1000, 100), 70).text = "<b>DEAD <color=#74E5D0>ZONE</color></b>";
            Label(menuRoot.transform, "Subtitle", new Vector2(0, 1), new Vector2(75, -155), new Vector2(1100, 60), 22).text = "Пять волн. Пять боссов. Один шанс выбраться. ";
            Label(menuRoot.transform, "Section", new Vector2(0, 1), new Vector2(75, -240), new Vector2(1000, 55), 20).text = "ВЫБЕРИ ПЕРСОНАЖА  /  " + HeroCount.ToString("00");
            heroCards = new UnityEngine.UI.Image[HeroCount];
            string[] roles = { "ВЫЖИВАНИЕ", "БЛИЖНИЙ БОЙ", "КОНТРОЛЬ", "ПРОКЛЯТИЕ" };
            string[] descriptions = { "Упорство\nМеньше урона при низком HP\n\nВозвращение · Q\nСтраховка от смертельного удара", "Кровь они\nВыносливость при низком HP\n\nФорма они · Q\nРог и булава на 40 секунд", "Бесконечность\nПериодическая блокировка удара\n\nФиолетовый · Q\nДальний пробивающий снаряд", "Король проклятий\n+4 HP за своё убийство\n\nЗловещее святилище · Q\nРубящие атаки в области на 8 секунд" };
            for (int i = 0; i < HeroCount; i++)
            {
                int index = i;
                float width = HeroCount > 3 ? 335 : 450;
                var card = Panel(menuRoot.transform, "Hero " + i, new Vector2(0, .5f), new Vector2(75 + i * (width + 25), -35), new Vector2(width, 340), new Color(.075f, .1f, .14f));
                card.GetComponent<RectTransform>().pivot = new Vector2(0, .5f);
                var image = card.GetComponent<UnityEngine.UI.Image>(); image.raycastTarget = true; heroCards[i] = image;
                var button = card.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image; button.onClick.AddListener(() => { if (network == null || !network.Active) selected = index; });
                Label(card.transform, "Number", new Vector2(0, 1), new Vector2(24, -22), new Vector2(width-110, 40), HeroCount>3?16:18).text = "<color=#74E5D0>0" + (i + 1) + "  /  " + (i < 4 ? roles[i] : "НОВЫЙ ГЕРОЙ") + "</color>";
                Label(card.transform, "Name", new Vector2(0, 1), new Vector2(24, HeroCount>3?-90:-78), new Vector2(width - 48, 60), HeroCount>3?28:34).text = "<b>" + HeroName(i) + "</b>";
                Label(card.transform, "Abilities", new Vector2(0, 1), new Vector2(24, -155), new Vector2(width - (HeroCount>3?48:150), 175), HeroCount>3?18:20).text = i < 4 ? descriptions[i] : "Способности из каталога персонажей";
                if (characterFrames != null && i < HeroCount)
                {
                    var portrait = Panel(card.transform, "Portrait", Vector2.one, new Vector2(-20, HeroCount>3?-15:-120), HeroCount>3?new Vector2(64,84):new Vector2(92,120), Color.white);
                    var imageComponent = portrait.GetComponent<UnityEngine.UI.Image>(); imageComponent.sprite = ZoneActorArt.Frame(this,i,Vector2.down,false); imageComponent.preserveAspect = true;
                }
            }
            var start = Panel(menuRoot.transform, "Start", new Vector2(1, 0), new Vector2(-75, 65), new Vector2(450, 75), Accent);
            var startButton = start.AddComponent<UnityEngine.UI.Button>(); start.GetComponent<UnityEngine.UI.Image>().raycastTarget = true; startButton.targetGraphic = start.GetComponent<UnityEngine.UI.Image>(); startButton.onClick.AddListener(Begin);
            var startText = Label(start.transform, "Label", new Vector2(.5f, .5f), Vector2.zero, new Vector2(430, 65), 25); startText.alignment = TextAlignmentOptions.Center; startText.color = new Color(.025f, .08f, .09f); startText.text = "НАЧАТЬ ВЫЛАЗКУ   →";
            Label(menuRoot.transform, "BuildInfo", Vector2.zero, new Vector2(75, 65), new Vector2(850, 70), 19).text = "ОДИНОЧНЫЙ РЕЖИМ\nВыбор: мышь или 1–4 · Старт: Enter";
            var coop = Panel(menuRoot.transform, "Coop", new Vector2(1, 0), new Vector2(-550, 65), new Vector2(250, 75), new Color(.12f, .2f, .25f));
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
            var rb = obj.AddComponent<Rigidbody2D>(); rb.gravityScale = 0; rb.freezeRotation = true; obj.AddComponent<CircleCollider2D>().radius = .22f;
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
            if(active)statisticsRecorded=false;pendingEnemies=pending;RemoteEnemyCount=alive;startedAt=Time.time-elapsed;wave = currentWave; kills = currentKills; damageDealt = damage; bossPhase = boss; RemoteBossHealth = bossHealth; breakUntil = Time.time + remaining; message = notice;
            running = active; if (finished && !ended) Finish(victory); ended = finished;
        }

        static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        GameObject Panel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image)); obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = anchor; rect.anchoredPosition = position; rect.sizeDelta = size;
            var image = obj.GetComponent<UnityEngine.UI.Image>(); image.color = color; image.raycastTarget = false; return obj;
        }

        TextMeshProUGUI Label(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize)
        {
            var obj = new GameObject(name, typeof(RectTransform)); obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = anchor; rect.anchoredPosition = pos; rect.sizeDelta = size;
            var text = obj.AddComponent<TextMeshProUGUI>(); text.font = font; text.fontSize = fontSize; text.color = Color.white; text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.Normal;
            ZoneL.Attach(text);text.outlineWidth = .2f; text.outlineColor = new Color(0, 0, 0, .8f); return text;
        }

        void OnDestroy() { Time.timeScale = 1; if (Instance == this) Instance = null; }
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
        public int StoredAmmo(int kind) => kind == weapon ? ammo : loaded[kind];
        public int StoredReserve(int kind) => kind == weapon ? reserve : reserves[kind];

        void Start()
        {
            body = GetComponent<Rigidbody2D>(); body.gravityScale = 0; body.freezeRotation = true; body.collisionDetectionMode = CollisionDetectionMode2D.Continuous; body.interpolation = RigidbodyInterpolation2D.Interpolate; animator = GetComponent<Animator>();
            gun = new GameObject("WeaponAim").transform; gun.SetParent(transform, false);
            var visual = new GameObject("Gun"); visual.transform.SetParent(gun, false); visual.transform.localPosition = new Vector3(.35f, .25f);
            var sr = visual.AddComponent<SpriteRenderer>(); gunSprite = sr; sr.sprite = ZoneArsenal.Sprite(weapon); sr.sortingOrder = 100; sr.sortingLayerID = GetComponentInChildren<SpriteRenderer>().sortingLayerID;
            var tip = new GameObject("MuzzleFlash"); tip.transform.SetParent(gun, false); tip.transform.localPosition = new Vector3(.83f, .31f); tip.transform.localScale = new Vector3(.35f, .25f, 1);
            muzzle = tip.AddComponent<SpriteRenderer>(); muzzle.sprite = ZoneArt.MuzzleFlash(0); muzzle.color = Color.white; muzzle.sortingOrder = 110; muzzle.sortingLayerID = sr.sortingLayerID; muzzle.enabled = false;
            var magazine = new GameObject("Reload magazine"); magazine.transform.SetParent(gun, false); magazine.transform.localScale = Vector3.one * .3f;
            reloadMagazine = magazine.AddComponent<SpriteRenderer>(); reloadMagazine.sprite = ZoneArt.Item(3); reloadMagazine.sortingOrder = 101; reloadMagazine.sortingLayerID = sr.sortingLayerID; reloadMagazine.enabled = false;
        }
        public void ResetHero(int index) { foreach(var shrine in FindObjectsByType<ZoneShrine>())if(shrine.Owner==this)Destroy(shrine.gameObject);shrineUntil=0;if(oni!=null)oni.Cancel(); PersonalKills=Revives=0;PersonalDamage=0;revived=false;CharacterIndex = index; hero = passive = index; maxHealth = index == 1 ? 120 : 100; if (game.roster != null && index < game.roster.characters.Length) { var definition = game.roster.characters[index]; hero = (int)definition.activeAbility; passive = (int)definition.passiveAbility; maxHealth = definition.maxHealth; } health = maxHealth; Downed = Dead = false; }
        public void SetCommand(ZoneCommand command) { if (!command.IsFinite) return; command.move = Vector2.ClampMagnitude(command.move, 1); remoteCommand.Merge(command); remoteInputAt = Time.time; }
        void Update()
        {
            var command = IsLocal ? ZoneCommand.Read(game.gameCamera) : (Time.time - remoteInputAt < .5f ? remoteCommand : default);
            remoteCommand.ClearEdges();
            if(IsLocal&&game.InputBlocked)command=new ZoneCommand {aim=aimPoint};
            if (IsLocal || game.IsAuthority) aimPoint = CastKind == 2 && CastRemaining > 0 ? castAimPoint : command.aim;
            if (!game.IsAuthority) { if (hasNetworkPosition) transform.position = Vector2.Lerp(transform.position, networkTarget, 1 - Mathf.Exp(-24 * Time.unscaledDeltaTime)); return; }
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
            if (command.dash && CastRemaining <= 0 && Time.time >= nextDash && move.sqrMagnitude > 0) { ZonePulse.Spawn(transform.position, gameObject.layer, .7f, 8, .35f); dashDirection = move; dashUntil = Time.time + .18f; nextDash = Time.time + 2; }
            if (command.ability && charge >= 100 && !Oni.Active && ShrineRemaining<=0) UseAbility();
            if (command.fire && Oni.Active && CastRemaining <= 0) Oni.TrySwing(aimPoint);
            if (command.fire && !Oni.Active && CastRemaining <= 0 && !Reloading && Time.time >= nextShot && ammo > 0)
            {
                AnimateWeapon();
                var origin = SafeMuzzlePosition();
                var direction = (aimPoint - origin).normalized; if (direction.sqrMagnitude < .01f) direction = gun.right;
                var stats = ZoneArsenal.Stats(weapon);
                for (int i = 0; i < stats.Pellets; i++) { float angle = Random.Range(-stats.Spread, stats.Spread); Vector2 dir = Quaternion.Euler(0, 0, angle) * direction; game.Shoot(origin, dir, stats.Damage, false, stats.Speed, this, stats.Range); }
                ZoneFeedback.Play(weapon);if(IsLocal)ZoneFeedback.Kick(weapon==3?.045f:.015f);
                ammo--; ShotsFired++; flashUntil = Time.time + .055f; nextShot = Time.time + stats.Interval;
            }
            if (command.interact) NearestPickup()?.Take(this);
            if (command.grenade && CastRemaining <= 0 && grenades > 0) { castUntil = Time.time + .25f; CastKind = 3; grenades--; var obj = new GameObject("ThrownGrenade"); var grenade = obj.AddComponent<ZoneGrenade>(); grenade.owner = this; grenade.origin = transform.position; grenade.destination = Vector2.ClampMagnitude(aimPoint - (Vector2)transform.position, 6) + (Vector2)transform.position; ZoneMap.SetFloor(obj, gameObject.layer); }
            if (command.mine && CastRemaining <= 0 && mines > 0) { castUntil = Time.time + .35f; CastKind = 4; mines--; var obj = new GameObject("Mine"); obj.layer = gameObject.layer; obj.transform.position = transform.position; var sr = obj.AddComponent<SpriteRenderer>(); sr.sprite = ZoneArt.Item(1); sr.sortingLayerID = ZoneMap.Sorting(obj.layer); sr.sortingOrder = 20; obj.transform.localScale = Vector3.one * .65f; obj.AddComponent<ZoneMine>().owner = this; }
            if (Time.time - hurtAt > 5) health = Mathf.Min(maxHealth, health + 7 * Time.deltaTime);
        }
        void LateUpdate()
        {
            AnimateWeapon();
            var sr = GetComponent<SpriteRenderer>();
            Vector2 look = aimPoint - (Vector2)transform.position;
            bool walking = game.IsAuthority ? move.sqrMagnitude > .01f : networkMoving;
            sr.sprite = CharacterIndex == 2 && CastKind == 2 && CastRemaining > 0 ? ZoneActorArt.Cast(game,look,CastRemaining) : ZoneActorArt.Frame(game, CharacterIndex, look, walking && !Dead);
            sr.flipX = ZoneActorArt.Flip(CharacterIndex,look);
            float cast = Mathf.Clamp01(CastRemaining / .6f);
            // Renderer-only motion never moves the collider, feet or weapon parent.
            sr.color = Dead ? new Color(1,1,1,.2f) : Downed ? new Color(1,.55f,.55f) : CastRemaining > 0 ? Color.Lerp(Color.white, CastKind == 1 ? Color.cyan : CastKind == 5 ? new Color(1,.2f,.25f) : CastKind == 2 ? new Color(1,.4f,.5f) : new Color(.75f,.5f,1), cast * .45f) : Color.white;
            sr.sortingOrder = 30;
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
            gunSprite.sortingOrder = direction.y > Mathf.Abs(direction.x) ? 25 : 35;
            muzzle.sortingOrder = gunSprite.sortingOrder + 1; muzzle.sprite = ZoneArt.MuzzleFlash(ShotsFired);
            muzzle.transform.position = gunSprite.transform.TransformPoint(new Vector3(tip.x, tip.y * sign));
            muzzle.transform.localScale = new Vector3(weapon == 3 ? .65f : .35f, .20f + recoil * .13f, 1);
            muzzle.enabled = gunSprite.enabled && Time.time < flashUntil && Vector2.Distance(SafeMuzzlePosition(), MuzzlePosition) < .05f;
            reloadMagazine.enabled = gunSprite.enabled && Reloading && progress > .1f && progress < .88f;
            reloadMagazine.sortingOrder = gunSprite.sortingOrder + 1;
            reloadMagazine.sprite = ZoneArt.Item(weapon == 3 ? 4 : weapon == 2 ? 2 : 3);
            float insert = weapon == 3 ? Mathf.Sin(Mathf.Repeat(progress * 6, 1) * Mathf.PI) : reload;
            reloadMagazine.transform.localPosition = new Vector3(weapon == 3 ? .25f + insert * .18f : .28f, (-.07f - insert * (weapon == 0 ? .3f : weapon == 1 ? .5f : weapon == 2 ? .35f : .17f)) * sign);
            reloadMagazine.transform.localRotation = Quaternion.Euler(0, 0, weapon == 1 ? reload * 25 * sign : weapon == 3 ? 90 : 0);
            if (ShotsFired > visualShots) { ZoneDebris.Spawn(EjectionPosition, gameObject.layer, true); if(!game.IsAuthority)ZoneFeedback.Play(weapon); }
            if(Reloading&&!wasReloading)ZoneFeedback.Play(8);if(!Reloading&&wasReloading)ZoneFeedback.Play(9);
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
            foreach (var pickup in FindObjectsByType<ZonePickup>()) if (pickup.Owner == OwnerId && pickup.gameObject.layer == gameObject.layer)
            { float next = Vector2.Distance(transform.position, pickup.transform.position); if (next < distance) { distance = next; nearest = pickup; } }
            return nearest;
        }
        public string PickupHint()
        {
            if (!game.running || Dead || Downed) return "";
            if (game.IsAuthority) { var pickup = NearestPickup(); return pickup == null ? "" : "E — " + ZoneL.Get(ZoneArsenal.LootName(pickup.kind)); }
            ZoneGhost nearest = null; float distance = 1.4f;
            foreach (var ghost in FindObjectsByType<ZoneGhost>()) if (ghost.ObjectType == 3 && ghost.gameObject.layer == gameObject.layer)
            { float next = Vector2.Distance(transform.position, ghost.transform.position); if (next < distance) { nearest = ghost; distance = next; } }
            return nearest == null ? "" : "E — " + ZoneL.Get(ZoneArsenal.LootName(nearest.Kind));
        }
        void UseAbility()
        {
            charge = 0; castAimPoint = aimPoint; castUntil = Time.time + .6f; CastKind = hero;
            if (hero == 0) { returnPoint = transform.position; returnHealth = health; returnUntil = Time.time + 12; ZonePulse.Spawn(returnPoint, gameObject.layer, .9f, 3, 12); }
            else if (hero == 1) { reloadUntil=0; Oni.Activate(); }
            else if(hero==3){CastKind=5;castUntil=Time.time+ZoneShrine.ChargeTime;shrineUntil=Time.time+ZoneShrine.ChargeTime+ZoneShrine.Duration;ZoneShrine.Spawn(this);}
            else { castUntil = Time.time + ZonePurple.ChargeTime; ZonePurple.Spawn(this, aimPoint - ((Vector2)transform.position + Vector2.up * .55f)); }
        }
        public void Hit(float damage)
        {
            if (!game.IsAuthority || !game.running || Downed || Dead || Time.time < dashUntil || Time.time < protectedUntil || Time.time - hurtAt < .4f) return;
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
            shrineUntil=Time.time+state.shrineRemaining;Connected=state.connected;PersonalKills=state.kills;Revives=state.revives;PersonalDamage=state.damage;reviveProgress=state.reviveProgress*2;remoteDownRemaining=state.downRemaining;OwnerId = state.id; CharacterIndex = state.hero; health = state.health; maxHealth = state.maxHealth; charge = state.charge; weapon = state.weapon; ammo = state.ammo; reserve = state.reserve; grenades = state.grenades; mines = state.mines; Downed = state.downed; Dead = state.dead; remoteReloadRemaining = state.reloadRemaining; remoteStateAt = Time.time; castUntil = Time.time + state.castRemaining; CastKind = state.castKind; networkMoving = state.moving; Oni.Apply(state); ZoneMap.SetFloor(gameObject, state.floor);
            networkTarget = state.position; if (!hasNetworkPosition || Vector2.Distance(transform.position, networkTarget) > 3) transform.position = networkTarget; hasNetworkPosition = true; if (!IsLocal || state.castKind == 2 && state.castRemaining > 0) aimPoint = state.aim; castAimPoint = state.aim;
            if (state.shots > ShotsFired) flashUntil = Time.time + .06f; ShotsFired = state.shots;
            GetComponent<Rigidbody2D>().simulated = false;
            var sr = GetComponent<SpriteRenderer>(); sr.sprite = game.CharacterFrame(state.hero, state.moving ? 1 + (Mathf.FloorToInt(Time.time * 8) % 2) : 0); sr.color = Dead ? new Color(1, 1, 1, .2f) : Downed ? new Color(1, .5f, .5f) : Color.white;
        }
    }

    public sealed class ZoneEnemy : MonoBehaviour
    {
        public DeadZoneGame game; public int kind; public bool boss; public float health, ReadyAt;
        Rigidbody2D body; float nextAttack,staggerUntil;Vector2 knockback;
        SpriteRenderer sprite; Color baseColor; float flashUntil, maxHealth;
        Transform healthBar;
        public float MaxHealth=>maxHealth>0?maxHealth:health;
        void Start()
        {
            body = GetComponent<Rigidbody2D>(); sprite = GetComponent<SpriteRenderer>(); baseColor = sprite.color; maxHealth = health;
            var bar = new GameObject("Health"); bar.transform.SetParent(transform, false); bar.transform.localPosition = new Vector3(0, 1.5f); bar.transform.localScale = new Vector3(1, .08f, 1);
            var renderer = bar.AddComponent<SpriteRenderer>(); renderer.sprite = game.marker; renderer.color = boss ? new Color(1, .2f, .45f) : new Color(.9f, .4f, .25f); renderer.sortingOrder = 200; renderer.sortingLayerID = sprite.sortingLayerID; healthBar = bar.transform;
        }
        void Update() { if (sprite != null) { var color = Time.time < flashUntil ? Color.white : baseColor; color.a = Mathf.Clamp01(1 - (ReadyAt - Time.time) / (boss ? 1.8f : .75f)); sprite.color = color; sprite.sprite = game.EnemyFrame(kind, boss); sprite.flipX = body.linearVelocity.x < -.1f; float size = boss ? 1.6f : 1; transform.localScale = new Vector3(size, size * (1 + (body.linearVelocity.sqrMagnitude > .1f ? Mathf.Sin(Time.time * 9) * .025f : 0)), 1); } if(healthBar!=null)healthBar.gameObject.SetActive(game.running&&Time.time>=ReadyAt&&(boss||health<maxHealth)); if (healthBar != null) healthBar.localScale = new Vector3(Mathf.Clamp01(health / maxHealth), .08f, 1); }
        void FixedUpdate()
        {
            GetComponent<Collider2D>().enabled = Time.time >= ReadyAt;
            if (!game.running || !game.IsAuthority || Time.time < ReadyAt) { body.linearVelocity = Vector2.zero; return; }
            if(Time.time<staggerUntil){body.linearVelocity=knockback;return;}
            var target = game.NearestTarget(transform.position); if (target == null) { body.linearVelocity = Vector2.zero; return; }
            Vector2 delta = target.transform.position - transform.position; float distance = delta.magnitude;
            Vector2 direction = ZoneMap.Current != null ? ZoneMap.Current.Direction(body.position, gameObject.layer, target) : delta.normalized;
            if (ZoneStairTravel.Step(gameObject, direction)) return;
            bool sameFloor = target.gameObject.layer == gameObject.layer;
            float speed = boss ? 1.4f + game.wave * .08f : kind == 2 ? .9f : 1.6f;
            body.linearVelocity = sameFloor && distance < (kind == 1 ? 3 : .6f) ? Vector2.zero : direction * speed;
            if (!sameFloor) return;
            if(boss){var brain=GetComponent<ZoneBossBrain>();if(brain!=null){body.linearVelocity=brain.Drive(body.linearVelocity);brain.Tick(target);return;}}
            if (Time.time < nextAttack) return;
            if (distance < .85f) { target.Hit(boss ? 25 : 12); nextAttack = Time.time + 1; }
            else if ((kind == 1 || boss) && distance < 9)
            {
                int count = boss ? 3 + kind * 2 : 1;
                for (int i = 0; i < count; i++) { Vector2 dir = boss && kind >= 2 ? Quaternion.Euler(0, 0, 360f * i / count) * Vector2.right : Quaternion.Euler(0, 0, (i - (count - 1) * .5f) * 14) * delta.normalized; game.Shoot(body.position + dir * .5f, dir, boss ? 15 : 9, true, 5, floor: gameObject.layer); }
                nextAttack = Time.time + (boss ? 1.7f : 2.5f);
            }
        }
        public void Hit(float damage, ZonePlayer owner = null, bool grantCharge = true) { if (!game.IsAuthority || health <= 0 || Time.time<ReadyAt) return; float actual = Mathf.Min(damage, health); health -= damage;
            Vector2 impulse=owner!=null?((Vector2)transform.position-(Vector2)owner.transform.position).normalized:Vector2.up;
            if(!boss){knockback=impulse*2.2f;staggerUntil=Time.time+.085f;}
            ZoneImpact.Spawn(transform.position+Vector3.up*.45f,impulse,gameObject.layer,true);ZoneFeedback.Hit(transform.position,actual,gameObject.layer); flashUntil = Time.time + .09f; game.damageDealt += actual; owner ??= game.player; owner.PersonalDamage += actual; if (grantCharge) owner.charge = Mathf.Min(100, owner.charge + actual * .09f); if (health <= 0) { owner.PersonalKills++;owner.OnEnemyKilled(); if (grantCharge) owner.charge = Mathf.Min(100, owner.charge + 8); game.EnemyKilled(this); Destroy(gameObject); } }
    }

    public sealed class ZoneBullet : MonoBehaviour
    {
        public Vector2 direction; public float damage, speed, range = 32; public bool hostile; public ZonePlayer owner; float travelled;
        void Update()
        {
            var game = DeadZoneGame.Instance; if (game == null || !game.running) { Destroy(gameObject); return; }
            float step = Mathf.Min(speed * Time.deltaTime, range - travelled);
            foreach (var hit in Physics2D.RaycastAll(transform.position, direction, step))
            {
                if (hit.collider.isTrigger) continue;
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
        public int kind;
        public int ammo = -1, reserve = -1;
        public ulong Owner;
        public static void Spawn(DeadZoneGame game, Vector2 point, int kind, ulong owner = 0, int ammo = -1, int reserve = -1, int floor = -1)
        {
            var obj = new GameObject("Pickup " + ZoneArsenal.LootName(kind)); obj.transform.position = point; obj.transform.localScale = Vector3.one * (kind >= 0 && kind < 4 ? 1.2f : .6f);
            obj.layer = floor >= 20 ? floor : game.player.gameObject.layer;
            var sr = obj.AddComponent<SpriteRenderer>(); sr.sprite = kind >= 0 && kind < 4 ? ZoneArsenal.Sprite(kind) : ZoneArt.Item(kind == 4 ? 1 : 2); sr.sortingOrder = 30; sr.sortingLayerID = ZoneMap.Sorting(obj.layer); sr.color = Color.white;
            var pickup = obj.AddComponent<ZonePickup>(); pickup.kind = kind; pickup.Owner = owner; pickup.ammo = ammo; pickup.reserve = reserve;
            if (game.network != null && game.network.Active && owner != game.player.OwnerId) sr.enabled = false;
        }
        public void Take(ZonePlayer player) { if (player.OwnerId != Owner) return; player.Pickup(kind, ammo, reserve); Destroy(gameObject); }
    }
    public sealed class ZoneMine : MonoBehaviour
    {
        public ZonePlayer owner;
        float armedAt;
        void Start() { armedAt = Time.time + 1; }
        void Update() { var game = DeadZoneGame.Instance; if (game == null || !game.IsAuthority || !game.running || Time.time < armedAt) return; GetComponent<SpriteRenderer>().color = Mathf.Sin(Time.time * 7) > 0 ? Color.white : new Color(.8f, 1, .8f); foreach (var enemy in game.enemies.ToArray()) if (enemy != null && enemy.gameObject.layer == gameObject.layer && Vector2.Distance(enemy.transform.position, transform.position) < 1) { game.Blast(transform.position, 2.5f, 150, owner, gameObject.layer); Destroy(gameObject); break; } }
    }
}




