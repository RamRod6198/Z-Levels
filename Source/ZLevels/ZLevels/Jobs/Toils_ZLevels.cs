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
            Toil setStairs = GetSetStairs(pawn, dest);
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

        internal static Toil GetSetStairs(Pawn pawn, TargetInfo targetInfo, JobDriver_ZLevels instance)
        {
            return new Toil
            {
                initAction = delegate ()
                {
                    if (instance.GetCurrentStairs(targetInfo) is Building_Stairs stairs)
                    {
                        pawn.CurJob.targetC = new LocalTargetInfo(stairs);
                    }
                    else
                    {
                        pawn.CurJob.targetC = null;
                    }

                }
            };
        }
        internal static Toil GetSetStairs(Pawn pawn, Map dest)
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
                        if (stairs?.Any() ?? false)
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
                        if (stairs?.Any() ?? false)
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
                    ZLogger.Message($"{pawn} teleporting to {pawn.CurJob.targetC.Thing.Map}", debugLevel: DebugLevel.Pathfinding);
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

        public static Toil GetTeleport(Pawn pawn, Building_Stairs stairs)
        {
            return new Toil
            {
                initAction = delegate ()
                {
                    var ZTracker = ZUtils.ZTracker;
                    ZLogger.Message($"{pawn} teleporting to {pawn.CurJob.targetC.Thing.Map}", debugLevel: DebugLevel.Pathfinding);
                    if (stairs is Building_StairsUp stairsUp)
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
                    else if (stairs is Building_StairsDown stairsDown)
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
                }
            };
        }

        public static Toil IncrementIndex(JobDriver_ZLevels driver)
        {
            return new Toil
            {
                initAction = delegate ()
                {
                    driver.curIndex++;
                }
            };
        }

        public static IEnumerable<Toil> FindRouteWithStairs(Pawn pawn, TargetInfo targetInfo, JobDriver_ZLevels instance)
        {
            Toil startToil = new Toil
            {
                initAction = delegate ()
                {
                    ZLogger.Message($"{pawn} - {pawn.Map} - {targetInfo} - {targetInfo.Map}", debugLevel: DebugLevel.Pathfinding);
                    var route = instance.GetRoute(targetInfo);
                    for (int i = 0; i < route.Count; i++)
                    {
                        ZLogger.Message($"{i} - cur node - {route[i]}", debugLevel: DebugLevel.Pathfinding);
                    }
                    instance.curLocation = route[instance.curIndex].Location;
                    instance.curStairs = route[instance.curIndex].key;
                    ZLogger.Message($"index - {instance.curIndex}", debugLevel: DebugLevel.Pathfinding);
                    ZLogger.Message($"list - {route}", debugLevel: DebugLevel.Pathfinding);
                    ZLogger.Message($"cur route - {route[instance.curIndex]}", debugLevel: DebugLevel.Pathfinding);
                    ZLogger.Message($"key - {route[instance.curIndex].key}", debugLevel: DebugLevel.Pathfinding);
                    instance.curMap = pawn.Map;
                    ZLogger.Message($"{pawn} - {pawn.Map} - Find Route with Stairs called, calling FindRoute", debugLevel: DebugLevel.Pathfinding);
                }
            };
            yield return startToil.FailOn(() => instance.GetRoute(targetInfo) == null);
            Toil setStairs = Toils_ZLevels.GetSetStairs(pawn, targetInfo, instance);
            Toil useStairs = Toils_General.Wait(60, 0);
            useStairs.WithProgressBarToilDelay(TargetIndex.A);
            yield return setStairs;
            yield return Toils_Goto.GotoCell(instance.GetCurrentLocation(targetInfo), PathEndMode.OnCell);
            yield return useStairs;
            yield return Toils_ZLevels.GetTeleport(pawn, instance.GetCurrentStairs(targetInfo));
            yield return Toils_Goto.GotoCell(instance.GetCurrentLocation(targetInfo), PathEndMode.OnCell);
            yield return Toils_ZLevels.IncrementIndex(instance);
            yield return Toils_Jump.JumpIf(startToil, () => pawn.Map != targetInfo.Map);
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