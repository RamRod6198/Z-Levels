using System;
using System.Collections.Generic;
using Verse;

namespace ZLevels
{
	public class ActiveArea : IExposable
	{
		public ActiveArea()
		{
		}

		public void ExposeData()
		{
			if (Scribe.mode == LoadSaveMode.Saving && this.activeAreas != null)
			{
				this.activeAreas.RemoveAll(x => x.Value == null);
			}
			Scribe_Collections.Look<Map, Area>(ref this.activeAreas, "activeAreas", LookMode.Reference, LookMode.Reference);
		}

		public Dictionary<Map, Area> activeAreas;
	}
}

