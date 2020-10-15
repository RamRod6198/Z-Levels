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

namespace ZLevels
{
	[StaticConstructorOnStartup]
	public static class WealthPatches
	{

		[HarmonyPatch(typeof(WealthWatcher), "WealthItems", MethodType.Getter)]
		internal static class WealthItemsPatch
		{
			[HarmonyPrefix]
			public static bool WealthItems(WealthWatcher __instance, Map ___map, ref float __result, float ___lastCountTick)
			{
				float result = 0;
				foreach (var map in ZUtils.ZTracker.GetAllMaps(___map.Tile))
				{
					if (Find.TickManager.TicksGame - ___lastCountTick > 5000f)
					{
						map.wealthWatcher.ForceRecount();
					}
					var value = map.wealthWatcher.wealthItems;
					result += value;
					//ZLogger.Message("Analyzing wealthItems: " + map + " - value: " + value);
				}
				//ZLogger.Message("New result: " + result);
				__result = result;
				return false;
			}
		}

		[HarmonyPatch(typeof(WealthWatcher), "WealthBuildings", MethodType.Getter)]
		internal static class WealthBuildingsPatch
		{
			[HarmonyPrefix]
			public static bool WealthBuildings(WealthWatcher __instance, Map ___map, ref float __result, float ___lastCountTick)
			{
				float result = 0;
				foreach (var map in ZUtils.ZTracker.GetAllMaps(___map.Tile))
				{
					if (Find.TickManager.TicksGame - ___lastCountTick > 5000f)
					{
						map.wealthWatcher.ForceRecount();
					}
					var value = map.wealthWatcher.wealthBuildings;
					result += value;
					//ZLogger.Message("Analyzing wealthBuildings: " + map + " - value: " + value);
				}
				//ZLogger.Message("New result: " + result);
				__result = result;
				return false;
			}
		}

		[HarmonyPatch(typeof(WealthWatcher), "WealthFloorsOnly", MethodType.Getter)]
		internal static class WealthFloorsOnlyPatch
		{
			[HarmonyPrefix]
			public static bool WealthFloorsOnly(WealthWatcher __instance, Map ___map, ref float __result, float ___lastCountTick)
			{
				float result = 0;
				foreach (var map in ZUtils.ZTracker.GetAllMaps(___map.Tile))
				{
					if ((float)Find.TickManager.TicksGame - ___lastCountTick > 5000f)
					{
						map.wealthWatcher.ForceRecount();
					}
					var value = map.wealthWatcher.wealthFloorsOnly;
					result += value;
					//ZLogger.Message("Analyzing wealthFloorsOnly: " + map + " - value: " + value);
				}
				//ZLogger.Message("New result: " + result);
				__result = result;
				return false;
			}
		}

		[HarmonyPatch(typeof(WealthWatcher), "WealthPawns", MethodType.Getter)]
		internal static class WealthPawnsPatch
		{
			[HarmonyPrefix]
			public static bool WealthPawns(WealthWatcher __instance, Map ___map, ref float __result, float ___lastCountTick)
			{
				float result = 0;
				foreach (var map in ZUtils.ZTracker.GetAllMaps(___map.Tile))
				{
					if ((float)Find.TickManager.TicksGame - ___lastCountTick > 5000f)
					{
						map.wealthWatcher.ForceRecount();
					}
					var value = map.wealthWatcher.wealthPawns;
					result += value;
					//ZLogger.Message("Analyzing wealthPawns: " + map + " - value: " + value);
				}
				//ZLogger.Message("New result: " + result);
				__result = result;
				return false;
			}
		}

		[HarmonyPatch(typeof(WealthWatcher), "WealthTotal", MethodType.Getter)]
		internal static class WealthTotalPatch
		{
			[HarmonyPrefix]
			public static bool WealthTotal(WealthWatcher __instance, Map ___map, ref float __result, float ___lastCountTick)
			{
				float result = 0;
				foreach (var map in ZUtils.ZTracker.GetAllMaps(___map.Tile))
				{
					if ((float)Find.TickManager.TicksGame - ___lastCountTick > 5000f)
					{
						map.wealthWatcher.ForceRecount();
					}
					var value = map.wealthWatcher.wealthItems + map.wealthWatcher.wealthBuildings + map.wealthWatcher.wealthPawns;
					result += value;
					//ZLogger.Message("Analyzing WealthTotal: " + map + " - value: " + value);
				}
				//ZLogger.Message("New result: " + result);
				__result = result;
				return false;
			}
		}
	}
}
