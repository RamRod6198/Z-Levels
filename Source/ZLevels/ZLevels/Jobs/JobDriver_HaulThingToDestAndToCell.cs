using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace ZLevels
{
    public class JobDriver_HaulThingToDestAndToCell : JobDriver_ZLevels
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
            return "ReportHauling".Translate(thing.Label, thing);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
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

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            //this.FailOnBurningImmobile(TargetIndex.B);
            if (!forbiddenInitially)
            {
                this.FailOnForbidden(TargetIndex.A);
            }
            var ZTracker = ZUtils.ZTracker;
            if (pawn.Map == this.job.targetA.Thing.Map && pawn.Map == ZTracker.jobTracker[pawn].targetDest.Map)
            {
                ZLogger.Message("pawn map and thing map and dest map are same, yield breaking in JobDriver_HaulThingToDest");
                yield break;
            }
            ZLogger.Message($"JobDriver HaulThingToDestAndCell1 About to call findRouteWithStairs, with pawn {GetActor()}, dest {new TargetInfo(TargetA.Thing)}, instance {this}");
            Log.Message("1 - pawn.Map: " + pawn.Map + " - dest: " + new TargetInfo(TargetA.Thing).Map, true);
            foreach (var toil in Toils_ZLevels.GoToMap(GetActor(), new TargetInfo(TargetA.Thing).Map, this))
            {
                yield return toil;
            }
            Toil reserveTargetA = Toils_Reserve.Reserve(TargetIndex.A);
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

            yield return new Toil
            {
                initAction = delegate ()
                {
                    ZLogger.Message("JobDriver_HaulThingToDestAndToCell 1: " + pawn + " trying to reserve: " + TargetA, true);
                }
            };
            yield return reserveTargetA;
            yield return toilGoto;
            yield return new Toil
            {
                initAction = delegate ()
                {
                    this.savedThing = TargetA.Thing;
                }
            };
            yield return Toils_Haul.StartCarryThing(TargetIndex.A, putRemainderInQueue: false, subtractNumTakenFromJobCount: true);
            if (job.haulOpportunisticDuplicates)
            {
                yield return new Toil
                {
                    initAction = delegate ()
                    {
                        ZLogger.Message("JobDriver_HaulThingToDestAndToCell 2: " + pawn + " trying to reserve other things: " + TargetA, true);
                    }
                };
                yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveTargetA, TargetIndex.A, TargetIndex.B);
            }
            ZLogger.Message($"JobDriver HaulThingToDestAndCell2 About to call findRouteWithStairs, with pawn {GetActor()}, dest {ZTracker.jobTracker[pawn].targetDest}, instance {this}");
            Log.Message("2 - pawn.Map: " + pawn.Map + " - dest: " + ZTracker.jobTracker[pawn].targetDest.Map, true);
            foreach (var toil in Toils_ZLevels.GoToMap(GetActor(), ZTracker.jobTracker[pawn].targetDest.Map, this))
            {
                yield return toil;
            }
            Toil carryToCell = Toils_Haul.CarryHauledThingToCell(TargetIndex.B);
            yield return carryToCell;
            yield return new Toil
            {
                initAction = delegate ()
                {
                    if (TargetB.Cell.GetFirstItem(pawn.Map) != null)
                    {
                        IntVec3 newPosition = IntVec3.Invalid;

                        IntVec3 center = (from x in GenRadial.RadialCellsAround(pawn.Position, 3f, useCenter: true)
                                          where x.InBounds(pawn.Map) && x.GetFirstItem(pawn.Map) == null
                                          select x).FirstOrDefault();
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
            yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.B, carryToCell, false);
        }
    }
}

