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
                    this.savedThing = this.TargetB.Thing;
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
                    this.pawn.jobs.curJob.count = Mathf.Min(TargetB.Thing.stackCount, (int)(pawn.GetStatValue(StatDefOf.CarryingCapacity, true) / TargetB.Thing.def.VolumePerUnit));
                    if (this.pawn.jobs.curJob.count < 0)
                    {
                        this.pawn.jobs.curJob.count = TargetB.Thing.stackCount;
                    }
                    //this.pawn.jobs.curJob.count = TargetB.Thing.stackCount;
                }
            };
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, true, false);
            yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveTargetA, TargetIndex.B, TargetIndex.A,
                false, null);
            yield return new Toil
            {
                initAction = delegate ()
                {
                    try
                    {
                        var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                        if (ZTracker.jobTracker.ContainsKey(pawn))
                        {
                            ZLogger.Message("Analyzing: " + pawn);
                            ZLogger.Message("Main thing: " + TargetB.Thing);
                            ZLogger.Message("Saved thing: " + this.savedThing);
                            ZLogger.Message("Carrying thing: " + pawn.carryTracker?.CarriedThing);
                            ZLogger.Message("Main job: " + ZTracker.jobTracker[pawn].mainJob);
                            ZLogger.Message("job.targetA.Thing: " + ZTracker.jobTracker[pawn].mainJob.targetA.Thing);
                            ZLogger.Message("job.targetB.Thing: " + ZTracker.jobTracker[pawn].mainJob.targetB.Thing);
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
                                    ZLogger.Message("job.targetQueueA: " + target.Thing);
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
                                for (int i = ZTracker.jobTracker[pawn].mainJob.targetQueueB.Count - 1; i >= 0; i--)
                                {
                                    var target = ZTracker.jobTracker[pawn].mainJob.targetQueueB[i];
                                    ZLogger.Message("job.targetQueueB: " + target.Thing);

                                    ZLogger.Message("target.Thing != null: " + (target.Thing != null).ToString());
                                    ZLogger.Message("target.Thing: " + target.Thing);
                                    ZLogger.Message("this.savedThing: " + this.savedThing);
                                    ZLogger.Message("target.Thing == this.savedThing: " + (target.Thing == this.savedThing).ToString());
                                    ZLogger.Message("target.Thing != TargetB.Thing: " + (target.Thing != TargetB.Thing).ToString());


                                    if (target.Thing != null && target.Thing == this.savedThing && target.Thing != TargetB.Thing)
                                    {
                                        ZLogger.Message("4 Pawns carried thing not the same: " + target.Thing);
                                        ZTracker.jobTracker[pawn].mainJob.targetQueueB[i] = new LocalTargetInfo(TargetB.Thing);
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
                        ZLogger.Message("1 lastTick");
                        ZTracker.jobTracker[pawn].lastTickFood = Find.TickManager.TicksGame + 201;
                        ZTracker.jobTracker[pawn].lastTickJoy = Find.TickManager.TicksGame + 201;
                    }
                    catch { };
                }
            };
        }
    }
}

