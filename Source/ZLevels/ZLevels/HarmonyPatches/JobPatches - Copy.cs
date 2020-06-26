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
            base.ExposeData();
            Scribe_Values.Look(ref workLeft, "workLeft", 0f);
            Scribe_Values.Look(ref billStartTick, "billStartTick", 0);
            Scribe_Values.Look(ref ticksSpentDoingRecipeWork, "ticksSpentDoingRecipeWork", 0);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            try
            {
                ZLogger.Message("--------------------------");
                for (int i = job.targetQueueB.Count - 1; i >= 0; i--)
                {
                    var target = job.targetQueueB[i];

                    ZLogger.Message("-2 job.targetQueueB: " + target.Thing);
                    ZLogger.Message("-2 job.targetQueueB.Map: " + target.Thing.Map);
                    ZLogger.Message("-2 job.targetQueueB.stackCount: " + target.Thing.stackCount);
                    ZLogger.Message("-2 job.targetQueueB.countQueue: " + job.countQueue[i]);

                }
            }
            catch { }

            if (!pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed))
            {
                return false;
            }
            pawn.ReserveAsManyAsPossible(job.GetTargetQueue(TargetIndex.B), job);
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Toil toil = new Toil();
            yield return new Toil
            {
                initAction = delegate ()
                {
                    try
                    {
                        ZLogger.Message("--------------------------");
                        for (int i = job.targetQueueB.Count - 1; i >= 0; i--)
                        {
                            var target = job.targetQueueB[i];

                            ZLogger.Message("-1 job.targetQueueB: " + target.Thing);
                            ZLogger.Message("-1 job.targetQueueB.Map: " + target.Thing.Map);
                            ZLogger.Message("-1 job.targetQueueB.stackCount: " + target.Thing.stackCount);
                            ZLogger.Message("-1 job.targetQueueB.countQueue: " + job.countQueue[i]);

                        }
                    }
                    catch { }

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
            Toil gotoBillGiver = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            toil.initAction = delegate
            {
                if (job.targetQueueB != null && job.targetQueueB.Count == 1)
                {
                    UnfinishedThing unfinishedThing = job.targetQueueB[0].Thing as UnfinishedThing;
                    if (unfinishedThing != null)
                    {
                        unfinishedThing.BoundBill = (Bill_ProductionWithUft)job.bill;
                    }
                }
            };
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
                        ZLogger.Message("--------------------------");
                        for (int i = job.targetQueueB.Count - 1; i >= 0; i--)
                        {
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
            yield return getToHaulTarget;
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, putRemainderInQueue: true, subtractNumTakenFromJobCount: false, failIfStackCountLessThanJobCount: true);
            yield return JumpToCollectNextIntoHandsForBill(getToHaulTarget, TargetIndex.B);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell).FailOnDestroyedOrNull(TargetIndex.B);
            Toil findPlaceTarget2 = Toils_JobTransforms.SetTargetToIngredientPlaceCell(TargetIndex.A, TargetIndex.B, TargetIndex.C);
            yield return findPlaceTarget2;
            yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.C, findPlaceTarget2, storageMode: false);
            yield return Toils_Jump.JumpIfHaveTargetInQueue(TargetIndex.B, extract);
            yield return gotoBillGiver;
            yield return Toils_Recipe.MakeUnfinishedThingIfNeeded();
            yield return DoRecipeWork().FailOnDespawnedNullOrForbiddenPlacedThings().FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
            yield return FinishRecipeAndStartStoringProduct();
            if (!job.RecipeDef.products.NullOrEmpty() || !job.RecipeDef.specialProducts.NullOrEmpty())
            {
                yield return Toils_Reserve.Reserve(TargetIndex.B);
                findPlaceTarget2 = Toils_Haul.CarryHauledThingToCell(TargetIndex.B);
                yield return findPlaceTarget2;
                yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.B, findPlaceTarget2, storageMode: true, tryStoreInSameStorageIfSpotCantHoldWholeStack: true);
                Toil recount = new Toil();
                recount.initAction = delegate
                {
                    Bill_Production bill_Production = recount.actor.jobs.curJob.bill as Bill_Production;
                    if (bill_Production != null && bill_Production.repeatMode == BillRepeatModeDefOf.TargetCount)
                    {
                        base.Map.resourceCounter.UpdateResourceCounts();
                    }
                };
                yield return recount;
            }
        }

        public static Toil ExtractNextTargetFromQueue(TargetIndex ind, bool failIfCountFromQueueTooBig = true)
        {
            Toil toil = new Toil();
            toil.initAction = delegate
            {

                Pawn actor = toil.actor;
                Job curJob = actor.jobs.curJob;
                try
                {
                    ZLogger.Message("--------------------------");
                    for (int i = curJob.targetQueueB.Count - 1; i >= 0; i--)
                    {
                        var target = curJob.targetQueueB[i];

                        ZLogger.Message("4 job.targetQueueB: " + target.Thing);
                        ZLogger.Message("4 job.targetQueueB.Map: " + target.Thing.Map);
                        ZLogger.Message("4 job.targetQueueB.stackCount: " + target.Thing.stackCount);
                        ZLogger.Message("4 job.targetQueueB.countQueue: " + curJob.countQueue[i]);

                    }
                }
                catch { }
                List<LocalTargetInfo> targetQueue = curJob.GetTargetQueue(ind);
                if (!targetQueue.NullOrEmpty())
                {
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
                        ZLogger.Pause("Stack count of thing is lesser than countQueue");
                        actor.jobs.curDriver.EndJobWith(JobCondition.Incompletable);
                    }
                    else
                    {
                        curJob.SetTarget(ind, targetQueue[0]);
                        targetQueue.RemoveAt(0);
                        if (!curJob.countQueue.NullOrEmpty())
                        {
                            curJob.count = curJob.countQueue[0];
                            curJob.countQueue.RemoveAt(0);
                        }
                    }
                }
            };
            return toil;
        }

        public static Toil DoRecipeWork()
        {
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                Pawn actor3 = toil.actor;
                Job curJob3 = actor3.jobs.curJob;
                JobDriver_DoBillZLevels jobDriver_DoBill2 = (JobDriver_DoBillZLevels)actor3.jobs.curDriver;
                UnfinishedThing unfinishedThing3 = curJob3.GetTarget(TargetIndex.B).Thing as UnfinishedThing;
                if (unfinishedThing3 != null && unfinishedThing3.Initialized)
                {
                    jobDriver_DoBill2.workLeft = unfinishedThing3.workLeft;
                }
                else
                {
                    jobDriver_DoBill2.workLeft = curJob3.bill.recipe.WorkAmountTotal(unfinishedThing3?.Stuff);
                    if (unfinishedThing3 != null)
                    {
                        unfinishedThing3.workLeft = jobDriver_DoBill2.workLeft;
                    }
                }
                jobDriver_DoBill2.billStartTick = Find.TickManager.TicksGame;
                jobDriver_DoBill2.ticksSpentDoingRecipeWork = 0;
                curJob3.bill.Notify_DoBillStarted(actor3);
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
            toil.WithEffect(() => toil.actor.CurJob.bill.recipe.effectWorking, TargetIndex.A);
            toil.PlaySustainerOrSound(() => toil.actor.CurJob.bill.recipe.soundWorking);
            toil.WithProgressBar(TargetIndex.A, delegate
            {
                Pawn actor = toil.actor;
                Job curJob = actor.CurJob;
                UnfinishedThing unfinishedThing = curJob.GetTarget(TargetIndex.B).Thing as UnfinishedThing;
                return 1f - ((JobDriver_DoBillZLevels)actor.jobs.curDriver).workLeft / curJob.bill.recipe.WorkAmountTotal(unfinishedThing?.Stuff);
            });
            toil.FailOn((Func<bool>)delegate
            {
                RecipeDef recipeDef = toil.actor.CurJob.RecipeDef;
                if (recipeDef != null && recipeDef.interruptIfIngredientIsRotting)
                {
                    LocalTargetInfo target = toil.actor.CurJob.GetTarget(TargetIndex.B);
                    if (target.HasThing && (int)target.Thing.GetRotStage() > 0)
                    {
                        return true;
                    }
                }
                return toil.actor.CurJob.bill.suspended;
            });
            toil.activeSkill = (() => toil.actor.CurJob.bill.recipe.workSkill);
            return toil;
        }

        public static Toil FinishRecipeAndStartStoringProduct()
        {
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                Job curJob = actor.jobs.curJob;
                JobDriver_DoBillZLevels jobDriver_DoBill = (JobDriver_DoBillZLevels)actor.jobs.curDriver;
                if (curJob.RecipeDef.workSkill != null && !curJob.RecipeDef.UsesUnfinishedThing)
                {
                    float xp = (float)jobDriver_DoBill.ticksSpentDoingRecipeWork * 0.1f * curJob.RecipeDef.workSkillLearnFactor;
                    actor.skills.GetSkill(curJob.RecipeDef.workSkill).Learn(xp);
                }
                List<Thing> ingredients = CalculateIngredients(curJob, actor);
                Thing dominantIngredient = CalculateDominantIngredient(curJob, ingredients);
                List<Thing> list = GenRecipe.MakeRecipeProducts(curJob.RecipeDef, actor, ingredients, dominantIngredient, jobDriver_DoBill.BillGiver).ToList();
                ConsumeIngredients(ingredients, curJob.RecipeDef, actor.Map);
                curJob.bill.Notify_IterationCompleted(actor, ingredients);
                RecordsUtility.Notify_BillDone(actor, list);
                UnfinishedThing unfinishedThing = curJob.GetTarget(TargetIndex.B).Thing as UnfinishedThing;
                if (curJob.bill.recipe.WorkAmountTotal(unfinishedThing?.Stuff) >= 10000f && list.Count > 0)
                {
                    TaleRecorder.RecordTale(TaleDefOf.CompletedLongCraftingProject, actor, list[0].GetInnerIfMinified().def);
                }
                if (list.Any())
                {
                    Find.QuestManager.Notify_ThingsProduced(actor, list);
                }
                if (list.Count == 0)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
                else if (curJob.bill.GetStoreMode() == BillStoreModeDefOf.DropOnFloor)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (!GenPlace.TryPlaceThing(list[i], actor.Position, actor.Map, ThingPlaceMode.Near))
                        {
                            Log.Error(string.Concat(actor, " could not drop recipe product ", list[i], " near ", actor.Position));
                        }
                    }
                    actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
                else
                {
                    if (list.Count > 1)
                    {
                        for (int j = 1; j < list.Count; j++)
                        {
                            if (!GenPlace.TryPlaceThing(list[j], actor.Position, actor.Map, ThingPlaceMode.Near))
                            {
                                Log.Error(string.Concat(actor, " could not drop recipe product ", list[j], " near ", actor.Position));
                            }
                        }
                    }
                    IntVec3 foundCell = IntVec3.Invalid;
                    if (curJob.bill.GetStoreMode() == BillStoreModeDefOf.BestStockpile)
                    {
                        StoreUtility.TryFindBestBetterStoreCellFor(list[0], actor, actor.Map, StoragePriority.Unstored, actor.Faction, out foundCell);
                    }
                    else if (curJob.bill.GetStoreMode() == BillStoreModeDefOf.SpecificStockpile)
                    {
                        StoreUtility.TryFindBestBetterStoreCellForIn(list[0], actor, actor.Map, StoragePriority.Unstored, actor.Faction, curJob.bill.GetStoreZone().slotGroup, out foundCell);
                    }
                    else
                    {
                        Log.ErrorOnce("Unknown store mode", 9158246);
                    }
                    if (foundCell.IsValid)
                    {
                        actor.carryTracker.TryStartCarry(list[0]);
                        curJob.targetB = foundCell;
                        curJob.targetA = list[0];
                        curJob.count = 99999;
                    }
                    else
                    {
                        if (!GenPlace.TryPlaceThing(list[0], actor.Position, actor.Map, ThingPlaceMode.Near))
                        {
                            Log.Error(string.Concat("Bill doer could not drop product ", list[0], " near ", actor.Position));
                        }
                        actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                    }
                }
            };
            return toil;
        }

        private static List<Thing> CalculateIngredients(Job job, Pawn actor)
        {
            UnfinishedThing unfinishedThing = job.GetTarget(TargetIndex.B).Thing as UnfinishedThing;
            if (unfinishedThing != null)
            {
                List<Thing> ingredients = unfinishedThing.ingredients;
                job.RecipeDef.Worker.ConsumeIngredient(unfinishedThing, job.RecipeDef, actor.Map);
                job.placedThings = null;
                return ingredients;
            }
            List<Thing> list = new List<Thing>();
            if (job.placedThings != null)
            {
                for (int i = 0; i < job.placedThings.Count; i++)
                {
                    if (job.placedThings[i].Count <= 0)
                    {
                        Log.Error(string.Concat("PlacedThing ", job.placedThings[i], " with count ", job.placedThings[i].Count, " for job ", job));
                        continue;
                    }
                    Thing thing = (job.placedThings[i].Count >= job.placedThings[i].thing.stackCount) ? job.placedThings[i].thing : job.placedThings[i].thing.SplitOff(job.placedThings[i].Count);
                    job.placedThings[i].Count = 0;
                    if (list.Contains(thing))
                    {
                        Log.Error("Tried to add ingredient from job placed targets twice: " + thing);
                        continue;
                    }
                    list.Add(thing);
                    if (job.RecipeDef.autoStripCorpses)
                    {
                        (thing as IStrippable)?.Strip();
                    }
                }
            }
            job.placedThings = null;
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
                Pawn actor = toil.actor;
                if (actor.carryTracker.CarriedThing == null)
                {
                    Log.Error(string.Concat("JumpToAlsoCollectTargetInQueue run on ", actor, " who is not carrying something."));
                }
                else if (!actor.carryTracker.Full)
                {
                    Job curJob = actor.jobs.curJob;
                    List<LocalTargetInfo> targetQueue = curJob.GetTargetQueue(ind);
                    if (!targetQueue.NullOrEmpty())
                    {
                        int num = 0;
                        int a;
                        while (true)
                        {
                            if (num >= targetQueue.Count)
                            {
                                return;
                            }
                            if (GenAI.CanUseItemForWork(actor, targetQueue[num].Thing) && targetQueue[num].Thing.CanStackWith(actor.carryTracker.CarriedThing) && !((float)(actor.Position - targetQueue[num].Thing.Position).LengthHorizontalSquared > 64f))
                            {
                                int num2 = (actor.carryTracker.CarriedThing != null) ? actor.carryTracker.CarriedThing.stackCount : 0;
                                a = curJob.countQueue[num];
                                a = Mathf.Min(a, targetQueue[num].Thing.def.stackLimit - num2);
                                a = Mathf.Min(a, actor.carryTracker.AvailableStackSpace(targetQueue[num].Thing.def));
                                if (a > 0)
                                {
                                    break;
                                }
                            }
                            num++;
                        }
                        curJob.count = a;
                        curJob.SetTarget(ind, targetQueue[num].Thing);
                        curJob.countQueue[num] -= a;
                        if (curJob.countQueue[num] <= 0)
                        {
                            curJob.countQueue.RemoveAt(num);
                            targetQueue.RemoveAt(num);
                        }
                        actor.jobs.curDriver.JumpToToil(gotoGetTargetToil);
                    }
                }
            };
            return toil;
        }
    }
}

