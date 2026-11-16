using HarmonyLib;
using UnityEngine;

namespace KingdomPartyNode
{
    /// <summary>Resolves custom Party Meetup sprite identifiers through SpriteLibrary.</summary>
    [HarmonyPatch(typeof(SpriteLibrary), nameof(SpriteLibrary.Get), new[] { typeof(string) })]
    internal static class PartySpritePatch
    {
        private static bool Prefix(string name, ref Sprite __result)
        {
            if (name == PartyArtwork.IconId)
            {
                __result = PartyArtwork.Sprite;
                return false;
            }

            if (name == PartyArtwork.RoomIconId)
            {
                __result = PartyArtwork.RoomSprite;
                return false;
            }

            return true;
        }
    }
}
