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

		[HarmonyPatch(typeof(AttackTargetFinder), "BestAttackTarget")]
		public class CombatPatches_BestAttackTarget_Patch
		{
			public static bool recursiveTrap = false;
			public static bool Prefix(ref IAttackTarget __result, IAttackTargetSearcher searcher, 
				TargetScanFlags flags, Predicate<Thing> validator = null, float minDist = 0f, 
				float maxDist = 9999f, IntVec3 locus = default(IntVec3), 
				float maxTravelRadiusFromLocus = 3.40282347E+38f, bool canBash = false,
				bool canTakeTargetsCloserThanEffectiveMinRange = true)
			{
				bool result = true;
				if (!recursiveTrap)
				{
					recursiveTrap = true;
					Map oldMap = searcher.Thing.Map;
					IntVec3 oldPosition = searcher.Thing.Position;
					foreach (var map in ZUtils.GetAllMapsInClosestOrder(searcher.Thing, oldMap, oldPosition))
					{
						var target = AttackTargetFinder.BestAttackTarget(searcher, flags, validator, minDist,
							maxDist, locus, maxTravelRadiusFromLocus, canBash, canTakeTargetsCloserThanEffectiveMinRange);
						Log.Message(ZUtils.ZTracker.GetMapInfo(searcher.Thing.Map) + " - result: " + target);
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
				return result;
			}

			public static void Postfix(ref IAttackTarget __result)
			{
				Log.Message("1 TEST: " + __result);
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

		//[HarmonyPatch(typeof(JobGiver_AIFightEnemies), "TryGiveJob")]
		//public class JobGiver_AIFightEnemies_Patch
		//{
		//	private static void Postfix(ref JobGiver_AIFightEnemies __instance, ref Job __result, ref Pawn pawn)
		//	{
		//
		//		Log.Message(pawn + " - 3 TEST: " + __result);
		//	}
		//}

		//[HarmonyPatch(typeof(JobGiver_AIDefendPoint), "TryGiveJob")]
		//public class JobGiver_AIDefendPoint_Patch
		//{
		//	private static void Postfix(ref JobGiver_AIDefendPoint __instance, ref Job __result, ref Pawn pawn)
		//	{
		//
		//		Log.Message(pawn + " - 3 TEST: " + __result);
		//	}
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

						Log.Message("2: " + ZUtils.ZTracker.GetMapInfo(pawn.Map) + " - result: " + job);
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
}

