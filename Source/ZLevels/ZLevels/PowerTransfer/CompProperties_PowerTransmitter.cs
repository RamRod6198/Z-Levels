using System;
using RimWorld;
using Verse;

namespace ZLevels
{
	public class CompProperties_PowerZTransmitter : CompProperties_Power
	{
		public CompProperties_PowerZTransmitter()
		{
			this.compClass = typeof(CompPowerZTransmitter);
		}

		public float storedEnergyMax = 1000f;

		public float efficiency = 0.7f;
	}
}

