using System;
using System.Reflection;
using Assets.Behaviour.Flow;
using Assets.Behaviour.Flow.Elements;
using Behaviour.UI.Tooltip;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomPartyNode
{
    /// <summary>Builds the custom 384x224 Party Node prefab with Forge upgrade slots and PartyWorld view.</summary>
    [HarmonyPatch(typeof(FlowPrefabManager), nameof(FlowPrefabManager.Get))]
    internal static class PartyPrefabPatch
    {
        private static readonly FieldInfo BackgroundField = AccessTools.Field(typeof(WorldFlowElement), "_background");
        private static readonly FieldInfo LabelField = AccessTools.Field(typeof(WorldFlowElement), "_label");
        private static readonly FieldInfo LabelContainerField = AccessTools.Field(typeof(WorldFlowElement), "_labelContainer");
        private static readonly FieldInfo UpgradeSlotsField = AccessTools.Field(typeof(WorldFlowElement), "_upgradeSlots");
        private static readonly FieldInfo UpgradePrefabField = AccessTools.Field(typeof(WorldFlowElement), "_upgradePrefab");
        private static readonly FieldInfo PrefabBackingField = AccessTools.Field(typeof(FlowPrefabSet), "<Prefab>k__BackingField");
        private static readonly FieldInfo DetailSpriteBackingField = AccessTools.Field(typeof(FlowPrefabSet), "<DetailSprite>k__BackingField");

        private static FlowPrefabManager owner;
        private static FlowPrefabSet custom;

        private static void Postfix(FlowPrefabManager __instance, string id, ref FlowPrefabSet __result)
        {
            if (id != Party.TypeId)
                return;

            if (!custom || owner != __instance)
            {
                custom = BuildPrefab(__instance);
                owner = __instance;
            }

            __result = custom;
        }

        private static FlowPrefabSet BuildPrefab(FlowPrefabManager manager)
        {
            FlowPrefabSet collector = manager.Get("Collector");
            if (!collector)
                throw new InvalidOperationException("Collector prefab is unavailable.");

            GameObject holder = new GameObject("PartyNodeAssets");
            holder.transform.SetParent(manager.transform, false);
            holder.SetActive(false);

            FlowPrefabSet prefabSet = UnityEngine.Object.Instantiate(collector, holder.transform, false);
            prefabSet.Identifier = Party.TypeId;
            prefabSet.name = Party.TypeId;

            WorldFlowElement world = UnityEngine.Object.Instantiate(collector.Prefab, holder.transform, false);
            world.name = "PartyNodePrefab";
            ((RectTransform)world.transform).sizeDelta = new Vector2(Party.Width, Party.Height);

            WorldFlowCollector collectorComponent = world.GetComponent<WorldFlowCollector>();
            if (collectorComponent)
                UnityEngine.Object.DestroyImmediate(collectorComponent);

            DisableNativeTemplateVisuals(world);
            AttachForgeUpgradeSlot(manager, world);
            world.gameObject.AddComponent<PartyWorld>();

            PrefabBackingField.SetValue(prefabSet, world);
            DetailSpriteBackingField.SetValue(prefabSet, PartyArtwork.Sprite);
            return prefabSet;
        }

        private static void DisableNativeTemplateVisuals(WorldFlowElement world)
        {
            Image background = BackgroundField.GetValue(world) as Image;
            TMP_Text label = LabelField.GetValue(world) as TMP_Text;
            Image labelContainer = LabelContainerField.GetValue(world) as Image;

            foreach (Graphic graphic in world.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic != background && graphic != label && graphic != labelContainer)
                    graphic.enabled = false;
            }

            foreach (Button button in world.GetComponentsInChildren<Button>(true))
                button.enabled = false;

            foreach (TooltipSource tooltip in world.GetComponentsInChildren<TooltipSource>(true))
                tooltip.enabled = false;
        }

        private static void AttachForgeUpgradeSlot(FlowPrefabManager manager, WorldFlowElement world)
        {
            WorldFlowElement forgePrefab = manager.Get("Forge")?.Prefab;
            if (!forgePrefab)
                return;

            RectTransform oldSlots = UpgradeSlotsField.GetValue(world) as RectTransform;
            if (oldSlots)
                UnityEngine.Object.DestroyImmediate(oldSlots.gameObject);

            RectTransform forgeSlots = UpgradeSlotsField.GetValue(forgePrefab) as RectTransform;
            if (forgeSlots)
            {
                RectTransform slots = UnityEngine.Object.Instantiate(forgeSlots, world.transform, false);
                slots.pivot = slots.anchorMax = slots.anchorMin = new Vector2(0f, 1f);
                slots.anchoredPosition = new Vector2(36f, -180f);
                slots.sizeDelta = new Vector2(36f, 36f);
                UpgradeSlotsField.SetValue(world, slots);
            }

            UpgradePrefabField.SetValue(world, UpgradePrefabField.GetValue(forgePrefab));
        }
    }
}
