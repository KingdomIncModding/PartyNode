using Assets.Source.Items;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomPartyNode
{
    /// <summary>Animation state for an individual adventurer pacing inside the meetup room.</summary>
    internal sealed class PartyWalker
    {
        internal InventoryItem Item;
        internal RectTransform Root;
        internal Image Image;
        internal float Distance;
        internal float Speed;
        internal float Phase;
        internal float Age;
        internal int Direction;
        internal Vector2 Ground;
    }
}
