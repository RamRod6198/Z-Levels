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
			Scribe_TargetInfo.Look(ref targetDest, "targetDest");
			Scribe_Collections.Look<Job>(ref this.activeJobs, "activeJobs", LookMode.Deep);
			//Scribe_Collections.Look<WorkGiverDef>(ref this.ignoreGiversInFirstTime, "ignoreGiversInFirstTime", LookMode.Def);
			Scribe_References.Look<Job>(ref this.mainJob, "mainJob");
			Scribe_Values.Look<bool>(ref searchingJobsNow, "searchingJobsNow", false);
			Scribe_References.Look<Map>(ref oldMap, "oldMap");
			Scribe_Values.Look<bool>(ref forceGoToDestMap, "forceGoToDestMap", false);
			Scribe_References.Look<Thing>(ref target, "target");
			Scribe_Values.Look<bool>(ref forceGoToDestMap, "failIfTargetMapIsNotDest", false);
			Scribe_Collections.Look<LocalTargetInfo>(ref reservedThings, "reservedThings", LookMode.LocalTargetInfo);
		}

		public bool searchingJobsNow = false;

		public Map oldMap;

		public Job mainJob;

		public bool forceGoToDestMap;

		public Thing target;

		public bool failIfTargetMapIsNotDest;

		//public HashSet<WorkGiverDef> ignoreGiversInFirstTime;

		public List<Job> activeJobs;

		public List<LocalTargetInfo> reservedThings;

		public TargetInfo targetDest;
	}
}

