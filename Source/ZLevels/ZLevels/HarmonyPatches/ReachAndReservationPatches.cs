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
            if (pawn != null && pawn.Map != null)
            {
                if (dest.HasThing && dest.thingInt.Map != null)
                {
                    if (dest.thingInt.Map != pawn.Map)
                    {
                        var cellToTeleport = ZUtils.GetCellToTeleportFrom(pawn.Map, pawn.Position, dest.thingInt.Map);
                        var cell = cellToTeleport.IsValid ? cellToTeleport : pawn.Position;
    
                        __state = true;
                        oldMap = pawn.Map;
                        oldPosition = pawn.Position;
                        ZUtils.TeleportThing(pawn, dest.thingInt.Map, cell);
                        if (dest.thingInt.Map != __instance.map)
                        {
                            __result = dest.thingInt.Map.reachability.CanReach(start, dest, peMode, traverseParams);
                            ZLogger.Message($"1 CanReach: Used dest thing map reachability: __instance.map: {__instance.map}, pawn: {pawn}, thing: {dest.thingInt}, pawn.Map: {pawn.Map}, thing.Map: {dest.thingInt.Map}, result: {__result}");
                            return false;
                        }
                    }
                    else if (dest.thingInt.Map != __instance.map)
                    {
                        __result = dest.thingInt.Map.reachability.CanReach(start, dest, peMode, traverseParams);
                        ZLogger.Message($"2 CanReach: Used dest thing map reachability: __instance.map: {__instance.map}, pawn: {pawn}, thing: {dest.thingInt}, pawn.Map: {pawn.Map}, thing.Map: {dest.thingInt.Map}, result: {__result}");
                        return false;
                    }
                }
                else if (ZUtils.ZTracker.jobTracker.TryGetValue(pawn, out var jobTracker) && jobTracker.lookedAtLocalCellMap != null
                    && jobTracker.lookedAtLocalCellMap.TryGetValue(dest.cellInt, out var mapDest) && mapDest != null)
                {
                    if (mapDest != pawn.Map)
                    {
                        var cellToTeleport = ZUtils.GetCellToTeleportFrom(pawn.Map, pawn.Position, mapDest);
                        var cell = cellToTeleport.IsValid ? cellToTeleport : pawn.Position;
    
                        __state = true;
                        oldMap = pawn.Map;
                        oldPosition = pawn.Position;
                        ZUtils.TeleportThing(pawn, mapDest, cell);
                        if (mapDest != __instance.map)
                        {
                            __result = mapDest.reachability.CanReach(start, dest, peMode, traverseParams);
                            ZLogger.Message($"3 CanReach: Used mapDest map reachability: __instance.map: {__instance.map}, pawn: {pawn}, mapDest: {mapDest}, pawn.Map: {pawn.Map}, result: {__result}");
                            return false;
                        }
                    }
                    else if (mapDest != __instance.map)
                    {
                        __result = mapDest.reachability.CanReach(start, dest, peMode, traverseParams);
                        ZLogger.Message($"4 CanReach: Used mapDest map reachability: __instance.map: {__instance.map}, pawn: {pawn}, mapDest: {mapDest}, pawn.Map: {pawn.Map}, result: {__result}");
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
                ZUtils.TeleportThing(traverseParams.pawn, oldMap, oldPosition);
            }
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
            if (claimant.RaceProps.Humanlike && claimant.Map != null)
            {
                if (target.HasThing)
                {
                    if (target.thingInt.Map != null && target.thingInt.Map != claimant.Map)
                    {
                        var cellToTeleport = ZUtils.GetCellToTeleportFrom(claimant.Map, claimant.Position, target.thingInt.Map);
                        var cell = cellToTeleport.IsValid ? cellToTeleport : claimant.Position;
    
                        __state = true;
                        oldMap = claimant.Map;
                        oldPosition = claimant.Position;
                        ZUtils.TeleportThing(claimant, target.thingInt.Map, cell);
                        __result = claimant.CanReserve(target, maxPawns, stackCount, layer, ignoreOtherReservations);
                        return false;
                    }
                }
                else
                {
                    if (ZUtils.ZTracker.jobTracker.TryGetValue(claimant, out var jobTracker) && jobTracker.lookedAtLocalCellMap != null
                        && jobTracker.lookedAtLocalCellMap.TryGetValue(target.cellInt, out var mapDest))
                    {
                        if (mapDest != null && mapDest != claimant.Map)
                        {
                            var cellToTeleport = ZUtils.GetCellToTeleportFrom(claimant.Map, claimant.Position, mapDest);
                            var cell = cellToTeleport.IsValid ? cellToTeleport : claimant.Position;
    
                            __state = true;
                            oldMap = claimant.Map;
                            oldPosition = claimant.Position;
                            ZUtils.TeleportThing(claimant, mapDest, cell);
                            __result = claimant.CanReserve(target, maxPawns, stackCount, layer, ignoreOtherReservations);
                            return false;
                        }
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
                                            try
                                            {
                                                ZLogger.Message("reservation.thingInt: " + reservation.thingInt + " - target.thingInt: " + target.thingInt);
                                                ZLogger.Message(data.Key + " - 1 data.Value.mainJob: " + data.Value.mainJob);
                                                ZLogger.Message(data.Key + " - 1 data.Key.CurJob: " + data.Key.CurJob);
                                                ZLogger.Message(data.Key + " - 1 thing.Map.reservationManager.reservations.Any(x => x.Target.thingInt == thing && x.claimant == data.Key): " + thing.Map.reservationManager.reservations.Any(x => x.Target.thingInt == thing && x.claimant == data.Key));
                                            }
                                            catch
                                            {
    
                                            }
                                           if (otherPawn.Map == thing.Map && !thing.Map.reservationManager.reservations.Any(x => x.Target.thingInt == thing && x.claimant == otherPawn))
                                            {
                                                ZLogger.Message($"PREVENTED ZTRACKER reservation disfunction: claimant: {claimant}, pawn: {data.Key}, thing: {thing}");
                                                continue;
                                            }
                                            __result = false;
                                            ZLogger.Message($"shouldChangeResult Detected ZTRACKER reservation disfunction: claimant: {claimant}, pawn: {data.Key}, thing: {thing}");
                                            return;
                                        }
                                    }
                                    //Log.Message($"ZTracker reservation: map: Reservation: {data.Key}, target: {reservation}, {data.Key.Map} - {reservation.Thing?.Map}");
                                }
                            }
                        }
                    }
                    else if (ZTracker.jobTracker.TryGetValue(claimant, out var jobTracker) && jobTracker.lookedAtLocalCellMap.TryGetValue(target.cellInt, out var mapDest) && mapDest != null)
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
                                    if (reservation.cellInt == cell && otherPawnJobTracker.mapDest == jobTracker.mapDest)
                                    {
                                        try
                                        {
                                            ZLogger.Message("claimant: " + claimant + " - " + otherPawn + " - 2 data.Value.mainJob: " + data.Value.mainJob);
                                            ZLogger.Message("claimant: " + claimant + " - " + otherPawn + " - 2 data.Key.CurJob: " + data.Key.CurJob);
                                            ZLogger.Message("claimant: " + claimant + " - " + otherPawn + " - 2 jobTracker.lookedAtMap.reservationManager.reservations.Any(x => !x.Target.HasThing && x.Target.cellInt == cell && x.claimant == data.Key)): " + jobTracker.mapDest.reservationManager.reservations.Any(x => !x.Target.HasThing && x.Target.cellInt == cell && x.claimant == data.Key));
                                        }
                                        catch { }
                                        if (otherPawn.Map == jobTracker.mapDest && !jobTracker.mapDest.reservationManager.reservations.Any(x => !x.Target.HasThing && x.Target.cellInt == cell && x.claimant == otherPawn))
                                        {
                                            ZLogger.Message($"2 PREVENTED ZTRACKER reservation disfunction: claimant: {claimant}, pawn: {data.Key}, cell: {cell}");
                                            continue;
                                        }
                                        __result = false;
                                        ZLogger.Message($"reservation.cellInt == cell 2 Detected ZTRACKER reservation disfunction: claimant: {claimant}, pawn: {data.Key}, cell: {cell}");
                                        return;
                                    }
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
                    ZLogger.Error($"claimant {claimant} can't reserve target: {target}");
                }
            }
        }
    }
}