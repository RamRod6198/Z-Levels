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

            yield return new Toil
            {
                initAction = delegate ()
                {
                    Log.Message("pawn.Map: " + pawn.Map);
                    Log.Message("this.job.targetA.Thing.Map: " + this.job.targetA.Thing?.Map);
                    Log.Message("ZTracker.jobTracker[pawn].targetDest.Map: " + ZTracker.jobTracker[pawn].targetDest.Map);

                    if (pawn.Map == this.job.targetA.Thing?.Map && pawn.Map == ZTracker.jobTracker[pawn].targetDest.Map)
                    {
                        ZLogger.Message("pawn map and thing map and dest map are same, yield breaking in JobDriver_HaulThingToDest");
                        this.EndJobWith(JobCondition.InterruptForced);
                    }
                    this.savedThing = this.job.targetA.Thing;
                }
            };

            Toil reserveItem = Toils_Reserve.Reserve(TargetIndex.A);
            if (TargetA.Thing?.Map != null)
            {
                foreach (var toil in Toils_ZLevels.GoToMap(GetActor(), new TargetInfo(TargetA.Thing).Map, this))
                {
                    yield return toil;
                }
            }

            Toil toilGoto = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
            yield return reserveItem.FailOnDespawnedNullOrForbidden(TargetIndex.A).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            yield return toilGoto;
            yield return Toils_Haul.StartCarryThing(TargetIndex.A, putRemainderInQueue: false, subtractNumTakenFromJobCount: true).FailOnDestroyedNullOrForbidden(TargetIndex.A);
            if (job.haulOpportunisticDuplicates)
            {
                yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveItem, TargetIndex.A, TargetIndex.None, takeFromValidStorage: true);
            }
            yield return Toils_Haul.JumpIfAlsoCollectingNextTargetInQueue(toilGoto, TargetIndex.A);
            yield return new Toil
            {
                initAction = delegate ()
                {
                    Log.Message("Pawn: " + pawn);
                    if (ZTracker.jobTracker.TryGetValue(pawn, out JobTracker jobTracker))
                    {
                        if (jobTracker.mainJob.targetA.Thing != null && jobTracker.mainJob.targetA.Thing == this.savedThing && jobTracker.mainJob.targetA.Thing != TargetA.Thing)
                        {
                            jobTracker.mainJob.targetA = new LocalTargetInfo(TargetA.Thing);
                        }
                        else if (jobTracker.mainJob.targetB.Thing != null && jobTracker.mainJob.targetB.Thing == this.savedThing && jobTracker.mainJob.targetB.Thing != TargetA.Thing)
                        {
                            jobTracker.mainJob.targetB = new LocalTargetInfo(TargetA.Thing);
                        }
                        if (jobTracker.mainJob.targetQueueA != null)
                        {
                            for (int i = jobTracker.mainJob.targetQueueA.Count - 1; i >= 0; i--)
                            {
                                var target = jobTracker.mainJob.targetQueueA[i];
                                if (target.Thing != null && target.Thing == this.savedThing && target.Thing != TargetA.Thing)
                                {
                                    jobTracker.mainJob.targetQueueA[i] = new LocalTargetInfo(TargetA.Thing);
                                }
                            }
                        }
                        if (jobTracker.mainJob.targetQueueB != null)
                        {
                            for (int i = jobTracker.mainJob.targetQueueB.Count - 1; i >= 0; i--)
                            {
                                var target = jobTracker.mainJob.targetQueueB[i];
                                if (target.Thing != null && target.Thing == this.savedThing && target.Thing != TargetA.Thing)
                                {
                                    if (jobTracker.mainJob.targetQueueB[i].Thing.stackCount == 0)
                                    {
                                        jobTracker.mainJob.targetQueueB[i] = new LocalTargetInfo(TargetA.Thing);
                                        jobTracker.mainJob.countQueue[i] = TargetA.Thing.stackCount;
                                        break;
                                    }
                                    else if (!jobTracker.mainJob.targetQueueB.Any(x => x.Thing == TargetA.Thing))
                                    {
                                        var newTarget = new LocalTargetInfo(TargetA.Thing);
                                        jobTracker.mainJob.targetQueueB.Add(newTarget);
                                        jobTracker.mainJob.countQueue.Add(newTarget.Thing.stackCount);
                                        int ind = jobTracker.mainJob.targetQueueB.FirstIndexOf(x => x.Thing == this.savedThing);
                                        jobTracker.mainJob.targetQueueB.RemoveAt(ind);
                                        jobTracker.mainJob.countQueue.RemoveAt(ind);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            };

            ZLogger.Message($"GoToMap 2");
            ZLogger.Message($"JobDriver HaulThingToDest 2About to call findRouteWithStairs, with pawn {GetActor()}, dest {ZTracker.jobTracker[pawn].targetDest.Map}, instance {this}");
            foreach (var toil in Toils_ZLevels.GoToMap(GetActor(), ZTracker.jobTracker[pawn].targetDest.Map, this))
            {
                yield return toil;
            }
        }
    }
}