using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace ZLevels
{
	public class JobDriver_ZHaulToCell : JobDriver
	{
		private bool forbiddenInitially;

		private const TargetIndex HaulableInd = TargetIndex.A;

		private const TargetIndex StoreCellInd = TargetIndex.B;

		private Thing savedThing = null;
		public override void ExposeData()
		{
			Log.Message("JobDriver_ZHaulToCell : JobDriver - ExposeData - base.ExposeData(); - 1", true);
			base.ExposeData();
			Log.Message("JobDriver_ZHaulToCell : JobDriver - ExposeData - Scribe_Values.Look(ref forbiddenInitially, \"forbiddenInitially\", defaultValue: false); - 2", true);
			Scribe_Values.Look(ref forbiddenInitially, "forbiddenInitially", defaultValue: false);
		}

		public override string GetReport()
		{
			Log.Message("JobDriver_ZHaulToCell : JobDriver - GetReport - IntVec3 cell = job.targetB.Cell; - 3", true);
			IntVec3 cell = job.targetB.Cell;
			Log.Message("JobDriver_ZHaulToCell : JobDriver - GetReport - Thing thing = null; - 4", true);
			Thing thing = null;
			Log.Message("JobDriver_ZHaulToCell : JobDriver - GetReport - if (pawn.CurJob == job && pawn.carryTracker.CarriedThing != null) - 5", true);
			if (pawn.CurJob == job && pawn.carryTracker.CarriedThing != null)
			{
				Log.Message("JobDriver_ZHaulToCell : JobDriver - GetReport - thing = pawn.carryTracker.CarriedThing; - 6", true);
				thing = pawn.carryTracker.CarriedThing;
			}
			else if (base.TargetThingA != null && base.TargetThingA.Spawned)
			{
				Log.Message("JobDriver_ZHaulToCell : JobDriver - GetReport - thing = base.TargetThingA; - 8", true);
				thing = base.TargetThingA;
			}
			Log.Message("JobDriver_ZHaulToCell : JobDriver - GetReport - if (thing == null) - 9", true);
			if (thing == null)
			{
				Log.Message("JobDriver_ZHaulToCell : JobDriver - GetReport - return \"ReportHaulingUnknown\".Translate(); - 10", true);
				return "ReportHaulingUnknown".Translate();
			}
			string text = null;
			Log.Message("JobDriver_ZHaulToCell : JobDriver - GetReport - SlotGroup slotGroup = cell.GetSlotGroup(base.Map); - 12", true);
			SlotGroup slotGroup = cell.GetSlotGroup(base.Map);
			Log.Message("JobDriver_ZHaulToCell : JobDriver - GetReport - if (slotGroup != null) - 13", true);
			if (slotGroup != null)
			{
				Log.Message("JobDriver_ZHaulToCell : JobDriver - GetReport - text = slotGroup.parent.SlotYielderLabel(); - 14", true);
				text = slotGroup.parent.SlotYielderLabel();
			}
			Log.Message("JobDriver_ZHaulToCell : JobDriver - GetReport - if (text != null) - 15", true);
			if (text != null)
			{
				Log.Message("JobDriver_ZHaulToCell : JobDriver - GetReport - return \"ReportHaulingTo\".Translate(thing.Label, text.Named(\"DESTINATION\"), thing.Named(\"THING\")); - 16", true);
				return "ReportHaulingTo".Translate(thing.Label, text.Named("DESTINATION"), thing.Named("THING"));
			}
			return "ReportHauling".Translate(thing.Label, thing);
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			Log.Message("JobDriver_ZHaulToCell : JobDriver - TryMakePreToilReservations - if (pawn.Reserve(job.GetTarget(TargetIndex.B), job, 1, -1, null, errorOnFailed)) - 18", true);
			if (pawn.Reserve(job.GetTarget(TargetIndex.B), job, 1, -1, null, errorOnFailed))
			{
				Log.Message("JobDriver_ZHaulToCell : JobDriver - TryMakePreToilReservations - return pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed); - 19", true);
				return pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed);
			}
			return false;
		}

		public override void Notify_Starting()
		{
			Log.Message("JobDriver_ZHaulToCell : JobDriver - Notify_Starting - base.Notify_Starting(); - 21", true);
			base.Notify_Starting();
			Log.Message("JobDriver_ZHaulToCell : JobDriver - Notify_Starting - if (base.TargetThingA != null) - 22", true);
			if (base.TargetThingA != null)
			{
				Log.Message("JobDriver_ZHaulToCell : JobDriver - Notify_Starting - forbiddenInitially = base.TargetThingA.IsForbidden(pawn); - 23", true);
				forbiddenInitially = base.TargetThingA.IsForbidden(pawn);
			}
			else
			{
				Log.Message("JobDriver_ZHaulToCell : JobDriver - Notify_Starting - forbiddenInitially = false; - 24", true);
				forbiddenInitially = false;
			}
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - this.FailOnDestroyedOrNull(TargetIndex.A); - 25", true);
			this.FailOnDestroyedOrNull(TargetIndex.A);
			Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - this.FailOnBurningImmobile(TargetIndex.B); - 26", true);
			this.FailOnBurningImmobile(TargetIndex.B);
			Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - if (!forbiddenInitially) - 27", true);
			if (!forbiddenInitially)
			{
				Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - this.FailOnForbidden(TargetIndex.A); - 28", true);
				this.FailOnForbidden(TargetIndex.A);
			}
			yield return new Toil
			{
				initAction = delegate ()
				{
					Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"200 TargetA.Thing: \" + TargetA.Thing); - 29", true);
					ZLogger.Message("200 TargetA.Thing: " + TargetA.Thing);
					Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"200 TargetA.Thing.Position: \" + TargetA.Thing?.Position); - 30", true);
					ZLogger.Message("200 TargetA.Thing.Position: " + TargetA.Thing?.Position);
					Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"200 TargetA.Thing.Map: \" + TargetA.Thing?.Map); - 31", true);
					ZLogger.Message("200 TargetA.Thing.Map: " + TargetA.Thing?.Map);
					Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"200 TargetA.Thing.stackCount: \" + TargetA.Thing?.stackCount); - 32", true);
					ZLogger.Message("200 TargetA.Thing.stackCount: " + TargetA.Thing?.stackCount);
					Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - var ZTracker = Current.Game.GetComponent<ZLevelsManager>(); - 33", true);
					var ZTracker = Current.Game.GetComponent<ZLevelsManager>();

					for (int i = ZTracker.jobTracker[pawn].mainJob.targetQueueB.Count - 1; i >= 0; i--)
					{
						Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - var target = ZTracker.jobTracker[pawn].mainJob.targetQueueB[i]; - 34", true);
						var target = ZTracker.jobTracker[pawn].mainJob.targetQueueB[i];

						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB: " + target.Thing);
						Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"JobDriver_ZHaulToCell job.targetQueueB.Map: \" + target.Thing.Map); - 36", true);
						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB.Map: " + target.Thing.Map);
						Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"JobDriver_ZHaulToCell job.targetQueueB.stackCount: \" + target.Thing.stackCount); - 37", true);
						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB.stackCount: " + target.Thing.stackCount);
						Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"JobDriver_ZHaulToCell job.targetQueueB.countQueue: \" + ZTracker.jobTracker[pawn].mainJob.countQueue[i]); - 38", true);
						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB.countQueue: " + ZTracker.jobTracker[pawn].mainJob.countQueue[i]);
					}
				}
			};

			Toil reserveTargetA = Toils_Reserve.Reserve(TargetIndex.A);
			yield return new Toil { initAction = delegate () { Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - yield return reserveTargetA; - 41", true); } };
			yield return reserveTargetA;
			Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - Toil toilGoto = null; - 42", true);
			Toil toilGoto = null;
			toilGoto = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnSomeonePhysicallyInteracting(TargetIndex.A).FailOn((Func<bool>)delegate
			{
				Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - Pawn actor = toilGoto.actor; - 43", true);
				Pawn actor = toilGoto.actor;
				Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - Job curJob = actor.jobs.curJob; - 44", true);
				Job curJob = actor.jobs.curJob;
				Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - if (curJob.haulMode == HaulMode.ToCellStorage) - 45", true);
				if (curJob.haulMode == HaulMode.ToCellStorage)
				{
					Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - Thing thing = curJob.GetTarget(TargetIndex.A).Thing; - 46", true);
					Thing thing = curJob.GetTarget(TargetIndex.A).Thing;
					Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - if (!actor.jobs.curJob.GetTarget(TargetIndex.B).Cell.IsValidStorageFor(base.Map, thing)) - 47", true);
					if (!actor.jobs.curJob.GetTarget(TargetIndex.B).Cell.IsValidStorageFor(base.Map, thing))
					{
						Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - return true; - 48", true);
						return true;
					}
				}
				return false;
			Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - }); - 50", true);
			});
			yield return new Toil { initAction = delegate () { Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - yield return toilGoto; - 51", true); } };
			yield return toilGoto;
			yield return new Toil
			{
				initAction = delegate ()
				{
					Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"200.3 TargetA.Thing: \" + TargetA.Thing); - 52", true);
					ZLogger.Message("200.3 TargetA.Thing: " + TargetA.Thing);
					Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"200.3 TargetA.Thing.Position: \" + TargetA.Thing?.Position); - 53", true);
					ZLogger.Message("200.3 TargetA.Thing.Position: " + TargetA.Thing?.Position);
					Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"200.3 TargetA.Thing.Map: \" + TargetA.Thing?.Map); - 54", true);
					ZLogger.Message("200.3 TargetA.Thing.Map: " + TargetA.Thing?.Map);
					Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"200.3 TargetA.Thing.stackCount: \" + TargetA.Thing?.stackCount); - 55", true);
					ZLogger.Message("200.3 TargetA.Thing.stackCount: " + TargetA.Thing?.stackCount);
					Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - this.savedThing = TargetA.Thing; - 56", true);
					this.savedThing = TargetA.Thing;
					Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - var ZTracker = Current.Game.GetComponent<ZLevelsManager>(); - 57", true);
					var ZTracker = Current.Game.GetComponent<ZLevelsManager>();

					for (int i = ZTracker.jobTracker[pawn].mainJob.targetQueueB.Count - 1; i >= 0; i--)
					{
						Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - var target = ZTracker.jobTracker[pawn].mainJob.targetQueueB[i]; - 58", true);
						var target = ZTracker.jobTracker[pawn].mainJob.targetQueueB[i];

						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB: " + target.Thing);
						Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"JobDriver_ZHaulToCell job.targetQueueB.Map: \" + target.Thing.Map); - 60", true);
						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB.Map: " + target.Thing.Map);
						Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"JobDriver_ZHaulToCell job.targetQueueB.stackCount: \" + target.Thing.stackCount); - 61", true);
						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB.stackCount: " + target.Thing.stackCount);
						Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"JobDriver_ZHaulToCell job.targetQueueB.countQueue: \" + ZTracker.jobTracker[pawn].mainJob.countQueue[i]); - 62", true);
						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB.countQueue: " + ZTracker.jobTracker[pawn].mainJob.countQueue[i]);
					}
				}
			};

			yield return Toils_Haul.StartCarryThing(TargetIndex.A, putRemainderInQueue: false, subtractNumTakenFromJobCount: true);
			Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - if (job.haulOpportunisticDuplicates) - 65", true);
			if (job.haulOpportunisticDuplicates)
			{
				yield return new Toil { initAction = delegate () { Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveTargetA, TargetIndex.A, TargetIndex.B); - 66", true); } };
				yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveTargetA, TargetIndex.A, TargetIndex.B);
			}
			Toil carryToCell = Toils_Haul.CarryHauledThingToCell(TargetIndex.B);
			yield return new Toil { initAction = delegate () { Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - yield return carryToCell; - 68", true); } };
			yield return carryToCell;
			yield return new Toil
			{
				initAction = delegate ()
				{
					Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"10 savedThing: \" + this.savedThing); - 69", true);
					ZLogger.Message("10 savedThing: " + this.savedThing);
					Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"10 carried Thing: \" + pawn.carryTracker?.CarriedThing); - 70", true);
					ZLogger.Message("10 carried Thing: " + pawn.carryTracker?.CarriedThing);
					Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"200.5 TargetA.Thing: \" + TargetA.Thing); - 71", true);
					ZLogger.Message("200.5 TargetA.Thing: " + TargetA.Thing);
					Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"200.5 TargetA.Thing.Position: \" + TargetA.Thing?.Position); - 72", true);
					ZLogger.Message("200.5 TargetA.Thing.Position: " + TargetA.Thing?.Position);
					Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"200.5 TargetA.Thing.Map: \" + TargetA.Thing?.Map); - 73", true);
					ZLogger.Message("200.5 TargetA.Thing.Map: " + TargetA.Thing?.Map);
					Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"200.5 TargetA.Thing.stackCount: \" + TargetA.Thing?.stackCount); - 74", true);
					ZLogger.Message("200.5 TargetA.Thing.stackCount: " + TargetA.Thing?.stackCount);
					Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - var ZTracker = Current.Game.GetComponent<ZLevelsManager>(); - 75", true);
					var ZTracker = Current.Game.GetComponent<ZLevelsManager>();

					for (int i = ZTracker.jobTracker[pawn].mainJob.targetQueueB.Count - 1; i >= 0; i--)
					{
						Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - var target = ZTracker.jobTracker[pawn].mainJob.targetQueueB[i]; - 76", true);
						var target = ZTracker.jobTracker[pawn].mainJob.targetQueueB[i];

						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB: " + target.Thing);
						Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"JobDriver_ZHaulToCell job.targetQueueB.Map: \" + target.Thing.Map); - 78", true);
						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB.Map: " + target.Thing.Map);
						Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"JobDriver_ZHaulToCell job.targetQueueB.stackCount: \" + target.Thing.stackCount); - 79", true);
						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB.stackCount: " + target.Thing.stackCount);
						Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"JobDriver_ZHaulToCell job.targetQueueB.countQueue: \" + ZTracker.jobTracker[pawn].mainJob.countQueue[i]); - 80", true);
						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB.countQueue: " + ZTracker.jobTracker[pawn].mainJob.countQueue[i]);
					}
				}
			};

			yield return new Toil
			{
				initAction = delegate ()
				{
					Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"Pawn: \" + pawn); - 82", true);
					ZLogger.Message("Pawn: " + pawn);
					Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"pawn.Map: \" + pawn.Map); - 83", true);
					ZLogger.Message("pawn.Map: " + pawn.Map);
					Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"TargetB: \" + TargetB); - 84", true);
					ZLogger.Message("TargetB: " + TargetB);
				}
			};
			yield return new Toil
			{
				initAction = delegate ()
				{
					Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - if (TargetB.Cell.GetFirstItem(pawn.Map) != null) - 86", true);
					if (TargetB.Cell.GetFirstItem(pawn.Map) != null)
					{
						Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - IntVec3 newPosition = IntVec3.Invalid; - 87", true);
						IntVec3 newPosition = IntVec3.Invalid;

						IntVec3 center = (from x in GenRadial.RadialCellsAround(pawn.Position, 3f, useCenter: true)
										  where x.InBounds(pawn.Map) && x.GetFirstItem(pawn.Map) == null select x).FirstOrDefault();
						Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - if (center != null) - 89", true);
						if (center != null)
						{
							Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - job.targetB = new LocalTargetInfo(center); - 90", true);
							job.targetB = new LocalTargetInfo(center);
						}

						else if (CellFinder.TryFindRandomCellNear(TargetB.Cell, pawn.Map, 3,
						    (IntVec3 c) => c.GetFirstItem(pawn.Map)?.def != TargetA.Thing.def, out newPosition))
						{
							Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - job.targetB = new LocalTargetInfo(newPosition); - 92", true);
							job.targetB = new LocalTargetInfo(newPosition);
						}
					}
				}
			};

			yield return new Toil
			{
				initAction = delegate ()
				{
					Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"2 Pawn: \" + pawn); - 94", true);
					ZLogger.Message("2 Pawn: " + pawn);
					Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"2 pawn.Map: \" + pawn.Map); - 95", true);
					ZLogger.Message("2 pawn.Map: " + pawn.Map);
					Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"2 TargetB: \" + TargetB); - 96", true);
					ZLogger.Message("2 TargetB: " + TargetB);
				}
			};

			yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.B, carryToCell, false);

			yield return new Toil
			{
				initAction = delegate ()
				{
					Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - var ZTracker = Current.Game.GetComponent<ZLevelsManager>(); - 99", true);
					var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
					Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"201 TargetA.Thing: \" + TargetA.Thing); - 100", true);
					ZLogger.Message("201 TargetA.Thing: " + TargetA.Thing);
					Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"201 TargetA.Thing.Position: \" + TargetA.Thing?.Position); - 101", true);
					ZLogger.Message("201 TargetA.Thing.Position: " + TargetA.Thing?.Position);
					Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"201 TargetA.Thing.Map: \" + TargetA.Thing?.Map); - 102", true);
					ZLogger.Message("201 TargetA.Thing.Map: " + TargetA.Thing?.Map);
					Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"201 TargetA.Thing.stackCount: \" + TargetA.Thing?.stackCount); - 103", true);
					ZLogger.Message("201 TargetA.Thing.stackCount: " + TargetA.Thing?.stackCount);
					for (int i = ZTracker.jobTracker[pawn].mainJob.targetQueueB.Count - 1; i >= 0; i--)
					{
						Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - var target = ZTracker.jobTracker[pawn].mainJob.targetQueueB[i]; - 104", true);
						var target = ZTracker.jobTracker[pawn].mainJob.targetQueueB[i];

						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB: " + target.Thing);
						Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"JobDriver_ZHaulToCell job.targetQueueB.Map: \" + target.Thing.Map); - 106", true);
						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB.Map: " + target.Thing.Map);
						Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"JobDriver_ZHaulToCell job.targetQueueB.stackCount: \" + target.Thing.stackCount); - 107", true);
						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB.stackCount: " + target.Thing.stackCount);
						Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"JobDriver_ZHaulToCell job.targetQueueB.countQueue: \" + ZTracker.jobTracker[pawn].mainJob.countQueue[i]); - 108", true);
						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB.countQueue: " + ZTracker.jobTracker[pawn].mainJob.countQueue[i]);
					}
					Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - if (ZTracker.jobTracker.ContainsKey(this.pawn) && ZTracker.jobTracker[this.pawn].mainJob != null) - 109", true);
					if (ZTracker.jobTracker.ContainsKey(this.pawn) && ZTracker.jobTracker[this.pawn].mainJob != null)
					{
						Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - if (ZTracker.jobTracker[this.pawn].mainJob.targetQueueB != null - 110", true);
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

					ZLogger.Message("202 TargetA.Thing: " + TargetA.Thing);
					Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"202 TargetA.Thing.Position: \" + TargetA.Thing?.Position); - 120", true);
					ZLogger.Message("202 TargetA.Thing.Position: " + TargetA.Thing?.Position);
					Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"202 TargetA.Thing.Map: \" + TargetA.Thing?.Map); - 121", true);
					ZLogger.Message("202 TargetA.Thing.Map: " + TargetA.Thing?.Map);
					Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"202 TargetA.Thing.stackCount: \" + TargetA.Thing?.stackCount); - 122", true);
					ZLogger.Message("202 TargetA.Thing.stackCount: " + TargetA.Thing?.stackCount);
					for (int i = ZTracker.jobTracker[pawn].mainJob.targetQueueB.Count - 1; i >= 0; i--)
					{
						Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - var target = ZTracker.jobTracker[pawn].mainJob.targetQueueB[i]; - 123", true);
						var target = ZTracker.jobTracker[pawn].mainJob.targetQueueB[i];

						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB: " + target.Thing);
						Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"JobDriver_ZHaulToCell job.targetQueueB.Map: \" + target.Thing.Map); - 125", true);
						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB.Map: " + target.Thing.Map);
						Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"JobDriver_ZHaulToCell job.targetQueueB.stackCount: \" + target.Thing.stackCount); - 126", true);
						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB.stackCount: " + target.Thing.stackCount);
						Log.Message("JobDriver_ZHaulToCell : JobDriver - MakeNewToils - ZLogger.Message(\"JobDriver_ZHaulToCell job.targetQueueB.countQueue: \" + ZTracker.jobTracker[pawn].mainJob.countQueue[i]); - 127", true);
						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB.countQueue: " + ZTracker.jobTracker[pawn].mainJob.countQueue[i]);
					}
				}
			};
		}
	}
}

