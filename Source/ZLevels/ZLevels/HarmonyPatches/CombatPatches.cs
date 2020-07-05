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
    public static class CombatPatches
    {
		//  things to look for patching: 
		//  \.BestAttackTarget
		//	BestShootTargetFromCurrentPosition
		//	FindAttackTarget
		//	TryFindNewTarget
		//	\.enemyTarget
		//	FindPawnTarget
		//	CheckForAutoAttack
		//	TryStartAttack
		//	TryGetAttackVerb
		//	TryStartCastOn

		[HarmonyPatch(typeof(CombatPatches), "BestAttackTarget")]
		public class CombatPatches_BestAttackTarget_Patch
		{
			public static bool Prefix(IAttackTargetSearcher searcher, ref Predicate<Thing> validator)
			{
				Log.Message("TEST");
				return true;
			}
			public static void PostFix(ref IAttackTarget __result, IAttackTargetSearcher searcher, TargetScanFlags flags, Predicate<Thing> validator = null, float minDist = 0f, float maxDist = 9999f, IntVec3 locus = default(IntVec3), float maxTravelRadiusFromLocus = 3.40282347E+38f, bool canBash = false, bool canTakeTargetsCloserThanEffectiveMinRange = true)
			{
				Log.Message("TEST: " + __result);
			}
		}
//
//		public static IAttackTarget BestAttackTarget(IAttackTargetSearcher searcher, TargetScanFlags flags, Predicate<Thing> validator = null, float minDist = 0f, float maxDist = 9999f, IntVec3 locus = default(IntVec3), float maxTravelRadiusFromLocus = 3.40282347E+38f, bool canBash = false, bool canTakeTargetsCloserThanEffectiveMinRange = true)
//		{
//			Thing searcherThing = searcher.Thing;
//			Pawn searcherPawn = searcher as Pawn;
//			Verb verb = searcher.CurrentEffectiveVerb;
//			if (verb == null)
//			{
//				Log.Error("BestAttackTarget with " + searcher.ToStringSafe<IAttackTargetSearcher>() + " who has no attack verb.", false);
//				return null;
//			}
//			bool onlyTargetMachines = verb.IsEMP();
//			float minDistSquared = minDist * minDist;
//			float num = maxTravelRadiusFromLocus + verb.verbProps.range;
//			float maxLocusDistSquared = num * num;
//			Func<IntVec3, bool> losValidator = null;
//			if ((flags & TargetScanFlags.LOSBlockableByGas) != TargetScanFlags.None)
//			{
//				losValidator = delegate (IntVec3 vec3)
//				{
//					Gas gas = vec3.GetGas(searcherThing.Map);
//					return gas == null || !gas.def.gas.blockTurretTracking;
//				};
//			}
//			Predicate<IAttackTarget> innerValidator = delegate (IAttackTarget t)
//			{
//				Thing thing = t.Thing;
//				if (t == searcher)
//				{
//					return false;
//				}
//				if (minDistSquared > 0f && (float)(searcherThing.Position - thing.Position).LengthHorizontalSquared < minDistSquared)
//				{
//					return false;
//				}
//				if (!canTakeTargetsCloserThanEffectiveMinRange)
//				{
//					float num2 = verb.verbProps.EffectiveMinRange(thing, searcherThing);
//					if (num2 > 0f && (float)(searcherThing.Position - thing.Position).LengthHorizontalSquared < num2 * num2)
//					{
//						return false;
//					}
//				}
//				if (maxTravelRadiusFromLocus < 9999f && (float)(thing.Position - locus).LengthHorizontalSquared > maxLocusDistSquared)
//				{
//					return false;
//				}
//				if (!searcherThing.HostileTo(thing))
//				{
//					return false;
//				}
//				if (validator != null && !validator(thing))
//				{
//					return false;
//				}
//				if (searcherPawn != null)
//				{
//					Lord lord = searcherPawn.GetLord();
//					if (lord != null && !lord.LordJob.ValidateAttackTarget(searcherPawn, thing))
//					{
//						return false;
//					}
//				}
//				if ((flags & TargetScanFlags.NeedLOSToAll) != TargetScanFlags.None)
//				{
//					if (losValidator != null && (!losValidator(searcherThing.Position) || !losValidator(thing.Position)))
//					{
//						return false;
//					}
//					if (!searcherThing.CanSee(thing, losValidator))
//					{
//						if (t is Pawn)
//						{
//							if ((flags & TargetScanFlags.NeedLOSToPawns) != TargetScanFlags.None)
//							{
//								return false;
//							}
//						}
//						else if ((flags & TargetScanFlags.NeedLOSToNonPawns) != TargetScanFlags.None)
//						{
//							return false;
//						}
//					}
//				}
//				if (((flags & TargetScanFlags.NeedThreat) != TargetScanFlags.None || (flags & TargetScanFlags.NeedAutoTargetable) != TargetScanFlags.None) && t.ThreatDisabled(searcher))
//				{
//					return false;
//				}
//				if ((flags & TargetScanFlags.NeedAutoTargetable) != TargetScanFlags.None && !CombatPatches.IsAutoTargetable(t))
//				{
//					return false;
//				}
//				if ((flags & TargetScanFlags.NeedActiveThreat) != TargetScanFlags.None && !GenHostility.IsActiveThreatTo(t, searcher.Thing.Faction))
//				{
//					return false;
//				}
//				Pawn pawn = t as Pawn;
//				if (onlyTargetMachines && pawn != null && pawn.RaceProps.IsFlesh)
//				{
//					return false;
//				}
//				if ((flags & TargetScanFlags.NeedNonBurning) != TargetScanFlags.None && thing.IsBurning())
//				{
//					return false;
//				}
//				if (searcherThing.def.race != null && searcherThing.def.race.intelligence >= Intelligence.Humanlike)
//				{
//					CompExplosive compExplosive = thing.TryGetComp<CompExplosive>();
//					if (compExplosive != null && compExplosive.wickStarted)
//					{
//						return false;
//					}
//				}
//				if (thing.def.size.x == 1 && thing.def.size.z == 1)
//				{
//					if (thing.Position.Fogged(thing.Map))
//					{
//						return false;
//					}
//				}
//				else
//				{
//					bool flag2 = false;
//					using (CellRect.Enumerator enumerator = thing.OccupiedRect().GetEnumerator())
//					{
//						while (enumerator.MoveNext())
//						{
//							if (!enumerator.Current.Fogged(thing.Map))
//							{
//								flag2 = true;
//								break;
//							}
//						}
//					}
//					if (!flag2)
//					{
//						return false;
//					}
//				}
//				return true;
//			};
//			if (CombatPatches.HasRangedAttack(searcher) && (searcherPawn == null || !searcherPawn.InAggroMentalState))
//			{
//				CombatPatches.tmpTargets.Clear();
//				CombatPatches.tmpTargets.AddRange(searcherThing.Map.attackTargetsCache.GetPotentialTargetsFor(searcher));
//				if ((flags & TargetScanFlags.NeedReachable) != TargetScanFlags.None)
//				{
//					Predicate<IAttackTarget> oldValidator = innerValidator;
//					innerValidator = ((IAttackTarget t) => oldValidator(t) && CombatPatches.CanReach(searcherThing, t.Thing, canBash));
//				}
//				bool flag = false;
//				for (int i = 0; i < CombatPatches.tmpTargets.Count; i++)
//				{
//					IAttackTarget attackTarget = CombatPatches.tmpTargets[i];
//					if (attackTarget.Thing.Position.InHorDistOf(searcherThing.Position, maxDist) && innerValidator(attackTarget) && CombatPatches.CanShootAtFromCurrentPosition(attackTarget, searcher, verb))
//					{
//						flag = true;
//						break;
//					}
//				}
//				IAttackTarget result;
//				if (flag)
//				{
//					CombatPatches.tmpTargets.RemoveAll((IAttackTarget x) => !x.Thing.Position.InHorDistOf(searcherThing.Position, maxDist) || !innerValidator(x));
//					result = CombatPatches.GetRandomShootingTargetByScore(CombatPatches.tmpTargets, searcher, verb);
//				}
//				else
//				{
//					Predicate<Thing> validator2;
//					if ((flags & TargetScanFlags.NeedReachableIfCantHitFromMyPos) != TargetScanFlags.None && (flags & TargetScanFlags.NeedReachable) == TargetScanFlags.None)
//					{
//						validator2 = ((Thing t) => innerValidator((IAttackTarget)t) && (CombatPatches.CanReach(searcherThing, t, canBash) || CombatPatches.CanShootAtFromCurrentPosition((IAttackTarget)t, searcher, verb)));
//					}
//					else
//					{
//						validator2 = ((Thing t) => innerValidator((IAttackTarget)t));
//					}
//					result = (IAttackTarget)GenClosest.ClosestThing_Global(searcherThing.Position, CombatPatches.tmpTargets, maxDist, validator2, null);
//				}
//				CombatPatches.tmpTargets.Clear();
//				return result;
//			}
//			if (searcherPawn != null && searcherPawn.mindState.duty != null && searcherPawn.mindState.duty.radius > 0f && !searcherPawn.InMentalState)
//			{
//				Predicate<IAttackTarget> oldValidator = innerValidator;
//				innerValidator = ((IAttackTarget t) => oldValidator(t) && t.Thing.Position.InHorDistOf(searcherPawn.mindState.duty.focus.Cell, searcherPawn.mindState.duty.radius));
//			}
//			IAttackTarget attackTarget2 = (IAttackTarget)GenClosest.ClosestThingReachable(searcherThing.Position, searcherThing.Map, ThingRequest.ForGroup(ThingRequestGroup.AttackTarget), PathEndMode.Touch, TraverseParms.For(searcherPawn, Danger.Deadly, TraverseMode.ByPawn, canBash), maxDist, (Thing x) => innerValidator((IAttackTarget)x), null, 0, (maxDist > 800f) ? -1 : 40, false, RegionType.Set_Passable, false);
//			if (attackTarget2 != null && PawnUtility.ShouldCollideWithPawns(searcherPawn))
//			{
//				IAttackTarget attackTarget3 = CombatPatches.FindBestReachableMeleeTarget(innerValidator, searcherPawn, maxDist, canBash);
//				if (attackTarget3 != null)
//				{
//					float lengthHorizontal = (searcherPawn.Position - attackTarget2.Thing.Position).LengthHorizontal;
//					float lengthHorizontal2 = (searcherPawn.Position - attackTarget3.Thing.Position).LengthHorizontal;
//					if (Mathf.Abs(lengthHorizontal - lengthHorizontal2) < 50f)
//					{
//						attackTarget2 = attackTarget3;
//					}
//				}
//			}
//			return attackTarget2;
//		}
//
//		private static bool CanReach(Thing searcher, Thing target, bool canBash)
//		{
//			Pawn pawn = searcher as Pawn;
//			if (pawn != null)
//			{
//				if (!pawn.CanReach(target, PathEndMode.Touch, Danger.Some, canBash, TraverseMode.ByPawn))
//				{
//					return false;
//				}
//			}
//			else
//			{
//				TraverseMode mode = canBash ? TraverseMode.PassDoors : TraverseMode.NoPassClosedDoors;
//				if (!searcher.Map.reachability.CanReach(searcher.Position, target, PathEndMode.Touch, TraverseParms.For(mode, Danger.Deadly, false)))
//				{
//					return false;
//				}
//			}
//			return true;
//		}
//
//		private static IAttackTarget FindBestReachableMeleeTarget(Predicate<IAttackTarget> validator, Pawn searcherPawn, float maxTargDist, bool canBash)
//		{
//			maxTargDist = Mathf.Min(maxTargDist, 30f);
//			IAttackTarget reachableTarget = null;
//			Func<IntVec3, IAttackTarget> bestTargetOnCell = delegate (IntVec3 x)
//			{
//				List<Thing> thingList = x.GetThingList(searcherPawn.Map);
//				for (int i = 0; i < thingList.Count; i++)
//				{
//					Thing thing = thingList[i];
//					IAttackTarget attackTarget = thing as IAttackTarget;
//					if (attackTarget != null && validator(attackTarget) && ReachabilityImmediate.CanReachImmediate(x, thing, searcherPawn.Map, PathEndMode.Touch, searcherPawn) && (searcherPawn.CanReachImmediate(thing, PathEndMode.Touch) || searcherPawn.Map.attackTargetReservationManager.CanReserve(searcherPawn, attackTarget)))
//					{
//						return attackTarget;
//					}
//				}
//				return null;
//			};
//			searcherPawn.Map.floodFiller.FloodFill(searcherPawn.Position, delegate (IntVec3 x)
//			{
//				if (!x.Walkable(searcherPawn.Map))
//				{
//					return false;
//				}
//				if ((float)x.DistanceToSquared(searcherPawn.Position) > maxTargDist * maxTargDist)
//				{
//					return false;
//				}
//				if (!canBash)
//				{
//					Building_Door building_Door = x.GetEdifice(searcherPawn.Map) as Building_Door;
//					if (building_Door != null && !building_Door.CanPhysicallyPass(searcherPawn))
//					{
//						return false;
//					}
//				}
//				return !PawnUtility.AnyPawnBlockingPathAt(x, searcherPawn, true, false, false);
//			}, delegate (IntVec3 x)
//			{
//				for (int i = 0; i < 8; i++)
//				{
//					IntVec3 intVec = x + GenAdj.AdjacentCells[i];
//					if (intVec.InBounds(searcherPawn.Map))
//					{
//						IAttackTarget attackTarget = bestTargetOnCell(intVec);
//						if (attackTarget != null)
//						{
//							reachableTarget = attackTarget;
//							break;
//						}
//					}
//				}
//				return reachableTarget != null;
//			}, int.MaxValue, false, null);
//			return reachableTarget;
//		}
//
//		private static bool HasRangedAttack(IAttackTargetSearcher t)
//		{
//			Verb currentEffectiveVerb = t.CurrentEffectiveVerb;
//			return currentEffectiveVerb != null && !currentEffectiveVerb.verbProps.IsMeleeAttack;
//		}
//
//		private static bool CanShootAtFromCurrentPosition(IAttackTarget target, IAttackTargetSearcher searcher, Verb verb)
//		{
//			return verb != null && verb.CanHitTargetFrom(searcher.Thing.Position, target.Thing);
//		}
//
//		private static IAttackTarget GetRandomShootingTargetByScore(List<IAttackTarget> targets, IAttackTargetSearcher searcher, Verb verb)
//		{
//			Pair<IAttackTarget, float> pair;
//			if (CombatPatches.GetAvailableShootingTargetsByScore(targets, searcher, verb).TryRandomElementByWeight((Pair<IAttackTarget, float> x) => x.Second, out pair))
//			{
//				return pair.First;
//			}
//			return null;
//		}
//
//		private static List<Pair<IAttackTarget, float>> GetAvailableShootingTargetsByScore(List<IAttackTarget> rawTargets, IAttackTargetSearcher searcher, Verb verb)
//		{
//			CombatPatches.availableShootingTargets.Clear();
//			if (rawTargets.Count == 0)
//			{
//				return CombatPatches.availableShootingTargets;
//			}
//			CombatPatches.tmpTargetScores.Clear();
//			CombatPatches.tmpCanShootAtTarget.Clear();
//			float num = 0f;
//			IAttackTarget attackTarget = null;
//			for (int i = 0; i < rawTargets.Count; i++)
//			{
//				CombatPatches.tmpTargetScores.Add(float.MinValue);
//				CombatPatches.tmpCanShootAtTarget.Add(false);
//				if (rawTargets[i] != searcher)
//				{
//					bool flag = CombatPatches.CanShootAtFromCurrentPosition(rawTargets[i], searcher, verb);
//					CombatPatches.tmpCanShootAtTarget[i] = flag;
//					if (flag)
//					{
//						float shootingTargetScore = CombatPatches.GetShootingTargetScore(rawTargets[i], searcher, verb);
//						CombatPatches.tmpTargetScores[i] = shootingTargetScore;
//						if (attackTarget == null || shootingTargetScore > num)
//						{
//							attackTarget = rawTargets[i];
//							num = shootingTargetScore;
//						}
//					}
//				}
//			}
//			if (num < 1f)
//			{
//				if (attackTarget != null)
//				{
//					CombatPatches.availableShootingTargets.Add(new Pair<IAttackTarget, float>(attackTarget, 1f));
//				}
//			}
//			else
//			{
//				float num2 = num - 30f;
//				for (int j = 0; j < rawTargets.Count; j++)
//				{
//					if (rawTargets[j] != searcher && CombatPatches.tmpCanShootAtTarget[j])
//					{
//						float num3 = CombatPatches.tmpTargetScores[j];
//						if (num3 >= num2)
//						{
//							float second = Mathf.InverseLerp(num - 30f, num, num3);
//							CombatPatches.availableShootingTargets.Add(new Pair<IAttackTarget, float>(rawTargets[j], second));
//						}
//					}
//				}
//			}
//			return CombatPatches.availableShootingTargets;
//		}
//
//		private static float GetShootingTargetScore(IAttackTarget target, IAttackTargetSearcher searcher, Verb verb)
//		{
//			float num = 60f;
//			num -= Mathf.Min((target.Thing.Position - searcher.Thing.Position).LengthHorizontal, 40f);
//			if (target.TargetCurrentlyAimingAt == searcher.Thing)
//			{
//				num += 10f;
//			}
//			if (searcher.LastAttackedTarget == target.Thing && Find.TickManager.TicksGame - searcher.LastAttackTargetTick <= 300)
//			{
//				num += 40f;
//			}
//			num -= CoverUtility.CalculateOverallBlockChance(target.Thing.Position, searcher.Thing.Position, searcher.Thing.Map) * 10f;
//			Pawn pawn = target as Pawn;
//			if (pawn != null && pawn.RaceProps.Animal && pawn.Faction != null && !pawn.IsFighting())
//			{
//				num -= 50f;
//			}
//			num += CombatPatches.FriendlyFireBlastRadiusTargetScoreOffset(target, searcher, verb);
//			num += CombatPatches.FriendlyFireConeTargetScoreOffset(target, searcher, verb);
//			return num * target.TargetPriorityFactor;
//		}
//
//		private static float FriendlyFireBlastRadiusTargetScoreOffset(IAttackTarget target, IAttackTargetSearcher searcher, Verb verb)
//		{
//			if (verb.verbProps.ai_AvoidFriendlyFireRadius <= 0f)
//			{
//				return 0f;
//			}
//			Map map = target.Thing.Map;
//			IntVec3 position = target.Thing.Position;
//			int num = GenRadial.NumCellsInRadius(verb.verbProps.ai_AvoidFriendlyFireRadius);
//			float num2 = 0f;
//			for (int i = 0; i < num; i++)
//			{
//				IntVec3 intVec = position + GenRadial.RadialPattern[i];
//				if (intVec.InBounds(map))
//				{
//					bool flag = true;
//					List<Thing> thingList = intVec.GetThingList(map);
//					for (int j = 0; j < thingList.Count; j++)
//					{
//						if (thingList[j] is IAttackTarget && thingList[j] != target)
//						{
//							if (flag)
//							{
//								if (!GenSight.LineOfSight(position, intVec, map, true, null, 0, 0))
//								{
//									break;
//								}
//								flag = false;
//							}
//							float num3;
//							if (thingList[j] == searcher)
//							{
//								num3 = 40f;
//							}
//							else if (thingList[j] is Pawn)
//							{
//								num3 = (thingList[j].def.race.Animal ? 7f : 18f);
//							}
//							else
//							{
//								num3 = 10f;
//							}
//							if (searcher.Thing.HostileTo(thingList[j]))
//							{
//								num2 += num3 * 0.6f;
//							}
//							else
//							{
//								num2 -= num3;
//							}
//						}
//					}
//				}
//			}
//			return num2;
//		}
//
//		private static float FriendlyFireConeTargetScoreOffset(IAttackTarget target, IAttackTargetSearcher searcher, Verb verb)
//		{
//			Pawn pawn = searcher.Thing as Pawn;
//			if (pawn == null)
//			{
//				return 0f;
//			}
//			if (pawn.RaceProps.intelligence < Intelligence.ToolUser)
//			{
//				return 0f;
//			}
//			if (pawn.RaceProps.IsMechanoid)
//			{
//				return 0f;
//			}
//			Verb_Shoot verb_Shoot = verb as Verb_Shoot;
//			if (verb_Shoot == null)
//			{
//				return 0f;
//			}
//			ThingDef defaultProjectile = verb_Shoot.verbProps.defaultProjectile;
//			if (defaultProjectile == null)
//			{
//				return 0f;
//			}
//			if (defaultProjectile.projectile.flyOverhead)
//			{
//				return 0f;
//			}
//			Map map = pawn.Map;
//			ShotReport report = ShotReport.HitReportFor(pawn, verb, (Thing)target);
//			float radius = Mathf.Max(VerbUtility.CalculateAdjustedForcedMiss(verb.verbProps.forcedMissRadius, report.ShootLine.Dest - report.ShootLine.Source), 1.5f);
//
//			IntVec3 dest2 = report.ShootLine.Dest;
//			IEnumerable<IntVec3> source = from dest in GenRadial.RadialCellsAround(dest2, radius, true)
//										  where dest.InBounds(map)
//										  select dest;
//			IEnumerable<ShootLine> source2 = from dest in source
//											 select new ShootLine(report.ShootLine.Source, dest);
//			IEnumerable<IntVec3> source3 = source2.SelectMany((ShootLine line) => line.Points().Concat(line.Dest).TakeWhile((IntVec3 pos) => pos.CanBeSeenOverFast(map)));
//			IEnumerable<IntVec3> enumerable = source3.Distinct<IntVec3>();
//
//			float num = 0f;
//			foreach (IntVec3 c in enumerable)
//			{
//				float num2 = VerbUtility.InterceptChanceFactorFromDistance(report.ShootLine.Source.ToVector3Shifted(), c);
//				if (num2 > 0f)
//				{
//					List<Thing> thingList = c.GetThingList(map);
//					for (int i = 0; i < thingList.Count; i++)
//					{
//						Thing thing = thingList[i];
//						if (thing is IAttackTarget && thing != target)
//						{
//							float num3;
//							if (thing == searcher)
//							{
//								num3 = 40f;
//							}
//							else if (thing is Pawn)
//							{
//								num3 = (thing.def.race.Animal ? 7f : 18f);
//							}
//							else
//							{
//								num3 = 10f;
//							}
//							num3 *= num2;
//							if (searcher.Thing.HostileTo(thing))
//							{
//								num3 *= 0.6f;
//							}
//							else
//							{
//								num3 *= -1f;
//							}
//							num += num3;
//						}
//					}
//				}
//			}
//			return num;
//		}
//
//		public static IAttackTarget BestShootTargetFromCurrentPosition(IAttackTargetSearcher searcher, TargetScanFlags flags, Predicate<Thing> validator = null, float minDistance = 0f, float maxDistance = 9999f)
//		{
//			Verb currentEffectiveVerb = searcher.CurrentEffectiveVerb;
//			if (currentEffectiveVerb == null)
//			{
//				Log.Error("BestShootTargetFromCurrentPosition with " + searcher.ToStringSafe<IAttackTargetSearcher>() + " who has no attack verb.", false);
//				return null;
//			}
//			return CombatPatches.BestAttackTarget(searcher, flags, validator, Mathf.Max(minDistance, currentEffectiveVerb.verbProps.minRange), Mathf.Min(maxDistance, currentEffectiveVerb.verbProps.range), default(IntVec3), float.MaxValue, false, false);
//		}
//
//		public static bool CanSee(this Thing seer, Thing target, Func<IntVec3, bool> validator = null)
//		{
//			ShootLeanUtility.CalcShootableCellsOf(CombatPatches.tempDestList, target);
//			for (int i = 0; i < CombatPatches.tempDestList.Count; i++)
//			{
//				if (GenSight.LineOfSight(seer.Position, CombatPatches.tempDestList[i], seer.Map, true, validator, 0, 0))
//				{
//					return true;
//				}
//			}
//			ShootLeanUtility.LeanShootingSourcesFromTo(seer.Position, target.Position, seer.Map, CombatPatches.tempSourceList);
//			for (int j = 0; j < CombatPatches.tempSourceList.Count; j++)
//			{
//				for (int k = 0; k < CombatPatches.tempDestList.Count; k++)
//				{
//					if (GenSight.LineOfSight(CombatPatches.tempSourceList[j], CombatPatches.tempDestList[k], seer.Map, true, validator, 0, 0))
//					{
//						return true;
//					}
//				}
//			}
//			return false;
//		}
//
//		public static void DebugDrawAttackTargetScores_Update()
//		{
//			IAttackTargetSearcher attackTargetSearcher = Find.Selector.SingleSelectedThing as IAttackTargetSearcher;
//			if (attackTargetSearcher == null)
//			{
//				return;
//			}
//			if (attackTargetSearcher.Thing.Map != Find.CurrentMap)
//			{
//				return;
//			}
//			Verb currentEffectiveVerb = attackTargetSearcher.CurrentEffectiveVerb;
//			if (currentEffectiveVerb == null)
//			{
//				return;
//			}
//			CombatPatches.tmpTargets.Clear();
//			List<Thing> list = attackTargetSearcher.Thing.Map.listerThings.ThingsInGroup(ThingRequestGroup.AttackTarget);
//			for (int i = 0; i < list.Count; i++)
//			{
//				CombatPatches.tmpTargets.Add((IAttackTarget)list[i]);
//			}
//			List<Pair<IAttackTarget, float>> availableShootingTargetsByScore = CombatPatches.GetAvailableShootingTargetsByScore(CombatPatches.tmpTargets, attackTargetSearcher, currentEffectiveVerb);
//			for (int j = 0; j < availableShootingTargetsByScore.Count; j++)
//			{
//				GenDraw.DrawLineBetween(attackTargetSearcher.Thing.DrawPos, availableShootingTargetsByScore[j].First.Thing.DrawPos);
//			}
//		}
//
//		public static void DebugDrawAttackTargetScores_OnGUI()
//		{
//			IAttackTargetSearcher attackTargetSearcher = Find.Selector.SingleSelectedThing as IAttackTargetSearcher;
//			if (attackTargetSearcher == null)
//			{
//				return;
//			}
//			if (attackTargetSearcher.Thing.Map != Find.CurrentMap)
//			{
//				return;
//			}
//			Verb currentEffectiveVerb = attackTargetSearcher.CurrentEffectiveVerb;
//			if (currentEffectiveVerb == null)
//			{
//				return;
//			}
//			List<Thing> list = attackTargetSearcher.Thing.Map.listerThings.ThingsInGroup(ThingRequestGroup.AttackTarget);
//			Text.Anchor = TextAnchor.MiddleCenter;
//			Text.Font = GameFont.Tiny;
//			for (int i = 0; i < list.Count; i++)
//			{
//				Thing thing = list[i];
//				if (thing != attackTargetSearcher)
//				{
//					string text;
//					Color red;
//					if (!CombatPatches.CanShootAtFromCurrentPosition((IAttackTarget)thing, attackTargetSearcher, currentEffectiveVerb))
//					{
//						text = "out of range";
//						red = Color.red;
//					}
//					else
//					{
//						text = CombatPatches.GetShootingTargetScore((IAttackTarget)thing, attackTargetSearcher, currentEffectiveVerb).ToString("F0");
//						red = new Color(0.25f, 1f, 0.25f);
//					}
//					GenMapUI.DrawThingLabel(thing.DrawPos.MapToUIPosition(), text, red);
//				}
//			}
//			Text.Anchor = TextAnchor.UpperLeft;
//			Text.Font = GameFont.Small;
//		}
//
//		public static bool IsAutoTargetable(IAttackTarget target)
//		{
//			CompCanBeDormant compCanBeDormant = target.Thing.TryGetComp<CompCanBeDormant>();
//			if (compCanBeDormant != null && !compCanBeDormant.Awake)
//			{
//				return false;
//			}
//			CompInitiatable compInitiatable = target.Thing.TryGetComp<CompInitiatable>();
//			return compInitiatable == null || compInitiatable.Initiated;
//		}
//
//		private const float FriendlyFireScoreOffsetPerHumanlikeOrMechanoid = 18f;
//
//		private const float FriendlyFireScoreOffsetPerAnimal = 7f;
//
//		private const float FriendlyFireScoreOffsetPerNonPawn = 10f;
//
//		private const float FriendlyFireScoreOffsetSelf = 40f;
//
//		private static List<IAttackTarget> tmpTargets = new List<IAttackTarget>();
//
//		private static List<Pair<IAttackTarget, float>> availableShootingTargets = new List<Pair<IAttackTarget, float>>();
//
//		private static List<float> tmpTargetScores = new List<float>();
//
//		private static List<bool> tmpCanShootAtTarget = new List<bool>();
//
//		private static List<IntVec3> tempDestList = new List<IntVec3>();
//
//		private static List<IntVec3> tempSourceList = new List<IntVec3>();
	}
}

