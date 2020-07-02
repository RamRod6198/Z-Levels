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
            Log.Message("JobDriver_GoToThingMap : JobDriver - TryMakePreToilReservations - return true; - 1", true);
            return true;
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            Log.Message("JobDriver_GoToThingMap : JobDriver - MakeNewToils - foreach (var toil in Toils_GoToThingMap(GetActor(), null, this.job.targetB.Thing, this)) - 2", true);
            foreach (var toil in Toils_GoToThingMap(GetActor(), null, this.job.targetB.Thing, this))
            {
                yield return new Toil { initAction = delegate () { Log.Message("JobDriver_GoToThingMap : JobDriver - MakeNewToils - yield return toil; - 3", true); } };
                yield return toil;
            }
        }

        public static IEnumerable<Toil> Toils_GoToThingMap(Pawn pawn, Thing selectedStairs, Thing thing, JobDriver instance)
        {
            Toil setStairs = new Toil
            {
                initAction = delegate ()
                {
                    Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - var ZTracker = Current.Game.GetComponent<ZLevelsManager>(); - 4", true);
                    var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                    Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - ZLogger.Message(\"Pawn: \" + pawn); - 5", true);
                    ZLogger.Message("Pawn: " + pawn);
                    Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - ZLogger.Message(\"Pawn.map: \" + pawn.Map); - 6", true);
                    ZLogger.Message("Pawn.map: " + pawn.Map);
                    Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - ZLogger.Message(\"thing: \" + thing); - 7", true);
                    ZLogger.Message("thing: " + thing);
                    Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - ZLogger.Message(\"thing.Map: \" + thing.Map); - 8", true);
                    ZLogger.Message("thing.Map: " + thing.Map);
                    Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - ZLogger.Message(\"pawn.CurJob: \" + pawn.jobs.curJob); - 9", true);
                    ZLogger.Message("pawn.CurJob: " + pawn.jobs.curJob);
                    Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - if (ZTracker.GetZIndexFor(pawn.Map) > ZTracker.GetZIndexFor(thing.Map)) - 10", true);
                    if (ZTracker.GetZIndexFor(pawn.Map) > ZTracker.GetZIndexFor(thing.Map))
                    {
                        Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - var stairs = ZTracker.stairsDown[pawn.Map]; - 11", true);
                        var stairs = ZTracker.stairsDown[pawn.Map];
                        Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - if (stairs?.Count() > 0) - 12", true);
                        if (stairs?.Count() > 0)
                        {
                            Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - selectedStairs = stairs.MinBy(x => IntVec3Utility.DistanceTo(thing.Position, x.Position)); - 13", true);
                            selectedStairs = stairs.MinBy(x => IntVec3Utility.DistanceTo(thing.Position, x.Position));
                        }
                    }
                    else if (ZTracker.GetZIndexFor(pawn.Map) < ZTracker.GetZIndexFor(thing.Map))
                    {
                        Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - var stairs = ZTracker.stairsUp[pawn.Map]; - 15", true);
                        var stairs = ZTracker.stairsUp[pawn.Map];
                        Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - if (stairs?.Count() > 0) - 16", true);
                        if (stairs?.Count() > 0)
                        {
                            Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - selectedStairs = stairs.MinBy(x => IntVec3Utility.DistanceTo(thing.Position, x.Position)); - 17", true);
                            selectedStairs = stairs.MinBy(x => IntVec3Utility.DistanceTo(thing.Position, x.Position));
                        }
                    }
                    pawn.jobs.curJob.targetC = new LocalTargetInfo(selectedStairs);
                }
            };
            yield return new Toil { initAction = delegate () { Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - yield return setStairs; - 20", true); } };
            yield return setStairs;
            yield return new Toil { initAction = delegate () { Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - yield return Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.OnCell); - 21", true); } };
            yield return Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.OnCell);
            Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - Toil useStairs = Toils_General.Wait(60, 0); - 22", true);
            Toil useStairs = Toils_General.Wait(60, 0);
            Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - ToilEffects.WithProgressBarToilDelay(useStairs, TargetIndex.C, false, -0.5f); - 23", true);
            ToilEffects.WithProgressBarToilDelay(useStairs, TargetIndex.C, false, -0.5f);
            Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - ToilFailConditions.FailOnDespawnedNullOrForbidden<Toil>(useStairs, TargetIndex.C); - 24", true);
            ToilFailConditions.FailOnDespawnedNullOrForbidden<Toil>(useStairs, TargetIndex.C);
            //ToilFailConditions.FailOnCannotTouch<Toil>(useStairs, TargetIndex.C, PathEndMode.OnCell);
            yield return new Toil { initAction = delegate () { Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - yield return useStairs; - 26", true); } };
            yield return useStairs;

            yield return new Toil
            {
                initAction = delegate ()
                {
                    Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - var ZTracker = Current.Game.GetComponent<ZLevelsManager>(); - 27", true);
                    var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                    Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - if (selectedStairs is Building_StairsUp stairsUp) - 28", true);
                    if (selectedStairs is Building_StairsUp stairsUp)
                    {
                        Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - Map map = ZTracker.GetUpperLevel(pawn.Map.Tile, pawn.Map); - 29", true);
                        Map map = ZTracker.GetUpperLevel(pawn.Map.Tile, pawn.Map);
                        Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - if (map == null) - 30", true);
                        if (map == null)
                        {
                            Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - map = ZTracker.CreateUpperLevel(pawn.Map, stairsUp.Position); - 31", true);
                            map = ZTracker.CreateUpperLevel(pawn.Map, stairsUp.Position);
                            Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - if (stairsUp.pathToPreset != null && stairsUp.pathToPreset.Length > 0) - 32", true);
                            if (stairsUp.pathToPreset != null && stairsUp.pathToPreset.Length > 0)
                            {
                                Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - var comp = map.GetComponent<MapComponentZLevel>(); - 33", true);
                                var comp = map.GetComponent<MapComponentZLevel>();
                                Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - comp.DoGeneration = true; - 34", true);
                                comp.DoGeneration = true;
                                Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - comp.path = stairsUp.pathToPreset; - 35", true);
                                comp.path = stairsUp.pathToPreset;
                            }
                            ZTracker.TeleportPawn(pawn, pawn.Position, map, true, false, true);
                        }
                        else
                        {
                            Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - if (stairsUp.pathToPreset != null && stairsUp.pathToPreset.Length > 0) - 37", true);
                            if (stairsUp.pathToPreset != null && stairsUp.pathToPreset.Length > 0)
                            {
                                Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - var comp = map.GetComponent<MapComponentZLevel>(); - 38", true);
                                var comp = map.GetComponent<MapComponentZLevel>();
                                Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - comp.DoGeneration = true; - 39", true);
                                comp.DoGeneration = true;
                                Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - comp.path = stairsUp.pathToPreset; - 40", true);
                                comp.path = stairsUp.pathToPreset;
                            }
                            ZTracker.TeleportPawn(pawn, pawn.Position, map, false, false, stairsUp.shouldSpawnStairsUpper);
                            Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - stairsUp.shouldSpawnStairsUpper = false; - 42", true);
                            stairsUp.shouldSpawnStairsUpper = false;
                        }
                    }

                    else if (selectedStairs is Building_StairsDown stairsDown)
                    {
                        Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - Map map = ZTracker.GetLowerLevel(pawn.Map.Tile, pawn.Map); - 44", true);
                        Map map = ZTracker.GetLowerLevel(pawn.Map.Tile, pawn.Map);
                        Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - if (map == null) - 45", true);
                        if (map == null)
                        {
                            Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - map = ZTracker.CreateLowerLevel(pawn.Map, stairsDown.Position); - 46", true);
                            map = ZTracker.CreateLowerLevel(pawn.Map, stairsDown.Position);
                            Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - if (stairsDown.pathToPreset != null && stairsDown.pathToPreset.Length > 0) - 47", true);
                            if (stairsDown.pathToPreset != null && stairsDown.pathToPreset.Length > 0)
                            {
                                Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - var comp = map.GetComponent<MapComponentZLevel>(); - 48", true);
                                var comp = map.GetComponent<MapComponentZLevel>();
                                Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - comp.DoGeneration = true; - 49", true);
                                comp.DoGeneration = true;
                                Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - comp.path = stairsDown.pathToPreset; - 50", true);
                                comp.path = stairsDown.pathToPreset;
                            }
                            ZTracker.TeleportPawn(pawn, pawn.Position, map, true, true);
                        }
                        else
                        {
                            Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - if (stairsDown.pathToPreset != null && stairsDown.pathToPreset.Length > 0) - 52", true);
                            if (stairsDown.pathToPreset != null && stairsDown.pathToPreset.Length > 0)
                            {
                                Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - var comp = map.GetComponent<MapComponentZLevel>(); - 53", true);
                                var comp = map.GetComponent<MapComponentZLevel>();
                                Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - comp.DoGeneration = true; - 54", true);
                                comp.DoGeneration = true;
                                Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - comp.path = stairsDown.pathToPreset; - 55", true);
                                comp.path = stairsDown.pathToPreset;
                            }
                            ZTracker.TeleportPawn(pawn, pawn.Position, map, false, stairsDown.shouldSpawnStairsBelow);
                            Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - stairsDown.shouldSpawnStairsBelow = false; - 57", true);
                            stairsDown.shouldSpawnStairsBelow = false;
                        }
                    }

                    Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - if (pawn.Map != thing.Map) - 58", true);
                    if (pawn.Map != thing.Map)
                    {
                        Log.Message("JobDriver_GoToThingMap : JobDriver - Toils_GoToThingMap - instance.JumpToToil(setStairs); - 59", true);
                        instance.JumpToToil(setStairs);
                    }
                }
            };
        }
    }
}

