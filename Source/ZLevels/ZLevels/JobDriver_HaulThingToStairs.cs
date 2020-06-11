using RimWorld;
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

        protected override IEnumerable<Toil> MakeNewToils()
        {
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
            //        Log.Message("this.pawn.CanReachImmediate(TargetA.Thing, PathEndMode.OnCell): "
            //            + this.pawn.CanReachImmediate(TargetA.Thing, PathEndMode.OnCell), true);
            //    }
            //};
            yield return useStairs;
            yield return new Toil()
            {
                initAction = () =>
                {
                    var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                    if (TargetA.Thing is Building_StairsUp sup)
                    {
                        Map map = ZTracker.GetUpperLevel(this.pawn.Map.Tile, this.pawn.Map);
                        if (map == null)
                        {
                            Log.Error("Cant go up because map is null");
                        }
                        else
                        {
                            ZTracker.TeleportPawn(GetActor(), GetActor().Position, map);
                        }
                    }
                    if (TargetA.Thing is Building_StairsDown sdo)
                    {
                        Map map = ZTracker.GetLowerLevel(this.pawn.Map.Tile, this.pawn.Map);
                        if (map == null)
                        {
                            map = sdo.Create(this.pawn.Map);
                            if (sdo.pathToPreset != null && sdo.pathToPreset.Length > 0)
                            {
                                var comp = map.GetComponent<MapComponentZLevel>();
                                comp.DoGeneration = true;
                                comp.path = sdo.pathToPreset;
                            }
                            ZTracker.TeleportPawn(GetActor(), GetActor().Position, map);
                        }
                        else
                        {
                            if (sdo.pathToPreset != null && sdo.pathToPreset.Length > 0)
                            {
                                var comp = map.GetComponent<MapComponentZLevel>();
                                comp.DoGeneration = true;
                                comp.path = sdo.pathToPreset;
                            }
                            ZTracker.TeleportPawn(GetActor(), GetActor().Position, map);
                        }
                    }
                }
            };
        }
    }
}

