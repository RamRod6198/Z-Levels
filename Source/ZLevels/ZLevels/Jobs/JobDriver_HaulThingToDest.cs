using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace ZLevels
{
    public class JobDriver_HaulThingToDest : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Log.Message("JobDriver_HaulThingToDest : JobDriver - TryMakePreToilReservations - return true; - 1", true);
            return true;
        }
        public Thing savedThing = null;

        public bool dummyLog(string str)
        {
            Log.Message("JobDriver_HaulThingToDest : JobDriver - dummyLog - ZLogger.Message(str); - 3", true);
            ZLogger.Message(str);
            Log.Message("JobDriver_HaulThingToDest : JobDriver - dummyLog - return true; - 4", true);
            return true;
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - var ZTracker = ZUtils.ZTracker; - 5", true);
            var ZTracker = ZUtils.ZTracker;
            Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - if (pawn.Map == this.job.targetA.Thing.Map && pawn.Map == ZTracker.jobTracker[pawn].dest) - 6", true);
            if (pawn.Map == this.job.targetA.Thing.Map && pawn.Map == ZTracker.jobTracker[pawn].dest)
            {
                Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - ZLogger.Message(\"pawn map and thing map and dest map are same, yield breaking in JobDriver_HaulThingToDest\"); - 7", true);
                ZLogger.Message("pawn map and thing map and dest map are same, yield breaking in JobDriver_HaulThingToDest");
                Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - yield break; - 8", true);
                yield break;
            }
            yield return new Toil
            {
                initAction = delegate ()
                {
                    Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - this.savedThing = this.job.targetA.Thing; - 9", true);
                    this.savedThing = this.job.targetA.Thing;
                    Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - ZLogger.Message(\"1111111111111: \" + this.savedThing); - 10", true);
                    ZLogger.Message("1111111111111: " + this.savedThing);
                    Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - ZLogger.Message(\"Saved thing: \" + this.savedThing); - 11", true);
                    ZLogger.Message("Saved thing: " + this.savedThing);
                    Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - ZLogger.Message(\"TargetA: \" + this.job.targetA.Thing); - 12", true);
                    ZLogger.Message("TargetA: " + this.job.targetA.Thing);
                    Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - ZLogger.Message(ZTracker.ShowJobData(this.job)); - 13", true);
                    ZLogger.Message(ZTracker.ShowJobData(this.job));
                    Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - ZLogger.Message(ZTracker.ShowJobData(ZTracker.jobTracker[pawn].mainJob)); - 14", true);
                    ZLogger.Message(ZTracker.ShowJobData(ZTracker.jobTracker[pawn].mainJob));
                }
            };

            Toil reserveItem = Toils_Reserve.Reserve(TargetIndex.A);
            Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - foreach (var toil in Toils_ZLevels.GoToMap(GetActor(), TargetA.Thing.Map, this)) - 17", true);
            foreach (var toil in Toils_ZLevels.GoToMap(GetActor(), TargetA.Thing.Map, this))
            {
                yield return new Toil { initAction = delegate () { Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - yield return toil; - 18", true); } };
                yield return toil;
            }
            yield return reserveItem.FailOnDespawnedNullOrForbidden(TargetIndex.A).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            yield return new Toil { initAction = delegate () { Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch); - 20", true); } };
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
            yield return new Toil { initAction = delegate () { Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - yield return Toils_Haul.StartCarryThing(TargetIndex.A, putRemainderInQueue: false, subtractNumTakenFromJobCount: true).FailOnDestroyedNullOrForbidden(TargetIndex.A); - 21", true); } };
            yield return Toils_Haul.StartCarryThing(TargetIndex.A, putRemainderInQueue: false, subtractNumTakenFromJobCount: true).FailOnDestroyedNullOrForbidden(TargetIndex.A);
            yield return new Toil { initAction = delegate () { Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveItem, TargetIndex.A, TargetIndex.None, takeFromValidStorage: true); - 22", true); } };
            yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveItem, TargetIndex.A, TargetIndex.None, takeFromValidStorage: true);
            yield return new Toil
            {
                initAction = delegate ()
                {
                    Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - ZLogger.Message(\"22222222222222: \" + this.savedThing); - 23", true);
                    ZLogger.Message("22222222222222: " + this.savedThing);
                    Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - ZLogger.Message(\"Saved thing: \" + this.savedThing); - 24", true);
                    ZLogger.Message("Saved thing: " + this.savedThing);
                    Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - ZLogger.Message(\"TargetA: \" + this.job.targetA.Thing); - 25", true);
                    ZLogger.Message("TargetA: " + this.job.targetA.Thing);
                    Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - ZLogger.Message(ZTracker.ShowJobData(this.job)); - 26", true);
                    ZLogger.Message(ZTracker.ShowJobData(this.job));
                    Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - ZLogger.Message(ZTracker.ShowJobData(ZTracker.jobTracker[pawn].mainJob)); - 27", true);
                    ZLogger.Message(ZTracker.ShowJobData(ZTracker.jobTracker[pawn].mainJob));
                }
            };

            yield return new Toil
            {
                initAction = delegate ()
                {
                    try
                    {
                        Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - if (ZTracker.jobTracker.ContainsKey(pawn)) - 29", true);
                        if (ZTracker.jobTracker.ContainsKey(pawn))
                        {
                            Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - if (ZTracker.jobTracker[pawn].mainJob.targetA.Thing != null - 30", true);
                            if (ZTracker.jobTracker[pawn].mainJob.targetA.Thing != null
                            && ZTracker.jobTracker[pawn].mainJob.targetA.Thing == this.savedThing
                            && ZTracker.jobTracker[pawn].mainJob.targetA.Thing != TargetA.Thing)
                            {
                                Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - ZTracker.jobTracker[pawn].mainJob.targetA = new LocalTargetInfo(TargetA.Thing); - 31", true);
                                ZTracker.jobTracker[pawn].mainJob.targetA = new LocalTargetInfo(TargetA.Thing);
                            }
                            else if (ZTracker.jobTracker[pawn].mainJob.targetB.Thing != null
                            && ZTracker.jobTracker[pawn].mainJob.targetB.Thing == this.savedThing
                            && ZTracker.jobTracker[pawn].mainJob.targetB.Thing != TargetA.Thing)
                            {
                                Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - ZTracker.jobTracker[pawn].mainJob.targetB = new LocalTargetInfo(TargetA.Thing); - 33", true);
                                ZTracker.jobTracker[pawn].mainJob.targetB = new LocalTargetInfo(TargetA.Thing);
                            }

                            try
                            {
                                for (int i = ZTracker.jobTracker[pawn].mainJob.targetQueueA.Count - 1; i >= 0; i--)
                                {
                                    Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - var target = ZTracker.jobTracker[pawn].mainJob.targetQueueA[i]; - 34", true);
                                    var target = ZTracker.jobTracker[pawn].mainJob.targetQueueA[i];
                                    Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - if (target.Thing != null && target.Thing == this.savedThing && target.Thing != TargetA.Thing) - 35", true);
                                    if (target.Thing != null && target.Thing == this.savedThing && target.Thing != TargetA.Thing)
                                    {
                                        Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - ZTracker.jobTracker[pawn].mainJob.targetQueueA[i] = new LocalTargetInfo(TargetA.Thing); - 36", true);
                                        ZTracker.jobTracker[pawn].mainJob.targetQueueA[i] = new LocalTargetInfo(TargetA.Thing);
                                    }
                                }
                            }
                            catch { }
                            try
                            {
                                for (int i = ZTracker.jobTracker[pawn].mainJob.targetQueueB.Count - 1; i >= 0; i--)
                                {
                                    Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - var target = ZTracker.jobTracker[pawn].mainJob.targetQueueB[i]; - 37", true);
                                    var target = ZTracker.jobTracker[pawn].mainJob.targetQueueB[i];
                                    Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - if (target.Thing != null && target.Thing == this.savedThing && target.Thing != TargetA.Thing) - 38", true);
                                    if (target.Thing != null && target.Thing == this.savedThing && target.Thing != TargetA.Thing)
                                    {
                                        Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - if (ZTracker.jobTracker[pawn].mainJob.targetQueueB[i].Thing.stackCount == 0) - 39", true);
                                        if (ZTracker.jobTracker[pawn].mainJob.targetQueueB[i].Thing.stackCount == 0)
                                        {
                                            Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - ZTracker.jobTracker[pawn].mainJob.targetQueueB[i] = new LocalTargetInfo(TargetA.Thing); - 40", true);
                                            ZTracker.jobTracker[pawn].mainJob.targetQueueB[i] = new LocalTargetInfo(TargetA.Thing);
                                            Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - ZTracker.jobTracker[pawn].mainJob.countQueue[i] = TargetA.Thing.stackCount; - 41", true);
                                            ZTracker.jobTracker[pawn].mainJob.countQueue[i] = TargetA.Thing.stackCount;
                                            Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - break; - 42", true);
                                            break;
                                        }
                                        else
                                        {
                                            Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - if (ZTracker.jobTracker[pawn].mainJob.targetQueueB - 43", true);
                                            if (ZTracker.jobTracker[pawn].mainJob.targetQueueB
                                            .Where(x => x.Thing == TargetA.Thing).Count() == 0)
                                            {
                                                Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - var newTarget = new LocalTargetInfo(TargetA.Thing); - 44", true);
                                                var newTarget = new LocalTargetInfo(TargetA.Thing);
                                                Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - ZTracker.jobTracker[this.pawn].mainJob.targetQueueB.Add(newTarget); - 45", true);
                                                ZTracker.jobTracker[this.pawn].mainJob.targetQueueB.Add(newTarget);
                                                Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - ZTracker.jobTracker[this.pawn].mainJob.countQueue.Add(newTarget.Thing.stackCount); - 46", true);
                                                ZTracker.jobTracker[this.pawn].mainJob.countQueue.Add(newTarget.Thing.stackCount);
                                                Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - int ind = ZTracker.jobTracker[this.pawn].mainJob.targetQueueB.FirstIndexOf(x => x.Thing == this.savedThing); - 47", true);
                                                int ind = ZTracker.jobTracker[this.pawn].mainJob.targetQueueB.FirstIndexOf(x => x.Thing == this.savedThing);
                                                Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - ZTracker.jobTracker[this.pawn].mainJob.targetQueueB.RemoveAt(ind); - 48", true);
                                                ZTracker.jobTracker[this.pawn].mainJob.targetQueueB.RemoveAt(ind);
                                                Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - ZTracker.jobTracker[this.pawn].mainJob.countQueue.RemoveAt(ind); - 49", true);
                                                ZTracker.jobTracker[this.pawn].mainJob.countQueue.RemoveAt(ind);
                                                Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - break; - 50", true);
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
                        Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - Log.Error(\"Z-Tracker produced an error in JobDriver_HaulThingToStairs class. Report about it to devs. Error: \" + ex); - 51", true);
                        Log.Error("Z-Tracker produced an error in JobDriver_HaulThingToStairs class. Report about it to devs. Error: " + ex);
                    }
                }
            };
            yield return new Toil
            {
                initAction = delegate ()
                {
                    Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - ZLogger.Message(\"33333333333333: \" + this.savedThing); - 53", true);
                    ZLogger.Message("33333333333333: " + this.savedThing);
                    Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - ZLogger.Message(\"Saved thing: \" + this.savedThing); - 54", true);
                    ZLogger.Message("Saved thing: " + this.savedThing);
                    Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - ZLogger.Message(\"TargetA: \" + this.job.targetA.Thing); - 55", true);
                    ZLogger.Message("TargetA: " + this.job.targetA.Thing);
                    Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - ZLogger.Message(ZTracker.ShowJobData(this.job)); - 56", true);
                    ZLogger.Message(ZTracker.ShowJobData(this.job));
                    Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - ZLogger.Message(ZTracker.ShowJobData(ZTracker.jobTracker[pawn].mainJob)); - 57", true);
                    ZLogger.Message(ZTracker.ShowJobData(ZTracker.jobTracker[pawn].mainJob));
                }
            };

            Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - foreach (var toil in Toils_ZLevels.GoToMap(GetActor(), ZTracker.jobTracker[pawn].dest, this)) - 59", true);
            foreach (var toil in Toils_ZLevels.GoToMap(GetActor(), ZTracker.jobTracker[pawn].dest, this))
            {
                yield return new Toil { initAction = delegate () { Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - yield return toil; - 60", true); } };
                yield return toil;
            }
        }
    }
}

