using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
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
using static Verse.AI.ReservationManager;

namespace ZLevels
{
    //[HarmonyPatch(typeof(LocalTargetInfo), "op_Implicit", new Type[] { typeof(IntVec3) })] // very terrible idea as it turned out
    //public class LocalTargetInfo_op_Implicit_Patch
    //{
    //    public static bool ignore;
    //    private static void Prefix()
    //    {
    //        Log.Message("Ignoring");
    //        ignore = true;
    //    }
    //    private static void Postfix()
    //    {
    //        ignore = false;
    //    }
    //}

    [HarmonyPatch(typeof(LocalTargetInfo), MethodType.Constructor, new Type[] { typeof(IntVec3) })]
    public class LocalTargetInfo_Constructor_Patch
    {
        public static JobTracker curPawnJobTracker;
        private static void Postfix(LocalTargetInfo __instance)
        {
            if (curPawnJobTracker != null && __instance.cellInt.IsValid)
            {
                curPawnJobTracker.lookedAtLocalCellMap[__instance.cellInt] = ZUtils.currentLookedIntoMap;
                //ZLogger.Message($"{curPawnJobTracker.pawn} is looking into {__instance.cellInt} - {ZUtils.currentLookedIntoMap}");//, stacktrace: {new StackTrace().ToString()}");
            }
        }
    }

    [HarmonyPatch(typeof(Reachability), "CanReach", new Type[] { typeof(IntVec3), typeof(LocalTargetInfo), typeof(PathEndMode), typeof(TraverseParms) })]
    public class CanReach_Patch
    {
        private static Map oldMap;
        private static IntVec3 oldPosition;
        private static bool Prefix(ref bool __result, Reachability __instance, IntVec3 start, LocalTargetInfo dest, PathEndMode peMode, TraverseParms traverseParams, out bool __state)
        {
            __state = false;
            var pawn = traverseParams.pawn;
            if (pawn != null)
            {
                if (dest.HasThing && dest.thingInt.Map != null)
                {
                    if (dest.thingInt.Map != pawn.Map)
                    {
                        var cell = ZUtils.GetCellToTeleportFrom(pawn.Map, pawn.Position, dest.thingInt.Map);
                        if (cell.IsValid)
                        {
                            __state = true;
                            oldMap = pawn.Map;
                            oldPosition = pawn.Position;
                            ZUtils.TeleportThing(pawn, dest.thingInt.Map, cell);
                            if (dest.thingInt.Map != __instance.map)
                            {
                                __result = dest.thingInt.Map.reachability.CanReach(start, dest, peMode, traverseParams);
                                ZLogger.Message($"CanReach: Used dest thing map reachability: __instance.map: {__instance.map}, pawn: {pawn}, thing: {dest.thingInt}, pawn.Map: {pawn.Map}, thing.Map: {dest.thingInt.Map}, result: {__result}");
                                return false;
                            }
                            else
                            {
                                ZLogger.Message($"CanReach: using instance map reachability: __instance.map: {__instance.map}, pawn: {pawn}, thing: {dest.thingInt}, pawn.Map: {pawn.Map}, thing.Map: {dest.thingInt.Map}, result: {__result}");
                            }
                        }
                        else
                        {
                            ZLogger.Pause($"CanReach: Detected reachability disfunction: pawn: {pawn}, thing: {dest.thingInt}, pawn.Map: {pawn.Map}, thing.Map: {dest.thingInt.Map}");
                        }
                    }
                    else if (dest.thingInt.Map != __instance.map)
                    {
                        __result = dest.thingInt.Map.reachability.CanReach(start, dest, peMode, traverseParams);
                        ZLogger.Message($"CanReach: Used dest thing map reachability: __instance.map: {__instance.map}, pawn: {pawn}, thing: {dest.thingInt}, pawn.Map: {pawn.Map}, thing.Map: {dest.thingInt.Map}, result: {__result}");
                        return false;
                    }
                }
                else if (ZUtils.ZTracker.jobTracker.TryGetValue(pawn, out var jobTracker) && jobTracker.lookedAtLocalCellMap.TryGetValue(dest.cellInt, out var mapDest))// jobTracker.mapDest != null)
                {
                    if (mapDest != pawn.Map)
                    {
                        var cell = ZUtils.GetCellToTeleportFrom(pawn.Map, pawn.Position, mapDest);
                        if (cell.IsValid)
                        {
                            __state = true;
                            oldMap = pawn.Map;
                            oldPosition = pawn.Position;
                            ZUtils.TeleportThing(pawn, mapDest, cell);
                            if (mapDest != __instance.map)
                            {
                                __result = mapDest.reachability.CanReach(start, dest, peMode, traverseParams);
                                ZLogger.Message($"CanReach: Used dest thing map reachability: __instance.map: {__instance.map}, pawn: {pawn}, mapDest: {mapDest}, pawn.Map: {pawn.Map}, thing.Map: {dest.thingInt.Map}, result: {__result}");
                                return false;
                            }
                        }
                        else
                        {
                            ZLogger.Pause($"3 CanReserve: Detected reservation disfunction: pawn: {pawn}, cell: {dest.cellInt}, pawn.Map: {pawn.Map}, mapDest: {mapDest}");
                        }
                    }
                    else if (mapDest != __instance.map)
                    {
                        __result = mapDest.reachability.CanReach(start, dest, peMode, traverseParams);
                        ZLogger.Message($"CanReach: Used dest thing map reachability: __instance.map: {__instance.map}, pawn: {pawn}, mapDest: {mapDest}, pawn.Map: {pawn.Map}, thing.Map: {dest.thingInt.Map}, result: {__result}");
                        return false;
                    }
                }
            }
            return true;
        }

        private static void Postfix(Reachability __instance, ref bool __result, IntVec3 start, LocalTargetInfo dest, PathEndMode peMode, TraverseParms traverseParams, bool __state)
        {
            if (__state && traverseParams.pawn != null)
            {
                //Log.Message("Postfix: dest: " + dest + " - dest.thingInt.Map: " + dest.thingInt?.Map + " - pawn.Map: " + traverseParams.pawn.Map + " - __instance: " + __instance.map);
                ZUtils.TeleportThing(traverseParams.pawn, oldMap, oldPosition);
            }

            //if (dest.HasThing)
            //{
            //    Log.Message($"{pawn} can reach {dest}: {__result}, {pawn.Map} - {dest.Thing.Map}");
            //}
        }
    }

    [HarmonyPatch(typeof(ReservationManager))]
    [HarmonyPatch("CanReserveStack")]
    public class ReservationManager_CanReserveStack_Patch
    {
        private static Map oldMap;
        private static void Prefix(ref int __result, ReservationManager __instance, Pawn claimant, LocalTargetInfo target, ref bool __state, int maxPawns = 1, ReservationLayerDef layer = null, bool ignoreOtherReservations = false)
        {
            if (target.HasThing && target.Thing.Map != claimant.Map)
            {
                oldMap = claimant.Map;
                ZUtils.TeleportThing(claimant, target.Thing.Map, claimant.Position);
                __state = true;
            }
        }

        private static void Postfix(ReservationManager __instance, Pawn claimant, LocalTargetInfo target, ref bool __state, int maxPawns = 1, ReservationLayerDef layer = null, bool ignoreOtherReservations = false)
        {
            if (__state)
            {
                ZUtils.TeleportThing(claimant, oldMap, claimant.Position);
            }
        }
    }
    [HarmonyPatch(typeof(ReservationManager))]
    [HarmonyPatch("CanReserve")]
    public class ReservationManager_CanReserve_Patch
    {
        private static Map oldMap;
        private static IntVec3 oldPosition;
        private static bool Prefix(ref bool __result, ReservationManager __instance, Pawn claimant, LocalTargetInfo target, out bool __state, int maxPawns = 1, int stackCount = -1, ReservationLayerDef layer = null, bool ignoreOtherReservations = false)
        {
            __state = false;
            if (claimant.RaceProps.Humanlike)
            {
                if (target.HasThing)
                {
                    if (target.thingInt.Map != null && target.thingInt.Map != claimant.Map)
                    {
                        var cell = ZUtils.GetCellToTeleportFrom(claimant.Map, claimant.Position, target.thingInt.Map);
                        if (cell.IsValid)
                        {
                            __state = true;
                            oldMap = claimant.Map;
                            oldPosition = claimant.Position;
                            ZUtils.TeleportThing(claimant, target.thingInt.Map, cell);
                            ZLogger.Message($"Teleporting claimaint {claimant} to {target.thingInt.Map}");
                            __result = claimant.CanReserve(target, maxPawns, stackCount, layer, ignoreOtherReservations);
                            return false;
                        }
                        else
                        {
                            ZLogger.Pause($"CanReserve: Detected reservation disfunction: pawn: {claimant}, thing: {target.thingInt}, pawn.Map: {claimant.Map}, thing.Map: {target.thingInt.Map}");
                        }
                    }
                }
                else
                {
                    if (ZUtils.ZTracker.jobTracker.TryGetValue(claimant, out var jobTracker) && jobTracker.lookedAtLocalCellMap.TryGetValue(target.cellInt, out var mapDest))
                    {
                        if (mapDest != null && mapDest != claimant.Map)
                        {
                            var cell = ZUtils.GetCellToTeleportFrom(claimant.Map, claimant.Position, mapDest);
                            if (cell.IsValid)
                            {
                                __state = true;
                                oldMap = claimant.Map;
                                oldPosition = claimant.Position;
                                ZUtils.TeleportThing(claimant, mapDest, cell);
                                ZLogger.Message($"3 Teleporting claimaint {claimant} to {mapDest}");
                                __result = claimant.CanReserve(target, maxPawns, stackCount, layer, ignoreOtherReservations);
                                return false;
                            }
                            else
                            {
                                ZLogger.Pause($"3 CanReserve: Detected reservation disfunction: pawn: {claimant}, thing: {target.thingInt}, pawn.Map: {claimant.Map}, thing.Map: {target.thingInt.Map}");
                            }
                        }
                    }
                    else
                    {
                        if (ZUtils.ZTracker.jobTracker.TryGetValue(claimant, out var job))
                        {
                            Log.Message("2 jobTracker.mapDest: " + job.mapDest);
                            Log.Message("2 jobTracker.lookedAtLocalCell: " + job.lookedAtLocalCellMap);
                        }
                        Log.Message("2 target: " + target);
                        ZLogger.Pause($"2 Unsupported target (most likely cell), claimant: {claimant}, target {target}");
                    }
                }
            }
            return true;
        }

        private static void Postfix(ref bool __result, Pawn claimant, LocalTargetInfo target, bool __state, int maxPawns = 1, int stackCount = -1, ReservationLayerDef layer = null, bool ignoreOtherReservations = false)
        {
            if (__state)
            {
                ZUtils.TeleportThing(claimant, oldMap, oldPosition);
            }
            if (__result)
            {
                var ZTracker = ZUtils.ZTracker;
                if (claimant.RaceProps.Humanlike && ZTracker.jobTracker != null)
                {
                    var thing = target.thingInt;
                    if (thing != null)
                    {
                        foreach (var data in ZTracker.jobTracker)
                        {
                            var otherPawn = data.Key;
                            var otherPawnJobTracker = data.Value;
                            if (otherPawn != claimant && otherPawnJobTracker.reservedThings != null)
                            {
                                foreach (var reservation in otherPawnJobTracker.reservedThings)
                                {
                                    if (reservation.thingInt == target.thingInt)
                                    {
                                        var shouldChangeResult = !(otherPawn.jobs?.curDriver is JobDriver_TakeToBed);// || !(data.Key.jobs?.curDriver is JobDriver_LayDown));
                                        if (shouldChangeResult)
                                        {
                                            ZLogger.Message(data.Key + " - 1 data.Value.mainJob: " + data.Value.mainJob);
                                            ZLogger.Message(data.Key + " - 1 data.Key.CurJob: " + data.Key.CurJob);
                                            ZLogger.Message(data.Key + " - 1 thing.Map.reservationManager.reservations.Any(x => x.Target.thingInt == thing && x.claimant == data.Key): " + thing.Map.reservationManager.reservations.Any(x => x.Target.thingInt == thing && x.claimant == data.Key));
                                            if (otherPawn.Map == thing.Map && !thing.Map.reservationManager.reservations.Any(x => x.Target.thingInt == thing && x.claimant == otherPawn))
                                            {
                                                ZLogger.Message($"PREVENTED ZTRACKER reservation disfunction: claimant: {claimant}, pawn: {data.Key}, thing: {thing}");
                                                continue;
                                            }
                                            __result = false;
                                            ZLogger.Message($"Detected ZTRACKER reservation disfunction: claimant: {claimant}, pawn: {data.Key}, thing: {thing}");
                                            return;
                                        }
                                    }
                                    //Log.Message($"ZTracker reservation: map: Reservation: {data.Key}, target: {reservation}, {data.Key.Map} - {reservation.Thing?.Map}");
                                }
                            }
                        }
                    }
                    else if (ZTracker.jobTracker.TryGetValue(claimant, out var jobTracker) && jobTracker.lookedAtLocalCellMap.TryGetValue(target.cellInt, out var mapDest))
                    {
                        var cell = target.cellInt;
                        foreach (var data in ZTracker.jobTracker)
                        {
                            var otherPawn = data.Key;
                            var otherPawnJobTracker = data.Value;
                            if (otherPawn != claimant && otherPawnJobTracker.reservedCells != null)
                            {
                                foreach (var reservation in otherPawnJobTracker.reservedCells)
                                {
                                    //ZLogger.Message($"otherPawn: {otherPawn}, reservation: {reservation}, reservation.HasThing: {reservation.HasThing}, reservation.cellInt: {reservation.cellInt}, data.Value.lookedAtMap: {data.Value.mapDest}, cell: {cell}, jobTracker.lookedAtMap: {jobTracker.mapDest}");
                                    if (reservation.cellInt == cell && otherPawnJobTracker.mapDest == jobTracker.mapDest)
                                    {
                                        //ZLogger.Message("claimant: " + claimant + " - " + otherPawn + " - 2 data.Value.mainJob: " + data.Value.mainJob);
                                        //ZLogger.Message("claimant: " + claimant + " - " + otherPawn + " - 2 data.Key.CurJob: " + data.Key.CurJob);
                                        //ZLogger.Message("claimant: " + claimant + " - " + otherPawn + " - 2 jobTracker.lookedAtMap.reservationManager.reservations.Any(x => !x.Target.HasThing && x.Target.cellInt == cell && x.claimant == data.Key)): " + jobTracker.mapDest.reservationManager.reservations.Any(x => !x.Target.HasThing && x.Target.cellInt == cell && x.claimant == data.Key));
                                        if (otherPawn.Map == jobTracker.mapDest && !jobTracker.mapDest.reservationManager.reservations.Any(x => !x.Target.HasThing && x.Target.cellInt == cell && x.claimant == otherPawn))
                                        {
                                            ZLogger.Message($"2 PREVENTED ZTRACKER reservation disfunction: claimant: {claimant}, pawn: {data.Key}, cell: {cell}");
                                            continue;
                                        }
                                        __result = false;
                                        ZLogger.Message($"2 Detected ZTRACKER reservation disfunction: claimant: {claimant}, pawn: {data.Key}, cell: {cell}");
                                        return;
                                    }
                                    //Log.Message($"ZTracker reservation: map: Reservation: {data.Key}, target: {reservation}, {data.Key.Map} - {reservation.Thing?.Map}");
                                }
                            }
                        }
                    }
                }
            }

            if (ZLogger.DebugEnabled)
            {
                if (!__result)
                {
                    ZLogger.Message($"claimant {claimant} can't reserve target: {target}");
                }
                //else
                //{
                //    ZLogger.Message($"claimant {claimant} can reserve target: {target}");
                //}
                //var ZTracker = ZUtils.ZTracker;
                //if (target.HasThing)
                //{
                //    foreach (var map in ZTracker.GetAllMaps(claimant.Map.Tile))
                //    {
                //        foreach (var reservation in map.reservationManager.reservations)
                //        {
                //            ZLogger.Message($"Vanilla reservation: map: {map}, Reservation: {reservation.claimant}, target: {reservation.target}, {reservation.claimant.Map} - {reservation.target.Thing?.Map}");
                //        }
                //    }
                //    if (ZTracker.jobTracker != null)
                //    {
                //        foreach (var data in ZTracker.jobTracker)
                //        {
                //            if (data.Value.reservedThings != null)
                //            {
                //                foreach (var reservation in data.Value.reservedThings)
                //                {
                //                    ZLogger.Message($"ZTracker reservation: map: Reservation: {data.Key}, target: {reservation}, {data.Key.Map} - {reservation.Thing?.Map}");
                //                }
                //            }
                //        }
                //    }
                //}
                //else
                //{
                //    foreach (var map in ZTracker.GetAllMaps(claimant.Map.Tile))
                //    {
                //        foreach (var reservation in map.reservationManager.reservations)
                //        {
                //            ZLogger.Message($"2 Vanilla reservation: map: {map}, claimant: {reservation.claimant}, target: {reservation.target}, {reservation.claimant.Map}");
                //        }
                //    }
                //    if (ZTracker.jobTracker != null)
                //    {
                //        foreach (var data in ZTracker.jobTracker)
                //        {
                //            if (data.Value.reservedThings != null)
                //            {
                //                foreach (var reservation in data.Value.reservedThings)
                //                {
                //                    ZLogger.Message($"2 ZTracker reservation: claimant: {data.Key}, target: {reservation}, pawn.Map: {data.Key.Map}, lookedAtLocalCell: {data.Value.lookedAtLocalCellMap}, lookedAtMap: {data.Value.mapDest}");
                //                }
                //            }
                //        }
                //    }
                //}
            }
        }
    }

    //[HarmonyPatch(typeof(ReservationManager))]
    //[HarmonyPatch("Reserve")]
    //public class ReservationManager_Patch_Reserve
    //{
    //    private static void Postfix(bool __result, Pawn claimant, Job job, LocalTargetInfo target, int maxPawns = 1, int stackCount = -1, ReservationLayerDef layer = null, bool errorOnFailed = true)
    //    {
    //        if (__result && claimant.RaceProps.Humanlike)
    //        {
    //            Log.Message($"{claimant} is reserving {target}: {__result}, {claimant.Map} - {target.Thing?.Map}");
    //            var ZTracker = ZUtils.ZTracker;
    //            foreach (var map in ZTracker.GetAllMaps(claimant.Map.Tile))
    //            {
    //                foreach (var reservation in map.reservationManager.reservations)
    //                {
    //                    Log.Message($"Vanilla reservation: map: {map}, Reservation: {reservation.claimant}, target: {reservation.target}, {reservation.claimant.Map} - {reservation.target.Thing?.Map}");
    //                }
    //            }
    //            if (ZTracker.jobTracker != null)
    //            {
    //                foreach (var data in ZTracker.jobTracker)
    //                {
    //                    if (data.Value.reservedThings != null)
    //                    {
    //                        foreach (var reservation in data.Value.reservedThings)
    //                        {
    //                            Log.Message($"ZTracker reservation: map: Reservation: {data.Key}, target: {reservation}, {data.Key.Map} - {reservation.Thing?.Map}");
    //                        }
    //                    }
    //                }
    //            }
    //
    //            Log.Message($"---------------------");
    //
    //        }
    //    }
    //}
}