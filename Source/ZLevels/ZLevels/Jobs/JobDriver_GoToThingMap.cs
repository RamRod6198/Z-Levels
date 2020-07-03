using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace ZLevels
{
    public class JobDriver_GoToThingMap : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Log.Message("JobDriver_GoToThingMap : JobDriver - TryMakePreToilReservations - return true; - 1", true);
            return true;
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            Log.Message("JobDriver_GoToThingMap : JobDriver - MakeNewToils - foreach (var toil in Toils_ZLevels.GoToMap(GetActor(), this.job.targetB.Thing.Map, this)) - 2", true);
            foreach (var toil in Toils_ZLevels.GoToMap(GetActor(), this.job.targetB.Thing.Map, this))
            {
                Log.Message("JobDriver_GoToThingMap : JobDriver - MakeNewToils - if (pawn.Map != this.job.targetB.Thing.Map) - 3", true);
                if (pawn.Map != this.job.targetB.Thing.Map)
                {
                    yield return new Toil { initAction = delegate () { Log.Message("JobDriver_GoToThingMap : JobDriver - MakeNewToils - yield return toil; - 4", true); } };
                    yield return toil;
                }
            }
        }
    }
}

