using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace ZLevels.Properties
{
    /// <summary>
    /// So first we'll go check to see if a path exists from stairs a to b on all stairs within every map.
    /// Along the way, we'll mark all the no path pairs to come back to.
    /// Once we have all the valid connections, we'll use those to patch in valid paths.  Once we have this data structure,
    /// we'll know all the pathfinders needed to go from a to b.
    /// Second, going to go through the pathfinders in order and get the pawn paths for each.
    /// Finally, we'll return a list of pawn paths for the pawn to use, unless I can stitch them together somehow
    /// 
    /// </summary>
    public class ZPathfinder
    {
        private static Lazy<ZPathfinder> _instance = new Lazy<ZPathfinder>();
        public static ZPathfinder Instance => _instance.Value;
        public static readonly TraverseParms StairParms = TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false);
        private HashSet<PathFinder> _pathFinders;
        public static void ResetPathfinder()
        {

            _instance = new Lazy<ZPathfinder>();

        }

        private Dictionary<Tuple<Thing, Thing>, List<PawnPath>> StairPaths;

        public ZPathfinder()
        {
            _pathFinders = new HashSet<PathFinder>();
        }

        public bool AddMap(Map map)
        {
            bool ret = _pathFinders.Add(map.pathFinder);



            return ret;
        }

        public void CalculateStairPaths()
        {
            StairPaths = new Dictionary<Tuple<Thing, Thing>, List<PawnPath>>();

            foreach (Map map in ZUtils.ZTracker.mapIndex.Keys)
            {
                ZLogger.Message($"Stair paths at start of map index {map.Index}: {StairPaths.Count}");

                foreach (Thing stairsDown in ZUtils.ZTracker.stairsDown[map])
                {
                    foreach (Thing stairsUp in ZUtils.ZTracker.stairsUp[map])
                    {
                        JoinStairs(stairsDown, stairsUp, map);
                    }

                    foreach (Thing stairsDown2 in ZUtils.ZTracker.stairsDown[map])
                    {
                        if (stairsDown == stairsDown2)
                        {
                            continue;
                        }
                        JoinStairs(stairsDown, stairsDown2, map);
                    }
                }
                foreach (Thing stairsUp in ZUtils.ZTracker.stairsDown[map])
                {
                    foreach (Thing stairsUp2 in ZUtils.ZTracker.stairsUp[map])
                    {
                        JoinStairs(stairsUp, stairsUp2, map);
                    }
                }
                ZLogger.Message($"Stair paths at end of map index {map.Index}: {StairPaths.Count}");

            }

            var nonPaths = new List<Tuple<Thing, Thing>>();

            foreach (var key in StairPaths.Keys)
            {
                if (StairPaths[key][0] == PawnPath.NotFound)
                {
                    ZLogger.Message($"Path not found between {key.Item1} & {key.Item2}");
                    nonPaths.Add(key);
                }
            }

            foreach (var pair in nonPaths)
            {
                StairPaths[pair] = FindPath(pair.Item1, pair.Item2);
            }



        }

        private List<PawnPath> FindPath(Thing source, Thing sink)
        {
            //Either stairsUp### or stairsDown### 


            // Recursion: get a list of directions back and incorporate them in the list
            // So if we go down a level, we can find a new source and sink- if both are going the same direction,
            // it'll be the stairs directly below/above each.  If they find, we're done.  If they don't, we will have to backtrack
            // Backtracking recursion... I haven't done that in a while.  We can use the lists themselves to backtrack.
            // Essentially, we'll check random stairs until we find a viable path.  We don't have
            // to actually do any traversals- we can just use Stair
            List<PawnPath> ret = RecursiveFindPath(source, sink, new List<PawnPath>());
            return ret;
        }

        private List<PawnPath> RecursiveFindPath(Thing source, Thing sink, List<PawnPath> paths)
        {
            //Simplest case, case 1: we find a path directly- but this won't be the first level if this function is getting called.
            if (CheckStairPathsForKeyPair(sink, source, out List<PawnPath> newPath) || CheckStairPathsForKeyPair(source, sink, out newPath))
            {
                paths.AddRange(newPath);
                return paths;
            }

            bool sinkDown = sink is Building_StairsDown, sourceDown = source is Building_StairsDown;
            var dict = sinkDown ? ZUtils.ZTracker.stairsDown : ZUtils.ZTracker.stairsUp;
            var connectingMap = ZUtils.ZTracker.GetMapByIndex(sink.Map.Tile,
                ZUtils.ZTracker.mapIndex[sink.Map] + (sinkDown ? -1 : 1));

            //Next we'll search for alternate paths on the same floor
            //First hold source constant

            //case 2: No direct connection, but source and sink go different directions
            if (sourceDown != sinkDown)
            {
                List<Thing> stairs = sinkDown ? ZUtils.ZTracker.stairsDown[sink.Map] : ZUtils.ZTracker.stairsUp[sink.Map];
                foreach (Thing stair in stairs)
                {
                    if (stair == source || stair == sink)
                    {
                        continue;
                    }

                    Thing nextSink, nextSource;
                    var map = sinkDown
                        ? ZUtils.ZTracker.GetLowerLevel(source.Tile, source.Map)
                        : ZUtils.ZTracker.GetUpperLevel(source.Tile, source.Map);
                    if (sinkDown)
                    {
                        nextSource = stair.Position.GetThingList(map).FirstOrDefault(x => x is Building_StairsUp);
                        nextSink = sink.Position.GetThingList(map).FirstOrDefault(x => x is Building_StairsUp);
                    }
                    else
                    {
                        nextSource = stair.Position.GetThingList(map).FirstOrDefault(x => x is Building_StairsDown);
                        nextSink = sink.Position.GetThingList(map).FirstOrDefault(x => x is Building_StairsDown);
                    }

                    RecursiveFindPath(nextSource, nextSink, newPath);
                    
                    if (CheckStairPathsForKeyPair(sink, stair, out newPath))
                    {
                        //found a path between those two
                        break;
                    }
                }
            }

            //case 3: No direct connection, but source and sink go the same direction.  
            if (sourceDown == sinkDown)
            {
                Thing nextSource, nextSink;
                //nextSource = source.GetMatchingStair    
            }




            //case 4: Deadend and backtrack
            return null;

        }

        private bool CheckStairPathsForKeyPair(Thing sink, Thing source, out List<PawnPath> newPath)
        {
            var spoot = new Tuple<Thing, Thing>(source, sink);
            if (StairPaths.ContainsKey(spoot))
            {
                newPath = StairPaths[spoot];
                return true;
            }

            newPath = null;


            return false;

        }



        private void JoinStairs(Thing startStairs, Thing endStairs, Map map)
        {
            LocalTargetInfo ti = new LocalTargetInfo(endStairs);
            //We'll add the path even if one isn't found because it will be the NotFound pawn path.
            //Also, it's a list because on not-found paths we'll need a list to get to the proper stairs
            //Most will be lists of one.  The idea will be if a path to an up stairway cannot be found,
            //then we'll need to find a path to that stairway somewhere else (later).  So add all the paths,  
            //Then come back and look at the no-paths and figure all those out.
            PawnPath path = map.pathFinder.FindPath(startStairs.Position, ti, StairParms);

            ZLogger.Message(
                $"Joining stairs- number of paths in stairPaths = {StairPaths.Count}, " +
                $"startStairs = {startStairs}, endStairs = {endStairs}");

            var tuple = new Tuple<Thing, Thing>(startStairs, endStairs);
            if (!StairPaths.ContainsKey(tuple))
            {
                StairPaths.Add(tuple, new List<PawnPath> { path });
            }





        }
    }



}
