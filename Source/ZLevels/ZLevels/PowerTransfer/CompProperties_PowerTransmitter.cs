using System;
using RimWorld;
using Verse;

namespace ZLevels
{
	public class CompProperties_PowerTransmitterTest : CompProperties_Power
	{
		public CompProperties_PowerTransmitterTest()
		{
			this.compClass = typeof(CompPowerTransmitterTest);
		}

		public float storedEnergyMax = 1000f;

		public float efficiency = 0.7f;
	}
}
