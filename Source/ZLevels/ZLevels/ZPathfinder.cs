using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Multiplayer.API;
using RimWorld;
using RimWorld.Planet;
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


        public class DijkstraGraph
        {
            HashSet<Node> nodes = new HashSet<Node>();

            public DijkstraGraph(int tile)
            {
                StringBuilder sb = new StringBuilder();
                List<Map> maps = ZUtils.ZTracker.GetAllMaps(tile);

                foreach (Map map in maps)
                {

                    foreach (Building_Stairs stairs in ZUtils.ZTracker.stairsUp[map])
                    {
                        nodes.Add(new Node(stairs));
                    }

                    //foreach (Map map2 in maps)
                    //{
                    //    foreach (Building_Stairs stairs2 in ZUtils.ZTracker.stairsUp[map2])
                    //    {
                    //        AddAndCalculateDistance(stairs, stairs2);
                    //    }
                    //    foreach (Building_Stairs stairs2 in ZUtils.ZTracker.stairsDown[map2])
                    //    {
                    //        AddAndCalculateDistance(stairs, stairs2);
                    //    }
                    //}

                    foreach (Building_Stairs stairs in ZUtils.ZTracker.stairsDown[map])
                    {
                        nodes.Add(new Node(stairs));
                    }

                    //foreach (Map map2 in maps)
                    //{
                    //    foreach (Building_Stairs stairs2 in ZUtils.ZTracker.stairsUp[map2])
                    //    {
                    //        AddAndCalculateDistance(stairs, stairs2);
                    //    }
                    //    foreach (Building_Stairs stairs2 in ZUtils.ZTracker.stairsDown[map2])
                    //    {
                    //        AddAndCalculateDistance(stairs, stairs2);
                    //    }
                    //}


                }

                foreach (var node in nodes)
                {
                    foreach (var neighborNode in nodes)
                    {
                        if (node == neighborNode) continue;
                        float cost = float.PositiveInfinity;
                        bool neighbors = false;
                        if (node.Map == neighborNode.Map)
                        {
                            PawnPath p = node.Map.pathFinder.FindPath(node.Location, neighborNode.Location, StairParms);
                            cost = p.TotalCost;
                            p.ReleaseToPool();
                            p = null;

                        }
                        else if (IsMatchedStairSet(node, neighborNode))
                        {
                            cost = 3;
                            neighbors = true;
                        }
                        if (cost < 0) cost = float.PositiveInfinity;
                        neighbors = !float.IsPositiveInfinity(cost);

                        if (neighbors && node.AddNeighbor(neighborNode, cost))
                        {
                            ZLogger.Message($"Made {node} and {neighborNode} neighbors.  Distance = {cost}");
                        }

                    }
                }



                //foreach (Tuple<Node, Node> key in graphDictionary.Keys)
                //{
                //    //Some of these will fail and that's good- we only need 1 of each node.
                //    nodes.Add(key.Item1);
                //    nodes.Add(key.Item2);

                //    float cost = graphDictionary[key].cost;
                //    if (!float.IsPositiveInfinity(graphDictionary[key].cost) && 
                //        (Math.Abs(cost - PathSummary.MatchedStairCost) < .001 || (key.Item1.key.Map == key.Item2.key.Map)))
                //    {
                //        //If they are matched stairs or if they are accessible to each other on the same level, they are neighbors
                //        key.Item1.AddNeighbor(key.Item2);
                //        key.Item2.AddNeighbor(key.Item1);
                //    }
                //}
            }

            private bool IsMatchedStairSet(Node node, Node node2)
            {
                if (node.Map == node2.Map) return false;
                int nodeIndex = ZUtils.ZTracker.mapIndex[node.Map], node2Index = ZUtils.ZTracker.mapIndex[node2.Map];
                if (nodeIndex + 1 == node2Index && node.key is Building_StairsUp ||
                    nodeIndex - 1 == node2Index && node.key is Building_StairsDown)
                {
                    return node.key.Position == node2.key.Position;
                }

                return false;
            }

            public List<Node> FindRoute(IntVec3 from, IntVec3 to, Map mapFrom, Map mapTo)
            {
                Building_Stairs sourceStairs = (Building_Stairs)GenClosest.ClosestThing_Global_Reachable(from
                    , mapFrom, mapFrom.listerThings.AllThings
                        .Where(x => x is Building_Stairs), PathEndMode.OnCell, StairParms);


                Building_Stairs sinkStairs = (Building_Stairs)GenClosest.ClosestThing_Global_Reachable(to
                    , mapFrom, mapTo.listerThings.AllThings
                        .Where(x => x is Building_Stairs), PathEndMode.OnCell, StairParms);

                Node source = nodes.First(x => x.key == sourceStairs), sink = nodes.First(x => x.key == sinkStairs);

                FindNeighbors(source);
           
                FindNeighbors(sink);
                StringBuilder sb = new StringBuilder("Source neighbors: \n");
                foreach (var neighbor in source.neighbors)
                {
                    sb.AppendLine($"Source({source}) neighbor: {neighbor} distance: {source.neighborDistances[neighbor]}");
                }

                sb.AppendLine("Sink neighbors");
                foreach (var neighbor in sink.neighbors)
                {
                    sb.AppendLine($"sink({sink}) neighbor: {neighbor} distance: {sink.neighborDistances[neighbor]}");
                }
                ZLogger.Message(sb.ToString());
                return DijkstraConnect(source, sink);
                //Clean up temp nodes
            }

            internal void FindNeighbors(Node node)
            {
                foreach (Node neighbor in nodes)
                {
                    if (neighbor.Map != node.Map) continue;
                    PawnPath p = node.Map.pathFinder.FindPath(node.Location, neighbor.Location, StairParms);
                    float cost = p.TotalCost;
                    p.ReleaseToPool();
                    p = null;
                    if (cost > 0)
                    {
                        node.AddNeighbor(neighbor, cost);
                    }
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="p"></param>
            private List<Node> DijkstraConnect(Node source, Node sink)
            {
                string lastKey = "nokey";
                StringBuilder sb = new StringBuilder();

                try
                {
                    List<Node> ret = null;
                    Dictionary<Node, float> distances = new Dictionary<Node, float>(nodes.Count);
                    Dictionary<Node, Node> previousNodes = new Dictionary<Node, Node>(nodes.Count);
                    HashSet<Node> Q = new HashSet<Node>(nodes);
                    foreach (Node nd in nodes)
                    {
                        distances.SetOrAdd(nd, Single.PositiveInfinity);
                        previousNodes.SetOrAdd(nd, null);
                    }

                    lastKey = "distances/source";
                    distances[source] = 0;
                    if (!Q.Remove(source))
                    {
                        lastKey = "source missing";
                        throw new InvalidDataException("Source somehow not in set of nodes");
                    }
                    foreach (Node neighbor in source.neighbors)
                    {
                        distances[neighbor] = source.neighborDistances[neighbor];
                    }

                    bool found = false;
                    Node u = source, next = null;
                    float cost = 0f;
                    while (Q.Count > 0)
                    {
                        sb.Clear();
                        u = Q.MinBy((x) => distances[x]);
                        foreach (Node nd in u.neighbors)
                        {
                            sb.Append($"nd = {nd}, distance = {distances[nd]}");
                        }

                        sb.Append($"Chose {u} as shortest with {distances[u]}");
                        //ZLogger.Message(sb.ToString());
                        Console.WriteLine(sb.ToString());
                        Q.Remove(u);
                        if (u == sink) break;
                        foreach (Node v in u.neighbors)
                        {
                            float altDistance = distances[u] + u.neighborDistances[v];
                            if (altDistance < distances[v])
                            {
                                distances[v] = altDistance;
                                previousNodes[v] = u;
                            }
                        }
                    }

                    sb.Clear();
                    foreach (Node node in previousNodes.Keys)
                    {
                        sb.Append($"Node:{node}, previous: {previousNodes[node]}, cumulative distance: {distances[node]}");
                    }
                    //ZLogger.Message(sb.ToString());
                    //when this finishes, we have the shortest path from sink to source in the previousNode dictionary- now we just need to get it out
                    ret = new List<Node>();
                    sb.Clear();
                    while (sink != null)
                    {
                        sb.AppendLine($"{sink.key}");
                        ret.Add(sink);
                        sink = previousNodes[sink];
                    }

                    ret.Add(source);
                    sb.AppendLine($"{source.key}");
                    ZLogger.Message(sb.ToString());
                    ret.Reverse();
                    return ret;
                }
                catch (Exception e)
                {
                    sb = new StringBuilder();
                    foreach (object v in e.Data.Keys)
                    {
                        sb.Append($"{e.Data[v]}: {v}");
                    }
                    ZLogger.Message($"Couldn't find key!  Or key was null... {lastKey}  Exception data of type {e.GetType()} follows: {sb}");
                    return new List<Node>();
                }
            }

            //private void AddAndCalculateDistance(Building_Stairs stairs, Building_Stairs stairs2)
            //{
            //    try
            //    {
            //        PathSummary ps = new PathSummary(stairs, stairs2);
            //        Node from = new Node(stairs), to = new Node(stairs2);
            //        var key = new Tuple<Node, Node>(from, to);
            //        if (graphDictionary.ContainsKey(key))
            //        {
            //            graphDictionary[key] = ps;
            //        }
            //        else
            //        {
            //            graphDictionary.Add(key, ps);
            //        }
            //        ps.CalculateInitialCost();
            //        if (float.IsPositiveInfinity(ps.cost))
            //        {
            //            from.neighborDistances.Remove(to);
            //            to.neighborDistances.Remove(from);
            //        }
            //        else
            //        {
            //            from.neighborDistances[to] = to.neighborDistances[from] = ps.cost;
            //            ZLogger.Message($"Adding distance of {ps.cost} for nodes {from} to {to}");
            //        }

            //    }
            //    catch (Exception)
            //    {
            //        ZLogger.Message($"Exception caught.  Something is null.  {stairs}  {stairs2} "); //{graphDictionary}");
            //    }
            //}

            public DijkstraGraph(Map map) : this(map.Tile) { }

            //private readonly Dictionary<Tuple<Node, Node>, PathSummary> graphDictionary = new Dictionary<Tuple<Node, Node>, PathSummary>();
            public class Node
            {
                public override string ToString()
                {
                    return $"Node of stairs: {(key?.ToString()) ?? "unknownStairs"}";
                }

                public Node(Building_Stairs stairs)
                {
                    key = stairs;
                    Location = stairs.Position;
                }

                public Dictionary<Node, float> neighborDistances = new Dictionary<Node, float>();
                public IntVec3 Location;

                /// <summary>
                /// This is used for non-stair nodes (temporary nodes)
                /// </summary>
                /// <param name="location"></param>
                public Node(IntVec3 location)
                {
                    this.Location = location;
                    key = null;
                }

                public Building_Stairs key;
                public readonly HashSet<Node> neighbors = new HashSet<Node>();

                private Map _map = null;

                public Map Map
                {
                    get => _map ?? key.Map;
                    set => _map = value;
                }

                public bool AddNeighbor(Node neighbor, float cost = float.PositiveInfinity)
                {
                    try
                    {
                        if (neighbor.key == null) return false;
                        if (!neighbors.Add(neighbor)) return false;

                        neighbor.AddNeighbor(this, cost);
                        neighborDistances.SetOrAdd(neighbor, cost);

                    }
                    catch (ArgumentException e)
                    {
                        ZLogger.Message(
                            $"AddNeighbor: Key already exists: (I think... e.message = {e.Message}.  Would be key was node for {neighbor.key}");
                    }

                    return true;
                }


                public bool SetDistance(Node neighbor, float distance)
                {
                    try
                    {
                        if (!neighborDistances.ContainsKey(neighbor)) return false;
                        neighborDistances[neighbor] = distance;
                    }
                    catch (ArgumentException e)
                    {
                        ZLogger.Message($"Set Distance: Key already exists: (I think... e.message = {e.Message}.  Would be key was node for {neighbor.key}");
                    }
                    return true;

                }
                //private bool removeTemporaryNeighbors()
                //{
                //    HashSet<Node> tempNeighborNodes = new HashSet<Node>(1);
                //    foreach (Node node in neighbors)
                //    {
                //        if (node.key == null)
                //        {
                //            tempNeighborNodes.Add(node);
                //        }

                //    }
                //    neighbors.ExceptWith(tempNeighborNodes);
                //    return tempNeighborNodes.Count > 0;
                //}

                public void MakeTempNode(Map map)
                {
                    Map = map;

                }
            }

            public class PathSummary : IEquatable<PathSummary>
            {
                public Map sinkMap, sourceMap;
                public IntVec3 sink, source;
                public float cost;

                //public PathSummary(Node from, Node to, float cost = Single.PositiveInfinity) : this(from.key, to.key, cost) { }

                public PathSummary(Thing from, Thing to, float cost = float.PositiveInfinity) : this(from.Map, to.Map, from.Position, to.Position, cost) { }

                PathSummary(LocalTargetInfo from, LocalTargetInfo to, Map fromMap, Map toMap, float cost = float.PositiveInfinity) : this(fromMap, toMap, from.Cell, to.Cell) { }

                public PathSummary(Map fromMap, Map toMap, IntVec3 fromPosition, IntVec3 toPosition, float pathCost = float.PositiveInfinity)
                {
                    sinkMap = toMap;
                    sourceMap = fromMap;
                    sink = toPosition;
                    source = fromPosition;
                    cost = pathCost;
                }

                public bool IsMatchedStair()
                {
                    if (source != sink || sourceMap == sinkMap || Math.Abs(sourceMap.Index - sinkMap.Index) != 1)
                        return false;
                    cost = 1;
                    return true;

                }

                public void CalculateInitialCost()
                {
                    if (sourceMap == sinkMap)
                    {
                        PawnPath p = sourceMap.pathFinder.FindPath(source, sink, StairParms);
                        cost = p.TotalCost;
                        p.ReleaseToPool();
                        if (cost < 0)//unreachable is -1
                        {
                            cost = float.PositiveInfinity;
                        }
                    }
                    else if (IsMatchedStair())
                    {
                        cost = MatchedStairCost;
                    }
                    else
                    {
                        cost = float.PositiveInfinity;
                    }
                }



                public const float MatchedStairCost = 3.0f;


                public bool Equals(PathSummary other)
                {
                    return (other != null) && (other.sourceMap == this.sourceMap) && (other.sinkMap == sinkMap) && (source == other.source) && (sink == other.sink);
                }

                public Job MakeJob()
                {
                    return JobMaker.MakeJob(JobDefOf.Goto, new LocalTargetInfo(source), new LocalTargetInfo(sink));
                }


            }


        }


        private Dictionary<int, DijkstraGraph> stairGraphs;

        private static Lazy<ZPathfinder> _instance = new Lazy<ZPathfinder>();
        public static ZPathfinder Instance => _instance.Value;
        public static readonly TraverseParms StairParms = TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false);

        private static Dictionary<Tuple<Building_Stairs, Building_Stairs>, List<Job>> StairPaths =
            new Dictionary<Tuple<Building_Stairs, Building_Stairs>, List<Job>>();
        public static void ResetPathfinder()
        {

            _instance = new Lazy<ZPathfinder>();

        }

        public ZPathfinder()
        {
            stairGraphs = new Dictionary<int, DijkstraGraph>();
        }



        internal DijkstraGraph getDijkstraGraphForTile(int tile)
        {
            if (!HasDijkstraForTile(tile)) //Already initialized
            {
                throw new ArgumentNullException("Call SetOrCreateDijkstraGraph for the tile first");
            }

            return stairGraphs[tile];
        }

        internal bool HasDijkstraForTile(int tile)
        {
            return stairGraphs.ContainsKey(tile);
        }


        internal void SetOrCreateDijkstraGraph(int tile)
        {
            DijkstraGraph dg = new DijkstraGraph(tile);
            if (!HasDijkstraForTile(tile)) //Already initialized
            {
                stairGraphs.Add(tile, dg);
            }
            else
            {
                stairGraphs[tile] = dg;
            }
        }
        //public List<PawnPath> FindPath(Pawn pawn, Thing dest, bool checkLocalPathfinderForPath = false)
        //{
        //    bool goingUp = !(dest is Building_Stairs) || dest is Building_StairsUp;
        //    return FindPath(pawn.Position, dest.Position, pawn.Map, checkLocalPathfinderForPath, goingUp);
        //}

        //public List<PawnPath> FindPath(IntVec3 source, IntVec3 sink, Map map, bool checkLocalPathfinderForPath = false, bool goingUp = true)
        //{
        //    Building_Stairs SourceStairs = GetClosestStair(source, goingUp, map, out PawnPath pathToSource);
        //    Building_Stairs SinkStairs = GetClosestStair(sink, SourceStairs is Building_StairsUp, map, out PawnPath pathToSink);
        //    List<PawnPath> paths = IterativeFindPath(SourceStairs, SinkStairs);
        //    paths.Insert(0, pathToSource);
        //    paths.Insert(paths.Count - 1, pathToSink);

        //    return paths;


        //}



        //Might implement this later... doesn't seem to be working.  Might have to implement
        //an equality comparer for a class which is taking the place of that Tuple.  
        //private Dictionary<Tuple<Building_Stairs, Building_Stairs>, List<PawnPath>> StairPaths;


        //public void CalculateStairPaths()
        //{
        //    StairPaths = new Dictionary<Tuple<Building_Stairs, Building_Stairs>, List<Job>>();

        //    foreach (Map map in ZUtils.ZTracker.mapIndex.Keys)
        //    {
        //        ZLogger.Message($"Stair paths at start of map index {map.Index}: {StairPaths.Count}");

        //        foreach (Building_Stairs stairsDown in ZUtils.ZTracker.stairsDown[map])
        //        {
        //            foreach (Building_Stairs stairsUp in ZUtils.ZTracker.stairsUp[map])
        //            {
        //                JoinStairs(stairsDown, stairsUp, map);
        //            }

        //            foreach (Building_Stairs stairsDown2 in
        //                ZUtils.ZTracker.stairsDown[map].Where(stairsDown2 => stairsDown != stairsDown2))
        //            {
        //                JoinStairs(stairsDown, stairsDown2, map);
        //            }
        //        }
        //        foreach (Building_Stairs stairsUp in ZUtils.ZTracker.stairsDown[map])
        //        {
        //            foreach (Building_Stairs stairsUp2 in ZUtils.ZTracker.stairsUp[map])
        //            {
        //                JoinStairs(stairsUp, stairsUp2, map);
        //            }
        //        }
        //        ZLogger.Message($"Stair paths at end of map index {map.Index}: {StairPaths.Count}");

        //    }

        //    //var nonPaths = new List<Tuple<Building_Stairs, Building_Stairs>>();

        //    //foreach (var key in StairPaths.Keys)
        //    //{
        //    //    if (StairPaths[key][0] == PawnPath.NotFound)
        //    //    {
        //    //        ZLogger.Message($"Path not found between {key.Item1} & {key.Item2}");
        //    //        nonPaths.Add(key);
        //    //    }
        //    //}

        //    //foreach (var pair in nonPaths)
        //    //{
        //    //    StairPaths[pair] = FindPath(pair.Item1, pair.Item2);
        //    //}
        //}

        //private List<PawnPath> FindPath(Thing source, Thing sink)
        //{
        //    List<PawnPath> ret = IterativeFindPath((Building_Stairs)source,
        //        (Building_Stairs)sink);
        //    return ret;
        //}

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetCurrentMethod()
        {
            var st = new StackTrace();
            var sf = st.GetFrame(1);

            return sf.GetMethod().Name;
        }

        private static int itMax = 10;
        enum StairCasesForPathfinding
        {
            PathKnown,
            SameFloorSameDirections,
            SameFloorDifferentDirections,
            DeadEnd
        };
        //private List<PawnPath> IterativeFindPath(Building_Stairs source, Building_Stairs sink)
        //{
        //    HashSet<Building_Stairs> stairsChecked = null;
        //    List<PawnPath> ret = new List<PawnPath>();

        //    Tuple<Building_Stairs, Building_Stairs, object>
        //        nextSet = new Tuple<Building_Stairs, Building_Stairs, object>(source, sink, null);
        //    for (int i = 0; i < itMax; ++i)
        //    {
        //        ZLogger.Message($"Iteration {i} of {itMax}, source->sink = {source}->{sink}");
        //        nextSet = IterativeFindPath(source, sink, ret, stairsChecked);
        //        source = nextSet.Item1;
        //        sink = nextSet.Item2;
        //        stairsChecked = (HashSet<Building_Stairs>)nextSet.Item3;
        //        //If item1 and 2 are equal, it means we've found the path and can break out
        //        if (nextSet.Item1 != nextSet.Item2) continue;
        //        ret.Reverse();
        //        ZLogger.Message("Path found. Path follows:");
        //        foreach (PawnPath pawnPath in ret)
        //        {
        //            ZLogger.Message($"{pawnPath}");
        //        }
        //        break;
        //    }

        //    return ret;
        //}

        //private Tuple<Building_Stairs, Building_Stairs, object> IterativeFindPath(Building_Stairs source,
        //    Building_Stairs sink, List<PawnPath> paths, HashSet<Building_Stairs> stairsChecked)
        //{
        //    List<PawnPath> PreexistingPath = new List<PawnPath>();
        //    Tuple<Building_Stairs, Building_Stairs, object> ret = null;
        //    switch (GetCaseForPair(source, sink, PreexistingPath))
        //    {
        //        case StairCasesForPathfinding.PathKnown:
        //            paths.AddRange(PreexistingPath);
        //            ret = new Tuple<Building_Stairs, Building_Stairs, object>(source, source, null);
        //            break;
        //        case StairCasesForPathfinding.SameFloorSameDirections:
        //            ret = new Tuple<Building_Stairs, Building_Stairs, object>(source.GetMatchingStair(),
        //                sink.GetMatchingStair(), null);
        //            break;
        //        case StairCasesForPathfinding.SameFloorDifferentDirections:
        //            //In simpler case, we just follow the 1 chain, in this case the source:
        //            HashSet<Building_Stairs> stairsTaken = new HashSet<Building_Stairs>();
        //            ret = new Tuple<Building_Stairs, Building_Stairs, object>(sink.GetMatchingStair(),
        //                PickNextSource(sink.GetMatchingStair(), ref stairsTaken), stairsTaken);

        //            //HashSet<Building_Stairs> sourceStairs= new HashSet<Building_Stairs>(), sinkStairs= new HashSet<Building_Stairs>();
        //            //Building_Stairs nextSource = PickNextSource(source.GetMatchingStair(), ref sourceStairs);
        //            //Building_Stairs nextSink = PickNextSource(sink.GetMatchingStair(), ref sinkStairs);
        //            //var tups = new Tuple<HashSet<Building_Stairs>, Building_Stairs,
        //            //    Building_Stairs, HashSet<Building_Stairs>>
        //            //    (sourceStairs, sink.GetMatchingStair(), nextSink, sinkStairs);

        //            //ret = new Tuple<Building_Stairs, Building_Stairs, object>(source.GetMatchingStair(), nextSource, tups);
        //            break;
        //        case StairCasesForPathfinding.DeadEnd:
        //            ret = null;
        //            break;
        //        default:
        //            throw new ArgumentOutOfRangeException();
        //    }

        //    return ret;
        //}

        //private List<PawnPath> RecursiveFindPath(Building_Stairs source, Building_Stairs sink, List<PawnPath> paths)
        //{
        // Recursion: get a list of directions back and incorporate them in the list
        // So if we go down a level, we can find a new source and sink- if both are going the same direction,
        // it'll be the stairs directly below/above each.  If they find, we're done.  If they don't, we will have to backtrack
        // Backtracking recursion... I haven't done that in a while.  We can use the lists themselves to backtrack.
        // Essentially, we'll check random stairs until we find a viable path.  We don't have
        // to actually do any traversals- we can just use Stair

        //    ZLogger.Message($"Recursing for stairs {source} to {sink}");
        //    //Case -1: handed null means one tail of a recursion has hit a dead end
        //    if (source == null || sink == null)
        //    {
        //        return null;
        //    }

        //    List<PawnPath> sourceSinkPaths = null;
        //    //Simplest case, case 0: we find a path directly- but this won't be the first level if this function is getting called.
        //    if (CheckStairPathsForKeyPair(sink, source, ref  sourceSinkPaths))
        //    {
        //        ZLogger.Message($"case 0 for stairs {source} to {sink}");
        //        paths.AddRange(sourceSinkPaths);
        //        return paths;
        //    }
        //    ZLogger.Message($"cases 1, 2, or 3 for stairs {source} to {sink}");

        //    bool sinkDown = sink is Building_StairsDown, sourceDown = source is Building_StairsDown;
        //    var dict = sinkDown ? ZUtils.ZTracker.stairsDown : ZUtils.ZTracker.stairsUp;
        //    var connectingMap = ZUtils.ZTracker.GetMapByIndex(sink.Map.Tile,
        //        ZUtils.ZTracker.mapIndex[sink.Map] + (sinkDown ? -1 : 1));

        //    //Next we'll search for alternate paths on the same floor
        //    //First hold source constant
        //    //case 1: No direct connection, but source and sink go the same direction.  
        //    if (sourceDown == sinkDown)
        //    {
        //        Building_Stairs nextSource, nextSink;
        //        nextSource = source.GetMatchingStair();
        //        nextSink = sink.GetMatchingStair();
        //        ZLogger.Message($"Case 1 for stairs {source} to {sink}");
        //        ZLogger.Message($"next stairs are  {nextSource} to {nextSink}");

        //        var temp = IterativeFindPath(nextSource, nextSink);
        //        if (temp == null)
        //        {
        //            return paths;
        //        }

        //        paths.AddRange(temp);
        //        return paths;
        //    }

        //    //case 2: No direct connection, and source and sink go different directions
        //    List<Building_Stairs> stairs = sinkDown ? ZUtils.ZTracker.stairsDown[sink.Map] : ZUtils.ZTracker.stairsUp[sink.Map];
        //    foreach (Building_Stairs stair in stairs)
        //    {
        //        ZLogger.Message($"case 2 for stairs {source.GetType()} {source} to {source.GetType()} {sink} ");

        //        var stairSet = new HashSet<Building_Stairs>(new[] { source, sink });
        //        if (stair == source || stair == sink)
        //        {
        //            continue;
        //        }

        //        Building_Stairs matchingSink = sink.GetMatchingStair(),
        //                        matchingSource = source.GetMatchingStair();
        //        Building_Stairs nextSink = PickNextSource(matchingSource, ref stairSet),
        //                        nextSource = PickNextSource(matchingSink, ref stairSet);
        //        //If PickNextSource is exhausted, then we've effectively hit a dead en-
        //        //we'll return null, but the next call of recurse will handle tying off that tail

        //        List<PawnPath> sourcePath = RecursiveFindPath(matchingSource, nextSource, new List<PawnPath>()),
        //                       sinkPath = RecursiveFindPath(matchingSink, nextSink, new List<PawnPath>());
        //        if (sourcePath == null || sinkPath == null)
        //        {
        //            if (sourcePath == null && sinkPath == null)
        //            {
        //                return null;
        //            }
        //            return sourcePath ?? sinkPath;
        //        }
        //        else
        //        {
        //            float sinkCost = 0, sourceCost = 0;
        //            sinkCost += sinkPath.Sum(path => path.TotalCost);
        //            sourceCost += sourcePath.Sum(path => path.TotalCost);
        //            return sinkCost < sourceCost ? sinkPath : sourcePath;
        //        }
        //    }
        //    //case 3: Dead end and backtrack
        //    return null;
        //}

        public List<DijkstraGraph.Node> FindRoute(IntVec3 from, IntVec3 to, Map mapFrom, Map mapTo)
        {
            if (!HasDijkstraForTile(mapFrom.Tile))
            {
                SetOrCreateDijkstraGraph(mapFrom.Tile);
            }

            return getDijkstraGraphForTile(mapFrom.Tile).FindRoute(from, to, mapFrom, mapTo);


        }



        private StairCasesForPathfinding GetCaseForPair(Building_Stairs source, Building_Stairs sink, List<Tuple<IntVec3, IntVec3>> newPath)
        {
            StairCasesForPathfinding casesForPathfinding;
            if (source == null || sink == null)
            {
                casesForPathfinding = StairCasesForPathfinding.DeadEnd;
            }
            else
            {
                //if (CheckStairPathsForKeyPair(sink, source,ref  newPath))
                if (sink.Map == source.Map)
                {
                    PawnPath path = source.Map.pathFinder.FindPath(source.Position, new LocalTargetInfo(sink), StairParms);
                    Job job = JobMaker.MakeJob(JobDefOf.Goto, source, sink);
                    LocalTargetInfo t = new LocalTargetInfo();
                    PawnPath f = source.Map.pathFinder.FindPath(source.Position, new LocalTargetInfo(sink), StairParms);

                    if (f != PawnPath.NotFound)
                    {
                        newPath.Add(new Tuple<IntVec3, IntVec3>(f.FirstNode, f.LastNode));

                        casesForPathfinding = StairCasesForPathfinding.PathKnown;
                    }
                    else
                    {
                        f.ReleaseToPool();

                        Building_Stairs newSink = GetClosestStair(source, out f);
                        casesForPathfinding = StairCasesForPathfinding.PathKnown;

                        if (f != null)
                        {
                            newPath.Add(new Tuple<IntVec3, IntVec3>(f.FirstNode, f.LastNode));
                            casesForPathfinding = StairCasesForPathfinding.DeadEnd;
                        }
                    }
                }
                else
                {
                    bool sourceDown = source is Building_StairsDown, sinkDown = sink is Building_StairsDown;
                    casesForPathfinding = sinkDown == sourceDown ? StairCasesForPathfinding.SameFloorSameDirections : StairCasesForPathfinding.SameFloorDifferentDirections;
                }
            }
            ZLogger.Message($"The case for {sink} and {source} is {casesForPathfinding}");

            return casesForPathfinding;
        }

        public Building_Stairs GetClosestStair(Building_Stairs source, out PawnPath path)
        {
            return GetClosestStair(source.Position, source is Building_StairsUp, source.Map, out path);
        }


        public Building_Stairs GetClosestStair(IntVec3 source, bool goingUp, Map sourceMap, out PawnPath path)
        {
            path = null;
            List<Building_Stairs> targetList = goingUp ? ZUtils.ZTracker.stairsUp[sourceMap] :
                ZUtils.ZTracker.stairsDown[sourceMap];
            var pathFinder = sourceMap.pathFinder;
            Dictionary<Building_Stairs, PawnPath> costs = new Dictionary<Building_Stairs, PawnPath>(targetList.Count - 1);
            foreach (var stairs in targetList)
            {
                if (stairs.Position == source) continue;
                path = pathFinder.FindPath(source, new LocalTargetInfo(stairs), StairParms);
                costs.Add(stairs, path);
            }

            if (costs.Count == 0) return null;

            float lowestCost = Single.PositiveInfinity;
            Building_Stairs ret = null;
            foreach (Building_Stairs stairs in costs.Keys)
            {
                if (lowestCost > costs[stairs].TotalCost)
                {
                    lowestCost = costs[stairs].TotalCost;
                    ret = stairs;
                }
            }
            return ret;
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

        //private bool CheckStairPathsForKeyPair(Building_Stairs sink, Building_Stairs source, ref List<PawnPath> newPath)
        //{
        //    ZLogger.Message($"Checking for path between {source} and {sink}");
        //    //Tuple<Building_Stairs, Building_Stairs> spoot = new Tuple<Building_Stairs, Building_Stairs>(source, sink),
        //    //   toops = new Tuple<Building_Stairs, Building_Stairs>(sink, source);
        //   if(newPath == null) newPath = new List<PawnPath>();
        //    newPath.Add(JoinStairs(sink, source, sink.Map));
        //    //newPath = StairPaths.ContainsKey(spoot) ? StairPaths[spoot] : StairPaths[toops];
        //    bool found = newPath[0] != PawnPath.NotFound;
        //    ZLogger.Message($"we {(found ? "found" : "didn't find")} a path)");
        //    return found;
        //}



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

            //var tuple = new Tuple<Building_Stairs, Building_Stairs>(startStairs, endStairs);
            //if (!StairPaths.ContainsKey(tuple))
            //{
            //    if (path == PawnPath.NotFound)
            //    {
            //        ZLogger.Message($"No path found for {startStairs} to {endStairs}");
            //    }
            //    else
            //    {
            //        ZLogger.Message($"Path found for {startStairs} to {endStairs}: {path}");
            //    }

            //    StairPaths.Add(tuple, new List<PawnPath> { path });
            //}
            //else
            //{
            //    if (path == PawnPath.NotFound)
            //    {
            //        ZLogger.Message($"No path found for {startStairs} to {endStairs}");
            //    }
            //    else
            //    {
            //        ZLogger.Message($"Path found for {startStairs} to {endStairs}: {path}");
            //    }

            //    StairPaths[tuple] = new List<PawnPath> { path };

            //}

            return path;



        }

    }



}
