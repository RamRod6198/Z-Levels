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
            return true;
        }
        public Thing savedThing = null;

        public override IEnumerable<Toil> MakeNewToils()
        {
            var ZTracker = ZUtils.ZTracker;
            if (pawn.Map == job.targetA.Thing.Map && pawn.Map == ZTracker.jobTracker[pawn].dest)
            {
                //TODO: This is a mistake.  We have to check to see if we're reachable.  
                ZLogger.Message("pawn map and thing map and dest map are same, yield breaking in JobDriver_HaulThingToDest");

                //foreach (Toil t in CheckStairsForSameLevel(pawn, dest))
                //{
                //    yield return t;
                //}
                yield break;
            }
            yield return new Toil
            {
                initAction = delegate ()
                {
                    savedThing = job.targetA.Thing;
                    ZLogger.Message($"1111111111111 Saved thing: {savedThing} TargetA: {job.targetA.Thing}");
                }
            };

            Toil reserveItem = Toils_Reserve.Reserve(TargetIndex.A);
            ZLogger.Message($"{pawn} 8 ZUtils.ZTracker.jobTracker[pawn].dest: {TargetA.Thing.Map}");
            foreach (var toil in Toils_ZLevels.GoToMap(GetActor(), TargetA.Thing.Map, this))
            {
                ZLogger.Message($"{pawn} 9 ZUtils.ZTracker.jobTracker[pawn].dest: {TargetA.Thing.Map}");
                yield return toil;
            }
            yield return new Toil
            {
                initAction = delegate ()
                {
                    ZLogger.Message($"Trying to reserve {job.targetA} for {pawn} - {job}");
                }
            };
            yield return reserveItem.FailOnDespawnedNullOrForbidden(TargetIndex.A).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
            yield return Toils_Haul.StartCarryThing(TargetIndex.A, putRemainderInQueue: false, subtractNumTakenFromJobCount: true).FailOnDestroyedNullOrForbidden(TargetIndex.A);
            if (job.haulOpportunisticDuplicates)
            {
                yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveItem, TargetIndex.A, TargetIndex.None, takeFromValidStorage: true);
            }
            yield return new Toil
            {
                initAction = delegate ()
                {
                    ZLogger.Message($"22222222222222 Saved thing: {savedThing} TargetA: {job.targetA.Thing}");
                }
            };

            yield return new Toil
            {
                initAction = delegate ()
                {
                    try
                    {
                        if (ZTracker.jobTracker.ContainsKey(pawn))
                        {
                            if (ZTracker.jobTracker[pawn].mainJob.targetA.Thing != null
                            && ZTracker.jobTracker[pawn].mainJob.targetA.Thing == savedThing
                            && ZTracker.jobTracker[pawn].mainJob.targetA.Thing != TargetA.Thing)
                            {
                                ZTracker.jobTracker[pawn].mainJob.targetA = new LocalTargetInfo(TargetA.Thing);
                            }
                            else if (ZTracker.jobTracker[pawn].mainJob.targetB.Thing != null
                            && ZTracker.jobTracker[pawn].mainJob.targetB.Thing == savedThing
                            && ZTracker.jobTracker[pawn].mainJob.targetB.Thing != TargetA.Thing)
                            {
                                ZTracker.jobTracker[pawn].mainJob.targetB = new LocalTargetInfo(TargetA.Thing);
                            }

                            try
                            {
                                for (int i = ZTracker.jobTracker[pawn].mainJob.targetQueueA.Count - 1; i >= 0; i--)
                                {
                                    var target = ZTracker.jobTracker[pawn].mainJob.targetQueueA[i];
                                    if (target.Thing != null && target.Thing == savedThing && target.Thing != TargetA.Thing)
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
                                    if (target.Thing != null && target.Thing == savedThing && target.Thing != TargetA.Thing)
                                    {
                                        if (ZTracker.jobTracker[pawn].mainJob.targetQueueB[i].Thing.stackCount == 0)
                                        {
                                            ZTracker.jobTracker[pawn].mainJob.targetQueueB[i] = new LocalTargetInfo(TargetA.Thing);
                                            ZTracker.jobTracker[pawn].mainJob.countQueue[i] = TargetA.Thing.stackCount;
                                            break;
                                        }
                                        else
                                        {
                                            if (!ZTracker.jobTracker[pawn].mainJob.targetQueueB.Any(x => x.Thing == TargetA.Thing))
                                            {
                                                var newTarget = new LocalTargetInfo(TargetA.Thing);
                                                ZTracker.jobTracker[pawn].mainJob.targetQueueB.Add(newTarget);
                                                ZTracker.jobTracker[pawn].mainJob.countQueue.Add(newTarget.Thing.stackCount);
                                                int ind = ZTracker.jobTracker[pawn].mainJob.targetQueueB.FirstIndexOf(x => x.Thing == savedThing);
                                                ZTracker.jobTracker[pawn].mainJob.targetQueueB.RemoveAt(ind);
                                                ZTracker.jobTracker[pawn].mainJob.countQueue.RemoveAt(ind);
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
            yield return new Toil
            {
                initAction = delegate ()
                {
                    ZLogger.Message("33333333333333: " + savedThing);
                    ZLogger.Message("Saved thing: " + savedThing);
                    ZLogger.Message("TargetA: " + job.targetA.Thing);
                }
            };
            ZLogger.Message(pawn + " 6 ZUtils.ZTracker.jobTracker[pawn].dest: " + ZTracker.jobTracker[pawn].dest);

            foreach (var toil in Toils_ZLevels.GoToMap(GetActor(), ZTracker.jobTracker[pawn].dest, this))
            {
                ZLogger.Message(pawn + " 7 ZUtils.ZTracker.jobTracker[pawn].dest: " + ZTracker.jobTracker[pawn].dest);

                yield return toil;
            }
        }
    }
}

