using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Verse;

namespace testApp
{
    class Program
    {
        static public Random rng = new Random((int)17);
        static List<Node> nodes = new List<Node>();
        
        static void Main(string[] args)
        {

            for (int i = 0; i < 25; ++i)
            {
                nodes.Add(new Node($"Node {i}"));
            }

            foreach (Node node in nodes)
            {
                int target = rng.Next(3, 5);
                for (int i = 0; i < target; i++)
                {
                    node.AddNeighbor(nodes[rng.Next(0, 10)]);
                }
            }

            DijkstraConnect(nodes[1], nodes[13]);
            Console.ReadLine();
        }
        private static List<Node> DijkstraConnect(Node source, Node sink)
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
                    lastKey = "bad";
                    throw new Exception("Bad");
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
                while (sink != null)
                {
                    ret.Add(sink);
                    sink = previousNodes[sink];
                }

                ret.Add(source);
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
                //ZLogger.Message($"Couldn't find key!  Or key was null... {lastKey}  Exception data of type {e.GetType()} follows: {sb}");
                return new List<Node>();
            }
        }

    }


    class Node
    {
        public HashSet<Node> neighbors = new HashSet<Node>();
        public Dictionary<Node, float> neighborDistances = new Dictionary<Node, float>();

        public Node(string name)
        {
            id = name;
        }

        public override string ToString()
        {
            return id;
        }

        public string id;
        public void AddNeighbor(Node node)
        {
            if (node == this) return;
            neighbors.Add(node);
            float dist = (float)Program.rng.NextDouble() * 100f;
            neighborDistances.SetOrAdd(node, dist);
                node.AddNeighbor(this, dist);
            
        }

        private void AddNeighbor(Node node, float dist)
        {
            neighborDistances.SetOrAdd(node, dist);
            neighbors.Add(node);
        }
    }

    public static class DictionaryModifier
    {
        public static void SetOrAdd<Tkey, Tval>(this Dictionary<Tkey, Tval> dict, Tkey key, Tval val)
        {
            if (dict.ContainsKey(key))
            {
                dict[key] = val;
            }
            else
            {
                dict.Add(key, val);
            }
        }
    }
}
