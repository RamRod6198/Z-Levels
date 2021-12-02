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
    public static class PawnPatches
    {
        [HarmonyPatch(typeof(PawnCollisionTweenerUtility), "CanGoDirectlyToNextCell")]
        internal static class Patch_CanGoDirectlyToNextCell
        {
            private static Exception Finalizer(Exception __exception, Pawn pawn)
            {
                if (__exception != null)
                {
                    Log.Error("Z-Levels caught an exception: " + __exception + ", trying to fix it");
                    ZUtils.TryFixPawnError(pawn);
					IntVec3 nextCell = pawn.pather.nextCell;
					foreach (IntVec3 item in CellRect.FromLimits(nextCell, pawn.Position).ExpandedBy(1))
					{
						if (item.InBounds(pawn.Map))
						{
							List<Thing> thingList = item.GetThingList(pawn.Map);
							for (int i = 0; i < thingList.Count; i++)
							{
								Pawn pawn2 = thingList[i] as Pawn;
								if (pawn2 != null && pawn2 != pawn && pawn2.GetPosture() == PawnPosture.Standing)
								{
                                    ZUtils.TryFixPawnError(pawn);
                                }
                            }
						}
					}
					return __exception;
                }
                return null;
            }
        }

        [HarmonyPatch(typeof(PawnCollisionTweenerUtility), "GetPawnsStandingAtOrAboutToStandAt")]
        internal static class Patch_GetPawnsStandingAtOrAboutToStandAt
        {
            private static Exception Finalizer(Exception __exception, IntVec3 at, Map map)
            {
                if (__exception != null)
                {
					foreach (IntVec3 item in CellRect.SingleCell(at).ExpandedBy(1))
					{
						if (item.InBounds(map))
						{
							List<Thing> thingList = item.GetThingList(map);
							for (int i = 0; i < thingList.Count; i++)
							{
								Pawn pawn = thingList[i] as Pawn;
								if (pawn != null && pawn.GetPosture() == PawnPosture.Standing)
								{
                                    ZUtils.TryFixPawnError(pawn);
                                }
							}
						}
					}
                    Log.Error("Z-Levels caught an exception: " + __exception + ", trying to fix it");
                    return __exception;
				}
                return null;
            }
        }
    }
}

