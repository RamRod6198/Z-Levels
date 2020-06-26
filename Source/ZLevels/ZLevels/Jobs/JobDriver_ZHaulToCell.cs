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
			base.ExposeData();
			Scribe_Values.Look(ref forbiddenInitially, "forbiddenInitially", defaultValue: false);
		}

		public override string GetReport()
		{
			IntVec3 cell = job.targetB.Cell;
			Thing thing = null;
			if (pawn.CurJob == job && pawn.carryTracker.CarriedThing != null)
			{
				thing = pawn.carryTracker.CarriedThing;
			}
			else if (base.TargetThingA != null && base.TargetThingA.Spawned)
			{
				thing = base.TargetThingA;
			}
			if (thing == null)
			{
				return "ReportHaulingUnknown".Translate();
			}
			string text = null;
			SlotGroup slotGroup = cell.GetSlotGroup(base.Map);
			if (slotGroup != null)
			{
				text = slotGroup.parent.SlotYielderLabel();
			}
			if (text != null)
			{
				return "ReportHaulingTo".Translate(thing.Label, text.Named("DESTINATION"), thing.Named("THING"));
			}
			return "ReportHauling".Translate(thing.Label, thing);
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			if (pawn.Reserve(job.GetTarget(TargetIndex.B), job, 1, -1, null, errorOnFailed))
			{
				return pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed);
			}
			return false;
		}

		public override void Notify_Starting()
		{
			base.Notify_Starting();
			if (base.TargetThingA != null)
			{
				forbiddenInitially = base.TargetThingA.IsForbidden(pawn);
			}
			else
			{
				forbiddenInitially = false;
			}
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDestroyedOrNull(TargetIndex.A);
			this.FailOnBurningImmobile(TargetIndex.B);
			if (!forbiddenInitially)
			{
				this.FailOnForbidden(TargetIndex.A);
			}
			yield return new Toil
			{
				initAction = delegate ()
				{
					ZLogger.Message("200 TargetA.Thing: " + TargetA.Thing);
					ZLogger.Message("200 TargetA.Thing.Position: " + TargetA.Thing?.Position);
					ZLogger.Message("200 TargetA.Thing.Map: " + TargetA.Thing?.Map);
					ZLogger.Message("200 TargetA.Thing.stackCount: " + TargetA.Thing?.stackCount);
					var ZTracker = Current.Game.GetComponent<ZLevelsManager>();

					for (int i = ZTracker.jobTracker[pawn].mainJob.targetQueueB.Count - 1; i >= 0; i--)
					{
						var target = ZTracker.jobTracker[pawn].mainJob.targetQueueB[i];

						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB: " + target.Thing);
						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB.Map: " + target.Thing.Map);
						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB.stackCount: " + target.Thing.stackCount);
						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB.countQueue: " + ZTracker.jobTracker[pawn].mainJob.countQueue[i]);
					}
				}
			};

			Toil reserveTargetA = Toils_Reserve.Reserve(TargetIndex.A);
			yield return reserveTargetA;
			Toil toilGoto = null;
			toilGoto = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnSomeonePhysicallyInteracting(TargetIndex.A).FailOn((Func<bool>)delegate
			{
				Pawn actor = toilGoto.actor;
				Job curJob = actor.jobs.curJob;
				if (curJob.haulMode == HaulMode.ToCellStorage)
				{
					Thing thing = curJob.GetTarget(TargetIndex.A).Thing;
					if (!actor.jobs.curJob.GetTarget(TargetIndex.B).Cell.IsValidStorageFor(base.Map, thing))
					{
						return true;
					}
				}
				return false;
			});
			yield return toilGoto;
			yield return new Toil
			{
				initAction = delegate ()
				{
					ZLogger.Message("200.3 TargetA.Thing: " + TargetA.Thing);
					ZLogger.Message("200.3 TargetA.Thing.Position: " + TargetA.Thing?.Position);
					ZLogger.Message("200.3 TargetA.Thing.Map: " + TargetA.Thing?.Map);
					ZLogger.Message("200.3 TargetA.Thing.stackCount: " + TargetA.Thing?.stackCount);
					this.savedThing = TargetA.Thing;
					var ZTracker = Current.Game.GetComponent<ZLevelsManager>();

					for (int i = ZTracker.jobTracker[pawn].mainJob.targetQueueB.Count - 1; i >= 0; i--)
					{
						var target = ZTracker.jobTracker[pawn].mainJob.targetQueueB[i];

						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB: " + target.Thing);
						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB.Map: " + target.Thing.Map);
						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB.stackCount: " + target.Thing.stackCount);
						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB.countQueue: " + ZTracker.jobTracker[pawn].mainJob.countQueue[i]);
					}
				}
			};

			yield return Toils_Haul.StartCarryThing(TargetIndex.A, putRemainderInQueue: false, subtractNumTakenFromJobCount: true);
			if (job.haulOpportunisticDuplicates)
			{
				yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveTargetA, TargetIndex.A, TargetIndex.B);
			}
			Toil carryToCell = Toils_Haul.CarryHauledThingToCell(TargetIndex.B);
			yield return carryToCell;
			yield return new Toil
			{
				initAction = delegate ()
				{
					ZLogger.Message("savedThing: " + this.savedThing);
					ZLogger.Message("200.5 TargetA.Thing: " + TargetA.Thing);
					ZLogger.Message("200.5 TargetA.Thing.Position: " + TargetA.Thing?.Position);
					ZLogger.Message("200.5 TargetA.Thing.Map: " + TargetA.Thing?.Map);
					ZLogger.Message("200.5 TargetA.Thing.stackCount: " + TargetA.Thing?.stackCount);
					var ZTracker = Current.Game.GetComponent<ZLevelsManager>();

					for (int i = ZTracker.jobTracker[pawn].mainJob.targetQueueB.Count - 1; i >= 0; i--)
					{
						var target = ZTracker.jobTracker[pawn].mainJob.targetQueueB[i];

						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB: " + target.Thing);
						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB.Map: " + target.Thing.Map);
						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB.stackCount: " + target.Thing.stackCount);
						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB.countQueue: " + ZTracker.jobTracker[pawn].mainJob.countQueue[i]);
					}
				}
			};

			yield return new Toil
			{
				initAction = delegate ()
				{
					ZLogger.Message("Pawn: " + pawn);
					ZLogger.Message("pawn.Map: " + pawn.Map);
					ZLogger.Message("TargetB: " + TargetB);
				}
			};
			yield return new Toil
			{
				initAction = delegate ()
				{
					if (TargetB.Cell.GetFirstItem(pawn.Map) != null)
					{
						IntVec3 newPosition = IntVec3.Invalid;

						IntVec3 center = (from x in GenRadial.RadialCellsAround(pawn.Position, 3f, useCenter: true)
										  where x.InBounds(pawn.Map) && x.GetFirstItem(pawn.Map) == null select x).FirstOrDefault();
						if (center != null)
						{
							job.targetB = new LocalTargetInfo(center);
						}

						else if (CellFinder.TryFindRandomCellNear(TargetB.Cell, pawn.Map, 3,
						    (IntVec3 c) => c.GetFirstItem(pawn.Map)?.def != TargetA.Thing.def, out newPosition))
						{
							job.targetB = new LocalTargetInfo(newPosition);
						}
					}
				}
			};

			yield return new Toil
			{
				initAction = delegate ()
				{
					ZLogger.Message("2 Pawn: " + pawn);
					ZLogger.Message("2 pawn.Map: " + pawn.Map);
					ZLogger.Message("2 TargetB: " + TargetB);
				}
			};

			yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.B, carryToCell, false);

			yield return new Toil
			{
				initAction = delegate ()
				{
					var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
					ZLogger.Message("201 TargetA.Thing: " + TargetA.Thing);
					ZLogger.Message("201 TargetA.Thing.Position: " + TargetA.Thing?.Position);
					ZLogger.Message("201 TargetA.Thing.Map: " + TargetA.Thing?.Map);
					ZLogger.Message("201 TargetA.Thing.stackCount: " + TargetA.Thing?.stackCount);
					for (int i = ZTracker.jobTracker[pawn].mainJob.targetQueueB.Count - 1; i >= 0; i--)
					{
						var target = ZTracker.jobTracker[pawn].mainJob.targetQueueB[i];

						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB: " + target.Thing);
						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB.Map: " + target.Thing.Map);
						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB.stackCount: " + target.Thing.stackCount);
						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB.countQueue: " + ZTracker.jobTracker[pawn].mainJob.countQueue[i]);
					}
					if (ZTracker.jobTracker.ContainsKey(this.pawn) && ZTracker.jobTracker[this.pawn].mainJob != null)
					{
						if (ZTracker.jobTracker[this.pawn].mainJob.targetQueueB != null 
						&& ZTracker.jobTracker[this.pawn].mainJob.targetQueueB
						.Where(x => x.Thing == TargetA.Thing).Count() == 0)
						{
							var newTarget = new LocalTargetInfo(TargetA.Thing);
							ZTracker.jobTracker[this.pawn].mainJob.targetQueueB.Add(newTarget);
							ZTracker.jobTracker[this.pawn].mainJob.countQueue.Add(newTarget.Thing.stackCount);
							ZLogger.Message("Adding " + newTarget + " to " + ZTracker.jobTracker[this.pawn].mainJob);
						}
					}

					ZLogger.Message("202 TargetA.Thing: " + TargetA.Thing);
					ZLogger.Message("202 TargetA.Thing.Position: " + TargetA.Thing?.Position);
					ZLogger.Message("202 TargetA.Thing.Map: " + TargetA.Thing?.Map);
					ZLogger.Message("202 TargetA.Thing.stackCount: " + TargetA.Thing?.stackCount);
					for (int i = ZTracker.jobTracker[pawn].mainJob.targetQueueB.Count - 1; i >= 0; i--)
					{
						var target = ZTracker.jobTracker[pawn].mainJob.targetQueueB[i];

						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB: " + target.Thing);
						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB.Map: " + target.Thing.Map);
						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB.stackCount: " + target.Thing.stackCount);
						ZLogger.Message("JobDriver_ZHaulToCell job.targetQueueB.countQueue: " + ZTracker.jobTracker[pawn].mainJob.countQueue[i]);
					}
				}
			};
		}
	}
}

