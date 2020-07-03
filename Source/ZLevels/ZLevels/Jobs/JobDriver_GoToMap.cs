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
            Log.Message("JobDriver_GoToMap : JobDriver - TryMakePreToilReservations - return true; - 1", true);
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Log.Message("JobDriver_GoToMap : JobDriver - MakeNewToils - foreach (var toil in Toils_ZLevels.GoToMap(GetActor(), ZUtils.ZTracker.jobTracker[pawn].dest, this)) - 2", true);
            foreach (var toil in Toils_ZLevels.GoToMap(GetActor(), ZUtils.ZTracker.jobTracker[pawn].dest, this))
            {
                yield return new Toil { initAction = delegate () { Log.Message("JobDriver_GoToMap : JobDriver - MakeNewToils - yield return toil; - 3", true); } };
                yield return toil;
            }
        }
    }
}

