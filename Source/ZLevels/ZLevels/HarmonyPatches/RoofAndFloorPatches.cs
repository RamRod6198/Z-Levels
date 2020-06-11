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
        [HarmonyPatch(typeof(RoofGrid), "SetRoof")]
        internal static class Patch_SetRoof
        {
            private static void Postfix(RoofGrid __instance, ref IntVec3 c, ref RoofDef def)
            {
                try
                {
                    if (def != null && !def.isNatural)
                    {
                        Map map = Traverse.Create(__instance).Field("map").GetValue<Map>();
                        var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                        var upperMap = ZTracker.GetUpperLevel(map.Tile, map);
                        if (upperMap != null && upperMap.terrainGrid.TerrainAt(c) != ZLevelsDefOf.ZL_RoofTerrain)
                        {
                            upperMap.terrainGrid.SetTerrain(c, ZLevelsDefOf.ZL_RoofTerrain);
                        }
                    }
                }
                catch (Exception ex)
                {

                };
            }
        }

        [HarmonyPatch(typeof(TerrainGrid), "SetTerrain")]
        internal static class Patch_SetTerrain
        {
            private static void Postfix(TerrainGrid __instance, ref IntVec3 c, ref TerrainDef newTerr)
            {
                try
                {
                    if (newTerr != null && newTerr.Removable)
                    {
                        var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                        Map map = Traverse.Create(__instance).Field("map").GetValue<Map>();
                        var lowerMap = ZTracker.GetLowerLevel(map.Tile, map);
                        if (lowerMap != null && lowerMap.roofGrid.RoofAt(c) != RoofDefOf.RoofConstructed)
                        {
                            lowerMap.roofGrid.SetRoof(c, RoofDefOf.RoofConstructed);
                        }
                    }
                    else if (newTerr == null)
                    {
                        var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                        Map map = Traverse.Create(__instance).Field("map").GetValue<Map>();
                        if (ZTracker.GetZIndexFor(map) > 0)
                        {
                            foreach (var t in c.GetThingList(map))
                            {
                                if (t is Pawn pawn)
                                {
                                    pawn.Kill(null);
                                }
                                else
                                {
                                    t.Destroy(DestroyMode.Refund);
                                }
                            }
                            map.terrainGrid.SetTerrain(c, ZLevelsDefOf.ZL_OutsideTerrain);
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

