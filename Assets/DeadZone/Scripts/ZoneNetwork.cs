using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadZone
{
    [Serializable] public sealed class ZoneHello { public int protocol = 7; public string token; public int hero; }
    [Serializable] public sealed class ZonePlayerState
    {
        public ulong id;
        public int hero, weapon, ammo, reserve, grenades, mines, shots, floor, castKind, maceSwings;
        public float health, maxHealth, charge, reloadRemaining, castRemaining, oniRemaining, maceRemaining;
        public Vector2 position, aim, maceDirection;
        public bool moving, downed, dead, connected, reloading;
    }
    [Serializable] public sealed class ZoneObjectState
    {
        public int id, type, kind, floor; public Vector2 position, velocity, origin; public float age, endAge; public float scale, health, remaining; public bool boss, hostile;
    }
    [Serializable] public sealed class ZoneWorld
    {
        public uint sequence;
        public bool running, ended, victory, boss;
        public int wave, kills, pending, alive;
        public float damage, bossHealth, remaining, elapsed;
        public string notice;
        public ZonePlayerState[] players;
        public ZoneObjectState[] objects;
    }

    public sealed class ZoneNetwork : MonoBehaviour
    {
        const string InputMessage = "dz.input.v1", WorldMessage = "dz.world.v1";
        const ushort Port = 7777;
        DeadZoneGame game;
        NetworkManager manager;
        UnityTransport transport;
        ISession session;
        ZoneNetworkUI ui;
        string token;
        bool handlersRegistered, leaving;
        float nextSend, connectDeadline;
        uint sequence, receivedSequence;
        ZoneCommand pendingInput;
        static int nextObjectId;
        bool smokeClient;
        bool expectReject, smokeReconnect;
        float smokeStarted, smokeOriginX;
        int worldsReceived;
        bool sawOni, sawMaceSwing, sawOniRestore;
        bool sawClock, sawRem, sawGojo, sawShield, sawPurpleCharge, sawPurpleFlight, sawPurpleImpact, smokeMineSent;
        bool smokeEffectsSent, smokeReloadSent, smokeSawMine, smokeSawGrenade, smokeSawPulse, smokeSawReload, smokeSawFloor;
        readonly List<Peer> peers = new();
        readonly Dictionary<int, GameObject> ghosts = new();
        readonly Dictionary<ulong, ZonePlayer> clientActors = new();
        sealed class Peer { public ulong id; public string token; public int hero; public bool connected; public ZonePlayer actor; }
        public bool Active => manager != null && manager.IsListening;
        public bool ClientOnly => manager != null && manager.IsClient && !manager.IsServer;
        public bool IsHost => manager != null && manager.IsHost;
        public bool PanelVisible => ui != null && ui.Visible;
        public bool Busy { get; private set; }
        public string Status { get; private set; } = "Выбери героя, затем создай комнату или подключись.";
        public string RoomCode => session == null ? "" : session.Code;
        public int MemberCount => IsHost ? peers.FindAll(p => p.connected).Count : clientActors.Count;
        public bool IsParticipantConnected(ulong id) => !Active || !IsHost || peers.Exists(p => p.id == id && p.connected);

        public void Initialize(DeadZoneGame owner, Transform canvas, TMPro.TMP_FontAsset font)
        {
            game = owner;
            Application.runInBackground = true;
            string key = "dz.token." + Profile();
            token = PlayerPrefs.GetString(key, "");
            if (string.IsNullOrEmpty(token)) { token = Guid.NewGuid().ToString("N"); PlayerPrefs.SetString(key, token); PlayerPrefs.Save(); }
            ui = gameObject.AddComponent<ZoneNetworkUI>(); ui.Initialize(this, game, canvas, font);
        }
        public void TogglePanel() => ui.SetVisible(!ui.Visible);
        public void MatchStarted() { if (Active) ui.SetVisible(false); }
        public string MemberSummary()
        {
            var lines = new List<string>();
            if (IsHost) foreach (var peer in peers) lines.Add(game.HeroName(peer.hero) + (peer.connected ? "  ·  в комнате" : "  ·  отключён"));
            else foreach (var actor in clientActors.Values) lines.Add(game.HeroName(actor.CharacterIndex));
            return string.Join("\n", lines);
        }
        static string Profile()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-dz-profile") return args[i + 1];
            return "default";
        }
        System.Collections.IEnumerator Start()
        {
            yield return null;
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-dz-hero" && int.TryParse(args[i + 1], out int hero)) game.selected = Mathf.Clamp(hero, 0, game.HeroCount - 1);
            smokeClient = Array.IndexOf(args, "-dz-smoke") >= 0;
            expectReject = Array.IndexOf(args, "-dz-expect-reject") >= 0;
            smokeReconnect = Array.IndexOf(args, "-dz-reconnect-test") >= 0;
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-dz-join") { ui.SetVisible(true); JoinLan(args[i + 1]); }
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-dz-join-code") { ui.SetVisible(true); JoinInternet(args[i + 1]); }
            if (Array.IndexOf(args, "-dz-host") >= 0) HostLan();
        }
        static int ObjectId(GameObject obj)
        {
            var identity = obj.GetComponent<ZoneIdentity>();
            if (identity == null) { identity = obj.AddComponent<ZoneIdentity>(); identity.Id = ++nextObjectId; }
            return identity.Id;
        }

        void Prepare(bool host)
        {
            if (Active || Busy) throw new InvalidOperationException("Сначала выйди из текущей комнаты.");
            if (manager == null)
            {
                var obj = new GameObject("DeadZone Network");
                transport = obj.AddComponent<UnityTransport>();
                manager = obj.AddComponent<NetworkManager>();
            }
            manager.ConnectionApprovalCallback = null;
            manager.OnServerStarted -= RegisterHandlers;
            manager.OnClientConnectedCallback -= Connected;
            manager.OnClientDisconnectCallback -= Disconnected;
            manager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport, EnableSceneManagement = false,
                ConnectionApproval = true, ForceSamePrefabs = false,
                ConnectionData = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new ZoneHello { token = token, hero = game.selected })),
                TickRate = 30
            };
            manager.ConnectionApprovalCallback = Approve;
            manager.OnServerStarted += RegisterHandlers;
            manager.OnClientConnectedCallback += Connected;
            manager.OnClientDisconnectCallback += Disconnected;
            peers.Clear(); handlersRegistered = false; leaving = false; sequence = receivedSequence = 0;
            game.player.OwnerId = 0; game.player.CharacterIndex = game.selected;
            if (host) peers.Add(new Peer { id = 0, token = token, hero = game.selected, actor = game.player, connected = true });
            connectDeadline = Time.realtimeSinceStartup + 20;
        }
        public void HostLan()
        {
            try { Prepare(true); transport.SetConnectionData("127.0.0.1", Port, "0.0.0.0"); if (!manager.StartHost()) throw new Exception("Не удалось открыть порт 7777."); RegisterHandlers(); Status = "Комната LAN создана. Друзья подключаются по IP этого компьютера."; }
            catch (Exception error) { Status = error.Message; }
        }
        public void JoinLan(string address)
        {
            try
            {
                if (!System.Net.IPAddress.TryParse(address.Trim(), out var ip) || ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) throw new Exception("Введи IPv4-адрес хоста, например 192.168.1.10.");
                Prepare(false); transport.SetConnectionData(address.Trim(), Port); if (!manager.StartClient()) throw new Exception("Не удалось начать подключение."); RegisterHandlers(); Status = "Подключение к " + address.Trim() + "…";
            }
            catch (Exception error) { Status = error.Message; }
        }

        async Task InitializeCloud()
        {
            if (string.IsNullOrWhiteSpace(Application.cloudProjectId)) throw new Exception("Для игры по интернету сначала привяжи Dead Zone к Unity Cloud в Project Settings → Services. LAN уже доступна.");
            if (UnityServices.State != ServicesInitializationState.Initialized) await UnityServices.InitializeAsync(new InitializationOptions().SetProfile(Profile()));
            if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        public async void HostInternet()
        {
            if (Busy || Active) return;
            try
            {
                Prepare(true); Busy = true; Status = "Создание интернет-комнаты…";
                await InitializeCloud();
                session = await MultiplayerService.Instance.CreateSessionAsync(new SessionOptions { Name = "Dead Zone", Type = "dead-zone-v1", MaxPlayers = Mathf.Min(4, game.HeroCount), IsPrivate = true }.WithRelayNetwork());
                if (this == null) return;
                RegisterHandlers(); Status = "Комната создана. Код: " + session.Code;
            }
            catch (Exception error) { Status = error.Message; if (manager != null && manager.IsListening) manager.Shutdown(); }
            finally { Busy = false; }
        }
        public async void JoinInternet(string code)
        {
            if (Busy || Active) return;
            try
            {
                if (string.IsNullOrWhiteSpace(code)) throw new Exception("Введи код комнаты.");
                Prepare(false); Busy = true; Status = "Вход в интернет-комнату…";
                await InitializeCloud();
                string requestedCode = code.Trim().ToUpperInvariant();
                if (session != null && session.Code != requestedCode) await session.LeaveAsync();
                session = null;
                string storedId = PlayerPrefs.GetString("dz.session.id." + Profile(), "");
                string storedCode = PlayerPrefs.GetString("dz.session.code." + Profile(), "");
                if (!string.IsNullOrEmpty(storedId) && storedCode == requestedCode)
                {
                    var joined = await MultiplayerService.Instance.GetJoinedSessionIdsAsync();
                    if (joined.Contains(storedId)) session = await MultiplayerService.Instance.ReconnectToSessionAsync(storedId, new ReconnectSessionOptions { Type = "dead-zone-v1" });
                }
                if (session == null) session = await MultiplayerService.Instance.JoinSessionByCodeAsync(requestedCode, new JoinSessionOptions { Type = "dead-zone-v1" });
                PlayerPrefs.SetString("dz.session.id." + Profile(), session.Id); PlayerPrefs.SetString("dz.session.code." + Profile(), requestedCode); PlayerPrefs.Save();
                if (this == null) return;
                RegisterHandlers(); Status = "Подключено. Ждём старта хоста.";
            }
            catch (Exception error) { Status = error.Message; if (manager != null && manager.IsListening) manager.Shutdown(); }
            finally { Busy = false; }
        }

        void Approve(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            response.CreatePlayerObject = false; response.Pending = false;
            if (request.ClientNetworkId == NetworkManager.ServerClientId) { response.Approved = true; return; }
            try
            {
                if (request.Payload.Length > 512) throw new Exception("Некорректные данные подключения.");
                var hello = JsonUtility.FromJson<ZoneHello>(Encoding.UTF8.GetString(request.Payload));
                if (hello == null || hello.protocol != 7 || hello.hero < 0 || hello.hero >= game.HeroCount || hello.token == null || hello.token.Length != 32) throw new Exception("Версии игры не совпадают.");
                var returning = peers.Find(p => p.token == hello.token);
                if (returning != null)
                {
                    if (returning.connected) throw new Exception("Этот игрок уже подключён.");
                    if (returning.hero != hello.hero) throw new Exception("Для возвращения выбери прежнего героя.");
                    returning.id = request.ClientNetworkId;
                }
                else
                {
                    if (game.running || game.ended) throw new Exception("Матч уже начался. Новые игроки входят до старта.");
                    if (peers.Count >= Mathf.Min(4, game.HeroCount)) throw new Exception("Комната заполнена.");
                    if (peers.Exists(p => p.hero == hello.hero)) throw new Exception("Этот герой уже занят. Выбери другого.");
                    peers.Add(new Peer { id = request.ClientNetworkId, token = hello.token, hero = hello.hero });
                }
                response.Approved = true;
            }
            catch (Exception error) { response.Approved = false; response.Reason = error.Message; }
        }
        void Connected(ulong id)
        {
            RegisterHandlers();
            if (manager.IsServer)
            {
                var peer = peers.Find(p => p.id == id);
                if (peer == null) return;
                peer.connected = true;
                if (peer.actor == null) peer.actor = id == 0 ? game.player : game.CreatePeer(id, peer.hero);
                peer.actor.OwnerId = id;
                Status = "Игроков в комнате: " + MemberCount + ". Хост может начать матч.";
            }
            else if (id == manager.LocalClientId)
            {
                game.player.OwnerId = id;
                clientActors[id] = game.player;
                Status = "Подключено. Ждём старта хоста.";
            }
        }
        void Disconnected(ulong id)
        {
            if (leaving || manager == null) return;
            if (manager.IsServer)
            {
                var peer = peers.Find(p => p.id == id); if (peer == null) return;
                peer.connected = false;
                if (peer.actor != null) peer.actor.SetCommand(default);
                if (game.running && peer.actor != null) { peer.actor.Dead = true; peer.actor.health = 0; peer.actor.GetComponent<Collider2D>().enabled = false; game.CheckTeamDefeat(); }
                if (!game.running) { if (peer.actor != null && peer.actor != game.player) { game.players.Remove(peer.actor); Destroy(peer.actor.gameObject); } peers.Remove(peer); }
            }
            else
            {
                Status = string.IsNullOrEmpty(manager.DisconnectReason) ? "Хост отключился или соединение потеряно. Матч завершён." : manager.DisconnectReason;
                if (expectReject)
                {
                    bool expected = Status.Contains("герой") || Status.Contains("занят");
                    Debug.Log(expected ? "DZ_NET_DUPLICATE_HERO_REJECT_PASS" : "DZ_NET_DUPLICATE_HERO_REJECT_FAIL: " + Status);
                    _ = FinishSmoke(expected);
                }
                ReturnToMenu(); ui.SetVisible(true);
            }
        }
        void RegisterHandlers()
        {
            if (handlersRegistered || manager == null || manager.CustomMessagingManager == null) return;
            manager.CustomMessagingManager.RegisterNamedMessageHandler(InputMessage, ReceiveInput);
            manager.CustomMessagingManager.RegisterNamedMessageHandler(WorldMessage, ReceiveWorld);
            handlersRegistered = true;
        }
        void ReceiveInput(ulong sender, FastBufferReader reader)
        {
            if (!manager.IsServer || reader.Length > 2048) return;
            var peer = peers.Find(p => p.id == sender && p.connected); if (peer?.actor == null) return;
            try { reader.ReadValueSafe(out string json); peer.actor.SetCommand(JsonUtility.FromJson<ZoneCommand>(json)); }
            catch (Exception) { manager.DisconnectClient(sender); }
        }
        void ReceiveWorld(ulong sender, FastBufferReader reader)
        {
            if (!ClientOnly || sender != NetworkManager.ServerClientId || reader.Length > 65536) return;
            reader.ReadValueSafe(out string json);
            var world = JsonUtility.FromJson<ZoneWorld>(json);
            if (world == null || world.sequence <= receivedSequence) return;
            receivedSequence = world.sequence;
            worldsReceived++;
            bool wasRunning = game.running;
            game.ApplyRemoteMatch(world.running, world.ended, world.victory, world.wave, world.kills, world.damage, world.boss, world.bossHealth, world.remaining, world.notice, world.pending,world.alive,world.elapsed);
            var seen = new HashSet<ulong>();
            foreach (var state in world.players)
            {
                seen.Add(state.id);
                if (!clientActors.TryGetValue(state.id, out var actor)) { actor = state.id == manager.LocalClientId ? game.player : game.CreatePeer(state.id, state.hero); clientActors[state.id] = actor; }
                if (state.id == manager.LocalClientId) { game.selected = state.hero; actor.IsLocal = true; }
                actor.ApplyNetworkState(state);
                if (smokeClient && state.id == manager.LocalClientId) { smokeSawReload |= state.reloading; sawOni |= state.oniRemaining>38; sawMaceSwing |= state.maceSwings>0 && state.maceRemaining>0; sawOniRestore |= sawOni && state.oniRemaining<=0 && state.shots>0; smokeSawFloor |= state.floor >= 20 && state.floor <= 22; }
            }
            foreach (var id in new List<ulong>(clientActors.Keys)) if (!seen.Contains(id)) { var actor = clientActors[id]; if (actor != game.player) { game.players.Remove(actor); Destroy(actor.gameObject); } clientActors.Remove(id); }
            var objectIds = new HashSet<int>();
            foreach (var state in world.objects)
            {
                objectIds.Add(state.id);
                if (!ghosts.TryGetValue(state.id, out var obj) || obj == null)
                {
                    obj = new GameObject("Network object " + state.id); var renderer = obj.AddComponent<SpriteRenderer>(); renderer.sortingOrder = state.type == 1 ? 20 : 100; renderer.sortingLayerID = game.playerVisual.GetComponent<SpriteRenderer>().sortingLayerID; obj.AddComponent<ZoneGhost>(); ghosts[state.id] = obj;
                }
                obj.GetComponent<ZoneGhost>().Apply(game, state);
                if (smokeClient) { smokeSawMine |= state.type == 4; smokeSawGrenade |= state.type == 5; smokeSawPulse |= state.type == 6; sawClock |= state.type==6 && state.kind==3; sawRem |= state.type==6 && state.kind==4; sawGojo |= state.type==7; if(state.type==7){sawPurpleCharge |= state.age<ZonePurple.ChargeTime; sawPurpleFlight |= state.age>=ZonePurple.ChargeTime && state.endAge<0; sawPurpleImpact |= state.endAge>=0;} sawShield |= state.type==6 && state.kind==6; }
            }
            foreach (var id in new List<int>(ghosts.Keys)) if (!objectIds.Contains(id)) { Destroy(ghosts[id]); ghosts.Remove(id); }
            if (world.running && !wasRunning) ui.SetVisible(false);
        }
        static void Send(NetworkManager network, string name, ulong recipient, string json)
        {
            if (json.Length > 30000) return;
            using var writer = new FastBufferWriter(json.Length * 2 + 16, Allocator.Temp);
            writer.WriteValueSafe(json);
            network.CustomMessagingManager.SendNamedMessage(name, recipient, writer, NetworkDelivery.ReliableFragmentedSequenced);
        }
        void Update()
        {
            if (!Active) return;
            RegisterHandlers();
            if (ClientOnly && smokeClient && smokeReconnect && worldsReceived >= 5)
            {
                bool pass = game.players.Count >= 2 && game.player.Dead;
                Debug.Log(pass ? "DZ_NET_RECONNECT_PASS" : "DZ_NET_RECONNECT_FAIL");
                smokeClient = false; _ = FinishSmoke(pass);
            }
            if (ClientOnly && game.running)
            {
                if (smokeClient)
                {
                    if (smokeStarted == 0) { smokeStarted = Time.realtimeSinceStartup; smokeOriginX = game.player.transform.position.x; }
                    float elapsed = Time.realtimeSinceStartup - smokeStarted;
                    pendingInput.Merge(new ZoneCommand { move = elapsed < 1.2f ? Vector2.right : Vector2.zero, aim = (Vector2)game.player.transform.position + Vector2.right * 5, fire = true });
                    if (!smokeEffectsSent && !smokeReconnect && elapsed > .75f) { smokeEffectsSent = true; pendingInput.Merge(new ZoneCommand { aim = (Vector2)game.player.transform.position + Vector2.right * 4, grenade = true }); }
                    if (!smokeMineSent && elapsed > 1.2f && !smokeReconnect) { smokeMineSent = true; pendingInput.Merge(new ZoneCommand { aim = (Vector2)game.player.transform.position + Vector2.right * 4, mine = true }); }
                    if (!smokeReloadSent && elapsed > 2.8f && !smokeReconnect) { smokeReloadSent = true; pendingInput.Merge(new ZoneCommand { aim = (Vector2)game.player.transform.position + Vector2.right * 4, reload = true }); }
                    if (elapsed > 5f)
                    {
                        bool pass = worldsReceived >= 5 && game.players.Count >= 2 && game.wave == 1 && (smokeReconnect ? game.player.Dead : game.player.ShotsFired > 0 && Mathf.Abs(game.player.transform.position.x - smokeOriginX) > .05f);
                        if (!smokeReconnect) pass &= smokeSawMine && smokeSawGrenade && smokeSawPulse && smokeSawReload && smokeSawFloor && sawClock && sawRem && sawGojo && sawShield && sawPurpleCharge && sawPurpleFlight && sawPurpleImpact && sawOni && sawMaceSwing && sawOniRestore;
                        if (smokeReconnect) Debug.Log(pass ? "DZ_NET_RECONNECT_PASS" : "DZ_NET_RECONNECT_FAIL");
                        Debug.Log(pass ? "DZ_NET_CLIENT_PASS: snapshots, two players, authoritative movement and shooting." : $"DZ_NET_CLIENT_FAIL: worlds={worldsReceived}, players={game.players.Count}, wave={game.wave}, shots={game.player.ShotsFired}, dx={game.player.transform.position.x - smokeOriginX}");
                        Debug.Log($"DZ_REM_NETWORK: hornAndMace={sawOni}, swing={sawMaceSwing}, restoredGun={sawOniRestore}");
                        Debug.Log($"DZ_PURPLE_NETWORK: charge={sawPurpleCharge}, flight={sawPurpleFlight}, impact={sawPurpleImpact}");
                        Debug.Log($"DZ_NET_ANIMATIONS: clock={sawClock}, rem={sawRem}, gojo={sawGojo}, shield={sawShield}");
                        Debug.Log($"DZ_NET_EFFECTS: mine={smokeSawMine}, grenade={smokeSawGrenade}, pulse={smokeSawPulse}, reload={smokeSawReload}, floor={smokeSawFloor}");
                        smokeClient = false; _ = FinishSmoke(pass);
                    }
                }
                else pendingInput.Merge(game.InputBlocked ? default : ZoneCommand.Read(game.gameCamera));
            }
            if (ClientOnly && !manager.IsConnectedClient && Time.realtimeSinceStartup > connectDeadline) { Status = "Время подключения истекло. Проверь IP и доступность хоста."; manager.Shutdown(); ui.SetVisible(true); return; }
            if (Time.unscaledTime < nextSend) return;
            nextSend = Time.unscaledTime + .1f;
            if (ClientOnly && manager.IsConnectedClient) { Send(manager, InputMessage, NetworkManager.ServerClientId, JsonUtility.ToJson(pendingInput)); pendingInput.ClearEdges(); }
            else if (IsHost)
            {
                sequence++;
                foreach (var peer in peers) if (peer.id != NetworkManager.ServerClientId && peer.connected) Send(manager, WorldMessage, peer.id, JsonUtility.ToJson(Snapshot(peer.id)));
            }
        }
        ZoneWorld Snapshot(ulong recipient)
        {
            var actors = new List<ZonePlayerState>();
            foreach (var peer in peers) if (peer.actor != null)
            {
                var actor = peer.actor;
                actors.Add(new ZonePlayerState { id = peer.id, hero = peer.hero, position = actor.transform.position, aim = actor.aimPoint, health = actor.health, maxHealth = actor.maxHealth, charge = actor.charge, weapon = actor.weapon, ammo = actor.ammo, reserve = actor.reserve, grenades = actor.grenades, mines = actor.mines, shots = actor.ShotsFired, moving = actor.GetComponent<Rigidbody2D>().linearVelocity.sqrMagnitude > .01f, downed = actor.Downed, dead = actor.Dead, connected = peer.connected, reloading = actor.Reloading, reloadRemaining = actor.ReloadRemaining, castRemaining = actor.CastRemaining, castKind = actor.CastKind, oniRemaining = actor.Oni.Remaining, maceRemaining=actor.Oni.SwingRemaining, maceSwings=actor.Oni.Swings, maceDirection=actor.Oni.SwingDirection, floor = actor.gameObject.layer });
            }
            var objects = new List<ZoneObjectState>();
            foreach (var enemy in game.enemies) if (enemy != null) objects.Add(new ZoneObjectState { id = ObjectId(enemy.gameObject), type = 1, kind = enemy.kind, position = enemy.transform.position, velocity = enemy.GetComponent<Rigidbody2D>().linearVelocity, scale = enemy.transform.localScale.x, health = enemy.health, boss = enemy.boss, floor = enemy.gameObject.layer, remaining = Mathf.Max(0, enemy.ReadyAt - Time.time) });
            foreach (var bullet in FindObjectsByType<ZoneBullet>()) objects.Add(new ZoneObjectState { id = ObjectId(bullet.gameObject), type = 2, position = bullet.transform.position, velocity = bullet.direction * bullet.speed, hostile = bullet.hostile, scale = .22f, floor = bullet.gameObject.layer });
            foreach (var pickup in FindObjectsByType<ZonePickup>()) if (pickup.Owner == recipient) objects.Add(new ZoneObjectState { id = ObjectId(pickup.gameObject), type = 3, kind = pickup.kind, position = pickup.transform.position, scale = .6f, floor = pickup.gameObject.layer });
            foreach (var mine in FindObjectsByType<ZoneMine>()) objects.Add(new ZoneObjectState { id = ObjectId(mine.gameObject), type = 4, position = mine.transform.position, scale = .65f, floor = mine.gameObject.layer });
            foreach (var grenade in FindObjectsByType<ZoneGrenade>()) objects.Add(new ZoneObjectState { id = ObjectId(grenade.gameObject), type = 5, position = grenade.transform.position, floor = grenade.gameObject.layer, scale = .45f });
            foreach (var purple in FindObjectsByType<ZonePurple>()) objects.Add(new ZoneObjectState { id=ObjectId(purple.gameObject), type=7, floor=purple.Floor, position=purple.transform.position, origin=purple.Origin, velocity=purple.Direction, age=purple.Age, endAge=purple.ImpactAge });
            foreach (var pulse in FindObjectsByType<ZonePulse>()) if (pulse.Remaining > .05f) objects.Add(new ZoneObjectState { id = ObjectId(pulse.gameObject), type = 6, position = pulse.transform.position, floor = pulse.Floor, kind = pulse.Kind, scale = pulse.Radius, health = pulse.Duration, velocity = pulse.Direction, remaining = pulse.Remaining });
            return new ZoneWorld { sequence = sequence, running = game.running, ended = game.ended, victory = game.Victory, wave = game.wave, pending=game.PendingEnemies, alive=game.enemies.Count,elapsed=game.Elapsed, kills = game.kills, damage = game.damageDealt, boss = game.BossPhase, bossHealth = game.CurrentBossHealth, remaining = game.BreakRemaining, notice = game.message, players = actors.ToArray(), objects = objects.ToArray() };
        }
        public async void Leave()
        {
            if (leaving) return;
            leaving = true; Busy = true;
            try { if (session != null) await session.LeaveAsync(); }
            catch (Exception error) { Debug.LogWarning("Session leave: " + error.Message); }
            finally
            {
                session = null;
                if (manager != null) { manager.Shutdown(); Destroy(manager.gameObject); manager = null; }
                Time.timeScale = 1; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }
        void ReturnToMenu()
        {
            game.running = game.ended = game.paused = false; Time.timeScale = 1;
            foreach (var obj in ghosts.Values) if (obj != null) Destroy(obj); ghosts.Clear();
            foreach (var actor in new List<ZonePlayer>(game.players)) if (actor != game.player) { game.players.Remove(actor); Destroy(actor.gameObject); }
            clientActors.Clear(); game.player.GetComponent<Rigidbody2D>().simulated = true; game.player.ResetHero(game.selected);
        }
        async Task FinishSmoke(bool pass)
        {
            leaving = true;
            if (session != null) { var active = session; session = null; await CloseSession(active); }
            if (manager != null) manager.Shutdown();
            Application.Quit(pass ? 0 : 1);
        }
        static async Task CloseSession(ISession active) { try { await active.LeaveAsync(); } catch (Exception error) { Debug.LogWarning("Session cleanup: " + error.Message); } }
        void OnDestroy() { leaving = true; if (session != null) { var active = session; session = null; _ = CloseSession(active); } if (manager != null) { manager.Shutdown(); Destroy(manager.gameObject); } }
    }

    public sealed class ZoneIdentity : MonoBehaviour { [NonSerialized] public int Id; }

    public sealed class ZoneGhost : MonoBehaviour
    {
        public int ObjectType { get; private set; }
        public int Kind { get; private set; }
        Vector2 target, velocity; float receivedAt; bool initialized;
        public void Apply(DeadZoneGame game, ZoneObjectState state)
        {
            ObjectType = state.type; Kind = state.kind; gameObject.layer = state.floor;
            if (state.type == 7)
            {
                GetComponent<SpriteRenderer>().enabled=false;
                var spell=GetComponent<ZonePurple>();if(spell==null)spell=gameObject.AddComponent<ZonePurple>();spell.Apply(state);return;
            }
            if (state.type == 6)
            {
                GetComponent<SpriteRenderer>().enabled = false; transform.position = state.position;
                if (GetComponent<ZonePulse>() == null) { var effect = gameObject.AddComponent<ZonePulse>(); effect.Floor = state.floor; effect.Kind = state.kind; effect.Radius = state.scale; effect.Direction = state.velocity; effect.Duration = Mathf.Max(.1f, state.health); }
                GetComponent<ZonePulse>().SetClock(Mathf.Max(.1f,state.health),state.remaining);
                return;
            }
            target = state.position; velocity = state.velocity; receivedAt = Time.unscaledTime;
            if (!initialized) { transform.position = target; initialized = true; }
            bool firearm = state.type == 3 && state.kind >= 0 && state.kind < 4;
            var sr = GetComponent<SpriteRenderer>(); sr.sprite = state.type == 1 ? game.EnemyFrame(state.kind, state.boss) : firearm ? ZoneArsenal.Sprite(state.kind) : state.type == 4 || state.type == 3 && state.kind == 4 ? ZoneArt.Item(1) : state.type == 5 ? ZoneArt.Item(0) : state.type == 3 ? ZoneArt.Item(2) : game.marker;
            if (state.type == 1) sr.flipX = state.velocity.x < -.1f;
            sr.color = state.type == 1 ? (state.boss ? new Color(1, .65f, .85f) : Color.white) : state.type == 2 ? (state.hostile ? Color.red : Color.yellow) : state.type == 4 || state.type == 3 && state.kind == 4 ? Color.cyan : firearm ? Color.white : Color.green;
            sr.sortingLayerID = ZoneMap.Sorting(state.floor);
            if (state.type == 3 || state.type == 4 || state.type == 5) sr.color = Color.white;
            if (state.type == 1) { var color = sr.color; color.a = Mathf.Clamp01(1 - state.remaining / (state.boss ? 1.8f : .75f)); sr.color = color; }
            transform.localScale = state.type == 2 ? new Vector3(.22f, .12f, 1) : Vector3.one * (firearm ? 1.2f : state.scale);
            if (state.type == 2) transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(state.velocity.y, state.velocity.x) * Mathf.Rad2Deg);
        }
        void Update() { if (ObjectType == 6 || ObjectType == 7) return; transform.position = Vector2.Lerp(transform.position, target + velocity * Mathf.Min(.1f, Time.unscaledTime - receivedAt), 1 - Mathf.Exp(-22 * Time.unscaledDeltaTime)); }
    }
}

