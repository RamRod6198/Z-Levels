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
            Log.Message("JobDriver_GoToMap : JobDriver - MakeNewToils - throw new System.NotImplementedException(); - 2", true);
            throw new System.NotImplementedException();
        }

        //protected override IEnumerable<Toil> MakeNewToils()
        //{
        //    
        //    foreach (var toil in Toils_ZLevels.GoToDest(GetActor(), this))
        //    {
        //        yield return toil;
        //    }
        //}
    }
}

