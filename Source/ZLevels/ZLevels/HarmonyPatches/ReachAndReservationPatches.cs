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
                if (pawn.RaceProps.Humanlike)
                {
                    if (dest.HasThing && dest.thingInt.Map != null && dest.thingInt.Map != pawn.Map)
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
                                ZLogger.Message($"CanReach: Used other's map reachability: pawn: {pawn}, thing: {dest.thingInt}, pawn.Map: {pawn.Map}, thing.Map: {dest.thingInt.Map}, result: {__result}");
                                return false;
                            }
                        }
                        else
                        {
                            ZLogger.Pause($"CanReach: Detected reachability disfunction: pawn: {pawn}, thing: {dest.thingInt}, pawn.Map: {pawn.Map}, thing.Map: {dest.thingInt.Map}");
                        }
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
                                            var shouldChangeResult = !((data.Key.jobs?.curDriver is JobDriver_TakeToBed) || (data.Key.jobs?.curDriver is JobDriver_LayDown));
                                            if (shouldChangeResult)
                                            {
                                                Log.Message(data.Key + " - data.Value.mainJob: " + data.Value.mainJob);
                                                Log.Message(data.Key + " - data.Key.CurJob: " + data.Key.CurJob);
                                                Log.Message(data.Key + " - thing.Map.reservationManager.reservations.Any(x => x.Target.thingInt == thing && x.claimant == data.Key): " + thing.Map.reservationManager.reservations.Any(x => x.Target.thingInt == thing && x.claimant == data.Key));
                                                if (data.Key.Map == thing.Map && !thing.Map.reservationManager.reservations.Any(x => x.Target.thingInt == thing && x.claimant == data.Key))
                                                {
                                                    ZLogger.Error($"PREVENTED ZTRACKER reservation disfunction: claimant: {claimant}, pawn: {data.Key}, thing: {thing}");
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