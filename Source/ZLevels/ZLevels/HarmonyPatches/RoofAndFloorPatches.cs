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
    public static class RoofAndFloorPatches
    {
        public static bool IsAllowedToSpawnBelow(this Thing thing)
        {
            if (thing is Mineable || thing is Blueprint || thing is Frame || thing is Explosion)
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(DamageWorker), "ExplosionDamageTerrain")]
        internal static class Patch_ExplosionDamageTerrain
        {
            private static bool Prefix(DamageWorker __instance, Explosion explosion, IntVec3 c)
            {
                if (c.GetTerrain(explosion.Map) == ZLevelsDefOf.ZL_RoofTerrain)
                {
                    if ((float)explosion.GetDamageAmountAt(c) >= ZLevelsDefOf.ZL_RoofTerrain.destroyOnBombDamageThreshold)
                    {
                        explosion.Map.terrainGrid.Notify_TerrainDestroyed(c);
                    }
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(TerrainGrid), "RemoveTopLayer")]
        public static class DestroyedTerrain
        {
            public static bool Prefix(TerrainGrid __instance, IntVec3 c, bool doLeavings, Map ___map, ref TerrainDef[] ___underGrid)
            {
                try
                {
                    var ZTracker = ZUtils.ZTracker;
                    if (ZTracker.GetZIndexFor(___map) > 0)
                    {
                        int num = ___map.cellIndices.CellToIndex(c);
                        if (doLeavings)
                        {
                            GenLeaving.DoLeavingsFor(__instance.topGrid[num], c, ___map);
                        }
                        if (___underGrid[num] != null)
                        {
                            __instance.topGrid[num] = ___underGrid[num];
                            ___underGrid[num] = null;
                            __instance.DoTerrainChangedEffects(c);
                        }
                        if (c.GetTerrain(___map) == TerrainDefOf.Sand)
                        {
                            __instance.SetTerrain(c, ZLevelsDefOf.ZL_OutsideTerrain);
                        }

                        Map lowerMap = ZTracker.GetLowerLevel(___map.Tile, ___map);
                        bool firstTime = false;
                        if (lowerMap == null)
                        {
                            return false;
                        }

                        var thingList = c.GetThingList(___map);
                        if (thingList.Where(x => x is Blueprint || x is Frame).Count() == 0)
                        {
                            for (int i = thingList.Count - 1; i >= 0; i--)
                            {
                                if (thingList[i].IsAllowedToSpawnBelow())
                                {
                                    ZTracker.TeleportThing(thingList[i], c, lowerMap, firstTime, 10);
                                }
                            }
                        }
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("[Z-Levels] DestroyedTerrain patch produced an error. That should not happen and will break things. Send a Hugslib log to the Z-Levels developers. Error message: " + ex, true);
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Thing))]
        [HarmonyPatch("SpawnSetup")]
        public static class Patch_SpawnSetup
        {
            public static bool doTeleport = true;
            public static void Postfix(Thing __instance)
            {
                try
                {
                    if (!doTeleport) return;
                    if (__instance.Position.GetTerrain(__instance.Map) == ZLevelsDefOf.ZL_OutsideTerrain)
                    {
                        var ZTracker = ZUtils.ZTracker;
                        Map lowerMap = ZTracker.GetLowerLevel(__instance.Map.Tile, __instance.Map);
                        bool firstTime = false;
                        if (lowerMap == null)
                        {
                            return;
                            //lowerMap = ZTracker.CreateLowerLevel(__instance.Map, __instance.Position);
                        }
                        var thingList = __instance.Position.GetThingList(__instance.Map);
                        if (thingList.Where(x => x is Blueprint || x is Frame).Count() == 0)
                        {
                            for (int i = thingList.Count - 1; i >= 0; i--)
                            {
                                if (thingList[i].IsAllowedToSpawnBelow())
                                {
                                    ZTracker.TeleportThing(thingList[i], __instance.Position, lowerMap, firstTime, 10);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("[Z-Levels] Patch_SpawnSetup patch produced an error. That should not happen and will break things. Send a Hugslib log to the Z-Levels developers. Error message: " + ex, true);
                }
            }
        }

        [HarmonyPatch(typeof(RoofGrid), "SetRoof")]
        internal static class Patch_SetRoof
        {
            private static void Prefix(RoofGrid __instance, ref IntVec3 c, ref RoofDef def, Map ___map)
            {
                try
                {
                    if (def != null && !def.isNatural)
                    {
                        var ZTracker = ZUtils.ZTracker;
                        var upperMap = ZTracker.GetUpperLevel(___map.Tile, ___map);
                        if (upperMap != null && upperMap.terrainGrid.TerrainAt(c) == ZLevelsDefOf.ZL_OutsideTerrain)
                        {
                            upperMap.terrainGrid.SetTerrain(c, ZLevelsDefOf.ZL_RoofTerrain);
                        }
                    }
                    else if (def == null)
                    {
                        if (c.GetRoof(___map) == RoofDefOf.RoofConstructed)
                        {
                            var ZTracker = ZUtils.ZTracker;
                            Map upperMap = ZTracker.GetUpperLevel(___map.Tile, ___map);
                            if (upperMap != null)
                            {
                                upperMap.terrainGrid.SetTerrain(c, ZLevelsDefOf.ZL_OutsideTerrain);
                                var thingList = c.GetThingList(upperMap);
                                for (int i = thingList.Count - 1; i >= 0; i--)
                                {
                                    if (thingList[i].IsAllowedToSpawnBelow())
                                    {
                                        ZTracker.TeleportThing(thingList[i], c, ___map, false, 10);
                                    }
                                }
                                ZLogger.Message("Removing roof " + c.GetRoof(___map), true);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    //Log.Error("Error in Patch_SetRoof: " + ex);
                };
            }
        }

        [HarmonyPatch(typeof(TerrainGrid), "SetTerrain")]
        internal static class Patch_SetTerrain
        {
            private static void Postfix(TerrainGrid __instance, ref IntVec3 c, ref TerrainDef newTerr, Map ___map)
            {
                try
                {
                    if (newTerr != null && newTerr.Removable)
                    {
                        var ZTracker = ZUtils.ZTracker;
                        var lowerMap = ZTracker.GetLowerLevel(___map.Tile, ___map);
                        if (lowerMap != null && lowerMap.roofGrid.RoofAt(c) != RoofDefOf.RoofConstructed)
                        {
                            lowerMap.roofGrid.SetRoof(c, RoofDefOf.RoofConstructed);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Error in Patch_SetTerrain: " + ex);
                };
            }
        }
    }
}

