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
        public int curIndex = 0;
        public IntVec3 curLocation;
        public Map curMap;
        public Building_Stairs curStairs;

        private List<ZPathfinder.DijkstraGraph.Node> nodeList;
        public List<ZPathfinder.DijkstraGraph.Node> GetRoute(TargetInfo targetInfo)
        {
            if (nodeList == null)
            {
                ZLogger.Message($"pawn map: {pawn.Map}", debugLevel: DebugLevel.Pathfinding);
                nodeList = ZPathfinder.Instance.FindRoute(pawn.Position, targetInfo.Cell, pawn.Map, targetInfo.Map,
                out float routeCost);
                curIndex = 1;
                curMap = pawn.Map;
            }
            return nodeList;
        }

        public Building_Stairs GetCurrentStairs(TargetInfo targetInfo)
        {
            if (curStairs != null)
            {
                return curStairs;
            }
            else
            {
                var route = this.GetRoute(targetInfo);
                return route[curIndex].key;
            }
        }

        public IntVec3 GetCurrentLocation(TargetInfo targetInfo)
        {
            if (curLocation != null)
            {
                return curLocation;
            }
            else
            {
                var route = this.GetRoute(targetInfo);
                return route[curIndex].key.Position;
            }
        }
        public Map GetNextMap(TargetInfo targetInfo)
        {
            var route = this.GetRoute(targetInfo);
            return route[curIndex + 1].key.Map;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref curIndex, "curIndex", 0);
            Scribe_Values.Look<IntVec3>(ref curLocation, "curLocation", IntVec3.Invalid);
            Scribe_References.Look<Map>(ref curMap, "curMap");
            Scribe_References.Look<Building_Stairs>(ref curStairs, "curStairs");
        }
    }
}

