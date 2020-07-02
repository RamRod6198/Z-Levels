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
        //public static IEnumerable<Toil> GoToDest(Pawn pawn, JobDriver instance)
        //{
        //    Toil setStairs = new Toil
        //    {
        //        initAction = delegate ()
        //        {
        //            var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
        //            ZLogger.Message("Pawn: " + pawn);
        //            ZLogger.Message("Pawn.map: " + pawn.Map);
        //            ZLogger.Message("Dest Map: " + ZTracker.jobTracker[pawn].dest);
        //            ZLogger.Message("pawn.CurJob: " + pawn.jobs.curJob);
        //            if (ZTracker.GetZIndexFor(pawn.Map) > ZTracker.GetZIndexFor(ZTracker.jobTracker[pawn].dest))
        //            {
        //                var stairs = ZTracker.stairsDown[pawn.Map];
        //                if (stairs?.Count() > 0)
        //                {
        //                    ZTracker.jobTracker[pawn].selectedStairs = stairs.MinBy(x => IntVec3Utility.DistanceTo(pawn.Position, x.Position));
        //                }
        //            }
        //            else if (ZTracker.GetZIndexFor(pawn.Map) < ZTracker.GetZIndexFor(ZTracker.jobTracker[pawn].dest))
        //            {
        //                var stairs = ZTracker.stairsUp[pawn.Map];
        //                if (stairs?.Count() > 0)
        //                {
        //                    ZTracker.jobTracker[pawn].selectedStairs = stairs.MinBy(x => IntVec3Utility.DistanceTo(pawn.Position, x.Position));
        //                }
        //            }
        //        }
        //    };
        //    yield return setStairs;
        //    yield return Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.OnCell);
        //    Toil useStairs = Toils_General.Wait(60, 0);
        //    ToilEffects.WithProgressBarToilDelay(useStairs, TargetIndex.C, false, -0.5f);
        //    ToilFailConditions.FailOnDespawnedNullOrForbidden<Toil>(useStairs, TargetIndex.C);
        //    //ToilFailConditions.FailOnCannotTouch<Toil>(useStairs, TargetIndex.C, PathEndMode.OnCell);
        //    yield return useStairs;
        //
        //    yield return new Toil
        //    {
        //        initAction = delegate ()
        //        {
        //            var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
        //            if (ZTracker.jobTracker[pawn].selectedStairs is Building_StairsUp stairsUp)
        //            {
        //                Map map = ZTracker.GetUpperLevel(pawn.Map.Tile, pawn.Map);
        //                if (map == null)
        //                {
        //                    map = ZTracker.CreateUpperLevel(pawn.Map, stairsUp.Position);
        //                    if (stairsUp.pathToPreset != null && stairsUp.pathToPreset.Length > 0)
        //                    {
        //                        var comp = map.GetComponent<MapComponentZLevel>();
        //                        comp.DoGeneration = true;
        //                        comp.path = stairsUp.pathToPreset;
        //                    }
        //                    ZTracker.TeleportPawn(pawn, pawn.Position, map, true, false, true);
        //                }
        //                else
        //                {
        //                    if (stairsUp.pathToPreset != null && stairsUp.pathToPreset.Length > 0)
        //                    {
        //                        var comp = map.GetComponent<MapComponentZLevel>();
        //                        comp.DoGeneration = true;
        //                        comp.path = stairsUp.pathToPreset;
        //                    }
        //                    ZTracker.TeleportPawn(pawn, pawn.Position, map, false, false, stairsUp.shouldSpawnStairsUpper);
        //                    stairsUp.shouldSpawnStairsUpper = false;
        //                }
        //            }
        //
        //            else if (ZTracker.jobTracker[pawn].selectedStairs is Building_StairsDown stairsDown)
        //            {
        //                Map map = ZTracker.GetLowerLevel(pawn.Map.Tile, pawn.Map);
        //                if (map == null)
        //                {
        //                    map = ZTracker.CreateLowerLevel(pawn.Map, stairsDown.Position);
        //                    if (stairsDown.pathToPreset != null && stairsDown.pathToPreset.Length > 0)
        //                    {
        //                        var comp = map.GetComponent<MapComponentZLevel>();
        //                        comp.DoGeneration = true;
        //                        comp.path = stairsDown.pathToPreset;
        //                    }
        //                    ZTracker.TeleportPawn(pawn, pawn.Position, map, true, true);
        //                }
        //                else
        //                {
        //                    if (stairsDown.pathToPreset != null && stairsDown.pathToPreset.Length > 0)
        //                    {
        //                        var comp = map.GetComponent<MapComponentZLevel>();
        //                        comp.DoGeneration = true;
        //                        comp.path = stairsDown.pathToPreset;
        //                    }
        //                    ZTracker.TeleportPawn(pawn, pawn.Position, map, false, stairsDown.shouldSpawnStairsBelow);
        //                    stairsDown.shouldSpawnStairsBelow = false;
        //                }
        //            }
        //
        //            if (pawn.Map != ZTracker.jobTracker[pawn].dest)
        //            {
        //                instance.JumpToToil(setStairs);
        //            }
        //        }
        //    };
        //}
    }
}

