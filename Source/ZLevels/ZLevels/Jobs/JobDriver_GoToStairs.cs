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
            return true;
        }
        public override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell);
            Toil useStairs = Toils_General.Wait(60, 0);
            ToilEffects.WithProgressBarToilDelay(useStairs, TargetIndex.A, false, -0.5f);
            ToilFailConditions.FailOnDespawnedNullOrForbidden<Toil>(useStairs, TargetIndex.A);
            ToilFailConditions.FailOnCannotTouch<Toil>(useStairs, TargetIndex.A, PathEndMode.OnCell);
            yield return useStairs;

            yield return new Toil
            {
                initAction = delegate ()
                {
                    var ZTracker = ZUtils.ZTracker;
                    Pawn pawn = GetActor();
                    if (TargetA.Thing is Building_StairsUp stairsUp)
                    {
                        Map map = ZTracker.GetUpperLevel(this.pawn.Map.Tile, this.pawn.Map);
                        if (map == null)
                        {
                            map = ZTracker.CreateUpperLevel(this.pawn.Map, stairsUp.Position);
                            if (!string.IsNullOrEmpty(stairsUp.pathToPreset))
                            {
                                var comp = ZUtils.GetMapComponentZLevel(map);
                                comp.DoGeneration = true;
                                comp.path = stairsUp.pathToPreset;
                            }
                            ZTracker.TeleportPawn(pawn, pawn.Position, map, true, false, true);
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(stairsUp.pathToPreset))
                            {
                                var comp = ZUtils.GetMapComponentZLevel(map);
                                comp.DoGeneration = true;
                                comp.path = stairsUp.pathToPreset;
                            }
                            ZTracker.TeleportPawn(pawn, pawn.Position, map, false, false, stairsUp.shouldSpawnStairsUpper);
                            stairsUp.shouldSpawnStairsUpper = false;
                        }
                    }

                    else if (TargetA.Thing is Building_StairsDown stairsDown)
                    {
                        Map map = ZTracker.GetLowerLevel(this.pawn.Map.Tile, this.pawn.Map);
                        if (map == null)
                        {
                            //ZLogger.Message("CREATING LOWER LEVEL AGAIG", true);
                            map = ZTracker.CreateLowerLevel(this.pawn.Map, stairsDown.Position);
                            if (!string.IsNullOrEmpty(stairsDown.pathToPreset))
                            {
                                var comp = ZUtils.GetMapComponentZLevel(map);
                                comp.DoGeneration = true;
                                comp.path = stairsDown.pathToPreset;
                            }
                            ZTracker.TeleportPawn(pawn, pawn.Position, map, true, true);
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(stairsDown.pathToPreset))
                            {
                                var comp = ZUtils.GetMapComponentZLevel(map);
                                comp.DoGeneration = true;
                                comp.path = stairsDown.pathToPreset;
                            }
                            ZTracker.TeleportPawn(pawn, pawn.Position, map, false, stairsDown.shouldSpawnStairsBelow);
                            stairsDown.shouldSpawnStairsBelow = false;
                        }
                    }
                }
            };
        }
    }
}

