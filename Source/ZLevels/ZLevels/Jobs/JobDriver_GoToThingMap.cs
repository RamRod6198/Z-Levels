using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace ZLevels
{
    public class JobDriver_GoToThingMap : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            Toil setStairs = new Toil
            {
                initAction = delegate ()
                {
                    var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                    Pawn pawn = GetActor();

                    Thing selectedStairs = null;
                    if (ZTracker.GetZIndexFor(pawn.Map) > ZTracker.GetZIndexFor(this.job.targetB.Thing.Map))
                    {
                        var stairs = ZTracker.stairsDown[pawn.Map];
                        if (stairs?.Count() > 0)
                        {
                            selectedStairs = stairs.MinBy(x => IntVec3Utility.DistanceTo(this.job.targetB.Thing.Position, x.Position));
                        }
                    }
                    else if (ZTracker.GetZIndexFor(pawn.Map) < ZTracker.GetZIndexFor(this.job.targetB.Thing.Map))
                    {
                        var stairs = ZTracker.stairsUp[pawn.Map];
                        if (stairs?.Count() > 0)
                        {
                            selectedStairs = stairs.MinBy(x => IntVec3Utility.DistanceTo(this.job.targetB.Thing.Position, x.Position));
                        }
                    }
                    this.job.targetA = new LocalTargetInfo(selectedStairs);
                }
            };
            yield return setStairs;

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
                            ZTracker.TeleportPawn(pawn, pawn.Position, map, true, false, true);
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

                    else if (TargetA.Thing is Building_StairsDown stairsDown)
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
                            ZTracker.TeleportPawn(pawn, pawn.Position, map, true, true);
                        }
                        else
                        {
                            if (stairsDown.pathToPreset != null && stairsDown.pathToPreset.Length > 0)
                            {
                                var comp = map.GetComponent<MapComponentZLevel>();
                                comp.DoGeneration = true;
                                comp.path = stairsDown.pathToPreset;
                            }
                            ZTracker.TeleportPawn(pawn, pawn.Position, map, false, stairsDown.shouldSpawnStairsBelow);
                            stairsDown.shouldSpawnStairsBelow = false;
                        }
                    }
                    try
                    {
                        ZLogger.Message("5 lastTick");
                        ZTracker.jobTracker[pawn].lastTickFood = Find.TickManager.TicksGame + 201;
                        ZTracker.jobTracker[pawn].lastTickJoy = Find.TickManager.TicksGame + 201;
                    }
                    catch { };
                    if (pawn.Map != job.targetB.Thing.Map)
                    {
                        JumpToToil(setStairs);
                    }
                }
            };
        }
    }
}

