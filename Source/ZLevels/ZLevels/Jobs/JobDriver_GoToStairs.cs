using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace ZLevels
{
    public class JobDriver_GoToStairs : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Log.Message("JobDriver_GoToStairs : JobDriver - TryMakePreToilReservations - return true; - 1", true);
            return true;
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return new Toil { initAction = delegate () { Log.Message("JobDriver_GoToStairs : JobDriver - MakeNewToils - yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell); - 2", true); } };
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell);
            Log.Message("JobDriver_GoToStairs : JobDriver - MakeNewToils - Toil useStairs = Toils_General.Wait(60, 0); - 3", true);
            Toil useStairs = Toils_General.Wait(60, 0);
            Log.Message("JobDriver_GoToStairs : JobDriver - MakeNewToils - ToilEffects.WithProgressBarToilDelay(useStairs, TargetIndex.A, false, -0.5f); - 4", true);
            ToilEffects.WithProgressBarToilDelay(useStairs, TargetIndex.A, false, -0.5f);
            Log.Message("JobDriver_GoToStairs : JobDriver - MakeNewToils - ToilFailConditions.FailOnDespawnedNullOrForbidden<Toil>(useStairs, TargetIndex.A); - 5", true);
            ToilFailConditions.FailOnDespawnedNullOrForbidden<Toil>(useStairs, TargetIndex.A);
            Log.Message("JobDriver_GoToStairs : JobDriver - MakeNewToils - ToilFailConditions.FailOnCannotTouch<Toil>(useStairs, TargetIndex.A, PathEndMode.OnCell); - 6", true);
            ToilFailConditions.FailOnCannotTouch<Toil>(useStairs, TargetIndex.A, PathEndMode.OnCell);
            yield return new Toil { initAction = delegate () { Log.Message("JobDriver_GoToStairs : JobDriver - MakeNewToils - yield return useStairs; - 7", true); } };
            yield return useStairs;

            yield return new Toil
            {
                initAction = delegate ()
                {
                    Log.Message("JobDriver_GoToStairs : JobDriver - MakeNewToils - var ZTracker = ZUtils.ZTracker; - 8", true);
                    var ZTracker = ZUtils.ZTracker;
                    Log.Message("JobDriver_GoToStairs : JobDriver - MakeNewToils - Pawn pawn = GetActor(); - 9", true);
                    Pawn pawn = GetActor();
                    Log.Message("JobDriver_GoToStairs : JobDriver - MakeNewToils - if (TargetA.Thing is Building_StairsUp stairsUp) - 10", true);
                    if (TargetA.Thing is Building_StairsUp stairsUp)
                    {
                        Log.Message("JobDriver_GoToStairs : JobDriver - MakeNewToils - Map map = ZTracker.GetUpperLevel(this.pawn.Map.Tile, this.pawn.Map); - 11", true);
                        Map map = ZTracker.GetUpperLevel(this.pawn.Map.Tile, this.pawn.Map);
                        Log.Message("JobDriver_GoToStairs : JobDriver - MakeNewToils - if (map == null) - 12", true);
                        if (map == null)
                        {
                            Log.Message("JobDriver_GoToStairs : JobDriver - MakeNewToils - map = ZTracker.CreateUpperLevel(this.pawn.Map, stairsUp.Position); - 13", true);
                            map = ZTracker.CreateUpperLevel(this.pawn.Map, stairsUp.Position);
                            Log.Message("JobDriver_GoToStairs : JobDriver - MakeNewToils - if (stairsUp.pathToPreset != null && stairsUp.pathToPreset.Length > 0) - 14", true);
                            if (stairsUp.pathToPreset != null && stairsUp.pathToPreset.Length > 0)
                            {
                                Log.Message("JobDriver_GoToStairs : JobDriver - MakeNewToils - var comp = map.GetComponent<MapComponentZLevel>(); - 15", true);
                                var comp = map.GetComponent<MapComponentZLevel>();
                                Log.Message("JobDriver_GoToStairs : JobDriver - MakeNewToils - comp.DoGeneration = true; - 16", true);
                                comp.DoGeneration = true;
                                Log.Message("JobDriver_GoToStairs : JobDriver - MakeNewToils - comp.path = stairsUp.pathToPreset; - 17", true);
                                comp.path = stairsUp.pathToPreset;
                            }
                            ZTracker.TeleportPawn(pawn, pawn.Position, map, true, false, true);
                        }
                        else
                        {
                            Log.Message("JobDriver_GoToStairs : JobDriver - MakeNewToils - if (stairsUp.pathToPreset != null && stairsUp.pathToPreset.Length > 0) - 19", true);
                            if (stairsUp.pathToPreset != null && stairsUp.pathToPreset.Length > 0)
                            {
                                Log.Message("JobDriver_GoToStairs : JobDriver - MakeNewToils - var comp = map.GetComponent<MapComponentZLevel>(); - 20", true);
                                var comp = map.GetComponent<MapComponentZLevel>();
                                Log.Message("JobDriver_GoToStairs : JobDriver - MakeNewToils - comp.DoGeneration = true; - 21", true);
                                comp.DoGeneration = true;
                                Log.Message("JobDriver_GoToStairs : JobDriver - MakeNewToils - comp.path = stairsUp.pathToPreset; - 22", true);
                                comp.path = stairsUp.pathToPreset;
                            }
                            ZTracker.TeleportPawn(pawn, pawn.Position, map, false, false, stairsUp.shouldSpawnStairsUpper);
                            Log.Message("JobDriver_GoToStairs : JobDriver - MakeNewToils - stairsUp.shouldSpawnStairsUpper = false; - 24", true);
                            stairsUp.shouldSpawnStairsUpper = false;
                        }
                    }

                    else if (TargetA.Thing is Building_StairsDown stairsDown)
                    {
                        Log.Message("JobDriver_GoToStairs : JobDriver - MakeNewToils - Map map = ZTracker.GetLowerLevel(this.pawn.Map.Tile, this.pawn.Map); - 26", true);
                        Map map = ZTracker.GetLowerLevel(this.pawn.Map.Tile, this.pawn.Map);
                        Log.Message("JobDriver_GoToStairs : JobDriver - MakeNewToils - if (map == null) - 27", true);
                        if (map == null)
                        {
                            Log.Message("JobDriver_GoToStairs : JobDriver - MakeNewToils - map = ZTracker.CreateLowerLevel(this.pawn.Map, stairsDown.Position); - 28", true);
                            map = ZTracker.CreateLowerLevel(this.pawn.Map, stairsDown.Position);
                            Log.Message("JobDriver_GoToStairs : JobDriver - MakeNewToils - if (stairsDown.pathToPreset != null && stairsDown.pathToPreset.Length > 0) - 29", true);
                            if (stairsDown.pathToPreset != null && stairsDown.pathToPreset.Length > 0)
                            {
                                Log.Message("JobDriver_GoToStairs : JobDriver - MakeNewToils - var comp = map.GetComponent<MapComponentZLevel>(); - 30", true);
                                var comp = map.GetComponent<MapComponentZLevel>();
                                Log.Message("JobDriver_GoToStairs : JobDriver - MakeNewToils - comp.DoGeneration = true; - 31", true);
                                comp.DoGeneration = true;
                                Log.Message("JobDriver_GoToStairs : JobDriver - MakeNewToils - comp.path = stairsDown.pathToPreset; - 32", true);
                                comp.path = stairsDown.pathToPreset;
                            }
                            ZTracker.TeleportPawn(pawn, pawn.Position, map, true, true);
                        }
                        else
                        {
                            Log.Message("JobDriver_GoToStairs : JobDriver - MakeNewToils - if (stairsDown.pathToPreset != null && stairsDown.pathToPreset.Length > 0) - 34", true);
                            if (stairsDown.pathToPreset != null && stairsDown.pathToPreset.Length > 0)
                            {
                                Log.Message("JobDriver_GoToStairs : JobDriver - MakeNewToils - var comp = map.GetComponent<MapComponentZLevel>(); - 35", true);
                                var comp = map.GetComponent<MapComponentZLevel>();
                                Log.Message("JobDriver_GoToStairs : JobDriver - MakeNewToils - comp.DoGeneration = true; - 36", true);
                                comp.DoGeneration = true;
                                Log.Message("JobDriver_GoToStairs : JobDriver - MakeNewToils - comp.path = stairsDown.pathToPreset; - 37", true);
                                comp.path = stairsDown.pathToPreset;
                            }
                            ZTracker.TeleportPawn(pawn, pawn.Position, map, false, stairsDown.shouldSpawnStairsBelow);
                            Log.Message("JobDriver_GoToStairs : JobDriver - MakeNewToils - stairsDown.shouldSpawnStairsBelow = false; - 39", true);
                            stairsDown.shouldSpawnStairsBelow = false;
                        }
                    }
                }
            };
        }
    }
}

