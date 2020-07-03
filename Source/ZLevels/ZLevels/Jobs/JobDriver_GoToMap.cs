using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace ZLevels
{
    public class JobDriver_GoToMap : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            foreach (var toil in Toils_ZLevels.GoToMap(GetActor(), ZUtils.ZTracker.jobTracker[pawn].dest, this))
            {
                yield return toil;
            }
        }
    }
}

