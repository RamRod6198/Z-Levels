using System;
using System.Collections.Generic;
using System.Diagnostics;
using RimWorld;
using Verse;
using Verse.AI;

namespace ZLevels
{
	public class JobTracker : IExposable
	{
		public Pawn pawn;
		public JobTracker()
		{
			activeJobs = new List<Job>();
			reservedThings = new List<LocalTargetInfo>();
			reservedCells = new List<LocalTargetInfo>();
			lookedAtLocalCellMap = new Dictionary<IntVec3, Map>();
		}
		public void ExposeData()
		{
			if (Scribe.mode == LoadSaveMode.Saving && this.activeJobs != null)
			{
				this.activeJobs.RemoveAll(x => x == null);
			}
			Scribe_TargetInfo.Look(ref targetDest, "targetDest");
			Scribe_Collections.Look(ref this.activeJobs, "activeJobs", LookMode.Deep);
			Scribe_References.Look(ref this.mainJob, "mainJob");
			Scribe_Values.Look(ref searchingJobsNow, "searchingJobsNow", false);
			Scribe_References.Look(ref oldMap, "oldMap");
			Scribe_Values.Look(ref forceGoToDestMap, "forceGoToDestMap", false);
			Scribe_References.Look(ref target, "target");
			Scribe_References.Look(ref mapDest, "mapDest");
			Scribe_Values.Look(ref lookedAtLocalCellMap, "lookedAtLocalCell");
			Scribe_Values.Look(ref forceGoToDestMap, "failIfTargetMapIsNotDest", false);
			Scribe_Collections.Look(ref reservedThings, "reservedThings", LookMode.LocalTargetInfo);
			Scribe_Collections.Look(ref reservedCells, "reservedCells", LookMode.LocalTargetInfo);
			Scribe_Collections.Look(ref lookedAtLocalCellMap, "lookedAtLocalCellMap", LookMode.Value, LookMode.Reference, ref intVec3Keys, ref mapValues);
		}

		public bool searchingJobsNow = false;
		public Map oldMap;
		public Job mainJob;
		public bool forceGoToDestMap;
		public Thing target;
		public bool failIfTargetMapIsNotDest;
		public List<Job> activeJobs;
		public List<LocalTargetInfo> reservedThings;
		public List<LocalTargetInfo> reservedCells;
		public TargetInfo targetDest;
		public Map mapDest;
		public Dictionary<IntVec3, Map> lookedAtLocalCellMap;
		private List<IntVec3> intVec3Keys;
		private List<Map> mapValues;
	}
}

