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
    public static class PawnPatches
    {
        //[HarmonyPatch(typeof(Pawn), "VerifyReservations")]
        //internal static class Patch_VerifyReservations
        //{
        //    private static bool Prefix(Pawn __instance)
        //    {
        //        try
        //        {
        //            if (__instance.jobs == null)
        //            {
        //                return false;
        //            }
        //            if (__instance.CurJob != null || __instance.jobs.jobQueue.Count > 0 || __instance.jobs.startingNewJob)
        //            {
        //                return false;
        //            }
        //            bool flag = false;
        //            List<Map> maps = Find.Maps;
        //            for (int i = 0; i < maps.Count; i++)
        //            {
        //                LocalTargetInfo obj = maps[i].reservationManager.FirstReservationFor(__instance);
        //                if (obj.IsValid)
        //                {
        //                    flag = true;
        //                }
        //                LocalTargetInfo obj2 = maps[i].physicalInteractionReservationManager.FirstReservationFor(__instance);
        //                if (obj2.IsValid)
        //                {
        //                    flag = true;
        //                }
        //                IAttackTarget attackTarget = maps[i].attackTargetReservationManager.FirstReservationFor(__instance);
        //                if (attackTarget != null)
        //                {
        //                    flag = true;
        //                }
        //                IntVec3 obj3 = maps[i].pawnDestinationReservationManager.FirstObsoleteReservationFor(__instance);
        //                if (obj3.IsValid)
        //                {
        //                    flag = true;
        //                }
        //            }
        //            if (flag)
        //            {
        //                __instance.ClearAllReservations(true);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Log.Error("[Z-Levels] Patch_VerifyReservations patch produced an error. That should not happen and will break things. Send a Hugslib log to the Z-Levels developers. Error message: " + ex, true);
        //        }
        //        return false;
        //    }
        //}

        //[HarmonyPatch(typeof(Pawn_PathFollower), "CostToMoveIntoCell", new Type[]
        //{
        //    typeof(Pawn),
        //    typeof(IntVec3)
        //})]
        //public static class CostToMoveIntoCell_Patch
        //{
        //    public static void Prefix(Pawn_PathFollower __instance, Pawn pawn, IntVec3 c, ref int __result)
        //    {
        //        try
        //        {
        //            if (c.GetTerrain(pawn.Map) == ZLevelsDefOf.ZL_OutsideTerrain)
        //            {
        //                pawn.pather.StopDead();
        //                __result = 100000;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Log.Error("[Z-Levels] CostToMoveIntoCell_Patch patch produced an error. That should not happen and will break things. Send a Hugslib log to the Z-Levels developers. Error message: " + ex, true);
        //        }
        //    }
        //}

        //[HarmonyPatch(typeof(Pawn_JobTracker))]
        //[HarmonyPatch(nameof(Pawn_JobTracker.StopAll))]
        //static class Pawn_JobTracker_Patch
        //{
        //    [HarmonyPrefix]
        //    public static bool Prefix()
        //    {
        //        try
        //        {
        //            if (JobManagerPatches.manualDespawn == true)
        //            {
        //                return false;
        //            }
        //            else
        //            {
        //                return true;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Log.Error("[Z-Levels] Pawn_JobTracker_Patch patch produced an error. That should not happen and will break things. Send a Hugslib log to the Z-Levels developers. Error message: " + ex, true);
        //        }
        //        return true;
        //    }
        //}
        //
        //[HarmonyPatch(typeof(Pawn_PathFollower))]
        //[HarmonyPatch(nameof(Pawn_PathFollower.StopDead))]
        //static class Pawn_PathFollower_Patch
        //{
        //    [HarmonyPrefix]
        //    public static bool Prefix()
        //    {
        //        try
        //        {
        //            if (JobManagerPatches.manualDespawn == true)
        //            {
        //                return false;
        //            }
        //            else
        //            {
        //                return true;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Log.Error("[Z-Levels] Pawn_JobTracker_Patch patch produced an error. That should not happen and will break things. Send a Hugslib log to the Z-Levels developers. Error message: " + ex, true);
        //        }
        //        return true;
        //    }
        //}
        //
        //[HarmonyPatch(typeof(Pawn))]
        //[HarmonyPatch(nameof(Pawn.ClearAllReservations))]
        //static class Pawn_ClearAllReservations_Patch
        //{
        //    [HarmonyPrefix]
        //    public static bool Prefix()
        //    {
        //        try
        //        {
        //            if (JobManagerPatches.manualDespawn == true)
        //            {
        //                return false;
        //            }
        //            else
        //            {
        //                return true;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Log.Error("[Z-Levels] Pawn_ClearAllReservations_Patch patch produced an error. That should not happen and will break things. Send a Hugslib log to the Z-Levels developers. Error message: " + ex, true);
        //        }
        //        return true;
        //    }
        //}
    }
}

