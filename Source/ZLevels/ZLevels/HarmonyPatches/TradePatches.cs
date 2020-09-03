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
    public static class TradePatches
    {
        [HarmonyPatch(typeof(Pawn_TraderTracker), "ColonyThingsWillingToBuy")]
        public class Pawn_TraderTrackerPatch
        {
            [HarmonyPostfix]
            private static void Postfix(Pawn ___pawn, ref IEnumerable<Thing> __result, Pawn playerNegotiator)
            {
                var result = __result.ToList();
                Map oldMap = ___pawn.Map;
                IntVec3 oldPosition = playerNegotiator.Position;
                bool select = false;
                if (Find.Selector.SelectedObjects.Contains(___pawn)) select = true;
                foreach (var map in ZUtils.GetAllMapsInClosestOrder(___pawn, oldMap, oldPosition))
                {
                    if (map != oldMap)
                    {
                        IEnumerable<Thing> enumerable = ___pawn.Map.listerThings.AllThings.Where((Thing x) 
                            => x.def.category == ThingCategory.Item && TradeUtility.PlayerSellableNow(x, ___pawn)
                            && !x.Position.Fogged(x.Map) && (___pawn.Map.areaManager.Home[x.Position] 
                            || x.IsInAnyStorage()) && ReachableForTrade(x, ___pawn));
                        foreach (Thing item in enumerable)
                        {
                            result.Add(item);
                        }
                        if (___pawn.GetLord() != null)
                        {
                            foreach (Pawn item2 in from x in TradeUtility.AllSellableColonyPawns(___pawn.Map)
                                                   where !x.Downed && ReachableForTrade(x, ___pawn)
                                                   select x)
                            {
                                result.Add(item2);
                            }
                        }
                    }
                }
                ZUtils.TeleportThing(___pawn, oldMap, oldPosition);
                if (select) Find.Selector.Select(___pawn);
                __result = result;
            }
            public static bool ReachableForTrade(Thing thing, Pawn pawn)
            {
                if (pawn.Map != thing.Map)
                {
                    return false;
                }
                return pawn.Map.reachability.CanReach(pawn.Position, thing, PathEndMode.Touch, TraverseMode.PassDoors, Danger.Some);
            }
        }
    }
}

