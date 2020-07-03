using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace ZLevels
{
	public class JobDriver_HaulThingToDestAndToCell : JobDriver
	{
		private bool forbiddenInitially;

		private const TargetIndex HaulableInd = TargetIndex.A;

		private const TargetIndex StoreCellInd = TargetIndex.B;

		private Thing savedThing = null;
		public override void ExposeData()
		{
			Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - ExposeData - base.ExposeData(); - 1", true);
			base.ExposeData();
			Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - ExposeData - Scribe_Values.Look(ref forbiddenInitially, \"forbiddenInitially\", defaultValue: false); - 2", true);
			Scribe_Values.Look(ref forbiddenInitially, "forbiddenInitially", defaultValue: false);
		}

		public override string GetReport()
		{
			Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - GetReport - IntVec3 cell = job.targetB.Cell; - 3", true);
			IntVec3 cell = job.targetB.Cell;
			Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - GetReport - Thing thing = null; - 4", true);
			Thing thing = null;
			Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - GetReport - if (pawn.CurJob == job && pawn.carryTracker.CarriedThing != null) - 5", true);
			if (pawn.CurJob == job && pawn.carryTracker.CarriedThing != null)
			{
				Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - GetReport - thing = pawn.carryTracker.CarriedThing; - 6", true);
				thing = pawn.carryTracker.CarriedThing;
			}
			else if (base.TargetThingA != null && base.TargetThingA.Spawned)
			{
				Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - GetReport - thing = base.TargetThingA; - 8", true);
				thing = base.TargetThingA;
			}
			Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - GetReport - if (thing == null) - 9", true);
			if (thing == null)
			{
				Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - GetReport - return \"ReportHaulingUnknown\".Translate(); - 10", true);
				return "ReportHaulingUnknown".Translate();
			}
			string text = null;
			Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - GetReport - SlotGroup slotGroup = cell.GetSlotGroup(base.Map); - 12", true);
			SlotGroup slotGroup = cell.GetSlotGroup(base.Map);
			Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - GetReport - if (slotGroup != null) - 13", true);
			if (slotGroup != null)
			{
				Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - GetReport - text = slotGroup.parent.SlotYielderLabel(); - 14", true);
				text = slotGroup.parent.SlotYielderLabel();
			}
			Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - GetReport - if (text != null) - 15", true);
			if (text != null)
			{
				Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - GetReport - return \"ReportHaulingTo\".Translate(thing.Label, text.Named(\"DESTINATION\"), thing.Named(\"THING\")); - 16", true);
				return "ReportHaulingTo".Translate(thing.Label, text.Named("DESTINATION"), thing.Named("THING"));
			}
			return "ReportHauling".Translate(thing.Label, thing);
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - TryMakePreToilReservations - pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed); - 18", true);
			pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed);
			Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - TryMakePreToilReservations - pawn.Reserve(job.GetTarget(TargetIndex.B), job, 1, -1, null, errorOnFailed); - 19", true);
			pawn.Reserve(job.GetTarget(TargetIndex.B), job, 1, -1, null, errorOnFailed);
			Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - TryMakePreToilReservations - return true; - 20", true);
			return true;
		}

		public override void Notify_Starting()
		{
			Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - Notify_Starting - base.Notify_Starting(); - 21", true);
			base.Notify_Starting();
			Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - Notify_Starting - if (base.TargetThingA != null) - 22", true);
			if (base.TargetThingA != null)
			{
				Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - Notify_Starting - forbiddenInitially = base.TargetThingA.IsForbidden(pawn); - 23", true);
				forbiddenInitially = base.TargetThingA.IsForbidden(pawn);
			}
			else
			{
				Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - Notify_Starting - forbiddenInitially = false; - 24", true);
				forbiddenInitially = false;
			}
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - this.FailOnDestroyedOrNull(TargetIndex.A); - 25", true);
			this.FailOnDestroyedOrNull(TargetIndex.A);
			Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - this.FailOnBurningImmobile(TargetIndex.B); - 26", true);
			this.FailOnBurningImmobile(TargetIndex.B);
			Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - if (!forbiddenInitially) - 27", true);
			if (!forbiddenInitially)
			{
				Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - this.FailOnForbidden(TargetIndex.A); - 28", true);
				this.FailOnForbidden(TargetIndex.A);
			}

			var ZTracker = ZUtils.ZTracker;
			Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - if (pawn.Map == this.job.targetA.Thing.Map && pawn.Map == ZTracker.jobTracker[pawn].dest) - 30", true);
			if (pawn.Map == this.job.targetA.Thing.Map && pawn.Map == ZTracker.jobTracker[pawn].dest)
			{
				Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - ZLogger.Message(\"pawn map and thing map and dest map are same, yield breaking in JobDriver_HaulThingToDest\"); - 31", true);
				ZLogger.Message("pawn map and thing map and dest map are same, yield breaking in JobDriver_HaulThingToDest");
				Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - yield break; - 32", true);
				yield break;
			}
			Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - foreach (var toil in Toils_ZLevels.GoToMap(GetActor(), TargetA.Thing.Map, this)) - 33", true);
			foreach (var toil in Toils_ZLevels.GoToMap(GetActor(), TargetA.Thing.Map, this))
			{
				yield return new Toil { initAction = delegate () { Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - yield return toil; - 34", true); } };
				yield return toil;
			}

			Toil reserveTargetA = Toils_Reserve.Reserve(TargetIndex.A);
			yield return new Toil { initAction = delegate () { Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - yield return reserveTargetA; - 36", true); } };
			yield return reserveTargetA;
			Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - Toil toilGoto = null; - 37", true);
			Toil toilGoto = null;
			toilGoto = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnSomeonePhysicallyInteracting(TargetIndex.A).FailOn((Func<bool>)delegate
			{
				Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - Pawn actor = toilGoto.actor; - 38", true);
				Pawn actor = toilGoto.actor;
				Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - Job curJob = actor.jobs.curJob; - 39", true);
				Job curJob = actor.jobs.curJob;
				Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - if (curJob.haulMode == HaulMode.ToCellStorage) - 40", true);
				if (curJob.haulMode == HaulMode.ToCellStorage)
				{
					Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - Thing thing = curJob.GetTarget(TargetIndex.A).Thing; - 41", true);
					Thing thing = curJob.GetTarget(TargetIndex.A).Thing;
					Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - if (!actor.jobs.curJob.GetTarget(TargetIndex.B).Cell.IsValidStorageFor(base.Map, thing)) - 42", true);
					if (!actor.jobs.curJob.GetTarget(TargetIndex.B).Cell.IsValidStorageFor(base.Map, thing))
					{
						Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - return true; - 43", true);
						return true;
					}
				}
				return false;
			Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - }); - 45", true);
			});
			
			yield return toilGoto;
			yield return new Toil
			{
				initAction = delegate ()
				{
					Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - this.savedThing = TargetA.Thing; - 47", true);
					this.savedThing = TargetA.Thing;
				}
			};

			yield return Toils_Haul.StartCarryThing(TargetIndex.A, putRemainderInQueue: false, subtractNumTakenFromJobCount: true);
			Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - if (job.haulOpportunisticDuplicates) - 50", true);
			if (job.haulOpportunisticDuplicates)
			{
				yield return new Toil { initAction = delegate () { Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveTargetA, TargetIndex.A, TargetIndex.B); - 51", true); } };
				yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveTargetA, TargetIndex.A, TargetIndex.B);
			}
			Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - foreach (var toil in Toils_ZLevels.GoToMap(GetActor(), ZTracker.jobTracker[pawn].dest, this)) - 52", true);
			foreach (var toil in Toils_ZLevels.GoToMap(GetActor(), ZTracker.jobTracker[pawn].dest, this))
			{
				yield return new Toil { initAction = delegate () { Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - yield return toil; - 53", true); } };
				yield return toil;
			}

			Toil carryToCell = Toils_Haul.CarryHauledThingToCell(TargetIndex.B);
			yield return new Toil { initAction = delegate () { Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - yield return carryToCell; - 55", true); } };
			yield return carryToCell;
			yield return new Toil
			{
				initAction = delegate ()
				{
					Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - ZLogger.Message(\"10 savedThing: \" + this.savedThing); - 56", true);
					ZLogger.Message("10 savedThing: " + this.savedThing);
					Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - ZLogger.Message(\"10 carried Thing: \" + pawn.carryTracker?.CarriedThing); - 57", true);
					ZLogger.Message("10 carried Thing: " + pawn.carryTracker?.CarriedThing);
					Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - ZLogger.Message(\"200.5 TargetA.Thing: \" + TargetA.Thing); - 58", true);
					ZLogger.Message("200.5 TargetA.Thing: " + TargetA.Thing);
					Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - ZLogger.Message(\"200.5 TargetA.Thing.Position: \" + TargetA.Thing?.Position); - 59", true);
					ZLogger.Message("200.5 TargetA.Thing.Position: " + TargetA.Thing?.Position);
					Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - ZLogger.Message(\"200.5 TargetA.Thing.Map: \" + TargetA.Thing?.Map); - 60", true);
					ZLogger.Message("200.5 TargetA.Thing.Map: " + TargetA.Thing?.Map);
					Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - ZLogger.Message(\"200.5 TargetA.Thing.stackCount: \" + TargetA.Thing?.stackCount); - 61", true);
					ZLogger.Message("200.5 TargetA.Thing.stackCount: " + TargetA.Thing?.stackCount);
					for (int i = ZTracker.jobTracker[pawn].mainJob.targetQueueB.Count - 1; i >= 0; i--)
					{
						Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - var target = ZTracker.jobTracker[pawn].mainJob.targetQueueB[i]; - 62", true);
						var target = ZTracker.jobTracker[pawn].mainJob.targetQueueB[i];

						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB: " + target.Thing);
						Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - ZLogger.Message(\"JobDriver_ZHaulToCell job.targetQueueB.Map: \" + target.Thing.Map); - 64", true);
						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB.Map: " + target.Thing.Map);
						Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - ZLogger.Message(\"JobDriver_ZHaulToCell job.targetQueueB.stackCount: \" + target.Thing.stackCount); - 65", true);
						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB.stackCount: " + target.Thing.stackCount);
						Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - ZLogger.Message(\"JobDriver_ZHaulToCell job.targetQueueB.countQueue: \" + ZTracker.jobTracker[pawn].mainJob.countQueue[i]); - 66", true);
						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB.countQueue: " + ZTracker.jobTracker[pawn].mainJob.countQueue[i]);
					}
				}
			};

			yield return new Toil
			{
				initAction = delegate ()
				{
					Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - if (TargetB.Cell.GetFirstItem(pawn.Map) != null) - 68", true);
					if (TargetB.Cell.GetFirstItem(pawn.Map) != null)
					{
						Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - IntVec3 newPosition = IntVec3.Invalid; - 69", true);
						IntVec3 newPosition = IntVec3.Invalid;

						IntVec3 center = (from x in GenRadial.RadialCellsAround(pawn.Position, 3f, useCenter: true)
										  where x.InBounds(pawn.Map) && x.GetFirstItem(pawn.Map) == null select x).FirstOrDefault();
						Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - if (center != null) - 71", true);
						if (center != null)
						{
							Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - job.targetB = new LocalTargetInfo(center); - 72", true);
							job.targetB = new LocalTargetInfo(center);
						}
						else if (CellFinder.TryFindRandomCellNear(TargetB.Cell, pawn.Map, 3,
						    (IntVec3 c) => c.GetFirstItem(pawn.Map)?.def != TargetA.Thing.def, out newPosition))
						{
							Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - job.targetB = new LocalTargetInfo(newPosition); - 74", true);
							job.targetB = new LocalTargetInfo(newPosition);
						}
					}
				}
			};

			yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.B, carryToCell, false);

			yield return new Toil
			{
				initAction = delegate ()
				{
					Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - ZLogger.Message(\"201 TargetA.Thing: \" + TargetA.Thing); - 77", true);
					ZLogger.Message("201 TargetA.Thing: " + TargetA.Thing);
					Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - ZLogger.Message(\"201 TargetA.Thing.Position: \" + TargetA.Thing?.Position); - 78", true);
					ZLogger.Message("201 TargetA.Thing.Position: " + TargetA.Thing?.Position);
					Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - ZLogger.Message(\"201 TargetA.Thing.Map: \" + TargetA.Thing?.Map); - 79", true);
					ZLogger.Message("201 TargetA.Thing.Map: " + TargetA.Thing?.Map);
					Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - ZLogger.Message(\"201 TargetA.Thing.stackCount: \" + TargetA.Thing?.stackCount); - 80", true);
					ZLogger.Message("201 TargetA.Thing.stackCount: " + TargetA.Thing?.stackCount);
					for (int i = ZTracker.jobTracker[pawn].mainJob.targetQueueB.Count - 1; i >= 0; i--)
					{
						Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - var target = ZTracker.jobTracker[pawn].mainJob.targetQueueB[i]; - 81", true);
						var target = ZTracker.jobTracker[pawn].mainJob.targetQueueB[i];

						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB: " + target.Thing);
						Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - ZLogger.Message(\"JobDriver_ZHaulToCell job.targetQueueB.Map: \" + target.Thing.Map); - 83", true);
						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB.Map: " + target.Thing.Map);
						Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - ZLogger.Message(\"JobDriver_ZHaulToCell job.targetQueueB.stackCount: \" + target.Thing.stackCount); - 84", true);
						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB.stackCount: " + target.Thing.stackCount);
						Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - ZLogger.Message(\"JobDriver_ZHaulToCell job.targetQueueB.countQueue: \" + ZTracker.jobTracker[pawn].mainJob.countQueue[i]); - 85", true);
						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB.countQueue: " + ZTracker.jobTracker[pawn].mainJob.countQueue[i]);
					}
					Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - if (ZTracker.jobTracker.ContainsKey(this.pawn) && ZTracker.jobTracker[this.pawn].mainJob != null) - 86", true);
					if (ZTracker.jobTracker.ContainsKey(this.pawn) && ZTracker.jobTracker[this.pawn].mainJob != null)
					{
						Log.Message("JobDriver_HaulThingToDestAndToCell : JobDriver - MakeNewToils - if (ZTracker.jobTracker[this.pawn].mainJob.targetQueueB != null - 87", true);
						if (ZTracker.jobTracker[this.pawn].mainJob.targetQueueB != null 
						&& ZTracker.jobTracker[this.pawn].mainJob.targetQueueB
						.Where(x => x.Thing == TargetA.Thing).Count() == 0)
						{
							//var newTarget = new LocalTargetInfo(TargetA.Thing);
							//ZTracker.jobTracker[this.pawn].mainJob.targetQueueB.Add(newTarget);
							//ZTracker.jobTracker[this.pawn].mainJob.countQueue.Add(newTarget.Thing.stackCount);
							//ZLogger.Message("1 Adding " + newTarget + " to " + ZTracker.jobTracker[this.pawn].mainJob);
							//int ind = ZTracker.jobTracker[this.pawn].mainJob.targetQueueB.FirstIndexOf(x => x.Thing == this.savedThing);
							//ZLogger.Message("1 Removing " + ZTracker.jobTracker[this.pawn].mainJob.targetQueueB[ind] + " from " + ZTracker.jobTracker[this.pawn].mainJob);
							//
							//ZTracker.jobTracker[this.pawn].mainJob.targetQueueB.RemoveAt(ind);
							//ZTracker.jobTracker[this.pawn].mainJob.countQueue.RemoveAt(ind);
						}
					}
				}
			};
		}
	}
}

