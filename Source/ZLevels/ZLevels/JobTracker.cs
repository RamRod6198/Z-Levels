using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ZLevels
{
	public class JobTracker : IExposable
	{
		public JobTracker()
		{
		}

		public void ExposeData()
		{
			if (Scribe.mode == LoadSaveMode.Saving && this.activeJobs != null)
			{
				this.activeJobs.RemoveAll(x => x == null);
			}
			Scribe_Collections.Look<Job>(ref this.activeJobs, "activeJobs", LookMode.Deep);
			Scribe_References.Look<Job>(ref this.mainJob, "mainJob");
		}
		public Map oldMap;

		public Map dest;

		public Job mainJob;

		public List<WorkGiverDef> ignoreGiversInFirstTime;

		public List<Job> activeJobs;
	}
}

