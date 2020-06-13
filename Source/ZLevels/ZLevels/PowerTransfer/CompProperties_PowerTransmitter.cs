using System;
using RimWorld;
using Verse;

namespace ZLevels
{
	public class CompProperties_PowerTransmitter : CompProperties_Power
	{
		public CompProperties_PowerTransmitter()
		{
			this.compClass = typeof(CompPowerTransmitter);
		}

		public float storedEnergyMax = 1000f;

		public float efficiency = 0.7f;
	}
}

