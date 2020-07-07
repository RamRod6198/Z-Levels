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
    public static class MapPatches
    {
        [HarmonyPatch(typeof(Map), "Biome", MethodType.Getter)]
        public class GetBiomePatch
        {
            [HarmonyPrefix]
            private static bool BiomePatch(Map __instance, ref BiomeDef __result)
            {
                if (__instance.ParentHolder is MapParent_ZLevel parent)
                {
                    if (parent.IsUnderground)
                    {
                        __result = ZLevelsDefOf.ZL_UndegroundBiome;
                    }
                    else if (parent.IsUpperLevel)
                    {
                        __result = ZLevelsDefOf.ZL_UpperBiome;
                    }
                    else if (!parent.IsUpperLevel && !parent.IsUnderground)
                    {
                        if (parent.Z_LevelIndex < 0)
                        {
                            parent.IsUnderground = true;
                            __result = ZLevelsDefOf.ZL_UndegroundBiome;
                        }
                        else if (parent.Z_LevelIndex > 0)
                        {
                            parent.IsUpperLevel = true;
                            __result = ZLevelsDefOf.ZL_UpperBiome;
                        }
                        else if (parent.Z_LevelIndex == 0)
                        {
                            var ind = ZUtils.ZTracker.GetZIndexFor(__instance);
                            if (ind < 0)
                            {
                                parent.IsUnderground = true;
                                __result = ZLevelsDefOf.ZL_UndegroundBiome;
                            }
                            else if (ind > 0)
                            {
                                parent.IsUpperLevel = true;
                                __result = ZLevelsDefOf.ZL_UpperBiome;
                            }
                            parent.Z_LevelIndex = ind;
                        }
                    }
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(ExitMapGrid))]
        [HarmonyPatch("MapUsesExitGrid", MethodType.Getter)]
        public class ExitCells_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(ExitMapGrid __instance, Map ___map, ref bool __result)
            {
                if (___map != null && ___map.ParentHolder is MapParent_ZLevel)
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(GenTemperature), "PushHeat")]
        [HarmonyPatch(new Type[]
        {
            typeof(Thing),
            typeof(float)
        }, new ArgumentType[]
        {
            0,
            0
        })]
        public class HeatPush_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(Thing t, float energy)
            {
                try
                {
                    if (t is Building_SteamGeyser 
                        && ZUtils.ZTracker.GetZIndexFor(t.Map) < 0 && ZUtils.ZTracker.GetUpperLevel(t.Map.Tile,
                        t.Map).listerThings.AllThings.Where(x => x is Building_SteamGeyser 
                        && x.Position == t.Position).Count() > 0)
                    {
                        return false;
                    }
                }
                catch { };
                return true;
            }
        }
    }
}
