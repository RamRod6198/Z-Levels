using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
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
    //  things to look for patching:
    //  \.BestAttackTarget
    //      BestShootTargetFromCurrentPosition
    //      FindAttackTarget
    //      TryFindNewTarget
    //      \.enemyTarget
    //      FindPawnTarget
    //      CheckForAutoAttack
    //      TryStartAttack
    //      TryGetAttackVerb
    //      TryStartCastOn

    [HarmonyPatch(typeof(AttackTargetFinder), "BestAttackTarget")]
    public static class CombatPatches_BestAttackTarget_Patch
    {
        private static List<IAttackTarget> tmpTargets = new List<IAttackTarget>();

        private static List<Pair<IAttackTarget, float>> availableShootingTargets = new List<Pair<IAttackTarget, float>>();

        private static List<float> tmpTargetScores = new List<float>();

        private static List<bool> tmpCanShootAtTarget = new List<bool>();

        private static List<IntVec3> tempDestList = new List<IntVec3>();

        private static List<IntVec3> tempSourceList = new List<IntVec3>();

        public static bool recursiveTrap = false;
        public static bool Prefix(ref IAttackTarget __result, List<IAttackTarget> ___tmpTargets, List<Pair<IAttackTarget, float>> ___availableShootingTargets,
                List<float> ___tmpTargetScores, List<bool> ___tmpCanShootAtTarget, List<IntVec3> ___tempDestList, List<IntVec3> ___tempSourceList,
                IAttackTargetSearcher searcher, TargetScanFlags flags, Predicate<Thing> validator = null, float minDist = 0f,
                float maxDist = 9999f, IntVec3 locus = default(IntVec3), float maxTravelRadiusFromLocus = 3.40282347E+38f, bool canBash = false,
                bool canTakeTargetsCloserThanEffectiveMinRange = true)
        {
            bool result = true;
            tmpTargets = ___tmpTargets;
            availableShootingTargets = ___availableShootingTargets;
            tmpTargetScores = ___tmpTargetScores;
            tmpCanShootAtTarget = ___tmpCanShootAtTarget;
            tempDestList = ___tempDestList;
            tempSourceList = ___tempSourceList;

            if (!recursiveTrap)
            {
                recursiveTrap = true;
                Map oldMap = searcher.Thing.Map;
                IntVec3 oldPosition = searcher.Thing.Position;

                foreach (var map in ZUtils.GetAllMapsInClosestOrder(searcher.Thing, oldMap, oldPosition))
                {
                    if (ZUtils.ZTracker.GetZIndexFor(map) < ZUtils.ZTracker.GetZIndexFor(oldMap))
                    {
                        CanBeSeenOverFast_Patch.returnTrue = true;
                        Log.Message("SETTING TRUE", true);
                    }
                    var target = AttackTargetFinder.BestAttackTarget(searcher, flags, validator, minDist,
                            maxDist, locus, maxTravelRadiusFromLocus, canBash, canTakeTargetsCloserThanEffectiveMinRange);
                    Log.Message(searcher.Thing + " - 1: " + ZUtils.ZTracker.GetMapInfo(searcher.Thing.Map) + " - result: " + target);
                    if (target != null)
                    {
                        __result = target;
                        result = false;
                        break;
                    }
                }
                ZUtils.TeleportThing(searcher.Thing, oldMap, oldPosition);
                recursiveTrap = false;
                CanBeSeenOverFast_Patch.returnTrue = false;
            }
            return result;
        }

        public static void Postfix(ref IAttackTarget __result)
        {
            Log.Message("1 TEST: " + __result);
        }
    }

    [HarmonyPatch(typeof(GenGrid), "CanBeSeenOverFast")]
    public static class CanBeSeenOverFast_Patch
    {
        public static bool returnTrue = false;
        public static bool Prefix(IntVec3 c, Map map, ref bool __result)
        {
            if (returnTrue)
            {
                __result = true;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Verb), "TryFindShootLineFromTo")]
    public static class TryFindShootLineFromTo_Patch
    {
        public static bool Prefix(Verb __instance, List<IntVec3> ___tempLeanShootSources, List<IntVec3> ___tempDestList, IntVec3 root, LocalTargetInfo targ, out ShootLine resultingLine, ref bool __result)
        {
            //if (targ.HasThing && targ.Thing.Map != __instance.caster.Map)
            //{
            //      resultingLine = default(ShootLine);
            //      __result = false;
            //      return false;
            //}
            if (__instance.verbProps.IsMeleeAttack || __instance.verbProps.range <= 1.42f)
            {
                resultingLine = new ShootLine(root, targ.Cell);
                __result = ReachabilityImmediate.CanReachImmediate(root, targ, __instance.caster.Map, PathEndMode.Touch, null);
                foreach (var t in resultingLine.Points())
                {
                    GenSpawn.Spawn(ThingDefOf.Gold, t, __instance.caster.Map);
                }
                return false;
            }
            CellRect cellRect = targ.HasThing ? targ.Thing.OccupiedRect() : CellRect.SingleCell(targ.Cell);
            float num = __instance.verbProps.EffectiveMinRange(targ, __instance.caster);
            float num2 = cellRect.ClosestDistSquaredTo(root);
            if (num2 > __instance.verbProps.range * __instance.verbProps.range || num2 < num * num)
            {
                resultingLine = new ShootLine(root, targ.Cell);
                __result = false;
                foreach (var t in resultingLine.Points())
                {
                    GenSpawn.Spawn(ThingDefOf.Gold, t, __instance.caster.Map);
                }
                return false;
            }
            if (!__instance.verbProps.requireLineOfSight)
            {
                resultingLine = new ShootLine(root, targ.Cell);
                __result = true;
                foreach (var t in resultingLine.Points())
                {
                    GenSpawn.Spawn(ThingDefOf.Gold, t, __instance.caster.Map);
                }
                return false;
            }
            IntVec3 goodDest;
            if (__instance.CasterIsPawn)
            {
                if (CanHitFromCellIgnoringRange(__instance, root, ___tempDestList, targ, out goodDest))
                {
                    resultingLine = new ShootLine(root, goodDest);
                    __result = true;
                    foreach (var t in resultingLine.Points())
                    {
                        GenSpawn.Spawn(ThingDefOf.Gold, t, __instance.caster.Map);
                    }
                    return false;
                }
                ShootLeanUtility.LeanShootingSourcesFromTo(root, cellRect.ClosestCellTo(root), __instance.caster.Map, ___tempLeanShootSources);
                for (int i = 0; i < ___tempLeanShootSources.Count; i++)
                {
                    IntVec3 intVec = ___tempLeanShootSources[i];
                    if (CanHitFromCellIgnoringRange(__instance, intVec, ___tempDestList, targ, out goodDest))
                    {
                        resultingLine = new ShootLine(intVec, goodDest);
                        __result = true;
                        foreach (var t in resultingLine.Points())
                        {
                            GenSpawn.Spawn(ThingDefOf.Gold, t, __instance.caster.Map);
                        }
                        return false;
                    }
                }
            }
            else
            {
                foreach (IntVec3 item in __instance.caster.OccupiedRect())
                {
                    if (CanHitFromCellIgnoringRange(__instance, item, ___tempDestList, targ, out goodDest))
                    {
                        resultingLine = new ShootLine(item, goodDest);
                        __result = true;
                        foreach (var t in resultingLine.Points())
                        {
                            GenSpawn.Spawn(ThingDefOf.Gold, t, __instance.caster.Map);
                        }
                        return false;
                    }
                }
            }
            resultingLine = new ShootLine(root, targ.Cell);
            foreach (var t in resultingLine.Points())
            {
                GenSpawn.Spawn(ThingDefOf.Gold, t, __instance.caster.Map);
            }
            __result = false;
            return false;
        }
        private static bool CanHitFromCellIgnoringRange(Verb __instance, IntVec3 sourceCell, List<IntVec3> ___tempDestList, LocalTargetInfo targ, out IntVec3 goodDest)
        {
            if (targ.Thing != null)
            {
                //if (targ.Thing.Map != __instance.caster.Map)
                //{
                //      goodDest = IntVec3.Invalid;
                //      return false;
                //}
                ShootLeanUtility.CalcShootableCellsOf(___tempDestList, targ.Thing);
                for (int i = 0; i < ___tempDestList.Count; i++)
                {
                    if (CanHitCellFromCellIgnoringRange(__instance, sourceCell, ___tempDestList[i], targ.Thing.def.Fillage == FillCategory.Full))
                    {
                        goodDest = ___tempDestList[i];
                        return true;
                    }
                }
            }
            else if (CanHitCellFromCellIgnoringRange(__instance, sourceCell, targ.Cell))
            {
                goodDest = targ.Cell;
                return true;
            }
            goodDest = IntVec3.Invalid;
            return false;
        }

        private static bool CanHitCellFromCellIgnoringRange(Verb __intance, IntVec3 sourceSq, IntVec3 targetLoc, bool includeCorners = false)
        {
            if (__intance.verbProps.mustCastOnOpenGround && (!targetLoc.Standable(__intance.caster.Map)
                            || __intance.caster.Map.thingGrid.CellContains(targetLoc, ThingCategory.Pawn)))
            {
                return false;
            }
            if (__intance.verbProps.requireLineOfSight)
            {
                if (!includeCorners)
                {
                    if (!GenSight.LineOfSight(sourceSq, targetLoc, __intance.caster.Map, skipFirstCell: true))
                    {
                        return false;
                    }
                }
                else if (!GenSight.LineOfSightToEdges(sourceSq, targetLoc, __intance.caster.Map, skipFirstCell: true))
                {
                    return false;
                }
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(JobGiver_ConfigurableHostilityResponse), "TryGiveJob")]
    public static class TryGiveJob_Patch
    {
        public static void Postfix(Pawn pawn, ref Job __result)
        {
            Log.Message(pawn + " got response: " + __result);
        }
    }

    [HarmonyPatch(typeof(Verb_LaunchProjectile), "TryCastShot")]
    public static class TryCastShot_Patch
    {
        public static bool Prefix(Verb_LaunchProjectile __instance, List<IntVec3> ___tempLeanShootSources, List<IntVec3> ___tempDestList, LocalTargetInfo ___currentTarget, ref bool __result)
        {
            //if (___currentTarget.HasThing && ___currentTarget.Thing.Map != __instance.caster.Map)
            //{
            //      return false;
            //}
            ThingDef projectile = __instance.Projectile;
            if (projectile == null)
            {
                return false;
            }
            ShootLine resultingLine;
            bool flag = false;
            TryFindShootLineFromTo_Patch.Prefix(__instance, ___tempLeanShootSources, ___tempDestList, __instance.caster.Position,
                    ___currentTarget, out resultingLine, ref flag);

            if (__instance.verbProps.stopBurstWithoutLos && !flag)
            {
                return false;
            }
            if (__instance.EquipmentSource != null)
            {
                __instance.EquipmentSource.GetComp<CompChangeableProjectile>()?.Notify_ProjectileLaunched();
            }
            Thing launcher = __instance.caster;
            Thing equipment = __instance.EquipmentSource;
            CompMannable compMannable = __instance.caster.TryGetComp<CompMannable>();
            if (compMannable != null && compMannable.ManningPawn != null)
            {
                launcher = compMannable.ManningPawn;
                equipment = __instance.caster;
            }
            Vector3 drawPos = __instance.caster.DrawPos;
            Projectile projectile2 = (Projectile)GenSpawn.Spawn(projectile, resultingLine.Source, __instance.caster.Map);
            if (__instance.verbProps.forcedMissRadius > 0.5f)
            {
                float num = VerbUtility.CalculateAdjustedForcedMiss(__instance.verbProps.forcedMissRadius, ___currentTarget.Cell - __instance.caster.Position);
                if (num > 0.5f)
                {
                    int max = GenRadial.NumCellsInRadius(num);
                    int num2 = Rand.Range(0, max);
                    if (num2 > 0)
                    {
                        IntVec3 c = ___currentTarget.Cell + GenRadial.RadialPattern[num2];
                        ProjectileHitFlags projectileHitFlags = ProjectileHitFlags.NonTargetWorld;
                        if (Rand.Chance(0.5f))
                        {
                            projectileHitFlags = ProjectileHitFlags.All;
                        }
                        if (!Traverse.Create(__instance).Field("canHitNonTargetPawnsNow").GetValue<bool>())
                        {
                            projectileHitFlags &= ~ProjectileHitFlags.NonTargetPawns;
                        }
                        projectile2.Launch(launcher, drawPos, c, ___currentTarget, projectileHitFlags, equipment);
                        return true;
                    }
                }
            }
            ShotReport shotReport = ShotReport.HitReportFor(__instance.caster, __instance, ___currentTarget);
            Thing randomCoverToMissInto = shotReport.GetRandomCoverToMissInto();
            ThingDef targetCoverDef = randomCoverToMissInto?.def;
            if (!Rand.Chance(shotReport.AimOnTargetChance_IgnoringPosture))
            {
                resultingLine.ChangeDestToMissWild(shotReport.AimOnTargetChance_StandardTarget);
                ProjectileHitFlags projectileHitFlags2 = ProjectileHitFlags.NonTargetWorld;
                if (Rand.Chance(0.5f) && Traverse.Create(__instance).Field("canHitNonTargetPawnsNow").GetValue<bool>())
                {
                    projectileHitFlags2 |= ProjectileHitFlags.NonTargetPawns;
                }
                projectile2.Launch(launcher, drawPos, resultingLine.Dest, ___currentTarget, projectileHitFlags2, equipment, targetCoverDef);
                return true;
            }
            if (___currentTarget.Thing != null && ___currentTarget.Thing.def.category == ThingCategory.Pawn && !Rand.Chance(shotReport.PassCoverChance))
            {
                ProjectileHitFlags projectileHitFlags3 = ProjectileHitFlags.NonTargetWorld;
                if (Traverse.Create(__instance).Field("canHitNonTargetPawnsNow").GetValue<bool>())
                {
                    projectileHitFlags3 |= ProjectileHitFlags.NonTargetPawns;
                }
                projectile2.Launch(launcher, drawPos, randomCoverToMissInto, ___currentTarget, projectileHitFlags3, equipment, targetCoverDef);
                return true;
            }
            ProjectileHitFlags projectileHitFlags4 = ProjectileHitFlags.IntendedTarget;
            if (Traverse.Create(__instance).Field("canHitNonTargetPawnsNow").GetValue<bool>())
            {
                projectileHitFlags4 |= ProjectileHitFlags.NonTargetPawns;
            }
            if (!___currentTarget.HasThing || ___currentTarget.Thing.def.Fillage == FillCategory.Full)
            {
                projectileHitFlags4 |= ProjectileHitFlags.NonTargetWorld;
            }
            if (___currentTarget.Thing != null)
            {
                projectile2.Launch(launcher, drawPos, ___currentTarget, ___currentTarget, projectileHitFlags4, equipment, targetCoverDef);
            }
            else
            {
                projectile2.Launch(launcher, drawPos, resultingLine.Dest, ___currentTarget, projectileHitFlags4, equipment, targetCoverDef);
            }
            return true;
        }
    }

    //[HarmonyPatch(typeof(JobGiver_AIFightEnemies), "TryGiveJob")]
    //public class JobGiver_AIFightEnemies_Patch
    //{
    //      private static void Postfix(ref JobGiver_AIFightEnemies __instance, ref Job __result, ref Pawn pawn)
    //      {
    //
    //              Log.Message(pawn + " - 3 TEST: " + __result);
    //      }
    //}

    //[HarmonyPatch(typeof(JobGiver_AIDefendPoint), "TryGiveJob")]
    //public class JobGiver_AIDefendPoint_Patch
    //{
    //      private static void Postfix(ref JobGiver_AIDefendPoint __instance, ref Job __result, ref Pawn pawn)
    //      {
    //
    //              Log.Message(pawn + " - 3 TEST: " + __result);
    //      }
    //}

    [HarmonyPatch(typeof(JobGiver_AIDefendPawn), "TryGiveJob")]
    public class JobGiver_AIDefendPawn_Patch
    {
        private static void Postfix(ref JobGiver_AIDefendPawn __instance, ref Job __result, ref Pawn pawn)
        {

            Log.Message(pawn + " - 3 TEST: " + __result);
        }
    }

    [HarmonyPatch(typeof(JobGiver_AIFightEnemy), "TryGiveJob")]
    public class JobGiver_AIFightEnemy_Patch
    {
        public static bool recursiveTrap = false;
        private static bool Prefix(ref JobGiver_AIFightEnemy __instance, ref Job __result, ref Pawn pawn)
        {
            bool result = true;
            if (!recursiveTrap)
            {
                recursiveTrap = true;
                Map oldMap = pawn.Map;
                IntVec3 oldPosition = pawn.Position;
                foreach (var map in ZUtils.GetAllMapsInClosestOrder(pawn, oldMap, oldPosition))
                {
                    var job = Traverse.Create(__instance).Method("TryGiveJob", new object[]
                                    {
                                                                        pawn
                                    }).GetValue<Job>();

                    Log.Message("4: " + ZUtils.ZTracker.GetMapInfo(pawn.Map) + " - result: " + job);
                    if (job != null)
                    {
                        __result = job;
                        result = false;
                        break;
                    }
                }
                ZUtils.TeleportThing(pawn, oldMap, oldPosition);
                recursiveTrap = false;
            }
            Log.Message(pawn + " - 4 TEST: " + __result);
            return result;
        }
    }
}