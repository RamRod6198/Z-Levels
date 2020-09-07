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
    /// //TODO: create temp nodes to eliminate tendency to walk to stairs first
    /// </summary>
    public class ZPathfinder
    {
        public const float MatchedStairCost = 20.0f;

        public class DijkstraGraph
        {
            private int _tile;
            HashSet<Node> nodes = new HashSet<Node>();

            public DijkstraGraph(int tile)
            {
                _tile = tile;
                Build();
            }

            private void Build()
            {
                List<Map> maps = ZUtils.ZTracker.GetAllMaps(_tile);
                foreach (Map map in maps)
                {
                    foreach (Building_Stairs stairs in ZUtils.ZTracker.stairsUp[map])
                    {
                        nodes.Add(new Node(stairs));
                    }

                    foreach (Building_Stairs stairs in ZUtils.ZTracker.stairsDown[map])
                    {
                        nodes.Add(new Node(stairs));
                    }
                }

                foreach (var node in nodes)
                {
                    foreach (var neighborNode in nodes)
                    {
                        if (node == neighborNode) continue;
                        float cost = Single.PositiveInfinity;
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
                        }

                        if (cost < 0) cost = Single.PositiveInfinity;
                        neighbors = !Single.IsPositiveInfinity(cost);
                        if (neighbors)
                        {
                            node.AddNeighbor(neighborNode, cost);
                        }

                        //if (neighbors && node.AddNeighbor(neighborNode, cost))
                        //{
                        //    ZLogger.Message($"Made {node} and {neighborNode} neighbors.  Distance = {cost}");
                        //}
                    }
                }
            }

            private bool IsMatchedStairSet(Node node, Node node2)
            {
                if (node.Map == node2.Map) return false;
                int nodeIndex = ZUtils.ZTracker.GetZIndexFor(node.Map), node2Index = ZUtils.ZTracker.GetZIndexFor(node2.Map);
                if (nodeIndex + 1 == node2Index && node.key is Building_StairsUp ||
                    nodeIndex - 1 == node2Index && node.key is Building_StairsDown)
                {
                    return node.key.Position == node2.key.Position;
                }

                return false;
            }

            public List<Node> FindRoute(IntVec3 from, IntVec3 to, Map mapFrom, Map mapTo, out float routeCost)
            {
                Building_Stairs sourceStairs = (Building_Stairs)GenClosest.ClosestThing_Global_Reachable(from
                    , mapFrom, mapFrom.listerThings.AllThings
                        .Where(x => x is Building_Stairs), PathEndMode.OnCell, StairParms);


                Building_Stairs sinkStairs = (Building_Stairs)GenClosest.ClosestThing_Global_Reachable(to
                    , mapFrom, mapTo.listerThings.AllThings
                        .Where(x => x is Building_Stairs), PathEndMode.OnCell, StairParms);
                Node source = null, sink = null;
                foreach (Node x in nodes)
                {
                    Building_Stairs stairs = x.key;
                    if (stairs == sourceStairs)
                    {
                        source = x;
                    }
                    if (stairs == sinkStairs)
                    {
                        sink = x;
                    }
                }

                if (sink == null || source == null)
                {
                    this.Rebuild();
                }

                FindNeighbors(source);

                FindNeighbors(sink);
                if (source == null || sink == null)
                {
                    routeCost = float.PositiveInfinity;
                    return null;
                }
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
                return DijkstraConnect(source, sink, out routeCost);

            }

            internal void Rebuild()
            {
                nodes = new HashSet<Node>();
                Build();
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

            private List<Node> DijkstraConnect(Node source, Node sink, out float routeCost)
            {
                string lastKey = "nokey";
                routeCost = -1;
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
                    Node u = source;
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
                    ret = new List<Node>();
                    routeCost = distances[sink];
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

            public DijkstraGraph(Map map) : this(map.Tile) { }

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

                public bool AddNeighbor(Node neighbor, float cost = Single.PositiveInfinity)
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

                public PathSummary(Thing from, Thing to, float cost = Single.PositiveInfinity) : this(@from.Map, to.Map, @from.Position, to.Position, cost) { }

                PathSummary(LocalTargetInfo from, LocalTargetInfo to, Map fromMap, Map toMap, float cost = Single.PositiveInfinity) : this(fromMap, toMap, @from.Cell, to.Cell) { }

                public PathSummary(Map fromMap, Map toMap, IntVec3 fromPosition, IntVec3 toPosition, float pathCost = Single.PositiveInfinity)
                {
                    sinkMap = toMap;
                    sourceMap = fromMap;
                    sink = toPosition;
                    source = fromPosition;
                    cost = pathCost;
                }

                public bool IsMatchedStair()
                {
                    if (source != sink || sourceMap == sinkMap || Math.Abs(ZUtils.ZTracker.GetZIndexFor(sourceMap) - ZUtils.ZTracker.GetZIndexFor(sinkMap)) != 1)
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
                            cost = Single.PositiveInfinity;
                        }
                    }
                    else if (IsMatchedStair())
                    {
                        cost = MatchedStairCost;
                    }
                    else
                    {
                        cost = Single.PositiveInfinity;
                    }
                }

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

        public List<DijkstraGraph.Node> FindRoute(IntVec3 from, IntVec3 to, Map mapFrom, Map mapTo, out float routeCost)
        {
            ZLogger.Message($"Find route from {from}to {to}  mapFrom {mapFrom} mapTo {mapTo}");
            if (!HasDijkstraForTile(mapFrom.Tile))
            {
                SetOrCreateDijkstraGraph(mapFrom.Tile);
            }
            else
            {
                DijkstraGraph graph = getDijkstraGraphForTile(mapFrom.Tile);
                graph?.Rebuild();
            }
            return getDijkstraGraphForTile(mapFrom.Tile).FindRoute(from, to, mapFrom, mapTo, out routeCost);

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
