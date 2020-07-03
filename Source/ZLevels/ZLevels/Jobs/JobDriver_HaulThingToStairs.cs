using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ZLevels
{
    public class JobDriver_HaulThingToStairs : JobDriver
    {

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Log.Message("JobDriver_HaulThingToStairs : JobDriver - TryMakePreToilReservations - return this.pawn.Reserve(TargetB, job, 1, -1, null, errorOnFailed); - 1", true);
            return this.pawn.Reserve(TargetB, job, 1, -1, null, errorOnFailed);
        }

        public Thing savedThing = null;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return new Toil
            {
                initAction = delegate ()
                {
                    Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - var ZTracker = Current.Game.GetComponent<ZLevelsManager>(); - 3", true);
                    var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                    Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - if (ZTracker.jobTracker.ContainsKey(pawn)) - 4", true);
                    if (ZTracker.jobTracker.ContainsKey(pawn))
                    {
                        Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - this.savedThing = this.TargetB.Thing; - 5", true);
                        this.savedThing = this.TargetB.Thing;
                    }
                }
            };
            Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - this.FailOnDestroyedOrNull(TargetIndex.B); - 7", true);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - this.FailOnBurningImmobile(TargetIndex.B); - 8", true);
            this.FailOnBurningImmobile(TargetIndex.B);
            Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - this.FailOnForbidden(TargetIndex.B); - 9", true);
            this.FailOnForbidden(TargetIndex.B);
            yield return new Toil
            {
                initAction = delegate ()
                {
                    Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - var ZTracker = Current.Game.GetComponent<ZLevelsManager>(); - 10", true);
                    var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                    Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - if (ZTracker.jobTracker[pawn].mainJob.count != -1) - 11", true);
                    if (ZTracker.jobTracker[pawn].mainJob.count != -1)
                    {
                        Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - this.pawn.jobs.curJob.count = ZTracker.jobTracker[pawn].mainJob.count; - 12", true);
                        this.pawn.jobs.curJob.count = ZTracker.jobTracker[pawn].mainJob.count;
                    }
                    else if (this.pawn.jobs.curJob.count == -1)
                    {
                        Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - this.pawn.jobs.curJob.count = Mathf.Min(TargetB.Thing.stackCount, (int)(pawn.GetStatValue(StatDefOf.CarryingCapacity, true) / TargetB.Thing.def.VolumePerUnit)); - 14", true);
                        this.pawn.jobs.curJob.count = Mathf.Min(TargetB.Thing.stackCount, (int)(pawn.GetStatValue(StatDefOf.CarryingCapacity, true) / TargetB.Thing.def.VolumePerUnit));
                        Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - if (this.pawn.jobs.curJob.count < 0) - 15", true);
                        if (this.pawn.jobs.curJob.count < 0)
                        {
                            Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - this.pawn.jobs.curJob.count = TargetB.Thing.stackCount; - 16", true);
                            this.pawn.jobs.curJob.count = TargetB.Thing.stackCount;
                        }
                    }
                    ZLogger.Message(this.pawn + " haul count: " + this.pawn.jobs.curJob.count);
                }
            };

            Toil reserveItem = Toils_Reserve.Reserve(TargetIndex.B);
            yield return new Toil { initAction = delegate () { Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - yield return reserveItem; - 20", true); } };
            yield return reserveItem;
            yield return new Toil { initAction = delegate () { Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B); - 21", true); } };
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
            yield return new Toil { initAction = delegate () { Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - yield return Toils_Haul.StartCarryThing(TargetIndex.B, putRemainderInQueue: false, subtractNumTakenFromJobCount: true).FailOnDestroyedNullOrForbidden(TargetIndex.B); - 22", true); } };
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, putRemainderInQueue: false, subtractNumTakenFromJobCount: true).FailOnDestroyedNullOrForbidden(TargetIndex.B);
            yield return new Toil { initAction = delegate () { Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveItem, TargetIndex.B, TargetIndex.None, takeFromValidStorage: true); - 23", true); } };
            yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveItem, TargetIndex.B, TargetIndex.None, takeFromValidStorage: true);
            yield return new Toil { initAction = delegate () { Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell); - 24", true); } };
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell);

            //Toil reserveTargetA = Toils_Reserve.Reserve(TargetIndex.B, 1, -1, null);
            //yield return reserveTargetA;
            //yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);
            //yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, true, false);
            //yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveTargetA, TargetIndex.B, TargetIndex.A,
            //    false, null);
            //Toil carryToCell = Toils_Haul.CarryHauledThingToCell(TargetIndex.A);
            //yield return carryToCell;
            //yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell);

            Toil useStairs = Toils_General.Wait(60, 0);
            Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ToilEffects.WithProgressBarToilDelay(useStairs, TargetIndex.A, false, -0.5f); - 34", true);
            ToilEffects.WithProgressBarToilDelay(useStairs, TargetIndex.A, false, -0.5f);
            Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ToilFailConditions.FailOnDespawnedNullOrForbidden<Toil>(useStairs, TargetIndex.A); - 35", true);
            ToilFailConditions.FailOnDespawnedNullOrForbidden<Toil>(useStairs, TargetIndex.A);
            Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ToilFailConditions.FailOnCannotTouch<Toil>(useStairs, TargetIndex.A, PathEndMode.OnCell); - 36", true);
            ToilFailConditions.FailOnCannotTouch<Toil>(useStairs, TargetIndex.A, PathEndMode.OnCell);

            //yield return new Toil
            //{
            //    initAction = delegate () {
            //        ZLogger.Message("this.pawn.CanReachImmediate(TargetA.Thing, PathEndMode.OnCell): "
            //            + this.pawn.CanReachImmediate(TargetA.Thing, PathEndMode.OnCell), true);
            //    }
            //};
            yield return new Toil { initAction = delegate () { Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - yield return useStairs; - 39", true); } };
            yield return useStairs;

            yield return new Toil
            {
                initAction = delegate ()
                {
                    try
                    {
                        Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - var ZTracker = Current.Game.GetComponent<ZLevelsManager>(); - 40", true);
                        var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                        Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - if (ZTracker.jobTracker.ContainsKey(pawn)) - 41", true);
                        if (ZTracker.jobTracker.ContainsKey(pawn))
                        {
                            Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - if (ZTracker.jobTracker[pawn].mainJob.targetA.Thing != null - 42", true);
                            if (ZTracker.jobTracker[pawn].mainJob.targetA.Thing != null
                            && ZTracker.jobTracker[pawn].mainJob.targetA.Thing == this.savedThing
                            && ZTracker.jobTracker[pawn].mainJob.targetA.Thing != TargetB.Thing)
                            {
                                Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ZLogger.Message(\"1 Pawns carried thing not the same: \" + ZTracker.jobTracker[pawn].mainJob.targetA.Thing); - 43", true);
                                ZLogger.Message("1 Pawns carried thing not the same: " + ZTracker.jobTracker[pawn].mainJob.targetA.Thing);
                                Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ZTracker.jobTracker[pawn].mainJob.targetA = new LocalTargetInfo(TargetB.Thing); - 44", true);
                                ZTracker.jobTracker[pawn].mainJob.targetA = new LocalTargetInfo(TargetB.Thing);
                            }
                            else if (ZTracker.jobTracker[pawn].mainJob.targetB.Thing != null
                            && ZTracker.jobTracker[pawn].mainJob.targetB.Thing == this.savedThing
                            && ZTracker.jobTracker[pawn].mainJob.targetB.Thing != TargetB.Thing)
                            {
                                Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ZLogger.Message(\"2 Pawns carried thing not the same: \" + ZTracker.jobTracker[pawn].mainJob.targetB.Thing); - 46", true);
                                ZLogger.Message("2 Pawns carried thing not the same: " + ZTracker.jobTracker[pawn].mainJob.targetB.Thing);
                                Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ZTracker.jobTracker[pawn].mainJob.targetB = new LocalTargetInfo(TargetB.Thing); - 47", true);
                                ZTracker.jobTracker[pawn].mainJob.targetB = new LocalTargetInfo(TargetB.Thing);
                            }

                            try
                            {
                                for (int i = ZTracker.jobTracker[pawn].mainJob.targetQueueA.Count - 1; i >= 0; i--)
                                {
                                    Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - var target = ZTracker.jobTracker[pawn].mainJob.targetQueueA[i]; - 48", true);
                                    var target = ZTracker.jobTracker[pawn].mainJob.targetQueueA[i];
                                    Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - if (target.Thing != null && target.Thing == this.savedThing && target.Thing != TargetB.Thing) - 49", true);
                                    if (target.Thing != null && target.Thing == this.savedThing && target.Thing != TargetB.Thing)
                                    {
                                        Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ZLogger.Message(\"3 Pawns carried thing not the same: \" + target.Thing); - 50", true);
                                        ZLogger.Message("3 Pawns carried thing not the same: " + target.Thing);
                                        Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ZTracker.jobTracker[pawn].mainJob.targetQueueA[i] = new LocalTargetInfo(TargetB.Thing); - 51", true);
                                        ZTracker.jobTracker[pawn].mainJob.targetQueueA[i] = new LocalTargetInfo(TargetB.Thing);
                                    }
                                }
                            }
                            catch { }
                            try
                            {

                                try
                                {
                                    Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ZLogger.Message(\"--------------------------\"); - 52", true);
                                    ZLogger.Message("--------------------------");
                                    for (int i = ZTracker.jobTracker[pawn].mainJob.targetQueueB.Count - 1; i >= 0; i--)
                                    {
                                        Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - var target = ZTracker.jobTracker[pawn].mainJob.targetQueueB[i]; - 53", true);
                                        var target = ZTracker.jobTracker[pawn].mainJob.targetQueueB[i];
                                        Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ZLogger.Message(\"JobDriver_HaulThingToStairs BEFORE job.targetQueueB: \" + target.Thing); - 54", true);
                                        ZLogger.Message("JobDriver_HaulThingToStairs BEFORE job.targetQueueB: " + target.Thing);
                                        Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ZLogger.Message(\"JobDriver_HaulThingToStairs BEFORE job.targetQueueB.Map: \" + target.Thing.Map); - 55", true);
                                        ZLogger.Message("JobDriver_HaulThingToStairs BEFORE job.targetQueueB.Map: " + target.Thing.Map);
                                        Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ZLogger.Message(\"JobDriver_HaulThingToStairs BEFORE job.targetQueueB.stackCount: \" + target.Thing.stackCount); - 56", true);
                                        ZLogger.Message("JobDriver_HaulThingToStairs BEFORE job.targetQueueB.stackCount: " + target.Thing.stackCount);
                                        Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ZLogger.Message(\"JobDriver_HaulThingToStairs BEFORE job.targetQueueB.countQueue: \" + ZTracker.jobTracker[pawn].mainJob.countQueue[i]); - 57", true);
                                        ZLogger.Message("JobDriver_HaulThingToStairs BEFORE job.targetQueueB.countQueue: " + ZTracker.jobTracker[pawn].mainJob.countQueue[i]);
                                    }
                                }
                                catch { }

                                for (int i = ZTracker.jobTracker[pawn].mainJob.targetQueueB.Count - 1; i >= 0; i--)
                                {
                                    Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - var target = ZTracker.jobTracker[pawn].mainJob.targetQueueB[i]; - 58", true);
                                    var target = ZTracker.jobTracker[pawn].mainJob.targetQueueB[i];
                                    Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - if (target.Thing != null && target.Thing == this.savedThing && target.Thing != TargetB.Thing) - 59", true);
                                    if (target.Thing != null && target.Thing == this.savedThing && target.Thing != TargetB.Thing)
                                    {
                                        Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ZLogger.Message(\"4 Pawns carried thing not the same\"); - 60", true);
                                        ZLogger.Message("4 Pawns carried thing not the same");
                                        Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ZLogger.Message(\"4 Pawns target.Thing: \" + target.Thing); - 61", true);
                                        ZLogger.Message("4 Pawns target.Thing: " + target.Thing);
                                        Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ZLogger.Message(\"4 Pawns target.Thing.Map: \" + target.Thing.Map); - 62", true);
                                        ZLogger.Message("4 Pawns target.Thing.Map: " + target.Thing.Map);
                                        Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ZLogger.Message(\"4 Pawns this.savedThing: \" + this.savedThing); - 63", true);
                                        ZLogger.Message("4 Pawns this.savedThing: " + this.savedThing);
                                        Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ZLogger.Message(\"4 Pawns this.savedThing.Map: \" + this.savedThing.Map); - 64", true);
                                        ZLogger.Message("4 Pawns this.savedThing.Map: " + this.savedThing.Map);
                                        Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ZLogger.Message(\"4 Pawns TargetB.Thing: \" + TargetB.Thing); - 65", true);
                                        ZLogger.Message("4 Pawns TargetB.Thing: " + TargetB.Thing);
                                        Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ZLogger.Message(\"4 Pawns TargetB.Thing.Map: \" + TargetB.Thing.Map); - 66", true);
                                        ZLogger.Message("4 Pawns TargetB.Thing.Map: " + TargetB.Thing.Map);

                                        //ZLogger.Message("Replacing " + ZTracker.jobTracker[this.pawn].mainJob.targetQueueB[i] + " by " + TargetB);
                                        //
                                        //ZTracker.jobTracker[pawn].mainJob.targetQueueB[i] = new LocalTargetInfo(TargetB.Thing);
                                        //ZTracker.jobTracker[pawn].mainJob.countQueue[i] = TargetB.Thing.stackCount;
                                        //

                                        Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - if (ZTracker.jobTracker[pawn].mainJob.targetQueueB[i].Thing.stackCount == 0) - 70", true);
                                        if (ZTracker.jobTracker[pawn].mainJob.targetQueueB[i].Thing.stackCount == 0)
                                        {
                                            Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ZTracker.jobTracker[pawn].mainJob.targetQueueB[i] = new LocalTargetInfo(TargetB.Thing); - 71", true);
                                            ZTracker.jobTracker[pawn].mainJob.targetQueueB[i] = new LocalTargetInfo(TargetB.Thing);
                                            Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ZTracker.jobTracker[pawn].mainJob.countQueue[i] = TargetB.Thing.stackCount; - 72", true);
                                            ZTracker.jobTracker[pawn].mainJob.countQueue[i] = TargetB.Thing.stackCount;
                                            Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ZLogger.Message(\"Altering \" + ZTracker.jobTracker[this.pawn].mainJob); - 73", true);
                                            ZLogger.Message("Altering " + ZTracker.jobTracker[this.pawn].mainJob);
                                            Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - break; - 74", true);
                                            break;

                                        }
                                        else
                                        {
                                            Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - if (ZTracker.jobTracker[pawn].mainJob.targetQueueB - 75", true);
                                            if (ZTracker.jobTracker[pawn].mainJob.targetQueueB
                                            .Where(x => x.Thing == TargetB.Thing).Count() == 0)
                                            {
                                                Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - var newTarget = new LocalTargetInfo(TargetB.Thing); - 76", true);
                                                var newTarget = new LocalTargetInfo(TargetB.Thing);
                                                Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ZTracker.jobTracker[this.pawn].mainJob.targetQueueB.Add(newTarget); - 77", true);
                                                ZTracker.jobTracker[this.pawn].mainJob.targetQueueB.Add(newTarget);
                                                Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ZTracker.jobTracker[this.pawn].mainJob.countQueue.Add(newTarget.Thing.stackCount); - 78", true);
                                                ZTracker.jobTracker[this.pawn].mainJob.countQueue.Add(newTarget.Thing.stackCount);
                                                Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ZLogger.Message(\"2 Adding \" + newTarget + \" to \" + ZTracker.jobTracker[this.pawn].mainJob); - 79", true);
                                                ZLogger.Message("2 Adding " + newTarget + " to " + ZTracker.jobTracker[this.pawn].mainJob);
                                                Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - int ind = ZTracker.jobTracker[this.pawn].mainJob.targetQueueB.FirstIndexOf(x => x.Thing == this.savedThing); - 80", true);
                                                int ind = ZTracker.jobTracker[this.pawn].mainJob.targetQueueB.FirstIndexOf(x => x.Thing == this.savedThing);
                                                Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ZLogger.Message(\"2 Removing \" + ZTracker.jobTracker[this.pawn].mainJob.targetQueueB[ind] + \" from \" + ZTracker.jobTracker[this.pawn].mainJob); - 81", true);
                                                ZLogger.Message("2 Removing " + ZTracker.jobTracker[this.pawn].mainJob.targetQueueB[ind] + " from " + ZTracker.jobTracker[this.pawn].mainJob);

                                                ZTracker.jobTracker[this.pawn].mainJob.targetQueueB.RemoveAt(ind);
                                                Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ZTracker.jobTracker[this.pawn].mainJob.countQueue.RemoveAt(ind); - 83", true);
                                                ZTracker.jobTracker[this.pawn].mainJob.countQueue.RemoveAt(ind);
                                                Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - break; - 84", true);
                                                break;
                                            }
                                            else
                                            {
                                                Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ZLogger.Message(\"Cant add  \" + TargetB.Thing + \" to \" + ZTracker.jobTracker[this.pawn].mainJob); - 85", true);
                                                ZLogger.Message("Cant add  " + TargetB.Thing + " to " + ZTracker.jobTracker[this.pawn].mainJob);

                                            }
                                        }
                                    }
                                }

                                try
                                {
                                    Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ZLogger.Message(\"--------------------------\"); - 86", true);
                                    ZLogger.Message("--------------------------");
                                    for (int i = ZTracker.jobTracker[pawn].mainJob.targetQueueB.Count - 1; i >= 0; i--)
                                    {
                                        Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - var target = ZTracker.jobTracker[pawn].mainJob.targetQueueB[i]; - 87", true);
                                        var target = ZTracker.jobTracker[pawn].mainJob.targetQueueB[i];

                                        ZLogger.Message("JobDriver_HaulThingToStairs AFTER job.targetQueueB: " + target.Thing);
                                        Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ZLogger.Message(\"JobDriver_HaulThingToStairs AFTER job.targetQueueB.Map: \" + target.Thing.Map); - 89", true);
                                        ZLogger.Message("JobDriver_HaulThingToStairs AFTER job.targetQueueB.Map: " + target.Thing.Map);
                                        Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ZLogger.Message(\"JobDriver_HaulThingToStairs AFTER job.targetQueueB.stackCount: \" + target.Thing.stackCount); - 90", true);
                                        ZLogger.Message("JobDriver_HaulThingToStairs AFTER job.targetQueueB.stackCount: " + target.Thing.stackCount);
                                        Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ZLogger.Message(\"JobDriver_HaulThingToStairs AFTER job.targetQueueB.countQueue: \" + ZTracker.jobTracker[pawn].mainJob.countQueue[i]); - 91", true);
                                        ZLogger.Message("JobDriver_HaulThingToStairs AFTER job.targetQueueB.countQueue: " + ZTracker.jobTracker[pawn].mainJob.countQueue[i]);

                                    }
                                }
                                catch { }
                            }
                            catch { }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - Log.Error(\"Z-Tracker produced an error in JobDriver_HaulThingToStairs class. Report about it to devs. Error: \" + ex); - 92", true);
                        Log.Error("Z-Tracker produced an error in JobDriver_HaulThingToStairs class. Report about it to devs. Error: " + ex);
                    }
                }
            };

            yield return new Toil()
            {
                initAction = () =>
                {
                    Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - var ZTracker = Current.Game.GetComponent<ZLevelsManager>(); - 94", true);
                    var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                    Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - Pawn pawn = GetActor(); - 95", true);
                    Pawn pawn = GetActor();
                    Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - if (TargetA.Thing is Building_StairsUp stairsUp) - 96", true);
                    if (TargetA.Thing is Building_StairsUp stairsUp)
                    {

                        Map map = ZTracker.GetUpperLevel(this.pawn.Map.Tile, this.pawn.Map);
                        Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - if (map == null) - 98", true);
                        if (map == null)
                        {
                            Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - map = ZTracker.CreateUpperLevel(this.pawn.Map, stairsUp.Position); - 99", true);
                            map = ZTracker.CreateUpperLevel(this.pawn.Map, stairsUp.Position);
                            Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - if (stairsUp.pathToPreset != null && stairsUp.pathToPreset.Length > 0) - 100", true);
                            if (stairsUp.pathToPreset != null && stairsUp.pathToPreset.Length > 0)
                            {
                                Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - var comp = map.GetComponent<MapComponentZLevel>(); - 101", true);
                                var comp = map.GetComponent<MapComponentZLevel>();
                                Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - comp.DoGeneration = true; - 102", true);
                                comp.DoGeneration = true;
                                Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - comp.path = stairsUp.pathToPreset; - 103", true);
                                comp.path = stairsUp.pathToPreset;
                            }
                            ZTracker.TeleportPawn(pawn, pawn.Position, map, true, false, stairsUp.shouldSpawnStairsUpper);
                            Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - stairsUp.shouldSpawnStairsUpper = false; - 105", true);
                            stairsUp.shouldSpawnStairsUpper = false;
                        }
                        else
                        {
                            Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - if (stairsUp.pathToPreset != null && stairsUp.pathToPreset.Length > 0) - 106", true);
                            if (stairsUp.pathToPreset != null && stairsUp.pathToPreset.Length > 0)
                            {
                                Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - var comp = map.GetComponent<MapComponentZLevel>(); - 107", true);
                                var comp = map.GetComponent<MapComponentZLevel>();
                                Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - comp.DoGeneration = true; - 108", true);
                                comp.DoGeneration = true;
                                Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - comp.path = stairsUp.pathToPreset; - 109", true);
                                comp.path = stairsUp.pathToPreset;
                            }
                            ZTracker.TeleportPawn(pawn, pawn.Position, map, false, false, stairsUp.shouldSpawnStairsUpper);
                            Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - stairsUp.shouldSpawnStairsUpper = false; - 111", true);
                            stairsUp.shouldSpawnStairsUpper = false;
                        }
                    }
                    Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - if (TargetA.Thing is Building_StairsDown stairsDown) - 112", true);
                    if (TargetA.Thing is Building_StairsDown stairsDown)
                    {
                        Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - Map map = ZTracker.GetLowerLevel(this.pawn.Map.Tile, this.pawn.Map); - 113", true);
                        Map map = ZTracker.GetLowerLevel(this.pawn.Map.Tile, this.pawn.Map);
                        Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - if (map == null) - 114", true);
                        if (map == null)
                        {
                            Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - map = ZTracker.CreateLowerLevel(this.pawn.Map, stairsDown.Position); - 115", true);
                            map = ZTracker.CreateLowerLevel(this.pawn.Map, stairsDown.Position);
                            Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - if (stairsDown.pathToPreset != null && stairsDown.pathToPreset.Length > 0) - 116", true);
                            if (stairsDown.pathToPreset != null && stairsDown.pathToPreset.Length > 0)
                            {
                                Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - var comp = map.GetComponent<MapComponentZLevel>(); - 117", true);
                                var comp = map.GetComponent<MapComponentZLevel>();
                                Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - comp.DoGeneration = true; - 118", true);
                                comp.DoGeneration = true;
                                Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - comp.path = stairsDown.pathToPreset; - 119", true);
                                comp.path = stairsDown.pathToPreset;
                            }
                            ZTracker.TeleportPawn(pawn, pawn.Position, map);
                        }
                        else
                        {
                            Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - if (stairsDown.pathToPreset != null && stairsDown.pathToPreset.Length > 0) - 121", true);
                            if (stairsDown.pathToPreset != null && stairsDown.pathToPreset.Length > 0)
                            {
                                Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - var comp = map.GetComponent<MapComponentZLevel>(); - 122", true);
                                var comp = map.GetComponent<MapComponentZLevel>();
                                Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - comp.DoGeneration = true; - 123", true);
                                comp.DoGeneration = true;
                                Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - comp.path = stairsDown.pathToPreset; - 124", true);
                                comp.path = stairsDown.pathToPreset;
                            }
                            ZTracker.TeleportPawn(pawn, pawn.Position, map);
                        }
                    }

                    try
                    {
                        for (int i = ZTracker.jobTracker[pawn].mainJob.targetQueueB.Count - 1; i >= 0; i--)
                        {
                            Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - var target = ZTracker.jobTracker[pawn].mainJob.targetQueueB[i]; - 126", true);
                            var target = ZTracker.jobTracker[pawn].mainJob.targetQueueB[i];
                            Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ZLogger.Message(\"JobDriver_HaulThingToStairs job.targetQueueB: \" + target.Thing); - 127", true);
                            ZLogger.Message("JobDriver_HaulThingToStairs job.targetQueueB: " + target.Thing);
                            Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ZLogger.Message(\"JobDriver_HaulThingToStairs job.targetQueueB.Map: \" + target.Thing.Map); - 128", true);
                            ZLogger.Message("JobDriver_HaulThingToStairs job.targetQueueB.Map: " + target.Thing.Map);
                            Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ZLogger.Message(\"JobDriver_HaulThingToStairs job.targetQueueB.stackCount: \" + target.Thing.stackCount); - 129", true);
                            ZLogger.Message("JobDriver_HaulThingToStairs job.targetQueueB.stackCount: " + target.Thing.stackCount);
                            Log.Message("JobDriver_HaulThingToStairs : JobDriver - MakeNewToils - ZLogger.Message(\"JobDriver_HaulThingToStairs job.targetQueueB.countQueue: \" + ZTracker.jobTracker[pawn].mainJob.countQueue[i]); - 130", true);
                            ZLogger.Message("JobDriver_HaulThingToStairs job.targetQueueB.countQueue: " + ZTracker.jobTracker[pawn].mainJob.countQueue[i]);
                        }
                    }
                    catch { }
                }
            };

            yield return new Toil { initAction = delegate () { Log.Message("END JobDriver_HaulThingToStairs", true); } };

        }
    }
}

