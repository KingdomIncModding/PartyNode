using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Behaviour.Flow;
using BepInEx;
using HarmonyLib;
using KingdomFoundation.Api;
using UnityEngine;

namespace KingdomPartyNode
{
    /// <summary>BepInEx entry point registering Party-Up research, workspace node, hooks, and patches.</summary>
    [BepInPlugin(Id, "Party-Up", Version)]
    [BepInDependency("urakka.kingdominc.foundation", "0.14.6")]
    public sealed class PartyNodePlugin : BaseUnityPlugin
    {
        public const string Id = "urakka.kingdominc.partynode";
        public const string Version = "0.7.1";

        private FoundationModContext context;
        private Harmony patches;
        private readonly List<IDisposable> hooks = new List<IDisposable>();
        private ITechNode partyResearch;
        private ITechNode autoSendResearch;
        private IWorkspaceNodeType partyType;

        internal static ITechNode PartyResearch => instance?.partyResearch;
        internal static ITechNode AutoSendResearch => instance?.autoSendResearch;
        internal static IWorkspaceNodeType PartyType => instance?.partyType;
        private static PartyNodePlugin instance;

        private void Awake()
        {
            instance = this;
            try
            {
                context = FoundationApi.ForMod(Id);
                patches = new Harmony(Id);
                patches.PatchAll(Assembly.GetExecutingAssembly());
                RegisterFoundation();
                RegisterHooks();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                Cleanup();
                throw;
            }

            Logger.LogInfo("Party Node ready: five archetype inputs, adjustable counts, one party output.");
        }

        private void OnDestroy()
        {
            Cleanup();
            if (instance == this)
                instance = null;
        }

        private void Cleanup()
        {
            foreach (IDisposable hook in hooks)
                hook.Dispose();
            hooks.Clear();
            context?.Dispose();
            context = null;
            patches?.UnpatchSelf();
            patches = null;
            PartyArtwork.Dispose();
        }

        private void RegisterFoundation()
        {
            GameResearchNode guild = FoundationApi.ResearchAccess.Find("adventurers_guild")
                ?? throw new InvalidOperationException("Adventurers' Guild research is unavailable.");

            partyResearch = context.Register(new TechNodeDefinition(
                Party.TechId,
                "Party Node",
                Id,
                "Party-Up",
                new Vector2(0f, 0f),
                "Assemble up to 5 adventurers. Press Send now to release.",
                PartyArtwork.Sprite,
                TechNodeOptions.Create(
                    nodeKind: "FlowElement",
                    costTier: guild.CostTier,
                    costMultiplier: 0.5,
                    goldMultiplier: 1.0,
                    requirements: new[] { guild.Identifier },
                    iconName: "Icons_17")));

            autoSendResearch = context.Register(new TechNodeDefinition(
                Party.AutoSendTechId,
                "Auto Send",
                Id,
                "Party-Up",
                new Vector2(270f, 0f),
                "Unlocks a Party Node upgrade slot. Use the node's + slot to buy Auto Send, which sends only complete selected lineups. Remove the upgrade to return to manual sending.",
                null,
                TechNodeOptions.Create(
                    nodeKind: "UpgradeSlot",
                    costTier: guild.CostTier,
                    costMultiplier: 0.5,
                    goldMultiplier: 1.0,
                    previousId: Party.TechId,
                    rootId: Party.TechId,
                    iconName: "Icons_17")));

            autoSendResearch.Clicked += _ =>
            {
                PartyAutoSend.Unlock(FoundationApi.World.Player);
                if (WorldFlowManager.Instance)
                    WorldFlowManager.Instance.UpdateFlowElements(Party.TypeId);
            };

            partyType = context.Register(new WorkspaceNodeDefinition(
                Party.TypeId,
                "Party Node",
                () => new Party(),
                Party.TechId,
                new Vector2Int(Party.Width, Party.Height),
                "Assemble a party of up to 5 adventurers total. Set each archetype's required count; 0 disables it. Press Send now to release the party. Research Auto Send, then install it in the node's + upgrade slot to automate complete lineups.",
                Assets.Source.Flow.ElementCategory.Utility,
                "Icons_104",
                PartyArtwork.RoomSprite,
                0.5,
                1.0));
        }

        private void RegisterHooks()
        {
            hooks.Add(FoundationApi.GameHooks.Subscribe(
                Id,
                GameHookPoints.PortBound,
                after: call => PartyPortColorsHook.Apply(call.View, call.Argument<WorkspacePort>(0))));
        }
    }
}
