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
    public static class SkyfallerPatches
    {

        [HarmonyPatch(typeof(SkyfallerMaker))]
        [HarmonyPatch("SpawnSkyfaller")]
        [HarmonyPatch(new Type[] { typeof(ThingDef), typeof(IntVec3), typeof(Map) })]
        internal static class Patch_SpawnSkyfaller1
        {
            private static void Postfix(ref Skyfaller __result, ThingDef skyfaller, IntVec3 pos, Map map)
            {
                ZLogger.Message("Spawning " + __result);

                if (pos.Roofed(map))
                {
                    var ZTracker = ZUtils.ZTracker;
                    var upperMap = ZTracker.ZLevelsTracker[map.Tile]
                        .ZLevels.Values.Where(x => !pos.Roofed(x)
                        && pos.GetTerrain(x) != ZLevelsDefOf.ZL_OutsideTerrain)
                        .OrderByDescending(x => ZTracker.GetZIndexFor(x)).FirstOrDefault();
                    if (upperMap != null)
                    {
                        ZTracker.TeleportThing(__result, pos, upperMap);
                        ZLogger.Message("Skyfaller: " + __result + " spawning in " + ZTracker.GetMapInfo(upperMap));
                    }
                    else
                    {
                        ZLogger.Message("Cant find unroofed map for " + __result);
                    }
                    ZLogger.Message("Roofed");

                }
            }
        }

        [HarmonyPatch(typeof(SkyfallerMaker))]
        [HarmonyPatch("SpawnSkyfaller")]
        [HarmonyPatch(new Type[] { typeof(ThingDef), typeof(ThingDef), typeof(IntVec3), typeof(Map) })]
        internal static class Patch_SpawnSkyfaller2
        {
            private static void Postfix(ref Skyfaller __result, ThingDef skyfaller, ThingDef innerThing, IntVec3 pos, Map map)
            {
                ZLogger.Message("Spawning " + __result);

                if (pos.Roofed(map))
                {
                    var ZTracker = ZUtils.ZTracker;
                    var upperMap = ZTracker.ZLevelsTracker[map.Tile]
                        .ZLevels.Values.Where(x => !pos.Roofed(x)
                        && pos.GetTerrain(x) != ZLevelsDefOf.ZL_OutsideTerrain)
                        .OrderByDescending(x => ZTracker.GetZIndexFor(x)).FirstOrDefault(); 
                    if (upperMap != null)
                    {
                        ZTracker.TeleportThing(__result, pos, upperMap);
                        ZLogger.Message("Skyfaller: " + __result + " spawning in " + ZTracker.GetMapInfo(upperMap));
                    }
                    else
                    {
                        ZLogger.Message("Cant find unroofed map for " + __result);
                    }
                    ZLogger.Message("Roofed");

                }
            }
        }

        [HarmonyPatch(typeof(SkyfallerMaker))]
        [HarmonyPatch("SpawnSkyfaller")]
        [HarmonyPatch(new Type[] { typeof(ThingDef), typeof(Thing), typeof(IntVec3), typeof(Map) })]
        internal static class Patch_SpawnSkyfaller3
        {
            private static void Postfix(ref Skyfaller __result, ThingDef skyfaller, Thing innerThing, IntVec3 pos, Map map)
            {
                ZLogger.Message("Spawning " + __result);

                if (pos.Roofed(map))
                {
                    var ZTracker = ZUtils.ZTracker;
                    var upperMap = ZTracker.ZLevelsTracker[map.Tile]
                        .ZLevels.Values.Where(x => !pos.Roofed(x) 
                        && pos.GetTerrain(x) != ZLevelsDefOf.ZL_OutsideTerrain)
                        .OrderByDescending(x => ZTracker.GetZIndexFor(x)).FirstOrDefault();
                    if (upperMap != null)
                    {
                        ZTracker.TeleportThing(__result, pos, upperMap);
                        ZLogger.Message("Skyfaller: " + __result + " spawning in " + ZTracker.GetMapInfo(upperMap));
                    }
                    else
                    {
                        ZLogger.Message("Cant find unroofed map for " + __result);
                    }
                    ZLogger.Message("Roofed");

                }
            }
        }

        [HarmonyPatch(typeof(SkyfallerMaker))]
        [HarmonyPatch("SpawnSkyfaller")]
        [HarmonyPatch(new Type[] { typeof(ThingDef), typeof(IEnumerable<Thing>), typeof(IntVec3), typeof(Map) })]
        internal static class Patch_SpawnSkyfaller4
        {
            private static void Postfix(ref Skyfaller __result, ThingDef skyfaller, IEnumerable<Thing> things, IntVec3 pos, Map map)
            {
                ZLogger.Message("Spawning " + __result);
                if (pos.Roofed(map))
                {
                    var ZTracker = ZUtils.ZTracker;
                    var upperMap = ZTracker.ZLevelsTracker[map.Tile]
                        .ZLevels.Values.Where(x => !pos.Roofed(x)
                        && pos.GetTerrain(x) != ZLevelsDefOf.ZL_OutsideTerrain)
                        .OrderByDescending(x => ZTracker.GetZIndexFor(x)).FirstOrDefault(); 
                    if (upperMap != null)
                    {
                        ZTracker.TeleportThing(__result, pos, upperMap);
                        ZLogger.Message("Skyfaller: " + __result + " spawning in " + ZTracker.GetMapInfo(upperMap));
                    }
                    else
                    {
                        ZLogger.Message("Cant find unroofed map for " + __result);
                    }
                    ZLogger.Message("Roofed");
                }
            }
        }
    }
}

