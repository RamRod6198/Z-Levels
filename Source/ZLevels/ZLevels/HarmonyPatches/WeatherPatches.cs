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
using Verse.Sound;

namespace ZLevels
{
    [StaticConstructorOnStartup]
    public static class WeatherPatches
    {
        [HarmonyPatch(typeof(WeatherDecider), "ChooseNextWeather")]
        internal static class Patch_ChooseNextWeather
        {
            private static void Postfix(WeatherDecider __instance, WeatherDef __result, Map ___map)
            {
                try
                {
                    var ZTracker = ZUtils.ZTracker;
                    if (ZTracker.ZLevelsTracker != null && ZTracker.ZLevelsTracker.ContainsKey(___map.Tile))
                    {
                        ZLogger.Message("Weather decider: " + __result + " - " + ZTracker.GetMapInfo(___map));
                        if (ZTracker.GetZIndexFor(___map) == 0)
                        {
                            foreach (var map2 in ZTracker.GetAllMaps(___map.Tile))
                            {
                                if (ZTracker.GetZIndexFor(map2) > 0)
                                {
                                    ZLogger.Message("1 - " + ZTracker.GetMapInfo(map2) + " transitioting to " + __result);
                                    map2.weatherManager.TransitionTo(__result);
                                    map2.weatherManager.curWeatherAge = ___map.weatherManager.curWeatherAge;
                                }
                            }
                        }
                        else if (ZTracker.GetZIndexFor(___map) > 0)
                        {
                            __result = ___map.weatherManager.curWeather;
                            ZLogger.Message("2 - " + ZTracker.GetMapInfo(___map) + " transitioting to " + __result);

                            ___map.weatherManager.TransitionTo(__result);
                        }
                        else if (ZTracker.GetZIndexFor(___map) < 0)
                        {
                            __result = WeatherDefOf.Clear;
                            ZLogger.Message("3 - " + ZTracker.GetMapInfo(___map) + " transitioting to " + __result);
                            ___map.weatherManager.TransitionTo(__result);
                        }
                        ZLogger.Message("Changed weather for " + ZTracker.GetMapInfo(___map) + " - " + __result);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("[Z-Levels] Patch_ChooseNextWeather patch produced an error. That should not happen and will break things. Send a Hugslib log to the Z-Levels developers. Error message: " + ex, true);
                }
            }
        }

        [HarmonyPatch(typeof(WeatherManager), "TransitionTo")]
        internal static class Patch_TransitionTo
        {
            private static void Postfix(WeatherManager __instance, WeatherDef newWeather, Map ___map)
            {
                try
                {
                    var ZTracker = ZUtils.ZTracker;
                    if (ZTracker.ZLevelsTracker != null && ZTracker.ZLevelsTracker.ContainsKey(___map.Tile))
                    {
                        ZLogger.Message("2 Weather decider: " + newWeather + " - " + ZTracker.GetMapInfo(___map));
                        if (ZTracker.GetZIndexFor(___map) == 0)
                        {
                            foreach (var map2 in ZTracker.GetAllMaps(___map.Tile))
                            {
                                if (ZTracker.GetZIndexFor(map2) > 0)
                                {
                                    map2.weatherManager.lastWeather = __instance.curWeather;
                                    map2.weatherManager.curWeather = newWeather;
                                    map2.weatherManager.curWeatherAge = ___map.weatherManager.curWeatherAge;
                                    ZLogger.Message("1.2 - " + ZTracker.GetMapInfo(map2) + " transitioting to " + newWeather);
                                }
                            }
                        }
                        else if (ZTracker.GetZIndexFor(___map) > 0)
                        {
                            Map playerMap = ZTracker.GetMapByIndex(___map.Tile, 0);
                            __instance.lastWeather = playerMap.weatherManager.lastWeather;
                            __instance.curWeather = playerMap.weatherManager.curWeather;
                            __instance.curWeatherAge = playerMap.weatherManager.curWeatherAge;
                            ZLogger.Message("2.2 - " + ZTracker.GetMapInfo(___map) + " transitioting to " + ___map.weatherManager.curWeather);
                        }
                        else if (ZTracker.GetZIndexFor(___map) < 0)
                        {
                            __instance.lastWeather = __instance.curWeather;
                            __instance.curWeather = WeatherDefOf.Clear;
                            __instance.curWeatherAge = 0;
                            ZLogger.Message("3.2 - " + ZTracker.GetMapInfo(___map) + " transitioting to " + WeatherDefOf.Clear);
                        }
                    }

                }
                catch (Exception ex)
                {
                    Log.Error("[Z-Levels] Patch_TransitionTo patch produced an error. That should not happen and will break things. Send a Hugslib log to the Z-Levels developers. Error message: " + ex, true);
                }
            }
        }

        [HarmonyPatch(typeof(WeatherDecider), "StartInitialWeather")]
        internal static class Patch_WeatherManager
        {
            private static bool Prefix(WeatherDecider __instance, Map ___map, ref int ___curWeatherDuration)
            {
                try
                {
                    Log.Message(" - Prefix - var ZTracker = ZUtils.ZTracker; - 1", true);
                    var ZTracker = ZUtils.ZTracker;
                    Log.Message(" - Prefix - if (ZTracker.ZLevelsTracker != null && ZTracker.ZLevelsTracker.ContainsKey(___map.Tile) - 2", true);
                    if (ZTracker.ZLevelsTracker != null && ZTracker.ZLevelsTracker.ContainsKey(___map.Tile)
                        && ZTracker.GetZIndexFor(___map) < 0)
                    {
                        Log.Message(" - Prefix - ___map.weatherManager.curWeather = null; - 3", true);
                        ___map.weatherManager.curWeather = null;
                        Log.Message(" - Prefix - WeatherDef weatherDef = WeatherDefOf.Clear; - 4", true);
                        WeatherDef weatherDef = WeatherDefOf.Clear;
                        Log.Message(" - Prefix - WeatherDef lastWeather = WeatherDefOf.Clear; - 5", true);
                        WeatherDef lastWeather = WeatherDefOf.Clear;
                        Log.Message(" - Prefix - ___map.weatherManager.curWeather = weatherDef; - 6", true);
                        ___map.weatherManager.curWeather = weatherDef;
                        Log.Message(" - Prefix - ___map.weatherManager.lastWeather = lastWeather; - 7", true);
                        ___map.weatherManager.lastWeather = lastWeather;
                        Log.Message(" - Prefix - ___curWeatherDuration = weatherDef.durationRange.RandomInRange; - 8", true);
                        ___curWeatherDuration = weatherDef.durationRange.RandomInRange;
                        Log.Message(" - Prefix - ___map.weatherManager.curWeatherAge = Rand.Range(0, ___curWeatherDuration); - 9", true);
                        ___map.weatherManager.curWeatherAge = Rand.Range(0, ___curWeatherDuration);
                        Log.Message(" - Prefix - return false; - 10", true);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Log.Message(" - Prefix - Log.Error(\"[Z-Levels] Patch_WeatherManager patch produced an error. That should not happen and will break things. Send a Hugslib log to the Z-Levels developers. Error message: \" + ex, true); - 11", true);
                    Log.Error("[Z-Levels] Patch_WeatherManager patch produced an error. That should not happen and will break things. Send a Hugslib log to the Z-Levels developers. Error message: " + ex, true);
                }
                return true;
            }

        }

        [HarmonyPatch(typeof(WeatherEvent_LightningStrike), "FireEvent")]
        internal static class Patch_FireEvent
        {
            private static bool Prefix(WeatherEvent_LightningStrike __instance, Map ___map)
            {
                try
                {
                    var ZTracker = ZUtils.ZTracker;
                    if (ZTracker.ZLevelsTracker != null && ZTracker.ZLevelsTracker.ContainsKey(___map.Tile) 
                        && ZTracker.GetZIndexFor(___map) > 0)
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("[Z-Levels] Patch_WeatherManager patch produced an error. That should not happen and will break things. Send a Hugslib log to the Z-Levels developers. Error message: " + ex, true);
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(GameConditionManager), "RegisterCondition")]
        internal static class RegisterConditionPatch
        {
            public static bool AddCondition = false;

            public static List<GameConditionDef> blackList = new List<GameConditionDef>
            {
                GameConditionDefOf.Aurora,
                GameConditionDefOf.Eclipse,
                GameConditionDefOf.SolarFlare,
                GameConditionDefOf.ToxicFallout,
                GameConditionDefOf.VolcanicWinter,
                GameConditionDefOf.Flashstorm,
                GameConditionDefOf.ToxicSpewer,
                GameConditionDefOf.WeatherController,
                GameConditionDef.Named("SunBlocker"),
                GameConditionDef.Named("GiantSmokeCloud"),
            };
            private static bool Prefix(GameConditionManager __instance, GameCondition cond)
            {
                var ind = ZUtils.ZTracker.GetZIndexFor(__instance.ownerMap);
                if (ind == 0 && !(__instance.ownerMap.Parent is MapParent_ZLevel))
                {
                    AddCondition = true;
                    foreach (var map in ZUtils.ZTracker.GetAllMaps(__instance.ownerMap.Tile))
                    {
                        if (map != __instance.ownerMap)
                        {
                            ZLogger.Message("Register: " + cond + " in the " + map, true);
                            map.gameConditionManager.RegisterCondition(cond);
                        }
                    }
                    AddCondition = false;
                }
                else if (ind < 0 && (!AddCondition || blackList.Contains(cond.def)))
                {
                    return false;
                }
                else if (ind > 0 && !AddCondition)
                {
                    return false;
                }
                return true;
            }
        }
    }
}