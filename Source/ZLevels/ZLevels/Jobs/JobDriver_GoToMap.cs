using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace ZLevels
{
    public class JobDriver_GoToMap : JobDriver_ZLevels
    {
        public override void ExposeData()
        {
            base.ExposeData();
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }
        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => ZUtils.ZTracker.jobTracker.ContainsKey(pawn) && ZUtils.ZTracker.jobTracker[pawn].failIfTargetMapIsNotDest 
                && ZUtils.ZTracker.jobTracker[pawn].target.Map != ZUtils.ZTracker.jobTracker[pawn].targetDest.Map);
            ZLogger.Message($"JobDriver GoToMap About to call findRouteWithStairs, with pawn {GetActor()}, dest { ZUtils.ZTracker.jobTracker[pawn].targetDest}, instance {this}");
            foreach (var toil in Toils_ZLevels.GoToMap(GetActor(), ZUtils.ZTracker.jobTracker[pawn].targetDest.Map, this))
            {
                yield return toil;
            }
        }
    }
}

