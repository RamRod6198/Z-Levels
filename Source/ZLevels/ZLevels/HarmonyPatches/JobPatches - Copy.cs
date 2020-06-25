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
            if (job.RecipeDef != null)
            {
                return ReportStringProcessed(job.RecipeDef.jobString);
            }
            return base.GetReport();
        }

        public override void ExposeData()
        {
            Log.Message("JobDriver_DoBillZLevels : JobDriver - ExposeData - base.ExposeData(); - 4", true);
            base.ExposeData();
            Log.Message("JobDriver_DoBillZLevels : JobDriver - ExposeData - Scribe_Values.Look(ref workLeft, \"workLeft\", 0f); - 5", true);
            Scribe_Values.Look(ref workLeft, "workLeft", 0f);
            Scribe_Values.Look(ref billStartTick, "billStartTick", 0);
            Log.Message("JobDriver_DoBillZLevels : JobDriver - ExposeData - Scribe_Values.Look(ref ticksSpentDoingRecipeWork, \"ticksSpentDoingRecipeWork\", 0); - 7", true);
            Scribe_Values.Look(ref ticksSpentDoingRecipeWork, "ticksSpentDoingRecipeWork", 0);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Log.Message("JobDriver_DoBillZLevels : JobDriver - TryMakePreToilReservations - if (!pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed)) - 8", true);
            if (!pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed))
            {
                Log.Message("JobDriver_DoBillZLevels : JobDriver - TryMakePreToilReservations - return false; - 9", true);
                return false;
            }
            pawn.ReserveAsManyAsPossible(job.GetTargetQueue(TargetIndex.B), job);
            Log.Message("JobDriver_DoBillZLevels : JobDriver - TryMakePreToilReservations - return true; - 11", true);
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - Toil toil = new Toil(); - 12", true);
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
                            Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - var target = job.targetQueueA[i]; - 25", true);
                            var target = job.targetQueueA[i];
                            ZLogger.Message("-1 job.targetQueueA: " + target.Thing);
                            ZLogger.Message("-1 job.targetQueueA: " + target.Thing.Map);

                        }
                    }
                    catch { }
                    try
                    {
                        ZLogger.Message("--------------------------");
                        for (int i = job.targetQueueB.Count - 1; i >= 0; i--)
                        {
                            Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - var target = job.targetQueueB[i]; - 29", true);
                            var target = job.targetQueueB[i];

                            ZLogger.Message("-1 job.targetQueueB: " + target.Thing);
                            ZLogger.Message("-1 job.targetQueueB.Map: " + target.Thing.Map);
                            ZLogger.Message("-1 job.targetQueueB.stackCount: " + target.Thing.stackCount);
                            ZLogger.Message("-1 job.targetQueueB.countQueue: " + job.countQueue[i]);

                        }
                    }
                    catch { }
                }
            };

            AddEndCondition(delegate
            {
                Thing thing = GetActor().jobs.curJob.GetTarget(TargetIndex.A).Thing;
                return (!(thing is Building) || thing.Spawned) ? JobCondition.Ongoing : JobCondition.Incompletable;
            });
            this.FailOnBurningImmobile(TargetIndex.A);
            this.FailOn(delegate
            {
                IBillGiver billGiver = job.GetTarget(TargetIndex.A).Thing as IBillGiver;
                if (billGiver != null)
                {
                    if (job.bill.DeletedOrDereferenced)
                    {
                        Log.Message(this.pawn + " - 1 fail", true);
                        return true;
                    }
                    if (!billGiver.CurrentlyUsableForBills())
                    {
                        Log.Message(this.pawn + " - 2 fail", true);
                        return true;
                    }
                }
                return false;
            });
            Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - Toil gotoBillGiver = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell); - 48", true);
            Toil gotoBillGiver = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            toil.initAction = delegate
            {
                Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - if (job.targetQueueB != null && job.targetQueueB.Count == 1) - 49", true);
                if (job.targetQueueB != null && job.targetQueueB.Count == 1)
                {
                    Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - UnfinishedThing unfinishedThing = job.targetQueueB[0].Thing as UnfinishedThing; - 50", true);
                    UnfinishedThing unfinishedThing = job.targetQueueB[0].Thing as UnfinishedThing;
                    Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - if (unfinishedThing != null) - 51", true);
                    if (unfinishedThing != null)
                    {
                        Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - unfinishedThing.BoundBill = (Bill_ProductionWithUft)job.bill; - 52", true);
                        unfinishedThing.BoundBill = (Bill_ProductionWithUft)job.bill;
                    }
                }
            };
            yield return new Toil { initAction = delegate () { Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - yield return toil; - 54", true); } };
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
                            Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - var target = job.targetQueueA[i]; - 67", true);
                            var target = job.targetQueueA[i];
                            ZLogger.Message("1 job.targetQueueA: " + target.Thing);
                            ZLogger.Message("1 job.targetQueueA: " + target.Thing.Map);
                            ZLogger.Message("1 job.targetQueueA: " + target.Thing.stackCount);

                        }
                    }
                    catch { }
                    try
                    {
                        ZLogger.Message("--------------------------");
                        for (int i = job.targetQueueB.Count - 1; i >= 0; i--)
                        {
                            Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - var target = job.targetQueueB[i]; - 72", true);
                            var target = job.targetQueueB[i];

                            ZLogger.Message("1 job.targetQueueB: " + target.Thing);
                            ZLogger.Message("1 job.targetQueueB.Map: " + target.Thing.Map);
                            ZLogger.Message("1 job.targetQueueB.stackCount: " + target.Thing.stackCount);
                            ZLogger.Message("1 job.targetQueueB.countQueue: " + job.countQueue[i]);

                        }
                    }
                    catch { }
                }
            };

            yield return Toils_Jump.JumpIf(gotoBillGiver, () => job.GetTargetQueue(TargetIndex.B).NullOrEmpty());
            Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - Toil extract = ExtractNextTargetFromQueue(TargetIndex.B); - 78", true);
            Toil extract = ExtractNextTargetFromQueue(TargetIndex.B);

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
                            Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - var target = job.targetQueueA[i]; - 91", true);
                            var target = job.targetQueueA[i];
                            ZLogger.Message("2 job.targetQueueA: " + target.Thing);
                            ZLogger.Message("2 job.targetQueueA: " + target.Thing.Map);

                        }
                    }
                    catch { }
                    try
                    {
                        ZLogger.Message("--------------------------");
                        for (int i = job.targetQueueB.Count - 1; i >= 0; i--)
                        {
                            Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - var target = job.targetQueueB[i]; - 95", true);
                            var target = job.targetQueueB[i];

                            ZLogger.Message("2 job.targetQueueB: " + target.Thing);
                            ZLogger.Message("2 job.targetQueueB.Map: " + target.Thing.Map);
                            ZLogger.Message("2 job.targetQueueB.stackCount: " + target.Thing.stackCount);
                            ZLogger.Message("2 job.targetQueueB.countQueue: " + job.countQueue[i]);
                        }
                    }
                    catch { }
                }
            };

            yield return extract;
            Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - Toil getToHaulTarget = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch); - 101", true);
            Toil getToHaulTarget = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);

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
                            Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - var target = job.targetQueueA[i]; - 114", true);
                            var target = job.targetQueueA[i];
                            ZLogger.Message("3 job.targetQueueA: " + target.Thing);
                            ZLogger.Message("3 job.targetQueueA: " + target.Thing.Map);

                        }
                    }
                    catch { }
                    try
                    {
                        ZLogger.Message("--------------------------");
                        for (int i = job.targetQueueB.Count - 1; i >= 0; i--)
                        {
                            Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - var target = job.targetQueueB[i]; - 118", true);
                            var target = job.targetQueueB[i];

                            ZLogger.Message("3 job.targetQueueB: " + target.Thing);
                            ZLogger.Message("3 job.targetQueueB.Map: " + target.Thing.Map);
                            ZLogger.Message("3 job.targetQueueB.stackCount: " + target.Thing.stackCount);
                            ZLogger.Message("3 job.targetQueueB.countQueue: " + job.countQueue[i]);

                        }
                    }
                    catch { }
                }
            };
            yield return new Toil { initAction = delegate () { Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - yield return getToHaulTarget; - 123", true); } };
            yield return getToHaulTarget;
            yield return new Toil { initAction = delegate () { Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - yield return Toils_Haul.StartCarryThing(TargetIndex.B, putRemainderInQueue: true, subtractNumTakenFromJobCount: false, failIfStackCountLessThanJobCount: true); - 124", true); } };
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, putRemainderInQueue: true, subtractNumTakenFromJobCount: false, failIfStackCountLessThanJobCount: true);
            yield return new Toil { initAction = delegate () { Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - yield return JumpToCollectNextIntoHandsForBill(getToHaulTarget, TargetIndex.B); - 125", true); } };
            yield return JumpToCollectNextIntoHandsForBill(getToHaulTarget, TargetIndex.B);
            yield return new Toil { initAction = delegate () { Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell).FailOnDestroyedOrNull(TargetIndex.B); - 126", true); } };
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell).FailOnDestroyedOrNull(TargetIndex.B);
            Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - Toil findPlaceTarget2 = Toils_JobTransforms.SetTargetToIngredientPlaceCell(TargetIndex.A, TargetIndex.B, TargetIndex.C); - 127", true);
            Toil findPlaceTarget2 = Toils_JobTransforms.SetTargetToIngredientPlaceCell(TargetIndex.A, TargetIndex.B, TargetIndex.C);
            yield return new Toil { initAction = delegate () { Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - yield return findPlaceTarget2; - 128", true); } };
            yield return findPlaceTarget2;
            yield return new Toil { initAction = delegate () { Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.C, findPlaceTarget2, storageMode: false); - 129", true); } };
            yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.C, findPlaceTarget2, storageMode: false);
            yield return new Toil { initAction = delegate () { Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - yield return Toils_Jump.JumpIfHaveTargetInQueue(TargetIndex.B, extract); - 130", true); } };
            yield return Toils_Jump.JumpIfHaveTargetInQueue(TargetIndex.B, extract);
            yield return new Toil { initAction = delegate () { Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - yield return gotoBillGiver; - 131", true); } };
            yield return gotoBillGiver;
            yield return new Toil { initAction = delegate () { Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - yield return Toils_Recipe.MakeUnfinishedThingIfNeeded(); - 132", true); } };
            yield return Toils_Recipe.MakeUnfinishedThingIfNeeded();
            yield return new Toil { initAction = delegate () { Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - yield return DoRecipeWork().FailOnDespawnedNullOrForbiddenPlacedThings().FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell); - 133", true); } };
            yield return DoRecipeWork().FailOnDespawnedNullOrForbiddenPlacedThings().FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
            yield return new Toil { initAction = delegate () { Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - yield return FinishRecipeAndStartStoringProduct(); - 134", true); } };
            yield return FinishRecipeAndStartStoringProduct();
            Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - if (!job.RecipeDef.products.NullOrEmpty() || !job.RecipeDef.specialProducts.NullOrEmpty()) - 135", true);
            if (!job.RecipeDef.products.NullOrEmpty() || !job.RecipeDef.specialProducts.NullOrEmpty())
            {
                yield return new Toil { initAction = delegate () { Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - yield return Toils_Reserve.Reserve(TargetIndex.B); - 136", true); } };
                yield return Toils_Reserve.Reserve(TargetIndex.B);
                Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - findPlaceTarget2 = Toils_Haul.CarryHauledThingToCell(TargetIndex.B); - 137", true);
                findPlaceTarget2 = Toils_Haul.CarryHauledThingToCell(TargetIndex.B);
                yield return new Toil { initAction = delegate () { Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - yield return findPlaceTarget2; - 138", true); } };
                yield return findPlaceTarget2;
                yield return new Toil { initAction = delegate () { Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.B, findPlaceTarget2, storageMode: true, tryStoreInSameStorageIfSpotCantHoldWholeStack: true); - 139", true); } };
                yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.B, findPlaceTarget2, storageMode: true, tryStoreInSameStorageIfSpotCantHoldWholeStack: true);
                Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - Toil recount = new Toil(); - 140", true);
                Toil recount = new Toil();
                recount.initAction = delegate
                {
                    Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - Bill_Production bill_Production = recount.actor.jobs.curJob.bill as Bill_Production; - 141", true);
                    Bill_Production bill_Production = recount.actor.jobs.curJob.bill as Bill_Production;
                    Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - if (bill_Production != null && bill_Production.repeatMode == BillRepeatModeDefOf.TargetCount) - 142", true);
                    if (bill_Production != null && bill_Production.repeatMode == BillRepeatModeDefOf.TargetCount)
                    {
                        Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - base.Map.resourceCounter.UpdateResourceCounts(); - 143", true);
                        base.Map.resourceCounter.UpdateResourceCounts();
                    }
                };
                yield return new Toil { initAction = delegate () { Log.Message("JobDriver_DoBillZLevels : JobDriver - MakeNewToils - yield return recount; - 145", true); } };
                yield return recount;
            }
        }

        public static Toil ExtractNextTargetFromQueue(TargetIndex ind, bool failIfCountFromQueueTooBig = true)
        {
            Log.Message("JobDriver_DoBillZLevels : JobDriver - ExtractNextTargetFromQueue - Toil toil = new Toil(); - 146", true);
            Toil toil = new Toil();
            toil.initAction = delegate
            {

                Pawn actor = toil.actor;
                Log.Message("JobDriver_DoBillZLevels : JobDriver - ExtractNextTargetFromQueue - Job curJob = actor.jobs.curJob; - 148", true);
                Job curJob = actor.jobs.curJob;
                try
                {
                    ZLogger.Message("--------------------------");
                    for (int i = curJob.targetQueueB.Count - 1; i >= 0; i--)
                    {
                        Log.Message("JobDriver_DoBillZLevels : JobDriver - ExtractNextTargetFromQueue - var target = curJob.targetQueueB[i]; - 150", true);
                        var target = curJob.targetQueueB[i];

                        ZLogger.Message("4 job.targetQueueB: " + target.Thing);
                        ZLogger.Message("4 job.targetQueueB.Map: " + target.Thing.Map);
                        ZLogger.Message("4 job.targetQueueB.stackCount: " + target.Thing.stackCount);
                        ZLogger.Message("4 job.targetQueueB.countQueue: " + curJob.countQueue[i]);

                    }
                }
                catch { }
                List<LocalTargetInfo> targetQueue = curJob.GetTargetQueue(ind);
                Log.Message("JobDriver_DoBillZLevels : JobDriver - ExtractNextTargetFromQueue - if (!targetQueue.NullOrEmpty()) - 155", true);
                if (!targetQueue.NullOrEmpty())
                {
                    Log.Message("JobDriver_DoBillZLevels : JobDriver - ExtractNextTargetFromQueue - if (failIfCountFromQueueTooBig && !curJob.countQueue.NullOrEmpty() && targetQueue[0].HasThing - 156", true);
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
                        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
                        actor.jobs.curDriver.EndJobWith(JobCondition.Incompletable);
                    }
                    else
                    {
                        Log.Message("JobDriver_DoBillZLevels : JobDriver - ExtractNextTargetFromQueue - curJob.SetTarget(ind, targetQueue[0]); - 166", true);
                        curJob.SetTarget(ind, targetQueue[0]);
                        Log.Message("JobDriver_DoBillZLevels : JobDriver - ExtractNextTargetFromQueue - targetQueue.RemoveAt(0); - 167", true);
                        targetQueue.RemoveAt(0);
                        Log.Message("JobDriver_DoBillZLevels : JobDriver - ExtractNextTargetFromQueue - if (!curJob.countQueue.NullOrEmpty()) - 168", true);
                        if (!curJob.countQueue.NullOrEmpty())
                        {
                            Log.Message("JobDriver_DoBillZLevels : JobDriver - ExtractNextTargetFromQueue - curJob.count = curJob.countQueue[0]; - 169", true);
                            curJob.count = curJob.countQueue[0];
                            Log.Message("JobDriver_DoBillZLevels : JobDriver - ExtractNextTargetFromQueue - curJob.countQueue.RemoveAt(0); - 170", true);
                            curJob.countQueue.RemoveAt(0);
                        }
                    }
                }
            };
            Log.Message("JobDriver_DoBillZLevels : JobDriver - ExtractNextTargetFromQueue - return toil; - 172", true);
            return toil;
        }

        public static Toil DoRecipeWork()
        {
            Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - Toil toil = new Toil(); - 173", true);
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - Pawn actor3 = toil.actor; - 174", true);
                Pawn actor3 = toil.actor;
                Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - Job curJob3 = actor3.jobs.curJob; - 175", true);
                Job curJob3 = actor3.jobs.curJob;
                Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - JobDriver_DoBillZLevels jobDriver_DoBill2 = (JobDriver_DoBillZLevels)actor3.jobs.curDriver; - 176", true);
                JobDriver_DoBillZLevels jobDriver_DoBill2 = (JobDriver_DoBillZLevels)actor3.jobs.curDriver;
                Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - UnfinishedThing unfinishedThing3 = curJob3.GetTarget(TargetIndex.B).Thing as UnfinishedThing; - 177", true);
                UnfinishedThing unfinishedThing3 = curJob3.GetTarget(TargetIndex.B).Thing as UnfinishedThing;
                Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - if (unfinishedThing3 != null && unfinishedThing3.Initialized) - 178", true);
                if (unfinishedThing3 != null && unfinishedThing3.Initialized)
                {
                    Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - jobDriver_DoBill2.workLeft = unfinishedThing3.workLeft; - 179", true);
                    jobDriver_DoBill2.workLeft = unfinishedThing3.workLeft;
                }
                else
                {
                    Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - jobDriver_DoBill2.workLeft = curJob3.bill.recipe.WorkAmountTotal(unfinishedThing3?.Stuff); - 180", true);
                    jobDriver_DoBill2.workLeft = curJob3.bill.recipe.WorkAmountTotal(unfinishedThing3?.Stuff);
                    Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - if (unfinishedThing3 != null) - 181", true);
                    if (unfinishedThing3 != null)
                    {
                        Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - unfinishedThing3.workLeft = jobDriver_DoBill2.workLeft; - 182", true);
                        unfinishedThing3.workLeft = jobDriver_DoBill2.workLeft;
                    }
                }
                jobDriver_DoBill2.billStartTick = Find.TickManager.TicksGame;
                Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - jobDriver_DoBill2.ticksSpentDoingRecipeWork = 0; - 184", true);
                jobDriver_DoBill2.ticksSpentDoingRecipeWork = 0;
                Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - curJob3.bill.Notify_DoBillStarted(actor3); - 185", true);
                curJob3.bill.Notify_DoBillStarted(actor3);
                Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - }; - 186", true);
            };
            toil.tickAction = delegate
            {
                Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - Pawn actor2 = toil.actor; - 187", true);
                Pawn actor2 = toil.actor;
                Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - Job curJob2 = actor2.jobs.curJob; - 188", true);
                Job curJob2 = actor2.jobs.curJob;
                Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - JobDriver_DoBillZLevels jobDriver_DoBill = (JobDriver_DoBillZLevels)actor2.jobs.curDriver; - 189", true);
                JobDriver_DoBillZLevels jobDriver_DoBill = (JobDriver_DoBillZLevels)actor2.jobs.curDriver;
                Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - UnfinishedThing unfinishedThing2 = curJob2.GetTarget(TargetIndex.B).Thing as UnfinishedThing; - 190", true);
                UnfinishedThing unfinishedThing2 = curJob2.GetTarget(TargetIndex.B).Thing as UnfinishedThing;
                Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - if (unfinishedThing2 != null && unfinishedThing2.Destroyed) - 191", true);
                if (unfinishedThing2 != null && unfinishedThing2.Destroyed)
                {
                    Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - actor2.jobs.EndCurrentJob(JobCondition.Incompletable); - 192", true);
                    actor2.jobs.EndCurrentJob(JobCondition.Incompletable);
                }
                else
                {
                    Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - jobDriver_DoBill.ticksSpentDoingRecipeWork++; - 193", true);
                    jobDriver_DoBill.ticksSpentDoingRecipeWork++;
                    Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - curJob2.bill.Notify_PawnDidWork(actor2); - 194", true);
                    curJob2.bill.Notify_PawnDidWork(actor2);
                    (toil.actor.CurJob.GetTarget(TargetIndex.A).Thing as IBillGiverWithTickAction)?.UsedThisTick();
                    Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - if (curJob2.RecipeDef.workSkill != null && curJob2.RecipeDef.UsesUnfinishedThing) - 196", true);
                    if (curJob2.RecipeDef.workSkill != null && curJob2.RecipeDef.UsesUnfinishedThing)
                    {
                        Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - actor2.skills.Learn(curJob2.RecipeDef.workSkill, 0.1f * curJob2.RecipeDef.workSkillLearnFactor); - 197", true);
                        actor2.skills.Learn(curJob2.RecipeDef.workSkill, 0.1f * curJob2.RecipeDef.workSkillLearnFactor);
                    }
                    float num = (curJob2.RecipeDef.workSpeedStat == null) ? 1f : actor2.GetStatValue(curJob2.RecipeDef.workSpeedStat);
                    Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - if (curJob2.RecipeDef.workTableSpeedStat != null) - 199", true);
                    if (curJob2.RecipeDef.workTableSpeedStat != null)
                    {
                        Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - Building_WorkTable building_WorkTable = jobDriver_DoBill.BillGiver as Building_WorkTable; - 200", true);
                        Building_WorkTable building_WorkTable = jobDriver_DoBill.BillGiver as Building_WorkTable;
                        Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - if (building_WorkTable != null) - 201", true);
                        if (building_WorkTable != null)
                        {
                            Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - num *= building_WorkTable.GetStatValue(curJob2.RecipeDef.workTableSpeedStat); - 202", true);
                            num *= building_WorkTable.GetStatValue(curJob2.RecipeDef.workTableSpeedStat);
                        }
                    }
                    Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - if (DebugSettings.fastCrafting) - 203", true);
                    if (DebugSettings.fastCrafting)
                    {
                        Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - num *= 30f; - 204", true);
                        num *= 30f;
                    }
                    jobDriver_DoBill.workLeft -= num;
                    Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - if (unfinishedThing2 != null) - 206", true);
                    if (unfinishedThing2 != null)
                    {
                        Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - unfinishedThing2.workLeft = jobDriver_DoBill.workLeft; - 207", true);
                        unfinishedThing2.workLeft = jobDriver_DoBill.workLeft;
                    }
                    actor2.GainComfortFromCellIfPossible(chairsOnly: true);
                    Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - if (jobDriver_DoBill.workLeft <= 0f) - 209", true);
                    if (jobDriver_DoBill.workLeft <= 0f)
                    {
                        Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - jobDriver_DoBill.ReadyForNextToil(); - 210", true);
                        jobDriver_DoBill.ReadyForNextToil();
                    }
                    else if (curJob2.bill.recipe.UsesUnfinishedThing)
                    {
                        int num2 = Find.TickManager.TicksGame - jobDriver_DoBill.billStartTick;
                        Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - if (num2 >= 3000 && num2 % 1000 == 0) - 213", true);
                        if (num2 >= 3000 && num2 % 1000 == 0)
                        {
                            Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - actor2.jobs.CheckForJobOverride(); - 214", true);
                            actor2.jobs.CheckForJobOverride();
                        }
                    }
                }
            };
            Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - toil.defaultCompleteMode = ToilCompleteMode.Never; - 216", true);
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - toil.WithEffect(() => toil.actor.CurJob.bill.recipe.effectWorking, TargetIndex.A); - 217", true);
            toil.WithEffect(() => toil.actor.CurJob.bill.recipe.effectWorking, TargetIndex.A);
            Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - toil.PlaySustainerOrSound(() => toil.actor.CurJob.bill.recipe.soundWorking); - 218", true);
            toil.PlaySustainerOrSound(() => toil.actor.CurJob.bill.recipe.soundWorking);
            toil.WithProgressBar(TargetIndex.A, delegate
            {
                Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - Pawn actor = toil.actor; - 219", true);
                Pawn actor = toil.actor;
                Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - Job curJob = actor.CurJob; - 220", true);
                Job curJob = actor.CurJob;
                Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - UnfinishedThing unfinishedThing = curJob.GetTarget(TargetIndex.B).Thing as UnfinishedThing; - 221", true);
                UnfinishedThing unfinishedThing = curJob.GetTarget(TargetIndex.B).Thing as UnfinishedThing;
                Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - return 1f - ((JobDriver_DoBillZLevels)actor.jobs.curDriver).workLeft / curJob.bill.recipe.WorkAmountTotal(unfinishedThing?.Stuff); - 222", true);
                return 1f - ((JobDriver_DoBillZLevels)actor.jobs.curDriver).workLeft / curJob.bill.recipe.WorkAmountTotal(unfinishedThing?.Stuff);
                Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - }); - 223", true);
            });
            toil.FailOn((Func<bool>)delegate
            {
                Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - RecipeDef recipeDef = toil.actor.CurJob.RecipeDef; - 224", true);
                RecipeDef recipeDef = toil.actor.CurJob.RecipeDef;
                Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - if (recipeDef != null && recipeDef.interruptIfIngredientIsRotting) - 225", true);
                if (recipeDef != null && recipeDef.interruptIfIngredientIsRotting)
                {
                    Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - LocalTargetInfo target = toil.actor.CurJob.GetTarget(TargetIndex.B); - 226", true);
                    LocalTargetInfo target = toil.actor.CurJob.GetTarget(TargetIndex.B);
                    Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - if (target.HasThing && (int)target.Thing.GetRotStage() > 0) - 227", true);
                    if (target.HasThing && (int)target.Thing.GetRotStage() > 0)
                    {
                        Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - return true; - 228", true);
                        return true;
                    }
                }
                return toil.actor.CurJob.bill.suspended;
                Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - }); - 230", true);
            });
            Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - toil.activeSkill = (() => toil.actor.CurJob.bill.recipe.workSkill); - 231", true);
            toil.activeSkill = (() => toil.actor.CurJob.bill.recipe.workSkill);
            Log.Message("JobDriver_DoBillZLevels : JobDriver - DoRecipeWork - return toil; - 232", true);
            return toil;
        }

        public static Toil FinishRecipeAndStartStoringProduct()
        {
            Log.Message("JobDriver_DoBillZLevels : JobDriver - FinishRecipeAndStartStoringProduct - Toil toil = new Toil(); - 233", true);
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                Log.Message("JobDriver_DoBillZLevels : JobDriver - FinishRecipeAndStartStoringProduct - Pawn actor = toil.actor; - 234", true);
                Pawn actor = toil.actor;
                Log.Message("JobDriver_DoBillZLevels : JobDriver - FinishRecipeAndStartStoringProduct - Job curJob = actor.jobs.curJob; - 235", true);
                Job curJob = actor.jobs.curJob;
                Log.Message("JobDriver_DoBillZLevels : JobDriver - FinishRecipeAndStartStoringProduct - JobDriver_DoBillZLevels jobDriver_DoBill = (JobDriver_DoBillZLevels)actor.jobs.curDriver; - 236", true);
                JobDriver_DoBillZLevels jobDriver_DoBill = (JobDriver_DoBillZLevels)actor.jobs.curDriver;
                Log.Message("JobDriver_DoBillZLevels : JobDriver - FinishRecipeAndStartStoringProduct - if (curJob.RecipeDef.workSkill != null && !curJob.RecipeDef.UsesUnfinishedThing) - 237", true);
                if (curJob.RecipeDef.workSkill != null && !curJob.RecipeDef.UsesUnfinishedThing)
                {
                    Log.Message("JobDriver_DoBillZLevels : JobDriver - FinishRecipeAndStartStoringProduct - float xp = (float)jobDriver_DoBill.ticksSpentDoingRecipeWork * 0.1f * curJob.RecipeDef.workSkillLearnFactor; - 238", true);
                    float xp = (float)jobDriver_DoBill.ticksSpentDoingRecipeWork * 0.1f * curJob.RecipeDef.workSkillLearnFactor;
                    Log.Message("JobDriver_DoBillZLevels : JobDriver - FinishRecipeAndStartStoringProduct - actor.skills.GetSkill(curJob.RecipeDef.workSkill).Learn(xp); - 239", true);
                    actor.skills.GetSkill(curJob.RecipeDef.workSkill).Learn(xp);
                }
                List<Thing> ingredients = CalculateIngredients(curJob, actor);
                Log.Message("JobDriver_DoBillZLevels : JobDriver - FinishRecipeAndStartStoringProduct - Thing dominantIngredient = CalculateDominantIngredient(curJob, ingredients); - 241", true);
                Thing dominantIngredient = CalculateDominantIngredient(curJob, ingredients);
                Log.Message("JobDriver_DoBillZLevels : JobDriver - FinishRecipeAndStartStoringProduct - List<Thing> list = GenRecipe.MakeRecipeProducts(curJob.RecipeDef, actor, ingredients, dominantIngredient, jobDriver_DoBill.BillGiver).ToList(); - 242", true);
                List<Thing> list = GenRecipe.MakeRecipeProducts(curJob.RecipeDef, actor, ingredients, dominantIngredient, jobDriver_DoBill.BillGiver).ToList();
                Log.Message("JobDriver_DoBillZLevels : JobDriver - FinishRecipeAndStartStoringProduct - ConsumeIngredients(ingredients, curJob.RecipeDef, actor.Map); - 243", true);
                ConsumeIngredients(ingredients, curJob.RecipeDef, actor.Map);
                Log.Message("JobDriver_DoBillZLevels : JobDriver - FinishRecipeAndStartStoringProduct - curJob.bill.Notify_IterationCompleted(actor, ingredients); - 244", true);
                curJob.bill.Notify_IterationCompleted(actor, ingredients);
                Log.Message("JobDriver_DoBillZLevels : JobDriver - FinishRecipeAndStartStoringProduct - RecordsUtility.Notify_BillDone(actor, list); - 245", true);
                RecordsUtility.Notify_BillDone(actor, list);
                Log.Message("JobDriver_DoBillZLevels : JobDriver - FinishRecipeAndStartStoringProduct - UnfinishedThing unfinishedThing = curJob.GetTarget(TargetIndex.B).Thing as UnfinishedThing; - 246", true);
                UnfinishedThing unfinishedThing = curJob.GetTarget(TargetIndex.B).Thing as UnfinishedThing;
                Log.Message("JobDriver_DoBillZLevels : JobDriver - FinishRecipeAndStartStoringProduct - if (curJob.bill.recipe.WorkAmountTotal(unfinishedThing?.Stuff) >= 10000f && list.Count > 0) - 247", true);
                if (curJob.bill.recipe.WorkAmountTotal(unfinishedThing?.Stuff) >= 10000f && list.Count > 0)
                {
                    Log.Message("JobDriver_DoBillZLevels : JobDriver - FinishRecipeAndStartStoringProduct - TaleRecorder.RecordTale(TaleDefOf.CompletedLongCraftingProject, actor, list[0].GetInnerIfMinified().def); - 248", true);
                    TaleRecorder.RecordTale(TaleDefOf.CompletedLongCraftingProject, actor, list[0].GetInnerIfMinified().def);
                }
                Log.Message("JobDriver_DoBillZLevels : JobDriver - FinishRecipeAndStartStoringProduct - if (list.Any()) - 249", true);
                if (list.Any())
                {
                    Log.Message("JobDriver_DoBillZLevels : JobDriver - FinishRecipeAndStartStoringProduct - Find.QuestManager.Notify_ThingsProduced(actor, list); - 250", true);
                    Find.QuestManager.Notify_ThingsProduced(actor, list);
                }
                Log.Message("JobDriver_DoBillZLevels : JobDriver - FinishRecipeAndStartStoringProduct - if (list.Count == 0) - 251", true);
                if (list.Count == 0)
                {
                    Log.Message("JobDriver_DoBillZLevels : JobDriver - FinishRecipeAndStartStoringProduct - actor.jobs.EndCurrentJob(JobCondition.Succeeded); - 252", true);
                    actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
                else if (curJob.bill.GetStoreMode() == BillStoreModeDefOf.DropOnFloor)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        Log.Message("JobDriver_DoBillZLevels : JobDriver - FinishRecipeAndStartStoringProduct - if (!GenPlace.TryPlaceThing(list[i], actor.Position, actor.Map, ThingPlaceMode.Near)) - 254", true);
                        if (!GenPlace.TryPlaceThing(list[i], actor.Position, actor.Map, ThingPlaceMode.Near))
                        {
                            Log.Message("JobDriver_DoBillZLevels : JobDriver - FinishRecipeAndStartStoringProduct - Log.Error(string.Concat(actor, \" could not drop recipe product \", list[i], \" near \", actor.Position)); - 255", true);
                            Log.Error(string.Concat(actor, " could not drop recipe product ", list[i], " near ", actor.Position));
                        }
                    }
                    actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
                else
                {
                    Log.Message("JobDriver_DoBillZLevels : JobDriver - FinishRecipeAndStartStoringProduct - if (list.Count > 1) - 257", true);
                    if (list.Count > 1)
                    {
                        for (int j = 1; j < list.Count; j++)
                        {
                            Log.Message("JobDriver_DoBillZLevels : JobDriver - FinishRecipeAndStartStoringProduct - if (!GenPlace.TryPlaceThing(list[j], actor.Position, actor.Map, ThingPlaceMode.Near)) - 258", true);
                            if (!GenPlace.TryPlaceThing(list[j], actor.Position, actor.Map, ThingPlaceMode.Near))
                            {
                                Log.Message("JobDriver_DoBillZLevels : JobDriver - FinishRecipeAndStartStoringProduct - Log.Error(string.Concat(actor, \" could not drop recipe product \", list[j], \" near \", actor.Position)); - 259", true);
                                Log.Error(string.Concat(actor, " could not drop recipe product ", list[j], " near ", actor.Position));
                            }
                        }
                    }
                    IntVec3 foundCell = IntVec3.Invalid;
                    Log.Message("JobDriver_DoBillZLevels : JobDriver - FinishRecipeAndStartStoringProduct - if (curJob.bill.GetStoreMode() == BillStoreModeDefOf.BestStockpile) - 261", true);
                    if (curJob.bill.GetStoreMode() == BillStoreModeDefOf.BestStockpile)
                    {
                        Log.Message("JobDriver_DoBillZLevels : JobDriver - FinishRecipeAndStartStoringProduct - StoreUtility.TryFindBestBetterStoreCellFor(list[0], actor, actor.Map, StoragePriority.Unstored, actor.Faction, out foundCell); - 262", true);
                        StoreUtility.TryFindBestBetterStoreCellFor(list[0], actor, actor.Map, StoragePriority.Unstored, actor.Faction, out foundCell);
                    }
                    else if (curJob.bill.GetStoreMode() == BillStoreModeDefOf.SpecificStockpile)
                    {
                        Log.Message("JobDriver_DoBillZLevels : JobDriver - FinishRecipeAndStartStoringProduct - StoreUtility.TryFindBestBetterStoreCellForIn(list[0], actor, actor.Map, StoragePriority.Unstored, actor.Faction, curJob.bill.GetStoreZone().slotGroup, out foundCell); - 264", true);
                        StoreUtility.TryFindBestBetterStoreCellForIn(list[0], actor, actor.Map, StoragePriority.Unstored, actor.Faction, curJob.bill.GetStoreZone().slotGroup, out foundCell);
                    }
                    else
                    {
                        Log.Message("JobDriver_DoBillZLevels : JobDriver - FinishRecipeAndStartStoringProduct - Log.ErrorOnce(\"Unknown store mode\", 9158246); - 265", true);
                        Log.ErrorOnce("Unknown store mode", 9158246);
                    }
                    Log.Message("JobDriver_DoBillZLevels : JobDriver - FinishRecipeAndStartStoringProduct - if (foundCell.IsValid) - 266", true);
                    if (foundCell.IsValid)
                    {
                        Log.Message("JobDriver_DoBillZLevels : JobDriver - FinishRecipeAndStartStoringProduct - actor.carryTracker.TryStartCarry(list[0]); - 267", true);
                        actor.carryTracker.TryStartCarry(list[0]);
                        Log.Message("JobDriver_DoBillZLevels : JobDriver - FinishRecipeAndStartStoringProduct - curJob.targetB = foundCell; - 268", true);
                        curJob.targetB = foundCell;
                        Log.Message("JobDriver_DoBillZLevels : JobDriver - FinishRecipeAndStartStoringProduct - curJob.targetA = list[0]; - 269", true);
                        curJob.targetA = list[0];
                        Log.Message("JobDriver_DoBillZLevels : JobDriver - FinishRecipeAndStartStoringProduct - curJob.count = 99999; - 270", true);
                        curJob.count = 99999;
                    }
                    else
                    {
                        Log.Message("JobDriver_DoBillZLevels : JobDriver - FinishRecipeAndStartStoringProduct - if (!GenPlace.TryPlaceThing(list[0], actor.Position, actor.Map, ThingPlaceMode.Near)) - 271", true);
                        if (!GenPlace.TryPlaceThing(list[0], actor.Position, actor.Map, ThingPlaceMode.Near))
                        {
                            Log.Message("JobDriver_DoBillZLevels : JobDriver - FinishRecipeAndStartStoringProduct - Log.Error(string.Concat(\"Bill doer could not drop product \", list[0], \" near \", actor.Position)); - 272", true);
                            Log.Error(string.Concat("Bill doer could not drop product ", list[0], " near ", actor.Position));
                        }
                        actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                    }
                }
            };
            Log.Message("JobDriver_DoBillZLevels : JobDriver - FinishRecipeAndStartStoringProduct - return toil; - 275", true);
            return toil;
        }

        private static List<Thing> CalculateIngredients(Job job, Pawn actor)
        {
            Log.Message("JobDriver_DoBillZLevels : JobDriver - CalculateIngredients - UnfinishedThing unfinishedThing = job.GetTarget(TargetIndex.B).Thing as UnfinishedThing; - 276", true);
            UnfinishedThing unfinishedThing = job.GetTarget(TargetIndex.B).Thing as UnfinishedThing;
            Log.Message("JobDriver_DoBillZLevels : JobDriver - CalculateIngredients - if (unfinishedThing != null) - 277", true);
            if (unfinishedThing != null)
            {
                Log.Message("JobDriver_DoBillZLevels : JobDriver - CalculateIngredients - List<Thing> ingredients = unfinishedThing.ingredients; - 278", true);
                List<Thing> ingredients = unfinishedThing.ingredients;
                Log.Message("JobDriver_DoBillZLevels : JobDriver - CalculateIngredients - job.RecipeDef.Worker.ConsumeIngredient(unfinishedThing, job.RecipeDef, actor.Map); - 279", true);
                job.RecipeDef.Worker.ConsumeIngredient(unfinishedThing, job.RecipeDef, actor.Map);
                Log.Message("JobDriver_DoBillZLevels : JobDriver - CalculateIngredients - job.placedThings = null; - 280", true);
                job.placedThings = null;
                Log.Message("JobDriver_DoBillZLevels : JobDriver - CalculateIngredients - return ingredients; - 281", true);
                return ingredients;
            }
            List<Thing> list = new List<Thing>();
            Log.Message("JobDriver_DoBillZLevels : JobDriver - CalculateIngredients - if (job.placedThings != null) - 283", true);
            if (job.placedThings != null)
            {
                for (int i = 0; i < job.placedThings.Count; i++)
                {
                    Log.Message("JobDriver_DoBillZLevels : JobDriver - CalculateIngredients - if (job.placedThings[i].Count <= 0) - 284", true);
                    if (job.placedThings[i].Count <= 0)
                    {
                        Log.Message("JobDriver_DoBillZLevels : JobDriver - CalculateIngredients - Log.Error(string.Concat(\"PlacedThing \", job.placedThings[i], \" with count \", job.placedThings[i].Count, \" for job \", job)); - 285", true);
                        Log.Error(string.Concat("PlacedThing ", job.placedThings[i], " with count ", job.placedThings[i].Count, " for job ", job));
                        Log.Message("JobDriver_DoBillZLevels : JobDriver - CalculateIngredients - continue; - 286", true);
                        continue;
                    }
                    Thing thing = (job.placedThings[i].Count >= job.placedThings[i].thing.stackCount) ? job.placedThings[i].thing : job.placedThings[i].thing.SplitOff(job.placedThings[i].Count);
                    Log.Message("JobDriver_DoBillZLevels : JobDriver - CalculateIngredients - job.placedThings[i].Count = 0; - 288", true);
                    job.placedThings[i].Count = 0;
                    Log.Message("JobDriver_DoBillZLevels : JobDriver - CalculateIngredients - if (list.Contains(thing)) - 289", true);
                    if (list.Contains(thing))
                    {
                        Log.Message("JobDriver_DoBillZLevels : JobDriver - CalculateIngredients - Log.Error(\"Tried to add ingredient from job placed targets twice: \" + thing); - 290", true);
                        Log.Error("Tried to add ingredient from job placed targets twice: " + thing);
                        Log.Message("JobDriver_DoBillZLevels : JobDriver - CalculateIngredients - continue; - 291", true);
                        continue;
                    }
                    list.Add(thing);
                    Log.Message("JobDriver_DoBillZLevels : JobDriver - CalculateIngredients - if (job.RecipeDef.autoStripCorpses) - 293", true);
                    if (job.RecipeDef.autoStripCorpses)
                    {
                        Log.Message("JobDriver_DoBillZLevels : JobDriver - CalculateIngredients - (thing as IStrippable)?.Strip(); - 294", true);
                        (thing as IStrippable)?.Strip();
                    }
                }
            }
            job.placedThings = null;
            Log.Message("JobDriver_DoBillZLevels : JobDriver - CalculateIngredients - return list; - 296", true);
            return list;
        }

        private static Thing CalculateDominantIngredient(Job job, List<Thing> ingredients)
        {
            Log.Message("JobDriver_DoBillZLevels : JobDriver - CalculateDominantIngredient - UnfinishedThing uft = job.GetTarget(TargetIndex.B).Thing as UnfinishedThing; - 297", true);
            UnfinishedThing uft = job.GetTarget(TargetIndex.B).Thing as UnfinishedThing;
            Log.Message("JobDriver_DoBillZLevels : JobDriver - CalculateDominantIngredient - if (uft != null && uft.def.MadeFromStuff) - 298", true);
            if (uft != null && uft.def.MadeFromStuff)
            {
                Log.Message("JobDriver_DoBillZLevels : JobDriver - CalculateDominantIngredient - return uft.ingredients.First((Thing ing) => ing.def == uft.Stuff); - 299", true);
                return uft.ingredients.First((Thing ing) => ing.def == uft.Stuff);
            }
            Log.Message("JobDriver_DoBillZLevels : JobDriver - CalculateDominantIngredient - if (!ingredients.NullOrEmpty()) - 300", true);
            if (!ingredients.NullOrEmpty())
            {
                Log.Message("JobDriver_DoBillZLevels : JobDriver - CalculateDominantIngredient - if (job.RecipeDef.productHasIngredientStuff) - 301", true);
                if (job.RecipeDef.productHasIngredientStuff)
                {
                    Log.Message("JobDriver_DoBillZLevels : JobDriver - CalculateDominantIngredient - return ingredients[0]; - 302", true);
                    return ingredients[0];
                }
                Log.Message("JobDriver_DoBillZLevels : JobDriver - CalculateDominantIngredient - if (job.RecipeDef.products.Any((ThingDefCountClass x) => x.thingDef.MadeFromStuff)) - 303", true);
                if (job.RecipeDef.products.Any((ThingDefCountClass x) => x.thingDef.MadeFromStuff))
                {
                    Log.Message("JobDriver_DoBillZLevels : JobDriver - CalculateDominantIngredient - return ingredients.Where((Thing x) => x.def.IsStuff).RandomElementByWeight((Thing x) => x.stackCount); - 304", true);
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
                Log.Message("JobDriver_DoBillZLevels : JobDriver - ConsumeIngredients - recipe.Worker.ConsumeIngredient(ingredients[i], recipe, map); - 307", true);
                recipe.Worker.ConsumeIngredient(ingredients[i], recipe, map);
            }
        }

        private static Toil JumpToCollectNextIntoHandsForBill(Toil gotoGetTargetToil, TargetIndex ind)
        {
            Log.Message("JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - Toil toil = new Toil(); - 308", true);
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                Log.Message("JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - Pawn actor = toil.actor; - 309", true);
                Pawn actor = toil.actor;
                Log.Message("JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - if (actor.carryTracker.CarriedThing == null) - 310", true);
                if (actor.carryTracker.CarriedThing == null)
                {
                    Log.Message("JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - Log.Error(string.Concat(\"JumpToAlsoCollectTargetInQueue run on \", actor, \" who is not carrying something.\")); - 311", true);
                    Log.Error(string.Concat("JumpToAlsoCollectTargetInQueue run on ", actor, " who is not carrying something."));
                }
                else if (!actor.carryTracker.Full)
                {
                    Log.Message("JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - Job curJob = actor.jobs.curJob; - 313", true);
                    Job curJob = actor.jobs.curJob;
                    Log.Message("JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - List<LocalTargetInfo> targetQueue = curJob.GetTargetQueue(ind); - 314", true);
                    List<LocalTargetInfo> targetQueue = curJob.GetTargetQueue(ind);
                    Log.Message("JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - if (!targetQueue.NullOrEmpty()) - 315", true);
                    if (!targetQueue.NullOrEmpty())
                    {
                        Log.Message("JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - int num = 0; - 316", true);
                        int num = 0;
                        Log.Message("JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - int a; - 317", true);
                        int a;
                        while (true)
                        {
                            Log.Message("JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - if (num >= targetQueue.Count) - 318", true);
                            if (num >= targetQueue.Count)
                            {
                                Log.Message("JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - return; - 319", true);
                                return;
                            }
                            Log.Message("JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - if (GenAI.CanUseItemForWork(actor, targetQueue[num].Thing) && targetQueue[num].Thing.CanStackWith(actor.carryTracker.CarriedThing) && !((float)(actor.Position - targetQueue[num].Thing.Position).LengthHorizontalSquared > 64f)) - 320", true);
                            if (GenAI.CanUseItemForWork(actor, targetQueue[num].Thing) && targetQueue[num].Thing.CanStackWith(actor.carryTracker.CarriedThing) && !((float)(actor.Position - targetQueue[num].Thing.Position).LengthHorizontalSquared > 64f))
                            {
                                Log.Message("JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - int num2 = (actor.carryTracker.CarriedThing != null) ? actor.carryTracker.CarriedThing.stackCount : 0; - 321", true);
                                int num2 = (actor.carryTracker.CarriedThing != null) ? actor.carryTracker.CarriedThing.stackCount : 0;
                                Log.Message("JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - a = curJob.countQueue[num]; - 322", true);
                                a = curJob.countQueue[num];
                                Log.Message("JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - a = Mathf.Min(a, targetQueue[num].Thing.def.stackLimit - num2); - 323", true);
                                a = Mathf.Min(a, targetQueue[num].Thing.def.stackLimit - num2);
                                Log.Message("JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - a = Mathf.Min(a, actor.carryTracker.AvailableStackSpace(targetQueue[num].Thing.def)); - 324", true);
                                a = Mathf.Min(a, actor.carryTracker.AvailableStackSpace(targetQueue[num].Thing.def));
                                Log.Message("JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - if (a > 0) - 325", true);
                                if (a > 0)
                                {
                                    Log.Message("JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - break; - 326", true);
                                    break;
                                }
                            }
                            num++;
                        }
                        curJob.count = a;
                        Log.Message("JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - curJob.SetTarget(ind, targetQueue[num].Thing); - 329", true);
                        curJob.SetTarget(ind, targetQueue[num].Thing);
                        Log.Message("JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - curJob.countQueue[num] -= a; - 330", true);
                        curJob.countQueue[num] -= a;
                        Log.Message("JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - if (curJob.countQueue[num] <= 0) - 331", true);
                        if (curJob.countQueue[num] <= 0)
                        {
                            Log.Message("JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - curJob.countQueue.RemoveAt(num); - 332", true);
                            curJob.countQueue.RemoveAt(num);
                            Log.Message("JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - targetQueue.RemoveAt(num); - 333", true);
                            targetQueue.RemoveAt(num);
                        }
                        actor.jobs.curDriver.JumpToToil(gotoGetTargetToil);
                    }
                }
            };
            Log.Message("JobDriver_DoBillZLevels : JobDriver - JumpToCollectNextIntoHandsForBill - return toil; - 336", true);
            return toil;
        }
    }
}