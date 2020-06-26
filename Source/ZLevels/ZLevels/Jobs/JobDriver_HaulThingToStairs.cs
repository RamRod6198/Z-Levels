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
            return this.pawn.Reserve(TargetB, job, 1, -1, null, errorOnFailed);
        }

        public Thing savedThing = null;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return new Toil
            {
                initAction = delegate ()
                {
                    var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                    if (ZTracker.jobTracker.ContainsKey(pawn))
                    {
                        this.savedThing = this.TargetB.Thing;
                    }
                }
            };
            this.FailOnDestroyedOrNull(TargetIndex.B);
            this.FailOnBurningImmobile(TargetIndex.B);
            this.FailOnForbidden(TargetIndex.B);
            Toil reserveTargetA = Toils_Reserve.Reserve(TargetIndex.B, 1, -1, null);
            yield return reserveTargetA;
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);
            yield return new Toil
            {
                initAction = delegate ()
                {
                    if (this.pawn.jobs.curJob.count == -1)
                    {
                        this.pawn.jobs.curJob.count = Mathf.Min(TargetB.Thing.stackCount, (int)(pawn.GetStatValue(StatDefOf.CarryingCapacity, true) / TargetB.Thing.def.VolumePerUnit));
                        if (this.pawn.jobs.curJob.count < 0)
                        {
                            this.pawn.jobs.curJob.count = TargetB.Thing.stackCount;
                        }
                    }
                    ZLogger.Message(this.pawn + " haul count: " + this.pawn.jobs.curJob.count);
                }
            };
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, true, false);
            yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveTargetA, TargetIndex.B, TargetIndex.A,
                false, null);
            Toil carryToCell = Toils_Haul.CarryHauledThingToCell(TargetIndex.A);
            yield return carryToCell;
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell);

            Toil useStairs = Toils_General.Wait(60, 0);
            ToilEffects.WithProgressBarToilDelay(useStairs, TargetIndex.A, false, -0.5f);
            ToilFailConditions.FailOnDespawnedNullOrForbidden<Toil>(useStairs, TargetIndex.A);
            ToilFailConditions.FailOnCannotTouch<Toil>(useStairs, TargetIndex.A, PathEndMode.OnCell);
            //yield return new Toil
            //{
            //    initAction = delegate () {
            //        ZLogger.Message("this.pawn.CanReachImmediate(TargetA.Thing, PathEndMode.OnCell): "
            //            + this.pawn.CanReachImmediate(TargetA.Thing, PathEndMode.OnCell), true);
            //    }
            //};
            yield return useStairs;

            yield return new Toil
            {
                initAction = delegate ()
                {
                    try
                    {
                        var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                        if (ZTracker.jobTracker.ContainsKey(pawn))
                        {
                            if (ZTracker.jobTracker[pawn].mainJob.targetA.Thing != null
                            && ZTracker.jobTracker[pawn].mainJob.targetA.Thing == this.savedThing
                            && ZTracker.jobTracker[pawn].mainJob.targetA.Thing != TargetB.Thing)
                            {
                                ZLogger.Message("1 Pawns carried thing not the same: " + ZTracker.jobTracker[pawn].mainJob.targetA.Thing);
                                ZTracker.jobTracker[pawn].mainJob.targetA = new LocalTargetInfo(TargetB.Thing);
                            }
                            else if (ZTracker.jobTracker[pawn].mainJob.targetB.Thing != null
                            && ZTracker.jobTracker[pawn].mainJob.targetB.Thing == this.savedThing
                            && ZTracker.jobTracker[pawn].mainJob.targetB.Thing != TargetB.Thing)
                            {
                                ZLogger.Message("2 Pawns carried thing not the same: " + ZTracker.jobTracker[pawn].mainJob.targetB.Thing);
                                ZTracker.jobTracker[pawn].mainJob.targetB = new LocalTargetInfo(TargetB.Thing);
                            }

                            try
                            {
                                for (int i = ZTracker.jobTracker[pawn].mainJob.targetQueueA.Count - 1; i >= 0; i--)
                                {
                                    var target = ZTracker.jobTracker[pawn].mainJob.targetQueueA[i];
                                    if (target.Thing != null && target.Thing == this.savedThing && target.Thing != TargetB.Thing)
                                    {
                                        ZLogger.Message("3 Pawns carried thing not the same: " + target.Thing);
                                        ZTracker.jobTracker[pawn].mainJob.targetQueueA[i] = new LocalTargetInfo(TargetB.Thing);
                                    }
                                }
                            }
                            catch { }
                            try
                            {

                                try
                                {
                                    ZLogger.Message("--------------------------");
                                    for (int i = ZTracker.jobTracker[pawn].mainJob.targetQueueB.Count - 1; i >= 0; i--)
                                    {
                                        var target = ZTracker.jobTracker[pawn].mainJob.targetQueueB[i];
                                        ZLogger.Message("JobDriver_HaulThingToStairs BEFORE job.targetQueueB: " + target.Thing);
                                        ZLogger.Message("JobDriver_HaulThingToStairs BEFORE job.targetQueueB.Map: " + target.Thing.Map);
                                        ZLogger.Message("JobDriver_HaulThingToStairs BEFORE job.targetQueueB.stackCount: " + target.Thing.stackCount);
                                        ZLogger.Message("JobDriver_HaulThingToStairs BEFORE job.targetQueueB.countQueue: " + ZTracker.jobTracker[pawn].mainJob.countQueue[i]);
                                    }
                                }
                                catch { }

                                for (int i = ZTracker.jobTracker[pawn].mainJob.targetQueueB.Count - 1; i >= 0; i--)
                                {
                                    var target = ZTracker.jobTracker[pawn].mainJob.targetQueueB[i];
                                    if (target.Thing != null && target.Thing == this.savedThing && target.Thing != TargetB.Thing)
                                    {
                                        ZLogger.Message("4 Pawns carried thing not the same");
                                        ZLogger.Message("4 Pawns target.Thing: " + target.Thing);
                                        ZLogger.Message("4 Pawns target.Thing.Map: " + target.Thing.Map);
                                        ZLogger.Message("4 Pawns this.savedThing: " + this.savedThing);
                                        ZLogger.Message("4 Pawns this.savedThing.Map: " + this.savedThing.Map);
                                        ZLogger.Message("4 Pawns TargetB.Thing: " + TargetB.Thing);
                                        ZLogger.Message("4 Pawns TargetB.Thing.Map: " + TargetB.Thing.Map);

                                        //ZLogger.Message("Replacing " + ZTracker.jobTracker[this.pawn].mainJob.targetQueueB[i] + " by " + TargetB);
                                        //
                                        //ZTracker.jobTracker[pawn].mainJob.targetQueueB[i] = new LocalTargetInfo(TargetB.Thing);
                                        //ZTracker.jobTracker[pawn].mainJob.countQueue[i] = TargetB.Thing.stackCount;
                                        //

                                        if (ZTracker.jobTracker[pawn].mainJob.targetQueueB[i].Thing.stackCount == 0)
                                        {
                                            ZTracker.jobTracker[pawn].mainJob.targetQueueB[i] = new LocalTargetInfo(TargetB.Thing);
                                            ZTracker.jobTracker[pawn].mainJob.countQueue[i] = TargetB.Thing.stackCount;
                                            ZLogger.Message("Altering " + ZTracker.jobTracker[this.pawn].mainJob);
                                            break;
                                        
                                        }
                                        else
                                        {
                                            if (ZTracker.jobTracker[pawn].mainJob.targetQueueB
                                            .Where(x => x.Thing == TargetB.Thing).Count() == 0)
                                            {
                                                var newTarget = new LocalTargetInfo(TargetB.Thing);
                                                ZTracker.jobTracker[this.pawn].mainJob.targetQueueB.Add(newTarget);
                                                ZTracker.jobTracker[this.pawn].mainJob.countQueue.Add(newTarget.Thing.stackCount);
                                                ZLogger.Message("2 Adding " + newTarget + " to " + ZTracker.jobTracker[this.pawn].mainJob);
                                                int ind = ZTracker.jobTracker[this.pawn].mainJob.targetQueueB.FirstIndexOf(x => x.Thing == this.savedThing);
                                                ZLogger.Message("2 Removing " + ZTracker.jobTracker[this.pawn].mainJob.targetQueueB[ind] + " from " + ZTracker.jobTracker[this.pawn].mainJob);
                                                
                                                ZTracker.jobTracker[this.pawn].mainJob.targetQueueB.RemoveAt(ind);
                                                ZTracker.jobTracker[this.pawn].mainJob.countQueue.RemoveAt(ind);
                                                break;
                                            }
                                            else
                                            {
                                                ZLogger.Message("Cant add  " + TargetB.Thing + " to " + ZTracker.jobTracker[this.pawn].mainJob);
                                        
                                            }
                                        }
                                    }
                                }

                                try
                                {
                                    ZLogger.Message("--------------------------");
                                    for (int i = ZTracker.jobTracker[pawn].mainJob.targetQueueB.Count - 1; i >= 0; i--)
                                    {
                                        var target = ZTracker.jobTracker[pawn].mainJob.targetQueueB[i];

                                        ZLogger.Message("JobDriver_HaulThingToStairs AFTER job.targetQueueB: " + target.Thing);
                                        ZLogger.Message("JobDriver_HaulThingToStairs AFTER job.targetQueueB.Map: " + target.Thing.Map);
                                        ZLogger.Message("JobDriver_HaulThingToStairs AFTER job.targetQueueB.stackCount: " + target.Thing.stackCount);
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
                        Log.Error("Z-Tracker produced an error in JobDriver_HaulThingToStairs class. Report about it to devs. Error: " + ex);
                    }
                }
            };

            yield return new Toil()
            {
                initAction = () =>
                {
                    var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                    Pawn pawn = GetActor();
                    if (TargetA.Thing is Building_StairsUp stairsUp)
                    {

                        Map map = ZTracker.GetUpperLevel(this.pawn.Map.Tile, this.pawn.Map);
                        if (map == null)
                        {
                            map = ZTracker.CreateUpperLevel(this.pawn.Map, stairsUp.Position);
                            if (stairsUp.pathToPreset != null && stairsUp.pathToPreset.Length > 0)
                            {
                                var comp = map.GetComponent<MapComponentZLevel>();
                                comp.DoGeneration = true;
                                comp.path = stairsUp.pathToPreset;
                            }
                            ZTracker.TeleportPawn(pawn, pawn.Position, map, true, false, stairsUp.shouldSpawnStairsUpper);
                            stairsUp.shouldSpawnStairsUpper = false;
                        }
                        else
                        {
                            if (stairsUp.pathToPreset != null && stairsUp.pathToPreset.Length > 0)
                            {
                                var comp = map.GetComponent<MapComponentZLevel>();
                                comp.DoGeneration = true;
                                comp.path = stairsUp.pathToPreset;
                            }
                            ZTracker.TeleportPawn(pawn, pawn.Position, map, false, false, stairsUp.shouldSpawnStairsUpper);
                            stairsUp.shouldSpawnStairsUpper = false;
                        }
                    }
                    if (TargetA.Thing is Building_StairsDown stairsDown)
                    {
                        Map map = ZTracker.GetLowerLevel(this.pawn.Map.Tile, this.pawn.Map);
                        if (map == null)
                        {
                            map = ZTracker.CreateLowerLevel(this.pawn.Map, stairsDown.Position);
                            if (stairsDown.pathToPreset != null && stairsDown.pathToPreset.Length > 0)
                            {
                                var comp = map.GetComponent<MapComponentZLevel>();
                                comp.DoGeneration = true;
                                comp.path = stairsDown.pathToPreset;
                            }
                            ZTracker.TeleportPawn(pawn, pawn.Position, map);
                        }
                        else
                        {
                            if (stairsDown.pathToPreset != null && stairsDown.pathToPreset.Length > 0)
                            {
                                var comp = map.GetComponent<MapComponentZLevel>();
                                comp.DoGeneration = true;
                                comp.path = stairsDown.pathToPreset;
                            }
                            ZTracker.TeleportPawn(pawn, pawn.Position, map);
                        }
                    }

                    try
                    {
                        for (int i = ZTracker.jobTracker[pawn].mainJob.targetQueueB.Count - 1; i >= 0; i--)
                        {
                            var target = ZTracker.jobTracker[pawn].mainJob.targetQueueB[i];
                            ZLogger.Message("JobDriver_HaulThingToStairs job.targetQueueB: " + target.Thing);
                            ZLogger.Message("JobDriver_HaulThingToStairs job.targetQueueB.Map: " + target.Thing.Map);
                            ZLogger.Message("JobDriver_HaulThingToStairs job.targetQueueB.stackCount: " + target.Thing.stackCount);
                            ZLogger.Message("JobDriver_HaulThingToStairs job.targetQueueB.countQueue: " + ZTracker.jobTracker[pawn].mainJob.countQueue[i]);
                        }
                    }
                    catch { }
                }
            };
        }
    }
}

