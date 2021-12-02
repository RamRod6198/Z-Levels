using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Multiplayer.API;
using RimWorld;
using Verse;
using Verse.AI;

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
                ZLogger.Message("otherMap: " + otherMap);
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

        public static void TryFixPawnError(Pawn pawn)
        {
            if (pawn.rotationTracker == null)
            {
                ZLogger.Pause(pawn + " Pawn_RotationTracker IS NULL. FIXING");
                pawn.rotationTracker = new Pawn_RotationTracker(pawn);
            }
            if (pawn.pather == null)
            {
                ZLogger.Pause(pawn + " Pawn_PathFollower IS NULL. FIXING");
                pawn.pather = new Pawn_PathFollower(pawn);
            }
            if (pawn.thinker == null)
            {
                ZLogger.Pause(pawn + " Pawn_Thinker IS NULL. FIXING");
                pawn.thinker = new Pawn_Thinker(pawn);
            }
            if (pawn.jobs == null)
            {
                ZLogger.Pause(pawn + " Pawn_JobTracker IS NULL. FIXING");
                pawn.jobs = new Pawn_JobTracker(pawn);
            }
            if (pawn.stances == null)
            {
                ZLogger.Pause(pawn + " Pawn_StanceTracker IS NULL. FIXING");
                pawn.stances = new Pawn_StanceTracker(pawn);
            }
            if (pawn.natives == null)
            {
                ZLogger.Pause(pawn + " Pawn_NativeVerbs IS NULL. FIXING");
                pawn.natives = new Pawn_NativeVerbs(pawn);
            }
            if (pawn.filth == null)
            {
                ZLogger.Pause(pawn + " Pawn_FilthTracker IS NULL. FIXING");
                pawn.filth = new Pawn_FilthTracker(pawn);
            }
            if ((int)pawn.RaceProps.intelligence <= 1 && pawn.caller == null)
            {
                ZLogger.Pause(pawn + " Pawn_CallTracker IS NULL. FIXING");
                pawn.caller = new Pawn_CallTracker(pawn);
            }
            if (pawn.RaceProps.IsFlesh)
            {
                if (pawn.interactions == null)
                {
                    ZLogger.Pause(pawn + " Pawn_InteractionsTracker IS NULL. FIXING");
                    pawn.interactions = new Pawn_InteractionsTracker(pawn);
                }
                if (pawn.psychicEntropy == null)
                {
                    ZLogger.Pause(pawn + " Pawn_PsychicEntropyTracker IS NULL. FIXING");
                    pawn.psychicEntropy = new Pawn_PsychicEntropyTracker(pawn);
                }
                if (pawn.abilities == null)
                {
                    ZLogger.Pause(pawn + " Pawn_AbilityTracker IS NULL. FIXING");
                    pawn.abilities = new Pawn_AbilityTracker(pawn);
                }
            }

            bool flag = pawn.Faction != null && pawn.Faction.IsPlayer;
            bool flag2 = pawn.HostFaction != null && pawn.HostFaction.IsPlayer;
            if (pawn.RaceProps.Humanlike && !pawn.Dead)
            {
                if (pawn.mindState.wantsToTradeWithColony)
                {
                    if (pawn.trader == null)
                    {
                        ZLogger.Pause(pawn + " Pawn_TraderTracker IS NULL. FIXING");
                        pawn.trader = new Pawn_TraderTracker(pawn);
                    }
                }
            }
            if (pawn.RaceProps.Humanlike)
            {
                if ((flag || flag2) && pawn.foodRestriction == null)
                {
                    ZLogger.Pause(pawn + " Pawn_FoodRestrictionTracker IS NULL. FIXING");

                    pawn.foodRestriction = new Pawn_FoodRestrictionTracker(pawn);
                }
                if (flag)
                {
                    if (pawn.outfits == null)
                    {
                        ZLogger.Pause(pawn + " Pawn_OutfitTracker IS NULL. FIXING");

                        pawn.outfits = new Pawn_OutfitTracker(pawn);
                    }
                    if (pawn.drugs == null)
                    {
                        ZLogger.Pause(pawn + " Pawn_DrugPolicyTracker IS NULL. FIXING");

                        pawn.drugs = new Pawn_DrugPolicyTracker(pawn);
                    }
                    if (pawn.timetable == null)
                    {
                        ZLogger.Pause(pawn + " Pawn_TimetableTracker IS NULL. FIXING");

                        pawn.timetable = new Pawn_TimetableTracker(pawn);
                    }
                    if (pawn.drafter == null)
                    {
                        ZLogger.Pause(pawn + " Pawn_DraftController IS NULL. FIXING");

                        pawn.drafter = new Pawn_DraftController(pawn);
                    }
                }
            }
            if ((flag || flag2) && pawn.playerSettings == null)
            {
                ZLogger.Pause(pawn + " Pawn_PlayerSettings IS NULL. FIXING");
                pawn.playerSettings = new Pawn_PlayerSettings(pawn);
            }
            if ((int)pawn.RaceProps.intelligence <= 1 && pawn.Faction != null && !pawn.RaceProps.IsMechanoid && pawn.training == null)
            {
                ZLogger.Pause(pawn + " Pawn_TrainingTracker IS NULL. FIXING");
                pawn.training = new Pawn_TrainingTracker(pawn);
            }
        }

        private static ZLevelsManager zTracker;
	}
}

