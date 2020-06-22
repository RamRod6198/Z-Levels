using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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
    public static class JobManagerPatches
    {
        public static bool manualDespawn = false;

        public static bool searchJobs = false;


        //static JobManagerPatches()
        //{
        //    MethodInfo method = typeof(JobManagerPatches).GetMethod("LogScanner");
        //    //MethodInfo method2 = typeof(JobManagerPatches).GetMethod("LogScanner2");
        //    foreach (Type type in GenTypes.AllSubclasses(typeof(ThinkNode_JobGiver)))
        //    {
        //        try
        //        {
        //            new Harmony("test.test.tst").Patch(type.GetMethod("TryGiveJob", BindingFlags.Instance | BindingFlags.NonPublic
        //                | BindingFlags.GetField), null, new HarmonyMethod(method), null);
        //            ZLogger.Message("Patch: " + type);
        //        }
        //
        //        catch (Exception ex)
        //        {
        //            ZLogger.Message("Error patching: " + ex);
        //        }
        //    }
        //
        //}
        //
        //public static void LogScanner(ThinkNode_JobGiver __instance, Job __result, Pawn pawn)
        //{
        //    //if (__result != null && pawn.def.race.Humanlike)
        //    //{
        //    //    ZLogger.Message(__instance + " - " + __result + " - " + pawn, true);
        //    //}
        //    if (pawn.Faction == Faction.OfPlayer && !pawn.def.race.Humanlike)
        //    {
        //        Log.Message(__instance + " - " + __result + " - " + pawn, true);
        //    }
        //}
        //
        //[HarmonyPatch(typeof(JobQueue), "EnqueueFirst")]
        //internal static class Patch_JobQueue
        //{
        //    private static void Postfix(Job j, JobTag? tag = null)
        //    {
        //        ZLogger.Message("Switching to " + j, true);
        //    }
        //}
        //
        //[HarmonyPatch(typeof(JobQueue), "EnqueueLast")]
        //internal static class Patch_JobQueue2
        //{
        //    private static void Postfix(Job j, JobTag? tag = null)
        //    {
        //        ZLogger.Message("Switching to " + j, true);
        //    }
        //}

        //
        //[HarmonyPatch(typeof(JobGiver_Work), "GiverTryGiveJobPrioritized")]
        //internal static class Patch_JobGiver_Work
        //{
        //    private static void Postfix(Pawn pawn, WorkGiver giver, IntVec3 cell)
        //    {
        //        ZLogger.Message("Switching to " + pawn, true);
        //    }
        //}

        //[HarmonyPatch(typeof(JobGiver_AIFollowMaster), "TryGiveJob")]
        //public class JobGiver_AIFollowMasterPatch
        //{
        //    [HarmonyPostfix]
        //    private static void JobGiver_AIFollowMasterPostfix(JobGiver_AIFollowMaster __instance, ref Job __result, Pawn pawn)
        //    {
        //        try
        //        {
        //            Log.Message("JobGiver_AIFollowPawnPostfix: " + __result + " - " + pawn, true);
        //            var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
        //            if (__result == null && ZTracker?.ZLevelsTracker[pawn.Map.Tile]?.ZLevels?.Count > 1)
        //            {
        //                if (ZTracker.jobTracker == null)
        //                {
        //                    ZTracker.jobTracker = new Dictionary<Pawn, JobTracker>();
        //                }
        //                if (ZTracker.jobTracker.ContainsKey(pawn))
        //                {
        //                    try
        //                    {
        //                        if (ZTracker.jobTracker[pawn]?.activeJobs?.Count() > 0)
        //                        {
        //                            if (!pawn.jobs.jobQueue.Contains(ZTracker.jobTracker[pawn].activeJobs[0]))
        //                            {
        //                                ZLogger.Message("Queue: " + ZTracker.jobTracker[pawn].activeJobs[0]);
        //                                pawn.jobs.jobQueue.EnqueueLast(ZTracker.jobTracker[pawn].activeJobs[0]);
        //                                return;
        //                            }
        //                            else if (pawn.jobs.jobQueue[0].job.targetA.Thing.Map != pawn.Map)
        //                            {
        //                                ZTracker.ResetJobTrackerFor(pawn);
        //                            }
        //                            else
        //                            {
        //                                return;
        //                            }
        //                        }
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        ZLogger.Message("Error2: " + ex);
        //                    };
        //                }
        //                else
        //                {
        //                    ZTracker.jobTracker[pawn] = new JobTracker();
        //                }
        //                if (Find.TickManager.TicksGame - ZTracker.jobTracker[pawn].lastTick < 200)
        //                // minimal job interval check per pawn is 200 ticks
        //                {
        //                    return;
        //                }
        //                Job result = null;
        //                var oldMap = pawn.Map;
        //                var oldPosition = pawn.Position;
        //                bool select = false;
        //
        //                foreach (var otherMap in ZTracker.ZLevelsTracker[pawn.Map.Tile].ZLevels.Values)
        //                {
        //                    if (otherMap != oldMap)
        //                    {
        //                        if (Find.TickManager.TicksGame - ZTracker.jobTracker[pawn].lastTick < 200)
        //                        {
        //                            return;
        //                        }
        //                        ZLogger.Message("Searching figth job for " + pawn);
        //                        var stairs = new List<Thing>();
        //
        //                        ZLogger.Message("Searching job for " + pawn + " in " + ZTracker.GetMapInfo(otherMap)
        //                            + " for " + ZTracker.GetMapInfo(oldMap));
        //
        //                        if (ZTracker.GetZIndexFor(otherMap) > ZTracker.GetZIndexFor(oldMap))
        //                        {
        //                            Map lowerMap = ZTracker.GetLowerLevel(otherMap.Tile, otherMap);
        //                            if (lowerMap != null)
        //                            {
        //                                ZLogger.Message("Searching stairs up in " + ZTracker.GetMapInfo(otherMap));
        //                                stairs = lowerMap.listerThings.AllThings.Where(x => x is Building_StairsUp).ToList();
        //                            }
        //                            else
        //                            {
        //                                ZLogger.Message("Lower map is null in " + ZTracker.GetMapInfo(otherMap));
        //                            }
        //                        }
        //                        else if (ZTracker.GetZIndexFor(otherMap) < ZTracker.GetZIndexFor(oldMap))
        //                        {
        //                            Map upperMap = ZTracker.GetUpperLevel(otherMap.Tile, otherMap);
        //                            if (upperMap != null)
        //                            {
        //                                ZLogger.Message("Searching stairs down in " + ZTracker.GetMapInfo(otherMap));
        //                                stairs = upperMap.listerThings.AllThings.Where(x => x is Building_StairsDown).ToList();
        //                            }
        //                            else
        //                            {
        //                                ZLogger.Message("Upper map is null in " + ZTracker.GetMapInfo(otherMap));
        //                            }
        //                        }
        //                        if (Find.TickManager.TicksGame - ZTracker.jobTracker[pawn].lastTick < 200)
        //                        // minimal job interval check per pawn is 200 ticks
        //                        {
        //                            return;
        //                        }
        //
        //                        if (stairs != null && stairs.Count() > 0)
        //                        {
        //                            var selectedStairs = GenClosest.ClosestThing_Global(pawn.Position, stairs, 99999f);
        //                            var position = selectedStairs.Position;
        //
        //                            if (Find.Selector.SelectedObjects.Contains(pawn)) select = true;
        //                            JobManagerPatches.manualDespawn = true;
        //                            pawn.DeSpawn();
        //                            JobManagerPatches.manualDespawn = false;
        //                            GenPlace.TryPlaceThing(pawn, position, otherMap, ThingPlaceMode.Direct);
        //
        //                            result = JobGiver_AIFollowMasterPatch.TryGiveJob(pawn, __instance);
        //
        //                            if (result != null)
        //                            {
        //                                ZLogger.Message(pawn + " gets food job " + result);
        //                                ZLogger.Message("TryIssueJobPackage: " + pawn + " - map: " + ZTracker.GetMapInfo(pawn.Map)
        //                                    + " - " + pawn.Position + " result " + result);
        //                                JobManagerPatches.manualDespawn = true;
        //                                pawn.DeSpawn();
        //                                JobManagerPatches.manualDespawn = false;
        //                                GenPlace.TryPlaceThing(pawn, oldPosition, oldMap, ThingPlaceMode.Direct);
        //                                if (select) Find.Selector.Select(pawn);
        //                                ZTracker.BuildJobListFor(pawn, oldMap, otherMap, result, null);
        //                                break;
        //                            }
        //                        }
        //                    }
        //                }
        //                if (pawn.Map != oldMap)
        //                {
        //                    JobManagerPatches.manualDespawn = true;
        //                    pawn.DeSpawn();
        //                    JobManagerPatches.manualDespawn = false;
        //                    GenPlace.TryPlaceThing(pawn, oldPosition, oldMap, ThingPlaceMode.Direct);
        //                    if (select) Find.Selector.Select(pawn);
        //                }
        //                try
        //                {
        //                    ZTracker.jobTracker[pawn].lastTick = Find.TickManager.TicksGame;
        //                }
        //                catch { }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Log.Error("Some kind of error occurred in Z-Levels JobManager: " + ex);
        //        }
        //    }
        //
        //    public static Job TryGiveJob(Pawn pawn, JobGiver_AIFollowMaster __instance)
        //    {
        //        Pawn followee = Traverse.Create(__instance).Method("GetFollowee", new object[]
        //                            {
        //                                pawn
        //                            }).GetValue<Pawn>();
        //        if (followee == null)
        //        {
        //            return null;
        //        }
        //        if (!followee.Spawned || !pawn.CanReach(followee, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn))
        //        {
        //            return null;
        //        }
        //
        //        float radius = Traverse.Create(__instance).Method("GetRadius", new object[]
        //                        {
        //                            pawn
        //                        }).GetValue<float>();
        //        if (!JobDriver_FollowClose.FarEnoughAndPossibleToStartJob(pawn, followee, radius))
        //        {
        //            return null;
        //        }
        //        Job job = JobMaker.MakeJob(JobDefOf.FollowClose, followee);
        //        job.expiryInterval = Traverse.Create(__instance).Field("FollowJobExpireInterval").GetValue<int>(); ;
        //        job.checkOverrideOnExpire = true;
        //        job.followRadius = radius;
        //        return job;
        //    }
        //}

        //[HarmonyPatch(typeof(JobGiver_ConfigurableHostilityResponse), "TryGiveJob")]
        //public class JobGiver_ConfigurableHostilityResponsePatch
        //{
        //    [HarmonyPostfix]
        //    private static void JobGiver_ConfigurableHostilityResponsePostfix(JobGiver_ConfigurableHostilityResponse __instance, ref Job __result, Pawn pawn)
        //    {
        //        try
        //        {
        //            var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
        //            if (pawn.def.race.Humanlike && pawn.Faction != Faction.OfPlayer && __result == null && ZTracker?.ZLevelsTracker[pawn.Map.Tile]?.ZLevels?.Count > 1)
        //            {
        //                if (ZTracker.jobTracker == null)
        //                {
        //                    ZTracker.jobTracker = new Dictionary<Pawn, JobTracker>();
        //                }
        //                if (ZTracker.jobTracker.ContainsKey(pawn))
        //                {
        //                    try
        //                    {
        //                        if (ZTracker.jobTracker[pawn]?.activeJobs?.Count() > 0)
        //                        {
        //                            if (!pawn.jobs.jobQueue.Contains(ZTracker.jobTracker[pawn].activeJobs[0]))
        //                            {
        //                                ZLogger.Message("Queue: " + ZTracker.jobTracker[pawn].activeJobs[0]);
        //                                pawn.jobs.jobQueue.EnqueueLast(ZTracker.jobTracker[pawn].activeJobs[0]);
        //                                return;
        //                            }
        //                            else if (pawn.jobs.jobQueue[0].job.targetA.Thing.Map != pawn.Map)
        //                            {
        //                                ZTracker.ResetJobTrackerFor(pawn);
        //                            }
        //                            else
        //                            {
        //                                return;
        //                            }
        //                        }
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        ZLogger.Message("Error2: " + ex);
        //                    };
        //                }
        //                else
        //                {
        //                    ZTracker.jobTracker[pawn] = new JobTracker();
        //                }
        //                if (Find.TickManager.TicksGame - ZTracker.jobTracker[pawn].lastTick < 200)
        //                // minimal job interval check per pawn is 200 ticks
        //                {
        //                    return;
        //                }
        //
        //                Map playerMap = ZTracker.GetMapByIndex(pawn.Map.Tile, 0);
        //                if (pawn.Map != playerMap)
        //                {
        //                    ZTracker.BuildJobListFor(pawn, pawn.Map, playerMap, JobMaker.MakeJob(JobDefOf.Wait_Combat, JobGiver_AIFightEnemy.ExpiryInterval_ShooterSucceeded.RandomInRange, true), null);
        //                    try
        //                    {
        //                        ZLogger.Message("3 lastTick");
        //                        ZTracker.jobTracker[pawn].lastTick = Find.TickManager.TicksGame;
        //                    }
        //                    catch { }
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Log.Error("Some kind of error occurred in Z-Levels JobManager: " + ex);
        //        }
        //    }
        //}
        //
        //[HarmonyPatch(typeof(JobGiver_AIGotoNearestHostile), "TryGiveJob")]
        //public class JobGiver_AIGotoNearestHostilePatch
        //{
        //    [HarmonyPostfix]
        //    private static void JobGiver_AIGotoNearestHostilePostfix(JobGiver_AIGotoNearestHostile __instance, ref Job __result, Pawn pawn)
        //    {
        //        try
        //        {
        //            var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
        //            if (pawn.def.race.Humanlike && pawn.Faction != Faction.OfPlayer && __result == null && ZTracker?.ZLevelsTracker[pawn.Map.Tile]?.ZLevels?.Count > 1)
        //            {
        //                if (ZTracker.jobTracker == null)
        //                {
        //                    ZTracker.jobTracker = new Dictionary<Pawn, JobTracker>();
        //                }
        //                if (ZTracker.jobTracker.ContainsKey(pawn))
        //                {
        //                    try
        //                    {
        //                        if (ZTracker.jobTracker[pawn]?.activeJobs?.Count() > 0)
        //                        {
        //                            if (!pawn.jobs.jobQueue.Contains(ZTracker.jobTracker[pawn].activeJobs[0]))
        //                            {
        //                                ZLogger.Message("Queue: " + ZTracker.jobTracker[pawn].activeJobs[0]);
        //                                pawn.jobs.jobQueue.EnqueueLast(ZTracker.jobTracker[pawn].activeJobs[0]);
        //                                return;
        //                            }
        //                            else if (pawn.jobs.jobQueue[0].job.targetA.Thing.Map != pawn.Map)
        //                            {
        //                                ZTracker.ResetJobTrackerFor(pawn);
        //                            }
        //                            else
        //                            {
        //                                return;
        //                            }
        //                        }
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        ZLogger.Message("Error2: " + ex);
        //                    };
        //                }
        //                else
        //                {
        //                    ZTracker.jobTracker[pawn] = new JobTracker();
        //                }
        //                if (Find.TickManager.TicksGame - ZTracker.jobTracker[pawn].lastTick < 200)
        //                // minimal job interval check per pawn is 200 ticks
        //                {
        //                    return;
        //                }
        //                Job result = null;
        //                var oldMap = pawn.Map;
        //                var oldPosition = pawn.Position;
        //                bool select = false;
        //
        //                foreach (var otherMap in ZTracker.ZLevelsTracker[pawn.Map.Tile].ZLevels.Values)
        //                {
        //                    if (otherMap != oldMap)
        //                    {
        //                        if (Find.TickManager.TicksGame - ZTracker.jobTracker[pawn].lastTick < 200)
        //                        {
        //                            return;
        //                        }
        //                        ZLogger.Message("Searching figth job for " + pawn);
        //                        var stairs = new List<Thing>();
        //
        //                        ZLogger.Message("Searching job for " + pawn + " in " + ZTracker.GetMapInfo(otherMap)
        //                            + " for " + ZTracker.GetMapInfo(oldMap));
        //
        //                        if (ZTracker.GetZIndexFor(otherMap) > ZTracker.GetZIndexFor(oldMap))
        //                        {
        //                            Map lowerMap = ZTracker.GetLowerLevel(otherMap.Tile, otherMap);
        //                            if (lowerMap != null)
        //                            {
        //                                ZLogger.Message("Searching stairs up in " + ZTracker.GetMapInfo(otherMap));
        //                                stairs = lowerMap.listerThings.AllThings.Where(x => x is Building_StairsUp).ToList();
        //                            }
        //                            else
        //                            {
        //                                ZLogger.Message("Lower map is null in " + ZTracker.GetMapInfo(otherMap));
        //                            }
        //                        }
        //                        else if (ZTracker.GetZIndexFor(otherMap) < ZTracker.GetZIndexFor(oldMap))
        //                        {
        //                            Map upperMap = ZTracker.GetUpperLevel(otherMap.Tile, otherMap);
        //                            if (upperMap != null)
        //                            {
        //                                ZLogger.Message("Searching stairs down in " + ZTracker.GetMapInfo(otherMap));
        //                                stairs = upperMap.listerThings.AllThings.Where(x => x is Building_StairsDown).ToList();
        //                            }
        //                            else
        //                            {
        //                                ZLogger.Message("Upper map is null in " + ZTracker.GetMapInfo(otherMap));
        //                            }
        //                        }
        //                        if (Find.TickManager.TicksGame - ZTracker.jobTracker[pawn].lastTick < 200)
        //                        // minimal job interval check per pawn is 200 ticks
        //                        {
        //                            return;
        //                        }
        //
        //                        if (stairs != null && stairs.Count() > 0)
        //                        {
        //                            var selectedStairs = GenClosest.ClosestThing_Global(pawn.Position, stairs, 99999f);
        //                            var position = selectedStairs.Position;
        //
        //                            if (Find.Selector.SelectedObjects.Contains(pawn)) select = true;
        //                            JobManagerPatches.manualDespawn = true;
        //                            pawn.DeSpawn();
        //                            JobManagerPatches.manualDespawn = false;
        //                            GenPlace.TryPlaceThing(pawn, position, otherMap, ThingPlaceMode.Direct);
        //
        //                            result = JobGiver_AIGotoNearestHostilePatch.TryGiveJob(pawn);
        //
        //                            if (result != null)
        //                            {
        //                                ZLogger.Message(pawn + " gets food job " + result);
        //                                ZLogger.Message("TryIssueJobPackage: " + pawn + " - map: " + ZTracker.GetMapInfo(pawn.Map)
        //                                    + " - " + pawn.Position + " result " + result);
        //                                JobManagerPatches.manualDespawn = true;
        //                                pawn.DeSpawn();
        //                                JobManagerPatches.manualDespawn = false;
        //                                GenPlace.TryPlaceThing(pawn, oldPosition, oldMap, ThingPlaceMode.Direct);
        //                                if (select) Find.Selector.Select(pawn);
        //                                ZTracker.BuildJobListFor(pawn, oldMap, otherMap, result, null);
        //                                break;
        //                            }
        //                        }
        //                    }
        //                }
        //                if (pawn.Map != oldMap)
        //                {
        //                    JobManagerPatches.manualDespawn = true;
        //                    pawn.DeSpawn();
        //                    JobManagerPatches.manualDespawn = false;
        //                    GenPlace.TryPlaceThing(pawn, oldPosition, oldMap, ThingPlaceMode.Direct);
        //                    if (select) Find.Selector.Select(pawn);
        //                }
        //                if (result == null)
        //                {
        //                    Map playerMap = ZTracker.GetMapByIndex(oldMap.Tile, 0);
        //                    if (pawn.Map != playerMap)
        //                    {
        //                        ZTracker.BuildJobListFor(pawn, oldMap, playerMap, JobMaker.MakeJob(JobDefOf.Wait_Combat, JobGiver_AIFightEnemy.ExpiryInterval_ShooterSucceeded.RandomInRange, true), null);
        //                    }
        //                }
        //                try
        //                {
        //                    ZLogger.Message("4 lastTick");
        //                    ZTracker.jobTracker[pawn].lastTick = Find.TickManager.TicksGame;
        //                }
        //                catch { }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Log.Error("Some kind of error occurred in Z-Levels JobManager: " + ex);
        //        }
        //    }
        //
        //    public static Job TryGiveJob(Pawn pawn)
        //    {
        //        float num = float.MaxValue;
        //        Thing thing = null;
        //        List<IAttackTarget> potentialTargetsFor = pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn);
        //        for (int i = 0; i < potentialTargetsFor.Count; i++)
        //        {
        //            IAttackTarget attackTarget = potentialTargetsFor[i];
        //            if (!attackTarget.ThreatDisabled(pawn) && AttackTargetFinder.IsAutoTargetable(attackTarget))
        //            {
        //                Thing thing2 = (Thing)attackTarget;
        //                int num2 = thing2.Position.DistanceToSquared(pawn.Position);
        //                if ((float)num2 < num && pawn.CanReach(thing2, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn))
        //                {
        //                    num = (float)num2;
        //                    thing = thing2;
        //                }
        //            }
        //        }
        //        if (thing != null)
        //        {
        //            Job job = JobMaker.MakeJob(JobDefOf.Goto, thing);
        //            job.checkOverrideOnExpire = true;
        //            job.expiryInterval = 500;
        //            job.collideWithPawns = true;
        //            return job;
        //        }
        //        return null;
        //    }
        //}



        [HarmonyPatch(typeof(JobGiver_GetFood), "TryGiveJob")]
        public class JobGiver_GetFoodPatch
        {
            [HarmonyPrefix]
            private static bool JobGiver_GetFoodPrefix(JobGiver_GetFood __instance, ref Job __result, Pawn pawn)
            {
                ZLogger.Message(pawn + " starting food search");
                if (pawn.def.race.Humanlike)
                {
                    try
                    {
                        var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                        if (ZTracker.jobTracker == null)
                        {
                            ZTracker.jobTracker = new Dictionary<Pawn, JobTracker>();
                        }
                        if (ZTracker.jobTracker.ContainsKey(pawn))
                        {
                            try
                            {
                                if (ZTracker.jobTracker[pawn]?.activeJobs?.Count() > 0)
                                {
                                    if (pawn.needs.food.CurCategory < HungerCategory.Starving
                                        && !pawn.jobs.jobQueue.Contains(ZTracker.jobTracker[pawn].activeJobs[0]))
                                    {
                                        if (ZTracker.jobTracker[pawn].activeJobs[0].def.defName != "UnloadYourHauledInventory")
                                        {

                                            ZLogger.Message("Queue: " + ZTracker.jobTracker[pawn].activeJobs[0]);
                                            pawn.jobs.jobQueue.EnqueueLast(ZTracker.jobTracker[pawn].activeJobs[0]);
                                            
                                            //Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;

                                            return false;
                                        }
                                    }
                                    else if (pawn.needs.food.CurCategory < HungerCategory.Starving
                                        && pawn.jobs.curJob == null)
                                    {
                                        ZLogger.Message("1 START JOB "
                                            + ZTracker.jobTracker[pawn].activeJobs[0] + " FOR " + pawn);
                                        __result = ZTracker.jobTracker[pawn].activeJobs[0];
                                        ZTracker.jobTracker[pawn].activeJobs.RemoveAt(0);

                                        //Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;

                                        return false;
                                    }
                                    else if (pawn.needs.food.CurCategory >= HungerCategory.Starving
                                        && pawn.jobs.curJob != ZTracker.jobTracker[pawn].activeJobs[0])
                                    {
                                        ZLogger.Message("1 RESETTING JOB TRACKER FOR " + pawn);
                                        ZLogger.Message(pawn + " - pawn.jobs.curJob: " + pawn.jobs.curJob);
                                        ZLogger.Message(pawn + " - ZTracker.jobTracker[pawn].activeJobs[0]: " + ZTracker.jobTracker[pawn].activeJobs[0]);
                                        foreach (var job in pawn.jobs.jobQueue)
                                        {
                                            ZLogger.Message(pawn + " - job in pawn queue: " + job.job);
                                        }
                                        foreach (var job in ZTracker.jobTracker[pawn].activeJobs)
                                        {
                                            ZLogger.Message(pawn + " - job in ZTracker queue: " + job);
                                        }
                                        ZTracker.ResetJobTrackerFor(pawn);

                                        //Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                ZLogger.Message("Error2: " + ex);
                            };
                        }
                        else
                        {
                            ZTracker.jobTracker[pawn] = new JobTracker();
                        }
        
                        Job result;
                        var oldMap = pawn.Map;
                        var oldPosition = pawn.Position;
                        bool select = false;
                        if (Find.Selector.SelectedObjects.Contains(pawn)) select = true;
        
                        float maxLevelPercentage = Traverse.Create(__instance).Field("maxLevelPercentage").GetValue<float>();
                        HungerCategory minCategory = Traverse.Create(__instance).Field("minCategory").GetValue<HungerCategory>();
        
                        foreach (var otherMap in ZTracker.GetAllMapsInClosestOrder(oldMap))
                        {
                            var stairs = new List<Thing>();
        
                            ZLogger.Message("Searching food job for " + pawn + " in " + ZTracker.GetMapInfo(otherMap)
                                + " for " + ZTracker.GetMapInfo(oldMap));
        
                            if (ZTracker.GetZIndexFor(otherMap) > ZTracker.GetZIndexFor(oldMap))
                            {
                                Map lowerMap = ZTracker.GetLowerLevel(otherMap.Tile, otherMap);
                                if (lowerMap != null)
                                {
                                    ZLogger.Message("Searching stairs up in " + ZTracker.GetMapInfo(otherMap));
                                    stairs = ZTracker.stairsUp[lowerMap];
                                    //stairs = lowerMap.listerThings.AllThings.Where(x => x is Building_StairsUp).ToList();
                                }
                                else
                                {
                                    ZLogger.Message("Lower map is null in " + ZTracker.GetMapInfo(otherMap));
                                }
                            }
                            else if (ZTracker.GetZIndexFor(otherMap) < ZTracker.GetZIndexFor(oldMap))
                            {
                                Map upperMap = ZTracker.GetUpperLevel(otherMap.Tile, otherMap);
                                if (upperMap != null)
                                {
                                    ZLogger.Message("Searching stairs down in " + ZTracker.GetMapInfo(otherMap));
                                    stairs = ZTracker.stairsDown[upperMap];
                                    //stairs = upperMap.listerThings.AllThings.Where(x => x is Building_StairsDown).ToList();
                                }
                                else
                                {
                                    ZLogger.Message("Upper map is null in " + ZTracker.GetMapInfo(otherMap));
                                }
                            }
        
                            if (stairs != null && stairs.Count() > 0)
                            {
                                var selectedStairs = stairs.MinBy(x => IntVec3Utility.DistanceTo(pawn.Position, x.Position));
                                var position = selectedStairs.Position;
        
                                //JobManagerPatches.manualDespawn = true;
                                //pawn.DeSpawn();
                                //JobManagerPatches.manualDespawn = false;
                                //GenPlace.TryPlaceThing(pawn, position, otherMap, ThingPlaceMode.Direct);
        
                                Traverse.Create(pawn).Field("mapIndexOrState")
                                    .SetValue((sbyte)Find.Maps.IndexOf(otherMap));
                                Traverse.Create(pawn).Field("positionInt")
                                    .SetValue(position);
                            }
                            else if (pawn.Map != oldMap && otherMap == oldMap)
                            {
                                Traverse.Create(pawn).Field("mapIndexOrState")
                                    .SetValue((sbyte)Find.Maps.IndexOf(oldMap));
                                Traverse.Create(pawn).Field("positionInt")
                                    .SetValue(oldPosition);
                            }
        
                            result = JobGiver_GetFoodPatch.TryGiveJob(pawn, __instance.forceScanWholeMap, maxLevelPercentage, minCategory);
        
                            if (result != null)
                            {
                                ZLogger.Message(pawn + " got food job " + result + " - map: "
                                    + ZTracker.GetMapInfo(pawn.Map) + " - " + pawn.Position);
        
                                Traverse.Create(pawn).Field("mapIndexOrState")
                                    .SetValue((sbyte)Find.Maps.IndexOf(oldMap));
                                Traverse.Create(pawn).Field("positionInt")
                                    .SetValue(oldPosition);
        
                                //JobManagerPatches.manualDespawn = true;
                                //pawn.DeSpawn();
                                //JobManagerPatches.manualDespawn = false;
                                //GenPlace.TryPlaceThing(pawn, oldPosition, oldMap, ThingPlaceMode.Direct);
        
                                ZTracker.BuildJobListFor(pawn, oldMap, otherMap, result, null);
                                __result = ZTracker.jobTracker[pawn].activeJobs[0];
                                ZTracker.jobTracker[pawn].activeJobs.RemoveAt(0);
                                break;
                            }
                        }
        
                        if (pawn.Map != oldMap)
                        {
                            Traverse.Create(pawn).Field("mapIndexOrState")
                                .SetValue((sbyte)Find.Maps.IndexOf(oldMap));
                            Traverse.Create(pawn).Field("positionInt")
                                .SetValue(oldPosition);
        
                            //JobManagerPatches.manualDespawn = true;
                            //    pawn.DeSpawn();
                            //    JobManagerPatches.manualDespawn = false;
                            //    GenPlace.TryPlaceThing(pawn, oldPosition, oldMap, ThingPlaceMode.Direct);
                        }
                        try
                        {
                            ZTracker.jobTracker[pawn].lastTickFood = Find.TickManager.TicksGame;
                        }
                        catch { }
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
            private static bool JobGiver_GetJoyPrefix(JobGiver_GetJoy __instance, ref Job __result, Pawn pawn)
            {
                try
                {
                    var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                    try
                    {
                        //    if (pawn.jobs.jobQueue.Count > 0)
                        //    {
                        //        //ZLogger.Message(pawn + " 1 taking job instead: " + pawn.jobs.jobQueue[0].job);
                        //        __result = null;
                        //        //Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
                        //        //pawn.jobs.jobQueue.Dequeue();
                        //        try
                        //        {
                        //            ZTracker.jobTracker[pawn].lastTickJoy = Find.TickManager.TicksGame;
                        //        }
                        //        catch { }
                        //        //try
                        //        //{
                        //        //    ZLogger.Message("ZTracker.jobTracker[pawn].activeJobs.Count > 0");
                        //        //    ZLogger.Message(pawn + " - pawn.jobs.curJob: " + pawn.jobs.curJob);
                        //        //    ZLogger.Message(pawn + " - ZTracker.jobTracker[pawn].activeJobs[0]: " + ZTracker.jobTracker[pawn].activeJobs[0]);
                        //        //    foreach (var job in pawn.jobs.jobQueue)
                        //        //    {
                        //        //        ZLogger.Message(pawn + " - job in pawn queue: " + job.job);
                        //        //    }
                        //        //    foreach (var job in ZTracker.jobTracker[pawn].activeJobs)
                        //        //    {
                        //        //        ZLogger.Message(pawn + " - job in ZTracker queue: " + job);
                        //        //    }
                        //        //}
                        //        //catch { };
                        //        return;
                        //    }
        
                        //if (ZTracker.jobTracker[pawn].activeJobs.Count > 0)
                        //{
                        //    ZLogger.Message(pawn + " 2 taking job instead: " + ZTracker.jobTracker[pawn].activeJobs[0]);
                        //    __result = ZTracker.jobTracker[pawn].activeJobs[0];
                        //    ZTracker.jobTracker[pawn].activeJobs.RemoveAt(0);
                        //    try
                        //    {
                        //        ZLogger.Message("ZTracker.jobTracker[pawn].activeJobs.Count > 0");
                        //        ZLogger.Message(pawn + " - pawn.jobs.curJob: " + pawn.jobs.curJob);
                        //        ZLogger.Message(pawn + " - ZTracker.jobTracker[pawn].activeJobs[0]: " + ZTracker.jobTracker[pawn].activeJobs[0]);
                        //        foreach (var job in pawn.jobs.jobQueue)
                        //        {
                        //            ZLogger.Message(pawn + " - job in pawn queue: " + job.job);
                        //        }
                        //        foreach (var job in ZTracker.jobTracker[pawn].activeJobs)
                        //        {
                        //            ZLogger.Message(pawn + " - job in ZTracker queue: " + job);
                        //        }
                        //    }
                        //    catch { };
                        //    return false;
                        //}
        
                    //    else
                    //    {
                    //        ZLogger.Message(pawn + " cant start new job");
                    //    }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error: " + ex);
                    }
        
                    if (ZTracker.jobTracker == null)
                    {
                        ZTracker.jobTracker = new Dictionary<Pawn, JobTracker>();
                    }
                    if (ZTracker.jobTracker.ContainsKey(pawn))
                    {
                        try
                        {
                            if (ZTracker.jobTracker[pawn]?.activeJobs?.Count() > 0)
                            {
                                if (pawn?.needs?.joy?.CurCategory > JoyCategory.Low
                                    && !pawn.jobs.jobQueue.Contains(ZTracker.jobTracker[pawn].activeJobs[0]))
                                {
                                    if (ZTracker.jobTracker[pawn].activeJobs[0].def.defName != "UnloadYourHauledInventory")
                                    {
                                        ZLogger.Message("Queue: " + ZTracker.jobTracker[pawn].activeJobs[0]);
                                        pawn.jobs.jobQueue.EnqueueLast(ZTracker.jobTracker[pawn].activeJobs[0]);
                                        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;

                                        return false;
                                    }
        
                                }
                                else if (pawn?.needs?.joy?.CurCategory > JoyCategory.Low
                                    && pawn.jobs.curJob == null)
                                {

                                    //ZLogger.Message("2 START JOB "
                                    //    + ZTracker.jobTracker[pawn].activeJobs[0] + " FOR " + pawn);
                                    //pawn.jobs.StartJob(ZTracker.jobTracker[pawn].activeJobs[0]);
                                    //ZTracker.jobTracker[pawn].activeJobs.RemoveAt(0);
                                    ZLogger.Message("Return 4");
                                    //Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;

                                    return false;
                                }
                                else if (pawn?.needs?.joy?.CurCategory <= JoyCategory.Low && pawn.jobs.curJob != ZTracker.jobTracker[pawn].activeJobs[0])
                                {
                                    ZLogger.Message("2 RESETTING JOB TRACKER FOR " + pawn);
                                    ZLogger.Message(pawn + " - pawn.jobs.curJob: " + pawn.jobs.curJob);
                                    ZLogger.Message(pawn + " - ZTracker.jobTracker[pawn].activeJobs[0]: " + ZTracker.jobTracker[pawn].activeJobs[0]);
                                    foreach (var job in pawn.jobs.jobQueue)
                                    {
                                        ZLogger.Message(pawn + " - job in pawn queue: " + job.job);
                                    }
                                    foreach (var job in ZTracker.jobTracker[pawn].activeJobs)
                                    {
                                        ZLogger.Message(pawn + " - job in ZTracker queue: " + job);
                                    }
                                    ZTracker.ResetJobTrackerFor(pawn);
                                    //Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;

                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ZLogger.Message("Error2: " + ex);
                        };
                    }
                    else
                    {
                        ZTracker.jobTracker[pawn] = new JobTracker();
                    }
                    //if (Find.TickManager.TicksGame - ZTracker.jobTracker[pawn].lastTickJoy < 200)
                    //// minimal job interval check per pawn is 200 ticks
                    //{
                    //    ZLogger.Message(pawn + " exceeded joy tick limit");
                    //    return;
                    //}
        
                    Job result = null;
                    var oldMap = pawn.Map;
                    var oldPosition = pawn.Position;
                    bool select = false;
                    if (Find.Selector.SelectedObjects.Contains(pawn)) select = true;
                    var jobList = new Dictionary<Job, Map>();
        
                    bool CanDoDuringMedicalRest = Traverse.Create(__instance).Field("CanDoDuringMedicalRest").GetValue<bool>();
                    DefMap<JoyGiverDef, float> joyGiverChances = Traverse.Create(__instance).Field("joyGiverChances").GetValue<DefMap<JoyGiverDef, float>>();
        
                    foreach (var otherMap in ZTracker.GetAllMapsInClosestOrder(oldMap))
                    {
                        ZLogger.Message(pawn + " - other map: " + otherMap);
        
                        var stairs = new List<Thing>();
        
                        ZLogger.Message("Searching joy job for " + pawn + " in " + ZTracker.GetMapInfo(otherMap)
                            + " for " + ZTracker.GetMapInfo(oldMap));
        
                        if (ZTracker.GetZIndexFor(otherMap) > ZTracker.GetZIndexFor(oldMap))
                        {
                            Map lowerMap = ZTracker.GetLowerLevel(otherMap.Tile, otherMap);
                            if (lowerMap != null)
                            {
                                //ZLogger.Message("Searching stairs up in " + ZTracker.GetMapInfo(otherMap));
                                stairs = ZTracker.stairsUp[lowerMap];
                                //stairs = lowerMap.listerThings.AllThings.Where(x => x is Building_StairsUp).ToList();
                            }
                            else
                            {
                                ZLogger.Message("Lower map is null in " + ZTracker.GetMapInfo(otherMap));
                            }
                        }
                        else if (ZTracker.GetZIndexFor(otherMap) < ZTracker.GetZIndexFor(oldMap))
                        {
                            Map upperMap = ZTracker.GetUpperLevel(otherMap.Tile, otherMap);
                            if (upperMap != null)
                            {
                                //ZLogger.Message("Searching stairs down in " + ZTracker.GetMapInfo(otherMap));
                                stairs = ZTracker.stairsDown[upperMap];
                                //stairs = upperMap.listerThings.AllThings.Where(x => x is Building_StairsDown).ToList();
                            }
                            else
                            {
                                ZLogger.Message("Upper map is null in " + ZTracker.GetMapInfo(otherMap));
                            }
                        }
        
                        if (stairs != null && stairs.Count() > 0)
                        {
                            var selectedStairs = stairs.MinBy(x => IntVec3Utility.DistanceTo(pawn.Position, x.Position));
                            //var selectedStairs = GenClosest.ClosestThing_Global(pawn.Position, stairs, 99999f);
                            var position = selectedStairs.Position;
        
                            Traverse.Create(pawn).Field("mapIndexOrState")
                                .SetValue((sbyte)Find.Maps.IndexOf(otherMap));
                            Traverse.Create(pawn).Field("positionInt")
                                .SetValue(position);
                        }
                        else if (pawn.Map != oldMap && otherMap == oldMap)
                        {
                            Traverse.Create(pawn).Field("mapIndexOrState")
                                .SetValue((sbyte)Find.Maps.IndexOf(oldMap));
                            Traverse.Create(pawn).Field("positionInt")
                                .SetValue(oldPosition);
                        }
                        result = JobGiver_GetJoyPatch.TryGiveJob(pawn, CanDoDuringMedicalRest, joyGiverChances, __instance);
                        if (result != null)
                        {
                            ZLogger.Message(pawn + " got joy job " + result + " - map: "
                                + ZTracker.GetMapInfo(pawn.Map) + " - " + pawn.Position);
                            jobList[result] = otherMap;
        
                            //JobManagerPatches.manualDespawn = true;
                            //pawn.DeSpawn();
                            //JobManagerPatches.manualDespawn = false;
                            //GenPlace.TryPlaceThing(pawn, oldPosition, oldMap, ThingPlaceMode.Direct);
        
                            //ZTracker.BuildJobListFor(pawn, oldMap, otherMap, result, null);
                            //__result = ZTracker.jobTracker[pawn].activeJobs[0];
                            //ZTracker.jobTracker[pawn].activeJobs.RemoveAt(0);
                            //break;
                        }
                    }
        
                    if (pawn.Map != oldMap)
                    {
                        Traverse.Create(pawn).Field("mapIndexOrState")
                            .SetValue((sbyte)Find.Maps.IndexOf(oldMap));
                        Traverse.Create(pawn).Field("positionInt")
                            .SetValue(oldPosition);
        
                        //JobManagerPatches.manualDespawn = true;
                        //pawn.DeSpawn();
                        //JobManagerPatches.manualDespawn = false;
                        //GenPlace.TryPlaceThing(pawn, oldPosition, oldMap, ThingPlaceMode.Direct);
                    }
        
                    if (jobList.Count > 0)
                    {
                        //var job = jobList.MaxBy(j => j.Key.def.joyGainRate);
                        var job = jobList.RandomElement();
                        ZTracker.BuildJobListFor(pawn, oldMap, job.Value, job.Key, null);
                        __result = ZTracker.jobTracker[pawn].activeJobs[0];
                        ZTracker.jobTracker[pawn].activeJobs.RemoveAt(0);
                    }
                    else
                    {
                        ZLogger.Message(pawn + " cant find joy job");
                    }
                    try
                    {
                        ZTracker.jobTracker[pawn].lastTickJoy = Find.TickManager.TicksGame;
                    }
                    catch { }
        
                    if (select) Find.Selector.Select(pawn);
        
                    return false;
                }
                catch (Exception ex)
                {
                    Log.Error("Some kind of error occurred in Z-Levels JobManager: " + ex);
                }
                return true;
            }
            public static Job TryGiveJob(Pawn pawn, bool CanDoDuringMedicalRest, DefMap<JoyGiverDef, float> joyGiverChances, JobGiver_GetJoy __instance)
            {
                if (!CanDoDuringMedicalRest && pawn.InBed() && HealthAIUtility.ShouldSeekMedicalRest(pawn))
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
                    Job job = result.Worker.TryGiveJob(pawn);
                    if (job != null)
                    {
                        return job;
                    }
                    joyGiverChances[result] = 0f;
                }
                return null;
            }
        }
        
        
        [HarmonyPatch(typeof(JobGiver_GetRest), "TryGiveJob")]
        public class JobGiver_GetRestPatch
        {
            [HarmonyPrefix]
            private static bool JobGiver_GetRestPrefix(JobGiver_GetFood __instance, ref Job __result, Pawn pawn)
            {
                try
                {
                    var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                    if (pawn.Faction == Faction.OfPlayer)
                    {
                        ZLogger.Message(pawn + " search for rest job");
                        if (ZTracker.jobTracker == null)
                        {
                            ZTracker.jobTracker = new Dictionary<Pawn, JobTracker>();
                        }
                        if (ZTracker.jobTracker.ContainsKey(pawn))
                        {
                            try
                            {
                                if (ZTracker.jobTracker[pawn]?.activeJobs?.Count() > 0)
                                {
                                    if (pawn.needs.rest.CurCategory < RestCategory.Exhausted &&
                                        !pawn.jobs.jobQueue.Contains(ZTracker.jobTracker[pawn].activeJobs[0]))
                                    {
                                        if (ZTracker.jobTracker[pawn].activeJobs[0].def.defName != "UnloadYourHauledInventory")
                                        {
                                            ZLogger.Message("Queue: " + ZTracker.jobTracker[pawn].activeJobs[0]);
                                            pawn.jobs.jobQueue.EnqueueLast(ZTracker.jobTracker[pawn].activeJobs[0]);
                                            //Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;

                                            return false;
                                        }
                                    }
                                    else if (pawn.needs.rest.CurCategory < RestCategory.Exhausted &&
                                        pawn.jobs.curJob == null)
                                    {
                                        ZLogger.Message("3 START JOB "
                                            + ZTracker.jobTracker[pawn].activeJobs[0] + " FOR " + pawn);
                                        __result = ZTracker.jobTracker[pawn].activeJobs[0];
                                        ZTracker.jobTracker[pawn].activeJobs.RemoveAt(0);
                                        //Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;

                                        return false;
                                    }
                                    else if (pawn.needs.rest.CurCategory >= RestCategory.Exhausted &&
                                        pawn.jobs.curJob != ZTracker.jobTracker[pawn].activeJobs[0])
                                    {
                                        ZLogger.Message("3 RESETTING JOB TRACKER FOR " + pawn);
                                        ZLogger.Message(pawn + " - pawn.jobs.curJob: " + pawn.jobs.curJob);
                                        ZLogger.Message(pawn + " - ZTracker.jobTracker[pawn].activeJobs[0]: " + ZTracker.jobTracker[pawn].activeJobs[0]);
                                        foreach (var job in pawn.jobs.jobQueue)
                                        {
                                            ZLogger.Message(pawn + " - job in pawn queue: " + job.job);
                                        }
                                        foreach (var job in ZTracker.jobTracker[pawn].activeJobs)
                                        {
                                            ZLogger.Message(pawn + " - job in ZTracker queue: " + job);
                                        }
                                        ZTracker.ResetJobTrackerFor(pawn);
                                        //Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;

                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                ZLogger.Message("Error2: " + ex);
                            };
                        }
                        else
                        {
                            ZTracker.jobTracker[pawn] = new JobTracker();
                        }
        
                        //if (pawn.needs?.rest?.CurCategory < RestCategory.VeryTired
                        //    && Find.TickManager.TicksGame - ZTracker.jobTracker[pawn].lastTickRest < 200)
                        //// minimal job interval check per pawn is 200 ticks
                        //{
                        //    return false;
                        //}
        
                        Job result = null;
                        var oldMap = pawn.Map;
                        var oldPosition = pawn.Position;
                        bool select = false;
                        if (Find.Selector.SelectedObjects.Contains(pawn)) select = true;
        
                        RestCategory minCategory = Traverse.Create(__instance).Field("minCategory").GetValue<RestCategory>();
                        float maxLevelPercentage = Traverse.Create(__instance).Field("maxLevelPercentage").GetValue<float>();
        
                        foreach (var otherMap in ZTracker.GetAllMapsInClosestOrder(oldMap))
                        {
                            //if (pawn?.needs?.rest?.CurCategory < RestCategory.VeryTired
                            //    && Find.TickManager.TicksGame - ZTracker.jobTracker[pawn].lastTickRest < 200)
                            //{
                            //    return false;
                            //}
        
                            ZLogger.Message("Searching rest job for " + pawn + " in " + ZTracker.GetMapInfo(otherMap)
                                + " for " + ZTracker.GetMapInfo(oldMap));
        
                            var stairs = new List<Thing>();
        
                            if (ZTracker.GetZIndexFor(otherMap) > ZTracker.GetZIndexFor(oldMap))
                            {
                                Map lowerMap = ZTracker.GetLowerLevel(otherMap.Tile, otherMap);
                                if (lowerMap != null)
                                {
                                    ZLogger.Message("Searching stairs up in " + ZTracker.GetMapInfo(otherMap));
                                    stairs = ZTracker.stairsUp[lowerMap];
                                    //stairs = lowerMap.listerThings.AllThings.Where(x => x is Building_StairsUp).ToList();
                                }
                                else
                                {
                                    ZLogger.Message("Lower map is null in " + ZTracker.GetMapInfo(otherMap));
                                }
                            }
                            else if (ZTracker.GetZIndexFor(otherMap) < ZTracker.GetZIndexFor(oldMap))
                            {
                                Map upperMap = ZTracker.GetUpperLevel(otherMap.Tile, otherMap);
                                if (upperMap != null)
                                {
                                    ZLogger.Message("Searching stairs down in " + ZTracker.GetMapInfo(otherMap));
                                    stairs = ZTracker.stairsDown[upperMap];
                                    //stairs = upperMap.listerThings.AllThings.Where(x => x is Building_StairsDown).ToList();
                                }
                                else
                                {
                                    ZLogger.Message("Upper map is null in " + ZTracker.GetMapInfo(otherMap));
                                }
                            }
        
                            //if (pawn?.needs?.rest?.CurCategory < RestCategory.VeryTired &&
                            //    Find.TickManager.TicksGame - ZTracker.jobTracker[pawn].lastTickRest < 200)
                            //// minimal job interval check per pawn is 200 ticks
                            //{
                            //    return false;
                            //}
        
                            if (stairs != null && stairs.Count() > 0)
                            {
                                var selectedStairs = stairs.MinBy(x => IntVec3Utility.DistanceTo(pawn.Position, x.Position));
                                //var selectedStairs = GenClosest.ClosestThing_Global(pawn.Position, stairs, 99999f);
                                var position = selectedStairs.Position;
        
                                Traverse.Create(pawn).Field("mapIndexOrState")
                                    .SetValue((sbyte)Find.Maps.IndexOf(otherMap));
                                Traverse.Create(pawn).Field("positionInt")
                                    .SetValue(position);
        
                                //JobManagerPatches.manualDespawn = true;
                                //pawn.DeSpawn();
                                //JobManagerPatches.manualDespawn = false;
                                //GenPlace.TryPlaceThing(pawn, position, otherMap, ThingPlaceMode.Direct);
                            }
                            else if (pawn.Map != oldMap && otherMap == oldMap)
                            {
                                Traverse.Create(pawn).Field("mapIndexOrState")
                                    .SetValue((sbyte)Find.Maps.IndexOf(oldMap));
                                Traverse.Create(pawn).Field("positionInt")
                                    .SetValue(oldPosition);
                            }
        
                            result = JobGiver_GetRestPatch.TryGiveJob(pawn, minCategory, maxLevelPercentage);
                            if (result != null && result.targetA.Thing != null)
                            {
                                ZLogger.Message(pawn + " got rest job " + result + " - map: "
                                    + ZTracker.GetMapInfo(pawn.Map) + " - " + pawn.Position);
                                if (pawn.Map != oldMap)
                                {
                                    Traverse.Create(pawn).Field("mapIndexOrState")
                                        .SetValue((sbyte)Find.Maps.IndexOf(oldMap));
                                    Traverse.Create(pawn).Field("positionInt")
                                        .SetValue(oldPosition);
        
                                    //JobManagerPatches.manualDespawn = true;
                                    //pawn.DeSpawn();
                                    //JobManagerPatches.manualDespawn = false;
                                    //GenPlace.TryPlaceThing(pawn, oldPosition, oldMap, ThingPlaceMode.Direct);
                                }
                                ZTracker.BuildJobListFor(pawn, oldMap, result.targetA.Thing.Map, result, null);
                                __result = ZTracker.jobTracker[pawn].activeJobs[0];
                                ZTracker.jobTracker[pawn].activeJobs.RemoveAt(0);
                                break;
                            }
                        }
        
                        if (pawn.Map != oldMap)
                        {
                            Traverse.Create(pawn).Field("mapIndexOrState")
                                .SetValue((sbyte)Find.Maps.IndexOf(oldMap));
                            Traverse.Create(pawn).Field("positionInt")
                                .SetValue(oldPosition);
        
                            //JobManagerPatches.manualDespawn = true;
                            //pawn.DeSpawn();
                            //JobManagerPatches.manualDespawn = false;
                            //GenPlace.TryPlaceThing(pawn, oldPosition, oldMap, ThingPlaceMode.Direct);
                        }
                        if (result.targetA.Thing == null)
                        {
                            ZLogger.Message(pawn + " taking rest on the ground");
                            __result = JobMaker.MakeJob(JobDefOf.LayDown, FindGroundSleepSpotFor(pawn));
                        }
        
                        if (select) Find.Selector.Select(pawn);
        
                        return false;
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
                var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                if ((lord != null && lord.CurLordToil != null && !lord.CurLordToil.AllowRestingInBed) || pawn.IsWildMan())
                {
                    building_Bed = null;
                }
                else
                {
                    if (pawn.ownership.OwnedBed != null && ZTracker.GetAllMaps(pawn.Map.Tile)
                        .Contains(pawn.ownership.OwnedBed.Map))
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

        //[HarmonyPatch(typeof(Pawn_WorkSettings), "WorkGiversInOrderNormal", MethodType.Getter)]
        //public static class Pawn_WorkSettings_Patch
        //{
        //    [HarmonyPrefix]
        //    public static bool WorkGiversInOrderNormal_Pre(Pawn_WorkSettings __instance, ref List<WorkGiver> __result)
        //    {
        //        if (JobManagerPatches.searchJobs)
        //        {
        //            __result = new List<WorkGiver>();
        //            return false;
        //        }
        //        return true;
        //    }
        //}
        //
        //[HarmonyPatch(typeof(Pawn_WorkSettings), "WorkGiversInOrderEmergency", MethodType.Getter)]
        //public static class Pawn_WorkSettings_Emerg_Patch
        //{
        //    [HarmonyPrefix]
        //    public static bool WorkGiversInOrderEmerg_Pre(Pawn_WorkSettings __instance, ref List<WorkGiver> __result)
        //    {
        //        if (JobManagerPatches.searchJobs)
        //        {
        //            __result = new List<WorkGiver>();
        //            return false;
        //        }
        //        return true;
        //    }
        //}
        
        [HarmonyPatch(typeof(JobGiver_Work), "TryIssueJobPackage")]
        public class TryIssueJobPackagePatch
        {
            //private static void Prefix()
            //{
            //    JobManagerPatches.searchJobs = true;
            //}
        
            private static bool Prefix(JobGiver_Work __instance, ref ThinkResult __result, Pawn pawn, JobIssueParams jobParams)
            {
                ZLogger.Message(pawn + " start work search");
                //JobManagerPatches.searchJobs = false;
                var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                try
                {
                    try
                    {
                        if (pawn.needs.food.CurCategory >= HungerCategory.UrgentlyHungry
                            || pawn.needs.joy.CurCategory < JoyCategory.Low
                            || pawn.needs.rest.CurCategory >= RestCategory.VeryTired)
                        {
                            ZLogger.Message(pawn + " refusing to work because low needs");
                            ZLogger.Message("pawn.needs.food.CurCategory: " + pawn.needs.food.CurCategory);
                            ZLogger.Message("pawn.needs.joy.CurCategory: " + pawn.needs.joy.CurCategory);
                            ZLogger.Message("pawn.needs.rest.CurCategory: " + pawn.needs.rest.CurCategory);
                            __result = ThinkResult.NoJob;
                            return false;
                        }
                    }
                    catch { };
            
                    if (ZTracker.jobTracker == null)
                    {
                        ZTracker.jobTracker = new Dictionary<Pawn, JobTracker>();
                    }
                    if (ZTracker.jobTracker.ContainsKey(pawn))
                    {
                        try
                        {
                            if (ZTracker.jobTracker[pawn]?.activeJobs?.Count() > 0)
                            {
                                if (!pawn.jobs.jobQueue.Contains(ZTracker.jobTracker[pawn].activeJobs[0]))
                                {
                                    if (ZTracker.jobTracker[pawn].activeJobs[0].def.defName != "UnloadYourHauledInventory")
                                    {
                                        ZLogger.Message("1 Queue: " + ZTracker.jobTracker[pawn].activeJobs[0]);
                                        
                                        __result = new ThinkResult(ZTracker.jobTracker[pawn].activeJobs[0], ZTracker.jobTracker[pawn].activeJobs[0].jobGiver);
                                        ZTracker.jobTracker[pawn].activeJobs.RemoveAt(0);
                                        
                                        //pawn.jobs.jobQueue.EnqueueLast(ZTracker.jobTracker[pawn].activeJobs[0]);
                                        //Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
                            
                                        return false;
                                    }
                                }
                                else if (pawn.jobs.curJob == null)
                                {
                                    //ZLogger.Message("4 START JOB "
                                    //    + ZTracker.jobTracker[pawn].activeJobs[0] + " FOR " + pawn);
                                    //
                                    //ZLogger.Message(pawn + " - pawn.jobs.curJob: " + pawn.jobs.curJob);
                                    //ZLogger.Message(pawn + " - ZTracker.jobTracker[pawn].activeJobs[0]: " + ZTracker.jobTracker[pawn].activeJobs[0]);
                                    //foreach (var job in pawn.jobs.jobQueue)
                                    //{
                                    //    ZLogger.Message(pawn + " - job in pawn queue: " + job.job);
                                    //    if (!job.job.def.defName.Contains("load"))
                                    //    {
                                    //        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
                                    //    }
                                    //}
                                    //foreach (var job in ZTracker.jobTracker[pawn].activeJobs)
                                    //{
                                    //    ZLogger.Message(pawn + " - job in ZTracker queue: " + job);
                                    //}
                            
                                    //pawn.jobs.jobQueue.EnqueueFirst(ZTracker.jobTracker[pawn].activeJobs[0]);
                                    //ZTracker.jobTracker[pawn].activeJobs.RemoveAt(0);
                                    ZLogger.Message(pawn + " - return 3");
                                    //Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
                            
                                    return false;
                                }
                                else if (pawn.jobs.curJob != ZTracker.jobTracker[pawn].activeJobs[0])
                                {
                                    ZLogger.Message("4 RESETTING JOB TRACKER FOR " + pawn);
                                    ZLogger.Message(pawn + " - pawn.jobs.curJob: " + pawn.jobs.curJob);
                                    ZLogger.Message(pawn + " - ZTracker.jobTracker[pawn].activeJobs[0]: " + ZTracker.jobTracker[pawn].activeJobs[0]);
                                    foreach (var job in pawn.jobs.jobQueue)
                                    {
                                        ZLogger.Message(pawn + " - job in pawn queue: " + job.job);
                                    }
                                    foreach (var job in ZTracker.jobTracker[pawn].activeJobs)
                                    {
                                        ZLogger.Message(pawn + " - job in ZTracker queue: " + job);
                                    }
                                    ZTracker.ResetJobTrackerFor(pawn);
                                    //Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ZLogger.Message("Error2: " + ex);
                        };
                    }
                    else
                    {
                        ZTracker.jobTracker[pawn] = new JobTracker();
                    }
            
                    ThinkResult result;
                    var oldMap = pawn.Map;
                    var oldPosition = pawn.Position;
                    bool select = false;
                    if (Find.Selector.SelectedObjects.Contains(pawn)) select = true;
            
                    Map dest = null;
                    result = TryIssueJobPackage(pawn, jobParams, __instance, __instance.emergency, ref dest, oldMap, oldPosition);
                    if (result.Job != null)
                    {
            
                        if (pawn.Map != oldMap)
                        {
                            //JobManagerPatches.manualDespawn = true;
                            //pawn.DeSpawn();
                            //JobManagerPatches.manualDespawn = false;
                            //GenPlace.TryPlaceThing(pawn, oldPosition, oldMap, ThingPlaceMode.Direct);
            
                            Traverse.Create(pawn).Field("mapIndexOrState")
                                .SetValue((sbyte)Find.Maps.IndexOf(oldMap));
                            Traverse.Create(pawn).Field("positionInt")
                                .SetValue(oldPosition);
                            //pawn.Position = oldPosition;
                            //Log.Message("1: " + oldPosition, true);
            
                        }
                        ZLogger.Message(pawn + " got job " + result + " - map: "
                            + ZTracker.GetMapInfo(pawn.Map) + " - " + pawn.Position);
                        if (dest != null)
                        {
                            ZTracker.BuildJobListFor(pawn, oldMap, dest, result.Job, null);
                        }
                        else
                        {
                            ZTracker.BuildJobListFor(pawn, oldMap, oldMap, result.Job, null);
                        }
                        __result = new ThinkResult(ZTracker.jobTracker[pawn].activeJobs[0], ZTracker.jobTracker[pawn].activeJobs[0].jobGiver);
                        ZTracker.jobTracker[pawn].activeJobs.RemoveAt(0);
            
                    }
                    else
                    {
                        __result = ThinkResult.NoJob;
                        ZLogger.Message(pawn + " failed to find job");
                    }
            
                    if (pawn.Map != oldMap)
                    {
                        Traverse.Create(pawn).Field("mapIndexOrState")
                            .SetValue((sbyte)Find.Maps.IndexOf(oldMap));
                        Traverse.Create(pawn).Field("positionInt")
                            .SetValue(oldPosition);
            
                        //pawn.Position = oldPosition;
                        //Log.Message("2: " + oldPosition, true);
            
                        //JobManagerPatches.manualDespawn = true;
                        //pawn.DeSpawn();
                        //JobManagerPatches.manualDespawn = false;
                        //GenPlace.TryPlaceThing(pawn, oldPosition, oldMap, ThingPlaceMode.Direct);
                    }
                    if (pawn.Position != oldPosition)
                    {
                        Traverse.Create(pawn).Field("positionInt")
                            .SetValue(oldPosition);
                        //pawn.Position = oldPosition;
                        //Log.Message("3: " + oldPosition, true);
                    }
            
                    if (select) Find.Selector.Select(pawn);
                    return false;
                }
                catch (Exception ex)
                {
                    Log.Error("Some kind of error occurred in Z-Levels JobManager: " + ex);
                }
                return true;
            }
                
        
            public static bool TryFindBestBetterStoreCellForValidator(Thing t, Pawn carrier, Map mapToSearch,
                StoragePriority currentPriority, Faction faction, out IntVec3 foundCell, bool needAccurateResult = true)
            {
                Map temp = null;
                return TryFindBestBetterStoreCellFor(t, carrier, mapToSearch, currentPriority, faction, out foundCell, ref temp);
            }
        
            public static bool TryFindBestBetterStoreCellFor(Thing t, Pawn carrier, Map mapToSearch, StoragePriority currentPriority, Faction faction, out IntVec3 foundCell, ref Map dest, bool needAccurateResult = true)
            {
                bool result = true;
                var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                var allZones = new Dictionary<Zone_Stockpile, Map>();
                var allMaps = ZTracker.GetAllMaps(carrier.Map.Tile);
                foreach (Map map in allMaps)
                {
                    foreach (var zone in map.zoneManager.AllZones.Where(x => x is Zone_Stockpile))
                    {
                        allZones[(Zone_Stockpile)zone] = map;
                    }
                }
        
                List<SlotGroup> allGroupsListInPriorityOrder = new List<SlotGroup>();
                foreach (var zone in allZones)
                {
                    allGroupsListInPriorityOrder.Add(zone.Key.GetSlotGroup());
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
                ZLogger.Message("----------------------------");
                foreach (var slotGroup in allGroupsListInPriorityOrder.OrderByDescending(x => x.Settings.Priority))
                {
                    ZLogger.Message("Checking on : " + t + " - " + ZTracker.GetMapInfo(t.Map) + " for " + carrier + " - " + carrier.Map + " - " + slotGroup.parent
                        + " - priority: " + slotGroup.Settings.Priority + " - " + slotGroup.parent.Map);
                    StoragePriority priority = slotGroup.Settings.Priority;
                    if (priority < storagePriority || priority <= currentPriority)
                    {
                        ZLogger.Message("Break on - " + slotGroup.parent + " - priority: " + slotGroup.Settings.Priority + " - " + slotGroup.parent.Map);
                        break;
                    }
                    TryIssueJobPackagePatch.TryFindBestBetterStoreCellForWorker
                    (t, carrier, mapToSearch, faction, slotGroup, needAccurateResult,
                    ref invalid, ref num, ref storagePriority, slotGroup.parent.Map, ref dest);
                }
                if (!invalid.IsValid)
                {
                    foundCell = IntVec3.Invalid;
                    result = false;
                }
                foundCell = invalid;
        
                return result;
            }
        
            private static void TryFindBestBetterStoreCellForWorker(Thing t, Pawn carrier, Map map, Faction faction, SlotGroup slotGroup, bool needAccurateResult, ref IntVec3 closestSlot, ref float closestDistSquared, ref StoragePriority foundPriority, Map oldMap, ref Map dest)
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
                    ZLogger.Message(t + " already in stockpile " + slotGroup.parent);
                    return;
                }
                ZLogger.Message(slotGroup.parent + " - priority: " + slotGroup.Settings.Priority
                    + " - " + slotGroup.parent.Map + " accepts " + t + " - " + t.Map, true);
        
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
                    if (num2 <= closestDistSquared && IsGoodStoreCell(intVec, map, t, carrier, faction, oldMap))
                    {
                        closestSlot = intVec;
                        dest = slotGroup.parent.Map;
                        closestDistSquared = num2;
                        foundPriority = slotGroup.Settings.Priority;
                        var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                        ZLogger.Message("RESULT: " + carrier + " - " + slotGroup.parent + " - priority: "
                            + slotGroup.Settings.Priority + " - " + ZTracker.GetMapInfo(slotGroup.parent.Map)
                            + " accepts " + t + " in " + ZTracker.GetMapInfo(dest), true);
                        if (i >= num)
                        {
                            break;
                        }
                    }
                    else
                    {
                        var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                        ZLogger.Message(slotGroup.parent + " - priority: "
                            + slotGroup.Settings.Priority + " - " + ZTracker.GetMapInfo(slotGroup.parent.Map)
                            + " cant accept " + t + " in " + ZTracker.GetMapInfo(t.Map));
                    }
                }
            }
        
            public static bool IsGoodStoreCell(IntVec3 c, Map map, Thing t, Pawn carrier, Faction faction, Map oldMap)
            {
                if (carrier != null && c.IsForbidden(carrier))
                {
                    return false;
                }
                if (!NoStorageBlockersIn(c, oldMap, t))
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
        
                return true;// carrier == null || carrier.Map.reachability.CanReach(t.SpawnedOrAnyParentSpawned ? t.PositionHeld : carrier.PositionHeld, c, PathEndMode.ClosestTouch, TraverseParms.For(carrier, Danger.Deadly, TraverseMode.ByPawn, false));
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
        
            public static Job HasJobOnThing(WorkGiver_DoBill scanner, Pawn pawn, Thing thing, bool forced = false)
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
                    var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                    var origMap = thing.Map;
                    var origMap2 = pawn.Map;
        
                    foreach (var map in ZTracker.GetAllMaps(pawn.Map.Tile))
                    {
                        try
                        {
                            Traverse.Create(thing).Field("mapIndexOrState")
                                .SetValue((sbyte)Find.Maps.IndexOf(map));
        
                            Traverse.Create(pawn).Field("mapIndexOrState")
                                .SetValue((sbyte)Find.Maps.IndexOf(map));
        
                            Job job = Traverse.Create(scanner).Method("StartOrResumeBillJob", new object[]
                            {
                                pawn, billGiver
                            }).GetValue<Job>();
                            if (job != null)
                            {
                                Traverse.Create(thing).Field("mapIndexOrState")
                                    .SetValue((sbyte)Find.Maps.IndexOf(origMap));
        
                                Traverse.Create(pawn).Field("mapIndexOrState")
                                    .SetValue((sbyte)Find.Maps.IndexOf(origMap2));
        
                                return job;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Z-Levels failed to process HasJobOnThing of DoBill workgiver. Report about it to devs and provide Hugslib log. Error: " + ex);
                        }
                    }
                    Traverse.Create(thing).Field("mapIndexOrState")
                        .SetValue((sbyte)Find.Maps.IndexOf(origMap));
                    Traverse.Create(pawn).Field("mapIndexOrState")
                        .SetValue((sbyte)Find.Maps.IndexOf(origMap2));
                    return null;
                }
                if (!RefuelWorkGiverUtility.CanRefuel(pawn, thing, forced))
                {
                    return null;
                }
                return RefuelWorkGiverUtility.RefuelJob(pawn, thing, forced, null, null);
            }
        
            public static Job JobOnThing(WorkGiver_ConstructDeliverResourcesToBlueprints scanner, Pawn pawn, Thing t, bool forced = false)
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
                Job job = Traverse.Create(scanner).Method("RemoveExistingFloorJob", new object[]
                {
                    pawn, blueprint
                }).GetValue<Job>();
        
                if (job != null)
                {
                    return job;
                }
                var method = Traverse.Create(scanner).Method("ResourceDeliverJobFor", new object[]
                {
                    pawn, blueprint, true
                });
        
                Job job2 = method.GetValue<Job>();
                if (job2 == null)
                {
                    var oldMap = pawn.Map;
                    var oldPosition = pawn.Position;
        
                    var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
        
                    foreach (var otherMap in ZTracker.ZLevelsTracker[pawn.Map.Tile].ZLevels.Values)
                    {
                        if (otherMap != oldMap)
                        {
                            var stairs = otherMap.listerThings.AllThings.Where(x => x is Building_StairsDown
                            || x is Building_StairsUp).ToList();
        
                            if (stairs != null && stairs.Count() > 0)
                            {
                                //var selectedStairs = GenClosest.ClosestThing_Global(pawn.Position, stairs, 99999f);
                                var selectedStairs = stairs.MinBy(x => IntVec3Utility.DistanceTo(pawn.Position, x.Position));
                                var position = selectedStairs.Position;
        
                                Traverse.Create(pawn).Field("mapIndexOrState")
                                    .SetValue((sbyte)Find.Maps.IndexOf(otherMap));
                                Traverse.Create(pawn).Field("positionInt")
                                    .SetValue(position);
                                //pawn.Position = position;
                                //Log.Message("4: " + oldPosition, true);
        
                                //JobManagerPatches.manualDespawn = true;
                                //pawn.DeSpawn();
                                //JobManagerPatches.manualDespawn = false;
                                //GenPlace.TryPlaceThing(pawn, position, otherMap, ThingPlaceMode.Direct);
        
                                job2 = method.GetValue<Job>();
                                if (job2 != null)
                                {
                                    break;
                                }
                            }
                        }
                    }
                    if (pawn.Map != oldMap)
                    {
                        Traverse.Create(pawn).Field("mapIndexOrState")
                            .SetValue((sbyte)Find.Maps.IndexOf(oldMap));
                        Traverse.Create(pawn).Field("positionInt")
                            .SetValue(oldPosition);
                        //pawn.Position = oldPosition;
                        //Log.Message("5: " + oldPosition, true);
        
                        //JobManagerPatches.manualDespawn = true;
                        //pawn.DeSpawn();
                        //JobManagerPatches.manualDespawn = false;
                        //GenPlace.TryPlaceThing(pawn, oldPosition, oldMap, ThingPlaceMode.Direct);
        
                    }
                }
        
                if (job2 != null)
                {
                    return job2;
                }
                if (scanner.def.workType != WorkTypeDefOf.Hauling)
                {
                    Job job3 = Traverse.Create(scanner).Method("NoCostFrameMakeJobFor", new object[]
                    {
                        pawn, blueprint
                    }).GetValue<Job>();
                    if (job3 != null)
                    {
                        return job3;
                    }
                }
                return null;
            }

            public static Job JobOnThing(WorkGiver_ConstructDeliverResourcesToFrames scanner, Pawn pawn, Thing t, bool forced = false)
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
                var method = Traverse.Create(scanner).Method("ResourceDeliverJobFor", new object[]
                {
                    pawn, frame, true
                });

                Job job2 = method.GetValue<Job>();
                if (job2 == null)
                {
                    var oldMap = pawn.Map;
                    var oldPosition = pawn.Position;

                    var ZTracker = Current.Game.GetComponent<ZLevelsManager>();

                    foreach (var otherMap in ZTracker.ZLevelsTracker[pawn.Map.Tile].ZLevels.Values)
                    {
                        if (otherMap != oldMap)
                        {
                            var stairs = otherMap.listerThings.AllThings.Where(x => x is Building_StairsDown
                            || x is Building_StairsUp).ToList();

                            if (stairs != null && stairs.Count() > 0)
                            {
                                //var selectedStairs = GenClosest.ClosestThing_Global(pawn.Position, stairs, 99999f);
                                var selectedStairs = stairs.MinBy(x => IntVec3Utility.DistanceTo(pawn.Position, x.Position));
                                var position = selectedStairs.Position;

                                Traverse.Create(pawn).Field("mapIndexOrState")
                                    .SetValue((sbyte)Find.Maps.IndexOf(otherMap));
                                Traverse.Create(pawn).Field("positionInt")
                                    .SetValue(position);
                                //pawn.Position = position;
                                //Log.Message("4: " + oldPosition, true);

                                //JobManagerPatches.manualDespawn = true;
                                //pawn.DeSpawn();
                                //JobManagerPatches.manualDespawn = false;
                                //GenPlace.TryPlaceThing(pawn, position, otherMap, ThingPlaceMode.Direct);

                                job2 = method.GetValue<Job>();
                                if (job2 != null)
                                {
                                    break;
                                }
                            }
                        }
                    }
                    if (pawn.Map != oldMap)
                    {
                        Traverse.Create(pawn).Field("mapIndexOrState")
                            .SetValue((sbyte)Find.Maps.IndexOf(oldMap));
                        Traverse.Create(pawn).Field("positionInt")
                            .SetValue(oldPosition);
                        //pawn.Position = oldPosition;
                        //Log.Message("5: " + oldPosition, true);

                        //JobManagerPatches.manualDespawn = true;
                        //pawn.DeSpawn();
                        //JobManagerPatches.manualDespawn = false;
                        //GenPlace.TryPlaceThing(pawn, oldPosition, oldMap, ThingPlaceMode.Direct);

                    }
                }

                if (job2 != null)
                {
                    return job2;
                }
                else
                {
                    return null;
                }

                //return ResourceDeliverJobFor(pawn, frame);
            }
            public static ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams, JobGiver_Work instance, bool emergency, ref Map dest, Map oldMap, IntVec3 oldPosition)
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
                List<WorkGiver> list = pawn.workSettings.WorkGiversInOrderNormal;
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
                var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                var entryPoints = new Dictionary<Map, IntVec3>();
                for (int j = 0; j < list.Count; j++)
                {
                    WorkGiver workGiver = list[j];
                    foreach (var otherMap in ZTracker.GetAllMapsInClosestOrder(oldMap))
                    {
                        //ZLogger.Message("Searching job for " + pawn + " in " + ZTracker.GetMapInfo(otherMap)
                        //        + " for " + ZTracker.GetMapInfo(oldMap));
                        var stairs = new List<Thing>();
                        if (ZTracker.GetZIndexFor(otherMap) > ZTracker.GetZIndexFor(oldMap))
                        {
                            Map lowerMap = ZTracker.GetLowerLevel(otherMap.Tile, otherMap);
                            if (lowerMap != null)
                            {
                                //ZLogger.Message("Searching stairs up in " + ZTracker.GetMapInfo(lowerMap));
                                stairs = ZTracker.stairsUp[lowerMap];
                                    //lowerMap.listerThings.AllThings.Where(x => x is Building_StairsUp).ToList();
                            }
                            else
                            {
                                ZLogger.Message("Lower map is null in " + ZTracker.GetMapInfo(otherMap));
                            }
                        }
                        else if (ZTracker.GetZIndexFor(otherMap) < ZTracker.GetZIndexFor(oldMap))
                        {
                            Map upperMap = ZTracker.GetUpperLevel(otherMap.Tile, otherMap);
                            if (upperMap != null)
                            {
                                //ZLogger.Message("Searching stairs down in " + ZTracker.GetMapInfo(upperMap));
                                stairs = ZTracker.stairsDown[upperMap];
                                    //upperMap.listerThings.AllThings.Where(x => x is Building_StairsDown).ToList();
                            }
                            else
                            {
                                ZLogger.Message("Upper map is null in " + ZTracker.GetMapInfo(otherMap));
                            }
                        }
                        if (stairs != null && stairs.Count() > 0)
                        {
                            IntVec3 position = IntVec3.Invalid;
                            if (!entryPoints.ContainsKey(otherMap))
                            {
                                //var selectedStairs = GenClosest.ClosestThing_Global(pawn.Position, stairs, 99999f);
                                var selectedStairs = stairs.MinBy(x => IntVec3Utility.DistanceTo(pawn.Position, x.Position));
                                position = selectedStairs.Position;
                                entryPoints[otherMap] = position;
                            }
                            else
                            {
                                position = entryPoints[otherMap];
                            }
        
                            Traverse.Create(pawn).Field("mapIndexOrState")
                                .SetValue((sbyte)Find.Maps.IndexOf(otherMap));
                            Traverse.Create(pawn).Field("positionInt")
                                .SetValue(position);
                            //pawn.Position = position;
                            //Log.Message(pawn + " - 6: " + oldPosition, true);
        
                            //JobManagerPatches.manualDespawn = true;
                            //pawn.DeSpawn();
                            //JobManagerPatches.manualDespawn = false;
                            //GenPlace.TryPlaceThing(pawn, position, otherMap, ThingPlaceMode.Direct);
        
                        }
                        else if (pawn.Map != oldMap && otherMap == oldMap)
                        {
        
                            //JobManagerPatches.manualDespawn = true;
                            //pawn.DeSpawn();
                            //JobManagerPatches.manualDespawn = false;
        
                            Traverse.Create(pawn).Field("mapIndexOrState")
                                .SetValue((sbyte)Find.Maps.IndexOf(oldMap));
                            Traverse.Create(pawn).Field("positionInt")
                                .SetValue(oldPosition);
        
                            //if (oldPosition.GetTerrain(oldMap) == ZLevelsDefOf.ZL_OutsideTerrain)
                            //{
                            //    IntVec3 position = IntVec3.Invalid;
                            //    if (!entryPoints.ContainsKey(oldMap))
                            //    {
                            //        //var selectedStairs = GenClosest.ClosestThing_Global(pawn.Position, stairs, 99999f);
                            //        var selectedStairs = stairs.MinBy(x => IntVec3Utility.DistanceTo(pawn.Position, x.Position));
                            //        position = selectedStairs.Position;
                            //        entryPoints[oldMap] = position;
                            //        if (CellFinder.TryFindRandomCellNear(oldPosition, oldMap, 100,
                            //            (IntVec3 c) => c.Walkable(oldMap), out position))
                            //        {
                            //            Traverse.Create(pawn).Field("mapIndexOrState")
                            //                .SetValue((sbyte)Find.Maps.IndexOf(oldMap));
                            //            Traverse.Create(pawn).Field("positionInt")
                            //                .SetValue(position);
                            //            //pawn.Position = position;
                            //            //Log.Message(pawn + " - 7: " + oldPosition, true);
                            //
                            //            //GenPlace.TryPlaceThing(pawn, position, oldMap, ThingPlaceMode.Direct);
                            //        }
                            //    }
                            //    else
                            //    {
                            //        position = entryPoints[oldMap];
                            //        Traverse.Create(pawn).Field("mapIndexOrState")
                            //            .SetValue((sbyte)Find.Maps.IndexOf(oldMap));
                            //        Traverse.Create(pawn).Field("positionInt")
                            //            .SetValue(position);
                            //        //pawn.Position = position;
                            //        //Log.Message(pawn + " - 8: " + oldPosition, true);
                            //
                            //        //GenPlace.TryPlaceThing(pawn, position, oldMap, ThingPlaceMode.Direct);
                            //    }
                            //}
                            //else
                            //{
                            //    Traverse.Create(pawn).Field("mapIndexOrState")
                            //        .SetValue((sbyte)Find.Maps.IndexOf(oldMap));
                            //    Traverse.Create(pawn).Field("positionInt")
                            //        .SetValue(oldPosition);
                            //    //Log.Message(pawn + " - 9: " + oldPosition, true);
                            //    //pawn.Position = oldPosition;
                            //
                            //    //GenPlace.TryPlaceThing(pawn, oldPosition, oldMap, ThingPlaceMode.Direct);
                            //}
                        }
                        if (workGiver.def.priorityInType != num && bestTargetOfLastPriority.IsValid)
                        {
                            break;
                        }
        
                        if (!PawnCanUseWorkGiver(pawn, workGiver))
                        {
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
                                    Predicate<Thing> validator = (Thing t) => !t.IsForbidden(pawn) &&
                                    scanner.HasJobOnThing(pawn, t);
        
                                    Predicate<Thing> deliverResourcesValidator = (Thing t) => !t.IsForbidden(pawn) &&
                                        JobOnThing((WorkGiver_ConstructDeliverResourcesToBlueprints)scanner, pawn, t) != null;

                                    Predicate<Thing> deliverResourcesValidator2 = (Thing t) => !t.IsForbidden(pawn) &&
                                        JobOnThing((WorkGiver_ConstructDeliverResourcesToFrames)scanner, pawn, t) != null;
                                    Predicate<Thing> billValidator = (Thing t) => !t.IsForbidden(pawn) &&
                                    TryIssueJobPackagePatch.HasJobOnThing((WorkGiver_DoBill)scanner, pawn, t) != null;
        
                                    IntVec3 foundCell;
                                    Predicate<Thing> haulingValidator = (Thing t) => !t.IsForbidden(pawn)
                                    && pawn?.carryTracker?.AvailableStackSpace(t?.def) > 0
                                    && pawn?.carryTracker?.MaxStackSpaceEver(t?.def) > 0
                                    && pawn.CanReserve(t, 1, -1, null, false)
                                    && TryIssueJobPackagePatch.TryFindBestBetterStoreCellForValidator(t,
                                    pawn, t.Map, StoreUtility.CurrentStoragePriorityOf(t), pawn.Faction,
                                    out foundCell, true);
        
                                    IEnumerable<Thing> enumerable;
        
                                    if (scanner is WorkGiver_HaulGeneral)
                                    {
                                        ZLogger.Message(pawn + " - pawn.map: " + pawn.Map);
                                        var allZones = new Dictionary<Zone_Stockpile, Map>();
                                        var allMaps = ZTracker.GetAllMaps(pawn.Map.Tile);
                                        foreach (Map map in allMaps)
                                        {
                                            foreach (var zone in map.zoneManager.AllZones.Where(x => x is Zone_Stockpile))
                                            {
                                                //ZLogger.Message("Adding " + zone + " in " + ZTracker.GetMapInfo(zone.Map) + " - " + ZTracker.GetMapInfo(map));
                                                allZones[(Zone_Stockpile)zone] = map;
                                            }
                                        }
        
                                        List<Zone_Stockpile> copiedZones = new List<Zone_Stockpile>();
                                        IntVec3 newPosition2 = IntVec3.Invalid;
                                        foreach (Map map in allMaps)
                                        {
                                            foreach (var zone in allZones)
                                            {
                                                if (zone.Key.Map != pawn.Map)
                                                {
                                                    var newZone = new Zone_Stockpile();
                                                    newZone.settings = zone.Key.settings;
                                                    newZone.label = zone.Key.label + " - Copy";
                                                    foreach (IntVec3 intVec in zone.Key.cells)
                                                    {
                                                        var newPosition = (intVec - zone.Key.Position) + pawn.Position;
                                                        if (newPosition.GetZone(pawn.Map) == null && newPosition.GetSlotGroup(pawn.Map) == null)
                                                        {
                                                            newZone.cells.Add(newPosition);
                                                        }
                                                        else if (CellFinder.TryFindRandomCellNear
                                                            (newPosition, map, 1000, (IntVec3 c) => c.Walkable(map)
                                                            && c.GetZone(map) == null && c.GetSlotGroup(pawn.Map) == null
                                                            && GenGrid.InBounds(c, map), out newPosition2))
                                                        {
                                                            newZone.cells.Add(newPosition2);
                                                        }
                                                    }
                                                    newZone.zoneManager = pawn.Map.zoneManager;
                                                    //ZLogger.Message("Registering " + newZone + " in " + ZTracker.GetMapInfo(pawn.Map));
                                                    pawn.Map.zoneManager.RegisterZone(newZone);
                                                    //ZLogger.Message("Adding " + newZone + " to " + ZTracker.GetMapInfo(pawn.Map));
                                                    copiedZones.Add(newZone);
                                                }
                                            }
                                        }
        
                                        foreach (var t in pawn.Map.listerThings.AllThings.Where(x => x.def.EverHaulable))
                                        {
                                            pawn.Map.listerHaulables.RecalcAllInCell(t.Position);
                                            pawn.Map.listerMergeables.RecalcAllInCell(t.Position);
                                        }
        
                                        enumerable = pawn.Map.listerHaulables.ThingsPotentiallyNeedingHauling();
        
                                        foreach (var zone in copiedZones)
                                        {
                                            pawn.Map.zoneManager.DeregisterZone(zone);
                                        }
                                        //ZLogger.Message("--------------------", true);
        
                                    }
                                    else
                                    {
                                        enumerable = scanner.PotentialWorkThingsGlobal(pawn);
                                    }
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
                                        if (scanner is WorkGiver_HaulGeneral)
                                        {
                                            thing = GenClosest.ClosestThingReachable
                                                (pawn.Position, pawn.Map, scanner.PotentialWorkThingRequest,
                                                scanner.PathEndMode, TraverseParms.For(pawn, scanner.MaxPathDanger(pawn)),
                                                9999f, haulingValidator, enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch,
                                                enumerable != null);
                                            try
                                            {
                                                //if (enumerable != null)
                                                //{
                                                //    ZLogger.Message("Try get thing from enumerable: " + enumerable.Count(), true);
                                                //}
        
                                                if (thing != null)
                                                {
                                                    ZLogger.Message("Get thing: " + thing + " in " + thing.Map + " for " + pawn + " in " + ZTracker.GetMapInfo(pawn.Map), true);
                                                }
                                                else
                                                {
                                                    ZLogger.Message("Cant get thing for " + pawn + " in " + ZTracker.GetMapInfo(pawn.Map), true);
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                ZLogger.Message(ex.ToString());
                                            };
                                        }
        
                                        else if (scanner is WorkGiver_DoBill)
                                        {
                                            try
                                            {
                                                //thing = GenClosest.ClosestThingReachable
                                                //    (pawn.Position, pawn.Map, scanner.PotentialWorkThingRequest,
                                                //    scanner.PathEndMode, TraverseParms.For(pawn,
                                                //    scanner.MaxPathDanger(pawn)), 9999f, validator,
                                                //    enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch,
                                                //    enumerable != null);
                                                thing = GenClosest.ClosestThingReachable
                                                    (pawn.Position, pawn.Map, scanner.PotentialWorkThingRequest,
                                                    scanner.PathEndMode, TraverseParms.For(pawn,
                                                    scanner.MaxPathDanger(pawn)), 9999f, billValidator,
                                                    enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch,
                                                    enumerable != null);
                                                //ZLogger.Message("Get thing: " + thing + " in " + pawn.Map);
                                            }
                                            catch { };
        
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
                                            ZLogger.Message(pawn + " - " + ZTracker.GetMapInfo(pawn.Map) + " - " + scanner + " Selected thing: " + thing);
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
                            if (scannerWhoProvidedTarget is WorkGiver_HaulGeneral)
                            {
                                IntVec3 foundCell;
                                if (TryIssueJobPackagePatch.TryFindBestBetterStoreCellFor(bestTargetOfLastPriority.Thing,
                                    pawn, bestTargetOfLastPriority.Thing.Map, StoreUtility.CurrentStoragePriorityOf
                                    (bestTargetOfLastPriority.Thing), pawn.Faction, out foundCell, ref dest))
                                {
                                    job3 = JobMaker.MakeJob(JobDefOf.HaulToCell, bestTargetOfLastPriority.Thing, foundCell);
                                    job3.count = Mathf.Min(bestTargetOfLastPriority.Thing.stackCount,
                                        (int)(pawn.GetStatValue(StatDefOf.CarryingCapacity, true)
                                        / bestTargetOfLastPriority.Thing.def.VolumePerUnit));
                                    if (job3.count < 0)
                                    {
                                        job3.count = bestTargetOfLastPriority.Thing.stackCount;
                                    }
                                }
                                else
                                {
                                    ZLogger.Message("Cant haul " + bestTargetOfLastPriority.Thing + " in " + ZTracker.GetMapInfo(bestTargetOfLastPriority.Thing.Map), true);
                                }
                            }
                            else if (scannerWhoProvidedTarget is WorkGiver_DoBill)
                            {
                                job3 = null;
                                if (!bestTargetOfLastPriority.HasThing)
                                {
                                    job3 = scannerWhoProvidedTarget.JobOnCell(pawn, bestTargetOfLastPriority.Cell);
                                }
                                else
                                {
                                    var origMap = bestTargetOfLastPriority.Thing.Map;
                                    var origMap2 = pawn.Map;
                                    foreach (var tempMap in ZTracker.GetAllMaps(pawn.Map.Tile))
                                    {
                                        try
                                        {
                                            Traverse.Create(bestTargetOfLastPriority.Thing).Field("mapIndexOrState")
                                                .SetValue((sbyte)Find.Maps.IndexOf(tempMap));
                                            Traverse.Create(pawn).Field("mapIndexOrState")
                                                .SetValue((sbyte)Find.Maps.IndexOf(tempMap));
        
                                            ZLogger.Message("JobOnThing: " + bestTargetOfLastPriority.Thing + " searching in "
                                                + bestTargetOfLastPriority.Thing.Map);
        
                                            job3 = scannerWhoProvidedTarget.JobOnThing(pawn, bestTargetOfLastPriority.Thing);
                                            if (job3 != null)
                                            {
                                                break;
                                            }
                                            else
                                            {
                                                ZLogger.Message("No job in " + ZTracker.GetMapInfo(tempMap) + " for " + bestTargetOfLastPriority.Thing
                                                    + " - " + bestTargetOfLastPriority.Thing.Map);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Error("Z-Levels failed to process DoBill workgiver. Report about it to Z-Levels devs and provide Hugslib log. Error: " + ex, true);
                                        }
                                    }
        
                                    Traverse.Create(bestTargetOfLastPriority.Thing).Field("mapIndexOrState")
                                        .SetValue((sbyte)Find.Maps.IndexOf(origMap));
        
                                    Traverse.Create(pawn).Field("mapIndexOrState")
                                        .SetValue((sbyte)Find.Maps.IndexOf(origMap2));
                                }
                            }
                            else if (scannerWhoProvidedTarget is WorkGiver_ConstructDeliverResourcesToBlueprints scanner2)
                            {
                                job3 = (!bestTargetOfLastPriority.HasThing) ?
                                    scannerWhoProvidedTarget.JobOnCell(pawn, bestTargetOfLastPriority.Cell)
                                    : JobOnThing(scanner2, pawn, bestTargetOfLastPriority.Thing);
                            }
                            else if (scannerWhoProvidedTarget is WorkGiver_ConstructDeliverResourcesToFrames scanner3)
                            {
                                job3 = (!bestTargetOfLastPriority.HasThing) ?
                                    scannerWhoProvidedTarget.JobOnCell(pawn, bestTargetOfLastPriority.Cell)
                                    : JobOnThing(scanner3, pawn, bestTargetOfLastPriority.Thing);
                            }
                            else
                            {
                                job3 = (!bestTargetOfLastPriority.HasThing) ?
                                    scannerWhoProvidedTarget.JobOnCell(pawn, bestTargetOfLastPriority.Cell)
                                    : scannerWhoProvidedTarget.JobOnThing(pawn, bestTargetOfLastPriority.Thing);
                            }
        
                            if (job3 != null)
                            {
                                job3.workGiverDef = scannerWhoProvidedTarget.def;
                                if (dest == null)
                                {
                                    dest = otherMap;
                                }
                                return new ThinkResult(job3, instance, list[j].def.tagToGive);
                            }
                            //Log.ErrorOnce(string.Concat(scannerWhoProvidedTarget, " provided target ", bestTargetOfLastPriority, " but yielded no actual job for pawn ", pawn, ". The CanGiveJob and JobOnX methods may not be synchronized."), 6112651);
                        }
                        num = workGiver.def.priorityInType;
                    }
                }
                return ThinkResult.NoJob;
            }
        
            private static bool HaulShouldSkip(Pawn pawn, WorkGiver giver)
            {
                var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                foreach (var map in ZTracker.GetAllMaps(pawn.Map.Tile))
                {
                    if (map.listerHaulables.ThingsPotentiallyNeedingHauling().Count > 0)
                    {
                        return false;
                    }
                }
                return true;
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
                if (giver is WorkGiver_Haul)
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
