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
        //Target B = sink stairs (closest to destination
        //Target C = source stairs (closest to pawn)  
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

            List<ZPathfinder.DijkstraGraph.Node> stairList =
                ZPathfinder.Instance.FindRoute(pawn.Position, TargetLocA, pawn.Map, TargetB.Thing.Map);
            yield return new Toil { initAction = delegate
            {
                ZLogger.Message(
                    $"Target  a = {TargetA.Thing}, target B = {TargetB.Thing}, Target c = {TargetC.Thing}"); } };
            yield return new Toil
            { initAction = delegate () {
                for (int j = 0; j < stairList.Count; ++j)
                {
                    ZLogger.Message($"Stairs {j} = {stairList[j]}");
                } } };            

            for (int i = 0; i < stairList.Count - 1; ++i)
            {
                int i1 = i;
                yield return new Toil
                { initAction = delegate () { ZLogger.Message($"Going to target {i1} boss"); } };
            
                Toil setStairs = Toils_ZLevels.GetSetStairs(pawn, stairList[i].Map, this);
                Toil useStairs = Toils_General.Wait(60, 0);
                useStairs.WithProgressBarToilDelay(TargetIndex.A);

                yield return setStairs;
                yield return Toils_Goto.GotoCell(stairList[i].Location, PathEndMode.OnCell);
                yield return useStairs;


                yield return Toils_ZLevels.GetTeleport(pawn, stairList[i1+1].Map, this, Toils_ZLevels.GetSetStairs(pawn, stairList[i1+1].Map, this));


                //foreach (Toil t in Toils_ZLevels.GoToMap(pawn, stairList[i1 + 1].Map, this))
                //{
                //    yield return t;
                //}

                yield return new Toil
                { initAction = delegate () { ZLogger.Message($"Should be done"); } };
            }
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.OnCell);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell);
        }


        //Get pawn, target location and destination. (rough algorithm for hauling with dijkstra
        //Check that pawn can reach target location and destination with Dijkstra.
        //Break if not.
        //Otherwise, calculate toils to haul thing, then toils to destination and return them one at a time.

    }
}
