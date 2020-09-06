using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Verse;
using Verse.AI;
using ZLevels.Properties;

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

                }
            };
            yield return new Toil
            {
                initAction = delegate ()
                {
                    if (pawn.Map == dest)
                    {
                        instance.JumpToToil(end);
                    }
                }
            };

            Toil setStairs = GetSetStairs(pawn, dest, instance);
            var goToStairs = Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.OnCell);
            if (pawn.HostileTo(Faction.OfPlayer))
            {
                pawn.CurJob.canBash = true;
            }
            Toil useStairs = Toils_General.Wait(60, 0);
            ToilEffects.WithProgressBarToilDelay(useStairs, TargetIndex.C, false, -0.5f);
            //ToilFailConditions.FailOnDespawnedNullOrForbidden<Toil>(useStairs, TargetIndex.C);
            //ToilFailConditions.FailOnCannotTouch<Toil>(useStairs, TargetIndex.C, PathEndMode.OnCell);

            Toil teleport = GetTeleport(pawn, dest, instance, setStairs);

            yield return setStairs;
            yield return goToStairs;
            yield return useStairs;
            yield return teleport;
            yield return end;
        }

        internal static Toil GetSetStairs(Pawn pawn, Map dest, JobDriver instance)
        {
            return new Toil
            {
                initAction = delegate ()
                {
                    var ZTracker = ZUtils.ZTracker;
                    ZTracker.ReCheckStairs();
                    if (ZTracker.GetZIndexFor(pawn.Map) > ZTracker.GetZIndexFor(dest))
                    {
                        var stairs = ZTracker.stairsDown[pawn.Map];
                        if (stairs?.Count() > 0)
                        {
                            pawn.CurJob.targetC = new LocalTargetInfo(stairs.MinBy(x => IntVec3Utility.DistanceTo(pawn.Position, x.Position)));
                        }
                        else
                        {
                            ZLogger.Pause(pawn + " cant find stairs down");
                        }
                    }
                    else if (ZTracker.GetZIndexFor(pawn.Map) < ZTracker.GetZIndexFor(dest))
                    {
                        var stairs = ZTracker.stairsUp[pawn.Map];
                        if (stairs?.Count() > 0)
                        {
                            pawn.CurJob.targetC = new LocalTargetInfo(stairs.MinBy(y => IntVec3Utility.DistanceTo(pawn.Position, y.Position)));
                        }
                        else
                        {
                            ZLogger.Pause(pawn + " cant find stairs up");
                        }
                    }
                    else
                    {
                        pawn.CurJob.targetC = null;
                    }

                }
            };
        }

        public static Toil GetTeleport(Pawn pawn, Map dest, JobDriver instance, Toil setStairs)
        {
            return new Toil
            {
                initAction = delegate ()
                {
                    var ZTracker = ZUtils.ZTracker;
                    if (pawn.CurJob.targetC.Thing is Building_StairsUp stairsUp)
                    {
                        Map map = ZTracker.GetUpperLevel(pawn.Map.Tile, pawn.Map);
                        if (map == null)
                        {
                            map = ZTracker.CreateUpperLevel(pawn.Map, stairsUp.Position);
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
                    else if (pawn.CurJob.targetC.Thing is Building_StairsDown stairsDown)
                    {
                        Map map = ZTracker.GetLowerLevel(pawn.Map.Tile, pawn.Map);
                        if (map != null)
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

                    if (pawn.Map != dest)
                    {
                        instance.JumpToToil(setStairs);
                    }
                }
            };
        }

        public static IEnumerable<Toil> DoNothingThings(Pawn pawn, Map dest, JobDriver instance)
        {
            //ZPathfinder check for route
            //If found, build a route with alternating toils of teleport and gotostairs
            //
            yield return null;
        }

        public static IEnumerable<Toil> GoToDestinationUsingStairs(Pawn pawn, LocalTargetInfo destInfo, JobDriver instance)
        {
            return GoToDestinationUsingStairs(pawn, destInfo.Cell, destInfo.Thing.Map, instance);
        }


        public static IEnumerable<Toil> GoToDestinationUsingStairs(Pawn pawn, IntVec3 dest, Map destMap, JobDriver instance)
        {
            List<ZPathfinder.DijkstraGraph.Node> stairList =
                ZPathfinder.Instance.FindRoute(pawn.Position, dest, pawn.Map, destMap, out float routeCost);

            yield return new Toil
            { initAction = delegate {
                for (int j = 0; j < stairList.Count; ++j)
                {
                    ZLogger.Message($"Stairs {j} = {stairList[j]}");
                } } };            

            for (int i = 0; i < stairList.Count - 1; ++i)
            {
                int i1 = i;
                yield return new Toil
                    { initAction = delegate{ ZLogger.Message($"Going to target {i1} boss"); } };
            
                Toil setStairs = Toils_ZLevels.GetSetStairs(pawn, stairList[i+1].Map, instance);
                Toil useStairs = Toils_General.Wait(60, 0);
                useStairs.WithProgressBarToilDelay(TargetIndex.A);

                yield return setStairs;
                yield return Toils_Goto.GotoCell(stairList[i].Location, PathEndMode.OnCell);
                yield return useStairs;


                yield return Toils_ZLevels.GetTeleport(pawn, stairList[i1+1].Map, instance, setStairs);


            }
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell);
        }

    }
}