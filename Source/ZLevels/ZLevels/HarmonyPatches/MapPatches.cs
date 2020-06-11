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
    public static class MapPatches
    {
        [HarmonyPatch(typeof(Map), "Biome", MethodType.Getter)]
        public class GetBiomePatch
        {
            [HarmonyPostfix]
            private static void MapBiomePostfix(Map __instance, ref BiomeDef __result)
            {
                if (__instance.ParentHolder is MapParent_ZLevel)
                {
                    var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                    if (ZTracker.GetZIndexFor(__instance) < 0)
                    {
                        __result = ZLevelsDefOf.ZL_UndegroundBiome;
                    }
                    else if (ZTracker.GetZIndexFor(__instance) > 0)
                    {
                        __result = ZLevelsDefOf.ZL_UpperBiome;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ExitMapGrid))]
        [HarmonyPatch("MapUsesExitGrid", MethodType.Getter)]
        public class ExitCells_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(ExitMapGrid __instance, ref bool __result)
            {
                Map map = (Map)typeof(ExitMapGrid).GetField("map", BindingFlags.Instance | BindingFlags.Static
                    | BindingFlags.Public | BindingFlags.NonPublic).GetValue(__instance);
                if (map != null)
                {
                    if (map.ParentHolder is MapParent_ZLevel)
                    {
                        __result = false;
                        return false;
                    }
                }
                return true;
            }
        }
    }
}

