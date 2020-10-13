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

        [HarmonyPatch(typeof(Map))]
        [HarmonyPatch("ParentFaction", MethodType.Getter)]
        public class ParentFaction_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(Map __instance, ref Faction __result)
            {
                try
                {
                    if (__instance != null && __instance.ParentHolder is MapParent_ZLevel)
                    {
                        __result = ZUtils.ZTracker.GetMapByIndex(__instance.Tile, 0).ParentFaction; ;
                        return false;
                    }
                }
                catch { };
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
                    if (t.def == ThingDefOf.SteamGeyser && ZUtils.ZTracker.GetZIndexFor(t.Map) < 0 && ZUtils.ZTracker.GetUpperLevel(t.Map.Tile,
                        t.Map).listerThings.AllThings.Where(x => x.def == ThingDefOf.SteamGeyser && x.Position == t.Position).Any())
                    {
                        return false;
                    }
                }
                catch { };
                return true;
            }
        }

        [HarmonyPatch(typeof(CompAssignableToPawn))]
        [HarmonyPatch("get_AssigningCandidates")]
        internal static class Patch_CandidatesFromAllLevels
        {
            private static void Postfix(ref IEnumerable<Pawn> __result, CompAssignableToPawn __instance)
            {
                if (__instance.parent.Spawned)
                {
                    var list = __result.ToList();
                    foreach (var map in ZUtils.ZTracker.GetAllMaps(__instance.parent.Map.Tile))
                    {
                        if (map != __instance.parent.Map)
                        {
                            list.AddRange(map.mapPawns.FreeColonists);
                        }
                    }
                    __result = list;
                }
            }
        }
    }
}
