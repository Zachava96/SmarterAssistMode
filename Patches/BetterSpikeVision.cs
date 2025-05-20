using HarmonyLib;
using Rhythm;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace SmarterAssistMode
{
    [HarmonyPatch(typeof(RhythmController))]
    [HarmonyPatch("UpdateUpcoming")]
    public class BetterSpikeVision
    {
        //this patch turns this code:
        //
        /* if (dodgeNote.height == Height.Top)
        {
         this.upcomingDodgeTop = dodgeNote;
        }
        else if (dodgeNote.height == Height.Low)
        {
         this.upcomingDodgeLow = dodgeNote;
        } */

        // into this code:
        //
        /* if (dodgeNote.height == Height.Top && this.upcomingDodgeTop == null)
        {
         this.upcomingDodgeTop = dodgeNote;
        }
        else if (dodgeNote.height == Height.Low && this.upcomingDodgeLow == null)
        {
         this.upcomingDodgeLow = dodgeNote;
        } */

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            int upcomingDodgeTopAssignmentIndex = -1;
            Label elseIfHeightIsLowLabel = il.DefineLabel();
            int foundElseIfHeightIsLow = 0;
            int upcomingDodgeLowAssignmentIndex = -1;
            Label pastAssignmentsLabel = il.DefineLabel();


            var code = new List<CodeInstruction>(instructions);

            for (int i = 0; i < code.Count - 3; i++)
            {
                // Find index of the assignment to upcomingDodgeTop
                if (code[i].opcode == OpCodes.Ldarg_0 &&
                    code[i + 1].opcode == OpCodes.Ldloc_S &&
                    code[i + 2].opcode == OpCodes.Stfld &&
                    object.ReferenceEquals(code[i + 2].operand, AccessTools.Field(typeof(RhythmController), nameof(RhythmController.upcomingDodgeTop))))
                {
                    if (upcomingDodgeTopAssignmentIndex != -1)
                    {
                        //multiple possible code sections found, we error out
                        upcomingDodgeTopAssignmentIndex = -2;
                    }
                    else
                    {
                        upcomingDodgeTopAssignmentIndex = i;
                    }
                    ;
                }
                // Find the else if statement that checks if the height is low
                if (code[i].opcode == OpCodes.Ldloc_S &&
                    code[i + 1].opcode == OpCodes.Ldfld &&
                    object.ReferenceEquals(code[i + 1].operand, AccessTools.Field(typeof(BaseNote), nameof(BaseNote.height))) &&
                    code[i + 2].opcode == OpCodes.Ldc_I4_1)
                {
                    if (foundElseIfHeightIsLow == 0)
                    {
                        code[i].labels.Add(elseIfHeightIsLowLabel);
                        foundElseIfHeightIsLow = 1;
                    }
                    else
                    {
                        //multiple possible code sections found, we error out
                        foundElseIfHeightIsLow = -1;
                    }
                    
                }
                // Find index of the assignment to upcomingDodgeLow
                if (code[i].opcode == OpCodes.Ldarg_0 &&
                    code[i + 1].opcode == OpCodes.Ldloc_S &&
                    code[i + 2].opcode == OpCodes.Stfld &&
                    object.ReferenceEquals(code[i + 2].operand, AccessTools.Field(typeof(RhythmController), nameof(RhythmController.upcomingDodgeLow))))
                {
                    if (upcomingDodgeLowAssignmentIndex != -1)
                    {
                        //multiple possible code sections found, we error out
                        upcomingDodgeLowAssignmentIndex = -2;
                    }
                    else
                    {
                        upcomingDodgeLowAssignmentIndex = i;
                        code[i + 3].labels.Add(pastAssignmentsLabel);
                    }
                    
                }

            }

            if (upcomingDodgeLowAssignmentIndex < 0 ||
                upcomingDodgeTopAssignmentIndex < 0 ||
                foundElseIfHeightIsLow < 1)
            {
                SmarterAssistMode.Logger.LogError("Failed to find the assignment to upcomingDodgeTop or upcomingDodgeLow, or we couldn't find the dodgeNote.height comparison to Height.Low.");
                SmarterAssistMode.Logger.LogError($"upcomingDodgeTopAssignmentIndex: {upcomingDodgeTopAssignmentIndex}, upcomingDodgeLowAssignmentIndex: {upcomingDodgeLowAssignmentIndex}, foundElseIfHeightIsLow: {foundElseIfHeightIsLow}");
                return code;
            }

            var instructionsToInsertForTop = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(RhythmController), nameof(RhythmController.upcomingDodgeTop))),
                new CodeInstruction(OpCodes.Ldnull),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.Object), "op_Equality", new Type[] { typeof(UnityEngine.Object), typeof(UnityEngine.Object) })),
                new CodeInstruction(OpCodes.Brfalse_S, elseIfHeightIsLowLabel)
            };

            var instructionsToInsertForLow = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(RhythmController), nameof(RhythmController.upcomingDodgeLow))),
                new CodeInstruction(OpCodes.Ldnull),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.Object), "op_Equality", new Type[] { typeof(UnityEngine.Object), typeof(UnityEngine.Object) })),
                new CodeInstruction(OpCodes.Brfalse_S, pastAssignmentsLabel)
            };

            code.InsertRange(upcomingDodgeLowAssignmentIndex, instructionsToInsertForLow);
            code.InsertRange(upcomingDodgeTopAssignmentIndex, instructionsToInsertForTop);

            return code;
        }
    }
}