using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Flow;
using Assets.Source.Items;
using Assets.Source.Items.Type;
using Assets.Source.Player;
using KingdomFoundation.Api;
using LightJson;
using UnityEngine;

namespace KingdomPartyNode
{
    /// <summary>Custom workspace node that buffers up to five adventurers by archetype and releases them as one party.</summary>
    public sealed class Party : WorkspaceNode
    {
        public const string TypeId = "Party";
        public const string TechId = "party_node";
        public const string AutoSendTechId = "party_node_auto_send";
        public const int SlotCount = 5;
        public const int MaxPerArchetype = 5;
        public const int MaxPartySize = 5;
        public const int Width = 384;
        public const int Height = 224;

        public static readonly string[] Names = { "Adventurer", "Fighter", "Rogue", "Mage", "Cleric" };
        public static readonly string[] Icons = { "Icons_139", "Icons_170", "Icons_169", "Icons_171", "Icons_172" };
        private static readonly string[] ItemIds = { "adventurer", "fighter", "rogue", "mage", "cleric" };

        private readonly int[] required = { 1, 1, 1, 1, 1 };
        private readonly List<InventoryItem> recovered = new List<InventoryItem>();
        private bool legacyAutoSend;
        private bool pendingPostDeserialize;

        public override string Label => "Party Node";
        public override string IconName => PartyArtwork.RoomIconId;
        public override ElementCategory Category => ElementCategory.Utility;
        public override bool HasCraftButton => false;
        public override int UpgradeSlots => AutoSendUnlocked ? 1 : 0;

        public int WaitingCount => Inputs.Sum(input => input.BufferedItems.Count);
        public int RequiredCount => required.Sum();
        public int RemainingSlots => MaxPartySize - RequiredCount;
        public int RecoveredCount => recovered.Count;
        public bool Sending => Outputs[0].HasBuffer;

        public bool AutoSendUnlocked
        {
            get
            {
                GamePlayer current = GamePlayer.Current;
                if (current == null)
                    return false;
                return current.HasTech(TechNode.Get(AutoSendTechId));
            }
        }

        public bool AutoSendEnabled => AutoSendUnlocked && HasUpgrade<PartyAutoSend>();

        public bool CanSendNow => !Sending && PartyDelivery.Plan(Outputs[0], ReleaseBatch(manual: true), out _, out _);

        public string SendBlockReason
        {
            get
            {
                if (Sending)
                    return null;
                PartyDelivery.Plan(Outputs[0], ReleaseBatch(manual: true), out _, out string reason);
                return reason;
            }
        }

        public bool Ready =>
            !Sending &&
            RequiredCount > 0 &&
            Enumerable.Range(0, RequiredCount).All(port =>
                Inputs[port].HasBuffer && Matches(GetInputArchetype(port), Inputs[port].NextItem));

        public Party() : base(TypeId)
        {
            RequiredTech = TechNode.Get(TechId);
            Description = "Assemble a party of up to 5 adventurers total. Set each archetype's required count; 0 disables it. Press Send now to release the party. Research Auto Send, then install it in the node's + upgrade slot to automate complete lineups.";
            CostCurrency = CurrencyCollection.GoldMedal;
            CostMultiplier = 0.5;
            Size = new Vector2Int(Width, Height);

            Outputs.Add(new FlowOutput(this, 0)
            {
                BufferSize = MaxPartySize,
                Position = 10,
                Filter = item => item?.Type is AdventurerItemType,
                FilterPreview = true
            });

            for (int i = 0; i < SlotCount; i++)
            {
                int slot = i;
                Inputs.Add(new FlowInput(this, i)
                {
                    Position = -10 - 36 * i,
                    BufferSize = 1,
                    FilterPreview = true,
                    Filter = item => Matches(GetInputArchetype(slot), item)
                });
            }

            RebuildLineup();
        }

        public static bool Matches(int slot, InventoryItem item)
        {
            return item?.Type is AdventurerItemType adventurer && (int)adventurer.Archetype == slot;
        }

        public int GetRequired(int slot) => required[slot];

        public int GetInputArchetype(int port)
        {
            if (port < 0 || port >= SlotCount)
                return -1;

            for (int i = 0; i < SlotCount; i++)
            {
                if (port < required[i])
                    return i;
                port -= required[i];
            }

            return -1;
        }

        public int GetWaiting(int archetype)
        {
            return Inputs.Sum(input => input.BufferedItems.Count(item => Matches(archetype, item)))
                + recovered.Count(item => Matches(archetype, item));
        }

        public void SetRequired(int slot, int count)
        {
            if (slot < 0 || slot >= SlotCount)
                throw new ArgumentOutOfRangeException(nameof(slot));

            int otherSlots = RequiredCount - required[slot];
            required[slot] = Math.Max(0, Math.Min(MaxPartySize - otherSlots, count));
            RebuildLineup();
        }

        public bool CanReceive(int slot, InventoryItem item)
        {
            return !Sending && recovered.Count == 0 && WaitingCount < MaxPartySize && Matches(GetInputArchetype(slot), item);
        }

        public override void Update(float delta)
        {
            if (!Sending && recovered.Count > 0)
                RebuildLineup();

            if (AutoSendEnabled && Ready)
                Release(manual: false);
        }

        public override void UpdateUpgrades()
        {
            if (pendingPostDeserialize)
            {
                pendingPostDeserialize = false;
                NormalizePorts();
                NormalizeLoadedMembers();
                MigrateLegacyUpgrade();
            }

            base.UpdateUpgrades();
        }

        public bool SendNow() => Release(manual: true);

        internal void NormalizePorts()
        {
            for (int i = 0; i < SlotCount; i++)
                Inputs[i].Position = -10 - 36 * i;
            Outputs[0].Position = 10;
        }

        internal void NormalizeLoadedMembers()
        {
            while (Outputs[0].BufferedItems.Count > MaxPartySize)
            {
                recovered.Add(Outputs[0].BufferedItems[MaxPartySize]);
                Outputs[0].BufferedItems.RemoveAt(MaxPartySize);
            }

            int room = MaxPartySize - Outputs[0].BufferedItems.Count;
            foreach (FlowInput input in Inputs)
            {
                int keep = Math.Min(room, input.BufferedItems.Count);
                room -= keep;
                while (input.BufferedItems.Count > keep)
                {
                    recovered.Add(input.BufferedItems[keep]);
                    input.BufferedItems.RemoveAt(keep);
                }
            }

            RebuildLineup();
        }

        internal void MigrateLegacyUpgrade()
        {
            if (legacyAutoSend && AutoSendUnlocked && GetUpgrade(0) == null)
                SetUpgrade(0, new PartyAutoSend());
            legacyAutoSend = false;
        }

        protected override void CopySettingsTo(FlowElement copy)
        {
            Party target = (Party)copy;
            Array.Clear(target.required, 0, SlotCount);
            for (int i = 0; i < SlotCount; i++)
                target.SetRequired(i, required[i]);
        }

        protected override void DataToJson(JsonObject data)
        {
            JsonArray counts = new JsonArray();
            foreach (int value in required)
                counts.Add(value);
            data["PartyRequirements"] = counts;

            if (recovered.Count > 0)
                data["RecoveredMembers"] = recovered.ToJsonArray();
        }

        protected override void DataFromJson(JsonObject data, FlowDiagram parent)
        {
            pendingPostDeserialize = true;
            legacyAutoSend = !data["Upgrades"].IsJsonArray && data["AutoSend"].AsBoolean;

            JsonValue requirements = data["PartyRequirements"];
            if (requirements.IsJsonArray)
            {
                Array.Clear(required, 0, SlotCount);
                for (int i = 0; i < SlotCount; i++)
                {
                    int count = i < requirements.AsJsonArray.Count && requirements[i].IsInteger
                        ? requirements[i].AsInteger
                        : 0;
                    SetRequired(i, count);
                }
            }

            if (data["RecoveredMembers"].IsJsonArray)
                recovered.FromJsonArray(data["RecoveredMembers"], InventoryItem.FromJson);
        }

        private void RebuildLineup()
        {
            List<InventoryItem> pooled = Inputs.SelectMany(input => input.BufferedItems).Concat(recovered).ToList();
            foreach (FlowInput input in Inputs)
                input.BufferedItems.Clear();
            recovered.Clear();

            for (int i = 0; i < SlotCount; i++)
            {
                int archetype = GetInputArchetype(i);
                Inputs[i].BufferSize = archetype >= 0 ? 1 : 0;
                Inputs[i].Item = archetype >= 0 ? ItemType.Get(ItemIds[archetype]) : null;
                Inputs[i].FilterPreview = archetype >= 0;
            }

            int room = Math.Max(0, MaxPartySize - Outputs[0].BufferedItems.Count);
            foreach (InventoryItem member in pooled)
            {
                FlowInput slot = Inputs.FirstOrDefault(input =>
                    input.CanBuffer && Matches(GetInputArchetype(input.ParentIndex), member));
                if (slot != null && room > 0)
                {
                    slot.BufferedItems.Add(member);
                    room--;
                }
                else
                {
                    recovered.Add(member);
                }
            }
        }

        private List<InventoryItem> ReleaseBatch(bool manual)
        {
            IEnumerable<InventoryItem> batch = Inputs
                .Take(manual ? SlotCount : RequiredCount)
                .SelectMany(input => input.BufferedItems);
            if (manual)
                batch = batch.Concat(recovered);
            return batch.Take(MaxPartySize).ToList();
        }

        private bool Release(bool manual)
        {
            if (Sending || (WaitingCount == 0 && recovered.Count == 0) || (!manual && !Ready))
                return false;

            if (!PartyDelivery.Plan(Outputs[0], ReleaseBatch(manual), out _, out _))
                return false;

            for (int i = 0; i < SlotCount; i++)
            {
                if (Outputs[0].BufferedItems.Count >= MaxPartySize)
                    break;
                if (Inputs[i].HasBuffer && (manual || i < RequiredCount))
                    Outputs[0].BufferedItems.Add(Inputs[i].ConsumeItem());
            }

            if (manual)
            {
                while (recovered.Count > 0 && Outputs[0].BufferedItems.Count < MaxPartySize)
                {
                    Outputs[0].BufferedItems.Add(recovered[0]);
                    recovered.RemoveAt(0);
                }
            }

            return Outputs[0].HasBuffer;
        }
    }
}
