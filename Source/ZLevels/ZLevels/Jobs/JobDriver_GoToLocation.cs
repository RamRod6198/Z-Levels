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
    public class JobDriver_GoToLocation : JobDriver
    {
        //Target A = destination
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            //have pawn, source stairs, sink stairs, and  destination.
            //Check that pawn can reach  destination with Dijkstra.
            //yield break if not.
            //Otherwise, calculate toils to go, then toils to destination and return them one at a time.
            foreach (var v in Toils_ZLevels.FindRouteWithStairs(pawn, TargetA, this))
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
