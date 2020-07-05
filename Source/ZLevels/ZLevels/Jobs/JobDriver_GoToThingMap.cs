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
            return true;
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            foreach (var toil in Toils_ZLevels.GoToMap(GetActor(), this.job.targetB.Thing.Map, this))
            {
                if (pawn.Map != this.job.targetB.Thing.Map)
                {
                    yield return toil;
                }
            }
        }
    }
}

