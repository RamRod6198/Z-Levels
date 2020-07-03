using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace ZLevels
{
	public static class Toils_ZLevels
	{

        public static IEnumerable<Toil> GoToMap(Pawn pawn, Map dest, JobDriver instance)
        {
            Toil end = new Toil
            {
                initAction = delegate ()
                {
                    Log.Message("Toils_ZLevels - GoToMap - ZLogger.Message(\"if (pawn.Map == dest): \" + pawn.Map + \" - \" + dest); - 1", true);
                    ZLogger.Message("if (pawn.Map == dest): " + pawn.Map + " - " + dest);
                }
            };
            yield return new Toil
            {
                initAction = delegate ()
                {
                    Log.Message("Toils_ZLevels - GoToMap - if (pawn.Map == dest) - 3", true);
                    if (pawn.Map == dest)
                    {
                        Log.Message("Toils_ZLevels - GoToMap - instance.JumpToToil(end); - 4", true);
                        instance.JumpToToil(end);
                    }
                }
            };

            Toil setStairs = new Toil
            {
                initAction = delegate ()
                {
                    Log.Message("Toils_ZLevels - GoToMap - var ZTracker = ZUtils.ZTracker; - 6", true);
                    var ZTracker = ZUtils.ZTracker;
                    Log.Message("Toils_ZLevels - GoToMap - ZLogger.Message(\"Pawn: \" + pawn); - 7", true);
                    ZLogger.Message("Pawn: " + pawn);
                    Log.Message("Toils_ZLevels - GoToMap - ZLogger.Message(\"Pawn.map: \" + pawn.Map); - 8", true);
                    ZLogger.Message("Pawn.map: " + pawn.Map);
                    Log.Message("Toils_ZLevels - GoToMap - ZLogger.Message(\"Dest Map: \" + dest); - 9", true);
                    ZLogger.Message("Dest Map: " + dest);
                    Log.Message("Toils_ZLevels - GoToMap - ZLogger.Message(\"pawn.CurJob: \" + pawn.jobs.curJob); - 10", true);
                    ZLogger.Message("pawn.CurJob: " + pawn.jobs.curJob);
                    Log.Message("Toils_ZLevels - GoToMap - if (ZTracker.GetZIndexFor(pawn.Map) > ZTracker.GetZIndexFor(dest)) - 11", true);
                    if (ZTracker.GetZIndexFor(pawn.Map) > ZTracker.GetZIndexFor(dest))
                    {
                        Log.Message("Toils_ZLevels - GoToMap - var stairs = ZTracker.stairsDown[pawn.Map]; - 12", true);
                        var stairs = ZTracker.stairsDown[pawn.Map];
                        Log.Message("Toils_ZLevels - GoToMap - if (stairs?.Count() > 0) - 13", true);
                        if (stairs?.Count() > 0)
                        {
                            Log.Message("Toils_ZLevels - GoToMap - pawn.CurJob.targetC = new LocalTargetInfo(stairs.MinBy(x => IntVec3Utility.DistanceTo(pawn.Position, x.Position))); - 14", true);
                            pawn.CurJob.targetC = new LocalTargetInfo(stairs.MinBy(x => IntVec3Utility.DistanceTo(pawn.Position, x.Position)));
                        }
                    }
                    else if (ZTracker.GetZIndexFor(pawn.Map) < ZTracker.GetZIndexFor(dest))
                    {
                        Log.Message("Toils_ZLevels - GoToMap - var stairs = ZTracker.stairsUp[pawn.Map]; - 16", true);
                        var stairs = ZTracker.stairsUp[pawn.Map];
                        Log.Message("Toils_ZLevels - GoToMap - if (stairs?.Count() > 0) - 17", true);
                        if (stairs?.Count() > 0)
                        {
                            Log.Message("Toils_ZLevels - GoToMap - pawn.CurJob.targetC = new LocalTargetInfo(stairs.MinBy(y => IntVec3Utility.DistanceTo(pawn.Position, y.Position))); - 18", true);
                            pawn.CurJob.targetC = new LocalTargetInfo(stairs.MinBy(y => IntVec3Utility.DistanceTo(pawn.Position, y.Position)));
                        }
                    }
                    else
                    {
                        Log.Message("Toils_ZLevels - GoToMap - pawn.CurJob.targetC = null; - 19", true);
                        pawn.CurJob.targetC = null;
                    }

                }
            };
            Log.Message("Toils_ZLevels - GoToMap - var goToStairs = Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.OnCell); - 21", true);
            var goToStairs = Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.OnCell);
            Log.Message("Toils_ZLevels - GoToMap - Toil useStairs = Toils_General.Wait(60, 0); - 22", true);
            Toil useStairs = Toils_General.Wait(60, 0);
            Log.Message("Toils_ZLevels - GoToMap - ToilEffects.WithProgressBarToilDelay(useStairs, TargetIndex.C, false, -0.5f); - 23", true);
            ToilEffects.WithProgressBarToilDelay(useStairs, TargetIndex.C, false, -0.5f);
            //ToilFailConditions.FailOnDespawnedNullOrForbidden<Toil>(useStairs, TargetIndex.C);
            //ToilFailConditions.FailOnCannotTouch<Toil>(useStairs, TargetIndex.C, PathEndMode.OnCell);

            Toil teleport = new Toil
            {
                initAction = delegate ()
                {
                    Log.Message("Toils_ZLevels - GoToMap - var ZTracker = ZUtils.ZTracker; - 26", true);
                    var ZTracker = ZUtils.ZTracker;
                    Log.Message("Toils_ZLevels - GoToMap - if (pawn.CurJob.targetC.Thing is Building_StairsUp stairsUp) - 27", true);
                    if (pawn.CurJob.targetC.Thing is Building_StairsUp stairsUp)
                    {
                        Log.Message("Toils_ZLevels - GoToMap - Map map = ZTracker.GetUpperLevel(pawn.Map.Tile, pawn.Map); - 28", true);
                        Map map = ZTracker.GetUpperLevel(pawn.Map.Tile, pawn.Map);
                        Log.Message("Toils_ZLevels - GoToMap - if (map == null) - 29", true);
                        if (map == null)
                        {
                            Log.Message("Toils_ZLevels - GoToMap - map = ZTracker.CreateUpperLevel(pawn.Map, stairsUp.Position); - 30", true);
                            map = ZTracker.CreateUpperLevel(pawn.Map, stairsUp.Position);
                            Log.Message("Toils_ZLevels - GoToMap - if (stairsUp.pathToPreset != null && stairsUp.pathToPreset.Length > 0) - 31", true);
                            if (stairsUp.pathToPreset != null && stairsUp.pathToPreset.Length > 0)
                            {
                                Log.Message("Toils_ZLevels - GoToMap - var comp = map.GetComponent<MapComponentZLevel>(); - 32", true);
                                var comp = map.GetComponent<MapComponentZLevel>();
                                Log.Message("Toils_ZLevels - GoToMap - comp.DoGeneration = true; - 33", true);
                                comp.DoGeneration = true;
                                Log.Message("Toils_ZLevels - GoToMap - comp.path = stairsUp.pathToPreset; - 34", true);
                                comp.path = stairsUp.pathToPreset;
                            }
                            ZTracker.TeleportPawn(pawn, pawn.Position, map, true, false, true);
                        }
                        else
                        {
                            Log.Message("Toils_ZLevels - GoToMap - if (stairsUp.pathToPreset != null && stairsUp.pathToPreset.Length > 0) - 36", true);
                            if (stairsUp.pathToPreset != null && stairsUp.pathToPreset.Length > 0)
                            {
                                Log.Message("Toils_ZLevels - GoToMap - var comp = map.GetComponent<MapComponentZLevel>(); - 37", true);
                                var comp = map.GetComponent<MapComponentZLevel>();
                                Log.Message("Toils_ZLevels - GoToMap - comp.DoGeneration = true; - 38", true);
                                comp.DoGeneration = true;
                                Log.Message("Toils_ZLevels - GoToMap - comp.path = stairsUp.pathToPreset; - 39", true);
                                comp.path = stairsUp.pathToPreset;
                            }
                            ZTracker.TeleportPawn(pawn, pawn.Position, map, false, false, stairsUp.shouldSpawnStairsUpper);
                            Log.Message("Toils_ZLevels - GoToMap - stairsUp.shouldSpawnStairsUpper = false; - 41", true);
                            stairsUp.shouldSpawnStairsUpper = false;
                        }
                    }
                    else if (pawn.CurJob.targetC.Thing is Building_StairsDown stairsDown)
                    {
                        Log.Message("Toils_ZLevels - GoToMap - Map map = ZTracker.GetLowerLevel(pawn.Map.Tile, pawn.Map); - 43", true);
                        Map map = ZTracker.GetLowerLevel(pawn.Map.Tile, pawn.Map);
                        Log.Message("Toils_ZLevels - GoToMap - if (map == null) - 44", true);
                        if (map == null)
                        {
                            Log.Message("Toils_ZLevels - GoToMap - map = ZTracker.CreateLowerLevel(pawn.Map, stairsDown.Position); - 45", true);
                            map = ZTracker.CreateLowerLevel(pawn.Map, stairsDown.Position);
                            Log.Message("Toils_ZLevels - GoToMap - if (stairsDown.pathToPreset != null && stairsDown.pathToPreset.Length > 0) - 46", true);
                            if (stairsDown.pathToPreset != null && stairsDown.pathToPreset.Length > 0)
                            {
                                Log.Message("Toils_ZLevels - GoToMap - var comp = map.GetComponent<MapComponentZLevel>(); - 47", true);
                                var comp = map.GetComponent<MapComponentZLevel>();
                                Log.Message("Toils_ZLevels - GoToMap - comp.DoGeneration = true; - 48", true);
                                comp.DoGeneration = true;
                                Log.Message("Toils_ZLevels - GoToMap - comp.path = stairsDown.pathToPreset; - 49", true);
                                comp.path = stairsDown.pathToPreset;
                            }
                            ZTracker.TeleportPawn(pawn, pawn.Position, map, true, true);
                        }
                        else
                        {
                            Log.Message("Toils_ZLevels - GoToMap - if (stairsDown.pathToPreset != null && stairsDown.pathToPreset.Length > 0) - 51", true);
                            if (stairsDown.pathToPreset != null && stairsDown.pathToPreset.Length > 0)
                            {
                                Log.Message("Toils_ZLevels - GoToMap - var comp = map.GetComponent<MapComponentZLevel>(); - 52", true);
                                var comp = map.GetComponent<MapComponentZLevel>();
                                Log.Message("Toils_ZLevels - GoToMap - comp.DoGeneration = true; - 53", true);
                                comp.DoGeneration = true;
                                Log.Message("Toils_ZLevels - GoToMap - comp.path = stairsDown.pathToPreset; - 54", true);
                                comp.path = stairsDown.pathToPreset;
                            }
                            ZTracker.TeleportPawn(pawn, pawn.Position, map, false, stairsDown.shouldSpawnStairsBelow);
                            Log.Message("Toils_ZLevels - GoToMap - stairsDown.shouldSpawnStairsBelow = false; - 56", true);
                            stairsDown.shouldSpawnStairsBelow = false;
                        }
                    }

                    Log.Message("Toils_ZLevels - GoToMap - if (pawn.Map != dest) - 57", true);
                    if (pawn.Map != dest)
                    {
                        Log.Message("Toils_ZLevels - GoToMap - instance.JumpToToil(setStairs); - 58", true);
                        instance.JumpToToil(setStairs);
                    }
                }
            };

            yield return setStairs;
            yield return goToStairs;
            yield return useStairs;
            yield return teleport;
            yield return end;
        }
    }
}

