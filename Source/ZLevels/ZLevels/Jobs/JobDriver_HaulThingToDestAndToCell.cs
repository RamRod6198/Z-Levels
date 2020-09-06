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

            yield return new Toil
            {
                initAction = delegate ()
                {
                    ZLogger.Message(pawn + " 6 ZUtils.ZTracker.jobTracker[pawn].dest: " + ZTracker.jobTracker[pawn].dest);
                }
            };
            if (pawn.Map == this.job.targetA.Thing.Map && pawn.Map == ZTracker.jobTracker[pawn].dest)
            {
                ZLogger.Message("pawn map and thing map and dest map are same, yield breaking in JobDriver_HaulThingToDest");
                yield break;
            }

            yield return new Toil
            {
                initAction = delegate ()
                {
                    ZLogger.Message(pawn + " 7 ZUtils.ZTracker.jobTracker[pawn].dest: " + ZTracker.jobTracker[pawn].dest);
                }
            };
            yield return new Toil
            {
                initAction = delegate ()
                {
                    ZLogger.Message(pawn + " 10 ZUtils.ZTracker.jobTracker[pawn].dest: " + TargetA.Thing.Map);
                }
            };

            yield return new Toil
            {
                initAction = delegate ()
                {
                    ZLogger.Message(pawn + " 9 ZUtils.ZTracker.jobTracker[pawn].dest: " + ZTracker.jobTracker[pawn].dest);
                }
            };

            foreach (var toil in Toils_ZLevels.GoToMap(GetActor(), TargetA.Thing.Map, this))
            {
                yield return new Toil
                {
                    initAction = delegate ()
                    {
                        ZLogger.Message(pawn + " 11 ZUtils.ZTracker.jobTracker[pawn].dest: " + TargetA.Thing.Map);
                    }
                };

                yield return toil;
            }
            yield return new Toil
            {
                initAction = delegate ()
                {
                    ZLogger.Message(pawn + " 12 ZUtils.ZTracker.jobTracker[pawn].dest: " + ZTracker.jobTracker[pawn].dest);
                }
            };
            yield return new Toil
            {
                initAction = delegate ()
                {
                    ZLogger.Message("Trying to reserve " + job.targetA + " for " + pawn + " - " + job);
                }
            };
            yield return new Toil
            {
                initAction = delegate ()
                {
                    ZLogger.Message(pawn + " 13 ZUtils.ZTracker.jobTracker[pawn].dest: " + ZTracker.jobTracker[pawn].dest);
                }
            };
            Toil reserveTargetA = Toils_Reserve.Reserve(TargetIndex.A);
            yield return new Toil
            {
                initAction = delegate ()
                {
                    ZLogger.Message(pawn + " 14 ZUtils.ZTracker.jobTracker[pawn].dest: " + ZTracker.jobTracker[pawn].dest);
                }
            };
            yield return reserveTargetA;
            yield return new Toil
            {
                initAction = delegate ()
                {
                    ZLogger.Message(pawn + " 15 ZUtils.ZTracker.jobTracker[pawn].dest: " + ZTracker.jobTracker[pawn].dest);
                }
            };
            Toil toilGoto = null;
            yield return new Toil
            {
                initAction = delegate ()
                {
                    ZLogger.Message(pawn + " 16 ZUtils.ZTracker.jobTracker[pawn].dest: " + ZTracker.jobTracker[pawn].dest);
                }
            };
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
                    ZLogger.Message(pawn + " 17 ZUtils.ZTracker.jobTracker[pawn].dest: " + ZTracker.jobTracker[pawn].dest);
                }
            };

            yield return toilGoto;
            yield return new Toil
            {
                initAction = delegate ()
                {
                    ZLogger.Message(pawn + " 18 ZUtils.ZTracker.jobTracker[pawn].dest: " + ZTracker.jobTracker[pawn].dest);
                }
            };
            yield return new Toil
            {
                initAction = delegate ()
                {
                    this.savedThing = TargetA.Thing;
                }
            };
            yield return new Toil
            {
                initAction = delegate ()
                {
                    ZLogger.Message(pawn + " 19 ZUtils.ZTracker.jobTracker[pawn].dest: " + ZTracker.jobTracker[pawn].dest);
                }
            };

            yield return Toils_Haul.StartCarryThing(TargetIndex.A, putRemainderInQueue: false, subtractNumTakenFromJobCount: true);
            
            yield return new Toil
            {
                initAction = delegate ()
                {
                    ZLogger.Message(pawn + " 19.1 ZUtils.ZTracker.jobTracker[pawn].dest: " + ZTracker.jobTracker[pawn].dest);
                }
            };
            if (job.haulOpportunisticDuplicates)
            {
                yield return new Toil
                {
                    initAction = delegate ()
                    {
                        ZLogger.Message(pawn + " 19.2 ZUtils.ZTracker.jobTracker[pawn].dest: " + ZTracker.jobTracker[pawn].dest);
                    }
                };
                yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveTargetA, TargetIndex.A, TargetIndex.B);
                yield return new Toil
                {
                    initAction = delegate ()
                    {
                        ZLogger.Message(pawn + " 19.3 ZUtils.ZTracker.jobTracker[pawn].dest: " + ZTracker.jobTracker[pawn].dest);
                    }
                };
            }
            yield return new Toil
            {
                initAction = delegate ()
                {
                    ZLogger.Message(pawn + " 20 ZUtils.ZTracker.jobTracker[pawn].dest: " + ZTracker.jobTracker[pawn].dest);
                }
            };

            yield return new Toil
            {
                initAction = delegate ()
                {
                    ZLogger.Message(pawn + " 21 ZUtils.ZTracker.jobTracker[pawn].dest: " + ZTracker.jobTracker[pawn].dest);
                }
            };

            foreach (var toil in Toils_ZLevels.GoToMap(GetActor(), ZTracker.jobTracker[pawn].dest, this))
            {
                yield return new Toil
                {
                    initAction = delegate ()
                    {
                        ZLogger.Message(pawn + " 22 ZUtils.ZTracker.jobTracker[pawn].dest: " + ZTracker.jobTracker[pawn].dest);
                    }
                };

                yield return toil;
            }

            Toil carryToCell = Toils_Haul.CarryHauledThingToCell(TargetIndex.B);
            yield return carryToCell;
            yield return new Toil
            {
                initAction = delegate ()
                {
                    ZLogger.Message("10 savedThing: " + this.savedThing);
                    ZLogger.Message("10 carried Thing: " + pawn.carryTracker?.CarriedThing);
                    ZLogger.Message("200.5 TargetA.Thing: " + TargetA.Thing);
                    ZLogger.Message("200.5 TargetA.Thing.Position: " + TargetA.Thing?.Position);
                    ZLogger.Message("200.5 TargetA.Thing.Map: " + TargetA.Thing?.Map);
                    ZLogger.Message("200.5 TargetA.Thing.stackCount: " + TargetA.Thing?.stackCount);
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

            yield return new Toil
            {
                initAction = delegate ()
                {
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

