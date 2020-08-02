using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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
        public static void ResetPathfinder()
        {

            _instance = new Lazy<ZPathfinder>();

        }

        private Dictionary<Tuple<Building_Stairs, Building_Stairs>, List<PawnPath>> StairPaths;

        public ZPathfinder()
        {
        }

        public void CalculateStairPaths()
        {
            StairPaths = new Dictionary<Tuple<Building_Stairs, Building_Stairs>, List<PawnPath>>();

            foreach (Map map in ZUtils.ZTracker.mapIndex.Keys)
            {
                ZLogger.Message($"Stair paths at start of map index {map.Index}: {StairPaths.Count}");

                foreach (Building_Stairs stairsDown in ZUtils.ZTracker.stairsDown[map])
                {
                    foreach (Building_Stairs stairsUp in ZUtils.ZTracker.stairsUp[map])
                    {
                        JoinStairs(stairsDown, stairsUp, map);
                    }

                    foreach (Building_Stairs stairsDown2 in
                        ZUtils.ZTracker.stairsDown[map].Where(stairsDown2 => stairsDown != stairsDown2))
                    {
                        JoinStairs(stairsDown, stairsDown2, map);
                    }
                }
                foreach (Building_Stairs stairsUp in ZUtils.ZTracker.stairsDown[map])
                {
                    foreach (Building_Stairs stairsUp2 in ZUtils.ZTracker.stairsUp[map])
                    {
                        JoinStairs(stairsUp, stairsUp2, map);
                    }
                }
                ZLogger.Message($"Stair paths at end of map index {map.Index}: {StairPaths.Count}");

            }

            var nonPaths = new List<Tuple<Building_Stairs, Building_Stairs>>();

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


            // Recursion: get a list of directions back and incorporate them in the list
            // So if we go down a level, we can find a new source and sink- if both are going the same direction,
            // it'll be the stairs directly below/above each.  If they find, we're done.  If they don't, we will have to backtrack
            // Backtracking recursion... I haven't done that in a while.  We can use the lists themselves to backtrack.
            // Essentially, we'll check random stairs until we find a viable path.  We don't have
            // to actually do any traversals- we can just use Stair
            List<PawnPath> ret = IterativeFindPath((Building_Stairs)source,
                (Building_Stairs)sink);
            return ret;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetCurrentMethod()
        {
            var st = new StackTrace();
            var sf = st.GetFrame(1);

            return sf.GetMethod().Name;
        }

        private static int itMax = 10;
        enum RecursionCases
        {
            PathKnown,
            SameFloorSameDirections,
            SameFloorDifferentDirections,
            DeadEnd
        };
        private List<PawnPath> IterativeFindPath(Building_Stairs source, Building_Stairs sink)
        {
            HashSet<Building_Stairs> stairsChecked = null;
            List<PawnPath> ret = new List<PawnPath>();

            Tuple<Building_Stairs, Building_Stairs, object>
                nextSet = new Tuple<Building_Stairs, Building_Stairs, object>(source, sink, null);
            for (int i = 0; i < itMax; ++i)
            {
                ZLogger.Message($"Iteration {i} of {itMax}, source->sink = {source}->{sink}");
                nextSet = IterativeFindPath(source, sink, ret, stairsChecked);
                source = nextSet.Item1;
                sink = nextSet.Item2;
                stairsChecked = (HashSet<Building_Stairs>)nextSet.Item3;
                //If item1 and 2 are equal, it means we've found the path and can break out
                if (nextSet.Item1 != nextSet.Item2) continue; 
                ret.Reverse();
                ZLogger.Message("Path found. Path follows:");
                foreach (PawnPath pawnPath in ret)
                {
                    ZLogger.Message($"{pawnPath}");
                }
                break;
            }

            return ret;
        }

        private Tuple<Building_Stairs, Building_Stairs, object> IterativeFindPath(Building_Stairs source,
            Building_Stairs sink, List<PawnPath> paths, HashSet<Building_Stairs> stairsChecked)
        {
            List<PawnPath> PreexistingPath = new List<PawnPath>();
            Tuple<Building_Stairs, Building_Stairs, object> ret = null;
            switch (GetCaseForPair(source, sink, PreexistingPath))
            {
                case RecursionCases.PathKnown:
                    paths.AddRange(PreexistingPath);
                    ret = new Tuple<Building_Stairs, Building_Stairs, object>(source, source, null);
                    break;
                case RecursionCases.SameFloorSameDirections:
                    ret = new Tuple<Building_Stairs, Building_Stairs, object>(source.GetMatchingStair(),
                        sink.GetMatchingStair(), null);
                    break;
                case RecursionCases.SameFloorDifferentDirections:
                    //In simpler case, we just follow the 1 chain, in this case the source:
                    HashSet<Building_Stairs> stairsTaken = new HashSet<Building_Stairs>();
                    ret = new Tuple<Building_Stairs, Building_Stairs, object>(sink.GetMatchingStair(),
                        PickNextSource(sink.GetMatchingStair(), ref stairsTaken), stairsTaken);

                    //HashSet<Building_Stairs> sourceStairs= new HashSet<Building_Stairs>(), sinkStairs= new HashSet<Building_Stairs>();
                    //Building_Stairs nextSource = PickNextSource(source.GetMatchingStair(), ref sourceStairs);
                    //Building_Stairs nextSink = PickNextSource(sink.GetMatchingStair(), ref sinkStairs);
                    //var tups = new Tuple<HashSet<Building_Stairs>, Building_Stairs,
                    //    Building_Stairs, HashSet<Building_Stairs>>
                    //    (sourceStairs, sink.GetMatchingStair(), nextSink, sinkStairs);

                    //ret = new Tuple<Building_Stairs, Building_Stairs, object>(source.GetMatchingStair(), nextSource, tups);
                    break;
                case RecursionCases.DeadEnd:
                    ret = null;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return ret;
        }

        private List<PawnPath> RecursiveFindPath(Building_Stairs source, Building_Stairs sink, List<PawnPath> paths)
        {

            ZLogger.Message($"Recursing for stairs {source} to {sink}");
            //Case -1: handed null means one tail of a recursion has hit a dead end
            if (source == null || sink == null)
            {
                return null;
            }

            List<PawnPath> sourceSinkPaths = null;
            //Simplest case, case 0: we find a path directly- but this won't be the first level if this function is getting called.
            if (CheckStairPathsForKeyPair(sink, source, ref  sourceSinkPaths))
            {
                ZLogger.Message($"case 0 for stairs {source} to {sink}");
                paths.AddRange(sourceSinkPaths);
                return paths;
            }
            ZLogger.Message($"cases 1, 2, or 3 for stairs {source} to {sink}");

            bool sinkDown = sink is Building_StairsDown, sourceDown = source is Building_StairsDown;
            var dict = sinkDown ? ZUtils.ZTracker.stairsDown : ZUtils.ZTracker.stairsUp;
            var connectingMap = ZUtils.ZTracker.GetMapByIndex(sink.Map.Tile,
                ZUtils.ZTracker.mapIndex[sink.Map] + (sinkDown ? -1 : 1));

            //Next we'll search for alternate paths on the same floor
            //First hold source constant
            //case 1: No direct connection, but source and sink go the same direction.  
            if (sourceDown == sinkDown)
            {
                Building_Stairs nextSource, nextSink;
                nextSource = source.GetMatchingStair();
                nextSink = sink.GetMatchingStair();
                ZLogger.Message($"Case 1 for stairs {source} to {sink}");
                ZLogger.Message($"next stairs are  {nextSource} to {nextSink}");

                var temp = IterativeFindPath(nextSource, nextSink);
                if (temp == null)
                {
                    return paths;
                }

                paths.AddRange(temp);
                return paths;
            }

            //case 2: No direct connection, and source and sink go different directions
            List<Building_Stairs> stairs = sinkDown ? ZUtils.ZTracker.stairsDown[sink.Map] : ZUtils.ZTracker.stairsUp[sink.Map];
            foreach (Building_Stairs stair in stairs)
            {
                ZLogger.Message($"case 2 for stairs {source.GetType()} {source} to {source.GetType()} {sink} ");

                var stairSet = new HashSet<Building_Stairs>(new[] { source, sink });
                if (stair == source || stair == sink)
                {
                    continue;
                }

                Building_Stairs matchingSink = sink.GetMatchingStair(),
                                matchingSource = source.GetMatchingStair();
                Building_Stairs nextSink = PickNextSource(matchingSource, ref stairSet),
                                nextSource = PickNextSource(matchingSink, ref stairSet);
                //If PickNextSource is exhausted, then we've effectively hit a dead en-
                //we'll return null, but the next call of recurse will handle tying off that tail

                List<PawnPath> sourcePath = RecursiveFindPath(matchingSource, nextSource, new List<PawnPath>()),
                               sinkPath = RecursiveFindPath(matchingSink, nextSink, new List<PawnPath>());
                if (sourcePath == null || sinkPath == null)
                {
                    if (sourcePath == null && sinkPath == null)
                    {
                        return null;
                    }
                    return sourcePath ?? sinkPath;
                }
                else
                {
                    float sinkCost = 0, sourceCost = 0;
                    sinkCost += sinkPath.Sum(path => path.TotalCost);
                    sourceCost += sourcePath.Sum(path => path.TotalCost);
                    return sinkCost < sourceCost ? sinkPath : sourcePath;
                }
            }
            //case 3: Dead end and backtrack
            return null;
        }

        private RecursionCases GetCaseForPair(Building_Stairs source, Building_Stairs sink, List<PawnPath> newPath)
        {
            RecursionCases cases;
            if (source == null || sink == null)
            {
                cases = RecursionCases.DeadEnd;
            }
            else
            {
                if (CheckStairPathsForKeyPair(sink, source,ref  newPath))
                {
                    cases = RecursionCases.PathKnown;
                }
                else
                {
                    bool sourceDown = source is Building_StairsDown, sinkDown = sink is Building_StairsDown;
                    cases = sinkDown == sourceDown ? RecursionCases.SameFloorSameDirections : RecursionCases.SameFloorDifferentDirections;
                }
            }
            ZLogger.Message($"The case for {sink} and {source} is {cases}");

            return cases;
        }

        private static Building_Stairs PickNextSource(Building_Stairs sink, ref HashSet<Building_Stairs> previouslySelectedStairs)
        {
            //ZLogger.Message($"entering {GetCurrentMethod()}");

            if (previouslySelectedStairs == null)
            {
                previouslySelectedStairs = new HashSet<Building_Stairs>();
            }

            List<Building_Stairs> targetStairses = new List<Building_Stairs>(sink is Building_StairsUp
                ? ZUtils.ZTracker.stairsUp[sink.Map]
                : ZUtils.ZTracker.stairsDown[sink.Map]);

            foreach (Building_Stairs next in targetStairses)
            {
                if (previouslySelectedStairs.Contains(next))
                {
                    continue;
                }

                previouslySelectedStairs.Add(next);
                return next;
            }

            return null;
        }

        private bool CheckStairPathsForKeyPair(Building_Stairs sink, Building_Stairs source, ref List<PawnPath> newPath)
        {
            ZLogger.Message($"Checking for path between {source} and {sink}");
            //Tuple<Building_Stairs, Building_Stairs> spoot = new Tuple<Building_Stairs, Building_Stairs>(source, sink),
            //   toops = new Tuple<Building_Stairs, Building_Stairs>(sink, source);
           if(newPath == null) newPath = new List<PawnPath>();
            newPath.Add(JoinStairs(sink, source, sink.Map));
            //newPath = StairPaths.ContainsKey(spoot) ? StairPaths[spoot] : StairPaths[toops];
            bool found = newPath[0] != PawnPath.NotFound;
            ZLogger.Message($"we {(found ? "found" : "didn't find")} a path)");
            return found;
        }



        private PawnPath JoinStairs(Building_Stairs startStairs, Building_Stairs endStairs, Map map)
        {


            LocalTargetInfo ti = new LocalTargetInfo(endStairs);
            //We'll add the path even if one isn't found because it will be the NotFound pawn path.
            //Also, it's a list because on not-found paths we'll need a list to get to the proper stairs
            //Most will be lists of one.  The idea will be if a path to an up stairway cannot be found,
            //then we'll need to find a path to that stairway somewhere else (later).  So add all the paths,  
            //Then come back and look at the no-paths and figure all those out.
            PawnPath path = map.pathFinder.FindPath(startStairs.Position, ti, StairParms);

            //ZLogger.Message(
            //    $"Joining stairs- number of paths in stairPaths = {StairPaths.Count}, " +
            //    $"startStairs = {startStairs}, endStairs = {endStairs}");

            var tuple = new Tuple<Building_Stairs, Building_Stairs>(startStairs, endStairs);
            if (!StairPaths.ContainsKey(tuple))
            {
                if (path == PawnPath.NotFound)
                {
                    ZLogger.Message($"No path found for {startStairs} to {endStairs}");
                }
                else
                {
                    ZLogger.Message($"Path found for {startStairs} to {endStairs}: {path}");
                }

                StairPaths.Add(tuple, new List<PawnPath> { path });
            }
            else
            {
                if (path == PawnPath.NotFound)
                {
                    ZLogger.Message($"No path found for {startStairs} to {endStairs}");
                }
                else
                {
                    ZLogger.Message($"Path found for {startStairs} to {endStairs}: {path}");
                }

                StairPaths[tuple] = new List<PawnPath> { path };

            }

            return path;



        }
    }



}
