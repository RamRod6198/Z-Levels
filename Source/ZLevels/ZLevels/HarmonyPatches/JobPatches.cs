using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HarmonyLib;
using Multiplayer.API;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using UnityEngine;
using UnityEngine.Assertions.Must;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ZLevels
{
    [StaticConstructorOnStartup]
    public static class JobPatches
    {
        [HarmonyPatch(typeof(ThinkNode_ConditionalShouldFollowMaster), "ShouldFollowMaster")]
        internal static class ShouldFollowMasterPatch
        {
            private static bool Prefix(Pawn pawn, ref bool __result)
            {
                if (!pawn.Spawned || pawn.playerSettings == null)
                {
                    __result = false;
                    return false;
                }
                Pawn respectedMaster = pawn.playerSettings.RespectedMaster;
                if (respectedMaster == null)
                {
                    __result = false;
                    return false;
                }
                if (respectedMaster.Spawned)
                {
                    if (pawn.playerSettings.followDrafted
                        && respectedMaster.Drafted)
                    //&& pawn.CanReach(respectedMaster, PathEndMode.OnCell, Danger.Deadly))
                    {
                        __result = true;
                        return false;
                    }
                    if (pawn.playerSettings.followFieldwork
                        && respectedMaster.mindState.lastJobTag == JobTag.Fieldwork)
                    //&& pawn.CanReach(respectedMaster, PathEndMode.OnCell, Danger.Deadly))
                    {
                        __result = true;
                        return false;
                    }
                }
                else
                {
                    Pawn carriedBy = respectedMaster.CarriedBy;
                    if (carriedBy != null && carriedBy.HostileTo(respectedMaster) && pawn.CanReach(carriedBy, PathEndMode.OnCell, Danger.Deadly))
                    {
                        __result = true;
                        return false;

                    }
                }
                __result = false;
                return false;
            }
        }

        [HarmonyPatch(typeof(JobGiver_AIFollowPawn), "TryGiveJob")]
        public class JobGiver_AIFollowPawnPatch
        {
            [HarmonyPrefix]
            private static bool JobGiver_AIFollowPawnPrefix(JobGiver_AIFollowPawn __instance, ref Job __result, Pawn pawn)
            {
                try
                {
                    var ZTracker = ZUtils.ZTracker;
                    if (ZTracker.jobTracker == null)
                    {
                        ZTracker.jobTracker = new Dictionary<Pawn, JobTracker>();
                    }
                    if (!ZTracker.jobTracker.TryGetValue(pawn, out JobTracker jobTracker))
                    {
                        jobTracker = new JobTracker();
                        ZTracker.jobTracker[pawn] = jobTracker;
                    }
                    if (jobTracker?.activeJobs?.Any() ?? false)
                    {
                        if (!pawn.jobs.jobQueue.Contains(jobTracker.activeJobs[0]))
                        {
                            if (jobTracker.activeJobs[0].def.defName != "UnloadYourHauledInventory"
                                && jobTracker.activeJobs[0].TryMakePreToilReservations(pawn, false))
                            {
                                __result = jobTracker.activeJobs[0];
                                jobTracker.activeJobs.RemoveAt(0);
                                return false;
                            }
                        }
                        else if (pawn.jobs.curJob == null)
                        {
                            __result = jobTracker.activeJobs[0];
                            jobTracker.activeJobs.RemoveAt(0);
                            return false;
                        }
                        else if (pawn.jobs.curJob != jobTracker.activeJobs[0])
                        {
                            __result = jobTracker.activeJobs[0];
                            jobTracker.activeJobs.RemoveAt(0);
                            return false;
                        }
                    }
                    Job result;
                    var oldMap = pawn.Map;
                    var oldPosition = pawn.Position;
                    bool select = false;
                    if (Find.Selector.SelectedObjects.Contains(pawn)) select = true;
                    ZTracker.ResetJobTrackerFor(pawn);

                    foreach (var otherMap in ZUtils.GetAllMapsInClosestOrder(pawn, oldMap, oldPosition))
                    {
                        ZLogger.Message("Searching follow job for " + pawn + " in " + ZTracker.GetMapInfo(otherMap)
                            + " for " + ZTracker.GetMapInfo(oldMap));
                        result = JobGiver_AIFollowPawnPatch.TryGiveJob(pawn, __instance);
                        if (result != null)
                        {
                            ZLogger.Message(pawn + " got follow job " + result + " - map: "
                                + ZTracker.GetMapInfo(pawn.Map) + " - " + pawn.Position);
                            ZUtils.TeleportThing(pawn, oldMap, oldPosition);
                            ZTracker.BuildJobListFor(pawn, otherMap, result);
                            __result = jobTracker.activeJobs[0];
                            jobTracker.activeJobs.RemoveAt(0);
                            break;
                        }
                    }
                    ZUtils.TeleportThing(pawn, oldMap, oldPosition);
                    if (select) Find.Selector.Select(pawn);
                    return false;
                }
                catch (Exception ex)
                {
                    Log.Error("Some kind of error occurred in Z-Levels JobManager: " + ex);
                }
                return true;
            }

            public static Job TryGiveJob(Pawn pawn, JobGiver_AIFollowPawn __instance)
            {
                Pawn followee = __instance.GetFollowee(pawn);
                if (followee == null)
                {
                    return null;
                }
                if (!followee.Spawned || !pawn.CanReach(followee, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn))
                {
                    return null;
                }
                ZLogger.Message(pawn + " starting following " + followee);
                float radius = __instance.GetRadius(pawn);
                if (!JobDriver_FollowClose.FarEnoughAndPossibleToStartJob(pawn, followee, radius))
                {
                    return null;
                }
                Job job = JobMaker.MakeJob(JobDefOf.FollowClose, followee);
                job.expiryInterval = __instance.FollowJobExpireInterval;
                job.checkOverrideOnExpire = true;
                job.followRadius = radius;
                return job;
            }
        }

        [HarmonyPatch(typeof(JobDriver_Ingest))]
        [HarmonyPatch("PrepareToIngestToils_ToolUser")]
        public static class Patch_PrepareToIngestToils_ToolUser_Postfix
        {
            private static void Postfix(ref IEnumerable<Toil> __result, JobDriver_Ingest __instance, Toil chewToil)
            {
                var list = __result.ToList<Toil>();
                Thing IngestibleSource = __instance.job.GetTarget(TargetIndex.A).Thing;
                Pawn actor = __instance.pawn;
                Thing thing = null;
                IntVec3 intVec = IntVec3.Invalid;
                var ZTracker = ZUtils.ZTracker;
                var oldMap = actor.Map;
                var oldPosition = actor.Position;
                bool select = false;
                if (Find.Selector.SelectedObjects.Contains(actor)) select = true;
                Predicate<Thing> baseChairValidator = delegate (Thing t)
                {
                    if (t.def.building == null || !t.def.building.isSittable)
                    {
                        return false;
                    }
                    if (t.IsForbidden(actor))
                    {
                        return false;
                    }
                    if (!actor.CanReserve(t))
                    {
                        return false;
                    }
                    if (!t.IsSociallyProper(actor))
                    {
                        return false;
                    }
                    if (t.IsBurning())
                    {
                        return false;
                    }
                    if (t.HostileTo(actor))
                    {
                        return false;
                    }
                    bool flag = false;
                    for (int i = 0; i < 4; i++)
                    {
                        Building edifice = (t.Position + GenAdj.CardinalDirections[i]).GetEdifice(t.Map);
                        if (edifice != null && edifice.def.surfaceType == SurfaceType.Eat)
                        {
                            flag = true;
                            break;
                        }
                    }
                    return flag ? true : false;
                };
                if (oldMap != null)
                {
                    foreach (var otherMap in ZUtils.GetAllMapsInClosestOrder(actor, oldMap, oldPosition))
                    {
                        if (IngestibleSource.def.ingestible.chairSearchRadius > 0f)
                        {
                            thing = GenClosest.ClosestThingReachable(actor.Position, actor.Map,
                                ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial), PathEndMode.OnCell,
                                TraverseParms.For(actor), IngestibleSource.def.ingestible.chairSearchRadius,
                                (Thing t) => baseChairValidator(t) && t.Position.GetDangerFor(actor, t.Map) == Danger.None);
                        }
                        if (thing == null)
                        {
                            try
                            {
                                intVec = RCellFinder.SpotToChewStandingNear(actor, actor.CurJob.GetTarget(TargetIndex.A).Thing);
                            }
                            catch
                            {
                                intVec = actor.Position;
                            }
                            Danger chewSpotDanger = intVec.GetDangerFor(actor, actor.Map);
                            if (chewSpotDanger != Danger.None)
                            {
                                thing = GenClosest.ClosestThingReachable(actor.Position, actor.Map,
                                    ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial), PathEndMode.OnCell,
                                    TraverseParms.For(actor), IngestibleSource.def.ingestible.chairSearchRadius, (Thing t) =>
                                    baseChairValidator(t) && (int)t.Position.GetDangerFor(actor, t.Map) <= (int)chewSpotDanger);
                            }
                        }

                        if (thing != null)
                        {
                            intVec = thing.Position;
                            actor.Reserve(thing, actor.CurJob);
                            ZLogger.Message(actor + " - Found: " + thing);
                            break;
                        }
                    }

                    ZUtils.TeleportThing(actor, oldMap, oldPosition);
                    if (select) Find.Selector.Select(actor);

                    if (thing != null && thing.Map != null && thing.Map != actor.Map)
                    {
                        if (!IngestibleSource.def.IsDrug)
                        {
                            list.InsertRange(list.Count - 2, Toils_ZLevels.GoToMap(__instance.GetActor()
                                , thing.Map, __instance));
                        }
                        else
                        {
                            list.InsertRange(list.Count - 1, Toils_ZLevels.GoToMap(__instance.GetActor()
                                , thing.Map, __instance));
                        }
                    }
                }


                __result = list;
            }
        }

        [HarmonyPatch(typeof(JobGiver_GetFood), "TryGiveJob")]
        public class JobGiver_GetFoodPatch
        {
            [HarmonyPrefix]
            private static bool JobGiver_GetFoodPrefix(JobGiver_GetFood __instance, ref Job __result, float ___maxLevelPercentage, HungerCategory ___minCategory, Pawn pawn)
            {
                if (pawn.def.race.Humanlike)
                {
                    ZLogger.Message(pawn + " starting food search");
                    try
                    {
                        var ZTracker = ZUtils.ZTracker;

                        if (ZTracker.jobTracker == null)
                        {
                            ZTracker.jobTracker = new Dictionary<Pawn, JobTracker>();
                        }

                        if (!ZTracker.jobTracker.TryGetValue(pawn, out JobTracker jobTracker))
                        {
                            jobTracker = new JobTracker();
                            ZTracker.jobTracker[pawn] = jobTracker;
                        }
                        if (jobTracker?.activeJobs?.Any() ?? false)
                        {
                            if (pawn.needs.food.CurCategory < HungerCategory.Starving
                                && !pawn.jobs.jobQueue.Contains(jobTracker.activeJobs[0]))
                            {
                                if (jobTracker.activeJobs[0].def.defName != "UnloadYourHauledInventory"
                                    && jobTracker.activeJobs[0].TryMakePreToilReservations(pawn, false))
                                {
                                    ZLogger.Message("Queue: " + jobTracker.activeJobs[0]);
                                    pawn.jobs.jobQueue.EnqueueLast(jobTracker.activeJobs[0]);
                                    return false;
                                }
                            }
                            else if (pawn.needs.food.CurCategory < HungerCategory.Starving
                                && pawn.jobs.curJob == null
                                && jobTracker.activeJobs[0].TryMakePreToilReservations(pawn, false))
                            {
                                ZLogger.Message("1 START JOB "
                                    + jobTracker.activeJobs[0] + " FOR " + pawn);
                                __result = jobTracker.activeJobs[0];
                                jobTracker.activeJobs.RemoveAt(0);

                                return false;
                            }
                            else if (pawn.jobs.curJob == null && jobTracker.activeJobs[0] != null)
                            {
                                ZLogger.Message("2 START JOB " + jobTracker.activeJobs[0]
                                    + " FOR " + pawn);
                                __result = jobTracker.activeJobs[0];
                                jobTracker.activeJobs.RemoveAt(0);
                                return false;
                                //ZLogger.Message("1 RESETTING JOB TRACKER FOR " + pawn);
                                //ZLogger.Message(pawn + " - pawn.jobs.curJob: " + pawn.jobs.curJob);
                                //ZLogger.Message(pawn + " - jobTracker.activeJobs[0]: " + jobTracker.activeJobs[0]);
                                //foreach (var job in pawn.jobs.jobQueue)
                                //{
                                //    ZLogger.Message(pawn + " - job in pawn queue: " + job.job);
                                //}
                                //foreach (var job in jobTracker.activeJobs)
                                //{
                                //    ZLogger.Message(pawn + " - job in ZTracker queue: " + job);
                                //}
                                //ZTracker.ResetJobTrackerFor(pawn);
                            }
                        }
                        Job result;
                        var oldMap = pawn.Map;
                        var oldPosition = pawn.Position;
                        bool select = false;
                        if (Find.Selector.SelectedObjects.Contains(pawn)) select = true;
                        ZTracker.ResetJobTrackerFor(pawn);
                        try
                        {
                            if (pawn.MentalStateDef != null)
                            {
                                result = JobGiver_GetFoodPatch.TryGiveJob(pawn, __instance.forceScanWholeMap,
                                    ___maxLevelPercentage, ___minCategory);
                                ZLogger.Pause(pawn + " in mental state, result: " + result);
                                if (result != null && result.targetA.Thing != null && result.targetA.Thing.Map == pawn.Map)
                                {
                                    __result = result;
                                    return false;
                                }
                                else
                                {
                                    return true;
                                }
                            }

                            foreach (var otherMap in ZUtils.GetAllMapsInClosestOrder(pawn, oldMap, oldPosition))
                            {
                                ZLogger.Message("Searching food job for " + pawn + " in " + ZTracker.GetMapInfo(otherMap)
                                    + " for " + ZTracker.GetMapInfo(oldMap));
                                result = JobGiver_GetFoodPatch.TryGiveJob(pawn, __instance.forceScanWholeMap,
                                    ___maxLevelPercentage, ___minCategory);
                                if (result != null)
                                {
                                    ZLogger.Message(pawn + " got food job " + result + " - map: "
                                        + ZTracker.GetMapInfo(pawn.Map) + " - " + pawn.Position);
                                    ZUtils.TeleportThing(pawn, oldMap, oldPosition);

                                    ZTracker.BuildJobListFor(pawn, otherMap, result);
                                    __result = jobTracker.activeJobs[0];
                                    jobTracker.activeJobs.RemoveAt(0);
                                    break;
                                }
                            }

                            ZUtils.TeleportThing(pawn, oldMap, oldPosition);
                            if (select) Find.Selector.Select(pawn);
                            return false;
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Some kind of error occurred in Z-Levels JobManager (reverting cloning bug): " + ex);
                            ZUtils.TeleportThing(pawn, oldMap, oldPosition);
                            if (select) Find.Selector.Select(pawn);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Some kind of error occurred in Z-Levels JobManager: " + ex);
                    }
                }
                return true;
            }

            private static Job TryGiveJob(Pawn pawn, bool forceScanWholeMap, float maxLevelPercentage, HungerCategory minCategory)
            {
                Need_Food food = pawn.needs.food;
                if (food == null || food.CurCategory < minCategory || food.CurLevelPercentage > maxLevelPercentage)
                {
                    return null;
                }
                bool allowCorpse;
                if (pawn.AnimalOrWildMan())
                {
                    allowCorpse = true;
                }
                else
                {
                    Hediff firstHediffOfDef = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Malnutrition, false);
                    allowCorpse = (firstHediffOfDef != null && firstHediffOfDef.Severity > 0.4f);
                }
                bool desperate = pawn.needs.food.CurCategory == HungerCategory.Starving;
                Thing thing;
                ThingDef thingDef;
                if (!FoodUtility.TryFindBestFoodSourceFor(pawn, pawn, desperate, out thing, out thingDef, true, true, false, allowCorpse, false, pawn.IsWildMan(), forceScanWholeMap, false, FoodPreferability.Undefined))
                {
                    return null;
                }
                Pawn pawn2 = thing as Pawn;
                if (pawn2 != null)
                {
                    Job job = JobMaker.MakeJob(JobDefOf.PredatorHunt, pawn2);
                    job.killIncappedTarget = true;
                    return job;
                }
                if (thing is Plant && thing.def.plant.harvestedThingDef == thingDef)
                {
                    return JobMaker.MakeJob(JobDefOf.Harvest, thing);
                }
                Building_NutrientPasteDispenser building_NutrientPasteDispenser = thing as Building_NutrientPasteDispenser;
                if (building_NutrientPasteDispenser != null && !building_NutrientPasteDispenser.HasEnoughFeedstockInHoppers())
                {
                    Building building = building_NutrientPasteDispenser.AdjacentReachableHopper(pawn);
                    if (building != null)
                    {
                        ISlotGroupParent hopperSgp = building as ISlotGroupParent;
                        Job job2 = WorkGiver_CookFillHopper.HopperFillFoodJob(pawn, hopperSgp);
                        if (job2 != null)
                        {
                            return job2;
                        }
                    }
                    thing = FoodUtility.BestFoodSourceOnMap(pawn, pawn, desperate, out thingDef, FoodPreferability.MealLavish, false, !pawn.IsTeetotaler(), false, false, false, false, false, false, forceScanWholeMap, false, FoodPreferability.Undefined);
                    if (thing == null)
                    {
                        return null;
                    }
                }
                float nutrition = FoodUtility.GetNutrition(thing, thingDef);
                Job job3 = JobMaker.MakeJob(JobDefOf.Ingest, thing);
                job3.count = FoodUtility.WillIngestStackCountOf(pawn, thingDef, nutrition);
                return job3;
            }
        }

        [HarmonyPatch(typeof(JobGiver_GetJoy), "TryGiveJob")]
        public class JobGiver_GetJoyPatch
        {
            [HarmonyPrefix]
            private static bool JobGiver_GetJoyPrefix(JobGiver_GetJoy __instance, ref Job __result,
                 ref DefMap<JoyGiverDef, float> ___joyGiverChances, Pawn pawn)
            {
                try
                {
                    ZLogger.Message(pawn + " starting joy search");
                    var ZTracker = ZUtils.ZTracker;
                    if (ZTracker.jobTracker == null)
                    {
                        ZTracker.jobTracker = new Dictionary<Pawn, JobTracker>();
                    }
                    if (!ZTracker.jobTracker.TryGetValue(pawn, out JobTracker jobTracker))
                    {
                        jobTracker = new JobTracker();
                        ZTracker.jobTracker[pawn] = jobTracker;
                    }
                    if (jobTracker?.activeJobs?.Any() ?? false)
                    {
                        if (pawn?.needs?.joy?.CurCategory > JoyCategory.Low
                            && !pawn.jobs.jobQueue.Contains(jobTracker.activeJobs[0]))
                        {
                            if (jobTracker.activeJobs[0].def.defName != "UnloadYourHauledInventory"
                                && jobTracker.activeJobs[0].TryMakePreToilReservations(pawn, false))
                            {
                                ZLogger.Message("Queue: " + jobTracker.activeJobs[0]);
                                pawn.jobs.jobQueue.EnqueueLast(jobTracker.activeJobs[0]);
                                return false;
                            }

                        }
                        else if (pawn?.needs?.joy?.CurCategory > JoyCategory.Low && pawn.jobs.curJob == null
                            && jobTracker.activeJobs[0].TryMakePreToilReservations(pawn, false))
                        {

                            //ZLogger.Message("2 START JOB "
                            //    + jobTracker.activeJobs[0] + " FOR " + pawn);
                            //pawn.jobs.StartJob(jobTracker.activeJobs[0]);
                            //jobTracker.activeJobs.RemoveAt(0);
                            ZLogger.Message("Return 4");
                            return false;
                        }
                        else if (pawn?.needs?.joy?.CurCategory <= JoyCategory.Low && pawn.jobs.curJob != jobTracker.activeJobs[0])
                        {
                            ZLogger.Message("2 RESETTING JOB TRACKER FOR " + pawn);
                            ZLogger.Message(pawn + " - pawn.jobs.curJob: " + pawn.jobs.curJob);
                            ZLogger.Message(pawn + " - jobTracker.activeJobs[0]: " + jobTracker.activeJobs[0]);
                            foreach (var job in pawn.jobs.jobQueue)
                            {
                                ZLogger.Message(pawn + " - job in pawn queue: " + job.job);
                            }
                            foreach (var job in jobTracker.activeJobs)
                            {
                                ZLogger.Message(pawn + " - job in ZTracker queue: " + job);
                            }
                            ZTracker.ResetJobTrackerFor(pawn);
                        }
                    }
                    Job result = null;
                    var oldMap = pawn.Map;
                    var oldPosition = pawn.Position;
                    bool select = false;
                    if (Find.Selector.SelectedObjects.Contains(pawn)) select = true;
                    ZTracker.ResetJobTrackerFor(pawn);

                    try
                    {
                        var jobList = new Dictionary<Job, Map>();
                        bool CanDoDuringMedicalRest = __instance.CanDoDuringMedicalRest;
                        bool InBed = pawn.InBed();

                        if (pawn.MentalStateDef != null)
                        {
                            __result = TryGiveJob(pawn, CanDoDuringMedicalRest, InBed, ___joyGiverChances, __instance);
                            ZLogger.Pause(pawn + " in mental state, result: " + __result);
                            return false;
                        }
                        foreach (var otherMap in ZUtils.GetAllMapsInClosestOrder(pawn, oldMap, oldPosition))
                        {
                            ZLogger.Message(pawn + " - other map: " + otherMap);
                            ZLogger.Message("Searching joy job for " + pawn + " in " + ZTracker.GetMapInfo(otherMap)
                                + " for " + ZTracker.GetMapInfo(oldMap));

                            result = TryGiveJob(pawn, CanDoDuringMedicalRest, InBed, ___joyGiverChances, __instance);
                            if (result != null)
                            {
                                ZLogger.Message(pawn + " got joy job " + result + " - map: "
                                    + ZTracker.GetMapInfo(pawn.Map) + " - " + pawn.Position);
                                jobList[result] = otherMap;
                            }
                        }

                        ZUtils.TeleportThing(pawn, oldMap, oldPosition);

                        if (jobList.Count > 0 && jobList.Keys.TryRandomElementByWeight(j => j.def.joyGainRate, out Job job))
                        {
                            //var job = jobList.RandomElement();
                            ZLogger.Message(pawn + " found joy " + job);
                            ZTracker.BuildJobListFor(pawn, jobList[job], job);
                            __result = jobTracker.activeJobs[0];
                            jobTracker.activeJobs.RemoveAt(0);
                        }
                        else
                        {
                            ZLogger.Message(pawn + " cant find joy job");
                        }

                        if (select) Find.Selector.Select(pawn);

                        return false;
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Some kind of error occurred in Z-Levels JobManager (reverting cloning bug): " + ex);
                        ZUtils.TeleportThing(pawn, oldMap, oldPosition);
                        if (select) Find.Selector.Select(pawn);
                    }

                }
                catch (Exception ex)
                {
                    Log.Error("Some kind of error occurred in Z-Levels JobManager: " + ex);
                }
                return true;
            }
            public static Job TryGiveJob(Pawn pawn, bool CanDoDuringMedicalRest, bool InBed, DefMap<JoyGiverDef, float> joyGiverChances, JobGiver_GetJoy __instance)
            {
                if (!CanDoDuringMedicalRest && InBed && HealthAIUtility.ShouldSeekMedicalRest(pawn))
                {
                    return null;
                }
                List<JoyGiverDef> allDefsListForReading = DefDatabase<JoyGiverDef>.AllDefsListForReading;
                JoyToleranceSet tolerances = pawn.needs.joy.tolerances;
                for (int i = 0; i < allDefsListForReading.Count; i++)
                {
                    JoyGiverDef joyGiverDef = allDefsListForReading[i];
                    joyGiverChances[joyGiverDef] = 0f;
                    if (pawn.needs.joy.tolerances.BoredOf(joyGiverDef.joyKind) || !joyGiverDef.Worker.CanBeGivenTo(pawn))
                    {
                        continue;
                    }
                    if (joyGiverDef.pctPawnsEverDo < 1f)
                    {
                        Rand.PushState(pawn.thingIDNumber ^ 0x3C49C49);
                        if (Rand.Value >= joyGiverDef.pctPawnsEverDo)
                        {
                            Rand.PopState();
                            continue;
                        }
                        Rand.PopState();
                    }
                    float num = tolerances[joyGiverDef.joyKind];
                    float b = Mathf.Pow(1f - num, 5f);
                    b = Mathf.Max(0.001f, b);
                    joyGiverChances[joyGiverDef] = joyGiverDef.Worker.GetChance(pawn) * b;
                }
                for (int j = 0; j < joyGiverChances.Count; j++)
                {
                    if (!allDefsListForReading.TryRandomElementByWeight((JoyGiverDef d) => joyGiverChances[d], out JoyGiverDef result))
                    {
                        break;
                    }
                    try
                    {
                        Job job = __instance.TryGiveJobFromJoyGiverDefDirect(result, pawn);
                        if (job != null)
                        {
                            return job;
                        }
                        joyGiverChances[result] = 0f;
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error in JobGiver_GetJoy: " + ex + " - " + result, true);
                    }
                }
                return null;
            }
        }

        [HarmonyPatch(typeof(JobGiver_GetRest), "TryGiveJob")]
        public class JobGiver_GetRestPatch
        {
            [HarmonyPrefix]
            private static bool JobGiver_GetRestPrefix(JobGiver_GetRest __instance, ref Job __result,
                RestCategory ___minCategory, float ___maxLevelPercentage, Pawn pawn)
            {
                try
                {
                    var ZTracker = ZUtils.ZTracker;
                    if (pawn.Faction == Faction.OfPlayer)
                    {
                        ZLogger.Message(pawn + " search for rest job");
                        if (ZTracker.jobTracker == null)
                        {
                            ZTracker.jobTracker = new Dictionary<Pawn, JobTracker>();
                        }
                        if (!ZTracker.jobTracker.TryGetValue(pawn, out JobTracker jobTracker))
                        {
                            jobTracker = new JobTracker();
                            ZTracker.jobTracker[pawn] = jobTracker;
                        }
                        if (jobTracker?.activeJobs?.Any() ?? false)
                        {
                            if (pawn.needs.rest.CurCategory < RestCategory.Exhausted &&
                                !pawn.jobs.jobQueue.Contains(jobTracker.activeJobs[0]))
                            {
                                if (jobTracker.activeJobs[0].def.defName != "UnloadYourHauledInventory"
                                    && jobTracker.activeJobs[0].TryMakePreToilReservations(pawn, false))
                                {
                                    ZLogger.Message("Queue: " + jobTracker.activeJobs[0]);

                                    try
                                    {
                                        ZLogger.Message("--------------------------");
                                        for (int i = jobTracker.mainJob.targetQueueB.Count - 1; i >= 0; i--)
                                        {
                                            var target = jobTracker.mainJob.targetQueueB[i];

                                            ZLogger.Message("31 job.targetQueueB: " + target.Thing);
                                            ZLogger.Message("31 job.targetQueueB.Map: " + target.Thing.Map);
                                            ZLogger.Message("31 job.targetQueueB.stackCount: " + target.Thing.stackCount);
                                            ZLogger.Message("31 job.targetQueueB.countQueue: " + jobTracker.mainJob.countQueue[i]);
                                        }
                                    }
                                    catch { }

                                    pawn.jobs.jobQueue.EnqueueLast(jobTracker.activeJobs[0]);

                                    return false;
                                }
                            }
                            else if (pawn.needs.rest.CurCategory < RestCategory.Exhausted &&
                                pawn.jobs.curJob == null)
                            {
                                ZLogger.Message("3 START JOB "
                                    + jobTracker.activeJobs[0] + " FOR " + pawn);
                                __result = jobTracker.activeJobs[0];
                                jobTracker.activeJobs.RemoveAt(0);

                                return false;
                            }
                            else if (pawn.needs.rest.CurCategory >= RestCategory.Exhausted &&
                                pawn.jobs.curJob != jobTracker.activeJobs[0])
                            {
                                ZLogger.Message("3 RESETTING JOB TRACKER FOR " + pawn);
                                ZLogger.Message(pawn + " - pawn.jobs.curJob: " + pawn.jobs.curJob);
                                ZLogger.Message(pawn + " - jobTracker.activeJobs[0]: " + jobTracker.activeJobs[0]);
                                foreach (var job in pawn.jobs.jobQueue)
                                {
                                    ZLogger.Message(pawn + " - job in pawn queue: " + job.job);
                                }
                                foreach (var job in jobTracker.activeJobs)
                                {
                                    ZLogger.Message(pawn + " - job in ZTracker queue: " + job);
                                }
                                ZTracker.ResetJobTrackerFor(pawn);

                            }
                        }

                        Job result = null;
                        var oldMap = pawn.Map;
                        var oldPosition = pawn.Position;
                        bool select = false;
                        if (Find.Selector.SelectedObjects.Contains(pawn)) select = true;
                        ZTracker.ResetJobTrackerFor(pawn);

                        try
                        {
                            if (pawn.MentalStateDef != null)
                            {
                                result = JobGiver_GetRestPatch.TryGiveJob(pawn, ___minCategory, ___maxLevelPercentage);
                                ZLogger.Pause(pawn + " in mental state, result: " + result);
                                if (result.targetA.Thing == null && result.targetA.Thing.Map == pawn.Map)
                                {
                                    __result = result;
                                    return false;
                                }
                                else
                                {
                                    return true;
                                }
                            }
                            foreach (var otherMap in ZUtils.GetAllMapsInClosestOrder(pawn, oldMap, oldPosition))
                            {
                                ZLogger.Message("Searching rest job for " + pawn + " in " + ZTracker.GetMapInfo(otherMap)
                                    + " for " + ZTracker.GetMapInfo(oldMap));
                                result = JobGiver_GetRestPatch.TryGiveJob(pawn, ___minCategory, ___maxLevelPercentage);
                                if (result != null && result.targetA.Thing != null)
                                {
                                    ZLogger.Message(pawn + " got rest job " + result + " - map: "
                                        + ZTracker.GetMapInfo(pawn.Map) + " - " + pawn.Position);
                                    ZUtils.TeleportThing(pawn, oldMap, oldPosition);

                                    ZTracker.BuildJobListFor(pawn, result.targetA.Thing.Map, result);
                                    __result = jobTracker.activeJobs[0];
                                    jobTracker.activeJobs.RemoveAt(0);
                                    break;
                                }
                            }
                            ZUtils.TeleportThing(pawn, oldMap, oldPosition);

                            if (result.targetA.Thing == null)
                            {
                                ZLogger.Message(pawn + " taking rest on the ground");
                                __result = JobMaker.MakeJob(JobDefOf.LayDown, FindGroundSleepSpotFor(pawn));
                            }
                            if (select) Find.Selector.Select(pawn);

                            return false;
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Some kind of error occurred in Z-Levels JobManager (reverting cloning bug): " + ex);
                            ZUtils.TeleportThing(pawn, oldMap, oldPosition);
                            if (select) Find.Selector.Select(pawn);
                        }

                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Some kind of error occurred in Z-Levels JobManager: " + ex);
                }
                return true;
            }

            private static Job TryGiveJob(Pawn pawn, RestCategory minCategory, float maxLevelPercentage = 1f)
            {
                Need_Rest rest = pawn.needs.rest;
                if (rest == null || rest.CurCategory < minCategory || rest.CurLevelPercentage > maxLevelPercentage)
                {
                    return null;
                }
                if (RestUtility.DisturbancePreventsLyingDown(pawn))
                {
                    return null;
                }
                Lord lord = pawn.GetLord();
                Building_Bed building_Bed;
                var ZTracker = ZUtils.ZTracker;
                if ((lord != null && lord.CurLordToil != null && !lord.CurLordToil.AllowRestingInBed) || pawn.IsWildMan())
                {
                    building_Bed = null;
                }
                else
                {
                    if (pawn.ownership?.OwnedBed != null && ZTracker.GetAllMaps(pawn.Map.Tile)
                        .Contains(pawn.ownership?.OwnedBed?.Map))
                    {
                        building_Bed = pawn.ownership.OwnedBed;
                    }
                    else
                    {
                        building_Bed = RestUtility.FindBedFor(pawn);
                    }
                }
                if (building_Bed != null)
                {
                    ZLogger.Message("Found " + building_Bed + " for " + pawn + " in " + ZTracker.GetMapInfo(building_Bed.Map));
                    return JobMaker.MakeJob(JobDefOf.LayDown, building_Bed);
                }
                return JobMaker.MakeJob(JobDefOf.LayDown, FindGroundSleepSpotFor(pawn));
            }

            private static IntVec3 FindGroundSleepSpotFor(Pawn pawn)
            {
                Map map = pawn.Map;
                for (int i = 0; i < 2; i++)
                {
                    int radius = (i == 0) ? 4 : 12;
                    if (CellFinder.TryRandomClosewalkCellNear(pawn.Position, map, radius, out IntVec3 result, (IntVec3 x) => !x.IsForbidden(pawn) && !x.GetTerrain(map).avoidWander))
                    {
                        return result;
                    }
                }
                return CellFinder.RandomClosewalkCellNearNotForbidden(pawn.Position, map, 4, pawn);
            }
        }


        [HarmonyPatch(typeof(WorkGiver_InteractAnimal), "TakeFoodForAnimalInteractJob")]
        public class TakeFoodForAnimalInteractJob_Patch
        {
            public static bool recursiveTrap;
            private static void Postfix(WorkGiver_InteractAnimal __instance, ref Job __result, Pawn pawn, Pawn tamee)
            {
                if (!recursiveTrap && __result is null)
                {
                    recursiveTrap = true;
                    Map oldMap = pawn.Map;
                    IntVec3 oldPosition = pawn.Position;
                    foreach (var map in ZUtils.GetAllMapsInClosestOrder(pawn, oldMap, oldPosition))
                    {
                        if (map != oldMap)
                        {
                            var newPosition = ZUtils.GetCellToTeleportFrom(pawn.Map, pawn.Position, map);
                            ZUtils.TeleportThing(pawn, map, newPosition);
                            var job = __instance.TakeFoodForAnimalInteractJob(pawn, tamee);
                            if (job != null)
                            {
                                __result = job;
                            }
                        }
                    }
                    ZUtils.TeleportThing(pawn, oldMap, oldPosition);
                    recursiveTrap = false;
                }
            }
        }

        [HarmonyPatch(typeof(WorkGiver_ConstructDeliverResources), "FindNearbyNeeders")]
        public class FindNearbyNeedersPatch
        {
            private static Map oldMap;
            private static IntVec3 oldPosition;
            private static void Prefix(Pawn pawn, ThingDefCountClass need, IConstructible c, int resTotalAvailable,
                bool canRemoveExistingFloorUnderNearbyNeeders, ref int neededTotal, ref Job jobToMakeNeederAvailable, out bool __state)
            {
                if (c is Thing thing && thing.Map != pawn.Map)
                {
                    oldMap = pawn.Map;
                    oldPosition = pawn.Position;
                    ZUtils.TeleportThing(pawn, thing.Map, thing.Position);
                    __state = true;
                }
                else
                {
                    __state = false;
                }
            }
            private static void Postfix(Pawn pawn, ThingDefCountClass need, IConstructible c, int resTotalAvailable,
                bool canRemoveExistingFloorUnderNearbyNeeders, ref int neededTotal, ref Job jobToMakeNeederAvailable, bool __state)
            {
                if (__state && pawn.Map != oldMap)
                {
                    ZUtils.TeleportThing(pawn, oldMap, oldPosition);
                }
            }
        }

        [HarmonyPatch(typeof(GenConstruct), "CanConstruct")]
        public static class CanConstructPatch
        {
            private static Map oldMap;
            private static IntVec3 oldPosition;
            private static void Prefix(Thing t, Pawn p, out bool __state, bool checkSkills = true, bool forced = false)
            {
                if (t.Map != p.Map)
                {
                    oldMap = p.Map;
                    oldPosition = p.Position;
                    ZUtils.TeleportThing(p, t.Map, t.Position);
                    __state = true;
                }
                else
                {
                    __state = false;
                }
            }
            private static void Postfix(Thing t, Pawn p, bool __state, bool checkSkills = true, bool forced = false)
            {
                if (__state && p.Map != oldMap)
                {
                    ZUtils.TeleportThing(p, oldMap, oldPosition);
                }
            }
        }
        [HarmonyPatch(typeof(WorkGiver_Haul), "PotentialWorkThingsGlobal")]
        public static class PotentialWorkThingsGlobalPatch
        {
            private static void Postfix(ref IEnumerable<Thing> __result, Pawn pawn)
            {
                var list = __result.ToList();
                foreach (var map in ZUtils.ZTracker.GetAllMaps(pawn.Tile))
                {
                    if (map != pawn.Map)
                    {
                        list.AddRange(map.listerHaulables.ThingsPotentiallyNeedingHauling());
                    }
                }
                __result = list;
            }
        }

        [HarmonyPatch(typeof(WorkGiver_DoBill), "ShouldSkip")]
        public static class ShouldSkipPatch
        {
            private static void Postfix(ref bool __result, WorkGiver_DoBill __instance, Pawn pawn, bool forced = false)
            {
                if (__result)
                {
                    foreach (var map in ZUtils.ZTracker.GetAllMaps(pawn.Tile))
                    {
                        if (map != pawn.Map)
                        {
                            List<Thing> list = map.listerThings.ThingsInGroup(ThingRequestGroup.PotentialBillGiver);
                            for (int i = 0; i < list.Count; i++)
                            {
                                IBillGiver billGiver;
                                if ((billGiver = (list[i] as IBillGiver)) != null && __instance.ThingIsUsableBillGiver(list[i]) && billGiver.BillStack.AnyShouldDoNow)
                                {
                                    __result = false;
                                    return;
                                }
                            }
                        }
                    }
                }

            }
        }
        [HarmonyPatch(typeof(StoreUtility), "IsInValidBestStorage")]
        public static class IsInValidBestStoragePatch
        {
            public static bool Prefix(Thing t, ref bool __result)
            {
                IHaulDestination haulDestination = StoreUtility.CurrentHaulDestinationOf(t);
                if (haulDestination == null || !haulDestination.Accepts(t))
                {
                    __result = false;
                    return false;
                }
                var ZTracker = ZUtils.ZTracker;
                foreach (var map in ZTracker.GetAllMapsInClosestOrder(t.Map))
                {
                    if (StoreUtility.TryFindBestBetterStorageFor(t, null, map,
                        haulDestination.GetStoreSettings().Priority,
                        Faction.OfPlayer, out IntVec3 _, out IHaulDestination _,
                        needAccurateResult: false))
                    {
                        __result = false;
                        return false;
                    }
                }
                __result = true;
                return false;
            }
        }


        [HarmonyPatch(typeof(Pawn_CarryTracker), "TryDropCarriedThing")]
        [HarmonyPatch(new Type[]
        {
            typeof(IntVec3),
            typeof(ThingPlaceMode),
            typeof(Thing),
            typeof(Action<Thing, int>)
        }, new ArgumentType[]
        {
             ArgumentType.Normal,
             ArgumentType.Normal,
             ArgumentType.Out,
             ArgumentType.Normal
        })]
        public static class TryDropCarriedThingPatch
        {
            public static bool blockTryDrop = false;
            private static bool Prefix(Pawn_CarryTracker __instance, IntVec3 dropLoc, ThingPlaceMode mode, Thing resultingThing, Action<Thing, int> placedAction = null)
            {
                if (__instance.pawn.RaceProps.Humanlike)
                {
                    var ZTracker = ZUtils.ZTracker;
                    if (ZTracker.jobTracker != null && ZTracker.jobTracker.TryGetValue(__instance.pawn, out var jobTracker) && jobTracker.activeJobs?.Count > 0 && blockTryDrop)
                    {
                        return false;
                    }
                }
                return true;
            }
        }


        [HarmonyPatch(typeof(Pawn_JobTracker), "DetermineNextJob")]
        public class DetermineNextJobPatch
        {
            private static void Postfix(Pawn_JobTracker __instance, Pawn ___pawn, ThinkResult __result, ref ThinkTreeDef thinkTree)
            {
                if (___pawn.RaceProps.Humanlike)
                {
                    try
                    {
                        ZLogger.Message(___pawn + " got next job " + __result.Job);
                    }
                    catch
                    {

                    }
                }
            }
        }

        [HarmonyPatch(typeof(Pawn_JobTracker), "StartJob")]
        public class StartJobPatch
        {
            private static void Postfix(Pawn_JobTracker __instance, Pawn ___pawn, Job newJob, JobTag? tag)
            {
                if (___pawn.RaceProps.Humanlike)
                {
                    try
                    {
                        ZLogger.Message(___pawn + " starts " + newJob);
                    }
                    catch
                    {
                        ZLogger.Message(___pawn + " starts " + newJob.def);
                    }
                }
            }
        }


        [HarmonyPatch(typeof(Pawn_JobTracker), "EndCurrentJob")]
        public class EndCurrentJobPatch
        {
            private static void Prefix(Pawn_JobTracker __instance, Pawn ___pawn, JobCondition condition, ref bool startNewJob, bool canReturnToPool = true)
            {
                if (___pawn.RaceProps.Humanlike)
                {
                    var ZTracker = ZUtils.ZTracker;
                    if (ZTracker.jobTracker != null && ZTracker.jobTracker.ContainsKey(___pawn) && ZTracker.jobTracker[___pawn].activeJobs?.Count > 0)
                    {
                        TryDropCarriedThingPatch.blockTryDrop = true;
                        startNewJob = false;
                    }
                    try
                    {
                        ZLogger.Message(___pawn + " ends " + __instance.curJob + " - " + startNewJob);
                    }
                    catch
                    {

                    }

                }
            }

            private static void Postfix(Pawn_JobTracker __instance, Pawn ___pawn, JobCondition condition, ref bool startNewJob, bool canReturnToPool = true)
            {

                if (___pawn.RaceProps.Humanlike)
                {
                    var ZTracker = ZUtils.ZTracker;
                    if (___pawn.CurJob == null && ZTracker.jobTracker != null && ZTracker.jobTracker.ContainsKey(___pawn)
                        && ZTracker.jobTracker[___pawn].activeJobs?.Count > 0)
                    {
                        TryDropCarriedThingPatch.blockTryDrop = false;
                        ZTracker.TryTakeFirstJob(___pawn);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(JobGiver_Work), "TryIssueJobPackage")]
        public class TryIssueJobPackagePatch
        {
            private static bool Prefix(JobGiver_Work __instance, bool ___emergency, ref ThinkResult __result, Pawn pawn, JobIssueParams jobParams)
            {
                if (pawn.RaceProps.Humanlike)
                {
                    var ZTracker = ZUtils.ZTracker;
                    try
                    {
                        if (!ZTracker.jobTracker.TryGetValue(pawn, out JobTracker jobTracker))
                        {
                            jobTracker = new JobTracker();
                            ZTracker.jobTracker[pawn] = jobTracker;
                        }
                        if (jobTracker.activeJobs?.Any() ?? false)
                        {
                            if (!pawn.jobs.jobQueue.Contains(jobTracker.activeJobs[0]))
                            {
                                if (jobTracker.activeJobs[0].def.defName != "UnloadYourHauledInventory" && jobTracker.activeJobs[0].TryMakePreToilReservations(pawn, false))
                                {
                                    __result = new ThinkResult(jobTracker.activeJobs[0], jobTracker.activeJobs[0].jobGiver);
                                    jobTracker.activeJobs.RemoveAt(0);
                                    return false;
                                }
                            }
                            else if (pawn.jobs.curJob == null)
                            {

                            }
                            else if (pawn.jobs.curJob != jobTracker.activeJobs[0])
                            {
                                ZLogger.Message("4 RESETTING JOB TRACKER FOR " + pawn);
                                ZTracker.ResetJobTrackerFor(pawn);
                            }
                        }
                        else
                        {
                            ZLogger.Message(pawn + " has no active jobs");
                        }
                        ThinkResult result;
                        var oldMap = pawn.Map;
                        var oldPosition = pawn.Position;
                        bool select = false;
                        if (Find.Selector.SelectedObjects.Contains(pawn)) select = true;
                        ZTracker.ResetJobTrackerFor(pawn);

                        try
                        {
                            jobTracker.searchingJobsNow = true;
                            jobTracker.oldMap = pawn.Map;
                            jobTracker.pawn = pawn;
                            result = TryIssueJobPackage(pawn, jobParams, __instance, ___emergency, jobTracker, oldMap, oldPosition);
                            jobTracker.searchingJobsNow = false;
                            if (result.Job != null)
                            {
                                ZUtils.TeleportThing(pawn, oldMap, oldPosition);
                                ZLogger.Message(pawn + " got job " + result + " - map: "
                                    + ZTracker.GetMapInfo(pawn.Map) + " - " + pawn.Position, debugLevel: DebugLevel.Jobs);
                                if (jobTracker.mapDest != null)
                                {
                                    ZTracker.BuildJobListFor(pawn, jobTracker.mapDest, result.Job);
                                }
                                else
                                {
                                    ZTracker.BuildJobListFor(pawn, oldMap, result.Job);
                                }
                                ZLogger.Message($"Assigned local data: jobTracker.lookedAtMap: {ZTracker.GetMapInfo(jobTracker.mapDest)}, jobTracker.lookedAtLocalCell: {jobTracker.lookedAtLocalCellMap}");
                                __result = new ThinkResult(jobTracker.activeJobs[0], jobTracker.activeJobs[0].jobGiver);
                                jobTracker.activeJobs.RemoveAt(0);
                            }
                            else
                            {
                                __result = ThinkResult.NoJob;
                                ZTracker.ResetJobTrackerFor(pawn);
                                ZLogger.Message(pawn + " failed to find job", debugLevel: DebugLevel.Jobs);
                            }
                        }
                        catch (Exception ex)
                        {
                            ZLogger.Message("Exception in TryIssueJobPackagePatch: " + ex);
                        }

                        ZUtils.TeleportThing(pawn, oldMap, oldPosition);
                        if (select) Find.Selector.Select(pawn);
                        return false;
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Some kind of error occurred in Z-Levels JobManager: " + ex);
                    }
                }
                return true;
            }
            private static Job StartOrResumeBillJob(WorkGiver_DoBill scanner, Pawn pawn, IBillGiver giver, JobTracker jobTracker)
            {
                List<ThingCount> chosenIngThings = scanner.chosenIngThings;
                for (int i = 0; i < giver.BillStack.Count; i++)
                {
                    Bill bill = giver.BillStack[i];
                    if ((bill.recipe.requiredGiverWorkType != null && bill.recipe.requiredGiverWorkType
                        != scanner.def.workType) || (Find.TickManager.TicksGame < bill.lastIngredientSearchFailTicks
                        + WorkGiver_DoBill.ReCheckFailedBillTicksRange.RandomInRange
                        && FloatMenuMakerMap.makingFor != pawn))
                    {
                        continue;
                    }
                    bill.lastIngredientSearchFailTicks = 0;
                    if (!bill.ShouldDoNow() || !bill.PawnAllowedToStartAnew(pawn))
                    {
                        continue;
                    }
                    SkillRequirement skillRequirement = bill.recipe.FirstSkillRequirementPawnDoesntSatisfy(pawn);
                    if (skillRequirement != null)
                    {
                        JobFailReason.Is("UnderRequiredSkill".Translate(skillRequirement.minLevel), bill.Label);
                        continue;
                    }
                    Bill_ProductionWithUft bill_ProductionWithUft = bill as Bill_ProductionWithUft;

                    if (bill_ProductionWithUft != null)
                    {
                        if (bill_ProductionWithUft.BoundUft != null)
                        {
                            if (bill_ProductionWithUft.BoundWorker == pawn && pawn.CanReserveAndReach(bill_ProductionWithUft.BoundUft, PathEndMode.Touch, Danger.Deadly) && !bill_ProductionWithUft.BoundUft.IsForbidden(pawn))
                            {
                                return WorkGiver_DoBill.FinishUftJob(pawn, bill_ProductionWithUft.BoundUft, bill_ProductionWithUft);
                            }
                            continue;
                        }
                        UnfinishedThing unfinishedThing = WorkGiver_DoBill.ClosestUnfinishedThingForBill(pawn, bill_ProductionWithUft);

                        if (unfinishedThing != null)
                        {
                            return WorkGiver_DoBill.FinishUftJob(pawn, unfinishedThing, bill_ProductionWithUft);
                        }
                    }

                    bool flag = false;
                    var ZTracker = ZUtils.ZTracker;
                    var workBench = ((Thing)giver);
                    var origBillGiverMap = giver.Map;
                    var origPawnMap = pawn.Map;
                    var origBillGiverPosition = workBench.Position;
                    var origPawnPosition = pawn.Position;
                    ZLogger.Message(giver + " - billGiver.Map: " + ZTracker.GetMapInfo(giver.Map));
                    ZLogger.Message(giver + " - billGiver.Position: " + workBench.Position);
                    foreach (var map in ZTracker.GetAllMapsInClosestOrder(origBillGiverMap))
                    {
                        try
                        {
                            if (origBillGiverMap != map)
                            {
                                workBench.positionInt = ZUtils.GetCellToTeleportFrom(workBench.Map, workBench.Position, map);
                                pawn.positionInt = workBench.positionInt;
                            }
                            else if (workBench.Position != origBillGiverPosition)
                            {
                                workBench.positionInt = origBillGiverPosition;
                                pawn.positionInt = origPawnPosition;
                            }
                            workBench.mapIndexOrState = (sbyte)Find.Maps.IndexOf(map);
                            pawn.mapIndexOrState = (sbyte)Find.Maps.IndexOf(map);
                            flag = WorkGiver_DoBill.TryFindBestBillIngredients(bill, pawn, (Thing)giver, chosenIngThings);
                            ZLogger.Message("Found ingredients: " + flag + " in " + ZTracker.GetMapInfo(map) + " for " + bill);
                            if (flag) break;
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Z-Levels failed to process HasJobOnThing of DoBill workgiver. Report about it to devs and provide Hugslib log. Error: " + ex);
                        }
                    }

                    ZLogger.Message("Final position " + workBench.Position);
                    ZUtils.TeleportThing(workBench, origBillGiverMap, origBillGiverPosition);
                    ZUtils.TeleportThing(pawn, origPawnMap, origPawnPosition);

                    if (!flag)
                    {
                        if (FloatMenuMakerMap.makingFor != pawn)
                        {
                            bill.lastIngredientSearchFailTicks = Find.TickManager.TicksGame;
                        }
                        else
                        {
                            JobFailReason.Is("MissingMaterials".Translate(), bill.Label);
                        }
                        chosenIngThings.Clear();
                        continue;
                    }
                    Job haulOffJob;
                    Job result = WorkGiver_DoBill.TryStartNewDoBillJob(pawn, bill, giver, chosenIngThings, out haulOffJob);
                    chosenIngThings.Clear();
                    jobTracker.mapDest = giver.Map;
                    return result;
                }
                chosenIngThings.Clear();
                return null;
            }
            public static Job DoBillJobOnThing(WorkGiver_DoBill scanner, Pawn pawn, Thing thing, JobTracker jobTracker, bool forced = false)
            {
                IBillGiver billGiver = thing as IBillGiver;
                if (billGiver == null || !scanner.ThingIsUsableBillGiver(thing)
                    || !billGiver.BillStack.AnyShouldDoNow || !billGiver.UsableForBillsAfterFueling()
                    || !pawn.CanReserve(thing, 1, -1, null, forced) || thing.IsBurning()
                    || thing.IsForbidden(pawn))
                {
                    return null;
                }
                CompRefuelable compRefuelable = thing.TryGetComp<CompRefuelable>();

                if (compRefuelable == null || compRefuelable.HasFuel)
                {
                    billGiver.BillStack.RemoveIncompletableBills();
                    Job job = StartOrResumeBillJob(scanner, pawn, billGiver, jobTracker);
                    return job;
                }
                if (!RefuelWorkGiverUtility.CanRefuel(pawn, thing, forced))
                {
                    return null;
                }
                return RefuelWorkGiverUtility.RefuelJob(pawn, thing, forced, null, null);
            }

            // Construction job
            public static Job ConstructDeliverJobOnThing(WorkGiver_ConstructDeliverResourcesToBlueprints scanner, Pawn pawn, Thing t, bool forced = false)
            {
                if (t.Faction != pawn.Faction)
                {
                    return null;
                }
                Blueprint blueprint = t as Blueprint;
                if (blueprint == null)
                {
                    return null;
                }
                if (GenConstruct.FirstBlockingThing(blueprint, pawn) != null)
                {
                    return GenConstruct.HandleBlockingThingJob(blueprint, pawn, forced);
                }
                bool flag = scanner.def.workType == WorkTypeDefOf.Construction;
                if (!GenConstruct.CanConstruct(blueprint, pawn, flag, forced))
                {
                    return null;
                }
                if (!flag && blueprint.def.entityDefToBuild is TerrainDef
                    && pawn.Map.terrainGrid.CanRemoveTopLayerAt(blueprint.Position))
                {
                    return null;
                }
                Job job = scanner.RemoveExistingFloorJob(pawn, blueprint);
                if (job != null)
                {
                    return job;
                }
                var oldMap = pawn.Map;
                var oldPosition = pawn.Position;
                foreach (var map in ZUtils.GetAllMapsInClosestOrder(pawn, oldMap, oldPosition))
                {
                    Job job2 = scanner.ResourceDeliverJobFor(pawn, blueprint, true);
                    if (job2 != null)
                    {
                        ZUtils.TeleportThing(pawn, oldMap, oldPosition);
                        return job2;
                    }
                }
                ZUtils.TeleportThing(pawn, oldMap, oldPosition);
                if (scanner.def.workType != WorkTypeDefOf.Hauling)
                {
                    Job job3 = scanner.NoCostFrameMakeJobFor(pawn, blueprint);
                    if (job3 != null)
                    {
                        return job3;
                    }
                }
                return null;
            }
            public static Job ConstructDeliverJobOnThing(WorkGiver_ConstructDeliverResourcesToFrames scanner, Pawn pawn, Thing t, bool forced = false)
            {
                if (t.Faction != pawn.Faction)
                {
                    return null;
                }
                Frame frame = t as Frame;
                if (frame == null)
                {
                    return null;
                }

                if (GenConstruct.FirstBlockingThing(frame, pawn) != null)
                {
                    return GenConstruct.HandleBlockingThingJob(frame, pawn, forced);
                }
                bool checkSkills = scanner.def.workType == WorkTypeDefOf.Construction;
                if (!GenConstruct.CanConstruct(frame, pawn, checkSkills, forced))
                {
                    return null;
                }

                Job job2 = scanner.ResourceDeliverJobFor(pawn, frame, true);
                if (job2 == null)
                {
                    var oldMap = pawn.Map;
                    var oldPosition = pawn.Position;
                    foreach (var otherMap in ZUtils.GetAllMapsInClosestOrder(pawn, oldMap, oldPosition))
                    {
                        if (otherMap != oldMap)
                        {
                            job2 = scanner.ResourceDeliverJobFor(pawn, frame, true);
                            if (job2 != null)
                            {
                                break;
                            }
                        }
                    }
                    ZUtils.TeleportThing(pawn, oldMap, oldPosition);
                }
                return job2;
            }

            // Haul job
            public static bool TryFindBestBetterStoreCellFor(Thing t, Pawn carrier, Map mapToSearch, StoragePriority currentPriority, Faction faction, out IntVec3 foundCell, JobTracker jobTracker, bool needAccurateResult = true)
            {
                bool result = true;
                List<SlotGroup> allGroupsListInPriorityOrder = new List<SlotGroup>();
                foreach (var map in ZUtils.GetAllMapsInClosestOrder(carrier, carrier.Map, carrier.Position))
                {
                    allGroupsListInPriorityOrder.AddRange(map.haulDestinationManager.AllGroupsListInPriorityOrder);
                }

                if (allGroupsListInPriorityOrder.Count == 0)
                {
                    foundCell = IntVec3.Invalid;
                    result = false;
                }
                StoragePriority storagePriority = currentPriority;
                float num = 2.14748365E+09f;
                IntVec3 invalid = IntVec3.Invalid;
                int count = allGroupsListInPriorityOrder.Count;

                foreach (var slotGroup in allGroupsListInPriorityOrder.OrderByDescending(x => x.Settings.Priority))
                {
                    //ZLogger.Message("Checking on : " + t + " - " + ZTracker.GetMapInfo(t.Map) + " for " + carrier + " - " + carrier?.Map + " - " + slotGroup.parent
                    //    + " - priority: " + slotGroup.Settings.Priority + " - " + slotGroup.parent.Map);
                    StoragePriority priority = slotGroup.Settings.Priority;
                    if (priority < storagePriority || priority <= currentPriority)
                    {
                        //ZLogger.Message("Break on - " + slotGroup.parent + " - priority: " + slotGroup.Settings.Priority + " - " + slotGroup.parent.Map);
                        break;
                    }
                    TryFindBestBetterStoreCellForWorker(t, carrier, mapToSearch, faction, slotGroup, needAccurateResult, ref invalid, ref num, ref storagePriority, jobTracker);
                }
                if (!invalid.IsValid)
                {
                    foundCell = IntVec3.Invalid;
                    result = false;
                }
                foundCell = invalid;
                return result;
            }

            private static void TryFindBestBetterStoreCellForWorker(Thing t, Pawn carrier, Map map, Faction faction, SlotGroup slotGroup, bool needAccurateResult, ref IntVec3 closestSlot, ref float closestDistSquared, ref StoragePriority foundPriority, JobTracker jobTracker)
            {
                if (slotGroup == null)
                {
                    return;
                }
                if (!slotGroup.parent.Accepts(t))
                {
                    return;
                }

                if (slotGroup.HeldThings.Contains(t))
                {
                    //ZLogger.Message(t + " already in stockpile " + slotGroup.parent);
                    return;
                }
                //ZLogger.Message(slotGroup.parent + " - priority: " + slotGroup.Settings.Priority
                //    + " - " + slotGroup.parent.Map + " accepts " + t + " - " + t.Map, true);

                IntVec3 a = t.SpawnedOrAnyParentSpawned ? t.PositionHeld : carrier.PositionHeld;
                List<IntVec3> cellsList = slotGroup.CellsList;
                int count = cellsList.Count;
                int num;
                if (needAccurateResult)
                {
                    num = Mathf.FloorToInt((float)count * Rand.Range(0.005f, 0.018f));
                }
                else
                {
                    num = 0;
                }
                for (int i = 0; i < count; i++)
                {
                    IntVec3 intVec = cellsList[i];
                    //ZLogger.Message("Checking " + intVec + " in " + slotGroup + " in " + slotGroup.parent.Map);
                    float num2 = (float)(a - intVec).LengthHorizontalSquared;
                    if (num2 <= closestDistSquared && IsGoodStoreCell(intVec, slotGroup.parent.Map, t, carrier, faction))
                    {
                        closestSlot = intVec;
                        jobTracker.mapDest = slotGroup.parent.Map;
                        closestDistSquared = num2;
                        foundPriority = slotGroup.Settings.Priority;
                        //var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                        //ZLogger.Message("RESULT: " + carrier + " - " + slotGroup.parent + " - priority: "
                        //    + slotGroup.Settings.Priority + " - " + ZTracker.GetMapInfo(slotGroup.parent.Map)
                        //    + " accepts " + t + " in " + ZTracker.GetMapInfo(dest), true);
                        if (i >= num)
                        {
                            break;
                        }
                    }
                }
            }

            public static bool IsGoodStoreCell(IntVec3 c, Map map, Thing t, Pawn carrier, Faction faction)
            {
                if (carrier != null && c.IsForbidden(carrier))
                {
                    return false;
                }
                if (!NoStorageBlockersIn(c, map, t))
                {
                    return false;
                }
                if (carrier != null)
                {
                    if (!carrier.CanReserveNew(c))
                    {
                        return false;
                    }
                }
                else if (faction != null && map.reservationManager.IsReservedByAnyoneOf(c, faction))
                {
                    return false;
                }
                if (c.ContainsStaticFire(map))
                {
                    return false;
                }
                List<Thing> thingList = c.GetThingList(map);
                for (int i = 0; i < thingList.Count; i++)
                {
                    if (thingList[i] is IConstructible && GenConstruct.BlocksConstruction(thingList[i], t))
                    {
                        return false;
                    }
                }

                //var tPosition = t.SpawnedOrAnyParentSpawned ? t.PositionHeld : carrier.PositionHeld;
                //Log.Message($"{carrier} CanReach({tPosition}, {c}, PathEndMode.ClosestTouch, TraverseParms.For(carrier, Danger.Deadly, TraverseMode.ByPawn, false)): " +
                //    $"{carrier.Map.reachability.CanReach(tPosition, c, PathEndMode.ClosestTouch, TraverseParms.For(carrier, Danger.Deadly, TraverseMode.ByPawn, false))}");

                return true;// carrier == null || carrier.Map.reachability.CanReach(t.SpawnedOrAnyParentSpawned ? t.PositionHeld : carrier.PositionHeld, c, PathEndMode.ClosestTouch,
                            //TraverseParms.For(carrier, Danger.Deadly, TraverseMode.ByPawn, false));
            }

            private static bool NoStorageBlockersIn(IntVec3 c, Map map, Thing thing)
            {
                List<Thing> list = map.thingGrid.ThingsListAt(c);
                for (int i = 0; i < list.Count; i++)
                {
                    Thing thing2 = list[i];
                    if (thing2.def.EverStorable(false))
                    {
                        if (!thing2.CanStackWith(thing))
                        {
                            return false;
                        }
                        if (thing2.stackCount >= thing.def.stackLimit)
                        {
                            return false;
                        }
                    }
                    if (thing2.def.entityDefToBuild != null && thing2.def.entityDefToBuild.passability != Traversability.Standable)
                    {
                        return false;
                    }
                    if (thing2.def.surfaceType == SurfaceType.None && thing2.def.passability != Traversability.Standable)
                    {
                        return false;
                    }
                }
                return true;
            }
            public static bool PawnCanAutomaticallyHaulFast(Pawn p, Thing t, bool forced)
            {
                UnfinishedThing unfinishedThing = t as UnfinishedThing;
                Building building;
                if (unfinishedThing != null && unfinishedThing.BoundBill != null
                    && ((building = (unfinishedThing.BoundBill.billStack.billGiver as Building)) == null
                    || (building.Spawned && building.OccupiedRect().ExpandedBy(1).Contains(unfinishedThing.Position))))
                {
                    return false;
                }
                if (!p.CanReserve(t, 1, -1, null, forced))
                {
                    return false;
                }
                if (!p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                {
                    return false;
                }
                if (t.def.IsNutritionGivingIngestible && t.def.ingestible.HumanEdible && !t.IsSociallyProper(p, forPrisoner: false, animalsCare: true))
                {
                    JobFailReason.Is("ReservedForPrisoners".Translate());
                    return false;
                }
                if (t.IsBurning())
                {
                    JobFailReason.Is("BurningLower".Translate());
                    return false;
                }
                return true;
            }

            public static bool HasHaulJobOnThing(WorkGiver_Scanner scanner, Pawn pawn, Thing t, JobTracker jobTracker, bool forced = false)
            {
                Map dest = null;
                if (scanner is WorkGiver_HaulGeneral)
                {
                    if (t is Corpse)
                    {
                        return false;
                    }
                }
                else if (scanner is WorkGiver_HaulCorpses)
                {
                    if (!(t is Corpse))
                    {
                        return false;
                    }
                }
                return HaulJobOnThing(pawn, t, jobTracker, forced) != null;
            }
            public static Job HaulJobOnThing(Pawn pawn, Thing t, JobTracker jobTracker, bool forced = false)
            {
                if (!PawnCanAutomaticallyHaulFast(pawn, t, forced))
                {
                    return null;
                }
                return HaulToStorageJob(pawn, t, jobTracker);
            }

            public static Job HaulToStorageJob(Pawn p, Thing t, JobTracker jobTracker)
            {
                StoragePriority currentPriority = StoreUtility.CurrentStoragePriorityOf(t);
                if (!TryFindBestBetterStorageFor(t, p, p.Map, currentPriority, p.Faction, out IntVec3 foundCell, out IHaulDestination haulDestination, jobTracker))
                {
                    JobFailReason.Is("NoEmptyPlaceLower".Translate());
                    return null;
                }
                if (haulDestination is ISlotGroupParent)
                {
                    return HaulToCellStorageJob(p, t, foundCell, fitInStoreCell: false);
                }
                Thing thing = haulDestination as Thing;
                if (thing != null && thing.TryGetInnerInteractableThingOwner() != null)
                {
                    return HaulToContainerJob(p, t, thing);
                }
                Log.Error("Don't know how to handle HaulToStorageJob for storage " + haulDestination.ToStringSafe() + ". thing=" + t.ToStringSafe());
                return null;
            }
            public static bool TryFindBestBetterStorageFor(Thing t, Pawn carrier, Map map, StoragePriority currentPriority, Faction faction, out IntVec3 foundCell, out IHaulDestination haulDestination, JobTracker jobTracker, bool needAccurateResult = true)
            {
                IntVec3 foundCell2 = IntVec3.Invalid;

                StoragePriority storagePriority = StoragePriority.Unstored;
                if (TryFindBestBetterStoreCellFor(t, carrier, map, currentPriority, faction, out foundCell2, jobTracker, needAccurateResult))
                {
                    storagePriority = foundCell2.GetSlotGroup(jobTracker.mapDest).Settings.Priority;
                }

                if (!TryFindBestBetterNonSlotGroupStorageFor(t, carrier, map, currentPriority, faction, out IHaulDestination haulDestination2, jobTracker))
                {
                    haulDestination2 = null;
                }
                if (storagePriority == StoragePriority.Unstored && haulDestination2 == null)
                {
                    foundCell = IntVec3.Invalid;
                    haulDestination = null;
                    return false;
                }
                if (haulDestination2 != null && (storagePriority == StoragePriority.Unstored || (int)haulDestination2.GetStoreSettings().Priority > (int)storagePriority))
                {
                    foundCell = IntVec3.Invalid;
                    haulDestination = haulDestination2;
                    return true;
                }
                foundCell = foundCell2;
                haulDestination = foundCell2.GetSlotGroup(jobTracker.mapDest).parent;
                return true;
            }

            public static bool TryFindBestBetterNonSlotGroupStorageFor(Thing t, Pawn carrier, Map map, StoragePriority currentPriority, Faction faction, out IHaulDestination haulDestination, JobTracker jobTracker, bool acceptSamePriority = false)
            {
                var ZTracker = ZUtils.ZTracker;
                List<IHaulDestination> allHaulDestinationsListInPriorityOrder = map.haulDestinationManager.AllHaulDestinationsListInPriorityOrder;
                List<IHaulDestination> allHaulDestinations = new List<IHaulDestination>();
                foreach (var map2 in ZTracker.GetAllMaps(map.Tile))
                {
                    allHaulDestinations.AddRange(map2.haulDestinationManager.AllHaulDestinationsListInPriorityOrder);
                }
                IntVec3 intVec = t.SpawnedOrAnyParentSpawned ? t.PositionHeld : carrier.PositionHeld;
                float num = float.MaxValue;
                StoragePriority storagePriority = StoragePriority.Unstored;
                haulDestination = null;
                foreach (var container in allHaulDestinations.OrderByDescending(x => x.GetStoreSettings().Priority))
                {
                    if (container is ISlotGroupParent)
                    {
                        continue;
                    }
                    StoragePriority priority = container.GetStoreSettings().Priority;
                    if ((int)priority < (int)storagePriority || (acceptSamePriority && (int)priority < (int)currentPriority) || (!acceptSamePriority && (int)priority <= (int)currentPriority))
                    {
                        break;
                    }
                    float num2 = intVec.DistanceToSquared(container.Position);
                    if (num2 > num || !container.Accepts(t))
                    {
                        continue;
                    }
                    //ZLogger.Message(container + " accepts " + t + " - " + (container.Accepts(t)).ToString());
                    Thing thing = container as Thing;
                    if (thing != null && thing.Faction != faction)
                    {
                        continue;
                    }
                    if (thing != null)
                    {
                        if (carrier != null)
                        {
                            if (thing.IsForbidden(carrier))
                            {
                                continue;
                            }
                        }
                        else if (faction != null && thing.IsForbidden(faction))
                        {
                            continue;
                        }
                    }
                    if (thing != null)
                    {
                        if (carrier != null)
                        {
                            if (!carrier.CanReserveNew(thing))
                            {
                                continue;
                            }
                        }
                        else if (faction != null && map.reservationManager.IsReservedByAnyoneOf(thing, faction))
                        {
                            continue;
                        }
                    }
                    if (carrier != null)
                    {
                        if (thing != null)
                        {
                            if (!carrier.Map.reachability.CanReach(intVec, thing, PathEndMode.ClosestTouch, TraverseParms.For(carrier)))
                            {
                                continue;
                            }
                        }
                        //else if (!carrier.Map.reachability.CanReach(intVec, container.Position, PathEndMode.ClosestTouch, TraverseParms.For(carrier)))
                        //{
                        //    continue;
                        //}
                    }
                    num = num2;
                    storagePriority = priority;
                    haulDestination = container;
                    jobTracker.mapDest = map;
                }
                return haulDestination != null;
            }

            public static Job HaulToCellStorageJob(Pawn p, Thing t, IntVec3 storeCell, bool fitInStoreCell)
            {
                Job job = JobMaker.MakeJob(JobDefOf.HaulToCell, t, storeCell);
                SlotGroup slotGroup = p.Map.haulDestinationManager.SlotGroupAt(storeCell);
                if (slotGroup != null)
                {
                    Thing thing = p.Map.thingGrid.ThingAt(storeCell, t.def);
                    if (thing != null)
                    {
                        job.count = t.def.stackLimit;
                        if (fitInStoreCell)
                        {
                            job.count -= thing.stackCount;
                        }
                    }
                    else
                    {
                        job.count = 99999;
                    }
                    int num = 0;
                    float statValue = p.GetStatValue(StatDefOf.CarryingCapacity);
                    List<IntVec3> cellsList = slotGroup.CellsList;
                    for (int i = 0; i < cellsList.Count; i++)
                    {
                        if (IsGoodStoreCell(cellsList[i], p.Map, t, p, p.Faction))
                        {
                            Thing thing2 = p.Map.thingGrid.ThingAt(cellsList[i], t.def);
                            num = ((thing2 == null || thing2 == t) ? (num + t.def.stackLimit) :
                                (num + Mathf.Max(t.def.stackLimit - thing2.stackCount, 0)));
                            if (num >= job.count || (float)num >= statValue)
                            {
                                break;
                            }
                        }
                    }
                    job.count = Mathf.Min(job.count, num);
                }
                else
                {
                    job.count = 99999;
                }
                job.haulOpportunisticDuplicates = true;
                job.haulMode = HaulMode.ToCellStorage;
                return job;
            }

            public static Job HaulToContainerJob(Pawn p, Thing t, Thing container)
            {
                ThingOwner thingOwner = container.TryGetInnerInteractableThingOwner();
                if (thingOwner == null)
                {
                    Log.Error(container.ToStringSafe() + " gave null ThingOwner.");
                    return null;
                }
                Job job = JobMaker.MakeJob(JobDefOf.HaulToContainer, t, container);
                job.count = Mathf.Min(t.stackCount, thingOwner.GetCountCanAccept(t));
                job.haulMode = HaulMode.ToContainer;
                return job;
            }

            public static Job RefuelJobOnThing(WorkGiver_Refuel scanner, Pawn pawn, Thing t, bool forced = false)
            {
                var ZTracker = ZUtils.ZTracker;
                Job job = null;
                var JobStandard = scanner.JobStandard;
                var JobAtomic = scanner.JobAtomic;
                Map oldPawnMap = pawn.Map;
                IntVec3 oldPawnPosition = pawn.Position;
                Map oldThingMap = t.Map;
                IntVec3 oldThingPosition = t.Position;

                foreach (var otherMap in ZTracker.GetAllMapsInClosestOrder(oldThingMap))
                {
                    if (oldThingMap != otherMap)
                    {
                        t.positionInt = ZUtils.GetCellToTeleportFrom(t.Map, t.Position, otherMap);
                        pawn.positionInt = t.positionInt;
                    }
                    else if (t.Position != oldThingPosition)
                    {
                        t.positionInt = oldThingPosition;
                        pawn.positionInt = oldPawnPosition;
                    }

                    t.mapIndexOrState = (sbyte)Find.Maps.IndexOf(otherMap);
                    pawn.mapIndexOrState = (sbyte)Find.Maps.IndexOf(otherMap);

                    if (scanner.HasJobOnThing(pawn, t, forced))
                    {
                        job = RefuelWorkGiverUtility.RefuelJob(pawn, t, forced, JobStandard, JobAtomic);
                    }
                    if (job != null)
                    {
                        job.count = job.GetTarget(TargetIndex.A).Thing.TryGetComp<CompRefuelable>().GetFuelCountToFullyRefuel();
                    }
                    if (job != null) break;
                }
                ZUtils.TeleportThing(pawn, oldPawnMap, oldPawnPosition);
                ZUtils.TeleportThing(t, oldThingMap, oldThingPosition);
                return job;
            }

            public static bool HasJobOnThingRescue(WorkGiver_RescueDowned scanner, Pawn pawn, Thing t, bool forced = false)
            {
                Pawn pawn2 = t as Pawn;
                if (pawn2 == null || !pawn2.Downed || pawn2.Faction != pawn.Faction || pawn2.InBed() || !pawn.CanReserve(pawn2, 1, -1, null, forced) || GenAI.EnemyIsNear(pawn2, 40f))
                {
                    return false;
                }

                var oldMap1 = pawn.Map;
                var oldMap2 = pawn2.Map;
                var oldPosition1 = pawn.Position;
                var oldPosition2 = pawn2.Position;
                bool select = false;
                if (Find.Selector.SelectedObjects.Contains(pawn)) select = true;
                Thing thing = null;

                foreach (var otherMap in ZUtils.GetAllMapsInClosestOrderForTwoThings(pawn, oldMap1, oldPosition1,
                    pawn2, oldMap2, oldPosition2))
                {
                    thing = scanner.FindBed(pawn, pawn2);
                    if (thing != null) break;
                }

                if (select) Find.Selector.Select(pawn);
                ZUtils.TeleportThing(pawn, oldMap1, oldPosition1);
                ZUtils.TeleportThing(pawn2, oldMap2, oldPosition2);

                Log.Message("HasJobOnThingRescue: " + thing + "pawn2.CanReserve(thing): " + pawn2.CanReserve(thing));
                if (thing != null && pawn2.CanReserve(thing))
                {
                    return true;
                }

                return false;
            }

            public static Job TendJobOnThing(Pawn pawn, Thing t, bool forced = false)
            {
                Pawn pawn2 = t as Pawn;
                var oldMap1 = pawn.Map;
                var oldMap2 = pawn2.Map;
                var oldPosition1 = pawn.Position;
                var oldPosition2 = pawn2.Position;
                bool select = false;
                if (Find.Selector.SelectedObjects.Contains(pawn)) select = true;
                Thing thing = null;

                foreach (var otherMap in ZUtils.GetAllMapsInClosestOrderForTwoThings(pawn, oldMap1, oldPosition1,
                    pawn2, oldMap2, oldPosition2))
                {
                    thing = HealthAIUtility.FindBestMedicine(pawn, pawn2);
                    if (thing != null) break;
                }

                if (select) Find.Selector.Select(pawn);
                ZUtils.TeleportThing(pawn, oldMap1, oldPosition1);
                ZUtils.TeleportThing(pawn2, oldMap2, oldPosition2);

                if (thing != null)
                {
                    return JobMaker.MakeJob(JobDefOf.TendPatient, pawn2, thing);
                }
                return JobMaker.MakeJob(JobDefOf.TendPatient, pawn2);
            }

            public static ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams, JobGiver_Work instance, bool emergency, JobTracker jobTracker, Map oldMap, IntVec3 oldPosition)
            {
                if (emergency && pawn.mindState.priorityWork.IsPrioritized)
                {
                    List<WorkGiverDef> workGiversByPriority =
                        pawn.mindState.priorityWork.WorkGiver.workType.workGiversByPriority;
                    for (int i = 0; i < workGiversByPriority.Count; i++)
                    {
                        WorkGiver worker = workGiversByPriority[i].Worker;
                        if (WorkGiversRelated(pawn.mindState.priorityWork.WorkGiver, worker.def))
                        {
                            Job job = GiverTryGiveJobPrioritized(pawn, worker, pawn.mindState.priorityWork.Cell);
                            if (job != null)
                            {
                                job.playerForced = true;
                                return new ThinkResult(job, instance, workGiversByPriority[i].tagToGive);
                            }
                        }
                    }
                    pawn.mindState.priorityWork.Clear();
                }
                List<WorkGiver> list = (!emergency) ? pawn.workSettings.WorkGiversInOrderNormal : pawn.workSettings.WorkGiversInOrderEmergency;
                ZLogger.Message(pawn + " - " + emergency + " - workgiver count: " + list.Count, debugLevel: DebugLevel.Jobs);
                int num = -999;
                TargetInfo bestTargetOfLastPriority = TargetInfo.Invalid;
                WorkGiver_Scanner scannerWhoProvidedTarget = null;
                WorkGiver_Scanner scanner;
                IntVec3 pawnPosition;
                float closestDistSquared;
                float bestPriority;
                bool prioritized;
                bool allowUnreachable;
                Danger maxPathDanger;
                var ZTracker = ZUtils.ZTracker;
                for (int j = 0; j < list.Count; j++)
                {
                    WorkGiver workGiver = list[j];
                    foreach (var otherMap in ZUtils.GetAllMapsInClosestOrder(pawn, oldMap, oldPosition))
                    {
                        jobTracker.mapDest = otherMap;
                        ZLogger.Message("Workgiver N" + (j + 1) + " from " + list.Count + " - " + pawn + " search job - " + workGiver + " in " + ZUtils.ZTracker.GetMapInfo(otherMap), debugLevel: DebugLevel.Jobs);
                        if (workGiver.def.priorityInType != num && bestTargetOfLastPriority.IsValid)
                        {
                            ZLogger.Message("Breaking the workgiver loop due to workGiver.def.priorityInType != num && bestTargetOfLastPriority.IsValid", debugLevel: DebugLevel.Jobs);
                            break;
                        }
                        if (!PawnCanUseWorkGiver(pawn, workGiver))
                        {
                            //ZLogger.Message(pawn + " CantUseWorkGiver in " + ZTracker.GetMapInfo(otherMap) + " - " + workGiver);
                            continue;
                        }
                        try
                        {

                            Job job2 = workGiver.NonScanJob(pawn);
                            if (job2 != null)
                            {
                                return new ThinkResult(job2, instance, list[j].def.tagToGive);
                            }

                            scanner = (workGiver as WorkGiver_Scanner);
                            if (scanner != null)
                            {
                                if (scanner.def.scanThings)
                                {
                                    Predicate<Thing> validator = (Thing t) => !t.IsForbidden(pawn) && scanner.HasJobOnThing(pawn, t);

                                    Predicate<Thing> deliverResourcesValidator = (Thing t) => !t.IsForbidden(pawn) && ConstructDeliverJobOnThing((WorkGiver_ConstructDeliverResourcesToBlueprints)scanner, pawn, t) != null;

                                    Predicate<Thing> deliverResourcesValidator2 = (Thing t) => !t.IsForbidden(pawn) && ConstructDeliverJobOnThing((WorkGiver_ConstructDeliverResourcesToFrames)scanner, pawn, t) != null;

                                    Predicate<Thing> refuelValidator = (Thing t) => !t.IsForbidden(pawn) && RefuelJobOnThing((WorkGiver_Refuel)scanner, pawn, t) != null;

                                    Predicate<Thing> rescueValidator = (Thing t) => !t.IsForbidden(pawn) && HasJobOnThingRescue((WorkGiver_RescueDowned)scanner, pawn, t);

                                    Predicate<Thing> billValidator = (Thing t) => !t.IsForbidden(pawn) && DoBillJobOnThing((WorkGiver_DoBill)scanner, pawn, t, jobTracker) != null;

                                    Predicate<Thing> haulingValidator = (Thing t) => !t.IsForbidden(pawn) && HaulJobOnThing(pawn, t, jobTracker, false) != null;

                                    IEnumerable<Thing> enumerable = scanner.PotentialWorkThingsGlobal(pawn);

                                    Thing thing = null;
                                    if (scanner.Prioritized)
                                    {
                                        IEnumerable<Thing> enumerable2 = enumerable;
                                        if (enumerable2 == null)
                                        {
                                            enumerable2 = pawn.Map.listerThings.ThingsMatching(scanner.PotentialWorkThingRequest);
                                        }
                                        thing = ((!scanner.AllowUnreachable) ? GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, enumerable2, scanner.PathEndMode, TraverseParms.For(pawn, scanner.MaxPathDanger(pawn)), 9999f, validator, (Thing x) => scanner.GetPriority(pawn, x)) : GenClosest.ClosestThing_Global(pawn.Position, enumerable2, 99999f, validator, (Thing x) => scanner.GetPriority(pawn, x)));
                                    }
                                    else if (scanner.AllowUnreachable)
                                    {
                                        IEnumerable<Thing> enumerable3 = enumerable;
                                        if (enumerable3 == null)
                                        {
                                            enumerable3 = pawn.Map.listerThings.ThingsMatching(scanner.PotentialWorkThingRequest);
                                        }
                                        thing = GenClosest.ClosestThing_Global(pawn.Position, enumerable3, 99999f, validator);
                                    }
                                    else
                                    {
                                        if (scanner.def.defName == "HaulGeneral" || scanner.def.defName == "HaulCorpses")
                                        {
                                            thing = GenClosest.ClosestThingReachable
                                                (pawn.Position, pawn.Map, scanner.PotentialWorkThingRequest,
                                                scanner.PathEndMode, TraverseParms.For(pawn, scanner.MaxPathDanger(pawn)),
                                                9999f, haulingValidator, enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch,
                                                enumerable != null);
                                        }

                                        else if (scanner is WorkGiver_DoBill)
                                        {
                                            thing = GenClosest.ClosestThingReachable
                                                (pawn.Position, pawn.Map, scanner.PotentialWorkThingRequest,
                                                scanner.PathEndMode, TraverseParms.For(pawn,
                                                scanner.MaxPathDanger(pawn)), 9999f, billValidator,
                                                enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch,
                                                enumerable != null);
                                        }

                                        else if (scanner is WorkGiver_ConstructDeliverResourcesToBlueprints)
                                        {
                                            thing = GenClosest.ClosestThingReachable
                                                (pawn.Position, pawn.Map, scanner.PotentialWorkThingRequest,
                                                scanner.PathEndMode, TraverseParms.For(pawn,
                                                scanner.MaxPathDanger(pawn)), 9999f, deliverResourcesValidator,
                                                enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch,
                                                enumerable != null);
                                        }

                                        else if (scanner is WorkGiver_ConstructDeliverResourcesToFrames)
                                        {
                                            thing = GenClosest.ClosestThingReachable
                                                (pawn.Position, pawn.Map, scanner.PotentialWorkThingRequest,
                                                scanner.PathEndMode, TraverseParms.For(pawn,
                                                scanner.MaxPathDanger(pawn)), 9999f, deliverResourcesValidator2,
                                                enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch,
                                                enumerable != null);
                                        }

                                        else if (scanner is WorkGiver_Refuel)
                                        {
                                            thing = GenClosest.ClosestThingReachable
                                                (pawn.Position, pawn.Map, scanner.PotentialWorkThingRequest,
                                                scanner.PathEndMode, TraverseParms.For(pawn,
                                                scanner.MaxPathDanger(pawn)), 9999f, refuelValidator,
                                                enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch,
                                                enumerable != null);
                                        }

                                        else if (scanner is WorkGiver_RescueDowned)
                                        {
                                            thing = GenClosest.ClosestThingReachable
                                                (pawn.Position, pawn.Map, scanner.PotentialWorkThingRequest,
                                                scanner.PathEndMode, TraverseParms.For(pawn,
                                                scanner.MaxPathDanger(pawn)), 9999f, rescueValidator,
                                                enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch,
                                                enumerable != null);
                                            Log.Message($"Thing: {thing}, pawn.Map: {pawn.Map}, dest: {jobTracker.mapDest}, ");
                                        }
                                        else
                                        {
                                            thing = GenClosest.ClosestThingReachable
                                                (pawn.Position, pawn.Map, scanner.PotentialWorkThingRequest,
                                                scanner.PathEndMode, TraverseParms.For(pawn,
                                                scanner.MaxPathDanger(pawn)), 9999f, validator,
                                                enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch,
                                                enumerable != null);
                                        }

                                        if (thing != null)
                                        {
                                            ZLogger.Message(pawn + " - " + ZTracker.GetMapInfo(pawn.Map) + " - " + scanner + " Selected thing: " + thing, debugLevel: DebugLevel.Jobs);
                                        }
                                    }
                                    if (thing != null)
                                    {
                                        bestTargetOfLastPriority = thing;
                                        scannerWhoProvidedTarget = scanner;
                                    }
                                }
                                if (scanner.def.scanCells)
                                {
                                    pawnPosition = pawn.Position;
                                    closestDistSquared = 99999f;
                                    bestPriority = float.MinValue;
                                    prioritized = scanner.Prioritized;
                                    allowUnreachable = scanner.AllowUnreachable;
                                    maxPathDanger = scanner.MaxPathDanger(pawn);
                                    IEnumerable<IntVec3> enumerable4 = scanner.PotentialWorkCellsGlobal(pawn);
                                    IList<IntVec3> list2;
                                    if ((list2 = (enumerable4 as IList<IntVec3>)) != null)
                                    {
                                        for (int k = 0; k < list2.Count; k++)
                                        {
                                            ProcessCell(list2[k]);
                                        }
                                    }
                                    else
                                    {
                                        foreach (IntVec3 item in enumerable4)
                                        {
                                            ProcessCell(item);
                                        }
                                    }
                                }
                            }

                            void ProcessCell(IntVec3 c)
                            {
                                bool flag = false;
                                float num2 = (c - pawnPosition).LengthHorizontalSquared;
                                float num3 = 0f;
                                if (prioritized)
                                {
                                    if (!c.IsForbidden(pawn) && scanner.HasJobOnCell(pawn, c))
                                    {
                                        if (!allowUnreachable && !pawn.CanReach(c, scanner.PathEndMode, maxPathDanger))
                                        {
                                            return;
                                        }
                                        num3 = scanner.GetPriority(pawn, c);
                                        if (num3 > bestPriority || (num3 == bestPriority && num2 < closestDistSquared))
                                        {
                                            flag = true;
                                        }
                                    }
                                }
                                else if (num2 < closestDistSquared && !c.IsForbidden(pawn) && scanner.HasJobOnCell(pawn, c))
                                {
                                    if (!allowUnreachable && !pawn.CanReach(c, scanner.PathEndMode, maxPathDanger))
                                    {
                                        return;
                                    }
                                    flag = true;
                                }
                                if (flag)
                                {
                                    bestTargetOfLastPriority = new TargetInfo(c, pawn.Map);
                                    scannerWhoProvidedTarget = scanner;
                                    closestDistSquared = num2;
                                    bestPriority = num3;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(string.Concat(pawn, " threw exception in WorkGiver ", workGiver.def.defName, ": ", ex.ToString()));
                        }
                        finally
                        {

                        }
                        if (bestTargetOfLastPriority.IsValid)
                        {
                            Job job3 = null;
                            try
                            {
                                if (scannerWhoProvidedTarget.def.defName == "HaulGeneral" || scannerWhoProvidedTarget.def.defName == "HaulCorpses")
                                {
                                    job3 = (!bestTargetOfLastPriority.HasThing) ?
                                        scannerWhoProvidedTarget.JobOnCell(pawn, bestTargetOfLastPriority.Cell)
                                        : HaulJobOnThing(pawn, bestTargetOfLastPriority.Thing, jobTracker);
                                    ZLogger.Message("1 - " + scannerWhoProvidedTarget + " - " + job3, debugLevel: DebugLevel.Jobs);
                                }
                                else if (scannerWhoProvidedTarget is WorkGiver_DoBill scanner1)
                                {
                                    job3 = (!bestTargetOfLastPriority.HasThing) ?
                                        scannerWhoProvidedTarget.JobOnCell(pawn, bestTargetOfLastPriority.Cell)
                                        : DoBillJobOnThing(scanner1, pawn, bestTargetOfLastPriority.Thing, jobTracker);
                                    ZLogger.Message("2 - " + scannerWhoProvidedTarget + " - " + job3, debugLevel: DebugLevel.Jobs);
                                }
                                else if (scannerWhoProvidedTarget is WorkGiver_ConstructDeliverResourcesToBlueprints scanner2)
                                {
                                    job3 = (!bestTargetOfLastPriority.HasThing) ?
                                        scannerWhoProvidedTarget.JobOnCell(pawn, bestTargetOfLastPriority.Cell)
                                        : ConstructDeliverJobOnThing(scanner2, pawn, bestTargetOfLastPriority.Thing);
                                    ZLogger.Message("3 - " + scannerWhoProvidedTarget + " - " + job3, debugLevel: DebugLevel.Jobs);
                                }
                                else if (scannerWhoProvidedTarget is WorkGiver_ConstructDeliverResourcesToFrames scanner3)
                                {
                                    job3 = (!bestTargetOfLastPriority.HasThing) ?
                                        scannerWhoProvidedTarget.JobOnCell(pawn, bestTargetOfLastPriority.Cell)
                                        : ConstructDeliverJobOnThing(scanner3, pawn, bestTargetOfLastPriority.Thing);
                                    ZLogger.Message("4 - " + scannerWhoProvidedTarget + " - " + job3, debugLevel: DebugLevel.Jobs);
                                }
                                else if (scannerWhoProvidedTarget is WorkGiver_Refuel scanner4)
                                {
                                    job3 = (!bestTargetOfLastPriority.HasThing) ?
                                        scannerWhoProvidedTarget.JobOnCell(pawn, bestTargetOfLastPriority.Cell)
                                        : RefuelJobOnThing(scanner4, pawn, bestTargetOfLastPriority.Thing);
                                    ZLogger.Message("5 - " + scannerWhoProvidedTarget + " - " + job3, debugLevel: DebugLevel.Jobs);
                                }
                                else if (scannerWhoProvidedTarget is WorkGiver_Tend)
                                {
                                    job3 = (!bestTargetOfLastPriority.HasThing) ?
                                        scannerWhoProvidedTarget.JobOnCell(pawn, bestTargetOfLastPriority.Cell)
                                        : TendJobOnThing(pawn, bestTargetOfLastPriority.Thing);
                                    ZLogger.Message("6 - " + scannerWhoProvidedTarget + " - " + job3, debugLevel: DebugLevel.Jobs);
                                }
                                else
                                {
                                    job3 = (!bestTargetOfLastPriority.HasThing) ?
                                        scannerWhoProvidedTarget.JobOnCell(pawn, bestTargetOfLastPriority.Cell)
                                        : scannerWhoProvidedTarget.JobOnThing(pawn, bestTargetOfLastPriority.Thing);
                                    ZLogger.Message("7 - " + scannerWhoProvidedTarget + " - " + job3, debugLevel: DebugLevel.Jobs);
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex.ToString());
                            }
                            if (job3 != null)
                            {
                                job3.workGiverDef = scannerWhoProvidedTarget.def;
                                if (jobTracker.mapDest == null)
                                {
                                    jobTracker.mapDest = otherMap;
                                }
                                return new ThinkResult(job3, instance, list[j].def.tagToGive);
                            }
                            else
                            {
                                jobTracker.mapDest = null;
                            }
                            //Log.ErrorOnce(string.Concat(scannerWhoProvidedTarget, " provided target ", bestTargetOfLastPriority, " but yielded no actual job for pawn ", pawn, ". The CanGiveJob and JobOnX methods may not be synchronized."), 6112651);
                        }
                        num = workGiver.def.priorityInType;
                    }
                }
                return ThinkResult.NoJob;
            }

            private static bool PawnCanUseWorkGiver(Pawn pawn, WorkGiver giver)
            {
                if (!giver.def.nonColonistsCanDo && !pawn.IsColonist)
                {
                    //ZLogger.Message(pawn + " cannot use " + giver + " - reason: !giver.def.nonColonistsCanDo && !pawn.IsColonist");
                    return false;
                }
                if (pawn.WorkTagIsDisabled(giver.def.workTags))
                {
                    //ZLogger.Message(pawn + " cannot use " + giver + " - reason: pawn.WorkTagIsDisabled(giver.def.workTags)");
                    return false;
                }
                if (giver.def.defName == "HaulGeneral" || giver.def.defName == "HaulCorpses")
                {

                }
                else if (giver.ShouldSkip(pawn))
                {
                    //ZLogger.Message(pawn + " cannot use " + giver + " - reason: giver.ShouldSkip(pawn)");
                    return false;
                }
                if (giver.MissingRequiredCapacity(pawn) != null)
                {
                    //ZLogger.Message(pawn + " cannot use " + giver + " - reason: giver.MissingRequiredCapacity(pawn) != null");
                    return false;
                }
                //ZLogger.Message(pawn + " can use " + giver);
                return true;
            }

            private static bool WorkGiversRelated(WorkGiverDef current, WorkGiverDef next)
            {
                if (next == WorkGiverDefOf.Repair)
                {
                    return current == WorkGiverDefOf.Repair;
                }
                return true;
            }

            private static Job GiverTryGiveJobPrioritized(Pawn pawn, WorkGiver giver, IntVec3 cell)
            {
                if (!PawnCanUseWorkGiver(pawn, giver))
                {
                    return null;
                }
                try
                {
                    Job job = giver.NonScanJob(pawn);
                    if (job != null)
                    {
                        return job;
                    }
                    WorkGiver_Scanner scanner = giver as WorkGiver_Scanner;
                    if (scanner != null)
                    {
                        if (giver.def.scanThings)
                        {
                            Predicate<Thing> predicate = (Thing t) => !t.IsForbidden(pawn) &&
                            scanner.HasJobOnThing(pawn, t);
                            List<Thing> thingList = cell.GetThingList(pawn.Map);
                            for (int i = 0; i < thingList.Count; i++)
                            {
                                Thing thing = thingList[i];
                                if (scanner.PotentialWorkThingRequest.Accepts(thing) && predicate(thing))
                                {
                                    Job job2 = scanner.JobOnThing(pawn, thing);
                                    if (job2 != null)
                                    {
                                        job2.workGiverDef = giver.def;
                                    }
                                    return job2;
                                }
                            }
                        }
                        if (giver.def.scanCells && !cell.IsForbidden(pawn) && scanner.HasJobOnCell(pawn, cell))
                        {
                            Job job3 = scanner.JobOnCell(pawn, cell);
                            if (job3 != null)
                            {
                                job3.workGiverDef = giver.def;
                            }
                            return job3;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(string.Concat(pawn, " threw exception in GiverTryGiveJobTargeted on WorkGiver ", giver.def.defName, ": ", ex.ToString()));
                }
                return null;
            }
        }
    }
}