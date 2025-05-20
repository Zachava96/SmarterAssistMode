using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Rhythm;

namespace SmarterAssistMode
{
    [HarmonyPatch(typeof(RhythmController))]
    [HarmonyPatch("Awake")]
    public class NoteTargetOffset
    {
        //just changing the assistLeniency to be note offset we want
        //this is zero by default
        static void Postfix(RhythmController __instance)
        {
            __instance.assistLeniency = (float)(SmarterAssistMode.NoteTargetOffsetValue.Value / __instance.leniencyMilliseconds);
        }
    }

    [HarmonyPatch(typeof(RhythmPlayer))]
    [HarmonyPatch("RhythmUpdateBegin")]
    public class SpikeTargetOffset
    {
        //changes this code:
        //float num2 = 0.8f * this.rhythm.leniencyMilliseconds;
        //to replace 0.8f with the new value
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            float newSpikeLeniency = (150f + (float)SmarterAssistMode.SpikeTargetOffsetValue.Value) / 150f;

            var code = new List<CodeInstruction>(instructions);

            for (int i = 0; i < code.Count; i++)
            {
                if (code[i].opcode == OpCodes.Ldc_R4 &&
                    code[i].operand is float f && f == 0.8f)
                {
                    code[i].operand = newSpikeLeniency;
                    SmarterAssistMode.LogVerbose($"Changed spike leniency from 0.8f to {newSpikeLeniency}f");
                    return code;
                }
            }

            SmarterAssistMode.Logger.LogError("Failed to find the spike leniency assignment, this is a bug. Returning unmodified code.");
            return code;
        }
    }
}