using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Multiplayer.API;
using Verse;

namespace ZLevels
{
	[StaticConstructorOnStartup]
	internal static class ZUtils
	{
		public static ZLevelsManager ZTracker
		{
			get
			{
				if (zTracker == null)
				{
					zTracker = Current.Game.GetComponent<ZLevelsManager>();
					return zTracker;
				}
				return zTracker;
			}
		}

		public static void ResetZTracker()
		{
			zTracker = null;
		}

        public static IEnumerable<Map> GetAllMapsInClosestOrder(Pawn pawn, Map oldMap, IntVec3 oldPosition)
        {
            bool cantGoDown = false;
            bool cantGoUP = false;

            foreach (var otherMap in ZTracker.GetAllMapsInClosestOrder(oldMap))
            {
                var stairs = new List<Thing>();
                ZLogger.Message("Trying to yield " + otherMap);
                if (ZTracker.GetZIndexFor(otherMap) > ZTracker.GetZIndexFor(oldMap) && !cantGoUP)
                {
                    Map lowerMap = ZTracker.GetLowerLevel(otherMap.Tile, otherMap);
                    if (lowerMap != null)
                    {
                        ZLogger.Message("Searching stairs up in " + ZTracker.GetMapInfo(otherMap));
                        stairs = ZTracker.stairsUp[lowerMap];
                    }
                    else
                    {
                        ZLogger.Message("Lower map is null in " + ZTracker.GetMapInfo(otherMap));
                    }
                }
                else if (ZTracker.GetZIndexFor(otherMap) < ZTracker.GetZIndexFor(oldMap) && !cantGoDown)
                {
                    Map upperMap = ZTracker.GetUpperLevel(otherMap.Tile, otherMap);
                    if (upperMap != null)
                    {
                        ZLogger.Message("Searching stairs down in " + ZTracker.GetMapInfo(otherMap));
                        stairs = ZTracker.stairsDown[upperMap];
                    }
                    else
                    {
                        ZLogger.Message("Upper map is null in " + ZTracker.GetMapInfo(otherMap));
                    }
                }
                if (stairs != null && stairs.Count > 0)
                {
                    var selectedStairs = stairs.MinBy(x => IntVec3Utility.DistanceTo(pawn.Position, x.Position));
                    var position = selectedStairs.Position;

                    Traverse.Create(pawn).Field("mapIndexOrState")
                        .SetValue((sbyte)Find.Maps.IndexOf(otherMap));
                    Traverse.Create(pawn).Field("positionInt")
                        .SetValue(position);
                    ZLogger.Message("19 SetPosition for " + pawn + " to " + position);
                    ZLogger.Message("return " + otherMap);
                    yield return otherMap;
                }
                else if (otherMap == oldMap)
                {
                    Traverse.Create(pawn).Field("mapIndexOrState")
                        .SetValue((sbyte)Find.Maps.IndexOf(oldMap));
                    Traverse.Create(pawn).Field("positionInt")
                        .SetValue(oldPosition);
                    ZLogger.Message("4 SetPosition for " + pawn + " to " + oldPosition);
                    ZLogger.Message("return " + otherMap);
                    yield return otherMap;
                }
                else
                {
                    if (ZTracker.GetZIndexFor(otherMap) > ZTracker.GetZIndexFor(oldMap))
                    {
                        ZLogger.Message(pawn + " cant go up in " + ZTracker.GetMapInfo(otherMap));
                        cantGoUP = true;
                    }
                    else if (ZTracker.GetZIndexFor(otherMap) < ZTracker.GetZIndexFor(oldMap))
                    {
                        ZLogger.Message(pawn + " cant go down in " + ZTracker.GetMapInfo(otherMap));
                        cantGoDown = true;
                    }
                    else
                    {
                        ZLogger.Message("My bad...");
                    }
                }
            }
        }

		private static ZLevelsManager zTracker;
	}
}

