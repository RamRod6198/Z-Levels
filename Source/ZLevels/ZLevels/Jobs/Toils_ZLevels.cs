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
            useStairs.WithProgressBarToilDelay(TargetIndex.C, false, -0.5f);
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
                            pawn.CurJob.targetC = new LocalTargetInfo(stairs.MinBy(x => pawn.Position.DistanceTo(x.Position)));
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
                            pawn.CurJob.targetC = new LocalTargetInfo(stairs.MinBy(y => pawn.Position.DistanceTo(y.Position)));
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



        public static IEnumerable<Toil> FindRouteWithStairs(Pawn pawn, TargetInfo targetInfo, JobDriver instance)
        {

            yield return new Toil
            {
                initAction = delegate ()
                {
                    ZLogger.Message($"Find Route with Stairs called, calling FindRoute", debugLevel: DebugLevel.Pathfinding);
                }
            };
            var nodeList =
                ZPathfinder.Instance.FindRoute(pawn.Position, targetInfo.Cell, pawn.Map, targetInfo.Map,
                    out float routeCost);


            //Register event here
            yield return new Toil
            {

                initAction = delegate ()
                {
                    for (int j = 0; j < nodeList.Count; ++j)
                    {
                        ZLogger.Message($"node {j} = {nodeList[j]}", debugLevel: DebugLevel.Pathfinding);
                    }
                }
            };

            for (int i = 1; i < nodeList.Count - 1; ++i)
            {

                Toil setStairs = Toils_ZLevels.GetSetStairs(pawn, nodeList[i].Map, instance);

                Toil useStairs = Toils_General.Wait(60, 0);
                useStairs.WithProgressBarToilDelay(TargetIndex.A);
                useStairs.FailOnStairAccess(pawn, nodeList[i].key);
                setStairs.FailOnStairAccess(pawn, nodeList[i].key);
                yield return setStairs;
                yield return Toils_Goto.GotoCell(nodeList[i].Location, PathEndMode.OnCell).FailOnStairAccess(pawn, nodeList[i].key);
                yield return useStairs;


                yield return Toils_ZLevels.GetTeleport(pawn, nodeList[i].Map, instance, setStairs).FailOnStairAccess(pawn, nodeList[i].key);


                //foreach (Toil t in Toils_ZLevels.GoToMap(pawn, stairList[i1 + 1].Map, this))
                //{
                //    yield return t;
                //}

            }

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell).FailOnForbidden(TargetIndex.A);
        }

        public static Toil FailOnStairAccess(this Toil f, Pawn actor, Building_Stairs stairs)
        {
            f.AddEndCondition(() =>
            {
                //Fail the job if any of these are true
                bool fail = stairs == null || stairs.Destroyed || stairs.IsForbidden(actor);
               // ZLogger.Message($"{debugMsg} {fail}");
                return fail ? JobCondition.Incompletable : JobCondition.Ongoing;
            });
            return f;
        }
    }
}