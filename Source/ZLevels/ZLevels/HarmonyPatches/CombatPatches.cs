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
                    }
                    var target = BestAttackTarget(searcher, flags, validator, minDist,
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
            }
            CanBeSeenOverFast_Patch.returnTrue = false;
            return result;
        }

        public static void Postfix(ref IAttackTarget __result)
        {
            Log.Message("1 TEST: " + __result);
        }
        public static IAttackTarget BestAttackTarget(IAttackTargetSearcher searcher, TargetScanFlags flags, Predicate<Thing> validator = null, float minDist = 0f, float maxDist = 9999f, IntVec3 locus = default(IntVec3), float maxTravelRadiusFromLocus = float.MaxValue, bool canBash = false, bool canTakeTargetsCloserThanEffectiveMinRange = true)
        {
            Thing searcherThing = searcher.Thing;
            Pawn searcherPawn = searcher as Pawn;
            Verb verb = searcher.CurrentEffectiveVerb;
            if (verb == null)
            {
                Log.Error("BestAttackTarget with " + searcher.ToStringSafe() + " who has no attack verb.");
                return null;
            }
            bool onlyTargetMachines = verb.IsEMP();
            float minDistSquared = minDist * minDist;
            float num = maxTravelRadiusFromLocus + verb.verbProps.range;
            float maxLocusDistSquared = num * num;
            Func<IntVec3, bool> losValidator = null;
            if ((flags & TargetScanFlags.LOSBlockableByGas) != 0)
            {
                losValidator = delegate (IntVec3 vec3)
                {
                    Gas gas = vec3.GetGas(searcherThing.Map);
                    return gas == null || !gas.def.gas.blockTurretTracking;
                };
            }
            Predicate<IAttackTarget> innerValidator = delegate (IAttackTarget t)
            {
                Thing thing = t.Thing;
                if (t == searcher)
                {
                    return false;
                }
                if (minDistSquared > 0f && (float)(searcherThing.Position - thing.Position).LengthHorizontalSquared < minDistSquared)
                {
                    return false;
                }
                if (!canTakeTargetsCloserThanEffectiveMinRange)
                {
                    float num2 = verb.verbProps.EffectiveMinRange(thing, searcherThing);
                    if (num2 > 0f && (float)(searcherThing.Position - thing.Position).LengthHorizontalSquared < num2 * num2)
                    {
                        return false;
                    }
                }
                if (maxTravelRadiusFromLocus < 9999f && (float)(thing.Position - locus).LengthHorizontalSquared > maxLocusDistSquared)
                {
                    return false;
                }
                if (!searcherThing.HostileTo(thing))
                {
                    return false;
                }
                if (validator != null && !validator(thing))
                {
                    return false;
                }
                if (searcherPawn != null)
                {
                    Lord lord = searcherPawn.GetLord();
                    if (lord != null && !lord.LordJob.ValidateAttackTarget(searcherPawn, thing))
                    {
                        return false;
                    }
                }
                if ((flags & TargetScanFlags.NeedLOSToAll) != 0)
                {
                    if (losValidator != null && (!losValidator(searcherThing.Position) || !losValidator(thing.Position)))
                    {
                        return false;
                    }
                    if (!searcherThing.CanSee(thing, losValidator))
                    {
                        if (t is Pawn)
                        {
                            if ((flags & TargetScanFlags.NeedLOSToPawns) != 0)
                            {
                                Log.Message("CHECK: flags & TargetScanFlags.NeedLOSToPawns: " + searcher.Thing, true);
                                return false;
                            }
                        }
                        else if ((flags & TargetScanFlags.NeedLOSToNonPawns) != 0)
                        {
                            return false;
                        }
                    }
                }
                if (((flags & TargetScanFlags.NeedThreat) != 0 || (flags & TargetScanFlags.NeedAutoTargetable) != 0) && t.ThreatDisabled(searcher))
                {
                    Log.Message("CHECK: Thread disabled: " + searcher.Thing, true);
                    return false;
                }
                if ((flags & TargetScanFlags.NeedAutoTargetable) != 0 && !IsAutoTargetable(t))
                {
                    return false;
                }
                if ((flags & TargetScanFlags.NeedActiveThreat) != 0 && !GenHostility.IsActiveThreatTo(t, searcher.Thing.Faction))
                {
                    return false;
                }
                Pawn pawn = t as Pawn;
                if (onlyTargetMachines && pawn != null && pawn.RaceProps.IsFlesh)
                {
                    return false;
                }
                if ((flags & TargetScanFlags.NeedNonBurning) != 0 && thing.IsBurning())
                {
                    return false;
                }
                if (searcherThing.def.race != null && (int)searcherThing.def.race.intelligence >= 2)
                {
                    CompExplosive compExplosive = thing.TryGetComp<CompExplosive>();
                    if (compExplosive != null && compExplosive.wickStarted)
                    {
                        return false;
                    }
                }
                if (thing.def.size.x == 1 && thing.def.size.z == 1)
                {
                    if (thing.Position.Fogged(thing.Map))
                    {
                        return false;
                    }
                }
                else
                {
                    bool flag2 = false;
                    foreach (IntVec3 item in thing.OccupiedRect())
                    {
                        if (!item.Fogged(thing.Map))
                        {
                            flag2 = true;
                            break;
                        }
                    }
                    if (!flag2)
                    {
                        return false;
                    }
                }
                return true;
            };


            if (HasRangedAttack(searcher) && (searcherPawn == null || !searcherPawn.InAggroMentalState))
            {
                tmpTargets.Clear();
                tmpTargets.AddRange(searcherThing.Map.attackTargetsCache.GetPotentialTargetsFor(searcher));
                if ((flags & TargetScanFlags.NeedReachable) != 0)
                {
                    Predicate<IAttackTarget> oldValidator2 = innerValidator;
                    innerValidator = ((IAttackTarget t) => oldValidator2(t) && CanReach(searcherThing, t.Thing, canBash));
                }
                bool flag = false;
                for (int i = 0; i < tmpTargets.Count; i++)
                {
                    IAttackTarget attackTarget = tmpTargets[i];
                    if (attackTarget.Thing.Position.InHorDistOf(searcherThing.Position, maxDist) && innerValidator(attackTarget) && CanShootAtFromCurrentPosition(attackTarget, searcher, verb))
                    {
                        flag = true;
                        break;
                    }
                }
                IAttackTarget result;
                if (flag)
                {
                    tmpTargets.RemoveAll((IAttackTarget x) => !x.Thing.Position.InHorDistOf(searcherThing.Position, maxDist) || !innerValidator(x));
                    result = GetRandomShootingTargetByScore(tmpTargets, searcher, verb);
                    Log.Message("CHECK: GetRandomShootingTargetByScore: " + result + " for " + searcher.Thing, true);
                }
                else
                {
                    foreach (var s in tmpTargets)
                    {
                        Log.Message("TArget: " + s, true);
                    }
                    result = (IAttackTarget)GenClosest.ClosestThing_Global(validator: ((flags & TargetScanFlags.NeedReachableIfCantHitFromMyPos) == 0 || (flags
    & TargetScanFlags.NeedReachable) != 0) ? ((Predicate<Thing>)((Thing t) => innerValidator((IAttackTarget)t))) : ((Predicate<Thing>)((Thing t) =>
    innerValidator((IAttackTarget)t) && (CanReach(searcherThing, t, canBash) || CanShootAtFromCurrentPosition((IAttackTarget)t, searcher, verb)))),
    center: searcherThing.Position, searchSet: tmpTargets, maxDistance: maxDist);

                    //result = (IAttackTarget)GenClosest.ClosestThing_Global(validator: ((flags & TargetScanFlags.NeedReachableIfCantHitFromMyPos) == 0 || (flags 
                    //    & TargetScanFlags.NeedReachable) != 0) ? ((Predicate<Thing>)((Thing t) => innerValidator((IAttackTarget)t))) : ((Predicate<Thing>)((Thing t) => 
                    //    innerValidator((IAttackTarget)t) && (CanReach(searcherThing, t, canBash) || CanShootAtFromCurrentPosition((IAttackTarget)t, searcher, verb)))), 
                    //    center: searcherThing.Position, searchSet: tmpTargets, maxDistance: maxDist);

                    Log.Message("CHECK: ClosestThing_Global: " + result + " for " + searcher.Thing, true);
                }
                tmpTargets.Clear();
                return result;
            }
            if (searcherPawn != null && searcherPawn.mindState.duty != null && searcherPawn.mindState.duty.radius > 0f && !searcherPawn.InMentalState)
            {
                Predicate<IAttackTarget> oldValidator = innerValidator;
                innerValidator = delegate (IAttackTarget t)
                {
                    if (!oldValidator(t))
                    {
                        return false;
                    }
                    return t.Thing.Position.InHorDistOf(searcherPawn.mindState.duty.focus.Cell, searcherPawn.mindState.duty.radius) ? true : false;
                };
            }
            IAttackTarget attackTarget2 = (IAttackTarget)GenClosest.ClosestThingReachable(searcherThing.Position, searcherThing.Map, ThingRequest.ForGroup(ThingRequestGroup.AttackTarget), PathEndMode.Touch, TraverseParms.For(searcherPawn, Danger.Deadly, TraverseMode.ByPawn, canBash), maxDist, (Thing x) => innerValidator((IAttackTarget)x), null, 0, (maxDist > 800f) ? (-1) : 40);
            if (attackTarget2 != null && PawnUtility.ShouldCollideWithPawns(searcherPawn))
            {
                IAttackTarget attackTarget3 = FindBestReachableMeleeTarget(innerValidator, searcherPawn, maxDist, canBash);
                if (attackTarget3 != null)
                {
                    float lengthHorizontal = (searcherPawn.Position - attackTarget2.Thing.Position).LengthHorizontal;
                    float lengthHorizontal2 = (searcherPawn.Position - attackTarget3.Thing.Position).LengthHorizontal;
                    if (Mathf.Abs(lengthHorizontal - lengthHorizontal2) < 50f)
                    {
                        attackTarget2 = attackTarget3;
                    }
                }
            }
            return attackTarget2;
        }

        private static bool CanReach(Thing searcher, Thing target, bool canBash)
        {
            Pawn pawn = searcher as Pawn;
            if (pawn != null)
            {
                if (!pawn.CanReach(target, PathEndMode.Touch, Danger.Some, canBash))
                {
                    Log.Message(searcher + " - false 1", true);
                    return false;
                }
            }
            else
            {
                TraverseMode mode = canBash ? TraverseMode.PassDoors : TraverseMode.NoPassClosedDoors;
                if (!searcher.Map.reachability.CanReach(searcher.Position, target, PathEndMode.Touch, TraverseParms.For(mode)))
                {
                    Log.Message(searcher + " - false 2", true);
                    return false;
                }
            }
            Log.Message(searcher + " - true 1", true);
            return true;
        }

        private static IAttackTarget FindBestReachableMeleeTarget(Predicate<IAttackTarget> validator, Pawn searcherPawn, float maxTargDist, bool canBash)
        {
            maxTargDist = Mathf.Min(maxTargDist, 30f);
            IAttackTarget reachableTarget = null;
            Func<IntVec3, IAttackTarget> bestTargetOnCell = delegate (IntVec3 x)
            {
                List<Thing> thingList = x.GetThingList(searcherPawn.Map);
                for (int j = 0; j < thingList.Count; j++)
                {
                    Thing thing = thingList[j];
                    IAttackTarget attackTarget2 = thing as IAttackTarget;
                    if (attackTarget2 != null && validator(attackTarget2) && ReachabilityImmediate.CanReachImmediate(x, thing, searcherPawn.Map, PathEndMode.Touch, searcherPawn) && (searcherPawn.CanReachImmediate(thing, PathEndMode.Touch) || searcherPawn.Map.attackTargetReservationManager.CanReserve(searcherPawn, attackTarget2)))
                    {
                        return attackTarget2;
                    }
                }
                return null;
            };
            searcherPawn.Map.floodFiller.FloodFill(searcherPawn.Position, delegate (IntVec3 x)
            {
                if (!x.Walkable(searcherPawn.Map))
                {
                    return false;
                }
                if ((float)x.DistanceToSquared(searcherPawn.Position) > maxTargDist * maxTargDist)
                {
                    return false;
                }
                if (!canBash)
                {
                    Building_Door building_Door = x.GetEdifice(searcherPawn.Map) as Building_Door;
                    if (building_Door != null && !building_Door.CanPhysicallyPass(searcherPawn))
                    {
                        return false;
                    }
                }
                return (!PawnUtility.AnyPawnBlockingPathAt(x, searcherPawn, actAsIfHadCollideWithPawnsJob: true)) ? true : false;
            }, delegate (IntVec3 x)
            {
                for (int i = 0; i < 8; i++)
                {
                    IntVec3 intVec = x + GenAdj.AdjacentCells[i];
                    if (intVec.InBounds(searcherPawn.Map))
                    {
                        IAttackTarget attackTarget = bestTargetOnCell(intVec);
                        if (attackTarget != null)
                        {
                            reachableTarget = attackTarget;
                            break;
                        }
                    }
                }
                return reachableTarget != null;
            });
            return reachableTarget;
        }

        private static bool HasRangedAttack(IAttackTargetSearcher t)
        {
            Verb currentEffectiveVerb = t.CurrentEffectiveVerb;
            if (currentEffectiveVerb != null)
            {
                return !currentEffectiveVerb.verbProps.IsMeleeAttack;
            }
            return false;
        }

        private static bool CanShootAtFromCurrentPosition(IAttackTarget target, IAttackTargetSearcher searcher, Verb verb)
        {
            Log.Message(searcher.Thing + " CanShootAtFromCurrentPosition -  " + (verb?.CanHitTargetFrom(searcher.Thing.Position, target.Thing) ?? false), true);
            return verb?.CanHitTargetFrom(searcher.Thing.Position, target.Thing) ?? false;
        }

        private static IAttackTarget GetRandomShootingTargetByScore(List<IAttackTarget> targets, IAttackTargetSearcher searcher, Verb verb)
        {
            if (GetAvailableShootingTargetsByScore(targets, searcher, verb).TryRandomElementByWeight((Pair<IAttackTarget, float> x) => x.Second, out Pair<IAttackTarget, float> result))
            {
                return result.First;
            }
            return null;
        }

        private static List<Pair<IAttackTarget, float>> GetAvailableShootingTargetsByScore(List<IAttackTarget> rawTargets, IAttackTargetSearcher searcher, Verb verb)
        //,     List<Pair<IAttackTarget, float>> ___availableShootingTargets)
        {
            availableShootingTargets.Clear();
            if (rawTargets.Count == 0)
            {
                return availableShootingTargets;
            }
            tmpTargetScores.Clear();
            tmpCanShootAtTarget.Clear();
            float num = 0f;
            IAttackTarget attackTarget = null;
            for (int i = 0; i < rawTargets.Count; i++)
            {
                tmpTargetScores.Add(float.MinValue);
                tmpCanShootAtTarget.Add(item: false);
                if (rawTargets[i] == searcher)
                {
                    continue;
                }
                bool flag = CanShootAtFromCurrentPosition(rawTargets[i], searcher, verb);
                tmpCanShootAtTarget[i] = flag;
                if (flag)
                {
                    float shootingTargetScore = GetShootingTargetScore(rawTargets[i], searcher, verb);
                    tmpTargetScores[i] = shootingTargetScore;
                    if (attackTarget == null || shootingTargetScore > num)
                    {
                        attackTarget = rawTargets[i];
                        num = shootingTargetScore;
                    }
                }
            }
            if (num < 1f)
            {
                if (attackTarget != null)
                {
                    availableShootingTargets.Add(new Pair<IAttackTarget, float>(attackTarget, 1f));
                }
            }
            else
            {
                float num2 = num - 30f;
                for (int j = 0; j < rawTargets.Count; j++)
                {
                    if (rawTargets[j] != searcher && tmpCanShootAtTarget[j])
                    {
                        float num3 = tmpTargetScores[j];
                        if (num3 >= num2)
                        {
                            float second = Mathf.InverseLerp(num - 30f, num, num3);
                            availableShootingTargets.Add(new Pair<IAttackTarget, float>(rawTargets[j], second));
                        }
                    }
                }
            }
            return availableShootingTargets;
        }

        private static float GetShootingTargetScore(IAttackTarget target, IAttackTargetSearcher searcher, Verb verb)
        {
            float num = 60f;
            num -= Mathf.Min((target.Thing.Position - searcher.Thing.Position).LengthHorizontal, 40f);
            if (target.TargetCurrentlyAimingAt == searcher.Thing)
            {
                num += 10f;
            }
            if (searcher.LastAttackedTarget == target.Thing && Find.TickManager.TicksGame - searcher.LastAttackTargetTick <= 300)
            {
                num += 40f;
            }
            num -= CoverUtility.CalculateOverallBlockChance(target.Thing.Position, searcher.Thing.Position, searcher.Thing.Map) * 10f;
            Pawn pawn = target as Pawn;
            if (pawn != null && pawn.RaceProps.Animal && pawn.Faction != null && !pawn.IsFighting())
            {
                num -= 50f;
            }
            num += FriendlyFireBlastRadiusTargetScoreOffset(target, searcher, verb);
            num += FriendlyFireConeTargetScoreOffset(target, searcher, verb);
            return num * target.TargetPriorityFactor;
        }

        private static float FriendlyFireBlastRadiusTargetScoreOffset(IAttackTarget target, IAttackTargetSearcher searcher, Verb verb)
        {
            if (verb.verbProps.ai_AvoidFriendlyFireRadius <= 0f)
            {
                return 0f;
            }
            Map map = target.Thing.Map;
            IntVec3 position = target.Thing.Position;
            int num = GenRadial.NumCellsInRadius(verb.verbProps.ai_AvoidFriendlyFireRadius);
            float num2 = 0f;
            for (int i = 0; i < num; i++)
            {
                IntVec3 intVec = position + GenRadial.RadialPattern[i];
                if (!intVec.InBounds(map))
                {
                    continue;
                }
                bool flag = true;
                List<Thing> thingList = intVec.GetThingList(map);
                for (int j = 0; j < thingList.Count; j++)
                {
                    if (!(thingList[j] is IAttackTarget) || thingList[j] == target)
                    {
                        continue;
                    }
                    if (flag)
                    {
                        if (!GenSight.LineOfSight(position, intVec, map, skipFirstCell: true))
                        {
                            break;
                        }
                        flag = false;
                    }
                    float num3 = (thingList[j] == searcher) ? 40f : ((!(thingList[j] is Pawn)) ? 10f : (thingList[j].def.race.Animal ? 7f : 18f));
                    num2 = ((!searcher.Thing.HostileTo(thingList[j])) ? (num2 - num3) : (num2 + num3 * 0.6f));
                }
            }
            return num2;
        }

        private static float FriendlyFireConeTargetScoreOffset(IAttackTarget target, IAttackTargetSearcher searcher, Verb verb)
        {
            Pawn pawn = searcher.Thing as Pawn;
            if (pawn == null)
            {
                return 0f;
            }
            if ((int)pawn.RaceProps.intelligence < 1)
            {
                return 0f;
            }
            if (pawn.RaceProps.IsMechanoid)
            {
                return 0f;
            }
            Verb_Shoot verb_Shoot = verb as Verb_Shoot;
            if (verb_Shoot == null)
            {
                return 0f;
            }
            ThingDef defaultProjectile = verb_Shoot.verbProps.defaultProjectile;
            if (defaultProjectile == null)
            {
                return 0f;
            }
            if (defaultProjectile.projectile.flyOverhead)
            {
                return 0f;
            }
            Map map = pawn.Map;
            ShotReport report = ShotReport.HitReportFor(pawn, verb, (Thing)target);
            float radius = Mathf.Max(VerbUtility.CalculateAdjustedForcedMiss(verb.verbProps.forcedMissRadius, report.ShootLine.Dest - report.ShootLine.Source), 1.5f);
            IEnumerable<IntVec3> enumerable = (from dest in GenRadial.RadialCellsAround(report.ShootLine.Dest, radius, useCenter: true)
                                               where dest.InBounds(map)
                                               select new ShootLine(report.ShootLine.Source, dest)).SelectMany((ShootLine line) => line.Points().Concat(line.Dest).TakeWhile((IntVec3 pos) => pos.CanBeSeenOverFast(map))).Distinct();
            float num = 0f;
            foreach (IntVec3 item in enumerable)
            {
                float num2 = VerbUtility.InterceptChanceFactorFromDistance(report.ShootLine.Source.ToVector3Shifted(), item);
                if (!(num2 <= 0f))
                {
                    List<Thing> thingList = item.GetThingList(map);
                    for (int i = 0; i < thingList.Count; i++)
                    {
                        Thing thing = thingList[i];
                        if (thing is IAttackTarget && thing != target)
                        {
                            float num3 = (thing == searcher) ? 40f : ((!(thing is Pawn)) ? 10f : (thing.def.race.Animal ? 7f : 18f));
                            num3 *= num2;
                            num3 = ((!searcher.Thing.HostileTo(thing)) ? (num3 * -1f) : (num3 * 0.6f));
                            num += num3;
                        }
                    }
                }
            }
            return num;
        }

        public static bool CanSee(this Thing seer, Thing target, Func<IntVec3, bool> validator = null)
        {
            ShootLeanUtility.CalcShootableCellsOf(tempDestList, target);
            for (int i = 0; i < tempDestList.Count; i++)
            {
                if (GenSight.LineOfSight(seer.Position, tempDestList[i], seer.Map, skipFirstCell: true, validator))
                {
                    return true;
                }
            }
            ShootLeanUtility.LeanShootingSourcesFromTo(seer.Position, target.Position, seer.Map, tempSourceList);
            for (int j = 0; j < tempSourceList.Count; j++)
            {
                for (int k = 0; k < tempDestList.Count; k++)
                {
                    if (GenSight.LineOfSight(tempSourceList[j], tempDestList[k], seer.Map, skipFirstCell: true, validator))
                    {
                        return true;
                    }
                }
            }
            Log.Message("FINAL RESULT: " + seer + " cant see " + target, true);
            return false;
        }

        //public static bool CanSee(this Thing seer, Thing target, Func<IntVec3, bool> validator = null)
        //{
        //    //ZUtils.TeleportThing(target, seer.Map, target.Position);
        //    ShootLeanUtility.CalcShootableCellsOf(tempDestList, target);
        //    for (int i = 0; i < tempDestList.Count; i++)
        //    {
        //        if (GenSight.LineOfSight(seer.Position, tempDestList[i], target.Map, skipFirstCell: true, null))
        //        {
        //            //ZUtils.TeleportThing(target, target.Map, target.Position);
        //            return true;
        //        }
        //        else
        //        {
        //        }
        //    }
        //    ShootLeanUtility.LeanShootingSourcesFromTo(seer.Position, target.Position, target.Map, tempSourceList);
        //    for (int j = 0; j < tempSourceList.Count; j++)
        //    {
        //        for (int k = 0; k < tempDestList.Count; k++)
        //        {
        //            if (GenSight.LineOfSight(tempSourceList[j], tempDestList[k], target.Map, skipFirstCell: true, null))
        //            {
        //                //ZUtils.TeleportThing(target, target.Map, target.Position);
        //                return true;
        //            }
        //            else
        //            {
        //            }
        //        }
        //    }
        //    //ZUtils.TeleportThing(target, target.Map, target.Position);
        //    Log.Message("FINAL RESULT: " + seer + " cant see " + target, true);
        //    return false;
        //}

        public static bool IsAutoTargetable(IAttackTarget target)
        {
            CompCanBeDormant compCanBeDormant = target.Thing.TryGetComp<CompCanBeDormant>();
            if (compCanBeDormant != null && !compCanBeDormant.Awake)
            {
                return false;
            }
            CompInitiatable compInitiatable = target.Thing.TryGetComp<CompInitiatable>();
            if (compInitiatable != null && !compInitiatable.Initiated)
            {
                return false;
            }
            return true;
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

    [HarmonyPatch(typeof(AttackTargetFinder), "BestShootTargetFromCurrentPosition")]
    public class BestShootTargetFromCurrentPosition_Patch
    {
        public static void Postfix(ref IAttackTarget __result, IAttackTargetSearcher searcher,
                TargetScanFlags flags, Predicate<Thing> validator = null, float minDistance = 0f,
                float maxDistance = 9999f)
        {
            Log.Message(searcher + " - 2 TEST: " + __result);
        }
    }

    [HarmonyPatch(typeof(Pawn), "TryStartAttack")]
    public class TryStartAttack_Patch
    {
        public static void Postfix(Pawn __instance, ref bool __result, LocalTargetInfo targ)
        {
            bool allowManualCastWeapons = !__instance.IsColonist;
            Verb verb = __instance.TryGetAttackVerb(targ.Thing, allowManualCastWeapons);
            Log.Message("verb: " + verb);
            Log.Message("result: " + (verb?.TryStartCastOn(targ) ?? false).ToString());
            Log.Message(__instance + " - 2.5 TEST: " + __result + " - " + targ.Thing);
        }
    }

    [HarmonyPatch(typeof(Verb), "TryStartCastOn", new Type[]
    {
                typeof(LocalTargetInfo),
                typeof(LocalTargetInfo),
                typeof(bool),
                typeof(bool)
    })]
    public class TryStartCastOn_Patch
    {
        public static void Postfix(Verb __instance, ref bool __result, LocalTargetInfo castTarg)
        {
            if (__instance.caster == null)
            {
                Log.Error("Verb " + __instance.GetUniqueLoadID() + " needs caster to work (possibly lost during saving/loading).");
                return;
            }
            if (!__instance.caster.Spawned)
            {
                return;
            }
            if (__instance.state == VerbState.Bursting || !__instance.CanHitTarget(castTarg))
            {
                return;
            }
            //__instance.surpriseAttack = surpriseAttack;
            //__instance.canHitNonTargetPawnsNow = canHitNonTargetPawns;
            //__instance.currentTarget = castTarg;
            //__instance.currentDestination = destTarg;
            if (__instance.CasterIsPawn && __instance.verbProps.warmupTime > 0f)
            {
                if (!__instance.TryFindShootLineFromTo(__instance.caster.Position, castTarg, out ShootLine resultingLine))
                {
                    return;
                }
                __instance.CasterPawn.Drawer.Notify_WarmingCastAlongLine(resultingLine, __instance.caster.Position);
                float statValue = __instance.CasterPawn.GetStatValue(StatDefOf.AimingDelayFactor);
                int ticks = (__instance.verbProps.warmupTime * statValue).SecondsToTicks();
                __instance.CasterPawn.stances.SetStance(new Stance_Warmup(ticks, castTarg, __instance));
            }
            else
            {
                __instance.WarmupComplete();
            }
            return;
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
                return false;
            }
            CellRect cellRect = targ.HasThing ? targ.Thing.OccupiedRect() : CellRect.SingleCell(targ.Cell);
            float num = __instance.verbProps.EffectiveMinRange(targ, __instance.caster);
            float num2 = cellRect.ClosestDistSquaredTo(root);
            if (num2 > __instance.verbProps.range * __instance.verbProps.range || num2 < num * num)
            {
                resultingLine = new ShootLine(root, targ.Cell);
                __result = false;
                return false;
            }
            if (!__instance.verbProps.requireLineOfSight)
            {
                resultingLine = new ShootLine(root, targ.Cell);
                __result = true;
                return false;
            }
            IntVec3 goodDest;
            if (__instance.CasterIsPawn)
            {
                if (CanHitFromCellIgnoringRange(__instance, root, ___tempDestList, targ, out goodDest))
                {
                    resultingLine = new ShootLine(root, goodDest);
                    __result = true;
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
                        return false;
                    }
                }
            }
            resultingLine = new ShootLine(root, targ.Cell);
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