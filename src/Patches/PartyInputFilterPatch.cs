using Assets.Source.Flow;
using Assets.Source.Items;
using HarmonyLib;

namespace KingdomPartyNode
{
    /// <summary>Enforces per-slot archetype and party-capacity acceptance rules on Party inputs.</summary>
    [HarmonyPatch(typeof(FlowIO), nameof(FlowIO.CanAcceptItem))]
    internal static class PartyInputFilterPatch
    {
        private static bool Prefix(FlowIO __instance, InventoryItem item, ref bool __result)
        {
            if (!(__instance is FlowInput) || !(__instance.Parent is Party party))
                return true;

            __result = party.CanReceive(__instance.ParentIndex, item);
            return false;
        }
    }
}
