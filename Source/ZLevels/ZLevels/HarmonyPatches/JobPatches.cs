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
        //            Log.Message("Patch: " + type);
        //        }
        //
        //        catch (Exception ex)
        //        {
        //            Log.Message("Error patching: " + ex);
        //        }
        //        //Log.Message("Method 1: " + method);
        //        //MethodInfo getMethod = type.GetMethod("TryGiveJob");
        //        //try
        //        //{
        //        //    Log.Message("Method: " + getMethod);
        //        //    new Harmony("test.test.tst").Patch(getMethod, null, new HarmonyMethod(method), null, null);
        //        //    ZLogger.Message("Patching: " + type);
        //        //}
        //        //catch (Exception ex)
        //        //{
        //        //    Log.Message("Error patching: " + ex);
        //        //}
        //
        //        //MethodInfo getMethod2 = type.GetProperty("PotentialWorkThingRequest").GetGetMethod(false);
        //        //try
        //        //{
        //        //    new Harmony("test.test.tst").Patch(getMethod2, null, new HarmonyMethod(method2), null, null);
        //        //    ZLogger.Message("Patching: " + type);
        //        //}
        //        //catch { }
        //    }
        //
        //}
        //
        //public static void LogScanner()
        //{
        //    Log.Message("Test");
        //    //if (__result != null)
        //    //{
        //    //    ZLogger.Message(pawn + " - " + __result);
        //    //}
        //    //else
        //    //{
        //    //    ZLogger.Message(pawn + " - null job");
        //    //}
        //}
        
        //public static void LogScanner2(ThingRequest __result, WorkGiver_Scanner __instance)
        //{
        //    ZLogger.Message(__instance.def + " - __result: " + __result);
        //    //throw new Exception("TEST");
        //}
        //
        //
        //[HarmonyPatch(typeof(JobQueue), "EnqueueFirst")]
        //internal static class Patch_JobQueue
        //{
        //    private static void Postfix(Job j, JobTag? tag = null)
        //    {
        //        ZLogger.Message("Switching to " + j, true);
        //    }
        //}

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

        [HarmonyPatch(typeof(JobGiver_Work), "TryIssueJobPackage")]
        public class JobManagerPatch
        {
            [HarmonyPostfix]
            private static void TryIssueJobPackagePostfix(JobGiver_Work __instance, ref ThinkResult __result, Pawn pawn, JobIssueParams jobParams)
            {
                try
                {
                    var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                    if (__result.Job == null && ZTracker?.ZLevelsTracker[pawn.Map.Tile]?.ZLevels?.Count > 1)
                    {
                        if (ZTracker.jobTracker == null)
                        {
                            ZTracker.jobTracker = new Dictionary<Pawn, JobTracker>();
                        }
                        if (ZTracker.jobTracker.ContainsKey(pawn))
                        {
                            if (Find.TickManager.TicksGame - ZTracker.jobTracker[pawn].lastTick < 200)
                            // minimal job interval check per pawn is 200 ticks
                            {
                                return;
                            }
                            try
                            {
                                if (ZTracker.jobTracker[pawn]?.activeJobs?.Count() > 0)
                                {
                                    if (!pawn.jobs.jobQueue.Contains(ZTracker.jobTracker[pawn].activeJobs[0]))
                                    {
                                        ZLogger.Message("Queue: " + ZTracker.jobTracker[pawn].activeJobs[0]);
                                        pawn.jobs.jobQueue.EnqueueLast(ZTracker.jobTracker[pawn].activeJobs[0]);
                                        return;
                                    }
                                    else if (pawn.jobs.jobQueue[0].job.targetA.Thing.Map != pawn.Map)
                                    {
                                        ZTracker.ResetJobTrackerFor(pawn);
                                    }
                                    else
                                    {
                                        return;
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
                        if (Find.TickManager.TicksGame - ZTracker.jobTracker[pawn].lastTick < 200)
                        // minimal job interval check per pawn is 200 ticks
                        {
                            return;
                        }
                        ThinkResult result;
                        var oldMap = pawn.Map;
                        var oldPosition = pawn.Position;
                        //ZLogger.Message("======================================");
                        foreach (var otherMap in ZTracker.ZLevelsTracker[pawn.Map.Tile].ZLevels.Values)
                        {
                            if (Find.TickManager.TicksGame - ZTracker.jobTracker[pawn].lastTick < 200)
                            // minimal job interval check per pawn is 200 ticks
                            {
                                return;
                            }
                            var stairs = new List<Thing>();

                            ZLogger.Message("Searching job for " + pawn + " in " + ZTracker.GetMapInfo(otherMap)
                                + " for " + ZTracker.GetMapInfo(oldMap));

                            if (ZTracker.GetZIndexFor(otherMap) > ZTracker.GetZIndexFor(oldMap))
                            {
                                Map lowerMap = ZTracker.GetLowerLevel(otherMap.Tile, otherMap);
                                if (lowerMap != null)
                                {
                                    ZLogger.Message("Searching stairs up in " + ZTracker.GetMapInfo(otherMap));
                                    stairs = lowerMap.listerThings.AllThings.Where(x => x is Building_StairsUp).ToList();
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
                                    stairs = upperMap.listerThings.AllThings.Where(x => x is Building_StairsDown).ToList();
                                }
                                else
                                {
                                    ZLogger.Message("Upper map is null in " + ZTracker.GetMapInfo(otherMap));
                                }
                            }
                            if (Find.TickManager.TicksGame - ZTracker.jobTracker[pawn].lastTick < 200)
                            // minimal job interval check per pawn is 200 ticks
                            {
                                return;
                            }
                            bool goToOtherMap = false;
                            bool select = false;

                            if (stairs != null && stairs.Count() > 0)
                            {
                                var selectedStairs = GenClosest.ClosestThing_Global(pawn.Position, stairs, 99999f);
                                var position = selectedStairs.Position;

                                if (Find.Selector.SelectedObjects.Contains(pawn)) select = true;
                                JobManagerPatches.manualDespawn = true;
                                pawn.DeSpawn();
                                JobManagerPatches.manualDespawn = false;
                                GenPlace.TryPlaceThing(pawn, position, otherMap, ThingPlaceMode.Direct);
                                goToOtherMap = true;
                            }

                            //try
                            //{
                            //    ZLogger.Message("102 Find.TickManager.TicksGame: " + Find.TickManager.TicksGame);
                            //    ZLogger.Message("102 ZTracker.jobTracker[pawn].lastTick: " + ZTracker.jobTracker[pawn].lastTick);
                            //    ZLogger.Message("102 Result: " + (Find.TickManager.TicksGame - ZTracker.jobTracker[pawn].lastTick));
                            //}
                            //catch { };
                            Map dest = null;
                            result = TryIssueJobPackage(pawn, jobParams, __instance, __instance.emergency, ref dest);
                            if (result.Job != null)
                            {
                                ZLogger.Message("TryIssueJobPackage: " + pawn + " - map: " + ZTracker.GetMapInfo(pawn.Map)
                                    + " - " + pawn.Position + " result " + result.Job);

                                if (goToOtherMap)
                                {
                                    JobManagerPatches.manualDespawn = true;
                                    pawn.DeSpawn();
                                    JobManagerPatches.manualDespawn = false;
                                    GenPlace.TryPlaceThing(pawn, oldPosition, oldMap, ThingPlaceMode.Direct);
                                    if (select) Find.Selector.Select(pawn);
                                }

                                if (dest != null)
                                {
                                    ZTracker.BuildJobListFor(pawn, oldMap, dest, result.Job, null);
                                }
                                else
                                {
                                    ZTracker.BuildJobListFor(pawn, oldMap, otherMap, result.Job, null);
                                }
                                break;
                            }
                            else if (goToOtherMap)
                            {
                                JobManagerPatches.manualDespawn = true;
                                pawn.DeSpawn();
                                JobManagerPatches.manualDespawn = false;
                                GenPlace.TryPlaceThing(pawn, oldPosition, oldMap, ThingPlaceMode.Direct);
                                if (select) Find.Selector.Select(pawn);
                            }
                        
                        }
                    }
                    try
                    {
                        ZTracker.jobTracker[pawn].lastTick = Find.TickManager.TicksGame;
                    }
                    catch { }
                }
                catch (Exception ex)
                {
                    ZLogger.Error("Some kind of error occurred in Z-Levels JobManager: " + ex);
                }
            }

            public static bool TryFindBestBetterStoreCellForValidator(Thing t, Pawn carrier, Map mapToSearch,
                StoragePriority currentPriority, Faction faction, out IntVec3 foundCell, bool needAccurateResult = true)
            {
                Map temp = null;
                return TryFindBestBetterStoreCellFor(t, carrier, mapToSearch, currentPriority, faction, out foundCell, ref temp);
            }

            public static bool TryFindBestBetterStoreCellFor(Thing t, Pawn carrier, Map mapToSearch, StoragePriority currentPriority, Faction faction, out IntVec3 foundCell, ref Map dest, bool needAccurateResult = true)
            {
                Zone_Stockpile zoneDest = null;
                bool result = true;
                //ZLogger.Message(t + " priority: " + currentPriority + " - checking on " + carrier + " _ " + mapToSearch +
                //    " --------------------------------------- start", true);

                var allZones = new Dictionary<Zone_Stockpile, Map>();
                var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
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
                for (int i = 0; i < count; i++)
                {
                    SlotGroup slotGroup = allGroupsListInPriorityOrder[i];
                    //ZLogger.Message("Checking: " + carrier + " - " + carrier.Map + " - " + slotGroup.parent + " - " + slotGroup.parent.Map);
                    StoragePriority priority = slotGroup.Settings.Priority;
                    if (priority < storagePriority || priority <= currentPriority)
                    {
                        break;
                    }
                    zoneDest = slotGroup.parent as Zone_Stockpile;
                    JobManagerPatch.TryFindBestBetterStoreCellForWorker
                        (t, carrier, mapToSearch, faction, slotGroup, needAccurateResult,
                        ref invalid, ref num, ref storagePriority, slotGroup.parent.Map);
                }

                if (!invalid.IsValid)
                {
                    foundCell = IntVec3.Invalid;
                    result = false;
                }
                else
                {
                    dest = zoneDest.Map;
                    try
                    {
                        ZLogger.Message("RESULT: " + carrier + " - " + zoneDest + " - " + ZTracker.GetMapInfo(zoneDest.Map) + " accepts " + t, true);

                    }
                    catch (Exception ex)
                    {
                        ZLogger.Message("Cant find result: " + ex);
                    }
                }
                foundCell = invalid;

                return result;
            }

            private static void TryFindBestBetterStoreCellForWorker(Thing t, Pawn carrier, Map map, Faction faction, SlotGroup slotGroup, bool needAccurateResult, ref IntVec3 closestSlot, ref float closestDistSquared, ref StoragePriority foundPriority, Map oldMap)
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
                //ZLogger.Message(t + " map " + t.Map + " - " + slotGroup.parent + " map " + slotGroup.parent.Map, true);
                //foreach (var test in slotGroup.HeldThings)
                //{
                //    ZLogger.Message(test + " in " + slotGroup.parent, true);
                //}

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
                    float num2 = (float)(a - intVec).LengthHorizontalSquared;
                    if (num2 <= closestDistSquared && IsGoodStoreCell(intVec, map, t, carrier, faction, oldMap))
                    {
                        closestSlot = intVec;
                        closestDistSquared = num2;
                        foundPriority = slotGroup.Settings.Priority;
                        if (i >= num)
                        {
                            break;
                        }
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
                if (billGiver == null || !scanner.ThingIsUsableBillGiver(thing) || !billGiver.BillStack.AnyShouldDoNow || !billGiver.UsableForBillsAfterFueling() || !pawn.CanReserve(thing, 1, -1, null, forced) || thing.IsBurning() || thing.IsForbidden(pawn))
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
                    bool select = false;
                    foreach (var map in ZTracker.GetAllMaps(pawn.Map.Tile))
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
            public static ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams, JobGiver_Work instance, bool emergency, ref Map dest)
            {
                if (emergency && pawn.mindState.priorityWork.IsPrioritized)
                {
                    List<WorkGiverDef> workGiversByPriority = pawn.mindState.priorityWork.WorkGiver.workType.workGiversByPriority;
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
                for (int j = 0; j < list.Count; j++)
                {

                    WorkGiver workGiver = list[j];
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

                                Predicate<Thing> billValidator = (Thing t) => !t.IsForbidden(pawn) &&
                                JobManagerPatch.HasJobOnThing((WorkGiver_DoBill)scanner, pawn, t) != null;

                                IntVec3 foundCell;
                                Predicate<Thing> haulingValidator = (Thing t) => !t.IsForbidden(pawn)
                                && pawn?.carryTracker?.AvailableStackSpace(t?.def) > 0
                                && pawn?.carryTracker?.MaxStackSpaceEver(t?.def) > 0
                                && pawn.CanReserve(t, 1, -1, null, false)
                                && JobManagerPatch.TryFindBestBetterStoreCellForValidator(t,
                                pawn, t.Map, StoreUtility.CurrentStoragePriorityOf(t), pawn.Faction,
                                out foundCell, true);

                                IEnumerable<Thing> enumerable;

                                if (scanner is WorkGiver_HaulGeneral)
                                {
                                    var allZones = new Dictionary<Zone_Stockpile, Map>();
                                    var allMaps = ZTracker.GetAllMaps(pawn.Map.Tile);
                                    foreach (Map map in allMaps)
                                    {
                                        foreach (var zone in map.zoneManager.AllZones.Where(x => x is Zone_Stockpile))
                                        {
                                            allZones[(Zone_Stockpile)zone] = map;
                                        }
                                    }
                                    
                                    List<Zone_Stockpile> copiedZones = new List<Zone_Stockpile>();
                                    
                                    foreach (Map map in allMaps)
                                    {
                                        foreach (var zone in allZones)
                                        {
                                            if (zone.Key.Map != pawn.Map)
                                            {
                                                var newZone = new Zone_Stockpile();
                                                newZone.settings = zone.Key.settings;
                                                foreach (IntVec3 intVec in zone.Key.cells)
                                                {
                                                    var newPosition = (intVec - zone.Key.Position) + pawn.Position;
                                                    newZone.cells.Add(newPosition);
                                                }
                                                newZone.zoneManager = pawn.Map.zoneManager;
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
                                        try
                                        {
                                            ZLogger.Message("Try get thing from enumerable: " + enumerable.Count(), true);
                                            thing = GenClosest.ClosestThingReachable
                                                (pawn.Position, pawn.Map, scanner.PotentialWorkThingRequest,
                                                scanner.PathEndMode, TraverseParms.For(pawn, scanner.MaxPathDanger(pawn)),
                                                9999f, null, enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch,
                                                enumerable != null);
                                            if (thing != null)
                                            {
                                                ZLogger.Message("Get thing: " + thing + " in " + thing.Map + " for " + pawn + " in " + ZTracker.GetMapInfo(pawn.Map), true);
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
                        ZLogger.Error(string.Concat(pawn, " threw exception in WorkGiver ", workGiver.def.defName, ": ", ex.ToString()));
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
                            if (JobManagerPatch.TryFindBestBetterStoreCellFor(bestTargetOfLastPriority.Thing,
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
                                ZLogger.Message(pawn + " tasked to haul " + bestTargetOfLastPriority.Thing + 
                                    " to " + foundCell + " to dest " + ZTracker.GetMapInfo(dest), true);
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
                                    Traverse.Create(bestTargetOfLastPriority.Thing).Field("mapIndexOrState")
                                        .SetValue((sbyte)Find.Maps.IndexOf(tempMap));
                                    Traverse.Create(pawn).Field("mapIndexOrState")
                                        .SetValue((sbyte)Find.Maps.IndexOf(tempMap));
                                    ZLogger.Message("JobOnThing: " + bestTargetOfLastPriority.Thing + " searching in "
                                        + bestTargetOfLastPriority.Thing.Map);
                                    job3 = scannerWhoProvidedTarget.JobOnThing(pawn, bestTargetOfLastPriority.Thing);
                                    if (job3 != null)
                                    {
                                        foreach (var t in job3.targetQueueB)
                                        {
                                            ZLogger.Message(ZTracker.GetMapInfo(tempMap) + " - Job2: " + job3 + " target: " + t);
                                        }
                                        break;
                                    }
                                    else
                                    {
                                        ZLogger.Message("No job in " + ZTracker.GetMapInfo(tempMap) + " for " + bestTargetOfLastPriority.Thing
                                            + " - " + bestTargetOfLastPriority.Thing.Map);
                                    }
                                }
                                Traverse.Create(bestTargetOfLastPriority.Thing).Field("mapIndexOrState")
                                    .SetValue((sbyte)Find.Maps.IndexOf(origMap));

                                Traverse.Create(pawn).Field("mapIndexOrState")
                                    .SetValue((sbyte)Find.Maps.IndexOf(origMap2));
                            }
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
                            return new ThinkResult(job3, instance, list[j].def.tagToGive);
                        }
                        //ZLogger.ErrorOnce(string.Concat(scannerWhoProvidedTarget, " provided target ", bestTargetOfLastPriority, " but yielded no actual job for pawn ", pawn, ". The CanGiveJob and JobOnX methods may not be synchronized."), 6112651);
                    }
                    num = workGiver.def.priorityInType;
                }
                return ThinkResult.NoJob;
            }

            private static bool PawnCanUseWorkGiver(Pawn pawn, WorkGiver giver)
            {
                if (!giver.def.nonColonistsCanDo && !pawn.IsColonist)
                {
                    return false;
                }
                if (pawn.WorkTagIsDisabled(giver.def.workTags))
                {
                    return false;
                }
                if (giver.ShouldSkip(pawn))
                {
                    return false;
                }
                if (giver.MissingRequiredCapacity(pawn) != null)
                {
                    return false;
                }
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
                    ZLogger.Error(string.Concat(pawn, " threw exception in GiverTryGiveJobTargeted on WorkGiver ", giver.def.defName, ": ", ex.ToString()));
                }
                return null;
            }
        }
    }
}

