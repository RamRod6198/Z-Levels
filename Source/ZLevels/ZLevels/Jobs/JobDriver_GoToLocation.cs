using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;
using ZLevels.Properties;

namespace ZLevels
{
    public class JobDriver_GoToLocation : JobDriver_ZLevels
    {
        //Target A = destination
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        public override void ExposeData()
        {
            base.ExposeData();
        }

        public List<ZPathfinder.DijkstraGraph.Node> ActiveStairs;

        public override IEnumerable<Toil> MakeNewToils()
        {
            //have pawn, and  destination.
            //Check that pawn can reach  destination with Dijkstra.
            //yield break if not.
            //Otherwise, calculate toils to go, then toils to destination and return them one at a time.
            ZLogger.Message($"JobDriver GoToLocation About to call findRouteWithStairs, with pawn {pawn}, dest { TargetA.ToTargetInfo(pawn.Map)}, instance {this}");

            foreach (var v in Toils_ZLevels.FindRouteWithStairs(pawn, TargetA.ToTargetInfo(pawn.Map), this))
            {
                yield return v;
            }
        }


        //Get pawn, target location and destination. (rough algorithm for hauling with dijkstra
        //Check that pawn can reach target location and destination with Dijkstra.
        //Break if not.
        //Otherwise, calculate toils to haul thing, then toils to destination and return them one at a time.

    }
}
