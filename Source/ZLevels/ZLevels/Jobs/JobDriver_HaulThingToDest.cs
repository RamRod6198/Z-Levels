using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace ZLevels
{
    public class JobDriver_HaulThingToDest : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Log.Message("JobDriver_HaulThingToDest : JobDriver - TryMakePreToilReservations - return true; - 1", true);
            return true;
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - var ZTracker = Current.Game.GetComponent<ZLevelsManager>(); - 2", true);
            var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
            Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - foreach (var toil in Toils_GoToMap(GetActor(), TargetA.Thing.Map, this)) - 3", true);
            foreach (var toil in Toils_GoToMap(GetActor(), TargetA.Thing.Map, this))
            {
                if (pawn.Map != pawn.CurJob.targetA.Thing.Map)
                {
                    yield return new Toil { initAction = delegate () { Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - yield return toil; - 4", true); } };
                    yield return toil;
                }
            }

            Toil reserveItem = Toils_Reserve.Reserve(TargetIndex.A);
            yield return new Toil { initAction = delegate () { Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - yield return reserveItem; - 6", true); } };
            yield return reserveItem;
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch)
                .FailOnDespawnedNullOrForbidden(TargetIndex.A).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(TargetIndex.A, putRemainderInQueue: false, 
                subtractNumTakenFromJobCount: true).FailOnDestroyedNullOrForbidden(TargetIndex.A);
            yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveItem, TargetIndex.A, TargetIndex.None, 
                takeFromValidStorage: true);
            
            Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - foreach (var toil in Toils_GoToMap(GetActor(), ZTracker.jobTracker[pawn].dest, this)) - 10", true);
            foreach (var toil in Toils_GoToMap(GetActor(), ZTracker.jobTracker[pawn].dest, this))
            {
                if (pawn.Map != ZTracker.jobTracker[pawn].dest)
                {
                    yield return new Toil { initAction = delegate () { Log.Message("JobDriver_HaulThingToDest : JobDriver - MakeNewToils - yield return toil; - 11", true); } };
                    yield return toil;
                }
            }
        }

        public static IEnumerable<Toil> Toils_GoToMap(Pawn pawn, Map dest, JobDriver instance)
        {
            Toil setStairs = new Toil
            {
                initAction = delegate ()
                {
                    Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - var ZTracker = Current.Game.GetComponent<ZLevelsManager>(); - 12", true);
                    var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                    Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - ZLogger.Message(\"Pawn: \" + pawn); - 13", true);
                    ZLogger.Message("Pawn: " + pawn);
                    Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - ZLogger.Message(\"Pawn.map: \" + pawn.Map); - 14", true);
                    ZLogger.Message("Pawn.map: " + pawn.Map);
                    Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - ZLogger.Message(\"Dest Map: \" + dest); - 15", true);
                    ZLogger.Message("Dest Map: " + dest);
                    Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - ZLogger.Message(\"pawn.CurJob: \" + pawn.jobs.curJob); - 16", true);
                    ZLogger.Message("pawn.CurJob: " + pawn.jobs.curJob);
                    Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - if (ZTracker.GetZIndexFor(pawn.Map) > ZTracker.GetZIndexFor(dest)) - 17", true);
                    if (ZTracker.GetZIndexFor(pawn.Map) > ZTracker.GetZIndexFor(dest))
                    {
                        Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - var stairs = ZTracker.stairsDown[pawn.Map]; - 18", true);
                        var stairs = ZTracker.stairsDown[pawn.Map];
                        Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - if (stairs?.Count() > 0) - 19", true);
                        if (stairs?.Count() > 0)
                        {
                            //ZTracker.jobTracker[pawn].selectedStairs = stairs.MinBy(x => IntVec3Utility.DistanceTo(pawn.Position, x.Position));
                            Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - pawn.CurJob.targetC = new LocalTargetInfo(stairs.MinBy(x => IntVec3Utility.DistanceTo(pawn.Position, x.Position))); - 21", true);
                            pawn.CurJob.targetC = new LocalTargetInfo(stairs.MinBy(x => IntVec3Utility.DistanceTo(pawn.Position, x.Position)));
                        }
                    }
                    else if (ZTracker.GetZIndexFor(pawn.Map) < ZTracker.GetZIndexFor(dest))
                    {
                        Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - var stairs = ZTracker.stairsUp[pawn.Map]; - 23", true);
                        var stairs = ZTracker.stairsUp[pawn.Map];
                        Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - if (stairs?.Count() > 0) - 24", true);
                        if (stairs?.Count() > 0)
                        {
                            //ZTracker.jobTracker[pawn].selectedStairs = stairs.MinBy(x => IntVec3Utility.DistanceTo(pawn.Position, x.Position));
                            Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - pawn.CurJob.targetC = new LocalTargetInfo(stairs.MinBy(x => IntVec3Utility.DistanceTo(pawn.Position, x.Position))); - 26", true);
                            pawn.CurJob.targetC = new LocalTargetInfo(stairs.MinBy(x => IntVec3Utility.DistanceTo(pawn.Position, x.Position)));
                        }
                    }
                    else
                    {
                        Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - pawn.CurJob.targetC = null; - 27", true);
                        pawn.CurJob.targetC = null;
                    }

                }
            };

            var goToStairs = Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.OnCell);

            Toil useStairs = Toils_General.Wait(60, 0);
            Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - ToilEffects.WithProgressBarToilDelay(useStairs, TargetIndex.C, false, -0.5f); - 31", true);
            ToilEffects.WithProgressBarToilDelay(useStairs, TargetIndex.C, false, -0.5f);
            //ToilFailConditions.FailOnDespawnedNullOrForbidden<Toil>(useStairs, TargetIndex.C);
            //ToilFailConditions.FailOnCannotTouch<Toil>(useStairs, TargetIndex.C, PathEndMode.OnCell);

            Toil teleport = new Toil
            {
                initAction = delegate ()
                {
                    Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - var ZTracker = Current.Game.GetComponent<ZLevelsManager>(); - 34", true);
                    var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                    Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - if (pawn.CurJob.targetC.Thing is Building_StairsUp stairsUp) - 35", true);
                    if (pawn.CurJob.targetC.Thing is Building_StairsUp stairsUp)
                    {
                        Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - Map map = ZTracker.GetUpperLevel(pawn.Map.Tile, pawn.Map); - 36", true);
                        Map map = ZTracker.GetUpperLevel(pawn.Map.Tile, pawn.Map);
                        Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - if (map == null) - 37", true);
                        if (map == null)
                        {
                            Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - map = ZTracker.CreateUpperLevel(pawn.Map, stairsUp.Position); - 38", true);
                            map = ZTracker.CreateUpperLevel(pawn.Map, stairsUp.Position);
                            Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - if (stairsUp.pathToPreset != null && stairsUp.pathToPreset.Length > 0) - 39", true);
                            if (stairsUp.pathToPreset != null && stairsUp.pathToPreset.Length > 0)
                            {
                                Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - var comp = map.GetComponent<MapComponentZLevel>(); - 40", true);
                                var comp = map.GetComponent<MapComponentZLevel>();
                                Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - comp.DoGeneration = true; - 41", true);
                                comp.DoGeneration = true;
                                Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - comp.path = stairsUp.pathToPreset; - 42", true);
                                comp.path = stairsUp.pathToPreset;
                            }
                            ZTracker.TeleportPawn(pawn, pawn.Position, map, true, false, true);
                        }
                        else
                        {
                            Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - if (stairsUp.pathToPreset != null && stairsUp.pathToPreset.Length > 0) - 44", true);
                            if (stairsUp.pathToPreset != null && stairsUp.pathToPreset.Length > 0)
                            {
                                Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - var comp = map.GetComponent<MapComponentZLevel>(); - 45", true);
                                var comp = map.GetComponent<MapComponentZLevel>();
                                Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - comp.DoGeneration = true; - 46", true);
                                comp.DoGeneration = true;
                                Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - comp.path = stairsUp.pathToPreset; - 47", true);
                                comp.path = stairsUp.pathToPreset;
                            }
                            ZTracker.TeleportPawn(pawn, pawn.Position, map, false, false, stairsUp.shouldSpawnStairsUpper);
                            Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - stairsUp.shouldSpawnStairsUpper = false; - 49", true);
                            stairsUp.shouldSpawnStairsUpper = false;
                        }
                    }
            
                    else if (pawn.CurJob.targetC.Thing is Building_StairsDown stairsDown)
                    {
                        Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - Map map = ZTracker.GetLowerLevel(pawn.Map.Tile, pawn.Map); - 51", true);
                        Map map = ZTracker.GetLowerLevel(pawn.Map.Tile, pawn.Map);
                        Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - if (map == null) - 52", true);
                        if (map == null)
                        {
                            Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - map = ZTracker.CreateLowerLevel(pawn.Map, stairsDown.Position); - 53", true);
                            map = ZTracker.CreateLowerLevel(pawn.Map, stairsDown.Position);
                            Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - if (stairsDown.pathToPreset != null && stairsDown.pathToPreset.Length > 0) - 54", true);
                            if (stairsDown.pathToPreset != null && stairsDown.pathToPreset.Length > 0)
                            {
                                Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - var comp = map.GetComponent<MapComponentZLevel>(); - 55", true);
                                var comp = map.GetComponent<MapComponentZLevel>();
                                Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - comp.DoGeneration = true; - 56", true);
                                comp.DoGeneration = true;
                                Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - comp.path = stairsDown.pathToPreset; - 57", true);
                                comp.path = stairsDown.pathToPreset;
                            }
                            ZTracker.TeleportPawn(pawn, pawn.Position, map, true, true);
                        }
                        else
                        {
                            Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - if (stairsDown.pathToPreset != null && stairsDown.pathToPreset.Length > 0) - 59", true);
                            if (stairsDown.pathToPreset != null && stairsDown.pathToPreset.Length > 0)
                            {
                                Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - var comp = map.GetComponent<MapComponentZLevel>(); - 60", true);
                                var comp = map.GetComponent<MapComponentZLevel>();
                                Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - comp.DoGeneration = true; - 61", true);
                                comp.DoGeneration = true;
                                Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - comp.path = stairsDown.pathToPreset; - 62", true);
                                comp.path = stairsDown.pathToPreset;
                            }
                            ZTracker.TeleportPawn(pawn, pawn.Position, map, false, stairsDown.shouldSpawnStairsBelow);
                            Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - stairsDown.shouldSpawnStairsBelow = false; - 64", true);
                            stairsDown.shouldSpawnStairsBelow = false;
                        }
                    }
            
                    Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - if (pawn.Map != dest) - 65", true);
                    if (pawn.Map != dest)
                    {
                        Log.Message("pawn.Map: " + pawn.Map);
                        Log.Message("dest: " + dest);
                        Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - instance.JumpToToil(setStairs); - 68", true);
                        instance.JumpToToil(setStairs);
                    }
                }
            };

            yield return setStairs;
            yield return new Toil { initAction = delegate () { Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - yield return goToStairs.EndOnDespawnedOrNull(TargetIndex.C, JobCondition.Ongoing); - 71", true); } };
            yield return goToStairs;
            yield return new Toil { initAction = delegate () { Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - yield return useStairs.EndOnDespawnedOrNull(TargetIndex.C, JobCondition.Ongoing); - 72", true); } };
            yield return useStairs;
            yield return new Toil { initAction = delegate () { Log.Message("JobDriver_HaulThingToDest : JobDriver - Toils_GoToMap - yield return teleport.EndOnDespawnedOrNull(TargetIndex.C, JobCondition.Ongoing); - 73", true); } };
            yield return teleport;
        }
    }
}

