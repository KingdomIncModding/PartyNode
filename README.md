# Party-Up

[![Support on Ko-fi](https://raw.githubusercontent.com/KingdomIncModding/.github/main/assets/kofi-button.png)](https://ko-fi.com/urakkaamyx/tip)

Assemble an exact party of up to five adventurers, then send everyone together.

## Features

- **Party Node building:** Unlocked via **Tech Tree > Mods > Party-Up** after researching **Adventurers' Guild**, then built from **Utility**.
- **Flexible lineup up to 5 members:** Configure required counts across **Adventurer**, **Fighter**, **Rogue**, **Mage**, and **Cleric** using the node's gear icon (`0` disables a class; `Adventurer` accepts ordinary adventurers only; advanced classes match their base archetype).
- **Manual or Auto Send:** **Send now** is the default, allowing full or partial party releases when the receiver has room. Research **Auto Send** in the Party Node branch and install it in the node's `+` upgrade slot to automatically send complete lineups.
- **Atomic whole-party transfers:** Checks real internal occupancy and queued arrivals on stockpiles, inns, quest boards, training grounds, dungeons, and other Party Nodes before releasing anyone. A blocked party stays buffered without partial transfers.
- **Live meetup room & save preservation:** Waiting adventurers walk around the meetup room table, and saves preserve equipment, quality, identities, settings, and upgrades.

## Installation

1. Close Kingdom Inc. and install **BepInEx 5** and **Foundation 0.14.6+** separately.
2. Extract the mod folder into `BepInEx/plugins/PartyUp/` (containing `PartyUp.dll` and `mod.json`).
3. Start the game, research **Party Node** under **Tech Tree > Mods > Party-Up**, and build it from **Utility**.

## Updating and Removal

- Before uninstalling, send any waiting adventurers, remove any placed Party Nodes, save, and close the game.
- Back up your kingdom save before adding or removing mods.

## Compatibility

| Class.Method | Hook Type | Impact |
|---|---|---|
| `FlowIO.CanAcceptItem` | Prefix | Enforces Party Node per-slot archetype filters and five-member party acceptance rules. |
| `FlowPipe.Update` | Prefix | Transfers buffered Party Node members atomically only when the receiver has room for the entire batch. |
| `FlowPrefabManager.Get` | Postfix | Supplies the custom 384x224 Party Node prefab, upgrade slot, and meetup artwork. |
| `FlowUpgrade.Create` | Prefix | Instantiates the `PartyAutoSend` upgrade from saved games and resolves native upgrades cleanly. |
| `GamePlayer.FromJson` | Postfix | Restores the Party Node upgrade slot and Auto Send availability when loading researched saves. |
| `SpriteLibrary.Get` | Prefix | Resolves the custom Party Meetup room and research icon sprites. |
| `UIElementTechNode.SetTech` | Postfix | Preserves aspect ratio and wide layout for Party Node research artwork. |
| `BuyMenuRow.SetElement` | Postfix | Preserves aspect ratio for the Party Node build-menu icon. |
| `WorldFlowIO.SetIO` | Foundation `PortBound` | Keeps Party Node input ports green and output port blue. |

## Support

If you enjoy these Kingdom Inc mods and tools, consider supporting development on [**Ko-fi**](https://ko-fi.com/urakkaamyx/tip)!
