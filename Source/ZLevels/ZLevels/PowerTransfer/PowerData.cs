using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ZLevels
{
    public class PowerData : IExposable
    {
        public PowerData()
        {

        }
        public void ExposeData()
        {
            Scribe_Collections.Look<CompPowerZTransmitter>(ref this.transmitters, "transmitters", LookMode.Deep, Array.Empty<object>());
            Scribe_Collections.Look<CompPowerBattery>(ref this.batteryComps, "batteryComps", LookMode.Deep, Array.Empty<object>());
        }
        public List<CompPowerZTransmitter> transmitters;
        public List<CompPowerBattery> batteryComps;
    }
}

