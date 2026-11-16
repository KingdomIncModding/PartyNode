using System.Reflection;
using Assets.Behaviour.Flow;
using HarmonyLib;
using KingdomFoundation.Api;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomPartyNode
{
    /// <summary>Keeps Party input ports green and output port blue even before buffered items settle.</summary>
    internal static class PartyPortColorsHook
    {
        private static readonly FieldInfo ErrorSpritesField = AccessTools.Field(typeof(WorldFlowIO), "_errorSprites");

        internal static void Apply(Component view, WorkspacePort port)
        {
            if (!view || port == null)
                return;

            WorldFlowElement elementView = view.GetComponentInParent<WorldFlowElement>();
            if (!elementView || !(elementView.Element is Party))
                return;

            Image image = FoundationApi.WorkspaceVisuals.PortGraphic(view, GamePortGraphic.Sprite) as Image;
            if (!image)
                return;

            int spriteIndex = port.HasBuffer ? 0 : 2;
            WorkspacePipe pipe = port.ConnectedPipe;
            if (pipe != null && pipe.ItemRejected)
            {
                Sprite[] errorSprites = ErrorSpritesField.GetValue(view) as Sprite[];
                if (errorSprites != null && spriteIndex < errorSprites.Length)
                    image.sprite = errorSprites[spriteIndex];
                return;
            }

            image.sprite = FoundationApi.WorkspaceVisuals.PortSprite(view, port.IsInput, spriteIndex);
        }
    }
}
