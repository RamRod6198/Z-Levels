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
        [HarmonyPatch(typeof(Pawn), "VerifyReservations")]
        internal static class Patch_VerifyReservations
        {
            private static bool Prefix(Pawn __instance)
            {
                try
                {
                    if (__instance.jobs == null)
                    {
                        return false;
                    }
                    if (__instance.CurJob != null || __instance.jobs.jobQueue.Count > 0 || __instance.jobs.startingNewJob)
                    {
                        return false;
                    }
                    bool flag = false;
                    List<Map> maps = Find.Maps;
                    for (int i = 0; i < maps.Count; i++)
                    {
                        LocalTargetInfo obj = maps[i].reservationManager.FirstReservationFor(__instance);
                        if (obj.IsValid)
                        {
                            flag = true;
                        }
                        LocalTargetInfo obj2 = maps[i].physicalInteractionReservationManager.FirstReservationFor(__instance);
                        if (obj2.IsValid)
                        {
                            Log.ErrorOnce(string.Format("Physical interaction reservation manager failed to clean up properly; {0} still reserving {1}", __instance.ToStringSafe<Pawn>(), obj2.ToStringSafe<LocalTargetInfo>()), 19586765 ^ __instance.thingIDNumber, false);
                            flag = true;
                        }
                        IAttackTarget attackTarget = maps[i].attackTargetReservationManager.FirstReservationFor(__instance);
                        if (attackTarget != null)
                        {
                            Log.ErrorOnce(string.Format("Attack target reservation manager failed to clean up properly; {0} still reserving {1}", __instance.ToStringSafe<Pawn>(), attackTarget.ToStringSafe<IAttackTarget>()), 100495878 ^ __instance.thingIDNumber, false);
                            flag = true;
                        }
                        IntVec3 obj3 = maps[i].pawnDestinationReservationManager.FirstObsoleteReservationFor(__instance);
                        if (obj3.IsValid)
                        {
                            flag = true;
                        }
                    }
                    if (flag)
                    {
                        __instance.ClearAllReservations(true);
                    }
                }
                catch
                {

                }
                return false;
            }
        }

        [HarmonyPatch(typeof(Pawn_PathFollower), "CostToMoveIntoCell", new Type[]
        {
            typeof(Pawn),
            typeof(IntVec3)
        })]
        public static class CostToMoveIntoCell_Patch
        {
            public static void Prefix(Pawn_PathFollower __instance, Pawn pawn, IntVec3 c, ref int __result)
            {
                if (c.GetTerrain(pawn.Map) == ZLevelsDefOf.ZL_OutsideTerrain)
                {
                    pawn.pather.StopDead();
                }
                __result = 100000;
            }
        }

        [HarmonyPatch(typeof(Pawn_JobTracker))]
        [HarmonyPatch(nameof(Pawn_JobTracker.StopAll))]
        static class Pawn_JobTracker_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix()
            {
                if (JobManagerPatches.manualDespawn == true)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(Pawn_PathFollower))]
        [HarmonyPatch(nameof(Pawn_PathFollower.StopDead))]
        static class Pawn_PathFollower_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix()
            {
                if (JobManagerPatches.manualDespawn == true)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(Pawn))]
        [HarmonyPatch(nameof(Pawn.ClearAllReservations))]
        static class Pawn_ClearAllReservations_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix()
            {
                if (JobManagerPatches.manualDespawn == true)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
    }
}

