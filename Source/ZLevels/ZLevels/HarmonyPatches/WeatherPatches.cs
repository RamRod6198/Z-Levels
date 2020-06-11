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
                    Log.Message("Weather decider: " + __result + " - " + ZTracker.GetMapInfo(map));
                    if (ZTracker.GetZIndexFor(map) == 0)
                    {
                        foreach (var map2 in ZTracker.GetAllMaps(map.Tile))
                        {
                            if (ZTracker.GetZIndexFor(map2) > 0)
                            {
                                Log.Message("1 - " + ZTracker.GetMapInfo(map2) + " transitioting to " + __result);
                                map2.weatherManager.TransitionTo(__result);
                                map2.weatherManager.curWeatherAge = map.weatherManager.curWeatherAge;
                            }
                        }
                    }
                    else if (ZTracker.GetZIndexFor(map) > 0)
                    {
                        __result = map.weatherManager.curWeather;
                        Log.Message("2 - " + ZTracker.GetMapInfo(map) + " transitioting to " + __result);

                        map.weatherManager.TransitionTo(__result);
                    }
                    else if (ZTracker.GetZIndexFor(map) < 0)
                    {
                        __result = WeatherDefOf.Clear;
                        Log.Message("3 - " + ZTracker.GetMapInfo(map) + " transitioting to " + __result);
                        map.weatherManager.TransitionTo(__result);
                    }
                    Log.Message("Changed weather for " + ZTracker.GetMapInfo(map) + " - " + __result);
                }
                catch { };
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
                    Log.Message("2 Weather decider: " + newWeather + " - " + ZTracker.GetMapInfo(map));
                    if (ZTracker.GetZIndexFor(map) == 0)
                    {
                        foreach (var map2 in ZTracker.GetAllMaps(map.Tile))
                        {
                            if (ZTracker.GetZIndexFor(map2) > 0)
                            {
                                map2.weatherManager.lastWeather = __instance.curWeather;
                                map2.weatherManager.curWeather = newWeather;
                                map2.weatherManager.curWeatherAge = map.weatherManager.curWeatherAge;
                                Log.Message("1.2 - " + ZTracker.GetMapInfo(map2) + " transitioting to " + newWeather);
                            }
                        }
                    }
                    else if (ZTracker.GetZIndexFor(map) > 0)
                    {
                        Map playerMap = ZTracker.GetMapByIndex(map.Tile, 0);
                        __instance.lastWeather = playerMap.weatherManager.lastWeather;
                        __instance.curWeather = playerMap.weatherManager.curWeather;
                        __instance.curWeatherAge = playerMap.weatherManager.curWeatherAge;
                        Log.Message("2.2 - " + ZTracker.GetMapInfo(map) + " transitioting to " + map.weatherManager.curWeather);
                    }
                    else if (ZTracker.GetZIndexFor(map) < 0)
                    {
                        __instance.lastWeather = __instance.curWeather;
                        __instance.curWeather = WeatherDefOf.Clear;
                        __instance.curWeatherAge = 0;
                        Log.Message("3.2 - " + ZTracker.GetMapInfo(map) + " transitioting to " + WeatherDefOf.Clear);
                    }
                }
                catch { };
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
                    if (ZTracker.GetZIndexFor(map) < 0)
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
                catch { };
                return true;
            }
        }
    }
}

