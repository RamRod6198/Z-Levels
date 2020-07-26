using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Verse;

namespace ZLevels.Properties
{
    [Serializable]
    public class Pathfinder : ISerializable
    {
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }

        private List<MapSynopsis> MapSummaries = new List<MapSynopsis>();
    }

    [Serializable]

    public class MapSynopsis : ISerializable
    {
        public MapSynopsis(Map basis)
        {
            Messages.Message($"Size = ({basis.Size.x},{basis.Size.y}, {basis.Size.z}", null, null, false);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class TileNode : ISerializable
    {
        private HashSet<TileNode> adjacencyList = new HashSet<TileNode>();


        public TileNode(bool traversable = false, params TileNode[] neighborList)
        {
            Traversable = traversable;
            adjacencyList.AddRange(neighborList);
        }

        public bool Traversable { get; set; }

        public bool AddAdjacentNode(TileNode newNode)
        {
            return adjacencyList.Add(newNode);
        }

        public bool RemoveNode(TileNode newNode)
        {
            return adjacencyList.Remove(newNode);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }
    }

}