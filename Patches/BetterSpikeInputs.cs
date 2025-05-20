using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Rhythm;

namespace SmarterAssistMode
{
    [HarmonyPatch(typeof(RhythmPlayer))]
    [HarmonyPatch("RhythmUpdateBegin")]
    public class BetterSpikeInputs
    {
        //this patch turns this code:
        //
        /* if (upcomingDodgeLow != null && upcomingDodgeLow.WithinHitRange(upcomingDodgeLow.hitTime + num2))
		{
			this.input.GetAssistAction(Height.Top).Press();
		}
		if (upcomingDodgeTop != null && upcomingDodgeTop.WithinHitRange(upcomingDodgeTop.hitTime + num2))
		{
			this.input.GetAssistAction(Height.Low).Press();
		} */

        // into this code:
        //
        /* if (this.height == Height.Low && upcomingDodgeLow != null && upcomingDodgeLow.WithinHitRange(upcomingDodgeLow.hitTime + num2))
		{
			this.input.GetAssistAction(Height.Top).Press();
		}
		if (this.height == Height.Top && upcomingDodgeTop != null && upcomingDodgeTop.WithinHitRange(upcomingDodgeTop.hitTime + num2))
		{
			this.input.GetAssistAction(Height.Low).Press();
		} */
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            int dodgeLowIndex = -1;
            int dodgeTopIndex = -1;
            Label startOfDodgeTopIfStatementLabel = il.DefineLabel();
            Label startOfIfanyLowLabel = il.DefineLabel();
            int foundStartOfIfanyLow = 0;

            var code = new List<CodeInstruction>(instructions);

            for (int i = 0; i < code.Count; i++)
            {
                SmarterAssistMode.LogVerbose($"Original code [{i}]: {code[i]}");
            }

            //if (this.height == Height.Low)
            var instructionsToInsertForLow = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(RhythmBaseCharacter), nameof(RhythmBaseCharacter.height))),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Bne_Un_S, startOfDodgeTopIfStatementLabel)
            };

            //if (this.height == Height.Top)
            var instructionsToInsertForTop = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(RhythmBaseCharacter), nameof(RhythmBaseCharacter.height))),
                new CodeInstruction(OpCodes.Ldc_I4_3),
                new CodeInstruction(OpCodes.Bne_Un_S, startOfIfanyLowLabel)
            };

            instructionsToInsertForTop[0].labels.Add(startOfDodgeTopIfStatementLabel);
            
            //TODO: perform checks to make sure we find only one match for each block of code we want
            for (int i = 0; i < code.Count - 3; i++)
            {
                //looking for "if (upcomingDodgeLow != null)"
                if (code[i].opcode == OpCodes.Ldloc_2 &&
                    code[i + 1].opcode == OpCodes.Ldnull &&
                    code[i + 2].opcode == OpCodes.Call &&
                    object.ReferenceEquals(code[i + 2].operand, AccessTools.Method(typeof(UnityEngine.Object), "op_Inequality", new Type[] { typeof(UnityEngine.Object), typeof(UnityEngine.Object) })) &&
                    (code[i + 3].opcode == OpCodes.Brfalse_S || code[i + 3].opcode == OpCodes.Brfalse))
                {
                    if (dodgeLowIndex != -1)
                    {
                        dodgeLowIndex = -2;
                    }
                    else
                    {
                        dodgeLowIndex = i;
                        CodeInstructionExtensions.MoveLabelsTo(code[i], instructionsToInsertForLow[0]);
                    }
                }

                //looking for "if (upcomingDodgeTop != null)"
                if (code[i].opcode == OpCodes.Ldloc_3 &&
                    code[i + 1].opcode == OpCodes.Ldnull &&
                    code[i + 2].opcode == OpCodes.Call &&
                    object.ReferenceEquals(code[i + 2].operand, AccessTools.Method(typeof(UnityEngine.Object), "op_Inequality", new Type[] { typeof(UnityEngine.Object), typeof(UnityEngine.Object) })) &&
                    (code[i + 3].opcode == OpCodes.Brfalse_S || code[i + 3].opcode == OpCodes.Brfalse))
                {
                    if (dodgeTopIndex != -1)
                    {
                        dodgeTopIndex = -2;
                    }
                    else
                    {
                        dodgeTopIndex = i;
                        CodeInstructionExtensions.MoveLabelsTo(code[i], instructionsToInsertForTop[0]);
                    }
                }

                //looking for "if (this.input.anyLow)"
                if (code[i].opcode == OpCodes.Ldarg_0 &&
                    code[i + 1].opcode == OpCodes.Ldfld &&
                    object.ReferenceEquals(code[i + 1].operand, AccessTools.Field(typeof(RhythmPlayer), nameof(RhythmPlayer.input))) &&
                    code[i + 2].opcode == OpCodes.Callvirt &&
                    object.ReferenceEquals(code[i + 2].operand, AccessTools.PropertyGetter(typeof(RhythmPlayerInput), "anyLow")))
                {
                    if (foundStartOfIfanyLow != 0)
                    {
                        foundStartOfIfanyLow = -1;
                    }
                    else
                    {
                        code[i].labels.Add(startOfIfanyLowLabel);
                        foundStartOfIfanyLow = 1;
                    }
                }
            }

            if (dodgeLowIndex < 0 ||
                dodgeTopIndex < 0 ||
                foundStartOfIfanyLow < 1)
            {
                SmarterAssistMode.Logger.LogError("Failed to find the checks for upcomingDodgeLow or upcomingDodgeTop, or we couldn't find the if statement for this.input.anyLow.");
                SmarterAssistMode.Logger.LogError($"dodgeLowIndex: {dodgeLowIndex}, dodgeTopIndex: {dodgeTopIndex}, foundStartOfIfanyLow: {foundStartOfIfanyLow}");
                SmarterAssistMode.Logger.LogError("Returning unmodified code.");
                return instructions;
            }



            code.InsertRange(dodgeTopIndex, instructionsToInsertForTop);
            code.InsertRange(dodgeLowIndex, instructionsToInsertForLow);

            for (int i = 0; i < code.Count; i++)
            {
                SmarterAssistMode.LogVerbose($"Modified code[{i}]: {code[i]}");
            }

            return code;
        }
    }
}