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
    public static class WeatherPatches
    {
        [HarmonyPatch(typeof(WeatherDecider), "ChooseNextWeather")]
        internal static class Patch_ChooseNextWeather
        {
            private static void Postfix(WeatherDecider __instance, WeatherDef __result)
            {
                try
                {
                    Map map = Traverse.Create(__instance).Field("map").GetValue<Map>();
                    var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                    if (ZTracker.ZLevelsTracker != null && ZTracker.ZLevelsTracker.ContainsKey(map.Tile))
                    {
                        ZLogger.Message("Weather decider: " + __result + " - " + ZTracker.GetMapInfo(map));
                        if (ZTracker.GetZIndexFor(map) == 0)
                        {
                            foreach (var map2 in ZTracker.GetAllMaps(map.Tile))
                            {
                                if (ZTracker.GetZIndexFor(map2) > 0)
                                {
                                    ZLogger.Message("1 - " + ZTracker.GetMapInfo(map2) + " transitioting to " + __result);
                                    map2.weatherManager.TransitionTo(__result);
                                    map2.weatherManager.curWeatherAge = map.weatherManager.curWeatherAge;
                                }
                            }
                        }
                        else if (ZTracker.GetZIndexFor(map) > 0)
                        {
                            __result = map.weatherManager.curWeather;
                            ZLogger.Message("2 - " + ZTracker.GetMapInfo(map) + " transitioting to " + __result);

                            map.weatherManager.TransitionTo(__result);
                        }
                        else if (ZTracker.GetZIndexFor(map) < 0)
                        {
                            __result = WeatherDefOf.Clear;
                            ZLogger.Message("3 - " + ZTracker.GetMapInfo(map) + " transitioting to " + __result);
                            map.weatherManager.TransitionTo(__result);
                        }
                        ZLogger.Message("Changed weather for " + ZTracker.GetMapInfo(map) + " - " + __result);
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
            private static void Postfix(WeatherManager __instance, WeatherDef newWeather)
            {
                try
                {
                    Map map = Traverse.Create(__instance).Field("map").GetValue<Map>();
                    var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                    if (ZTracker.ZLevelsTracker != null && ZTracker.ZLevelsTracker.ContainsKey(map.Tile))
                    {
                        ZLogger.Message("2 Weather decider: " + newWeather + " - " + ZTracker.GetMapInfo(map));
                        if (ZTracker.GetZIndexFor(map) == 0)
                        {
                            foreach (var map2 in ZTracker.GetAllMaps(map.Tile))
                            {
                                if (ZTracker.GetZIndexFor(map2) > 0)
                                {
                                    map2.weatherManager.lastWeather = __instance.curWeather;
                                    map2.weatherManager.curWeather = newWeather;
                                    map2.weatherManager.curWeatherAge = map.weatherManager.curWeatherAge;
                                    ZLogger.Message("1.2 - " + ZTracker.GetMapInfo(map2) + " transitioting to " + newWeather);
                                }
                            }
                        }
                        else if (ZTracker.GetZIndexFor(map) > 0)
                        {
                            Map playerMap = ZTracker.GetMapByIndex(map.Tile, 0);
                            __instance.lastWeather = playerMap.weatherManager.lastWeather;
                            __instance.curWeather = playerMap.weatherManager.curWeather;
                            __instance.curWeatherAge = playerMap.weatherManager.curWeatherAge;
                            ZLogger.Message("2.2 - " + ZTracker.GetMapInfo(map) + " transitioting to " + map.weatherManager.curWeather);
                        }
                        else if (ZTracker.GetZIndexFor(map) < 0)
                        {
                            __instance.lastWeather = __instance.curWeather;
                            __instance.curWeather = WeatherDefOf.Clear;
                            __instance.curWeatherAge = 0;
                            ZLogger.Message("3.2 - " + ZTracker.GetMapInfo(map) + " transitioting to " + WeatherDefOf.Clear);
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
            private static bool Prefix(WeatherDecider __instance)
            {
                try
                {
                    Map map = Traverse.Create(__instance).Field("map").GetValue<Map>();
                    var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                    if (ZTracker.ZLevelsTracker != null && ZTracker.ZLevelsTracker.ContainsKey(map.Tile) && ZTracker.GetZIndexFor(map) < 0)
                    {
                        int curWeatherDuration = Traverse.Create(__instance).Field("curWeatherDuration").GetValue<int>();
                        map.weatherManager.curWeather = null;
                        WeatherDef weatherDef = WeatherDefOf.Clear;
                        WeatherDef lastWeather = WeatherDefOf.Clear;
                        map.weatherManager.curWeather = weatherDef;
                        map.weatherManager.lastWeather = lastWeather;
                        curWeatherDuration = weatherDef.durationRange.RandomInRange;
                        map.weatherManager.curWeatherAge = Rand.Range(0, curWeatherDuration);
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
    }
}

