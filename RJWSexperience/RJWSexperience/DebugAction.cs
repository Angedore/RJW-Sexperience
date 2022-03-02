﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using rjw;

namespace RJWSexperience
{
    public static class DebugToolsSexperience
    {
        [DebugAction("RJW Sexperience", "Reset pawn's record", false, false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ResetRecords(Pawn p)
        {
            Trait virgin = p.story?.traits?.GetTrait(VariousDefOf.Virgin);
            if (virgin != null) p.story.traits.RemoveTrait(virgin);
            p.ResetRecord(true);
            p.ResetRecord(false);
            MoteMaker.ThrowText(p.TrueCenter(), p.Map, "Records resetted!");
        }

        [DebugAction("RJW Sexperience", "Reset pawn's record(virgin)", false, false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ResetRecordsZero(Pawn p)
        {
            p.ResetRecord(true);
            p.AddVirginTrait();
            MoteMaker.ThrowText(p.TrueCenter(), p.Map, "Records resetted!\nVirginified!");
        }


        [DebugAction("RJW Sexperience", "Reset lust", false, false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ResetLust(Pawn p)
        {
            float lust = RecordRandomizer.RandomizeLust(p);
            MoteMaker.ThrowText(p.TrueCenter(), p.Map, "Lust: " + lust);
        }

        [DebugAction("RJW Sexperience", "Set lust to 0", false, false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SetLust(Pawn p)
        {
            p.records.SetTo(VariousDefOf.Lust, 0);
            MoteMaker.ThrowText(p.TrueCenter(), p.Map, "Lust: 0");
        }


        [DebugAction("RJW Sexperience", "Add 10 to lust", false, false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void AddLust(Pawn p)
        {
            p.records.AddTo(VariousDefOf.Lust, 10);
            MoteMaker.ThrowText(p.TrueCenter(), p.Map, "Lust: " + p.records.GetValue(VariousDefOf.Lust));
        }

        [DebugAction("RJW Sexperience", "Subtract 10 to lust", false, false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SubtractLust(Pawn p)
        {
            p.records.AddTo(VariousDefOf.Lust, -10);
            MoteMaker.ThrowText(p.TrueCenter(), p.Map, "Lust: " + p.records.GetValue(VariousDefOf.Lust));
        }

    }
}
