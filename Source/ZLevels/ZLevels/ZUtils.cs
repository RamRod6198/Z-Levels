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

        public static Map currentLookedIntoMap;
        public static IEnumerable<Map> GetAllMapsInClosestOrder(Thing thing, Map oldMap, IntVec3 oldPosition, bool skipOldMap = false, bool dontCheckForStairs = false)
        {
            bool cantGoDown = false;
            bool cantGoUP = false;
            var zTracker = ZTracker;
            var jobTracker = thing is Pawn p ? zTracker.jobTracker.TryGetValue(p, out var value) ? value : null : null;
            foreach (var otherMap in zTracker.GetAllMapsInClosestOrder(oldMap))
            {
                if (!dontCheckForStairs)
                {
                    var stairs = new List<Building_Stairs>();
                    var otherMapZIndex = zTracker.GetZIndexFor(otherMap);
                    var oldMapZIndex = zTracker.GetZIndexFor(oldMap);

                    if (otherMapZIndex > oldMapZIndex && !cantGoUP) // old map is lower than other map
                    {
                        Map lowerMap = zTracker.GetLowerLevel(otherMap.Tile, otherMap);
                        if (lowerMap != null)
                        {
                            stairs = zTracker.stairsUp[lowerMap]; // fetching stairs from lower map, in this case from map below than other map
                        }
                        else
                        {
                            ZLogger.Error("Lower map is null in " + zTracker.GetMapInfo(otherMap));
                        }
                    }
                    else if (otherMapZIndex < oldMapZIndex && !cantGoDown) // other map is lower than old map
                    {
                        Map upperMap = zTracker.GetUpperLevel(otherMap.Tile, otherMap);
                        if (upperMap != null)
                        {
                            stairs = zTracker.stairsDown[upperMap]; // fetching stairs from upper map, in this case from map upper than other map
                        }
                        else
                        {
                            ZLogger.Error("Upper map is null in " + zTracker.GetMapInfo(otherMap));
                        }
                    }

                    if (stairs != null && stairs.Count > 0)
                    {
                        //foreach (var stair in stairs)
                        //{
                        //    ZLogger.Message($"CHECKING STAIR: {stair}, stair.Spawned: {stair.Spawned}, stair.Destroyed: {stair.Destroyed}, stair.Position: {stair.Position}, stair.Map: {stair.Map}, otherMap: {otherMap}");
                        //}

                        if (!skipOldMap || skipOldMap && otherMap != oldMap)
                        {
                            IntVec3 newPosition = IntVec3.Invalid;// stairs.MinBy(x => IntVec3Utility.DistanceTo(thing.Position, x.Position)).Position;
                            if (thing is Pawn pawn)
                            {
                                foreach (var stair in stairs.OrderBy(x => IntVec3Utility.DistanceTo(thing.Position, x.Position)))
                                {
                                    //ZLogger.Message($"CHECK: {pawn} is from {zTracker.GetMapInfo(pawn.Map)}, can reach {stair} - {pawn.CanReach(stair, Verse.AI.PathEndMode.OnCell, Danger.Deadly)}," +
                                    //    $" stair is from {zTracker.GetMapInfo(stair.Map)}");
                                    if (pawn.CanReach(stair, Verse.AI.PathEndMode.OnCell, Danger.Deadly))
                                    {
                                        newPosition = stair.Position;
                                        break;
                                    }
                                }
                            }

                            if (newPosition.IsValid)
                            {
                                TeleportThing(thing, otherMap, newPosition);
                                //ZLogger.Message($"1 CHECK: {thing} is going to {zTracker.GetMapInfo(otherMap)}");
                                currentLookedIntoMap = otherMap;
                                yield return otherMap;
                            }
                            else
                            {
                                var firstStairs = stairs.FirstOrDefault();
                                var map = firstStairs?.Map;
                                if (map != null)
                                {
                                    //ZLogger.Message($"3 CHECK: stairs map: {zTracker.GetMapInfo(map)} - other map: {zTracker.GetMapInfo(otherMap)} - pawn map: {zTracker.GetMapInfo(thing.Map)}");
                                    if (map != oldMap)
                                    {
                                        var stairsMapIndex = zTracker.GetZIndexFor(map);
                                        if (stairsMapIndex > oldMapZIndex)
                                        {
                                            cantGoUP = true;
                                        }
                                        else if (oldMapZIndex > stairsMapIndex)
                                        {
                                            cantGoDown = true;
                                        }
                                    }
                                    else if (firstStairs is Building_StairsUp)
                                    {
                                        cantGoUP = true;
                                    }
                                    else if (firstStairs is Building_StairsDown)
                                    {
                                        cantGoDown = true;
                                    }
                                }
                            }
                        }
                    }
                    else if (otherMap == oldMap && !skipOldMap)
                    {
                        TeleportThing(thing, oldMap, oldPosition);
                        //ZLogger.Message($"2 CHECK: {thing} is going to {zTracker.GetMapInfo(otherMap)}");
                        currentLookedIntoMap = otherMap;
                        yield return otherMap;
                    }
                    else
                    {
                        if (otherMapZIndex > oldMapZIndex)
                        {
                            //ZLogger.Message(thing + " cant go up in " + zTracker.GetMapInfo(otherMap));
                            cantGoUP = true;
                        }
                        else if (otherMapZIndex < oldMapZIndex)
                        {
                            //ZLogger.Message(thing + " cant go down in " + zTracker.GetMapInfo(otherMap));
                            cantGoDown = true;
                        }
                    }
                }
                else
                {
                    TeleportThing(thing, otherMap, oldPosition);
                    currentLookedIntoMap = otherMap;
                    yield return otherMap;
                }
            }
        }

        public static IEnumerable<Map> GetAllMapsInClosestOrderForTwoThings(Thing thing, Map oldMap, IntVec3 oldPosition, Thing thing2, Map oldMap2, IntVec3 oldPosition2)
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
                    currentLookedIntoMap = otherMap;
                    yield return otherMap;
                }
                else if (otherMap == oldMap)
                {
                    TeleportThing(thing, oldMap, oldPosition);
                    TeleportThing(thing2, oldMap2, oldPosition2);
                    currentLookedIntoMap = otherMap;
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

        public static IntVec3 GetCellToTeleportFrom(Map oldMap, IntVec3 originPosition, Map newMap)
        {
            IntVec3 position = originPosition;
            var oldMapZIndex = ZTracker.GetZIndexFor(oldMap);
            var newMapZIndex = ZTracker.GetZIndexFor(newMap);
            var maps = oldMapZIndex > newMapZIndex ? ZTracker.GetAllMapsFromToBelow(oldMap, newMap) : ZTracker.GetAllMapsFromToUpper(oldMap, newMap);
            foreach (var otherMap in maps)
            {
                var stairs = new List<Building_Stairs>();
                var otherMapZIndex = ZTracker.GetZIndexFor(otherMap);

                if (otherMap == oldMap)
                {
                    if (oldMapZIndex > newMapZIndex)
                    {
                        Map lowerMap = ZTracker.GetLowerLevel(otherMap.Tile, otherMap);
                        if (lowerMap != null)
                        {
                            stairs = ZTracker.stairsUp[lowerMap];
                        }
                    }
                    else if (oldMapZIndex < newMapZIndex)
                    {
                        Map upperMap = ZTracker.GetUpperLevel(otherMap.Tile, otherMap);
                        if (upperMap != null)
                        {
                            stairs = ZTracker.stairsDown[upperMap];
                        }
                    }
                }
                else if (otherMapZIndex > oldMapZIndex)
                {
                    Map lowerMap = ZTracker.GetLowerLevel(otherMap.Tile, otherMap);
                    if (lowerMap != null)
                    {
                        stairs = ZTracker.stairsUp[lowerMap];
                    }
                }
                else if (otherMapZIndex < oldMapZIndex)
                {
                    Map upperMap = ZTracker.GetUpperLevel(otherMap.Tile, otherMap);
                    if (upperMap != null)
                    {
                        stairs = ZTracker.stairsDown[upperMap];
                    }
                }

                if (stairs != null && stairs.Any())
                {
                    var selectedStairs = stairs.MinBy(x => IntVec3Utility.DistanceTo(position, x.Position));
                    position = selectedStairs.Position;
                }
                else
                {
                    return IntVec3.Invalid;
                }
            }
            return position;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TeleportThing(Thing thing, Map map, IntVec3 position)
        {
            //ZLogger.Message($"Instant Teleporting {thing} to {map} - {position}");
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

