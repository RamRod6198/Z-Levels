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
			public static bool WealthItems(WealthWatcher __instance, Map ___map, ref float __result)
			{
				float result = 0;
				//Log.Message("Old result: " + Traverse.Create(__instance).Field("wealthItems").GetValue<float>());
				foreach (var map in ZUtils.ZTracker.GetAllMaps(___map.Tile))
				{
					map.wealthWatcher.ForceRecount();
					var value = Traverse.Create(map.wealthWatcher).Field("wealthItems").GetValue<float>();
					result += value;
					//Log.Message("Analyzing wealthItems: " + map + " - value: " + value);
				}
				//Log.Message("New result: " + result);
				__result = result;
				return false;
			}
		}

		[HarmonyPatch(typeof(WealthWatcher), "WealthBuildings", MethodType.Getter)]
		internal static class WealthBuildingsPatch
		{
			[HarmonyPrefix]
			public static bool WealthBuildings(WealthWatcher __instance, Map ___map, ref float __result)
			{
				float result = 0;
				//Log.Message("Old result: " + Traverse.Create(__instance).Field("wealthBuildings").GetValue<float>());
				foreach (var map in ZUtils.ZTracker.GetAllMaps(___map.Tile))
				{
					map.wealthWatcher.ForceRecount();
					var value = Traverse.Create(map.wealthWatcher).Field("wealthBuildings").GetValue<float>();
					result += value;
					//Log.Message("Analyzing wealthBuildings: " + map + " - value: " + value);
				}
				//Log.Message("New result: " + result);
				__result = result;
				return false;
			}
		}

		[HarmonyPatch(typeof(WealthWatcher), "WealthFloorsOnly", MethodType.Getter)]
		internal static class WealthFloorsOnlyPatch
		{
			[HarmonyPrefix]
			public static bool WealthFloorsOnly(WealthWatcher __instance, Map ___map, ref float __result)
			{
				float result = 0;
				//Log.Message("Old result: " + Traverse.Create(__instance).Field("wealthFloorsOnly").GetValue<float>());
				foreach (var map in ZUtils.ZTracker.GetAllMaps(___map.Tile))
				{
					map.wealthWatcher.ForceRecount();
					var value = Traverse.Create(map.wealthWatcher).Field("wealthFloorsOnly").GetValue<float>();
					result += value;
					//Log.Message("Analyzing wealthFloorsOnly: " + map + " - value: " + value);
				}
				//Log.Message("New result: " + result);
				__result = result;
				return false;
			}
		}

		[HarmonyPatch(typeof(WealthWatcher), "WealthPawns", MethodType.Getter)]
		internal static class WealthPawnsPatch
		{
			[HarmonyPrefix]
			public static bool WealthPawns(WealthWatcher __instance, Map ___map, ref float __result)
			{
				float result = 0;
				//Log.Message("Old result: " + Traverse.Create(__instance).Field("wealthPawns").GetValue<float>());
				foreach (var map in ZUtils.ZTracker.GetAllMaps(___map.Tile))
				{
					map.wealthWatcher.ForceRecount();
					var value = Traverse.Create(map.wealthWatcher).Field("wealthPawns").GetValue<float>();
					result += value;
					//Log.Message("Analyzing wealthPawns: " + map + " - value: " + value);
				}
				//Log.Message("New result: " + result);
				__result = result;
				return false;
			}
		}

		[HarmonyPatch(typeof(WealthWatcher), "WealthTotal", MethodType.Getter)]
		internal static class WealthTotalPatch
		{
			[HarmonyPrefix]
			public static bool WealthTotal(WealthWatcher __instance, Map ___map, ref float __result)
			{
				float result = 0;
				//Log.Message("Old result: " + Traverse.Create(__instance).Field("wealthItems").GetValue<float>()
				//				+ Traverse.Create(__instance).Field("wealthBuildings").GetValue<float>()
				//				+ Traverse.Create(__instance).Field("wealthPawns").GetValue<float>());

				foreach (var map in ZUtils.ZTracker.GetAllMaps(___map.Tile))
				{
					map.wealthWatcher.ForceRecount();
					var value = Traverse.Create(map.wealthWatcher).Field("wealthItems").GetValue<float>()
								+ Traverse.Create(map.wealthWatcher).Field("wealthBuildings").GetValue<float>()
								+ Traverse.Create(map.wealthWatcher).Field("wealthPawns").GetValue<float>();
					result += value;
					//Log.Message("Analyzing WealthTotal: " + map + " - value: " + value);
				}
				//Log.Message("New result: " + result);
				__result = result;
				return false;
			}
		}
	}
}