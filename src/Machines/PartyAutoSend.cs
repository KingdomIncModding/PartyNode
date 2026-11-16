using Assets.Source.Flow;
using Assets.Source.Player;
using LightJson;

namespace KingdomPartyNode
{
    /// <summary>Native upgrade enabling automatic release when every selected party slot is filled.</summary>
    public sealed class PartyAutoSend : FlowUpgrade
    {
        public const string TypeId = "PartyAutoSend";

        public override string UpgradeType => TypeId;
        public override string DisplayName => "Auto Send";
        public override string IconName => "Icons_17";

        public override string GetDescription(FlowElement element, int nthUpgrade)
        {
            return "Automatically sends the party when every selected lineup slot is filled. Send now can still release an incomplete party. Remove this upgrade to return to manual sending.";
        }

        public override bool IsAvailableFor(FlowElement element)
        {
            return element is Party { AutoSendUnlocked: true } party && !party.HasUpgrade<PartyAutoSend>();
        }

        protected override void DataToJson(JsonObject data)
        {
        }

        protected override void DataFromJson(JsonObject data)
        {
        }

        internal static void Unlock(GamePlayer player)
        {
            if (player == null)
                return;
            player.SetUpgradeSlotCount(Party.TypeId, 1);
            player.AddAvailableFlowUpgrade(Party.TypeId, TypeId);
        }
    }
}
