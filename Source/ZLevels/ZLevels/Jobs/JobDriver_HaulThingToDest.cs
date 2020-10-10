using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace ZLevels
{
    public class JobDriver_HaulThingToDest : JobDriver_ZLevels
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }
        public Thing savedThing = null;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref savedThing, "savedThing");
        }
        public override IEnumerable<Toil> MakeNewToils()
        {
            var ZTracker = ZUtils.ZTracker;
            if (pawn.Map == this.job.targetA.Thing.Map && pawn.Map == ZTracker.jobTracker[pawn].targetDest.Map)
            {
                ZLogger.Message("pawn map and thing map and dest map are same, yield breaking in JobDriver_HaulThingToDest");
                yield break;
            }
            yield return new Toil
            {
                initAction = delegate ()
                {
                    this.savedThing = this.job.targetA.Thing;
                }
            };

            Toil reserveItem = Toils_Reserve.Reserve(TargetIndex.A);
            ZLogger.Message($"JobDriver HaulThingToDest1 About to call findRouteWithStairs, with pawn {GetActor()}, dest { TargetA.Thing}, instance {this}");
            foreach (var toil in Toils_ZLevels.GoToMap(GetActor(), new TargetInfo(TargetA.Thing).Map, this))
            {
                yield return toil;
            }
            yield return new Toil
            {
                initAction = delegate ()
                {
                    ZLogger.Message("JobDriver_HaulThingToDest 1: " + pawn + " trying to reserve: " + TargetA, true);
                }
            };
            Toil toilGoto = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);

            yield return reserveItem.FailOnDespawnedNullOrForbidden(TargetIndex.A).FailOnSomeonePhysicallyInteracting(TargetIndex.A);

            yield return new Toil
            {
                initAction = delegate ()
                {
                    ZLogger.Message("JobDriver_HaulThingToDest 1: " + pawn + " reserved: " + TargetA, true);
                }
            };

            yield return toilGoto;
            yield return Toils_Haul.StartCarryThing(TargetIndex.A, putRemainderInQueue: false, subtractNumTakenFromJobCount: true).FailOnDestroyedNullOrForbidden(TargetIndex.A);
            if (job.haulOpportunisticDuplicates)
            {
                yield return new Toil
                {
                    initAction = delegate ()
                    {
                        ZLogger.Message("2: " + pawn + " trying to reserve other things: " + TargetA, true);
                    }
                };

                yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveItem, TargetIndex.A, TargetIndex.None, takeFromValidStorage: true);
                yield return new Toil
                {
                    initAction = delegate ()
                    {
                        ZLogger.Message("2: " + pawn + " reserved other things: " + TargetA, true);
                    }
                };
            }
            yield return new Toil
            {
                initAction = delegate ()
                {
                    try
                    {
                        if (ZTracker.jobTracker.ContainsKey(pawn))
                        {
                            if (ZTracker.jobTracker[pawn].mainJob.targetA.Thing != null
                            && ZTracker.jobTracker[pawn].mainJob.targetA.Thing == this.savedThing
                            && ZTracker.jobTracker[pawn].mainJob.targetA.Thing != TargetA.Thing)
                            {
                                ZTracker.jobTracker[pawn].mainJob.targetA = new LocalTargetInfo(TargetA.Thing);
                            }
                            else if (ZTracker.jobTracker[pawn].mainJob.targetB.Thing != null
                            && ZTracker.jobTracker[pawn].mainJob.targetB.Thing == this.savedThing
                            && ZTracker.jobTracker[pawn].mainJob.targetB.Thing != TargetA.Thing)
                            {
                                ZTracker.jobTracker[pawn].mainJob.targetB = new LocalTargetInfo(TargetA.Thing);
                            }

                            try
                            {
                                for (int i = ZTracker.jobTracker[pawn].mainJob.targetQueueA.Count - 1; i >= 0; i--)
                                {
                                    var target = ZTracker.jobTracker[pawn].mainJob.targetQueueA[i];
                                    if (target.Thing != null && target.Thing == this.savedThing && target.Thing != TargetA.Thing)
                                    {
                                        ZTracker.jobTracker[pawn].mainJob.targetQueueA[i] = new LocalTargetInfo(TargetA.Thing);
                                    }
                                }
                            }
                            catch { }
                            try
                            {
                                for (int i = ZTracker.jobTracker[pawn].mainJob.targetQueueB.Count - 1; i >= 0; i--)
                                {
                                    var target = ZTracker.jobTracker[pawn].mainJob.targetQueueB[i];
                                    if (target.Thing != null && target.Thing == this.savedThing && target.Thing != TargetA.Thing)
                                    {
                                        if (ZTracker.jobTracker[pawn].mainJob.targetQueueB[i].Thing.stackCount == 0)
                                        {
                                            ZTracker.jobTracker[pawn].mainJob.targetQueueB[i] = new LocalTargetInfo(TargetA.Thing);
                                            ZTracker.jobTracker[pawn].mainJob.countQueue[i] = TargetA.Thing.stackCount;
                                            break;
                                        }
                                        else
                                        {
                                            if (ZTracker.jobTracker[pawn].mainJob.targetQueueB
                                            .Where(x => x.Thing == TargetA.Thing).Count() == 0)
                                            {
                                                var newTarget = new LocalTargetInfo(TargetA.Thing);
                                                ZTracker.jobTracker[this.pawn].mainJob.targetQueueB.Add(newTarget);
                                                ZTracker.jobTracker[this.pawn].mainJob.countQueue.Add(newTarget.Thing.stackCount);
                                                int ind = ZTracker.jobTracker[this.pawn].mainJob.targetQueueB.FirstIndexOf(x => x.Thing == this.savedThing);
                                                ZTracker.jobTracker[this.pawn].mainJob.targetQueueB.RemoveAt(ind);
                                                ZTracker.jobTracker[this.pawn].mainJob.countQueue.RemoveAt(ind);
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Z-Tracker produced an error in JobDriver_HaulThingToStairs class. Report about it to devs. Error: " + ex);
                    }
                }
            };
            ZLogger.Message($"JobDriver HaulThingToDest 2About to call findRouteWithStairs, with pawn {GetActor()}, dest {ZTracker.jobTracker[pawn].targetDest}, instance {this}");
            foreach (var toil in Toils_ZLevels.GoToMap(GetActor(), ZTracker.jobTracker[pawn].targetDest.Map, this))
            {
                yield return toil;
            }
        }
    }
}

