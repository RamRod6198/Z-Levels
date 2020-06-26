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
            foreach (var toil in Toils_GoToThingMap(GetActor(), null, this.job.targetB.Thing, this))
            {
                yield return toil;
            }
        }


        public static IEnumerable<Toil> Toils_GoToThingMap(Pawn pawn, Thing selectedStairs, Thing thing, JobDriver instance)
        {
            Toil setStairs = new Toil
            {
                initAction = delegate ()
                {
                    Log.Message(" - Toils_GoToThingMap - var ZTracker = Current.Game.GetComponent<ZLevelsManager>(); - 1", true);
                    var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                    Log.Message(" - Toils_GoToThingMap - if (ZTracker.GetZIndexFor(pawn.Map) > ZTracker.GetZIndexFor(thing.Map)) - 2", true);
                    Log.Message("Pawn: " + pawn);
                    Log.Message("Pawn.map: " + pawn.Map);
                    Log.Message("thing: " + thing);
                    Log.Message("thing.Map: " + thing.Map);
                    Log.Message("pawn.CurJob: " + pawn.jobs.curJob);
                    if (ZTracker.GetZIndexFor(pawn.Map) > ZTracker.GetZIndexFor(thing.Map))
                    {
                        Log.Message(" - Toils_GoToThingMap - var stairs = ZTracker.stairsDown[pawn.Map]; - 3", true);
                        var stairs = ZTracker.stairsDown[pawn.Map];
                        Log.Message(" - Toils_GoToThingMap - if (stairs?.Count() > 0) - 4", true);
                        if (stairs?.Count() > 0)
                        {
                            Log.Message(" - Toils_GoToThingMap - selectedStairs = stairs.MinBy(x => IntVec3Utility.DistanceTo(thing.Position, x.Position)); - 5", true);
                            selectedStairs = stairs.MinBy(x => IntVec3Utility.DistanceTo(thing.Position, x.Position));
                        }
                    }
                    else if (ZTracker.GetZIndexFor(pawn.Map) < ZTracker.GetZIndexFor(thing.Map))
                    {
                        Log.Message(" - Toils_GoToThingMap - var stairs = ZTracker.stairsUp[pawn.Map]; - 7", true);
                        var stairs = ZTracker.stairsUp[pawn.Map];
                        Log.Message(" - Toils_GoToThingMap - if (stairs?.Count() > 0) - 8", true);
                        if (stairs?.Count() > 0)
                        {
                            Log.Message(" - Toils_GoToThingMap - selectedStairs = stairs.MinBy(x => IntVec3Utility.DistanceTo(thing.Position, x.Position)); - 9", true);
                            selectedStairs = stairs.MinBy(x => IntVec3Utility.DistanceTo(thing.Position, x.Position));
                        }
                    }
                    pawn.jobs.curJob.targetC = new LocalTargetInfo(selectedStairs);
                }
            };
            yield return setStairs;

            yield return Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.OnCell);
            Log.Message(" - Toils_GoToThingMap - Toil useStairs = Toils_General.Wait(60, 0); - 14", true);
            Toil useStairs = Toils_General.Wait(60, 0);
            Log.Message(" - Toils_GoToThingMap - ToilEffects.WithProgressBarToilDelay(useStairs, TargetIndex.C, false, -0.5f); - 15", true);
            ToilEffects.WithProgressBarToilDelay(useStairs, TargetIndex.C, false, -0.5f);
            Log.Message(" - Toils_GoToThingMap - ToilFailConditions.FailOnDespawnedNullOrForbidden<Toil>(useStairs, TargetIndex.C); - 16", true);
            ToilFailConditions.FailOnDespawnedNullOrForbidden<Toil>(useStairs, TargetIndex.C);
            Log.Message(" - Toils_GoToThingMap - ToilFailConditions.FailOnCannotTouch<Toil>(useStairs, TargetIndex.C, PathEndMode.OnCell); - 17", true);
            ToilFailConditions.FailOnCannotTouch<Toil>(useStairs, TargetIndex.C, PathEndMode.OnCell);
            yield return useStairs;

            yield return new Toil
            {
                initAction = delegate ()
                {
                    Log.Message(" - Toils_GoToThingMap - var ZTracker = Current.Game.GetComponent<ZLevelsManager>(); - 19", true);
                    var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                    Log.Message(" - Toils_GoToThingMap - if (selectedStairs is Building_StairsUp stairsUp) - 20", true);
                    if (selectedStairs is Building_StairsUp stairsUp)
                    {
                        Log.Message(" - Toils_GoToThingMap - Map map = ZTracker.GetUpperLevel(pawn.Map.Tile, pawn.Map); - 21", true);
                        Map map = ZTracker.GetUpperLevel(pawn.Map.Tile, pawn.Map);
                        Log.Message(" - Toils_GoToThingMap - if (map == null) - 22", true);
                        if (map == null)
                        {
                            Log.Message(" - Toils_GoToThingMap - map = ZTracker.CreateUpperLevel(pawn.Map, stairsUp.Position); - 23", true);
                            map = ZTracker.CreateUpperLevel(pawn.Map, stairsUp.Position);
                            Log.Message(" - Toils_GoToThingMap - if (stairsUp.pathToPreset != null && stairsUp.pathToPreset.Length > 0) - 24", true);
                            if (stairsUp.pathToPreset != null && stairsUp.pathToPreset.Length > 0)
                            {
                                Log.Message(" - Toils_GoToThingMap - var comp = map.GetComponent<MapComponentZLevel>(); - 25", true);
                                var comp = map.GetComponent<MapComponentZLevel>();
                                Log.Message(" - Toils_GoToThingMap - comp.DoGeneration = true; - 26", true);
                                comp.DoGeneration = true;
                                Log.Message(" - Toils_GoToThingMap - comp.path = stairsUp.pathToPreset; - 27", true);
                                comp.path = stairsUp.pathToPreset;
                            }
                            ZTracker.TeleportPawn(pawn, pawn.Position, map, true, false, true);
                        }
                        else
                        {
                            Log.Message(" - Toils_GoToThingMap - if (stairsUp.pathToPreset != null && stairsUp.pathToPreset.Length > 0) - 29", true);
                            if (stairsUp.pathToPreset != null && stairsUp.pathToPreset.Length > 0)
                            {
                                Log.Message(" - Toils_GoToThingMap - var comp = map.GetComponent<MapComponentZLevel>(); - 30", true);
                                var comp = map.GetComponent<MapComponentZLevel>();
                                Log.Message(" - Toils_GoToThingMap - comp.DoGeneration = true; - 31", true);
                                comp.DoGeneration = true;
                                Log.Message(" - Toils_GoToThingMap - comp.path = stairsUp.pathToPreset; - 32", true);
                                comp.path = stairsUp.pathToPreset;
                            }
                            ZTracker.TeleportPawn(pawn, pawn.Position, map, false, false, stairsUp.shouldSpawnStairsUpper);
                            Log.Message(" - Toils_GoToThingMap - stairsUp.shouldSpawnStairsUpper = false; - 34", true);
                            stairsUp.shouldSpawnStairsUpper = false;
                        }
                    }

                    else if (selectedStairs is Building_StairsDown stairsDown)
                    {
                        Log.Message(" - Toils_GoToThingMap - Map map = ZTracker.GetLowerLevel(pawn.Map.Tile, pawn.Map); - 36", true);
                        Map map = ZTracker.GetLowerLevel(pawn.Map.Tile, pawn.Map);
                        Log.Message(" - Toils_GoToThingMap - if (map == null) - 37", true);
                        if (map == null)
                        {
                            Log.Message(" - Toils_GoToThingMap - map = ZTracker.CreateLowerLevel(pawn.Map, stairsDown.Position); - 38", true);
                            map = ZTracker.CreateLowerLevel(pawn.Map, stairsDown.Position);
                            Log.Message(" - Toils_GoToThingMap - if (stairsDown.pathToPreset != null && stairsDown.pathToPreset.Length > 0) - 39", true);
                            if (stairsDown.pathToPreset != null && stairsDown.pathToPreset.Length > 0)
                            {
                                Log.Message(" - Toils_GoToThingMap - var comp = map.GetComponent<MapComponentZLevel>(); - 40", true);
                                var comp = map.GetComponent<MapComponentZLevel>();
                                Log.Message(" - Toils_GoToThingMap - comp.DoGeneration = true; - 41", true);
                                comp.DoGeneration = true;
                                Log.Message(" - Toils_GoToThingMap - comp.path = stairsDown.pathToPreset; - 42", true);
                                comp.path = stairsDown.pathToPreset;
                            }
                            ZTracker.TeleportPawn(pawn, pawn.Position, map, true, true);
                        }
                        else
                        {
                            Log.Message(" - Toils_GoToThingMap - if (stairsDown.pathToPreset != null && stairsDown.pathToPreset.Length > 0) - 44", true);
                            if (stairsDown.pathToPreset != null && stairsDown.pathToPreset.Length > 0)
                            {
                                Log.Message(" - Toils_GoToThingMap - var comp = map.GetComponent<MapComponentZLevel>(); - 45", true);
                                var comp = map.GetComponent<MapComponentZLevel>();
                                Log.Message(" - Toils_GoToThingMap - comp.DoGeneration = true; - 46", true);
                                comp.DoGeneration = true;
                                Log.Message(" - Toils_GoToThingMap - comp.path = stairsDown.pathToPreset; - 47", true);
                                comp.path = stairsDown.pathToPreset;
                            }
                            ZTracker.TeleportPawn(pawn, pawn.Position, map, false, stairsDown.shouldSpawnStairsBelow);
                            Log.Message(" - Toils_GoToThingMap - stairsDown.shouldSpawnStairsBelow = false; - 49", true);
                            stairsDown.shouldSpawnStairsBelow = false;
                        }
                    }

                    Log.Message(" - Toils_GoToThingMap - if (pawn.Map != thing.Map) - 50", true);
                    if (pawn.Map != thing.Map)
                    {
                        Log.Message(" - Toils_GoToThingMap - instance.JumpToToil(setStairs); - 51", true);
                        instance.JumpToToil(setStairs);
                    }
                }
            };
        }
    }
}
