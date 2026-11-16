using System.Reflection;
using Assets.Behaviour.UI.BuyMenu;
using Assets.Source.Flow;
using HarmonyLib;
using UnityEngine.UI;

namespace KingdomPartyNode
{
    /// <summary>Preserves aspect ratio on the Party Node build-menu icon.</summary>
    [HarmonyPatch(typeof(BuyMenuRow), nameof(BuyMenuRow.SetElement))]
    internal static class PartyBuyMenuArtworkPatch
    {
        private static readonly FieldInfo IconField = AccessTools.Field(typeof(BuyMenuRow), "_icon");

        private static void Postfix(BuyMenuRow __instance, FlowElement el)
        {
            if (el is Party && IconField.GetValue(__instance) is Image icon && icon)
                icon.preserveAspect = true;
        }
    }
}
