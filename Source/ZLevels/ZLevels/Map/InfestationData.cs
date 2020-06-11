using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ZLevels
{
    public class InfestationData : IExposable
    {
        public InfestationData()
        {
        }

        public InfestationData(List<PawnKindDef> infestators, float infestationParms, IntVec3 infestationPlace)
        {
            this.infestators = infestators;
            this.infestationParms = infestationParms;
            this.infestationPlace = infestationPlace;
        }
        public void ExposeData()
        {
            Scribe_Collections.Look<PawnKindDef>(ref this.infestators, "infestators", LookMode.Def, Array.Empty<object>());
            Scribe_Values.Look<float>(ref this.infestationParms, "infestationParms", 0f, true);
            Scribe_Values.Look<IntVec3>(ref this.infestationPlace, "infestationPlace", IntVec3.Invalid, true);
        }

        public List<PawnKindDef> infestators;
        public float infestationParms;
        public IntVec3 infestationPlace;
    }
}

