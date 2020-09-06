using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private static Dictionary<Map, MapComponentZLevel> mapComponents = new Dictionary<Map, MapComponentZLevel>();
        public static MapComponentZLevel GetMapComponentZLevel(Map key)
        {
            if (mapComponents.TryGetValue(key, out MapComponentZLevel mapComponentZLevel) && mapComponentZLevel != null)
            {
                return mapComponentZLevel;
            }
            else
            {
                var comp = key.GetComponent<MapComponentZLevel>();
                mapComponents[key] = comp;
                return comp;
            }
        }
        public static void ResetZTracker()
		{
			zTracker = null;
		}

        public static IEnumerable<Map> GetAllMapsInClosestOrder(Thing thing, Map oldMap, IntVec3 oldPosition, bool skipOldMap = false, bool dontCheckForStairs = false)
        {
            bool cantGoDown = false;
            bool cantGoUP = false;

            foreach (var otherMap in ZTracker.GetAllMapsInClosestOrder(oldMap))
            {
                if (!dontCheckForStairs)
                {
                    var stairs = new List<Building_Stairs>();
                    if (ZTracker.GetZIndexFor(otherMap) > ZTracker.GetZIndexFor(oldMap) && !cantGoUP)
                    {
                        Map lowerMap = ZTracker.GetLowerLevel(otherMap.Tile, otherMap);
                        if (lowerMap != null)
                        {
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
                            stairs = ZTracker.stairsDown[upperMap];
                        }
                        else
                        {
                            ZLogger.Message("Upper map is null in " + ZTracker.GetMapInfo(otherMap));
                        }
                    }
                    if (stairs != null && stairs.Count > 0)
                    {
                        var selectedStairs = stairs.MinBy(x => IntVec3Utility.DistanceTo(thing.Position, x.Position));
                        var position = selectedStairs.Position;
                        TeleportThing(thing, otherMap, position);
                        if (!skipOldMap || skipOldMap && otherMap != oldMap)
                        {
                            yield return otherMap;
                        }
                    }
                    else if (otherMap == oldMap && !skipOldMap)
                    {
                        TeleportThing(thing, oldMap, oldPosition);
                        yield return otherMap;
                    }
                    else
                    {
                        if (ZTracker.GetZIndexFor(otherMap) > ZTracker.GetZIndexFor(oldMap))
                        {
                            ZLogger.Message(thing + " cant go up in " + ZTracker.GetMapInfo(otherMap));
                            cantGoUP = true;
                        }
                        else if (ZTracker.GetZIndexFor(otherMap) < ZTracker.GetZIndexFor(oldMap))
                        {
                            ZLogger.Message(thing + " cant go down in " + ZTracker.GetMapInfo(otherMap));
                            cantGoDown = true;
                        }
                    }
                }
                else
                {
                    TeleportThing(thing, otherMap, oldPosition);
                    yield return otherMap;
                }
            }
        }

        public static IEnumerable<Map> GetAllMapsInClosestOrderForTwoThings(Thing thing, Map oldMap, IntVec3 oldPosition, 
            Thing thing2, Map oldMap2, IntVec3 oldPosition2)
        {
            bool cantGoDown = false;
            bool cantGoUP = false;

            foreach (var otherMap in ZTracker.GetAllMapsInClosestOrder(oldMap))
            {
                var stairs = new List<Building_Stairs>();
                if (ZTracker.GetZIndexFor(otherMap) > ZTracker.GetZIndexFor(oldMap) && !cantGoUP)
                {
                    Map lowerMap = ZTracker.GetLowerLevel(otherMap.Tile, otherMap);
                    if (lowerMap != null)
                    {
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
                        stairs = ZTracker.stairsDown[upperMap];
                    }
                    else
                    {
                        ZLogger.Message("Upper map is null in " + ZTracker.GetMapInfo(otherMap));
                    }
                }
                if (stairs != null && stairs.Count > 0)
                {
                    var selectedStairs = stairs.MinBy(x => IntVec3Utility.DistanceTo(thing.Position, x.Position));
                    var position = selectedStairs.Position;
                    TeleportThing(thing, otherMap, position);
                    TeleportThing(thing2, otherMap, position);
                    yield return otherMap;
                }
                else if (otherMap == oldMap)
                {
                    TeleportThing(thing, oldMap, oldPosition);
                    TeleportThing(thing, oldMap2, oldPosition2);
                    yield return otherMap;
                }
                else
                {
                    if (ZTracker.GetZIndexFor(otherMap) > ZTracker.GetZIndexFor(oldMap))
                    {
                        ZLogger.Message(thing + " cant go up in " + ZTracker.GetMapInfo(otherMap));
                        cantGoUP = true;
                    }
                    else if (ZTracker.GetZIndexFor(otherMap) < ZTracker.GetZIndexFor(oldMap))
                    {
                        ZLogger.Message(thing + " cant go down in " + ZTracker.GetMapInfo(otherMap));
                        cantGoDown = true;
                    }
                }
            }
        }

        static int GetIndex(List<Map> list, Map value)
        {
            for (int index = 0; index < list.Count; index++)
            {
                if (list[index] == value)
                {
                    return index;
                }
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TeleportThing(Thing thing, Map map, IntVec3 position)
        {
            //    var mth = new StackTrace().GetFrame(1).GetMethod();
            //    var cls = mth.ReflectedType.Name;
            //    ZLogger.Message(cls + " - " + mth.Name + " - teleport " + thing + " from " + thing.Map + " to " + map + " from " + thing.Position + " to " + position, true);
            var value = (sbyte)Find.Maps.IndexOf(map);
            if (thing.mapIndexOrState != value)
            {
                thing.mapIndexOrState = value;
            }

            if (thing.positionInt != position)
            {
                thing.positionInt = position;
            }
        }

		private static ZLevelsManager zTracker;
	}
}

