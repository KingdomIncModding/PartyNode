using System.Reflection;
using Assets.Behaviour.UI.TechTree;
using Assets.Source.Player;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomPartyNode
{
    /// <summary>Preserves aspect ratio and wide layout for Party Node artwork in the tech tree.</summary>
    [HarmonyPatch(typeof(UIElementTechNode), nameof(UIElementTechNode.SetTech))]
    internal static class PartyTechArtworkPatch
    {
        private static readonly FieldInfo IconField = AccessTools.Field(typeof(UITechNode), "_icon");
        private static readonly FieldInfo DetailImageField = AccessTools.Field(typeof(UIElementTechNode), "_detailImage");
        private static readonly FieldInfo DescriptionField = AccessTools.Field(typeof(UIElementTechNode), "_description");

        private static void Postfix(UIElementTechNode __instance, TechNode node)
        {
            if (node == null || node.Identifier != Party.TechId)
                return;

            if (IconField.GetValue(__instance) is Image icon && icon)
            {
                icon.preserveAspect = true;
                icon.rectTransform.sizeDelta = node.HasChildNodes ? new Vector2(64f, 64f) : new Vector2(144f, 80f);
            }

            if (DetailImageField.GetValue(__instance) is Image detail && detail)
            {
                detail.preserveAspect = true;
                detail.rectTransform.sizeDelta = new Vector2(150f, 82f);
            }

            if (DescriptionField.GetValue(__instance) is TMP_Text description && description && node.HasChildNodes)
                description.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 170f);
        }
    }
}
