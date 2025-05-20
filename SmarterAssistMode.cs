using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using Rhythm;
using HarmonyLib;
using UnityEngine;

namespace SmarterAssistMode
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("UNBEATABLE [DEMO].exe")]
    [BepInProcess("UNBEATABLE [white label].exe")]
    public class SmarterAssistMode : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "net.zachava.smarterassistmode";
        public const string PLUGIN_NAME = "Smarter Assist Mode";
        public const string PLUGIN_VERSION = "1.0.0";
        internal static new ManualLogSource Logger;
        public static ConfigEntry<bool> BetterSpikeVisionEnabled;
        public static ConfigEntry<bool> BetterSpikeInputsEnabled;
        public static ConfigEntry<double> NoteTargetOffsetValue;
        public static ConfigEntry<double> SpikeTargetOffsetValue;
        public static ConfigEntry<bool> OverlyVerboseLoggingEnabled;
        private static readonly Harmony Harmony = new Harmony(PLUGIN_GUID);

        public static void LogVerbose(object message)
        {
            if (OverlyVerboseLoggingEnabled.Value)
            {
                Logger.LogDebug(message);
            }
        }

        private void Awake()
        {
            Logger = base.Logger;
            BetterSpikeVisionEnabled = Config.Bind(
                "General",
                "BetterSpikeVisionEnabled",
                true,
                "If Assist Mode will not ignore multiple spike notes in a lane."
            );

            BetterSpikeInputsEnabled = Config.Bind(
                "General",
                "BetterSpikeInputsEnabled",
                true,
                "If Assist Mode will not attempt to dodge spikes that won't currently hit it."
            );

            NoteTargetOffsetValue = Config.Bind(
                "General",
                "NoteTargetOffsetValue",
                0D,
                "How many milliseconds offset from the note Assist Mode will attempt to hit notes.\n" +
                "Negative values will make it hit earlier, positive values will make it hit later.\n" +
                "This is 0 in the base game, and results in every note hit being either exactly on-time or slightly late.\n" +
                "Enter 0 to disable this feature."
            );

            SpikeTargetOffsetValue = Config.Bind(
                "General",
                "SpikeTargetOffsetValue",
                -30D,
                "How many milliseconds offset from spikes Assist Mode will attempt to dodge spikes.\n" +
                "Negative values will make it dodge earlier, positive values will make it dodge later.\n" +
                "This is effectively -30 in the base game.\n" +
                "Enter -30 to disable this feature."
            );

            OverlyVerboseLoggingEnabled = Config.Bind(
                "General",
                "OverlyVerboseLoggingEnabled",
                false,
                "Developer use. You probably don't want this."
            );

            Logger.LogInfo($"Plugin {PLUGIN_GUID} is loaded!");

            List<Type> classesToPatch = new List<Type>();

            if (BetterSpikeVisionEnabled.Value)
            {
                Logger.LogInfo("We will patch for Better Spike Vision");
                classesToPatch.Add(typeof(BetterSpikeVision));
            }
            else
            {
                Logger.LogInfo("We will NOT patch for Better Spike Vision");
            }

            if (BetterSpikeInputsEnabled.Value)
            {
                Logger.LogInfo("We will patch for Better Spike Inputs");
                classesToPatch.Add(typeof(BetterSpikeInputs));
            }
            else
            {
                Logger.LogInfo("We will NOT patch for Better Spike Inputs");
            }

            if (NoteTargetOffsetValue.Value != 0)
            {
                Logger.LogInfo($"We will patch for Note Target Offset: {NoteTargetOffsetValue.Value}ms");
                classesToPatch.Add(typeof(NoteTargetOffset));
            }
            else
            {
                Logger.LogInfo("We will NOT patch for Note Target Offset");
            }

            if (SpikeTargetOffsetValue.Value != -30)
            {
                Logger.LogInfo($"We will patch for Spike Target Offset: {SpikeTargetOffsetValue.Value}ms");
                classesToPatch.Add(typeof(SpikeTargetOffset));
            }
            else
            {
                Logger.LogInfo("We will NOT patch for Spike Target Offset");
            }

            foreach (var toPatch in classesToPatch)
            {
                try
                {
                    Logger.LogDebug($"Patching {toPatch.Name}");
                    Harmony.CreateAndPatchAll(toPatch, PLUGIN_GUID);
                }
                catch (Exception e)
                {
                    Logger.LogError($"Failed to patch {toPatch.Name}: {e}");
                }

            }
        }
    }
}
