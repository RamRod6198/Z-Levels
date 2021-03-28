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
using static Verse.AI.ReservationManager;

namespace ZLevels
{
    [HarmonyPatch(typeof(ReachabilityUtility), "CanReach")]
    public class CanReach_Patch
    {
        private static Map oldMap;
        private static IntVec3 oldPosition;
        private static void Prefix(Pawn pawn, LocalTargetInfo dest, PathEndMode peMode, Danger maxDanger, out bool __state, bool canBash = false, TraverseMode mode = TraverseMode.ByPawn)
        {
            __state = false;
            if (pawn.RaceProps.Humanlike)
            {
                if (dest.HasThing && dest.thingInt.Map != pawn.Map)
                {
                    var cell = ZUtils.GetCellToTeleportFrom(pawn.Map, pawn.Position, dest.thingInt.Map);
                    if (cell.IsValid)
                    {
                        __state = true;
                        oldMap = pawn.Map;
                        oldPosition = pawn.Position;
                        ZUtils.TeleportThing(pawn, dest.thingInt.Map, cell);
                    }
                    else
                    {
                        ZLogger.Pause($"CanReach: Detected reachability disfunction: pawn: {pawn}, thing: {dest.thingInt}, pawn.Map: {pawn.Map}, thing.Map: {dest.thingInt.Map}");
                    }
                }
            }
        }
        private static void Postfix(ref bool __result, Pawn pawn, LocalTargetInfo dest, PathEndMode peMode, Danger maxDanger, bool __state, bool canBash = false, TraverseMode mode = TraverseMode.ByPawn)
        {
            if (__state)
            {
                ZUtils.TeleportThing(pawn, oldMap, oldPosition);
            }

            //if (dest.HasThing)
            //{
            //    Log.Message($"{pawn} can reach {dest}: {__result}, {pawn.Map} - {dest.Thing.Map}");
            //}
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
                if (target.HasThing && target.thingInt.Map != null && target.thingInt.Map != claimant.Map)
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
                var thing = target.Thing;
                if (thing != null)
                {
                    if (claimant.RaceProps.Humanlike && target.HasThing)
                    {
                        var ZTracker = ZUtils.ZTracker;
                        //foreach (var map in ZTracker.GetAllMaps(claimant.Map.Tile))
                        //{
                        //    foreach (var reservation in map.reservationManager.reservations)
                        //    {
                        //        Log.Message($"map: {map}, Reservation: {reservation.claimant}, target: {reservation.target}, {reservation.claimant.Map} - {reservation.target.Thing?.Map}");
                        //    }
                        //}

                        if (ZTracker.jobTracker != null)
                        {
                            foreach (var data in ZTracker.jobTracker)
                            {

                                if (data.Key != claimant && data.Value.reservedThings != null)
                                {
                                    foreach (var reservation in data.Value.reservedThings)
                                    {
                                        if (reservation.HasThing && reservation.thingInt == target.thingInt)
                                        {
                                            var shouldChangeResult = !(data.Key.jobs?.curDriver is JobDriver_TakeToBed);
                                            if (shouldChangeResult)
                                            {
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
                        //Log.Message($"---------------------");
                    }
                    //Log.Message($"{claimant} can reserve {target}: {__result}, {claimant.Map} - {target.Thing?.Map}");

                    //if (claimant.Map != thing.Map)
                    //{
                    //    ZLogger.Pause($"CanReserve: {__result}, Detected reservation disfunction: claimant.Map != thing.Map, claimant: {claimant}, thing: {thing}");
                    //    var ZTracker = ZUtils.ZTracker;
                    //    foreach (var map in ZTracker.GetAllMaps(thing.Map.Tile))
                    //    {
                    //        var pawn = map.reservationManager.FirstRespectedReserver(target, claimant);
                    //        if (pawn != null && pawn != claimant)
                    //        {
                    //            ZLogger.Pause($"CanReserve: {__result}, Detected reservation disfunction: claimant: {claimant}, pawn: {pawn}, thing: {thing}");
                    //        }
                    //    }
                    //}
                    //else
                    //{
                    //    var ZTracker = ZUtils.ZTracker;
                    //    foreach (var map in ZTracker.GetAllMaps(thing.Map.Tile))
                    //    {
                    //        var pawn = map.reservationManager.FirstRespectedReserver(target, claimant);
                    //        if (pawn != null && pawn != claimant)
                    //        {
                    //            ZLogger.Pause($"CanReserve: {__result}, Detected other claimant: first claimant: {claimant}, second claimant: {pawn}, thing: {thing}");
                    //        }
                    //    }
                    //}
                }
            }
            else if (ZLogger.DebugEnabled)
            {
                ZLogger.Message($"claimant {claimant} can't reserve target: {target}");
                //var ZTracker = ZUtils.ZTracker;
                //if (target.HasThing)
                //{
                //    foreach (var map in ZTracker.GetAllMaps(claimant.Map.Tile))
                //    {
                //        foreach (var reservation in map.reservationManager.reservations)
                //        {
                //            Log.Message($"Vanilla reservation: map: {map}, Reservation: {reservation.claimant}, target: {reservation.target}, {reservation.claimant.Map} - {reservation.target.Thing?.Map}");
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
                //                    Log.Message($"ZTracker reservation: map: Reservation: {data.Key}, target: {reservation}, {data.Key.Map} - {reservation.Thing?.Map}");
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