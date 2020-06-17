using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ZLevels
{
    [StaticConstructorOnStartup]
    public static class FloatMenuPatches
    {
        [HarmonyPatch(typeof(FloatMenuOption), "Disabled", MethodType.Getter)]
        internal static class Patch_FloatDisabled
        {
            private static bool Prefix(FloatMenuOption __instance, bool __result)
            {
                if (__instance.Label == "GoDown".Translate() || __instance.Label == "GoUP".Translate())
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(FloatMenuOption), "Chosen")]
        internal static class Patch_FloatMenuOption
        {
            private static void Postfix(FloatMenuOption __instance, bool colonistOrdering, FloatMenu floatMenu)
            {
                if (__instance.Label == "GoDown".Translate() || __instance.Label == "GoUP".Translate())
                {
                    if (Find.Selector.SelectedObjects.Where(x => x is Pawn).Count() > 1)
                    {
                        Thing thing;
                        foreach (var pawn in Find.Selector.SelectedObjects.Where(x => x is Pawn))
                        {
                            if (__instance.Label == "GoDown".Translate())
                            {
                                thing = GenClosest.ClosestThing_Global_Reachable(UI.MouseMapPosition().ToIntVec3()
                                    , ((Pawn)pawn).Map, ((Pawn)pawn).Map.listerThings.AllThings
                                    .Where(x => x is Building_StairsDown), PathEndMode.OnCell,
                                    TraverseParms.For(TraverseMode.ByPawn, Danger.Deadly, false), 9999f);
                                Job job = JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToStairs, thing);
                                ((Pawn)pawn).jobs.StartJob(job, JobCondition.InterruptForced);
                            }
                            else
                            {
                                thing = GenClosest.ClosestThing_Global_Reachable(UI.MouseMapPosition().ToIntVec3()
                                    , ((Pawn)pawn).Map, ((Pawn)pawn).Map.listerThings.AllThings
                                    .Where(x => x is Building_StairsUp), PathEndMode.OnCell,
                                    TraverseParms.For(TraverseMode.ByPawn, Danger.Deadly, false), 9999f);
                                Job job = JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToStairs, thing);
                                ((Pawn)pawn).jobs.StartJob(job, JobCondition.InterruptForced);
                            }
                        }
                        ZLogger.Message("Choosen");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
        public class AddHumanlikeOrders_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
            {
                Pawn pawn2 = GridsUtility.GetThingList(IntVec3.FromVector3(clickPos), pawn.Map)
                    .FirstOrDefault((Thing x) => x is Pawn) as Pawn;
                var mapComp = pawn.Map.GetComponent<MapComponentZLevel>();
                var ZTracker = Current.Game.GetComponent<ZLevelsManager>();

                if (pawn2 != null && ZTracker.ZLevelsTracker[pawn.Map.Tile].ZLevels.Count > 1)
                {
                    TaggedString toCheck = "Rescue".Translate(pawn2.LabelCap, pawn2);
                    FloatMenuOption floatMenuOption = opts.FirstOrDefault((FloatMenuOption x) => x.Label.Contains
                    (toCheck));
                    if (floatMenuOption != null)
                    {
                        opts.Remove(floatMenuOption);
                        opts.Add(AddHumanlikeOrders_Patch.addRescueOption(pawn, pawn2));
                    }
                    TaggedString toCheck2 = "Capture".Translate(pawn2.LabelCap, pawn2);
                    FloatMenuOption floatMenuOption2 = opts.FirstOrDefault((FloatMenuOption x) => x.Label.Contains
                    (toCheck2));
                    if (floatMenuOption2 != null)
                    {
                        opts.Remove(floatMenuOption2);
                        opts.Add(AddHumanlikeOrders_Patch.addCaptureOption(pawn, pawn2));
                    }
                }
            }

            public static FloatMenuOption addCaptureOption(Pawn pawn, Pawn victim)
            {
                var floatOption = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Capture".Translate
                    (victim.LabelCap, victim), delegate ()
                    {
                        Building_Bed building_Bed = RestUtility.FindBedFor(victim, pawn, true, false, false);
                        if (building_Bed == null)
                        {
                            building_Bed = RestUtility.FindBedFor(victim, pawn, true, false, true);
                        }
                        if (building_Bed == null)
                        {
                            var mapComp = pawn.Map.GetComponent<MapComponentZLevel>();
                            var oldMap = pawn.Map;
                            var oldPosition1 = pawn.Position;
                            var oldPosition2 = victim.Position;
                            var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                            foreach (var otherMap in ZTracker.GetAllMaps(pawn.Map.Tile))
                            {
                                if (oldMap != otherMap)
                                {
                                    var mapComp2 = otherMap.GetComponent<MapComponentZLevel>();
                                    var stairs = new List<Thing>();
                                    if (mapComp2.Z_LevelIndex >= 0)
                                    {

                                        Map lowerMap = ZTracker.GetLowerLevel(otherMap.Tile, otherMap);
                                        if (lowerMap != null)
                                        {
                                            stairs = lowerMap.listerThings.AllThings.Where(x => x is Building_StairsUp).ToList();
                                        }
                                    }
                                    else
                                    {
                                        Map upperMap = ZTracker.GetUpperLevel(otherMap.Tile, otherMap);
                                        if (upperMap != null)
                                        {
                                            stairs = upperMap.listerThings.AllThings.Where(x => x is Building_StairsDown).ToList();
                                        }
                                    }
                                    if (stairs != null && stairs.Count() > 0)
                                    {
                                        var selectedStairs = GenClosest.ClosestThing_Global(pawn.Position, stairs, 99999f);
                                        var position = selectedStairs.Position;
                                        bool select = false;
                                        if (Find.Selector.SelectedObjects.Contains(pawn)) select = true;
                                        JobManagerPatches.manualDespawn = true;
                                        pawn.DeSpawn();
                                        JobManagerPatches.manualDespawn = false;
                                        GenPlace.TryPlaceThing(pawn, position, otherMap, ThingPlaceMode.Direct);

                                        JobManagerPatches.manualDespawn = true;
                                        victim.DeSpawn();
                                        JobManagerPatches.manualDespawn = false;
                                        GenPlace.TryPlaceThing(victim, position, otherMap, ThingPlaceMode.Direct);
                                        building_Bed = RestUtility.FindBedFor(victim, pawn, true, false, false);
                                        if (building_Bed == null)
                                        {
                                            building_Bed = RestUtility.FindBedFor(victim, pawn, true, false, true);
                                        }

                                        JobManagerPatches.manualDespawn = true;
                                        pawn.DeSpawn();
                                        JobManagerPatches.manualDespawn = false;
                                        GenPlace.TryPlaceThing(pawn, oldPosition1, oldMap, ThingPlaceMode.Direct);
                                        if (select) Find.Selector.Select(pawn);

                                        JobManagerPatches.manualDespawn = true;
                                        victim.DeSpawn();
                                        JobManagerPatches.manualDespawn = false;
                                        GenPlace.TryPlaceThing(victim, oldPosition2, oldMap, ThingPlaceMode.Direct);

                                        if (building_Bed != null)
                                        {
                                            ZLogger.Message("Found bed: " + building_Bed);
                                            Job captureJob = JobMaker.MakeJob(JobDefOf.Capture, victim, building_Bed);
                                            captureJob.count = 1;
                                            ZTracker.BuildJobListFor(pawn, oldMap, oldMap, captureJob, null);
                                            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Rescuing, KnowledgeAmount.Total);
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                        if (building_Bed == null)
                        {
                            Messages.Message("CannotCapture".Translate() + ": " + "NoPrisonerBed".Translate(), victim, MessageTypeDefOf.RejectInput, false);
                            return;
                        }
                        Job job = JobMaker.MakeJob(JobDefOf.Capture, victim, building_Bed);
                        job.count = 1;
                        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                        PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Capturing, KnowledgeAmount.Total);
                        if (victim.Faction != null && victim.Faction != Faction.OfPlayer && !victim.Faction.def.hidden && !victim.Faction.HostileTo(Faction.OfPlayer) && !victim.IsPrisonerOfColony)
                        {
                            Messages.Message("MessageCapturingWillAngerFaction".Translate(victim.Named("PAWN"))
                                .AdjustedFor(victim, "PAWN", true), victim, MessageTypeDefOf.CautionInput, false);
                        }
                    }, MenuOptionPriority.RescueOrCapture, null, victim, 0f, null, null), pawn, victim, "ReservedBy");
                return floatOption;
            }

            public static FloatMenuOption addRescueOption(Pawn pawn, Pawn victim)
            {
                var floatOption = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Rescue".Translate
                    (victim.LabelCap, victim), delegate ()
                    {
                        Building_Bed building_Bed = RestUtility.FindBedFor(victim, pawn, false, false, false);
                        if (building_Bed == null)
                        {
                            building_Bed = RestUtility.FindBedFor(victim, pawn, false, false, true);
                        }
                        if (building_Bed == null)
                        {
                            var mapComp = pawn.Map.GetComponent<MapComponentZLevel>();
                            var oldMap = pawn.Map;
                            var oldPosition1 = pawn.Position;
                            var oldPosition2 = victim.Position;
                            var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                            foreach (var otherMap in ZTracker.GetAllMaps(pawn.Map.Tile))
                            {
                                if (oldMap != otherMap)
                                {
                                    var mapComp2 = otherMap.GetComponent<MapComponentZLevel>();
                                    var stairs = new List<Thing>();
                                    if (mapComp2.Z_LevelIndex >= 0)
                                    {
                                        Map lowerMap = ZTracker.GetLowerLevel(otherMap.Tile, otherMap);
                                        if (lowerMap != null)
                                        {
                                            stairs = lowerMap.listerThings.AllThings.Where(x => x is Building_StairsUp).ToList();
                                        }
                                    }
                                    else
                                    {
                                        Map upperMap = ZTracker.GetUpperLevel(otherMap.Tile, otherMap);
                                        if (upperMap != null)
                                        {
                                            stairs = upperMap.listerThings.AllThings.Where(x => x is Building_StairsDown).ToList();
                                        }
                                    }
                                    if (stairs != null && stairs.Count() > 0)
                                    {
                                        var selectedStairs = GenClosest.ClosestThing_Global(pawn.Position, stairs, 99999f);
                                        var position = selectedStairs.Position;
                                        bool select = false;
                                        if (Find.Selector.SelectedObjects.Contains(pawn)) select = true;
                                        JobManagerPatches.manualDespawn = true;
                                        pawn.DeSpawn();
                                        JobManagerPatches.manualDespawn = false;
                                        GenPlace.TryPlaceThing(pawn, position, otherMap, ThingPlaceMode.Direct);

                                        JobManagerPatches.manualDespawn = true;
                                        victim.DeSpawn();
                                        JobManagerPatches.manualDespawn = false;
                                        GenPlace.TryPlaceThing(victim, position, otherMap, ThingPlaceMode.Direct);
                                        building_Bed = RestUtility.FindBedFor(victim, pawn, false, false, false);
                                        if (building_Bed == null)
                                        {
                                            building_Bed = RestUtility.FindBedFor(victim, pawn, false, false, true);
                                        }

                                        JobManagerPatches.manualDespawn = true;
                                        pawn.DeSpawn();
                                        JobManagerPatches.manualDespawn = false;
                                        GenPlace.TryPlaceThing(pawn, oldPosition1, oldMap, ThingPlaceMode.Direct);
                                        if (select) Find.Selector.Select(pawn);

                                        JobManagerPatches.manualDespawn = true;
                                        victim.DeSpawn();
                                        JobManagerPatches.manualDespawn = false;
                                        GenPlace.TryPlaceThing(victim, oldPosition2, oldMap, ThingPlaceMode.Direct);

                                        if (building_Bed != null)
                                        {
                                            ZLogger.Message("Found bed: " + building_Bed);
                                            Job rescureJob = JobMaker.MakeJob(JobDefOf.Rescue, victim, building_Bed);
                                            rescureJob.count = 1;
                                            ZTracker.BuildJobListFor(pawn, oldMap, oldMap, rescureJob, null);
                                            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Rescuing, KnowledgeAmount.Total);
                                            return;
                                        }
                                    }
                                }
                            }
                            string t5;
                            if (victim.RaceProps.Animal)
                            {
                                t5 = "NoAnimalBed".Translate();
                            }
                            else
                            {
                                t5 = "NoNonPrisonerBed".Translate();
                            }
                            Messages.Message("CannotRescue".Translate() + ": " + t5, victim, MessageTypeDefOf.RejectInput, false);
                            return;
                        }
                        Job job = JobMaker.MakeJob(JobDefOf.Rescue, victim, building_Bed);
                        job.count = 1;
                        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                        PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Rescuing, KnowledgeAmount.Total);
                    }, MenuOptionPriority.RescueOrCapture, null, victim, 0f, null, null), pawn, victim, "ReservedBy");
                return floatOption;
            }
        }

        //[HarmonyPatch(typeof(FloatMenuMakerMap))]
        //[HarmonyPatch("TryMakeFloatMenu")]
        //public class TryMakeFloatMenu_Transpiler
        //{
        //    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        //    {
        //        var foundCurrentMapMethod = false;
        //        int startIndex = -1, endIndex = -1;
        //
        //        var codes = new List<CodeInstruction>(instructions);
        //        for (int i = 0; i < codes.Count; i++)
        //        {
        //
        //            if (codes[i].opcode == OpCodes.Ret)
        //            {
        //                if (foundCurrentMapMethod)
        //                {
        //                    Log.Error("END " + i);
        //            
        //                    endIndex = i; // include current 'ret'
        //                    break;
        //                }
        //                else
        //                {
        //                    Log.Error("START " + (i + 1));
        //            
        //                    startIndex = i + 1; // exclude current 'ret'
        //            
        //                    for (int j = startIndex; j < codes.Count; j++)
        //                    {
        //                        if (codes[j].opcode == OpCodes.Ret)
        //                            break;
        //                        var strOperand = codes[j].ToString();
        //                        if (strOperand != null)
        //                        {
        //                            Log.Message(strOperand);
        //                        }
        //                        if (strOperand != null && strOperand.Contains("CurrentMap"))
        //                        {
        //                            Log.Message("Found");
        //                            foundCurrentMapMethod = true;
        //                            break;
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        if (startIndex > -1 && endIndex > -1)
        //        {
        //            Log.Message("Remove");
        //            // we cannot remove the first code of our range since some jump actually jumps to
        //            // it, so we replace it with a no-op instead of fixing that jump (easier).
        //            codes[startIndex].opcode = OpCodes.Nop;
        //            codes.RemoveRange(startIndex + 1, endIndex - startIndex - 1);
        //        }
        //
        //        return codes.AsEnumerable();
        //    }
        //}
        //
        //[HarmonyPatch(typeof(FloatMenuMakerMap), "ChoicesAtFor")]
        //internal static class Patch_ChoicesAtFor
        //{
        //    private static void Postfix()
        //    {
        //        Log.Message("Run");
        //    }
        //}
    }
}

