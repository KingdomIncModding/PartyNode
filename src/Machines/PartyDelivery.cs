using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Items;

namespace KingdomPartyNode
{
    /// <summary>Plans atomic whole-party deliveries across receiver buffers and internal capacities.</summary>
    internal static class PartyDelivery
    {
        internal static bool Plan(FlowOutput output, IReadOnlyList<InventoryItem> members, out FlowInput[] slots, out string reason)
        {
            slots = null;
            reason = null;
            if (members.Count == 0)
                return false;

            FlowPipe pipe = output.ConnectedPipe;
            FlowInput input = pipe?.ConnectedInput;
            if (input == null || pipe.ConnectedOutput != output || input.ConnectedPipe != pipe || input.Parent == output.Parent)
            {
                reason = "Connect output";
                return false;
            }

            int? internalFree = GetInternalFreeCapacity(input);
            if (internalFree.HasValue)
            {
                if (internalFree.Value - input.BufferedItems.Count < members.Count)
                {
                    reason = "Need room \u00b7 " + members.Count;
                    return false;
                }
                if (members.Any(member => !input.CanAcceptItem(member)))
                {
                    reason = "Target rejects";
                    return false;
                }
                slots = Enumerable.Repeat(input, members.Count).ToArray();
                return true;
            }

            List<FlowInput> targets = CollectTargetInputs(input);
            int[] free = targets.Select(i => Math.Max(0, i.BufferSize - i.BufferedItems.Count)).ToArray();
            if (free.Sum() < members.Count)
            {
                reason = "Need room \u00b7 " + members.Count;
                return false;
            }

            FlowInput[] planned = new FlowInput[members.Count];
            if (!Assign(0, members, targets, free, planned))
            {
                reason = "Target rejects";
                return false;
            }

            slots = planned;
            return true;
        }

        private static int? GetInternalFreeCapacity(FlowInput input)
        {
            if (input.ParentIndex != 0)
                return null;

            return input.Parent switch
            {
                Stockpile stockpile => stockpile.Capacity - stockpile.Contents.Count,
                Inn inn => inn.Contents.Count(slot => slot == null),
                TrainingGrounds training => training.Capacity - training.Contents.Count(slot => slot != null),
                QuestBoard questBoard => QuestBoard.SlotCount - questBoard.Contents.Count,
                _ => null
            };
        }

        private static List<FlowInput> CollectTargetInputs(FlowInput primary)
        {
            List<FlowInput> targets = new List<FlowInput> { primary };
            if (primary.Parent is Dungeon || primary.Parent is Party)
            {
                targets.AddRange(primary.Parent.Inputs.Where(i => i != primary && i.ConnectedPipe == null));
            }
            return targets;
        }

        private static bool Assign(int member, IReadOnlyList<InventoryItem> members, List<FlowInput> inputs, int[] free, FlowInput[] plan)
        {
            if (member == members.Count)
                return true;

            for (int i = 0; i < inputs.Count; i++)
            {
                if (free[i] == 0 || !inputs[i].CanAcceptItem(members[member]))
                    continue;

                free[i]--;
                plan[member] = inputs[i];
                if (Assign(member + 1, members, inputs, free, plan))
                    return true;
                free[i]++;
            }

            return false;
        }
    }
}
