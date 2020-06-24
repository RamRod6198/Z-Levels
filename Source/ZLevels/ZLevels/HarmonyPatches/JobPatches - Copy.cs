using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ZLevels
{
    public class JobDriver_DoBillZLevels : JobDriver
    {
        public float workLeft;

        public int billStartTick;

        public int ticksSpentDoingRecipeWork;

        public const PathEndMode GotoIngredientPathEndMode = PathEndMode.ClosestTouch;

        public const TargetIndex BillGiverInd = TargetIndex.A;

        public const TargetIndex IngredientInd = TargetIndex.B;

        public const TargetIndex IngredientPlaceCellInd = TargetIndex.C;

        public IBillGiver BillGiver => (job.GetTarget(TargetIndex.A).Thing as IBillGiver) ?? throw new InvalidOperationException("DoBill on non-Billgiver.");

        public override string GetReport()
        {
            Log.Message(this.pawn + " - JobDriver_DoBillZLevels : JobDriver - GetReport - if (job.RecipeDef != null) - 1", true);
            if (job.RecipeDef != null)
            {
                Log.Message(this.pawn + " - JobDriver_DoBillZLevels : JobDriver - GetReport - return ReportStringProcessed(job.RecipeDef.jobString); - 2", true);
                return ReportStringProcessed(job.RecipeDef.jobString);
            }
            return base.GetReport();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref workLeft, "workLeft", 0f);
            Scribe_Values.Look(ref billStartTick, "billStartTick", 0);
            Scribe_Values.Look(ref ticksSpentDoingRecipeWork, "ticksSpentDoingRecipeWork", 0);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Log.Message(this.pawn + " - JobDriver_DoBillZLevels : JobDriver - TryMakePreToilReservations - if (!pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed)) - 8", true);
            if (!pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed))
            {
                Log.Message(this.pawn + " - JobDriver_DoBillZLevels : JobDriver - TryMakePreToilReservations - return false; - 9", true);
                return false;
            }
            pawn.ReserveAsManyAsPossible(job.GetTargetQueue(TargetIndex.B), job);
            Log.Message(this.pawn + " - JobDriver_DoBillZLevels : JobDriver - TryMakePreToilReservations - return true; - 11", true);
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Toil toil = new Toil();

            yield return new Toil
            {
                initAction = delegate ()
                {

                    ZLogger.Message("-1 toil.actor: " + toil.actor);
                    ZLogger.Message("-1 toil.actor.Position: " + toil.actor.Position);
                    ZLogger.Message("-1 toil.actor.Map: " + toil.actor.Map);
                    ZLogger.Message("-1 TargetB.Thing: " + TargetB.Thing);
                    ZLogger.Message("-1 TargetB.Thing.Position: " + TargetB.Thing?.Position);
                    ZLogger.Message("-1 TargetB.Thing.Map: " + TargetB.Thing?.Map);

                    ZLogger.Message(toil.actor + " - checking reservations on: " + TargetB.Thing, true);
                    ZLogger.Message("-1 pawn.Map.physicalInteractionReservationManager.FirstReserverOf(target): " + toil.actor.Map.physicalInteractionReservationManager.FirstReserverOf(TargetB));
                    ZLogger.Message("-1 TargetB.Thing == null: " + (TargetB.Thing == null).ToString());
                    ZLogger.Message("-1 !toil.actor.Map.physicalInteractionReservationManager.IsReserved(TargetB.Thing): " + (!toil.actor.Map.physicalInteractionReservationManager.IsReserved(TargetB.Thing)).ToString());
                    ZLogger.Message("-1 toil.actor.Map.physicalInteractionReservationManager.IsReservedBy(toil.actor, TargetB.Thing)): " + (toil.actor.Map.physicalInteractionReservationManager.IsReservedBy(toil.actor, TargetB.Thing)).ToString());
                    ZLogger.Message("-1 failcondition" + ((TargetB == null
                        || !toil.actor.Map.physicalInteractionReservationManager.IsReserved(TargetB)
                        || toil.actor.Map.physicalInteractionReservationManager.IsReservedBy(toil.actor, TargetB))
                        ? JobCondition.Ongoing : JobCondition.Incompletable).ToString());
                    try
                    {
                        for (int i = job.targetQueueA.Count - 1; i >= 0; i--)
                        {
                            var target = job.targetQueueA[i];
                            ZLogger.Message("-1 job.targetQueueA: " + target.Thing);
                            ZLogger.Message("-1 job.targetQueueA: " + target.Thing.Map);

                        }
                    }
                    catch { }
                    try
                    {
                        for (int i = job.targetQueueB.Count - 1; i >= 0; i--)
                        {
                            var target = job.targetQueueB[i];
                            ZLogger.Message("-1 job.targetQueueB: " + target.Thing);
                            ZLogger.Message("-1 job.targetQueueB.Map: " + target.Thing.Map);
                            ZLogger.Message("-1 job.targetQueueB.stackCount: " + target.Thing.stackCount);
                        }
                    }
                    catch { }
                }
            };

            //AddEndCondition(delegate
            //{
            //    Thing thing = GetActor().jobs.curJob.GetTarget(TargetIndex.A).Thing;
            //    Log.Message(this.pawn + " - 1 failcondition: " + ((!(thing is Building) || thing.Spawned) ? JobCondition.Ongoing : JobCondition.Incompletable).ToString(), true);
            //    return (!(thing is Building) || thing.Spawned) ? JobCondition.Ongoing : JobCondition.Incompletable;
            //});
            //this.FailOnBurningImmobile(TargetIndex.A);
            //this.FailOn(delegate
            //{
            //    IBillGiver billGiver = job.GetTarget(TargetIndex.A).Thing as IBillGiver;
            //    if (billGiver != null)
            //    {
            //        if (job.bill.DeletedOrDereferenced)
            //        {
            //            Log.Message(this.pawn + " - JobDriver_DoBillZLevels : JobDriver - MakeNewToils - return true; - 19", true);
            //            return true;
            //        }
            //        if (!billGiver.CurrentlyUsableForBills())
            //        {
            //            Log.Message(this.pawn + " - JobDriver_DoBillZLevels : JobDriver - MakeNewToils - return true; - 21", true);
            //            return true;
            //        }
            //    }
            //    return false;
            //});
            Toil gotoBillGiver = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            toil.initAction = delegate
            {
                Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - MakeNewToils - if (job.targetQueueB != null && job.targetQueueB.Count == 1) - 26", true);
                if (job.targetQueueB != null && job.targetQueueB.Count == 1)
                {
                    Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - MakeNewToils - UnfinishedThing unfinishedThing = job.targetQueueB[0].Thing as UnfinishedThing; - 27", true);
                    UnfinishedThing unfinishedThing = job.targetQueueB[0].Thing as UnfinishedThing;
                    Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - MakeNewToils - if (unfinishedThing != null) - 28", true);
                    if (unfinishedThing != null)
                    {
                        Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - MakeNewToils - unfinishedThing.BoundBill = (Bill_ProductionWithUft)job.bill; - 29", true);
                        unfinishedThing.BoundBill = (Bill_ProductionWithUft)job.bill;
                    }
                }
            };
            yield return new Toil { initAction = delegate () { Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - MakeNewToils - yield return toil; - 31", true); } };
            yield return toil;
            yield return new Toil
            {
                initAction = delegate ()
                {

                    ZLogger.Message("1 toil.actor: " + toil.actor);
                    ZLogger.Message("1 toil.actor.Position: " + toil.actor.Position);
                    ZLogger.Message("1 toil.actor.Map: " + toil.actor.Map);
                    ZLogger.Message("1 TargetB.Thing: " + TargetB.Thing);
                    ZLogger.Message("1 TargetB.Thing.Position: " + TargetB.Thing?.Position);
                    ZLogger.Message("1 TargetB.Thing.Map: " + TargetB.Thing?.Map);

                    ZLogger.Message(toil.actor + " - checking reservations on: " + TargetB.Thing, true);
                    ZLogger.Message("1 pawn.Map.physicalInteractionReservationManager.FirstReserverOf(target): " + toil.actor.Map.physicalInteractionReservationManager.FirstReserverOf(TargetB));
                    ZLogger.Message("1 TargetB.Thing == null: " + (TargetB.Thing == null).ToString());
                    ZLogger.Message("1 !toil.actor.Map.physicalInteractionReservationManager.IsReserved(TargetB.Thing): " + (!toil.actor.Map.physicalInteractionReservationManager.IsReserved(TargetB.Thing)).ToString());
                    ZLogger.Message("1 toil.actor.Map.physicalInteractionReservationManager.IsReservedBy(toil.actor, TargetB.Thing)): " + (toil.actor.Map.physicalInteractionReservationManager.IsReservedBy(toil.actor, TargetB.Thing)).ToString());
                    ZLogger.Message("1 failcondition" + ((TargetB == null
                        || !toil.actor.Map.physicalInteractionReservationManager.IsReserved(TargetB)
                        || toil.actor.Map.physicalInteractionReservationManager.IsReservedBy(toil.actor, TargetB))
                        ? JobCondition.Ongoing : JobCondition.Incompletable).ToString());
                    try
                    {
                        for (int i = job.targetQueueA.Count - 1; i >= 0; i--)
                        {
                            var target = job.targetQueueA[i];
                            ZLogger.Message("1 job.targetQueueA: " + target.Thing);
                            ZLogger.Message("1 job.targetQueueA: " + target.Thing.Map);
                            ZLogger.Message("1 job.targetQueueA: " + target.Thing.stackCount);

                        }
                    }
                    catch { }
                    try
                    {
                        for (int i = job.targetQueueB.Count - 1; i >= 0; i--)
                        {
                            var target = job.targetQueueB[i];
                            ZLogger.Message("1 job.targetQueueB: " + target.Thing);
                            ZLogger.Message("1 job.targetQueueB.Map: " + target.Thing.Map);
                            ZLogger.Message("1 job.targetQueueB.stackCount: " + target.Thing.stackCount);
                        }
                    }
                    catch { }
                }
            };

            yield return new Toil { initAction = delegate () { Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - MakeNewToils - yield return Toils_Jump.JumpIf(gotoBillGiver, () => job.GetTargetQueue(TargetIndex.B).NullOrEmpty()); - 32", true); } };
            yield return Toils_Jump.JumpIf(gotoBillGiver, () => job.GetTargetQueue(TargetIndex.B).NullOrEmpty());
            Toil extract = ExtractNextTargetFromQueue(TargetIndex.B);
            yield return new Toil { initAction = delegate () { Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - MakeNewToils - yield return extract; - 34", true); } };
            
            yield return new Toil
            {
                initAction = delegate ()
                {
                    
                    ZLogger.Message("2 toil.actor: " + toil.actor);
                    ZLogger.Message("2 toil.actor.Position: " + toil.actor.Position);
                    ZLogger.Message("2 toil.actor.Map: " + toil.actor.Map);
                    ZLogger.Message("2 TargetB.Thing: " + TargetB.Thing);
                    ZLogger.Message("2 TargetB.Thing.Position: " + TargetB.Thing?.Position);
                    ZLogger.Message("2 TargetB.Thing.Map: " + TargetB.Thing?.Map);

                    ZLogger.Message(toil.actor + " - checking reservations on: " + TargetB.Thing, true);
                    ZLogger.Message("2 pawn.Map.physicalInteractionReservationManager.FirstReserverOf(target): " + toil.actor.Map.physicalInteractionReservationManager.FirstReserverOf(TargetB));
                    ZLogger.Message("2 TargetB.Thing == null: " + (TargetB.Thing == null).ToString());
                    ZLogger.Message("2 !toil.actor.Map.physicalInteractionReservationManager.IsReserved(TargetB.Thing): " + (!toil.actor.Map.physicalInteractionReservationManager.IsReserved(TargetB.Thing)).ToString());
                    ZLogger.Message("2 toil.actor.Map.physicalInteractionReservationManager.IsReservedBy(toil.actor, TargetB.Thing)): " + (toil.actor.Map.physicalInteractionReservationManager.IsReservedBy(toil.actor, TargetB.Thing)).ToString());
                    ZLogger.Message("2 failcondition" + ((TargetB == null
                        || !toil.actor.Map.physicalInteractionReservationManager.IsReserved(TargetB)
                        || toil.actor.Map.physicalInteractionReservationManager.IsReservedBy(toil.actor, TargetB))
                        ? JobCondition.Ongoing : JobCondition.Incompletable).ToString());

                    try
                    {
                        for (int i = job.targetQueueA.Count - 1; i >= 0; i--)
                        {
                            var target = job.targetQueueA[i];
                            ZLogger.Message("2 job.targetQueueA: " + target.Thing);
                            ZLogger.Message("2 job.targetQueueA: " + target.Thing.Map);

                        }
                    }
                    catch { }
                    try
                    {
                        for (int i = job.targetQueueB.Count - 1; i >= 0; i--)
                        {
                            var target = job.targetQueueB[i];
                            ZLogger.Message("2 job.targetQueueB: " + target.Thing);
                            ZLogger.Message("2 job.targetQueueB.Map: " + target.Thing.Map);
                            ZLogger.Message("2 job.targetQueueB.stackCount: " + target.Thing.stackCount);
                        }
                    }
                    catch { }
                }
            };

            yield return extract;
            Toil getToHaulTarget = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);

            yield return new Toil { initAction = delegate () { Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - MakeNewToils - yield return getToHaulTarget; - 36", true); } };
            yield return new Toil
            {
                initAction = delegate ()
                {

                    ZLogger.Message("3 toil.actor: " + toil.actor);
                    ZLogger.Message("3 toil.actor.Position: " + toil.actor.Position);
                    ZLogger.Message("3 toil.actor.Map: " + toil.actor.Map);
                    ZLogger.Message("3 TargetB.Thing: " + TargetB.Thing);
                    ZLogger.Message("3 TargetB.Thing.Position: " + TargetB.Thing?.Position);
                    ZLogger.Message("3 TargetB.Thing.Map: " + TargetB.Thing?.Map);

                    ZLogger.Message(toil.actor + " - checking reservations on: " + TargetB.Thing, true);
                    ZLogger.Message("3 pawn.Map.physicalInteractionReservationManager.FirstReserverOf(target): " + toil.actor.Map.physicalInteractionReservationManager.FirstReserverOf(TargetB));
                    ZLogger.Message("3 TargetB.Thing == null: " + (TargetB.Thing == null).ToString());
                    ZLogger.Message("3 !toil.actor.Map.physicalInteractionReservationManager.IsReserved(TargetB.Thing): " + (!toil.actor.Map.physicalInteractionReservationManager.IsReserved(TargetB.Thing)).ToString());
                    ZLogger.Message("3 toil.actor.Map.physicalInteractionReservationManager.IsReservedBy(toil.actor, TargetB.Thing)): " + (toil.actor.Map.physicalInteractionReservationManager.IsReservedBy(toil.actor, TargetB.Thing)).ToString());
                    ZLogger.Message("3 failcondition" + ((TargetB == null
                        || !toil.actor.Map.physicalInteractionReservationManager.IsReserved(TargetB)
                        || toil.actor.Map.physicalInteractionReservationManager.IsReservedBy(toil.actor, TargetB))
                        ? JobCondition.Ongoing : JobCondition.Incompletable).ToString());
                    try
                    {
                        for (int i = job.targetQueueA.Count - 1; i >= 0; i--)
                        {
                            var target = job.targetQueueA[i];
                            ZLogger.Message("3 job.targetQueueA: " + target.Thing);
                            ZLogger.Message("3 job.targetQueueA: " + target.Thing.Map);

                        }
                    }
                    catch { }
                    try
                    {
                        for (int i = job.targetQueueB.Count - 1; i >= 0; i--)
                        {
                            var target = job.targetQueueB[i];
                            ZLogger.Message("3 job.targetQueueB: " + target.Thing);
                            ZLogger.Message("3 job.targetQueueB.Map: " + target.Thing.Map);
                            ZLogger.Message("3 job.targetQueueB.stackCount: " + target.Thing.stackCount);
                        }
                    }
                    catch { }
                }
            };
            yield return getToHaulTarget;
            yield return new Toil { initAction = delegate () { Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - MakeNewToils - yield return Toils_Haul.StartCarryThing(TargetIndex.B, putRemainderInQueue: true, subtractNumTakenFromJobCount: false, failIfStackCountLessThanJobCount: true); - 37", true); } };
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, putRemainderInQueue: true, subtractNumTakenFromJobCount: false, failIfStackCountLessThanJobCount: true);
            yield return new Toil { initAction = delegate () { Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - MakeNewToils - yield return JumpToCollectNextIntoHandsForBill(getToHaulTarget, TargetIndex.B); - 38", true); } };
            yield return JumpToCollectNextIntoHandsForBill(getToHaulTarget, TargetIndex.B);
            yield return new Toil { initAction = delegate () { Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - MakeNewToils - yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell).FailOnDestroyedOrNull(TargetIndex.B); - 39", true); } };
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell).FailOnDestroyedOrNull(TargetIndex.B);
            Toil findPlaceTarget2 = Toils_JobTransforms.SetTargetToIngredientPlaceCell(TargetIndex.A, TargetIndex.B, TargetIndex.C);
            yield return new Toil { initAction = delegate () { Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - MakeNewToils - yield return findPlaceTarget2; - 41", true); } };
            yield return findPlaceTarget2;
            yield return new Toil { initAction = delegate () { Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - MakeNewToils - yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.C, findPlaceTarget2, storageMode: false); - 42", true); } };
            yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.C, findPlaceTarget2, storageMode: false);
            yield return new Toil { initAction = delegate () { Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - MakeNewToils - yield return Toils_Jump.JumpIfHaveTargetInQueue(TargetIndex.B, extract); - 43", true); } };
            yield return Toils_Jump.JumpIfHaveTargetInQueue(TargetIndex.B, extract);
            yield return new Toil { initAction = delegate () { Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - MakeNewToils - yield return gotoBillGiver; - 44", true); } };
            yield return gotoBillGiver;
            yield return new Toil { initAction = delegate () { Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - MakeNewToils - yield return Toils_Recipe.MakeUnfinishedThingIfNeeded(); - 45", true); } };
            yield return Toils_Recipe.MakeUnfinishedThingIfNeeded();
            yield return new Toil { initAction = delegate () { Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - MakeNewToils - yield return Toils_Recipe.DoRecipeWork().FailOnDespawnedNullOrForbiddenPlacedThings().FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell); - 46", true); } };
            yield return DoRecipeWork().FailOnDespawnedNullOrForbiddenPlacedThings().FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
            yield return new Toil { initAction = delegate () { Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - MakeNewToils - yield return Toils_Recipe.FinishRecipeAndStartStoringProduct(); - 47", true); } };
            yield return FinishRecipeAndStartStoringProduct();
            if (!job.RecipeDef.products.NullOrEmpty() || !job.RecipeDef.specialProducts.NullOrEmpty())
            {
                yield return new Toil { initAction = delegate () { Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - MakeNewToils - yield return Toils_Reserve.Reserve(TargetIndex.B); - 49", true); } };
                yield return Toils_Reserve.Reserve(TargetIndex.B);
                Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - MakeNewToils - findPlaceTarget2 = Toils_Haul.CarryHauledThingToCell(TargetIndex.B); - 50", true);
                findPlaceTarget2 = Toils_Haul.CarryHauledThingToCell(TargetIndex.B);
                yield return new Toil { initAction = delegate () { Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - MakeNewToils - yield return findPlaceTarget2; - 51", true); } };
                yield return findPlaceTarget2;
                yield return new Toil { initAction = delegate () { Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - MakeNewToils - yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.B, findPlaceTarget2, storageMode: true, tryStoreInSameStorageIfSpotCantHoldWholeStack: true); - 52", true); } };
                yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.B, findPlaceTarget2, storageMode: true, tryStoreInSameStorageIfSpotCantHoldWholeStack: true);
                Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - MakeNewToils - Toil recount = new Toil(); - 53", true);
                Toil recount = new Toil();
                recount.initAction = delegate
                {
                    Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - MakeNewToils - Bill_Production bill_Production = recount.actor.jobs.curJob.bill as Bill_Production; - 54", true);
                    Bill_Production bill_Production = recount.actor.jobs.curJob.bill as Bill_Production;
                    Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - MakeNewToils - if (bill_Production != null && bill_Production.repeatMode == BillRepeatModeDefOf.TargetCount) - 55", true);
                    if (bill_Production != null && bill_Production.repeatMode == BillRepeatModeDefOf.TargetCount)
                    {
                        Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - MakeNewToils - base.Map.resourceCounter.UpdateResourceCounts(); - 56", true);
                        base.Map.resourceCounter.UpdateResourceCounts();
                    }
                };
                yield return new Toil { initAction = delegate () { Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - MakeNewToils - yield return recount; - 58", true); } };
                yield return recount;
            }
        }

        public static Toil ExtractNextTargetFromQueue(TargetIndex ind, bool failIfCountFromQueueTooBig = true)
        {
            Log.Message(" - ExtractNextTargetFromQueue - Toil toil = new Toil(); - 1", true);
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                Log.Message(" - ExtractNextTargetFromQueue - Pawn actor = toil.actor; - 2", true);
                Pawn actor = toil.actor;
                Log.Message(" - ExtractNextTargetFromQueue - Job curJob = actor.jobs.curJob; - 3", true);
                Job curJob = actor.jobs.curJob;
                Log.Message(" - ExtractNextTargetFromQueue - List<LocalTargetInfo> targetQueue = curJob.GetTargetQueue(ind); - 4", true);
                List<LocalTargetInfo> targetQueue = curJob.GetTargetQueue(ind);
                Log.Message(" - ExtractNextTargetFromQueue - if (!targetQueue.NullOrEmpty()) - 5", true);
                if (!targetQueue.NullOrEmpty())
                {
                    Log.Message(" - ExtractNextTargetFromQueue - if (failIfCountFromQueueTooBig && !curJob.countQueue.NullOrEmpty() && targetQueue[0].HasThing && curJob.countQueue[0] > targetQueue[0].Thing.stackCount) - 6", true);
                    if (failIfCountFromQueueTooBig && !curJob.countQueue.NullOrEmpty() && targetQueue[0].HasThing 
                    && curJob.countQueue[0] > targetQueue[0].Thing.stackCount)
                    {
                        Log.Message("targetQueue[0].Thing: " + targetQueue[0].Thing, true);
                        Log.Message("failIfCountFromQueueTooBig: " + failIfCountFromQueueTooBig, true);
                        Log.Message("!curJob.countQueue.NullOrEmpty(): " + (!curJob.countQueue.NullOrEmpty()).ToString(), true);
                        Log.Message("targetQueue[0].HasThing: " + (targetQueue[0].HasThing).ToString(), true);
                        Log.Message("curJob.countQueue[0] > targetQueue[0].Thing.stackCount: " + (curJob.countQueue[0] > targetQueue[0].Thing.stackCount).ToString(), true);
                        Log.Message("targetQueue[0].Thing.stackCount: " + (targetQueue[0].Thing.stackCount).ToString(), true);
                        Log.Message("curJob.countQueue[0]: " + (curJob.countQueue[0]).ToString(), true);
                        Log.Message(" - ExtractNextTargetFromQueue - actor.jobs.curDriver.EndJobWith(JobCondition.Incompletable); - 7", true);
                        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;

                        actor.jobs.curDriver.EndJobWith(JobCondition.Incompletable);
                    }
                    else
                    {
                        Log.Message(" - ExtractNextTargetFromQueue - curJob.SetTarget(ind, targetQueue[0]); - 8", true);
                        curJob.SetTarget(ind, targetQueue[0]);
                        Log.Message(" - ExtractNextTargetFromQueue - targetQueue.RemoveAt(0); - 9", true);
                        targetQueue.RemoveAt(0);
                        Log.Message(" - ExtractNextTargetFromQueue - if (!curJob.countQueue.NullOrEmpty()) - 10", true);
                        if (!curJob.countQueue.NullOrEmpty())
                        {
                            Log.Message(" - ExtractNextTargetFromQueue - curJob.count = curJob.countQueue[0]; - 11", true);
                            curJob.count = curJob.countQueue[0];
                            Log.Message(" - ExtractNextTargetFromQueue - curJob.countQueue.RemoveAt(0); - 12", true);
                            curJob.countQueue.RemoveAt(0);
                        }
                    }
                }
            };
            Log.Message(" - ExtractNextTargetFromQueue - return toil; - 14", true);
            return toil;
        }

        public static Toil DoRecipeWork()
        {
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                Pawn actor3 = toil.actor;
                Log.Message(toil.actor + " - DoRecipeWork - Job curJob3 = actor3.jobs.curJob; - 3", true);
                Job curJob3 = actor3.jobs.curJob;
                Log.Message(toil.actor + " - DoRecipeWork - JobDriver_DoBillZLevels jobDriver_DoBill2 = (JobDriver_DoBillZLevels)actor3.jobs.curDriver; - 4", true);
                JobDriver_DoBillZLevels jobDriver_DoBill2 = (JobDriver_DoBillZLevels)actor3.jobs.curDriver;
                Log.Message(toil.actor + " - DoRecipeWork - UnfinishedThing unfinishedThing3 = curJob3.GetTarget(TargetIndex.B).Thing as UnfinishedThing; - 5", true);
                UnfinishedThing unfinishedThing3 = curJob3.GetTarget(TargetIndex.B).Thing as UnfinishedThing;
                Log.Message(toil.actor + " - DoRecipeWork - if (unfinishedThing3 != null && unfinishedThing3.Initialized) - 6", true);
                if (unfinishedThing3 != null && unfinishedThing3.Initialized)
                {
                    Log.Message(toil.actor + " - DoRecipeWork - jobDriver_DoBill2.workLeft = unfinishedThing3.workLeft; - 7", true);
                    jobDriver_DoBill2.workLeft = unfinishedThing3.workLeft;
                }
                else
                {
                    Log.Message(toil.actor + " - DoRecipeWork - jobDriver_DoBill2.workLeft = curJob3.bill.recipe.WorkAmountTotal(unfinishedThing3?.Stuff); - 8", true);
                    jobDriver_DoBill2.workLeft = curJob3.bill.recipe.WorkAmountTotal(unfinishedThing3?.Stuff);
                    Log.Message(toil.actor + " - DoRecipeWork - if (unfinishedThing3 != null) - 9", true);
                    if (unfinishedThing3 != null)
                    {
                        Log.Message(toil.actor + " - DoRecipeWork - unfinishedThing3.workLeft = jobDriver_DoBill2.workLeft; - 10", true);
                        unfinishedThing3.workLeft = jobDriver_DoBill2.workLeft;
                    }
                }
                jobDriver_DoBill2.billStartTick = Find.TickManager.TicksGame;
                Log.Message(toil.actor + " - DoRecipeWork - jobDriver_DoBill2.ticksSpentDoingRecipeWork = 0; - 12", true);
                jobDriver_DoBill2.ticksSpentDoingRecipeWork = 0;
                Log.Message(toil.actor + " - DoRecipeWork - curJob3.bill.Notify_DoBillStarted(actor3); - 13", true);
                curJob3.bill.Notify_DoBillStarted(actor3);
                Log.Message(toil.actor + " - DoRecipeWork - }; - 14", true);
            };
            toil.tickAction = delegate
            {
                Pawn actor2 = toil.actor;
                Job curJob2 = actor2.jobs.curJob;
                JobDriver_DoBillZLevels jobDriver_DoBill = (JobDriver_DoBillZLevels)actor2.jobs.curDriver;
                UnfinishedThing unfinishedThing2 = curJob2.GetTarget(TargetIndex.B).Thing as UnfinishedThing;
                if (unfinishedThing2 != null && unfinishedThing2.Destroyed)
                {
                    actor2.jobs.EndCurrentJob(JobCondition.Incompletable);
                }
                else
                {
                    jobDriver_DoBill.ticksSpentDoingRecipeWork++;
                    curJob2.bill.Notify_PawnDidWork(actor2);
                    (toil.actor.CurJob.GetTarget(TargetIndex.A).Thing as IBillGiverWithTickAction)?.UsedThisTick();
                    if (curJob2.RecipeDef.workSkill != null && curJob2.RecipeDef.UsesUnfinishedThing)
                    {
                        actor2.skills.Learn(curJob2.RecipeDef.workSkill, 0.1f * curJob2.RecipeDef.workSkillLearnFactor);
                    }
                    float num = (curJob2.RecipeDef.workSpeedStat == null) ? 1f : actor2.GetStatValue(curJob2.RecipeDef.workSpeedStat);
                    if (curJob2.RecipeDef.workTableSpeedStat != null)
                    {
                        Building_WorkTable building_WorkTable = jobDriver_DoBill.BillGiver as Building_WorkTable;
                        if (building_WorkTable != null)
                        {
                            num *= building_WorkTable.GetStatValue(curJob2.RecipeDef.workTableSpeedStat);
                        }
                    }
                    if (DebugSettings.fastCrafting)
                    {
                        num *= 30f;
                    }
                    jobDriver_DoBill.workLeft -= num;
                    if (unfinishedThing2 != null)
                    {
                        unfinishedThing2.workLeft = jobDriver_DoBill.workLeft;
                    }
                    actor2.GainComfortFromCellIfPossible(chairsOnly: true);
                    if (jobDriver_DoBill.workLeft <= 0f)
                    {
                        jobDriver_DoBill.ReadyForNextToil();
                    }
                    else if (curJob2.bill.recipe.UsesUnfinishedThing)
                    {
                        int num2 = Find.TickManager.TicksGame - jobDriver_DoBill.billStartTick;
                        if (num2 >= 3000 && num2 % 1000 == 0)
                        {
                            actor2.jobs.CheckForJobOverride();
                        }
                    }
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            Log.Message(toil.actor + " - DoRecipeWork - toil.WithEffect(() => toil.actor.CurJob.bill.recipe.effectWorking, TargetIndex.A); - 45", true);
            toil.WithEffect(() => toil.actor.CurJob.bill.recipe.effectWorking, TargetIndex.A);
            Log.Message(toil.actor + " - DoRecipeWork - toil.PlaySustainerOrSound(() => toil.actor.CurJob.bill.recipe.soundWorking); - 46", true);
            toil.PlaySustainerOrSound(() => toil.actor.CurJob.bill.recipe.soundWorking);
            toil.WithProgressBar(TargetIndex.A, delegate
            {
                Pawn actor = toil.actor;
                Job curJob = actor.CurJob;
                UnfinishedThing unfinishedThing = curJob.GetTarget(TargetIndex.B).Thing as UnfinishedThing;
                return 1f - ((JobDriver_DoBillZLevels)actor.jobs.curDriver).workLeft / curJob.bill.recipe.WorkAmountTotal(unfinishedThing?.Stuff);
                Log.Message(toil.actor + " - DoRecipeWork - }); - 51", true);
            });
            toil.FailOn((Func<bool>)delegate
            {
                RecipeDef recipeDef = toil.actor.CurJob.RecipeDef;
                if (recipeDef != null && recipeDef.interruptIfIngredientIsRotting)
                {
                    LocalTargetInfo target = toil.actor.CurJob.GetTarget(TargetIndex.B);
                    if (target.HasThing && (int)target.Thing.GetRotStage() > 0)
                    {
                        Log.Message(toil.actor + " - DoRecipeWork - return true; - 56", true);
                        return true;
                    }
                }
                return toil.actor.CurJob.bill.suspended;
                Log.Message(toil.actor + " - DoRecipeWork - }); - 58", true);
            });
            Log.Message(toil.actor + " - DoRecipeWork - toil.activeSkill = (() => toil.actor.CurJob.bill.recipe.workSkill); - 59", true);
            toil.activeSkill = (() => toil.actor.CurJob.bill.recipe.workSkill);
            Log.Message(toil.actor + " - DoRecipeWork - return toil; - 60", true);
            return toil;
        }

        public static Toil FinishRecipeAndStartStoringProduct()
        {
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                Log.Message(toil.actor + " - FinishRecipeAndStartStoringProduct - Pawn actor = toil.actor; - 62", true);
                Pawn actor = toil.actor;
                Log.Message(toil.actor + " - FinishRecipeAndStartStoringProduct - Job curJob = actor.jobs.curJob; - 63", true);
                Job curJob = actor.jobs.curJob;
                Log.Message(toil.actor + " - FinishRecipeAndStartStoringProduct - JobDriver_DoBillZLevels jobDriver_DoBill = (JobDriver_DoBillZLevels)actor.jobs.curDriver; - 64", true);
                JobDriver_DoBillZLevels jobDriver_DoBill = (JobDriver_DoBillZLevels)actor.jobs.curDriver;
                Log.Message(toil.actor + " - FinishRecipeAndStartStoringProduct - if (curJob.RecipeDef.workSkill != null && !curJob.RecipeDef.UsesUnfinishedThing) - 65", true);
                if (curJob.RecipeDef.workSkill != null && !curJob.RecipeDef.UsesUnfinishedThing)
                {
                    Log.Message(toil.actor + " - FinishRecipeAndStartStoringProduct - float xp = (float)jobDriver_DoBill.ticksSpentDoingRecipeWork * 0.1f * curJob.RecipeDef.workSkillLearnFactor; - 66", true);
                    float xp = (float)jobDriver_DoBill.ticksSpentDoingRecipeWork * 0.1f * curJob.RecipeDef.workSkillLearnFactor;
                    Log.Message(toil.actor + " - FinishRecipeAndStartStoringProduct - actor.skills.GetSkill(curJob.RecipeDef.workSkill).Learn(xp); - 67", true);
                    actor.skills.GetSkill(curJob.RecipeDef.workSkill).Learn(xp);
                }
                List<Thing> ingredients = CalculateIngredients(curJob, actor);
                Log.Message(toil.actor + " - FinishRecipeAndStartStoringProduct - Thing dominantIngredient = CalculateDominantIngredient(curJob, ingredients); - 69", true);
                Thing dominantIngredient = CalculateDominantIngredient(curJob, ingredients);
                Log.Message(toil.actor + " - FinishRecipeAndStartStoringProduct - List<Thing> list = GenRecipe.MakeRecipeProducts(curJob.RecipeDef, actor, ingredients, dominantIngredient, jobDriver_DoBill.BillGiver).ToList(); - 70", true);
                List<Thing> list = GenRecipe.MakeRecipeProducts(curJob.RecipeDef, actor, ingredients, dominantIngredient, jobDriver_DoBill.BillGiver).ToList();
                Log.Message(toil.actor + " - FinishRecipeAndStartStoringProduct - ConsumeIngredients(ingredients, curJob.RecipeDef, actor.Map); - 71", true);
                ConsumeIngredients(ingredients, curJob.RecipeDef, actor.Map);
                Log.Message(toil.actor + " - FinishRecipeAndStartStoringProduct - curJob.bill.Notify_IterationCompleted(actor, ingredients); - 72", true);
                curJob.bill.Notify_IterationCompleted(actor, ingredients);
                Log.Message(toil.actor + " - FinishRecipeAndStartStoringProduct - RecordsUtility.Notify_BillDone(actor, list); - 73", true);
                RecordsUtility.Notify_BillDone(actor, list);
                Log.Message(toil.actor + " - FinishRecipeAndStartStoringProduct - UnfinishedThing unfinishedThing = curJob.GetTarget(TargetIndex.B).Thing as UnfinishedThing; - 74", true);
                UnfinishedThing unfinishedThing = curJob.GetTarget(TargetIndex.B).Thing as UnfinishedThing;
                Log.Message(toil.actor + " - FinishRecipeAndStartStoringProduct - if (curJob.bill.recipe.WorkAmountTotal(unfinishedThing?.Stuff) >= 10000f && list.Count > 0) - 75", true);
                if (curJob.bill.recipe.WorkAmountTotal(unfinishedThing?.Stuff) >= 10000f && list.Count > 0)
                {
                    Log.Message(toil.actor + " - FinishRecipeAndStartStoringProduct - TaleRecorder.RecordTale(TaleDefOf.CompletedLongCraftingProject, actor, list[0].GetInnerIfMinified().def); - 76", true);
                    TaleRecorder.RecordTale(TaleDefOf.CompletedLongCraftingProject, actor, list[0].GetInnerIfMinified().def);
                }
                Log.Message(toil.actor + " - FinishRecipeAndStartStoringProduct - if (list.Any()) - 77", true);
                if (list.Any())
                {
                    Log.Message(toil.actor + " - FinishRecipeAndStartStoringProduct - Find.QuestManager.Notify_ThingsProduced(actor, list); - 78", true);
                    Find.QuestManager.Notify_ThingsProduced(actor, list);
                }
                Log.Message(toil.actor + " - FinishRecipeAndStartStoringProduct - if (list.Count == 0) - 79", true);
                if (list.Count == 0)
                {
                    Log.Message(toil.actor + " - FinishRecipeAndStartStoringProduct - actor.jobs.EndCurrentJob(JobCondition.Succeeded); - 80", true);
                    actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
                else if (curJob.bill.GetStoreMode() == BillStoreModeDefOf.DropOnFloor)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        Log.Message(toil.actor + " - FinishRecipeAndStartStoringProduct - if (!GenPlace.TryPlaceThing(list[i], actor.Position, actor.Map, ThingPlaceMode.Near)) - 82", true);
                        if (!GenPlace.TryPlaceThing(list[i], actor.Position, actor.Map, ThingPlaceMode.Near))
                        {
                            Log.Message(toil.actor + " - FinishRecipeAndStartStoringProduct - Log.Error(string.Concat(actor, \" could not drop recipe product \", list[i], \" near \", actor.Position)); - 83", true);
                            Log.Error(string.Concat(actor, " could not drop recipe product ", list[i], " near ", actor.Position));
                        }
                    }
                    actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
                else
                {
                    Log.Message(toil.actor + " - FinishRecipeAndStartStoringProduct - if (list.Count > 1) - 85", true);
                    if (list.Count > 1)
                    {
                        for (int j = 1; j < list.Count; j++)
                        {
                            Log.Message(toil.actor + " - FinishRecipeAndStartStoringProduct - if (!GenPlace.TryPlaceThing(list[j], actor.Position, actor.Map, ThingPlaceMode.Near)) - 86", true);
                            if (!GenPlace.TryPlaceThing(list[j], actor.Position, actor.Map, ThingPlaceMode.Near))
                            {
                                Log.Message(toil.actor + " - FinishRecipeAndStartStoringProduct - Log.Error(string.Concat(actor, \" could not drop recipe product \", list[j], \" near \", actor.Position)); - 87", true);
                                Log.Error(string.Concat(actor, " could not drop recipe product ", list[j], " near ", actor.Position));
                            }
                        }
                    }
                    IntVec3 foundCell = IntVec3.Invalid;
                    Log.Message(toil.actor + " - FinishRecipeAndStartStoringProduct - if (curJob.bill.GetStoreMode() == BillStoreModeDefOf.BestStockpile) - 89", true);
                    if (curJob.bill.GetStoreMode() == BillStoreModeDefOf.BestStockpile)
                    {
                        Log.Message(toil.actor + " - FinishRecipeAndStartStoringProduct - StoreUtility.TryFindBestBetterStoreCellFor(list[0], actor, actor.Map, StoragePriority.Unstored, actor.Faction, out foundCell); - 90", true);
                        StoreUtility.TryFindBestBetterStoreCellFor(list[0], actor, actor.Map, StoragePriority.Unstored, actor.Faction, out foundCell);
                    }
                    else if (curJob.bill.GetStoreMode() == BillStoreModeDefOf.SpecificStockpile)
                    {
                        Log.Message(toil.actor + " - FinishRecipeAndStartStoringProduct - StoreUtility.TryFindBestBetterStoreCellForIn(list[0], actor, actor.Map, StoragePriority.Unstored, actor.Faction, curJob.bill.GetStoreZone().slotGroup, out foundCell); - 92", true);
                        StoreUtility.TryFindBestBetterStoreCellForIn(list[0], actor, actor.Map, StoragePriority.Unstored, actor.Faction, curJob.bill.GetStoreZone().slotGroup, out foundCell);
                    }
                    else
                    {
                        Log.Message(toil.actor + " - FinishRecipeAndStartStoringProduct - Log.ErrorOnce(\"Unknown store mode\", 9158246); - 93", true);
                        Log.ErrorOnce("Unknown store mode", 9158246);
                    }
                    Log.Message(toil.actor + " - FinishRecipeAndStartStoringProduct - if (foundCell.IsValid) - 94", true);
                    if (foundCell.IsValid)
                    {
                        Log.Message(toil.actor + " - FinishRecipeAndStartStoringProduct - actor.carryTracker.TryStartCarry(list[0]); - 95", true);
                        actor.carryTracker.TryStartCarry(list[0]);
                        Log.Message(toil.actor + " - FinishRecipeAndStartStoringProduct - curJob.targetB = foundCell; - 96", true);
                        curJob.targetB = foundCell;
                        Log.Message(toil.actor + " - FinishRecipeAndStartStoringProduct - curJob.targetA = list[0]; - 97", true);
                        curJob.targetA = list[0];
                        Log.Message(toil.actor + " - FinishRecipeAndStartStoringProduct - curJob.count = 99999; - 98", true);
                        curJob.count = 99999;
                    }
                    else
                    {
                        Log.Message(toil.actor + " - FinishRecipeAndStartStoringProduct - if (!GenPlace.TryPlaceThing(list[0], actor.Position, actor.Map, ThingPlaceMode.Near)) - 99", true);
                        if (!GenPlace.TryPlaceThing(list[0], actor.Position, actor.Map, ThingPlaceMode.Near))
                        {
                            Log.Message(toil.actor + " - FinishRecipeAndStartStoringProduct - Log.Error(string.Concat(\"Bill doer could not drop product \", list[0], \" near \", actor.Position)); - 100", true);
                            Log.Error(string.Concat("Bill doer could not drop product ", list[0], " near ", actor.Position));
                        }
                        actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                    }
                }
            };
            Log.Message(toil.actor + " - FinishRecipeAndStartStoringProduct - return toil; - 103", true);
            return toil;
        }

        private static List<Thing> CalculateIngredients(Job job, Pawn actor)
        {
            Log.Message(actor + " - CalculateIngredients - UnfinishedThing unfinishedThing = job.GetTarget(TargetIndex.B).Thing as UnfinishedThing; - 104", true);
            UnfinishedThing unfinishedThing = job.GetTarget(TargetIndex.B).Thing as UnfinishedThing;
            Log.Message(actor + " - CalculateIngredients - if (unfinishedThing != null) - 105", true);
            if (unfinishedThing != null)
            {
                Log.Message(actor + " - CalculateIngredients - List<Thing> ingredients = unfinishedThing.ingredients; - 106", true);
                List<Thing> ingredients = unfinishedThing.ingredients;
                Log.Message(actor + " - CalculateIngredients - job.RecipeDef.Worker.ConsumeIngredient(unfinishedThing, job.RecipeDef, actor.Map); - 107", true);
                job.RecipeDef.Worker.ConsumeIngredient(unfinishedThing, job.RecipeDef, actor.Map);
                Log.Message(actor + " - CalculateIngredients - job.placedThings = null; - 108", true);
                job.placedThings = null;
                Log.Message(actor + " - CalculateIngredients - return ingredients; - 109", true);
                return ingredients;
            }
            List<Thing> list = new List<Thing>();
            Log.Message(actor + " - CalculateIngredients - if (job.placedThings != null) - 111", true);
            if (job.placedThings != null)
            {
                for (int i = 0; i < job.placedThings.Count; i++)
                {
                    Log.Message(actor + " - CalculateIngredients - if (job.placedThings[i].Count <= 0) - 112", true);
                    if (job.placedThings[i].Count <= 0)
                    {
                        Log.Message(actor + " - CalculateIngredients - Log.Error(string.Concat(\"PlacedThing \", job.placedThings[i], \" with count \", job.placedThings[i].Count, \" for job \", job)); - 113", true);
                        Log.Error(string.Concat("PlacedThing ", job.placedThings[i], " with count ", job.placedThings[i].Count, " for job ", job));
                        Log.Message(actor + " - CalculateIngredients - continue; - 114", true);
                        continue;
                    }
                    Thing thing = (job.placedThings[i].Count >= job.placedThings[i].thing.stackCount) ? job.placedThings[i].thing : job.placedThings[i].thing.SplitOff(job.placedThings[i].Count);
                    Log.Message(actor + " - CalculateIngredients - job.placedThings[i].Count = 0; - 116", true);
                    job.placedThings[i].Count = 0;
                    Log.Message(actor + " - CalculateIngredients - if (list.Contains(thing)) - 117", true);
                    if (list.Contains(thing))
                    {
                        Log.Message(actor + " - CalculateIngredients - Log.Error(\"Tried to add ingredient from job placed targets twice: \" + thing); - 118", true);
                        Log.Error("Tried to add ingredient from job placed targets twice: " + thing);
                        Log.Message(actor + " - CalculateIngredients - continue; - 119", true);
                        continue;
                    }
                    list.Add(thing);
                    Log.Message(actor + " - CalculateIngredients - if (job.RecipeDef.autoStripCorpses) - 121", true);
                    if (job.RecipeDef.autoStripCorpses)
                    {
                        Log.Message(actor + " - CalculateIngredients - (thing as IStrippable)?.Strip(); - 122", true);
                        (thing as IStrippable)?.Strip();
                    }
                }
            }
            job.placedThings = null;
            Log.Message(actor + " - CalculateIngredients - return list; - 124", true);
            return list;
        }

        private static Thing CalculateDominantIngredient(Job job, List<Thing> ingredients)
        {
            UnfinishedThing uft = job.GetTarget(TargetIndex.B).Thing as UnfinishedThing;
            if (uft != null && uft.def.MadeFromStuff)
            {
                return uft.ingredients.First((Thing ing) => ing.def == uft.Stuff);
            }
            if (!ingredients.NullOrEmpty())
            {
                if (job.RecipeDef.productHasIngredientStuff)
                {
                    return ingredients[0];
                }
                if (job.RecipeDef.products.Any((ThingDefCountClass x) => x.thingDef.MadeFromStuff))
                {
                    return ingredients.Where((Thing x) => x.def.IsStuff).RandomElementByWeight((Thing x) => x.stackCount);
                }
                return ingredients.RandomElementByWeight((Thing x) => x.stackCount);
            }
            return null;
        }

        private static void ConsumeIngredients(List<Thing> ingredients, RecipeDef recipe, Map map)
        {
            for (int i = 0; i < ingredients.Count; i++)
            {
                recipe.Worker.ConsumeIngredient(ingredients[i], recipe, map);
            }
        }


        private static Toil JumpToCollectNextIntoHandsForBill(Toil gotoGetTargetToil, TargetIndex ind)
        {
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - Pawn actor = toil.actor; - 60", true);
                Pawn actor = toil.actor;
                Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - if (actor.carryTracker.CarriedThing == null) - 61", true);
                if (actor.carryTracker.CarriedThing == null)
                {
                    Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - Log.Error(string.Concat(\"JumpToAlsoCollectTargetInQueue run on \", actor, \" who is not carrying something.\")); - 62", true);
                    Log.Error(string.Concat("JumpToAlsoCollectTargetInQueue run on ", actor, " who is not carrying something."));
                }
                else if (!actor.carryTracker.Full)
                {
                    Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - Job curJob = actor.jobs.curJob; - 64", true);
                    Job curJob = actor.jobs.curJob;
                    Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - List<LocalTargetInfo> targetQueue = curJob.GetTargetQueue(ind); - 65", true);
                    List<LocalTargetInfo> targetQueue = curJob.GetTargetQueue(ind);
                    Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - if (!targetQueue.NullOrEmpty()) - 66", true);
                    if (!targetQueue.NullOrEmpty())
                    {
                        Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - int num = 0; - 67", true);
                        int num = 0;
                        Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - int a; - 68", true);
                        int a;
                        while (true)
                        {
                            Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - if (num >= targetQueue.Count) - 69", true);
                            if (num >= targetQueue.Count)
                            {
                                Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - return; - 70", true);
                                return;
                            }
                            Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - if (GenAI.CanUseItemForWork(actor, targetQueue[num].Thing) && targetQueue[num].Thing.CanStackWith(actor.carryTracker.CarriedThing) && !((float)(actor.Position - targetQueue[num].Thing.Position).LengthHorizontalSquared > 64f)) - 71", true);
                            if (GenAI.CanUseItemForWork(actor, targetQueue[num].Thing) && targetQueue[num].Thing.CanStackWith(actor.carryTracker.CarriedThing) && !((float)(actor.Position - targetQueue[num].Thing.Position).LengthHorizontalSquared > 64f))
                            {
                                Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - int num2 = (actor.carryTracker.CarriedThing != null) ? actor.carryTracker.CarriedThing.stackCount : 0; - 72", true);
                                int num2 = (actor.carryTracker.CarriedThing != null) ? actor.carryTracker.CarriedThing.stackCount : 0;
                                Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - a = curJob.countQueue[num]; - 73", true);
                                a = curJob.countQueue[num];
                                Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - a = Mathf.Min(a, targetQueue[num].Thing.def.stackLimit - num2); - 74", true);
                                a = Mathf.Min(a, targetQueue[num].Thing.def.stackLimit - num2);
                                Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - a = Mathf.Min(a, actor.carryTracker.AvailableStackSpace(targetQueue[num].Thing.def)); - 75", true);
                                a = Mathf.Min(a, actor.carryTracker.AvailableStackSpace(targetQueue[num].Thing.def));
                                Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - if (a > 0) - 76", true);
                                if (a > 0)
                                {
                                    Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - break; - 77", true);
                                    break;
                                }
                            }
                            num++;
                        }
                        curJob.count = a;
                        Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - curJob.SetTarget(ind, targetQueue[num].Thing); - 80", true);
                        curJob.SetTarget(ind, targetQueue[num].Thing);
                        Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - curJob.countQueue[num] -= a; - 81", true);
                        curJob.countQueue[num] -= a;
                        Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - if (curJob.countQueue[num] <= 0) - 82", true);
                        if (curJob.countQueue[num] <= 0)
                        {
                            Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - curJob.countQueue.RemoveAt(num); - 83", true);
                            curJob.countQueue.RemoveAt(num);
                            Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - targetQueue.RemoveAt(num); - 84", true);
                            targetQueue.RemoveAt(num);
                        }
                        actor.jobs.curDriver.JumpToToil(gotoGetTargetToil);
                    }
                }
            };
            Log.Message(toil.actor + " - JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - return toil; - 87", true);
            return toil;
        }
    }
}