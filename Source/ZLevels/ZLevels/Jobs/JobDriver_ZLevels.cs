using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using ZLevels.Properties;

namespace ZLevels
{
    public abstract class JobDriver_ZLevels : JobDriver
    {
        private List<ZPathfinder.DijkstraGraph.Node> nodeList;
        public List<ZPathfinder.DijkstraGraph.Node> GetRoute(TargetInfo targetInfo)
        {
            if (nodeList == null)
            {
                ZLogger.Message($"pawn map: {pawn.Map}", debugLevel: DebugLevel.Pathfinding);
                nodeList = ZPathfinder.Instance.FindRoute(pawn.Position, targetInfo.Cell, pawn.Map, targetInfo.Map,
                out float routeCost);
            }
            return nodeList;
        }
    }
}

