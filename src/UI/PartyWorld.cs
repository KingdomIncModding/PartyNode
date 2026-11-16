using System.Collections.Generic;
using System.Linq;
using Assets.Behaviour.Flow;
using Assets.Source.Flow;
using Assets.Source.Items;
using KingdomFoundation.Api;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomPartyNode
{
    /// <summary>World-space UI and walking adventurer animation for the Party Node workspace building.</summary>
    public sealed class PartyWorld : MonoBehaviour
    {
        private static readonly Vector2[] Path =
        {
            new Vector2(0.16f, 0.62f),
            new Vector2(0.2f, 0.45f),
            new Vector2(0.39f, 0.4f),
            new Vector2(0.68f, 0.4f),
            new Vector2(0.81f, 0.53f),
            new Vector2(0.82f, 0.8f),
            new Vector2(0.72f, 0.95f),
            new Vector2(0.29f, 0.95f),
            new Vector2(0.16f, 0.82f)
        };

        private const float RoomX = 68f;
        private const float RoomY = 36f;
        private const float RoomWidth = 268f;
        private const float RoomHeight = 134f;
        private const float SendWidth = 116f;
        private const float SendX = 252f;

        private readonly Dictionary<InventoryItem, PartyWalker> walkers = new Dictionary<InventoryItem, PartyWalker>();
        private readonly List<InventoryItem> present = new List<InventoryItem>(25);
        private readonly Image[] inputBadges = new Image[Party.SlotCount];
        private readonly TMP_Text[] inactiveBadges = new TMP_Text[Party.SlotCount];
        private readonly int[] lastArchetypes = { -2, -2, -2, -2, -2 };

        private RectTransform members;
        private Vector2[] route;
        private float[] lengths;
        private float perimeter;
        private int nextWalker;
        private TMP_Text status;
        private Button send;
        private Party party;
        private PartyUi ui;

        internal PartySettingsPopup Settings { get; private set; }
        internal int VisibleMemberCount => walkers.Count;
        internal IEnumerable<InventoryItem> VisibleMembers => walkers.Keys;
        internal Vector2 MemberPosition(InventoryItem item) => walkers[item].Root.anchoredPosition;

        private void Start()
        {
            WorldFlowElement elementView = GetComponent<WorldFlowElement>();
            party = elementView.Element as Party;
            if (party == null)
            {
                enabled = false;
                return;
            }

            ui = new PartyUi(FoundationApi.WorkspaceVisuals.LabelText(elementView).font);
            RectTransform root = PartyUi.Rect(transform, "PartyMeetup", 0f, 0f, Party.Width, Party.Height);

            Image room = PartyUi.Rect(root, "EmptyMeetupRoom", RoomX, RoomY, RoomWidth, RoomHeight).gameObject.AddComponent<Image>();
            room.sprite = PartyArtwork.RoomSprite;
            room.raycastTarget = false;

            members = PartyUi.Rect(root, "ActualPartyMembers", RoomX, RoomY, RoomWidth, RoomHeight);
            BuildRoute();
            BuildControls(root, elementView);
            Refresh();
        }

        private void BuildRoute()
        {
            route = Path.Select(p => new Vector2(p.x * RoomWidth, -p.y * RoomHeight)).ToArray();
            lengths = new float[route.Length];
            for (int i = 0; i < route.Length; i++)
                perimeter += lengths[i] = Vector2.Distance(route[i], route[(i + 1) % route.Length]);
        }

        private void BuildControls(RectTransform root, WorldFlowElement elementView)
        {
            Button gear = ui.Button(root, "", 342f, 0f, 30f, 30f, OpenSettings);
            gear.name = "PartySettingsGear";
            PartyGearGraphic cog = PartyUi.Rect(gear.transform, "Cog", 5f, 5f, 20f, 20f).gameObject.AddComponent<PartyGearGraphic>();
            cog.color = PartyUi.Paper;
            cog.raycastTarget = false;
            gear.gameObject.AddComponent<FoundationTooltipSource>().SetTooltip(
                "Configure party",
                "Choose up to five members total across all archetypes.");

            for (int i = 0; i < Party.SlotCount; i++)
            {
                inputBadges[i] = PartyUi.Rect(root, "InputClass_" + (i + 1), 8f, 28f + 36f * i, 20f, 24f).gameObject.AddComponent<Image>();
                inputBadges[i].preserveAspect = true;
                inputBadges[i].gameObject.AddComponent<FoundationTooltipSource>();

                inactiveBadges[i] = ui.Text(root, "\u2013", 8f, 28f + 36f * i, 20f, 24f, 16f);
                inactiveBadges[i].alignment = TextAlignmentOptions.Center;
                inactiveBadges[i].color = new Color(0.5f, 0.48f, 0.45f);
            }

            status = ui.Text(root, "Waiting", 82f, 184f, 158f, 26f, 14f);
            send = ui.Button(root, "Send now", SendX, 186f, SendWidth, 26f, SendNow);
            send.GetComponentInChildren<TMP_Text>().fontSize = 14f;
            send.gameObject.AddComponent<FoundationTooltipSource>().SetTooltip(
                "Send party",
                "Release the adventurers currently waiting, even if required counts are not met. The connected receiver must have room for everyone being sent.");

            Component upgradeSlots = FoundationApi.WorkspaceVisuals.BuildingGraphic(elementView, GameBuildingGraphic.UpgradeSlots);
            if (upgradeSlots)
                upgradeSlots.transform.SetAsLastSibling();
        }

        private void Update()
        {
            if (party == null)
                return;

            SyncInputBadges();
            Refresh();
            TickWalkers(Time.deltaTime);
        }

        private void SyncInputBadges()
        {
            for (int i = 0; i < Party.SlotCount; i++)
            {
                int archetype = party.GetInputArchetype(i);
                if (lastArchetypes[i] == archetype)
                    continue;

                lastArchetypes[i] = archetype;
                inputBadges[i].sprite = archetype >= 0 ? FoundationApi.Presentation.FindSprite(Party.Icons[archetype]) : null;
                inputBadges[i].enabled = archetype >= 0;
                inactiveBadges[i].gameObject.SetActive(archetype < 0);
                inputBadges[i].GetComponent<FoundationTooltipSource>().SetTooltip(
                    archetype >= 0 ? "Input " + (i + 1) + ": " + Party.Names[archetype] : "Unused party slot",
                    archetype >= 0
                        ? "Accepts one matching adventurer for this party slot. Advanced classes match their base archetype."
                        : "Select more members with the gear to use this input.");
            }
        }

        internal void Refresh()
        {
            if (party == null)
                return;

            string text = FormatStatus();
            if (status.text != text)
                status.text = text;

            send.interactable = party.CanSendNow;
            SyncWalkers();

            if (Settings && Settings.IsOpen)
                Settings.Refresh();
        }

        private string FormatStatus()
        {
            if (party.Sending)
                return "Sending \u00b7 " + party.Outputs[0].BufferedItems.Count;
            if ((!party.AutoSendEnabled || party.Ready) && party.SendBlockReason != null)
                return party.SendBlockReason;
            if (party.RecoveredCount > 0)
                return "Recovered \u00b7 " + party.RecoveredCount;
            if (party.Ready && !party.AutoSendEnabled)
                return "Ready \u00b7 " + party.WaitingCount + "/" + party.RequiredCount;
            if (party.AutoSendEnabled)
                return "Auto \u00b7 " + party.WaitingCount + "/" + party.RequiredCount;
            return party.RequiredCount == 0
                ? "No lineup"
                : "Waiting \u00b7 " + party.WaitingCount + "/" + party.RequiredCount;
        }

        private void SyncWalkers()
        {
            present.Clear();
            foreach (FlowInput input in party.Inputs)
                present.AddRange(input.BufferedItems);
            present.AddRange(party.Outputs[0].BufferedItems);

            InventoryItem[] departed = walkers.Keys.Where(item => !present.Contains(item)).ToArray();
            foreach (InventoryItem item in departed)
            {
                walkers[item].Root.gameObject.SetActive(false);
                Destroy(walkers[item].Root.gameObject);
                walkers.Remove(item);
            }

            foreach (InventoryItem item in present)
            {
                if (walkers.ContainsKey(item))
                    continue;

                int index = nextWalker++;
                RectTransform root = PartyUi.Rect(members, "Member_" + item.Type.Identifier, 0f, 0f, 24f, 30f);
                root.pivot = new Vector2(0.5f, 0f);
                Image image = root.gameObject.AddComponent<Image>();
                image.sprite = item.Icon;
                image.preserveAspect = true;
                image.raycastTarget = false;

                PartyWalker walker = new PartyWalker
                {
                    Item = item,
                    Root = root,
                    Image = image,
                    Distance = Mathf.Repeat(index * 83.7f, perimeter),
                    Speed = 13f + (index % 5) * 1.5f,
                    Phase = index * 1.37f,
                    Direction = index % 2 == 0 ? 1 : -1
                };
                walkers.Add(item, walker);
                PositionWalker(walker, 0f);
            }
        }

        internal void TickWalkers(float delta)
        {
            foreach (PartyWalker walker in walkers.Values)
                PositionWalker(walker, delta);

            foreach (PartyWalker walker in walkers.Values.OrderByDescending(w => w.Ground.y))
                walker.Root.SetAsLastSibling();
        }

        private void PositionWalker(PartyWalker walker, float delta)
        {
            walker.Age += delta;
            walker.Distance = Mathf.Repeat(walker.Distance + delta * walker.Speed * walker.Direction, perimeter);

            float remaining = walker.Distance;
            int segment = 0;
            while (segment < lengths.Length - 1 && remaining >= lengths[segment])
                remaining -= lengths[segment++];

            Vector2 start = route[segment];
            Vector2 end = route[(segment + 1) % route.Length];
            walker.Ground = Vector2.Lerp(start, end, remaining / lengths[segment]);

            float bob = Mathf.Abs(Mathf.Sin(walker.Age * 9f + walker.Phase)) * 1.3f;
            walker.Root.anchoredPosition = new Vector2(Mathf.Round(walker.Ground.x), Mathf.Round(walker.Ground.y + bob));

            float horizontalDelta = (end.x - start.x) * walker.Direction;
            if (Mathf.Abs(horizontalDelta) > 0.1f)
                walker.Root.localScale = new Vector3(horizontalDelta < 0f ? -1f : 1f, 1f, 1f);

            walker.Image.color = new Color(1f, 1f, 1f, Mathf.Clamp01(walker.Age * 4f));
        }

        internal void SendNow()
        {
            if (party.SendNow())
                FoundationApi.UiAccess.PlaySound(GameUiSound.CraftFinished);
            Refresh();
        }

        internal void OpenSettings()
        {
            if (!Settings)
            {
                Canvas rootCanvas = FoundationApi.UiAccess.EditToolbar.GetComponentInParent<Canvas>().rootCanvas;
                GameObject holder = new GameObject("PartyConfiguration", typeof(RectTransform));
                holder.SetActive(false);
                Settings = holder.AddComponent<PartySettingsPopup>();
                Settings.Initialize(this, party, ui.Font, rootCanvas.transform);
            }

            Settings.Open();
        }

        private void OnDisable()
        {
            if (Settings && Settings.IsOpen && FoundationApi.UiAccess.Root)
                Settings.ClosePopup();
        }

        private void OnDestroy()
        {
            if (!Settings)
                return;
            if (Settings.IsOpen && FoundationApi.UiAccess.Root)
                Settings.ClosePopup();
            Destroy(Settings.gameObject);
        }
    }
}
