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
using ZLevels.Properties;

namespace ZLevels
{
    [StaticConstructorOnStartup]
    public static class FloatMenuPatches
    {
        //[HarmonyPatch(typeof(FloatMenuMakerMap), "ChoicesAtFor")]
        //internal static class Patch_ChoicesAtFor
        //{
        //    private static void Postfix(Vector3 clickPos, Pawn pawn)
        //    {
        //        ChoicesAtFor(clickPos, pawn);
        //    }
        //    public static List<FloatMenuOption> ChoicesAtFor(Vector3 clickPos, Pawn pawn)
        //    {
        //        IntVec3 intVec = IntVec3.FromVector3(clickPos);
        //        List<FloatMenuOption> list = new List<FloatMenuOption>();
        //        if (!intVec.InBounds(pawn.Map) || !FloatMenuMakerMap.CanTakeOrder(pawn))
        //        {
        //            return list;
        //        }
        //        if (pawn.Map != Find.CurrentMap)
        //        {
        //            return list;
        //        }
        //        FloatMenuMakerMap.makingFor = pawn;
        //        try
        //        {
        //            if (!intVec.Fogged(pawn.Map))
        //            {
        //                if (pawn.Drafted)
        //                {
        //                    FloatMenuMakerMap.AddDraftedOrders(clickPos, pawn, list);
        //                }
        //                if (pawn.RaceProps.Humanlike)
        //                {
        //                    FloatMenuMakerMap.AddHumanlikeOrders(clickPos, pawn, list);
        //                }
        //                if (!pawn.Drafted)
        //                {
        //                    ZLogger.Message($"FloatMenuMakerMap.AddUndraftedOrders(clickPos, pawn, list): {list?.Count}");
        //                    FloatMenuMakerMap.AddUndraftedOrders(clickPos, pawn, list);
        //                    ZLogger.Message($"2 FloatMenuMakerMap.AddUndraftedOrders(clickPos, pawn, list): {list?.Count}");
        //                }
        //                {
        //                    foreach (FloatMenuOption item in pawn.GetExtraFloatMenuOptionsFor(intVec))
        //                    {
        //                        list.Add(item);
        //                    }
        //                    return list;
        //                }
        //            }
        //            if (!pawn.Drafted)
        //            {
        //                return list;
        //            }
        //            FloatMenuOption floatMenuOption = FloatMenuMakerMap.GotoLocationOption(intVec, pawn);
        //            if (floatMenuOption == null)
        //            {
        //                return list;
        //            }
        //            if (floatMenuOption.Disabled)
        //            {
        //                return list;
        //            }
        //            list.Add(floatMenuOption);
        //            return list;
        //        }
        //        finally
        //        {
        //            FloatMenuMakerMap.makingFor = null;
        //        }
        //    }
        //}


        //[HarmonyPatch(typeof(FloatMenuOption), "Chosen")]
        //internal static class Patch_FloatMenuOption
        //{
        //    private static void Postfix(FloatMenuOption __instance, bool colonistOrdering, FloatMenu floatMenu)
        //    {
        //        bool goDown = __instance.Label != "GoDown".Translate();
        //        if (goDown && __instance.Label != "GoUP".Translate()) return;
        //        if (Find.Selector.SelectedObjects.Count(x => x is Pawn) > 1)
        //        {
        //            foreach (var pawn in Find.Selector.SelectedObjects.Where(x => x is Pawn))
        //            {
        //                Thing thing;
        //                if (goDown)
        //                {
        //                    thing = GenClosest.ClosestThing_Global_Reachable(UI.MouseMapPosition().ToIntVec3()
        //                        , ((Pawn)pawn).Map, ((Pawn)pawn).Map.listerThings.AllThings
        //                        .Where(x => x is Building_StairsDown), PathEndMode.OnCell,
        //                        TraverseParms.For(TraverseMode.ByPawn, Danger.Deadly, false), 9999f);
        //                    Job job = JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToStairs, thing);
        //                    ((Pawn)pawn).jobs.StartJob(job, JobCondition.InterruptForced);
        //                }
        //                else
        //                {
        //                    thing = GenClosest.ClosestThing_Global_Reachable(UI.MouseMapPosition().ToIntVec3()
        //                        , ((Pawn)pawn).Map, ((Pawn)pawn).Map.listerThings.AllThings
        //                        .Where(x => x is Building_StairsUp), PathEndMode.OnCell,
        //                        TraverseParms.For(TraverseMode.ByPawn, Danger.Deadly, false), 9999f);
        //                    Job job = JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToStairs, thing);
        //                    ((Pawn)pawn).jobs.StartJob(job, JobCondition.InterruptForced);
        //                }
        //            }
        //        
        //            ZLogger.Message("Chosen");
        //        }
        //    }
        //}

        //[HarmonyPatch(typeof(GenUI), "TargetsAt_NewTemp", new Type[] { typeof(Vector3), typeof(TargetingParameters), typeof(bool), typeof(ITargetingSource) })]
        //public static class TargetsAtMouse_NewTemp_Patch3
        //{
        //    public static void Prefix(Vector3 clickPos, TargetingParameters clickParams, bool thingsOnly = false, ITargetingSource source = null)
        //    {
        //        ZLogger.Message(clickPos + " - TargetsAt_NewTemp");
        //    }
        //}

        //
        //[HarmonyPatch(typeof(FloatMenuMakerMap), "AddUndraftedOrders")]
        //internal static class Patch_FloatMenuMakerMap
        //{
        //    private static void Postfix(Vector3 clickPos, Pawn pawn, ref List<FloatMenuOption> opts)
        //    {
        //        if (clickPos.ToIntVec3().GetTerrain(pawn.Map) == ZLevelsDefOf.ZL_OutsideTerrain)
        //        {
        //            ZLogger.Message($"3 FloatMenuMakerMap.AddUndraftedOrders(clickPos, pawn, list): {opts?.Count}");
        //            FloatMenuMakerMap.AddJobGiverWorkOrders_NewTmp(clickPos, pawn, opts, drafted: false);
        //            ZLogger.Message($"4 FloatMenuMakerMap.AddUndraftedOrders(clickPos, pawn, list): {opts?.Count}");
        //
        //        }
        //    }
        //}

        //[HarmonyPatch(typeof(FloatMenuMakerMap), "GotoLocationOption")]
        //public class GotoLocationOption_Patch
        //{
        //    [HarmonyPostfix]
        //    public static void Postfix(ref FloatMenuOption __result, ref IntVec3 clickCell, ref Pawn pawn)
        //    {
        //        var route = ZPathfinder.Instance.FindRoute(pawn.Position, clickCell, pawn.Map, pawn.Map,
        //            out float routeCost);
        //
        //        if (__result.Label == "CannotGoNoPath".Translate())
        //        {
        //            ZLogger.Message($"postfix goto location node count = {route.Count}");
        //            //This means we know that there is a path, it just isn't direct
        //            if (route.Count > 0)
        //            {
        //                __result.Label = "ZGoHere".Translate();
        //                __result.Disabled = false;
        //                SetActionForDelegate(ref __result, clickCell, route, pawn);
        //                //These are the defaults for autotakeable in the original code
        //                __result.autoTakeable = true;
        //                __result.autoTakeablePriority = 10f;
        //            }
        //        }
        //        else
        //        {
        //            //On this branch, we recognize that the direct path might not be the fastest
        //            PawnPath path = pawn.Map.pathFinder.FindPath(pawn.Position, clickCell, ZPathfinder.StairParms);
        //            float pathCost = path.TotalCost;
        //            path.ReleaseToPool();
        //            if (pathCost > routeCost)
        //            {
        //                SetActionForDelegate(ref __result, clickCell, route, pawn);
        //            }
        //        }
        //    }
        //
        //    private static void SetActionForDelegate(ref FloatMenuOption result, IntVec3 clickCell,
        //        List<ZPathfinder.DijkstraGraph.Node> route, Pawn pawn)
        //    {
        //        result.action = delegate ()
        //        {
        //            Job job = JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToLocation, clickCell);
        //            pawn.jobs.StartJob(job, JobCondition.InterruptForced);
        //        };
        //
        //    }
        //
        //}

        //[HarmonyPatch(typeof(FloatMenuOption), "Disabled", MethodType.Getter)]
        //internal static class Patch_FloatDisabled
        //{
        //    private static bool Prefix(FloatMenuOption __instance, ref bool __result)
        //    {
        //        if (__instance.Label != "GoDown".Translate() && __instance.Label != "GoUP".Translate()) return true;
        //        __result = false;
        //        return false;
        //    }
        //}
        //

        //[HarmonyPatch(typeof(FloatMenuOption), "Disabled", MethodType.Setter)]
        //internal static class Patch_Disabled
        //{
        //    private static void Postfix(FloatMenuOption __instance, bool value)
        //    {
        //        ZLogger.Message($"Setting {__instance} to {value}");
        //    }
        //}


        [HarmonyPatch(typeof(GenUI), "ThingsUnderMouse")]
        public static class ThingsUnderMouse_Patch
        {
            public static void Postfix(ref List<Thing> __result, ref Vector3 clickPos, float pawnWideClickRadius, TargetingParameters clickParams)
            {
                //System.Diagnostics.StackTrace t = new System.Diagnostics.StackTrace();
                //ZLogger.Message("ThingsUnderMouse: " + t);
                //foreach (var thin in __result)
                //{
                //    ZLogger.Message("Prev: " + thin);
                //}
                var map = Find.CurrentMap;
                var curMapIndex = ZUtils.ZTracker.GetZIndexFor(map);
                var c = clickPos.ToIntVec3();

                if (c.GetTerrain(map) != ZLevelsDefOf.ZL_OutsideTerrain) return;

                while (true)
                {
                    map = ZUtils.ZTracker.GetLowerLevel(map.Tile, map);
                    if (map != null)
                    {
                        if (map.terrainGrid?.TerrainAt(c) == ZLevelsDefOf.ZL_OutsideTerrain)
                        {
                            continue;
                        }
                        else
                        {
                            clickPos.z += (curMapIndex - ZUtils.ZTracker.GetZIndexFor(map)) * 0.5f;
                            __result.AddRange(ThingsUnderMouse(map, clickPos, pawnWideClickRadius, clickParams));
                            break;
                        }
                    }
                }

                //foreach (var thin in __result)
                //{
                //    ZLogger.Message("Added: " + thin);
                //}
            }

            private static List<Pawn> clickedPawns = new List<Pawn>();
            public static List<Thing> ThingsUnderMouse(Map map, Vector3 clickPos, float pawnWideClickRadius, TargetingParameters clickParams)
            {
                IntVec3 c = IntVec3.FromVector3(clickPos);
                List<Thing> list = new List<Thing>();
                clickedPawns.Clear();
                List<Pawn> allPawnsSpawned = map.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < allPawnsSpawned.Count; i++)
                {
                    Pawn pawn = allPawnsSpawned[i];
                    if ((pawn.DrawPos - clickPos).MagnitudeHorizontal() < 0.4f && clickParams.CanTarget(pawn))
                    {
                        clickedPawns.Add(pawn);
                    }
                }
                clickedPawns.Sort(CompareThingsByDistanceToMousePointer);
                for (int j = 0; j < clickedPawns.Count; j++)
                {
                    list.Add(clickedPawns[j]);
                }
                List<Thing> list2 = new List<Thing>();
                foreach (Thing item in map.thingGrid.ThingsAt(c))
                {
                    if (!list.Contains(item) && clickParams.CanTarget(item))
                    {
                        list2.Add(item);
                    }
                }
                List<Thing> list3 = map.listerThings.ThingsInGroup(ThingRequestGroup.WithCustomRectForSelector);
                for (int k = 0; k < list3.Count; k++)
                {
                    Thing thing = list3[k];
                    if (thing.CustomRectForSelector.HasValue && thing.CustomRectForSelector.Value.Contains(c) && !list.Contains(thing) && clickParams.CanTarget(thing))
                    {
                        list2.Add(thing);
                    }
                }
                list2.Sort(CompareThingsByDrawAltitude);
                list.AddRange(list2);
                clickedPawns.Clear();
                List<Pawn> allPawnsSpawned2 = map.mapPawns.AllPawnsSpawned;
                for (int l = 0; l < allPawnsSpawned2.Count; l++)
                {
                    Pawn pawn2 = allPawnsSpawned2[l];
                    if ((pawn2.DrawPos - clickPos).MagnitudeHorizontal() < pawnWideClickRadius && clickParams.CanTarget(pawn2))
                    {
                        clickedPawns.Add(pawn2);
                    }
                }
                clickedPawns.Sort(CompareThingsByDistanceToMousePointer);
                for (int m = 0; m < clickedPawns.Count; m++)
                {
                    if (!list.Contains(clickedPawns[m]))
                    {
                        list.Add(clickedPawns[m]);
                    }
                }
                list.RemoveAll((Thing t) => !t.Spawned);
                clickedPawns.Clear();
                return list;
            }

            private static int CompareThingsByDistanceToMousePointer(Thing a, Thing b)
            {
                Vector3 b2 = UI.MouseMapPosition();
                float num = (a.DrawPos - b2).MagnitudeHorizontalSquared();
                float num2 = (b.DrawPos - b2).MagnitudeHorizontalSquared();
                if (num < num2)
                {
                    return -1;
                }
                if (num == num2)
                {
                    return 0;
                }
                return 1;
            }

            private static int CompareThingsByDrawAltitude(Thing A, Thing B)
            {
                if (A.def.Altitude < B.def.Altitude)
                {
                    return 1;
                }
                if (A.def.Altitude == B.def.Altitude)
                {
                    return 0;
                }
                return -1;
            }
        }

        [HarmonyPatch(typeof(FloatMenuMap), "StillValid", new Type[] { typeof(FloatMenuOption), typeof(List<FloatMenuOption>), typeof(Pawn) })]
        internal static class Patch_StillValid
        {
            private static bool Prefix(ref bool __result, FloatMenuMap __instance, FloatMenuOption opt, List<FloatMenuOption> curOpts, Pawn forPawn)
            {
                if (opt.revalidateClickTarget?.Map != Find.CurrentMap)
                {
                    __result = StillValid(__instance, opt, curOpts, forPawn);
                    ZLogger.Message("Result: " + __result);
                    return false;
                }
                return true;
            }

            private static bool StillValid(FloatMenuMap __instance, FloatMenuOption opt, List<FloatMenuOption> curOpts, Pawn forPawn)
            {
                if (opt.revalidateClickTarget == null)
                {
                    for (int i = 0; i < curOpts.Count; i++)
                    {
                        if (FloatMenuMap.OptionsMatch(opt, curOpts[i]))
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    if (!opt.revalidateClickTarget.Spawned)
                    {
                        return false;
                    }

                    Vector3 key = opt.revalidateClickTarget.Position.ToVector3Shifted();
                    key.z += (ZUtils.ZTracker.GetZIndexFor(opt.revalidateClickTarget.Map) - ZUtils.ZTracker.GetZIndexFor(Find.CurrentMap)) * 0.5f;

                    if (!FloatMenuMap.cachedChoices.TryGetValue(key, out List<FloatMenuOption> value))
                    {
                        List<FloatMenuOption> list = FloatMenuMakerMap.ChoicesAtFor(key, forPawn);
                        FloatMenuMap.cachedChoices.Add(key, list);
                        value = list;
                    }
                    for (int j = 0; j < value.Count; j++)
                    {
                        if (FloatMenuMap.OptionsMatch(opt, value[j]))
                        {
                            return !value[j].Disabled;
                        }
                    }
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
        public class AddHumanlikeOrders_Patch
        {
            public static void Postfix(Vector3 clickPos, Pawn pawn, ref List<FloatMenuOption> opts)
            {
                Pawn pawn2 = GridsUtility.GetThingList(IntVec3.FromVector3(clickPos), pawn.Map)
                    .FirstOrDefault((Thing x) => x is Pawn) as Pawn;
                var ZTracker = ZUtils.ZTracker;

                if (pawn2 != null && ZTracker.ZLevelsTracker[pawn.Map.Tile].ZLevels.Count > 1)
                {
                    TaggedString toCheck = "Rescue".Translate(pawn2.LabelCap, pawn2);
                    FloatMenuOption floatMenuOption = opts.FirstOrDefault((FloatMenuOption x) => x.Label.Contains
                        (toCheck));
                    if (floatMenuOption != null)
                    {
                        opts.Remove(floatMenuOption);
                        opts.Add(AddHumanlikeOrders_Patch.AddRescueOption(pawn, pawn2));
                    }

                    TaggedString toCheck2 = "Capture".Translate(pawn2.LabelCap, pawn2);
                    FloatMenuOption floatMenuOption2 = opts.FirstOrDefault((FloatMenuOption x) => x.Label.Contains
                        (toCheck2));
                    if (floatMenuOption2 != null)
                    {
                        opts.Remove(floatMenuOption2);
                        opts.Add(AddHumanlikeOrders_Patch.AddCaptureOption(pawn, pawn2));
                    }

                    TaggedString toCheck3 = "TryToArrest".Translate(pawn2.LabelCap, pawn2,
                        pawn2.GetAcceptArrestChance(pawn).ToStringPercent());
                    FloatMenuOption floatMenuOption3 = opts.FirstOrDefault((FloatMenuOption x) => x.Label.Contains
                        (toCheck3));
                    if (floatMenuOption3 != null && (pawn.Drafted || pawn2.IsWildMan()))
                    {
                        opts.Remove(floatMenuOption3);
                        opts.Add(AddHumanlikeOrders_Patch.AddArrestOption(pawn, pawn2));
                    }
                }

                var map = Find.CurrentMap;
                var curMapIndex = ZUtils.ZTracker.GetZIndexFor(map);
                var c = clickPos.ToIntVec3();

                if (c.GetTerrain(map) != ZLevelsDefOf.ZL_OutsideTerrain) return;

                while (true)
                {
                    map = ZUtils.ZTracker.GetLowerLevel(map.Tile, map);
                    if (map != null)
                    {
                        if (map.terrainGrid?.TerrainAt(c) == ZLevelsDefOf.ZL_OutsideTerrain)
                        {
                            continue;
                        }
                        else
                        {
                            clickPos.z += (curMapIndex - ZUtils.ZTracker.GetZIndexFor(map)) * 0.5f;
                            c = IntVec3.FromVector3(clickPos);
                            foreach (Thing thing2 in c.GetThingList(map))
                            {
                                Thing t = thing2;
                                if (t.def.ingestible != null && pawn.RaceProps.CanEverEat(t) && t.IngestibleNow)
                                {
                                    string text = (!t.def.ingestible.ingestCommandString.NullOrEmpty()) ? string.Format(t.def.ingestible.ingestCommandString, t.LabelShort) : ((string)"ConsumeThing".Translate(t.LabelShort, t));
                                    if (!t.IsSociallyProper(pawn))
                                    {
                                        text = text + ": " + "ReservedForPrisoners".Translate().CapitalizeFirst();
                                    }
                                    FloatMenuOption floatMenuOption;
                                    if (t.def.IsNonMedicalDrug && pawn.IsTeetotaler())
                                    {
                                        floatMenuOption = new FloatMenuOption(text + ": " + TraitDefOf.DrugDesire.DataAtDegree(-1).GetLabelCapFor(pawn), null);
                                    }
                                    else if (FoodUtility.InappropriateForTitle(t.def, pawn, allowIfStarving: true))
                                    {
                                        floatMenuOption = new FloatMenuOption(text + ": " + "FoodBelowTitleRequirements".Translate(pawn.royalty.MostSeniorTitle.def.GetLabelFor(pawn)), null);
                                    }
                                    else if (!pawn.CanReach(t, PathEndMode.OnCell, Danger.Deadly))
                                    {
                                        floatMenuOption = new FloatMenuOption(text + ": " + "NoPath".Translate().CapitalizeFirst(), null);
                                    }
                                    else
                                    {

                                        MenuOptionPriority priority = (t is Corpse) ? MenuOptionPriority.Low : MenuOptionPriority.Default;
                                        int maxAmountToPickup = FoodUtility.GetMaxAmountToPickup(t, pawn, FoodUtility.WillIngestStackCountOf(pawn, t.def, t.GetStatValue(StatDefOf.Nutrition)));
                                        floatMenuOption = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text, delegate
                                        {
                                            int maxAmountToPickup2 = FoodUtility.GetMaxAmountToPickup(t, pawn, FoodUtility.WillIngestStackCountOf(pawn, t.def, t.GetStatValue(StatDefOf.Nutrition)));
                                            if (maxAmountToPickup2 != 0)
                                            {
                                                t.SetForbidden(value: false);
                                                Job job18 = JobMaker.MakeJob(JobDefOf.Ingest, t);
                                                job18.count = maxAmountToPickup2;
                                                ZTracker.ResetJobTrackerFor(pawn);
                                                ZTracker.BuildJobListFor(pawn, pawn.Map, job18);
                                                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                                            }
                                        }, priority), pawn, t);
                                        if (maxAmountToPickup == 0)
                                        {
                                            floatMenuOption.action = null;
                                        }
                                    }
                                    opts.Add(floatMenuOption);
                                }
                            }
                            break;
                        }
                    }
                }
            }
            public static FloatMenuOption AddArrestOption(Pawn pawn, Pawn victim)
            {
                if (!pawn.CanReach(victim, PathEndMode.OnCell, Danger.Deadly))
                {
                    return new FloatMenuOption(
                        "CannotArrest".Translate() + ": " + "NoPath".Translate().CapitalizeFirst(), null);
                }
                else
                {
                    Pawn pTarg2 = victim;
                    Action action = delegate
                    {
                        var ZTracker = ZUtils.ZTracker;
                        var oldMap = pawn.Map;
                        var oldPosition1 = pawn.Position;
                        var oldPosition2 = victim.Position;
                        bool select = false;
                        if (Find.Selector.SelectedObjects.Contains(pawn)) select = true;

                        Building building_Bed3 = null;
                        foreach (var otherMap in ZUtils.GetAllMapsInClosestOrderForTwoThings(pawn, oldMap,
                            oldPosition1, victim, oldMap, oldPosition2))
                        {
                            building_Bed3 = RestUtility.FindBedFor(pTarg2, pawn, sleeperWillBePrisoner: true,
                                checkSocialProperness: false);
                            if (building_Bed3 == null)
                            {
                                building_Bed3 = RestUtility.FindBedFor(pTarg2, pawn, sleeperWillBePrisoner: true,
                                    checkSocialProperness: false, ignoreOtherReservations: true);
                            }

                            if (building_Bed3 != null) break;
                        }

                        ZUtils.TeleportThing(pawn, oldMap, oldPosition1);
                        ZUtils.TeleportThing(victim, oldMap, oldPosition2);

                        if (select) Find.Selector.Select(pawn);

                        if (building_Bed3 == null)
                        {
                            Messages.Message("CannotArrest".Translate() + ": " + "NoPrisonerBed".Translate(),
                                pTarg2, MessageTypeDefOf.RejectInput, historical: false);
                        }
                        else
                        {
                            Job job = JobMaker.MakeJob(JobDefOf.Arrest, pTarg2, building_Bed3);
                            job.count = 1;
                            ZTracker.ResetJobTrackerFor(pawn);
                            ZTracker.BuildJobListFor(pawn, pawn.Map, job);
                            ZLogger.Message(pawn + " taking first job 3");
                            pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, false);

                            if (pTarg2.Faction != null &&
                                ((pTarg2.Faction != Faction.OfPlayer && !pTarg2.Faction.def.hidden) ||
                                 pTarg2.IsQuestLodger()))
                            {
                                TutorUtility.DoModalDialogIfNotKnown(ConceptDefOf.ArrestingCreatesEnemies);
                            }
                        }
                    };
                    return FloatMenuUtility.DecoratePrioritizedTask(
                        new FloatMenuOption(
                            "TryToArrest".Translate(victim.LabelCap, victim,
                                pTarg2.GetAcceptArrestChance(pawn).ToStringPercent()), action,
                            MenuOptionPriority.High, null, victim), pawn, pTarg2);
                }
            }

            public static FloatMenuOption AddCaptureOption(Pawn pawn, Pawn victim)
            {
                var floatOption = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Capture".Translate
                    (victim.LabelCap, victim), delegate ()
                    {
                        var ZTracker = ZUtils.ZTracker;
                        var oldMap = pawn.Map;
                        var oldPosition1 = pawn.Position;
                        var oldPosition2 = victim.Position;
                        bool select = Find.Selector.SelectedObjects.Contains(pawn);

                        Building building_Bed = null;
                        foreach (var otherMap in ZUtils.GetAllMapsInClosestOrderForTwoThings(pawn, oldMap, oldPosition1,
                            victim, oldMap, oldPosition2))
                        {
                            building_Bed = RestUtility.FindBedFor(victim, pawn, true, false, false);
                            if (building_Bed == null)
                            {
                                building_Bed = RestUtility.FindBedFor(victim, pawn, true, false, true);
                            }

                            if (building_Bed != null) break;
                        }

                        ZUtils.TeleportThing(pawn, oldMap, oldPosition1);
                        ZUtils.TeleportThing(victim, oldMap, oldPosition2);

                        if (select) Find.Selector.Select(pawn);

                        if (building_Bed == null)
                        {
                            Messages.Message("CannotCapture".Translate() + ": " + "NoPrisonerBed".Translate(), victim,
                                MessageTypeDefOf.RejectInput, false);
                            return;
                        }

                        Job job = JobMaker.MakeJob(JobDefOf.Capture, victim, building_Bed);
                        job.count = 1;
                        ZTracker.ResetJobTrackerFor(pawn);
                        ZTracker.BuildJobListFor(pawn, pawn.Map, job);
                        ZLogger.Message(pawn + " taking first job 3");
                        pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, false);
                        PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Capturing, KnowledgeAmount.Total);
                        if (victim.Faction != null && victim.Faction != Faction.OfPlayer &&
                            !victim.Faction.def.hidden && !victim.Faction.HostileTo(Faction.OfPlayer) &&
                            !victim.IsPrisonerOfColony)
                        {
                            Messages.Message("MessageCapturingWillAngerFaction".Translate(victim.Named("PAWN"))
                                .AdjustedFor(victim, "PAWN", true), victim, MessageTypeDefOf.CautionInput, false);
                        }
                    }, MenuOptionPriority.RescueOrCapture, null, victim, 0f, null, null), pawn, victim, "ReservedBy");
                return floatOption;
            }

            public static FloatMenuOption AddRescueOption(Pawn pawn, Pawn victim)
            {
                var floatOption = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Rescue".Translate
                    (victim.LabelCap, victim), delegate ()
                    {
                        var ZTracker = ZUtils.ZTracker;
                        var oldMap = pawn.Map;
                        var oldPosition1 = pawn.Position;
                        var oldPosition2 = victim.Position;
                        bool select = false;
                        if (Find.Selector.SelectedObjects.Contains(pawn)) select = true;
                        Building building_Bed = null;

                        foreach (var otherMap in ZUtils.GetAllMapsInClosestOrderForTwoThings(pawn, oldMap, oldPosition1,
                            victim, oldMap, oldPosition2))
                        {
                            ZLogger.Message("Searching rest job for " + pawn + " in " + ZTracker.GetMapInfo(otherMap)
                                            + " for " + ZTracker.GetMapInfo(oldMap));
                            building_Bed = RestUtility.FindBedFor(victim, pawn, sleeperWillBePrisoner: false,
                                checkSocialProperness: false);
                            if (building_Bed == null)
                            {
                                building_Bed = RestUtility.FindBedFor(victim, pawn, sleeperWillBePrisoner: false,
                                    checkSocialProperness: false, ignoreOtherReservations: true);
                            }

                            if (building_Bed != null) break;
                        }

                        if (select) Find.Selector.Select(pawn);
                        ZUtils.TeleportThing(pawn, oldMap, oldPosition1);
                        ZUtils.TeleportThing(victim, oldMap, oldPosition2);

                        if (building_Bed == null)
                        {
                            string t3 = (!victim.RaceProps.Animal)
                                ? ((string)"NoNonPrisonerBed".Translate())
                                : ((string)"NoAnimalBed".Translate());
                            Messages.Message("CannotRescue".Translate() + ": " + t3, victim,
                                MessageTypeDefOf.RejectInput, historical: false);
                        }
                        else
                        {
                            Job job = JobMaker.MakeJob(JobDefOf.Rescue, victim, building_Bed);
                            job.count = 1;
                            ZTracker.ResetJobTrackerFor(pawn);
                            ZTracker.BuildJobListFor(pawn, pawn.Map, job);
                            ZLogger.Message(pawn + " taking first job 2");
                            pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, false);
                            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Rescuing, KnowledgeAmount.Total);
                        }
                    }, MenuOptionPriority.RescueOrCapture, null, victim, 0f, null, null), pawn, victim, "ReservedBy");
                return floatOption;
            }
        }


        [HarmonyPatch(typeof(FloatMenuMakerMap))]
        [HarmonyPatch("AddJobGiverWorkOrders_NewTmp")]
        internal static class FloatMenuMakerMap_AddJobGiverWorkOrders_Patch
        {
            private static bool Prefix(Vector3 clickPos, Pawn pawn, ref List<FloatMenuOption> opts, bool drafted, ref FloatMenuOption[] ___equivalenceGroupTempStorage)
            {
                var inactiveOptions = new List<FloatMenuOption>();
                foreach (var option in opts)
                {
                    if (option.action is null)
                    {
                        inactiveOptions.Add(option);
                    }
                }

                var duplicateOptions = new List<FloatMenuOption>();
                if (pawn.thinker.TryGetMainTreeThinkNode<JobGiver_Work>() != null)
                {
                    IntVec3 clickCell = IntVec3.FromVector3(clickPos);
                    TargetingParameters targetingParameters = new TargetingParameters();
                    targetingParameters.canTargetPawns = true;
                    targetingParameters.canTargetBuildings = true;
                    targetingParameters.canTargetItems = true;
                    targetingParameters.mapObjectTargetsMustBeAutoAttackable = false;
                    var ZTracker = ZUtils.ZTracker;
                    ZTracker.ResetJobTrackerFor(pawn);
                    if (!ZTracker.jobTracker.TryGetValue(pawn, out var jobTracker))
                    {
                        jobTracker = new JobTracker();
                    }
                    foreach (Thing item in GenUI.ThingsUnderMouse(clickPos, 1f, targetingParameters))
                    {
                        bool flag = false;
                        foreach (WorkTypeDef item2 in DefDatabase<WorkTypeDef>.AllDefsListForReading)
                        {
                            for (int i = 0; i < item2.workGiversByPriority.Count; i++)
                            {
                                WorkGiverDef workGiver2 = item2.workGiversByPriority[i];
                                if (!drafted || workGiver2.canBeDoneWhileDrafted)
                                {
                                    WorkGiver_Scanner workGiver_Scanner = workGiver2.Worker as WorkGiver_Scanner;
                                    if (workGiver_Scanner != null && workGiver_Scanner.def.directOrderable)
                                    {
                                        JobFailReason.Clear();
                                        if ((workGiver_Scanner.PotentialWorkThingRequest.Accepts(item) || (workGiver_Scanner.PotentialWorkThingsGlobal(pawn) != null
                                            && workGiver_Scanner.PotentialWorkThingsGlobal(pawn).Contains(item))) && !workGiver_Scanner.ShouldSkip(pawn, forced: true))
                                        {
                                            string text = null;
                                            Action action = null;
                                            PawnCapacityDef pawnCapacityDef = workGiver_Scanner.MissingRequiredCapacity(pawn);
                                            if (pawnCapacityDef != null)
                                            {
                                                text = "CannotMissingHealthActivities".Translate(pawnCapacityDef.label);
                                            }
                                            else
                                            {
                                                Job job = null;
                                                Map dest = null;
                                                Map oldPawnMap = pawn.Map;
                                                IntVec3 oldPawnPosition = pawn.Position;

                                                foreach (var otherMap in ZTracker.GetAllMapsInClosestOrder(oldPawnMap))
                                                {
                                                    if (oldPawnMap != otherMap)
                                                    {
                                                        pawn.positionInt = ZUtils.GetCellToTeleportFrom(pawn.Map, pawn.Position, otherMap);
                                                    }
                                                    else if (pawn.Position != oldPawnPosition)
                                                    {
                                                        pawn.positionInt = oldPawnPosition;
                                                    }
                                                    pawn.mapIndexOrState = (sbyte)Find.Maps.IndexOf(otherMap);

                                                    if (workGiver_Scanner is WorkGiver_Refuel scanner1)
                                                    {
                                                        job = JobPatches.TryIssueJobPackagePatch.RefuelJobOnThing(scanner1, pawn, item, true);
                                                    }
                                                    else if (workGiver_Scanner.def.defName == "HaulGeneral" || workGiver_Scanner.def.defName == "HaulCorpses")
                                                    {
                                                        job = JobPatches.TryIssueJobPackagePatch.HaulJobOnThing(pawn, item, jobTracker, true);
                                                    }
                                                    else if (workGiver_Scanner is WorkGiver_DoBill scanner2)
                                                    {
                                                        job = JobPatches.TryIssueJobPackagePatch.DoBillJobOnThing(scanner2, pawn, item, jobTracker, true);
                                                    }
                                                    else if (workGiver_Scanner is
                                                        WorkGiver_ConstructDeliverResourcesToBlueprints scanner3)
                                                    {
                                                        job = JobPatches.TryIssueJobPackagePatch.ConstructDeliverJobOnThing(scanner3, pawn, item, true);
                                                    }
                                                    else if (workGiver_Scanner is WorkGiver_ConstructDeliverResourcesToFrames scanner4)
                                                    {
                                                        job = JobPatches.TryIssueJobPackagePatch.ConstructDeliverJobOnThing(scanner4, pawn, item, true);
                                                    }
                                                    else if (workGiver_Scanner is WorkGiver_Tend)
                                                    {
                                                        job = JobPatches.TryIssueJobPackagePatch.TendJobOnThing(pawn, item, true);
                                                    }
                                                    else
                                                    {
                                                        job = workGiver_Scanner.HasJobOnThing(pawn, item, forced: true) ? workGiver_Scanner.JobOnThing(pawn, item, forced: true) : null;
                                                    }

                                                    if (job != null)
                                                    {
                                                        if (dest == null)
                                                        {
                                                            dest = otherMap;
                                                        }
                                                        ZLogger.Message($"Found job {job} from " + workGiver_Scanner + " in " + otherMap + " dest: " + dest);
                                                        break;
                                                    }
                                                    else
                                                    {
                                                        dest = null;
                                                        //ZLogger.Message("No job was yielded from " + workGiver_Scanner + " in " + otherMap);
                                                    }
                                                }
                                                ZUtils.TeleportThing(pawn, oldPawnMap, oldPawnPosition);
                                                if (job == null)
                                                {
                                                    if (JobFailReason.HaveReason)
                                                    {
                                                        text = (JobFailReason.CustomJobString.NullOrEmpty() ? ((string)"CannotGenericWork".Translate(workGiver_Scanner.def.verb, item.LabelShort, item)) : ((string)"CannotGenericWorkCustom".Translate(JobFailReason.CustomJobString)));
                                                        text = text + ": " + JobFailReason.Reason.CapitalizeFirst();
                                                    }
                                                    else
                                                    {
                                                        if (!item.IsForbidden(pawn))
                                                        {
                                                            continue;
                                                        }
                                                        text = (item.Position.InAllowedArea(pawn) ? ((string)"CannotPrioritizeForbidden".Translate(item.Label, item)) : ((string)("CannotPrioritizeForbiddenOutsideAllowedArea".Translate() + ": " + pawn.playerSettings.EffectiveAreaRestriction.Label)));
                                                    }
                                                }
                                                else
                                                {
                                                    WorkTypeDef workType = workGiver_Scanner.def.workType;
                                                    if (pawn.WorkTagIsDisabled(workGiver_Scanner.def.workTags))
                                                    {
                                                        text = "CannotPrioritizeWorkGiverDisabled".Translate(workGiver_Scanner.def.label);
                                                    }
                                                    else if (pawn.jobs.curJob != null && pawn.jobs.curJob.JobIsSameAs(job))
                                                    {
                                                        text = "CannotGenericAlreadyAm".Translate(workGiver_Scanner.PostProcessedGerund(job), item.LabelShort, item);
                                                    }
                                                    else if (pawn.workSettings.GetPriority(workType) == 0)
                                                    {
                                                        text = (pawn.WorkTypeIsDisabled(workType) ? ((string)"CannotPrioritizeWorkTypeDisabled".Translate(workType.gerundLabel)) : ((!"CannotPrioritizeNotAssignedToWorkType".CanTranslate()) ? ((string)"CannotPrioritizeWorkTypeDisabled".Translate(workType.pawnLabel)) : ((string)"CannotPrioritizeNotAssignedToWorkType".Translate(workType.gerundLabel))));
                                                    }
                                                    else if (job.def == JobDefOf.Research && item is Building_ResearchBench)
                                                    {
                                                        text = "CannotPrioritizeResearch".Translate();
                                                    }
                                                    else if (item.IsForbidden(pawn))
                                                    {
                                                        text = (item.Position.InAllowedArea(pawn) ? ((string)"CannotPrioritizeForbidden".Translate(item.Label, item)) : ((string)("CannotPrioritizeForbiddenOutsideAllowedArea".Translate() + ": " + pawn.playerSettings.EffectiveAreaRestriction.Label)));
                                                    }
                                                    else if (!pawn.CanReach(item, workGiver_Scanner.PathEndMode, Danger.Deadly))
                                                    {
                                                        text = (item.Label + ": " + "NoPath".Translate().CapitalizeFirst()).CapitalizeFirst();
                                                    }
                                                    else
                                                    {
                                                        text = "PrioritizeGeneric".Translate(workGiver_Scanner.PostProcessedGerund(job), item.Label);
                                                        Job localJob2 = job;
                                                        WorkGiver_Scanner localScanner2 = workGiver_Scanner;
                                                        job.workGiverDef = workGiver_Scanner.def;
                                                        action = delegate
                                                        {
                                                            if (!ZTracker.jobTracker.ContainsKey(pawn))
                                                            {
                                                                ZTracker.jobTracker[pawn] = new JobTracker();
                                                            }
                                                            if (dest != null)
                                                            {
                                                                ZLogger.Message("1 Dest: " + dest);

                                                                ZTracker.BuildJobListFor(pawn, dest, job);
                                                            }
                                                            else
                                                            {
                                                                ZLogger.Message("2 Dest (oldPawnMap): " + oldPawnMap);

                                                                ZTracker.BuildJobListFor(pawn, oldPawnMap, job);
                                                            }
                                                            pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                                                            if (workGiver2.forceMote != null)
                                                            {
                                                                MoteMaker.MakeStaticMote(clickCell, pawn.Map,
                                                                    workGiver2.forceMote);
                                                            }
                                                        };
                                                    }
                                                }
                                            }
                                            if (DebugViewSettings.showFloatMenuWorkGivers)
                                            {
                                                text += $" (from {workGiver2.defName})";
                                            }
                                            FloatMenuOption menuOption = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text, action), pawn, item);
                                            if (drafted && workGiver2.autoTakeablePriorityDrafted != -1)
                                            {
                                                menuOption.autoTakeable = true;
                                                menuOption.autoTakeablePriority = workGiver2.autoTakeablePriorityDrafted;
                                            }

                                            if (!opts.Any(op => op.Label == menuOption.Label))
                                            {
                                                if (workGiver2.equivalenceGroup != null)
                                                {
                                                    if (___equivalenceGroupTempStorage[workGiver2.equivalenceGroup.index] == null
                                                        || (___equivalenceGroupTempStorage[workGiver2.equivalenceGroup.index].Disabled && !menuOption.Disabled))
                                                    {
                                                        ZLogger.Message("2 Adding menuOption: " + menuOption);
                                                        ___equivalenceGroupTempStorage[workGiver2.equivalenceGroup.index] = menuOption;
                                                        flag = true;
                                                    }
                                                }
                                                else
                                                {
                                                    ZLogger.Message("Adding menuOption: " + menuOption);
                                                    opts.Add(menuOption);
                                                }
                                            }
                                            else
                                            {
                                                ZLogger.Message("Adding duplicate: " + menuOption);
                                                duplicateOptions.Add(menuOption);
                                            }
                                        }
                                        //else
                                        //{
                                        //    ZLogger.Message($"Failed to get result from {workGiver_Scanner} for {item}");
                                        //}
                                    }
                                }
                            }
                        }

                        if (flag)
                        {
                            for (int j = 0; j < ___equivalenceGroupTempStorage.Length; j++)
                            {
                                if (___equivalenceGroupTempStorage[j] != null)
                                {
                                    opts.Add(___equivalenceGroupTempStorage[j]);
                                    ___equivalenceGroupTempStorage[j] = null;
                                }
                            }
                        }
                    }
                }


                //foreach (var inactiveOption in inactiveOptions)
                //{
                //    if (!duplicateOptions.Any(x => inactiveOption.Label == x.Label))
                //    {
                //        for (int num = opts.Count - 1; num >= 0; num--)
                //        {
                //            var option = opts[num];
                //            if (inactiveOption.Label != option.Label && option.revalidateClickTarget != null && inactiveOption.Label.Contains(option.revalidateClickTarget.Label))
                //            {
                //                ZLogger.Message("Removing " + inactiveOption);
                //                opts.Remove(inactiveOption);
                //            }
                //        }
                //    }
                //}
                return false;
            }
        }
    }
}