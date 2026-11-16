using KingdomFoundation.Api;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace KingdomPartyNode
{
    /// <summary>Configuration popup for choosing per-archetype required party counts and sending manually.</summary>
    public sealed class PartySettingsPopup : FoundationPopup
    {
        private const float Width = 500f;
        private const float Height = 516f;

        private readonly TMP_Text[] targets = new TMP_Text[Party.SlotCount];
        private readonly TMP_Text[] stored = new TMP_Text[Party.SlotCount];
        private readonly Button[] minus = new Button[Party.SlotCount];
        private readonly Button[] plus = new Button[Party.SlotCount];

        private Party party;
        private PartyWorld owner;
        private RectTransform panel;
        private TMP_Text status;
        private TMP_Text budget;
        private Button send;
        private bool restoreInput;
        private int openedFrame;

        internal bool IsOpen => gameObject.activeSelf;

        internal void Initialize(PartyWorld world, Party element, TMP_FontAsset font, Transform parent)
        {
            owner = world;
            party = element;
            transform.SetParent(parent, false);

            RectTransform root = (RectTransform)transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;

            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
            gameObject.AddComponent<GraphicRaycaster>();

            RectTransform backdrop = PartyUi.Rect(root, "DismissBackdrop", 0f, 0f, 0f, 0f);
            backdrop.anchorMin = Vector2.zero;
            backdrop.anchorMax = Vector2.one;
            backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
            backdrop.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
            backdrop.gameObject.AddComponent<Button>().onClick.AddListener(ClosePopup);

            PartyUi ui = new PartyUi(font);
            panel = ui.Box(root, "PartySettingsPanel", 0f, 0f, Width, Height, "Tooltip").rectTransform;
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;

            ui.Text(panel, "Party Meetup", 26f, 20f, 400f, 40f, 25f);
            ui.Button(panel, "X", 444f, 23f, 32f, 32f, ClosePopup).name = "ClosePartySettings";
            ui.Text(panel, "Choose up to five members total.", 26f, 66f, 448f, 28f, 16f);
            ui.Text(panel, "Required", 272f, 103f, 100f, 24f, 15f).alignment = TextAlignmentOptions.Center;
            ui.Text(panel, "Waiting", 386f, 103f, 88f, 24f, 15f).alignment = TextAlignmentOptions.Center;

            BuildArchetypeRows(ui);

            budget = ui.Text(panel, "", 26f, 399f, 448f, 26f, 15f);
            status = ui.Text(panel, "", 26f, 429f, 448f, 26f, 16f);
            status.alignment = TextAlignmentOptions.Center;

            send = ui.Button(panel, "Send now", 152f, 472f, 196f, 32f, () =>
            {
                owner.SendNow();
                Refresh();
            });
            send.gameObject.AddComponent<FoundationTooltipSource>().SetTooltip(
                "Send party",
                "Send everyone currently waiting, even if the required counts are incomplete. The receiving node must have room for the entire batch.");
        }

        private void BuildArchetypeRows(PartyUi ui)
        {
            for (int i = 0; i < Party.SlotCount; i++)
            {
                int slot = i;
                float y = 136f + i * 51f;

                Image icon = PartyUi.Rect(panel, Party.Names[i] + "Icon", 30f, y, 32f, 32f).gameObject.AddComponent<Image>();
                icon.sprite = FoundationApi.Presentation.FindSprite(Party.Icons[i]);
                icon.preserveAspect = true;
                icon.gameObject.AddComponent<FoundationTooltipSource>().SetTooltip(
                    Party.Names[i],
                    i == 0 ? "None means an ordinary Adventurer. Conscripts are not accepted." : "Advanced classes match their base archetype.");

                ui.Text(panel, Party.Names[i], 76f, y, 180f, 32f, 18f);

                minus[i] = ui.Button(panel, "\u2212", 272f, y, 32f, 32f, () => Change(slot, -1));
                targets[i] = ui.Text(panel, "1", 311f, y, 28f, 32f, 20f);
                targets[i].alignment = TextAlignmentOptions.Center;

                plus[i] = ui.Button(panel, "+", 346f, y, 32f, 32f, () => Change(slot, 1));
                plus[i].gameObject.AddComponent<FoundationTooltipSource>().SetTooltip(
                    "Five members total",
                    "Reduce another required count to make room when all five slots are selected.");

                stored[i] = ui.Text(panel, "0 / 1", 391f, y, 78f, 32f, 18f);
                stored[i].alignment = TextAlignmentOptions.Center;
            }
        }

        internal void Open()
        {
            if (IsOpen)
            {
                ClosePopup();
                return;
            }

            Refresh();
            Show(true);
            transform.SetAsLastSibling();
            openedFrame = Time.frameCount;
            restoreInput = FoundationApi.UiAccess.IsGameInputEnabled;
            FoundationApi.UiAccess.SetGameInputEnabled(false);
        }

        private void Change(int slot, int delta)
        {
            party.SetRequired(slot, party.GetRequired(slot) + delta);
            FoundationApi.UiAccess.PlaySound(GameUiSound.CraftStep);
            Refresh();
            owner.Refresh();
        }

        internal void Refresh()
        {
            for (int i = 0; i < Party.SlotCount; i++)
            {
                int waiting = party.GetWaiting(i);
                int required = party.GetRequired(i);
                targets[i].text = required.ToString();
                stored[i].text = waiting + " / " + required;
                stored[i].color = required == 0
                    ? new Color(0.65f, 0.64f, 0.6f)
                    : waiting >= required ? new Color(0.6f, 1f, 0.65f) : PartyUi.Paper;
                minus[i].interactable = required > 0;
                plus[i].interactable = required < Party.MaxPerArchetype && party.RemainingSlots > 0;
            }

            budget.text = party.RequiredCount + " / 5 selected. 0 disables an input.";
            status.text = FormatStatusText();
            send.interactable = party.CanSendNow;
        }

        private string FormatStatusText()
        {
            if (party.Sending)
                return "Sending party \u00b7 " + party.Outputs[0].BufferedItems.Count + " remaining";
            if ((!party.AutoSendEnabled || party.Ready) && party.SendBlockReason != null)
                return party.SendBlockReason + " \u00b7 party stays here";
            if (party.RecoveredCount > 0)
                return "Recovered reserve: " + party.RecoveredCount + " \u00b7 Send now (max 5).";
            if (party.Ready && !party.AutoSendEnabled)
                return "Party ready \u00b7 click Send now";
            if (party.RequiredCount == 0)
                return "All inputs disabled";
            return "Waiting \u00b7 " + party.WaitingCount + " / " + party.RequiredCount + " adventurers";
        }

        private void Update()
        {
            if (!owner || !owner.isActiveAndEnabled || !FoundationApi.UiAccess.IsWorkspaceVisible)
            {
                ClosePopup();
                return;
            }

            if (Time.frameCount != openedFrame &&
                ((Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                 (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)))
            {
                ClosePopup();
                return;
            }

            if (transform.parent is RectTransform parentRect)
            {
                float scale = Mathf.Max(0.1f, Mathf.Min(1f, (parentRect.rect.width - 32f) / Width, (parentRect.rect.height - 32f) / Height));
                panel.localScale = Vector3.one * scale;
            }

            Refresh();
        }

        public override void PopupClosed()
        {
            if (EventSystem.current && EventSystem.current.currentSelectedGameObject &&
                EventSystem.current.currentSelectedGameObject.transform.IsChildOf(transform))
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            base.PopupClosed();
        }

        private void OnDisable()
        {
            base.OnPointerExit(null);
            if (restoreInput && FoundationApi.UiAccess.Root && !FoundationApi.UiAccess.IsMenuVisible)
                FoundationApi.UiAccess.SetGameInputEnabled(true);
            restoreInput = false;
        }
    }
}
