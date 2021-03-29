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
            Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - TryMakePreToilReservations - return true; - 1", true);
            return true;
        }
        public Thing savedThing = null;

        public override void ExposeData()
        {
            Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - ExposeData - base.ExposeData(); - 3", true);
            base.ExposeData();
            Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - ExposeData - Scribe_References.Look(ref savedThing, \"savedThing\"); - 4", true);
            Scribe_References.Look(ref savedThing, "savedThing");
        }
        public override IEnumerable<Toil> MakeNewToils()
        {
            Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - MakeNewToils - var ZTracker = ZUtils.ZTracker; - 5", true);
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
            Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - MakeNewToils - if (TargetA.Thing?.Map != null) - 12", true);
            if (TargetA.Thing?.Map != null)
            {
                Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - MakeNewToils - foreach (var toil in Toils_ZLevels.GoToMap(GetActor(), new TargetInfo(TargetA.Thing).Map, this)) - 13", true);
                foreach (var toil in Toils_ZLevels.GoToMap(GetActor(), new TargetInfo(TargetA.Thing).Map, this))
                {
                    yield return new Toil { initAction = delegate () { Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - MakeNewToils - yield return toil; - 14", true); } };
                    yield return toil;
                }
            }

            Toil toilGoto = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
            yield return new Toil { initAction = delegate () { Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - MakeNewToils - yield return reserveItem.FailOnDespawnedNullOrForbidden(TargetIndex.A).FailOnSomeonePhysicallyInteracting(TargetIndex.A); - 16", true); } };
            yield return reserveItem.FailOnDespawnedNullOrForbidden(TargetIndex.A).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            yield return new Toil { initAction = delegate () { Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - MakeNewToils - yield return toilGoto; - 17", true); } };
            yield return toilGoto;
            yield return new Toil { initAction = delegate () { Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - MakeNewToils - yield return Toils_Haul.StartCarryThing(TargetIndex.A, putRemainderInQueue: false, subtractNumTakenFromJobCount: true).FailOnDestroyedNullOrForbidden(TargetIndex.A); - 18", true); } };
            yield return Toils_Haul.StartCarryThing(TargetIndex.A, putRemainderInQueue: false, subtractNumTakenFromJobCount: true).FailOnDestroyedNullOrForbidden(TargetIndex.A);
            Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - MakeNewToils - if (job.haulOpportunisticDuplicates) - 19", true);
            if (job.haulOpportunisticDuplicates)
            {
                yield return new Toil { initAction = delegate () { Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - MakeNewToils - yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveItem, TargetIndex.A, TargetIndex.None, takeFromValidStorage: true); - 20", true); } };
                yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveItem, TargetIndex.A, TargetIndex.None, takeFromValidStorage: true);
            }
            yield return Toils_Haul.JumpIfAlsoCollectingNextTargetInQueue(toilGoto, TargetIndex.A);
            yield return new Toil
            {
                initAction = delegate ()
                {
                    Log.Message("Pawn: " + pawn);
                    Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - MakeNewToils - if (ZTracker.jobTracker.TryGetValue(pawn, out JobTracker jobTracker)) - 23", true);
                    if (ZTracker.jobTracker.TryGetValue(pawn, out JobTracker jobTracker))
                    {
                        Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - MakeNewToils - if (jobTracker.mainJob.targetA.Thing != null && jobTracker.mainJob.targetA.Thing == this.savedThing && jobTracker.mainJob.targetA.Thing != TargetA.Thing) - 24", true);
                        if (jobTracker.mainJob.targetA.Thing != null && jobTracker.mainJob.targetA.Thing == this.savedThing && jobTracker.mainJob.targetA.Thing != TargetA.Thing)
                        {
                            Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - MakeNewToils - jobTracker.mainJob.targetA = new LocalTargetInfo(TargetA.Thing); - 25", true);
                            jobTracker.mainJob.targetA = new LocalTargetInfo(TargetA.Thing);
                        }
                        else if (jobTracker.mainJob.targetB.Thing != null && jobTracker.mainJob.targetB.Thing == this.savedThing && jobTracker.mainJob.targetB.Thing != TargetA.Thing)
                        {
                            Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - MakeNewToils - jobTracker.mainJob.targetB = new LocalTargetInfo(TargetA.Thing); - 27", true);
                            jobTracker.mainJob.targetB = new LocalTargetInfo(TargetA.Thing);
                        }
                        Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - MakeNewToils - if (jobTracker.mainJob.targetQueueA != null) - 28", true);
                        if (jobTracker.mainJob.targetQueueA != null)
                        {
                            for (int i = jobTracker.mainJob.targetQueueA.Count - 1; i >= 0; i--)
                            {
                                Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - MakeNewToils - var target = jobTracker.mainJob.targetQueueA[i]; - 29", true);
                                var target = jobTracker.mainJob.targetQueueA[i];
                                Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - MakeNewToils - if (target.Thing != null && target.Thing == this.savedThing && target.Thing != TargetA.Thing) - 30", true);
                                if (target.Thing != null && target.Thing == this.savedThing && target.Thing != TargetA.Thing)
                                {
                                    Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - MakeNewToils - jobTracker.mainJob.targetQueueA[i] = new LocalTargetInfo(TargetA.Thing); - 31", true);
                                    jobTracker.mainJob.targetQueueA[i] = new LocalTargetInfo(TargetA.Thing);
                                }
                            }
                        }
                        Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - MakeNewToils - if (jobTracker.mainJob.targetQueueB != null) - 32", true);
                        if (jobTracker.mainJob.targetQueueB != null)
                        {
                            for (int i = jobTracker.mainJob.targetQueueB.Count - 1; i >= 0; i--)
                            {
                                Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - MakeNewToils - var target = jobTracker.mainJob.targetQueueB[i]; - 33", true);
                                var target = jobTracker.mainJob.targetQueueB[i];
                                Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - MakeNewToils - if (target.Thing != null && target.Thing == this.savedThing && target.Thing != TargetA.Thing) - 34", true);
                                if (target.Thing != null && target.Thing == this.savedThing && target.Thing != TargetA.Thing)
                                {
                                    Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - MakeNewToils - if (jobTracker.mainJob.targetQueueB[i].Thing.stackCount == 0) - 35", true);
                                    if (jobTracker.mainJob.targetQueueB[i].Thing.stackCount == 0)
                                    {
                                        Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - MakeNewToils - jobTracker.mainJob.targetQueueB[i] = new LocalTargetInfo(TargetA.Thing); - 36", true);
                                        jobTracker.mainJob.targetQueueB[i] = new LocalTargetInfo(TargetA.Thing);
                                        Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - MakeNewToils - jobTracker.mainJob.countQueue[i] = TargetA.Thing.stackCount; - 37", true);
                                        jobTracker.mainJob.countQueue[i] = TargetA.Thing.stackCount;
                                        Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - MakeNewToils - break; - 38", true);
                                        break;
                                    }
                                    else if (!jobTracker.mainJob.targetQueueB.Any(x => x.Thing == TargetA.Thing))
                                    {
                                        Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - MakeNewToils - var newTarget = new LocalTargetInfo(TargetA.Thing); - 40", true);
                                        var newTarget = new LocalTargetInfo(TargetA.Thing);
                                        Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - MakeNewToils - jobTracker.mainJob.targetQueueB.Add(newTarget); - 41", true);
                                        jobTracker.mainJob.targetQueueB.Add(newTarget);
                                        Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - MakeNewToils - jobTracker.mainJob.countQueue.Add(newTarget.Thing.stackCount); - 42", true);
                                        jobTracker.mainJob.countQueue.Add(newTarget.Thing.stackCount);
                                        Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - MakeNewToils - int ind = jobTracker.mainJob.targetQueueB.FirstIndexOf(x => x.Thing == this.savedThing); - 43", true);
                                        int ind = jobTracker.mainJob.targetQueueB.FirstIndexOf(x => x.Thing == this.savedThing);
                                        Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - MakeNewToils - jobTracker.mainJob.targetQueueB.RemoveAt(ind); - 44", true);
                                        jobTracker.mainJob.targetQueueB.RemoveAt(ind);
                                        Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - MakeNewToils - jobTracker.mainJob.countQueue.RemoveAt(ind); - 45", true);
                                        jobTracker.mainJob.countQueue.RemoveAt(ind);
                                        Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - MakeNewToils - break; - 46", true);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            };

            ZLogger.Message($"GoToMap 2");
            Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - MakeNewToils - ZLogger.Message($\"JobDriver HaulThingToDest 2About to call findRouteWithStairs, with pawn {GetActor()}, dest {ZTracker.jobTracker[pawn].targetDest.Map}, instance {this}\"); - 49", true);
            ZLogger.Message($"JobDriver HaulThingToDest 2About to call findRouteWithStairs, with pawn {GetActor()}, dest {ZTracker.jobTracker[pawn].targetDest.Map}, instance {this}");
            Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - MakeNewToils - foreach (var toil in Toils_ZLevels.GoToMap(GetActor(), ZTracker.jobTracker[pawn].targetDest.Map, this)) - 50", true);
            foreach (var toil in Toils_ZLevels.GoToMap(GetActor(), ZTracker.jobTracker[pawn].targetDest.Map, this))
            {
                yield return new Toil { initAction = delegate () { Log.Message("JobDriver_HaulThingToDest : JobDriver_ZLevels - MakeNewToils - yield return toil; - 51", true); } };
                yield return toil;
            }
        }
    }
}