using System;
using Assets.Source.Flow;
using HarmonyLib;

namespace KingdomPartyNode
{
    /// <summary>Instantiates PartyAutoSend from saved upgrade JSON and resolves native upgrades cleanly.</summary>
    [HarmonyPatch(typeof(FlowUpgrade), nameof(FlowUpgrade.Create))]
    internal static class PartyAutoSendFactoryPatch
    {
        private static bool Prefix(string name, ref FlowUpgrade __result)
        {
            if (name == PartyAutoSend.TypeId)
            {
                __result = new PartyAutoSend();
                return false;
            }

            Type nativeType = typeof(FlowUpgrade).Assembly.GetType("Assets.Source.Flow.Upgrades." + name);
            if (nativeType == null)
                return true;

            __result = (FlowUpgrade)Activator.CreateInstance(nativeType);
            return false;
        }
    }
}
