using Assets.Source.Player;
using HarmonyLib;

namespace KingdomPartyNode
{
    /// <summary>Restores the native Party Node upgrade slot and Auto Send availability on loaded saves.</summary>
    [HarmonyPatch(typeof(GamePlayer), nameof(GamePlayer.FromJson))]
    internal static class PartyUpgradeMigrationPatch
    {
        private static void Postfix(GamePlayer __result)
        {
            if (__result != null && __result.HasTech(TechNode.Get(Party.AutoSendTechId)))
                PartyAutoSend.Unlock(__result);
        }
    }
}
