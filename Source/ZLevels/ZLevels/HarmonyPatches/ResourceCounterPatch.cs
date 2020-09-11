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
    public static class ResourceCounterPatches
    {
        [HarmonyPatch(typeof(ResourceCounter))]
        [HarmonyPatch("UpdateResourceCounts")]
        public class UpdateResourceCounts_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(ResourceCounter __instance, Map ___map)
            {
                try
                {
                    var ZTracker = ZUtils.ZTracker;
                    if (ZTracker.ZLevelsTracker.ContainsKey(___map.Tile))
                    {
                        Dictionary<ThingDef, int> countedAllAmounts = new Dictionary<ThingDef, int>();
                        foreach (var map1 in ZTracker.ZLevelsTracker[___map.Tile].ZLevels.Values)
                        {
                            if (map1.resourceCounter != null)
                            {
                                map1.resourceCounter.ResetResourceCounts();
                                List<SlotGroup> allGroupsListForReading = map1.haulDestinationManager.AllGroupsListForReading;
                                for (int i = 0; i < allGroupsListForReading.Count; i++)
                                {
                                    foreach (Thing outerThing in allGroupsListForReading[i].HeldThings)
                                    {
                                        Thing innerIfMinified = outerThing.GetInnerIfMinified();
                                        if (innerIfMinified.def.CountAsResource && !innerIfMinified.IsNotFresh())
                                        {
                                            ThingDef def = innerIfMinified.def;
                                            if (countedAllAmounts.ContainsKey(def))
                                            {
                                                countedAllAmounts[def] += innerIfMinified.stackCount;
                                            }
                                            else
                                            {
                                                countedAllAmounts[def] = innerIfMinified.stackCount;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        foreach (var map1 in ZTracker.ZLevelsTracker[___map.Tile].ZLevels.Values)
                        {
                            if (map1.resourceCounter != null)
                            {
                                foreach (var d in countedAllAmounts)
                                {
                                    map1.resourceCounter.AllCountedAmounts[d.Key] = d.Value;
                                }
                            }
                        }
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("[Z-Levels] UpdateResourceCounts patch produced an error. That should not happen and will break things. Send a Hugslib log to the Z-Levels developers. Error message: " + ex, true);
                };
                return true;
            }
        }

        [HarmonyPatch(typeof(Designator_Build))]
        [HarmonyPatch("ProcessInput")]
        public class ProcessInput_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                FieldInfo godmode = AccessTools.Field(typeof(DebugSettings), "godMode");
                foreach (CodeInstruction i in instructions)
                {
                    yield return i;
                    if (CodeInstructionExtensions.LoadsField(i, godmode, false))
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_1, null);
                        yield return new CodeInstruction(OpCodes.Ldarg_0, null);
                        yield return new CodeInstruction(OpCodes.Ldloc_S, 4);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(
                            typeof(ProcessInput_Patch), "IfInResourceCounter", null));
                    }
                }
                IEnumerator<CodeInstruction> enumerator = null;
            }

            public static bool IfInResourceCounter(bool __result, Event ev, Designator_Build instance, ThingDef thingDef2)
            {
                if (DebugSettings.godMode)
                {
                    return true;
                }
                BuildableDef entDef = instance.entDef;
                ThingDef thingDef = entDef as ThingDef;
                if (thingDef2.IsStuff && thingDef2.stuffProps.CanMake(thingDef))
                {
                    var ZTracker = ZUtils.ZTracker;
                    foreach (var map in ZTracker.GetAllMaps(instance.Map.Tile))
                    {
                        if (map.listerThings.ThingsOfDef(thingDef2).Count > 0)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }
    }
}

