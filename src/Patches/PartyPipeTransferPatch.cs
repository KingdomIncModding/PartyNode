using Assets.Source.Flow;
using Assets.Source.Items;
using HarmonyLib;

namespace KingdomPartyNode
{
    /// <summary>Transfers an entire buffered party atomically across the connected pipe.</summary>
    [HarmonyPatch(typeof(FlowPipe), nameof(FlowPipe.Update))]
    internal static class PartyPipeTransferPatch
    {
        private static bool Prefix(FlowPipe __instance)
        {
            FlowOutput output = __instance.ConnectedOutput;
            if (!(output?.Parent is Party))
                return true;

            __instance.ItemRejected = false;
            if (!output.HasBuffer || output.ConnectedPipe != __instance)
                return false;

            InventoryItem[] batch = output.BufferedItems.ToArray();
            if (!PartyDelivery.Plan(output, batch, out FlowInput[] slots, out string reason))
            {
                __instance.ItemRejected = reason == "Target rejects";
                return false;
            }

            for (int i = 0; i < batch.Length; i++)
                slots[i].BufferedItems.Add(batch[i]);

            output.BufferedItems.Clear();
            foreach (InventoryItem member in batch)
                __instance.World?.ItemTransferred(member);

            return false;
        }
    }
}
