using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadZone
{
    public sealed class ZoneNetworkUI : MonoBehaviour
    {
        ZoneNetwork network; DeadZoneGame game; TMP_FontAsset font;
        GameObject root; TextMeshProUGUI status, team;
        TMP_InputField address, code;
        UnityEngine.UI.Button hostLan, joinLan, hostOnline, joinOnline, start;
        public bool Visible => root != null && root.activeSelf;
        public void SetVisible(bool value) { root.SetActive(value); }
        public void Initialize(ZoneNetwork owner, DeadZoneGame match, Transform canvas, TMP_FontAsset uiFont)
        {
            network = owner; game = match; font = uiFont;
            root = Panel(canvas, "CoopRoom", Vector2.zero, Vector2.zero, Vector2.zero, new Color(.025f, .04f, .065f, .98f));
            root.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            var rect = root.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            Text(root.transform, "Title", new Vector2(0, 1), new Vector2(80, -45), new Vector2(1300, 100), 60).text = "DEAD ZONE <color=#94A86B>/ КОМАНДА</color>";
            Text(root.transform, "Hint", new Vector2(0, 1), new Vector2(85, -155), new Vector2(1350, 55), 22).text = "Один создаёт комнату, остальные подключаются. Каждый играет своим героем.";
            team = Text(root.transform, "Team", new Vector2(0, 1), new Vector2(85, -210), new Vector2(1300, 45), 23);
            var local = Panel(root.transform, "LAN", new Vector2(0, 1), new Vector2(80, -280), new Vector2(690, 320), new Color(.075f, .1f, .14f));
            Text(local.transform, "Title", new Vector2(0, 1), new Vector2(25, -20), new Vector2(630, 45), 28).text = "ЛОКАЛЬНАЯ СЕТЬ";
            address = Field(local.transform, "Host address", new Vector2(25, -90), "IP хоста", "127.0.0.1");
            hostLan = Button(local.transform, "Создать LAN", new Vector2(25, -175), new Vector2(300, 65), () => network.HostLan());
            joinLan = Button(local.transform, "Подключиться", new Vector2(355, -175), new Vector2(300, 65), () => network.JoinLan(address.text));
            Text(local.transform, "Description", new Vector2(0, 1), new Vector2(25, -260), new Vector2(630, 45), 18).text = "Без аккаунта. IP 127.0.0.1 — для теста на одном ПК. Порт: 7777.";
            var online = Panel(root.transform, "Internet", new Vector2(0, 1), new Vector2(830, -280), new Vector2(690, 320), new Color(.075f, .1f, .14f));
            Text(online.transform, "Title", new Vector2(0, 1), new Vector2(25, -20), new Vector2(630, 45), 28).text = "ЧЕРЕЗ ИНТЕРНЕТ";
            code = Field(online.transform, "Room code", new Vector2(25, -90), "Код комнаты", "");
            hostOnline = Button(online.transform, "Создать комнату", new Vector2(25, -175), new Vector2(300, 65), () => network.HostInternet());
            joinOnline = Button(online.transform, "Войти по коду", new Vector2(355, -175), new Vector2(300, 65), () => network.JoinInternet(code.text));
            Text(online.transform, "Description", new Vector2(0, 1), new Vector2(25, -260), new Vector2(630, 45), 18).text = "Требует привязки проекта к Unity Cloud.";
            status = Text(root.transform, "Status", Vector2.zero, new Vector2(85, 160), new Vector2(1350, 120), 22);
            var back = BottomButton("Назад", new Vector2(80, 55), new Vector2(220, 65), () => SetVisible(false));
            BottomButton("Выйти из комнаты", new Vector2(330, 55), new Vector2(320, 65), () => network.Leave());
            start = BottomButton("НАЧАТЬ МАТЧ", new Vector2(1120, 55), new Vector2(400, 65), () => game.Begin());
            SetVisible(false);
        }
        void Update()
        {
            if (!Visible) return;
            bool available = !network.Active && !network.Busy;
            hostLan.interactable = joinLan.interactable = hostOnline.interactable = joinOnline.interactable = available;
            address.interactable = code.interactable = available;
            start.interactable = network.IsHost && !game.running && !game.ended;
            team.text = ZoneL.Format("ТВОЙ ГЕРОЙ: <color=#94A86B>{0}</color>     ·     УЧАСТНИКИ: {1} / {2}",game.HeroName(game.selected),network.MemberCount,Mathf.Min(4,game.HeroCount));
            status.text = ZoneL.Get(network.Status) + (string.IsNullOrEmpty(network.RoomCode) ? "" : ZoneL.Format("\nКОД: <color=#94A86B>{0}</color>",network.RoomCode)) + "\n" + network.MemberSummary();
        }
        UnityEngine.UI.Button BottomButton(string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction click) => Button(root.transform, label, position, size, click, true);
        UnityEngine.UI.Button Button(Transform parent, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction click, bool bottom = false)
        {
            var anchor = bottom ? Vector2.zero : new Vector2(0, 1);
            var obj = Panel(parent, label, anchor, position, size, ZonePalette.Button);
            var image = obj.GetComponent<UnityEngine.UI.Image>(); image.raycastTarget = true;
            var button = obj.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image; button.onClick.AddListener(click);
            var text = Text(obj.transform, "Label", new Vector2(.5f, .5f), Vector2.zero, size - Vector2.one * 8, 23); text.text = label; text.alignment = TextAlignmentOptions.Center;
            return button;
        }
        TMP_InputField Field(Transform parent, string name, Vector2 position, string placeholder, string value)
        {
            var obj = Panel(parent, name, new Vector2(0, 1), position, new Vector2(630, 60), new Color(.025f, .04f, .065f));
            obj.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D)); viewport.transform.SetParent(obj.transform, false);
            var rect = viewport.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = new Vector2(15, 4); rect.offsetMax = new Vector2(-15, -4);
            var text = Text(viewport.transform, "Text", new Vector2(0, .5f), Vector2.zero, new Vector2(595, 48), 26); text.alignment = TextAlignmentOptions.MidlineLeft;
            var hint = Text(viewport.transform, "Placeholder", new Vector2(0, .5f), Vector2.zero, new Vector2(595, 48), 26); hint.alignment = TextAlignmentOptions.MidlineLeft; hint.text = placeholder; hint.color = ZonePalette.Muted;
            var input = obj.AddComponent<TMP_InputField>(); input.textViewport = rect; input.textComponent = text; input.placeholder = hint; input.characterLimit = 64; input.text = value; return input;
        }
        TextMeshProUGUI Text(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, int fontSize)
        {
            var obj = new GameObject(name, typeof(RectTransform)); obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = anchor; rect.anchoredPosition = position; rect.sizeDelta = size;
            var text = obj.AddComponent<TextMeshProUGUI>(); text.font = font; text.fontSize = fontSize; text.color = ZonePalette.Text; text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.Normal;ZoneL.Attach(text); return text;
        }
        GameObject Panel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image)); obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = anchor; rect.anchoredPosition = position; rect.sizeDelta = size;
            var image = obj.GetComponent<UnityEngine.UI.Image>(); image.color = ZonePalette.Surface(color); ZoneQuarantineFrame.Attach(obj,size); image.raycastTarget = false; return obj;
        }
    }
}
