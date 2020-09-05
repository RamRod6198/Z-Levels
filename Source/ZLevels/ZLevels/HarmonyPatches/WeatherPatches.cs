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
            private static void Postfix(WeatherDecider __instance, ref WeatherDef __result, Map ___map)
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
                    var ZTracker = ZUtils.ZTracker;
                    if (ZTracker.ZLevelsTracker != null && ZTracker.ZLevelsTracker.ContainsKey(___map.Tile)
                        && ZTracker.GetZIndexFor(___map) < 0)
                    {
                        ___map.weatherManager.curWeather = null;
                        WeatherDef weatherDef = WeatherDefOf.Clear;
                        WeatherDef lastWeather = WeatherDefOf.Clear;
                        ___map.weatherManager.curWeather = weatherDef;
                        ___map.weatherManager.lastWeather = lastWeather;
                        ___curWeatherDuration = weatherDef.durationRange.RandomInRange;
                        ___map.weatherManager.curWeatherAge = Rand.Range(0, ___curWeatherDuration);
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

        [HarmonyPatch(typeof(GameCondition), "End")]
        internal static class End_Patch
        {
            private static void Postfix(GameCondition __instance)
            {
                foreach (var map in __instance.AffectedMaps)
                {
                    if (ZUtils.ZTracker.GetZIndexFor(map) == 0)
                    {
                        foreach (var otherMap in ZUtils.ZTracker.GetAllMaps(map.Tile))
                        {
                            if (ZUtils.ZTracker.GetZIndexFor(otherMap) != 0)
                            {
                                for (int num = otherMap.gameConditionManager.ActiveConditions.Count - 1; num >= 0; num--)
                                {
                                    if (otherMap.gameConditionManager.ActiveConditions[num].def == __instance.def)
                                    {
                                        otherMap.gameConditionManager.ActiveConditions[num].End();
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(GameConditionManager), "RegisterCondition")]
        internal static class RegisterConditionPatch
        {
            public static bool AddCondition = false;
        
            public static List<string> blackList = new List<string>
            {
                "Aurora",
                "Eclipse",
                "SolarFlare",
                "ToxicFallout",
                "VolcanicWinter",
                "Flashstorm",
                "ToxicSpewer",
                "WeatherController",
                "SunBlocker",
                "GiantSmokeCloud",
            };
            private static bool Prefix(GameConditionManager __instance, GameCondition cond)
            {
                if (__instance.ownerMap != null && !AddCondition)
                {
                    var ind = ZUtils.ZTracker.GetZIndexFor(__instance.ownerMap);
                    if (ind == 0 && !(__instance.ownerMap.Parent is MapParent_ZLevel))
                    {
                        AddCondition = true;
                        foreach (var map in ZUtils.ZTracker.GetAllMaps(__instance.ownerMap.Tile))
                        {
                            if (map != __instance.ownerMap && (ZUtils.ZTracker.GetZIndexFor(map) < 0
                                && !blackList.Contains(cond.def?.defName) || ZUtils.ZTracker.GetZIndexFor(map) > 0))
                            {
                                var newCond = GameConditionMaker.MakeCondition(cond.def, cond.Duration);
                                newCond.conditionCauser = cond.conditionCauser;
                                newCond.Permanent = cond.Permanent;
                                newCond.startTick = cond.startTick;
                                newCond.quest = cond.quest;
                                ZLogger.Message("Register: " + newCond + " in the " + ZUtils.ZTracker.GetMapInfo(map), true);
                                map.gameConditionManager.RegisterCondition(newCond);
                            }
                        }
                        AddCondition = false;
                    }
                    else if (ind < 0 && ind != -99999 && blackList.Contains(cond.def?.defName))
                    {
                        return false;
                    }
                    else if (ind > 0)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(GameCondition), "AffectedMaps", MethodType.Getter)]
        internal static class AffectedMapsPatch
        {
            [HarmonyPostfix]
            public static void Postfix(GameCondition __instance, ref List<Map> __result)
            {
                foreach (var tile in ZUtils.ZTracker.ZLevelsTracker.Values)
                {
                    foreach (var map in tile.ZLevels)
                    {
                        if (map.Key < 0 && RegisterConditionPatch.blackList.Contains(__instance?.def?.defName))
                        {
                            //ZLogger.Message("Removing from maps: " + map.Value + " - " + __instance.def);
                            __result.Remove(map.Value);
                        }
                    }
                }
            }
        }

        //[HarmonyPatch(typeof(GameConditionManager), "ElectricityDisabled", MethodType.Getter)]
        //internal static class ElectricityDisabledPatch
        //{
        //    [HarmonyPostfix]
        //    public static void Postfix(GameConditionManager __instance, ref bool __result)
        //    {
        //        ZLogger.Message("TEST: " + __instance.ownerMap + " - " + __result);
        //    }
        //}
        //
        //
        //[HarmonyPatch(typeof(GameConditionManager), "DoConditionsUI")]
        //internal static class DoConditionsUIPatch
        //{
        //    [HarmonyPrefix]
        //    public static bool Prefix(GameConditionManager __instance, Rect rect)
        //    {
        //        if (__instance.ownerMap == null && ZUtils.ZTracker.GetZIndexFor(Find.CurrentMap) < 0)
        //        {
        //            DoConditionsUI(rect, __instance);
        //            return false;
        //        }
        //        return true;
        //    }
        //
        //    public static void DoConditionsUI(Rect rect, GameConditionManager __instance)
        //    {
        //        GUI.BeginGroup(rect);
        //        float num = 0f;
        //        var activeConditions = __instance.ActiveConditions;
        //        for (int i = 0; i < activeConditions.Count; i++)
        //        {
        //            if (RegisterConditionPatch.blackList.Contains(activeConditions[i].def)) continue;
        //            string labelCap = activeConditions[i].LabelCap;
        //            Rect rect2 = new Rect(0f, num, rect.width, Text.CalcHeight(labelCap, rect.width - 6f));
        //            Text.Font = GameFont.Small;
        //            Text.Anchor = TextAnchor.MiddleRight;
        //            Widgets.DrawHighlightIfMouseover(rect2);
        //            Rect rect3 = rect2;
        //            rect3.width -= 6f;
        //            Widgets.Label(rect3, labelCap);
        //            if (Mouse.IsOver(rect2))
        //            {
        //                TooltipHandler.TipRegion(rect2, new TipSignal(activeConditions[i].TooltipString, 0x3A2DF42A ^ i));
        //            }
        //            if (Widgets.ButtonInvisible(rect2))
        //            {
        //                if (activeConditions[i].conditionCauser != null && CameraJumper.CanJump(activeConditions[i].conditionCauser))
        //                {
        //                    CameraJumper.TryJumpAndSelect(activeConditions[i].conditionCauser);
        //                }
        //                else if (activeConditions[i].quest != null)
        //                {
        //                    Find.MainTabsRoot.SetCurrentTab(MainButtonDefOf.Quests);
        //                    ((MainTabWindow_Quests)MainButtonDefOf.Quests.TabWindow).Select(activeConditions[i].quest);
        //                }
        //            }
        //            num += rect2.height;
        //        }
        //        rect.yMin += num;
        //        GUI.EndGroup();
        //        Text.Anchor = TextAnchor.UpperLeft;
        //        if (__instance.Parent != null)
        //        {
        //            __instance.Parent.DoConditionsUI(rect);
        //        }
        //    }
        //}
    }
}
