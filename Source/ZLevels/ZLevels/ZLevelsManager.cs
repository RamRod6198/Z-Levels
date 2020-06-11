using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ZLevels
{
    public class ZLevelsManager : GameComponent
    {
        public ZLevelsManager()
        {

        }

        public ZLevelsManager(Game game)
        {

        }

        public override void GameComponentOnGUI()
        {
            base.GameComponentOnGUI();
            this.CheckHotkeys();
        }

        public void CheckHotkeys()
        {
            bool keyDownEvent = ZLevelsDefOf.ZL_switchToUpperMap.KeyDownEvent;
            if (keyDownEvent)
            {
                Map mapToSwitch = this.GetUpperLevel(Find.CurrentMap.Tile, Find.CurrentMap);
                if (mapToSwitch != null)
                {
                    var pos = Current.Game.CurrentMap.rememberedCameraPos.rootPos;
                    Current.Game.CurrentMap = mapToSwitch;
                    Find.CameraDriver.JumpToCurrentMapLoc(pos);
                }
                Event.current.Use();
            }
            bool keyDownEvent2 = ZLevelsDefOf.ZL_switchToLowerMap.KeyDownEvent;
            if (keyDownEvent2)
            {
                Map mapToSwitch = this.GetLowerLevel(Find.CurrentMap.Tile, Find.CurrentMap);
                if (mapToSwitch != null)
                {
                    var pos = Current.Game.CurrentMap.rememberedCameraPos.rootPos;
                    Current.Game.CurrentMap = mapToSwitch;
                    Find.CameraDriver.JumpToCurrentMapLoc(pos);
                }
                Event.current.Use();
            }
        }

        public void SaveArea(Pawn pawn)
        {
            if (this.activeAreas == null) this.activeAreas = new Dictionary<Pawn, ActiveArea>();
            if (this.activeAreas.ContainsKey(pawn))
            {
                if (this.activeAreas[pawn].activeAreas == null)
                {
                    this.activeAreas[pawn].activeAreas = new Dictionary<Map, Area>()
                    {
                        {pawn.Map, pawn.playerSettings.AreaRestriction}
                    };
                }
                else
                {
                    this.activeAreas[pawn].activeAreas[pawn.Map] = pawn.playerSettings.AreaRestriction;
                }
            }
            else
            {
                this.activeAreas[pawn] = new ActiveArea();
                this.activeAreas[pawn].activeAreas = new Dictionary<Map, Area>()
                {
                    {pawn.Map, pawn.playerSettings.AreaRestriction}
                };
            }

            //foreach (var test in this.activeAreas)
            //{
            //    Log.Message("Pawn: " + test.Key);
            //    foreach (var d in test.Value.activeAreas)
            //    {
            //        Log.Message("ActiveAreas: " + d);
            //    }
            //}
        }

        public void LoadArea(Pawn pawn)
        {
            if (this.activeAreas.ContainsKey(pawn) &&
                this.activeAreas[pawn].activeAreas.ContainsKey(pawn.Map))
            {
                pawn.playerSettings.AreaRestriction = this.activeAreas[pawn].activeAreas[pawn.Map];
            }
            else
            {
                pawn.playerSettings.AreaRestriction = null;
            }
        }

        public Map GetUpperLevel(int tile, Map map)
        {
            //Log.Message("Index to get: " + this.GetZIndexFor(map));
            if (this.ZLevelsTracker != null && this.ZLevelsTracker.ContainsKey(tile) 
                && this.ZLevelsTracker[tile].ZLevels.ContainsKey(this.GetZIndexFor(map) + 1))
            {
                //foreach (var d in this.ZLevelsTracker[tile].ZLevels)
                //{
                //    Log.Message("Data: " + d.Key + " - " + d.Value);
                //}
                //Log.Message("Getting: " + this.ZLevelsTracker[tile].ZLevels[this.GetZIndexFor(map) + 1]);

                //Log.Message("Z_Levels contains key, getting map:" + Z_Levels[Z_LevelIndex + 1]);
                return this.ZLevelsTracker[tile].ZLevels[this.GetZIndexFor(map) + 1];
            }
            return null;
        }
        public Map GetLowerLevel(int tile, Map map)
        {
            //Log.Message("Current map index: " + Z_LevelIndex);
            //Log.Message("Trying to get index:" + (Z_LevelIndex - 1));
            if (this.ZLevelsTracker != null && this.ZLevelsTracker.ContainsKey(tile) 
                && this.ZLevelsTracker[tile].ZLevels.ContainsKey(this.GetZIndexFor(map) - 1))
            {
                //Log.Message("Z_Levels contains key, getting map:" + Z_Levels[Z_LevelIndex + 1]);
                return this.ZLevelsTracker[tile].ZLevels[this.GetZIndexFor(map) - 1];
            }
            return null;
        }

        public List<Map> GetAllMaps(int tile)
        {
            List<Map> maps = new List<Map>();
            try
            {
                foreach (var map in this.ZLevelsTracker[tile].ZLevels.Values)
                {
                    maps.Add(map);
                }
                return maps;
            }
            catch
            {
                Log.Error("GetAllMaps returned null on " + tile);
                return null;
            }
        }

        public int GetZIndexFor(Map map)
        {
            var comp = map.GetComponent<MapComponentZLevel>();
            return comp.Z_LevelIndex;
        }

        public Map GetMapByIndex(int tile, int index)
        {
            return this.ZLevelsTracker[tile].ZLevels[index];
        }

        public string GetMapInfo(Map map)
        {
            return "(" + map + " - Level " + GetZIndexFor(map) + ")";
        }

        public bool TryRegisterMap(Map map, int index)
        {
            if (this.ZLevelsTracker == null)
            {
                this.ZLevelsTracker = new Dictionary<int, ZLevelData>();
            }
            if (this.ZLevelsTracker.ContainsKey(map.Tile))
            {
                if (this.ZLevelsTracker[map.Tile].ZLevels == null)
                    this.ZLevelsTracker[map.Tile].ZLevels = new Dictionary<int, Map>();
                this.ZLevelsTracker[map.Tile].ZLevels[index] = map;
                Log.Message("Registering " + this.GetMapInfo(map) + " for index: " + index);
                return true;
            }
            else
            {
                this.ZLevelsTracker[map.Tile] = new ZLevelData();
                this.ZLevelsTracker[map.Tile].ZLevels = new Dictionary<int, Map>();
                this.ZLevelsTracker[map.Tile].ZLevels[index] = map;
                Log.Message("Registering " + this.GetMapInfo(map) + " for index: " + index);
                return true;
            }
            return false;
        }

        //public override void GameComponentTick()
        //{
        //    base.GameComponentTick();
        //    if (Find.TickManager.TicksGame % 200 == 0)
        //    {
        //        foreach (var t in this.ZLevelsTracker)
        //        {
        //            foreach (var d in this.ZLevelsTracker[t.Key].ZLevels)
        //            {
        //                Log.Message(this.GetMapInfo(d.Value) + " - " + d.Value.weatherManager.curWeather
        //                    + " - " + d.Value.weatherManager.curWeatherAge + " - " + d.Value.weatherManager.lastWeather);
        //                
        //            }
        //        }
        //        Log.Message("========================");
        //    }
        //}
        public List<Job> HaulThingToDest(Pawn pawn, Thing thing, Map dest, ref IntVec3 lastStairsPosition, ref bool fail)
        {
            List<Job> tempJobs = new List<Job>();
            //Log.Message(pawn + " - ???++++++++++++++++++++++++++++++++++++++++???");
            //Log.Message(pawn + " - Hauling " + thing + "(" + thing.Map + ") from " + pawn.Map + " to " + dest);
            //Log.Message(pawn + " - Current pawn map: " + pawn.Map);
            //Log.Message(pawn + " - Thing map: " + thing.Map);
            //Log.Message(pawn + " - Dest map: " + dest);
            if (this.GetZIndexFor(thing.Map) > this.GetZIndexFor(dest))
            {
                Log.Message("2 - Build tree (HaulThingToDest): " + pawn + " - Going down");
                foreach (var map in this.ZLevelsTracker[pawn.Map.Tile].ZLevels.Values.OrderByDescending(x => this.GetZIndexFor(x)))
                {
                    if (this.GetZIndexFor(thing.Map) >= this.GetZIndexFor(map) &&
                        this.GetZIndexFor(map) >= this.GetZIndexFor(dest))
                    {
                        if (map != dest)
                        {
                            var stairs = map.listerThings.AllThings.Where(x => x is Building_StairsDown && x.Spawned).ToList();
                            if (stairs?.Count() > 0)
                            {
                                var selectedStairs = GenClosest.ClosestThing_Global(lastStairsPosition, stairs, 99999f);
                                if (selectedStairs != null)
                                {
                                    lastStairsPosition = selectedStairs.Position;
                                    Job gotoStairs = null;
                                    if (thing.Map == map)
                                    {
                                        Log.Message("Build tree (HaulThingToDest): " + pawn + " - Hauling " + thing + " to " 
                                            + selectedStairs + " in " + this.GetMapInfo(map));
                                        gotoStairs = JobMaker.MakeJob(ZLevelsDefOf.ZL_HaulThingToStairs,
                                            selectedStairs, thing);
                                    }
                                    else
                                    {
                                        Log.Message("Build tree (HaulThingToDest): " + pawn + " - Finding and using " + selectedStairs 
                                            + " in " + this.GetMapInfo(map));
                                        gotoStairs = JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToStairs, selectedStairs);
                                    }
                                    tempJobs.Add(gotoStairs);
                                }
                                else
                                {
                                    fail = true;
                                }
                            }
                            else
                            {
                                fail = true;
                            }
                        }
                    }
                }
            }
            else if (this.GetZIndexFor(thing.Map) < this.GetZIndexFor(dest))
            {
                Log.Message("2 - Build tree (HaulThingToDest): " + pawn + " - Going up");
                foreach (var map in this.ZLevelsTracker[pawn.Map.Tile].ZLevels.Values.OrderBy(x => this.GetZIndexFor(x)))
                {
                    if (this.GetZIndexFor(thing.Map) <= this.GetZIndexFor(map) &&
                        this.GetZIndexFor(map) <= this.GetZIndexFor(dest))
                    {
                        if (map != dest)
                        {
                            var stairs = map.listerThings.AllThings.Where(x => x is Building_StairsUp && x.Spawned).ToList();
                            if (stairs?.Count() > 0)
                            {
                                var selectedStairs = GenClosest.ClosestThing_Global(lastStairsPosition, stairs, 99999f);
                                if (selectedStairs != null)
                                {
                                    lastStairsPosition = selectedStairs.Position;
                                    Job gotoStairs = null;
                                    if (thing.Map == map)
                                    {
                                        Log.Message("Build tree (HaulThingToDest): " + pawn + " - Hauling " + thing + " to " 
                                            + selectedStairs + " in " + this.GetMapInfo(map));
                                        gotoStairs = JobMaker.MakeJob(ZLevelsDefOf.ZL_HaulThingToStairs, selectedStairs, thing);
                                    }
                                    else
                                    {
                                        Log.Message("Build tree (HaulThingToDest): " + pawn + " - Finding and using " + selectedStairs 
                                            + " in " + this.GetMapInfo(map));
                                        gotoStairs = JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToStairs, selectedStairs);
                                    }
                                    tempJobs.Add(gotoStairs);
                                }
                                else
                                {
                                    fail = true;
                                }
                            }
                            else
                            {
                                fail = true;
                            }
                        }
                    }
                }
            }
            return tempJobs;
        }
        public List<Job> GoToMap(Pawn pawn, Map dest, ref IntVec3 lastStairsPosition, ref bool fail)
        {
            List<Job> tempJobs = new List<Job>();
            Log.Message("Build tree: " + pawn + " - GoToMap from " + pawn.Map + " to " + this.GetMapInfo(dest));
            if (pawn.Map != dest)
            {
                if (this.GetZIndexFor(pawn.Map) > this.GetZIndexFor(dest))
                {
                    Log.Message("1 - Build tree (GoToMap): " + pawn + " - Going down");
                    foreach (var map in this.ZLevelsTracker[pawn.Map.Tile].ZLevels.Values.OrderByDescending(x => this.GetZIndexFor(x)))
                    {
                        if (this.GetZIndexFor(pawn.Map) >= this.GetZIndexFor(map) &&
                            this.GetZIndexFor(map) >= this.GetZIndexFor(dest))
                        {
                            if (map != dest)
                            {
                                var stairs = map.listerThings.AllThings.Where(x => x is Building_StairsDown && x.Spawned).ToList();
                                if (stairs?.Count() > 0)
                                {
                                    var selectedStairs = GenClosest.ClosestThing_Global(lastStairsPosition, stairs, 99999f);
                                    if (selectedStairs != null)
                                    {
                                        Log.Message("Build tree (GoToMap): " + pawn + " - Finding and using " + selectedStairs 
                                            + " in " + this.GetMapInfo(map));
                                        lastStairsPosition = selectedStairs.Position;
                                        Job goToStairs = JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToStairs, selectedStairs);
                                        tempJobs.Add(goToStairs);
                                    }
                                    else
                                    {
                                        fail = true;
                                    }
                                }
                                else
                                {
                                    fail = true;
                                }
                            }
                        }
                    }
                }
                else if (this.GetZIndexFor(pawn.Map) < this.GetZIndexFor(dest))
                {
                    Log.Message("1 - Build tree (GoToMap): " + pawn + " - Going up");
                    foreach (var map in this.ZLevelsTracker[pawn.Map.Tile].ZLevels.Values.OrderBy(x => this.GetZIndexFor(x)))
                    {
                        if (this.GetZIndexFor(pawn.Map) <= this.GetZIndexFor(map) &&
                            this.GetZIndexFor(map) <= this.GetZIndexFor(dest))
                        {
                            if (map != dest)
                            {
                                var stairs = map.listerThings.AllThings.Where(x => x is Building_StairsUp && x.Spawned).ToList();
                                if (stairs?.Count() > 0)
                                {
                                    var selectedStairs = GenClosest.ClosestThing_Global(lastStairsPosition, stairs, 99999f);
                                    if (selectedStairs != null)
                                    {
                                        Log.Message("Build tree (GoToMap): " + pawn + " - Finding and using " + selectedStairs 
                                            + " in " + this.GetMapInfo(map));
                                        lastStairsPosition = selectedStairs.Position;
                                        Job goToStairs = JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToStairs, selectedStairs);
                                        tempJobs.Add(goToStairs);
                                    }
                                    else
                                    {
                                        fail = true;
                                    }
                                }
                                else
                                {
                                    fail = true;
                                }
                            }
                        }
                    }
                }
            }
            return tempJobs;
        }
        public void BuildJobListFor(Pawn pawn, Map start, Map dest, Job jobToDo, Thing thingToHaul)
        {
            this.ResetJobs(pawn);
            List<Job> tempJobs = new List<Job>();
            bool fail = false;
            IntVec3 lastStairsPosition = pawn.Position;
            Log.Message("Starting build tree for " + pawn);
            if (jobToDo.def == JobDefOf.HaulToCell)
            {
                Log.Message("Job method 1");
                tempJobs.AddRange(this.GoToMap(pawn, jobToDo.targetA.Thing.Map, ref lastStairsPosition, ref fail));
                Log.Message(pawn + " haul " + jobToDo.targetA.Thing + " to " + dest);
                tempJobs.AddRange(this.HaulThingToDest(pawn, jobToDo.targetA.Thing, dest, ref lastStairsPosition, ref fail));
                tempJobs.Add(jobToDo);
            }
            else if (jobToDo.def == JobDefOf.Rescue || jobToDo.def == JobDefOf.Capture)
            {
                Log.Message("Job method 1.5: " + jobToDo.targetA.Thing);
                tempJobs.AddRange(this.GoToMap(pawn, jobToDo.targetA.Thing.Map, ref lastStairsPosition, ref fail));
                tempJobs.AddRange(this.HaulThingToDest(pawn, jobToDo.targetA.Thing, jobToDo.targetB.Thing.Map, ref lastStairsPosition, ref fail));
                tempJobs.Add(jobToDo);
            }
            else if (jobToDo?.targetQueueB?.Count > 0)
            {
                foreach (var t in jobToDo.targetQueueB)
                {
                    Log.Message("Job method 2");
                    tempJobs.AddRange(this.GoToMap(pawn, t.Thing.Map, ref lastStairsPosition, ref fail));
                    tempJobs.AddRange(this.HaulThingToDest(pawn, t.Thing, dest, ref lastStairsPosition, ref fail));
                    tempJobs.Add(jobToDo);
                }
            }
            else if (jobToDo.targetA != null && jobToDo.targetA.Thing?.Map != null)
            {
                Log.Message("Job method 3: " + jobToDo.targetA.Thing);
                tempJobs.AddRange(this.GoToMap(pawn, jobToDo.targetA.Thing.Map, ref lastStairsPosition, ref fail));
                tempJobs.Add(jobToDo);
            }
            else if (dest != null)
            {
                Log.Message("Job method 4");
                tempJobs.AddRange(this.GoToMap(pawn, dest, ref lastStairsPosition, ref fail));
                tempJobs.Add(jobToDo);
            }

            if (fail != true && tempJobs.Count > 0)
            {
                this.jobTracker[pawn].activeJobs = tempJobs;
                this.jobTracker[pawn].mainJob = jobToDo;
                this.TryTakeFirstJob(pawn);
            }
            else
            {
                Log.Message("FAIL!!!");
                Log.Message("fail: " + fail);
                Log.Message("tempJobs.Count: " + tempJobs.Count);
                //Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
            }
            Log.Message("Ending build tree for " + pawn);
        }

        public void ResetJobs(Pawn pawn)
        {
            if (this.jobTracker == null)
            {
                Log.Message("Creating new jobTracker");
                this.jobTracker = new Dictionary<Pawn, JobTracker>();
            }
            if (this.jobTracker.ContainsKey(pawn))
            {
                if (this.jobTracker[pawn].activeJobs?.Count() > 0)
                {
                    this.jobTracker[pawn].activeJobs.Clear();
                }
                if (this.jobTracker[pawn].activeJobs == null)
                {
                    this.jobTracker[pawn].activeJobs = new List<Job>();
                }
            }
            else
            {
                Log.Message("Resetting jobTracker for " + pawn);
                this.jobTracker[pawn] = new JobTracker();
                this.jobTracker[pawn].activeJobs = new List<Job>();
            }
        }

        public bool TryTakeFirstJob(Pawn pawn)
        {
            try
            {
                if (this.jobTracker.ContainsKey(pawn) && this.jobTracker[pawn].activeJobs?.Count() > 0)
                {
                    //try
                    //{
                    //    foreach (var d in this.jobTracker)
                    //    {
                    //        foreach (var t in d.Value.activeJobs)
                    //        {
                    //            Log.Message("Active jobs 1: " + d.Key + " - " + t);
                    //        }
                    //        foreach (var t in d.Key.jobs.jobQueue)
                    //        {
                    //            Log.Message("Active jobQueue 1: " + d.Key + " - " + t.job);
                    //        }
                    //        Log.Message("========================");
                    //    }
                    //}
                    //catch { }

                    Job job = this.jobTracker[pawn].activeJobs[0];
                    if (job?.def != null && job.TryMakePreToilReservations(pawn, false))
                    {
                        Log.Message(pawn + " taking job " + job + " in " + this.GetMapInfo(pawn.Map));
                        if (job == this.jobTracker[pawn].mainJob)
                        {
                            if (pawn?.carryTracker?.CarriedThing != null)
                            {
                                //Log.Message("this.jobTracker[pawn].mainJob: " + this.jobTracker[pawn].mainJob.targetB.Thing);
                                //Log.Message("pawn?.carryTracker?.CarriedThing: " + pawn?.carryTracker?.CarriedThing);
                                Thing newThing;
                                pawn.carryTracker.TryDropCarriedThing
                                (pawn.Position, ThingPlaceMode.Direct, out newThing);
                                //Log.Message("newThing: " + newThing);
                                //Log.Message("this.jobTracker[pawn].mainJob: " + 
                                //    this.jobTracker[pawn].mainJob.targetB.Thing);
                                //Log.Message("Same things: " 
                                //    + (newThing == this.jobTracker[pawn].mainJob.targetB.Thing).ToString());
                            }
                            //Log.Message("pawn.jobs.jobQueue.EnqueueFirst: " + job);
                            pawn.jobs.jobQueue.EnqueueLast(this.jobTracker[pawn].mainJob);
                        }
                        else
                        {
                            //Log.Message("pawn.jobs.jobQueue.EnqueueLast: " + job);
                            pawn.jobs.jobQueue.EnqueueLast(job);
                        }
                        this.jobTracker[pawn].activeJobs.RemoveAt(0);
                    }
                    else
                    {
                        Log.Message("Resetting jobs for " + pawn);
                        this.ResetJobs(pawn);
                        //Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
                    }
                    //try
                    //{
                    //    foreach (var d in this.jobTracker)
                    //    {
                    //        foreach (var t in d.Value.activeJobs)
                    //        {
                    //            Log.Message("Active jobs 2: " + d.Key + " - " + t);
                    //        }
                    //        foreach (var t in d.Key.jobs.jobQueue)
                    //        {
                    //            Log.Message("Active jobQueue 2: " + d.Key + " - " + t.job);
                    //        }
                    //        Log.Message("========================");
                    //    }
                    //}
                    //catch { }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Message("Fail in TryTakeFirstJob: " + ex);
            }
            return false;
        }

        public void ResetJobTrackerFor(Pawn pawn)
        {
            if (this.jobTracker.ContainsKey(pawn))
            {
                this.jobTracker[pawn].activeJobs.Clear();
            }
        }
        public void TeleportPawn(Pawn pawnToTeleport, IntVec3 cellToTeleport, Map mapToTeleport, bool firstTime = false, bool spawnStairsBelow = false, bool spawnStairsUpper = false)
        {
            //Log.Message("Trying to teleport to " + mapToTeleport);
            bool jump = false;
            bool draft = false;
            if (Find.Selector.SelectedObjects.Contains(pawnToTeleport))
            {
                jump = true;
            }
            if (pawnToTeleport.Drafted)
            {
                draft = true;
            }
            if (mapToTeleport.thingGrid.ThingsListAt(cellToTeleport).Any())
            {
                for (int i = mapToTeleport.thingGrid.ThingsListAt(cellToTeleport).Count - 1; i >= 0; i--)
                {
                    Thing thing = mapToTeleport.thingGrid.ThingsListAt(cellToTeleport)[i];
                    if (thing is Mineable)
                    {
                        if (thing.Spawned)
                        {
                            thing.DeSpawn(DestroyMode.WillReplace);
                        }
                    }
                }
            }
            var mapComp = mapToTeleport.GetComponent<MapComponentZLevel>();

            //if (mapComp.path != null && mapComp.path.Length > 0)
            //{
            //    mapComp.DoForcedGeneration(true);
            //}

            if (spawnStairsUpper)
            {
                if (this.GetZIndexFor(pawnToTeleport.Map) < this.GetZIndexFor(mapToTeleport))
                {
                    var stairs = this.GetLowerLevel(mapToTeleport.Tile, mapToTeleport)?.thingGrid?
                        .ThingsListAt(cellToTeleport)?.Where(x => x is Building_StairsUp)?.FirstOrDefault();
                    if (stairs.Stuff != null)
                    {
                        var thingToMake = ZLevelsDefOf.ZL_StairsDown;
                        if (cellToTeleport.GetThingList(mapToTeleport).Where(x => x.def == thingToMake).Count() == 0)
                        {
                            var newStairs = ThingMaker.MakeThing(thingToMake, stairs.Stuff);
                            newStairs.SetFaction(stairs.Faction);
                            GenPlace.TryPlaceThing(newStairs, cellToTeleport, mapToTeleport, ThingPlaceMode.Direct);
                        }
                    }
                }
            }
            if (spawnStairsBelow)
            {
                if (this.GetZIndexFor(pawnToTeleport.Map) > this.GetZIndexFor(mapToTeleport))
                {
                    var stairs = pawnToTeleport.Map.thingGrid.ThingsListAt(cellToTeleport)?
                        .Where(x => x is Building_StairsDown)?.FirstOrDefault();
                    Log.Message("Stairs: " + stairs);
                    if (stairs.Stuff != null)
                    {
                        var thingToMake = ZLevelsDefOf.ZL_StairsUp;
                        if (cellToTeleport.GetThingList(mapToTeleport).Where(x => x.def == thingToMake).Count() == 0)
                        {
                            var newStairs = ThingMaker.MakeThing(thingToMake, stairs.Stuff);
                            newStairs.SetFaction(stairs.Faction);
                            GenPlace.TryPlaceThing(newStairs, cellToTeleport, mapToTeleport, ThingPlaceMode.Direct);
                        }
                    }
                    else if (stairs.def.defName == ZLevelsDefOf.ZL_NaturalHole.defName)
                    {
                        foreach (var thing in pawnToTeleport.Map.listerThings.AllThings)
                        {
                            if (thing is Building_StairsDown naturalHole && naturalHole.def.defName == ZLevelsDefOf.ZL_NaturalHole.defName)
                            {
                                var infestatorsPlace = IntVec3.Invalid;
                                Thing pawn = null;
                                if (naturalHole?.infestationData?.infestators != null)
                                {
                                    Predicate<Thing> validator = delegate (Thing t)
                                    {
                                        return naturalHole.infestationData.infestators.Contains(((Pawn)t).kindDef);
                                    };
                                    pawn = GenClosest.ClosestThing_Global(naturalHole.Position,
                                        mapToTeleport.mapPawns.AllPawns, 99999f, validator);
                                }
                                if (pawn != null)
                                {
                                    infestatorsPlace = pawn.Position;
                                    var tunnel = mapToTeleport.pathFinder.FindPath
                                        (naturalHole.Position, pawn, TraverseParms.For
                                        (TraverseMode.PassAllDestroyableThings, Danger.Deadly),
                                        PathEndMode.OnCell);
                                    if (tunnel?.NodesReversed != null && tunnel.NodesReversed.Count > 0)
                                    {
                                        foreach (var tile in tunnel.NodesReversed)
                                        {
                                            var building = tile.GetFirstBuilding(mapToTeleport);
                                            if (building != null)
                                            {
                                                building.DeSpawn(DestroyMode.WillReplace);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //var jobs = pawnToTeleport.jobs.jobQueue.ToList().ListFullCopy();
            try
            {
                this.SaveArea(pawnToTeleport);
            }
            catch { }

            foreach (var animal in pawnToTeleport.relations.DirectRelations
                .Where(x => x.def == PawnRelationDefOf.Bond && x.otherPawn.Spawned
                && pawnToTeleport.Position.InHorDistOf(x.otherPawn.Position, 15)))
            {
                var stairs = pawnToTeleport.Position.GetThingList(pawnToTeleport.Map)
                    .Where(x => x is Building_StairsDown || x is Building_StairsUp).FirstOrDefault();
                if (stairs != null)
                {
                    Job goToStairs = JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToStairs, stairs);
                    animal.otherPawn.jobs.jobQueue.EnqueueFirst(goToStairs);
                }
            }

            JobManagerPatches.manualDespawn = true;
            pawnToTeleport.DeSpawn();
            JobManagerPatches.manualDespawn = false;
            GenPlace.TryPlaceThing(pawnToTeleport, cellToTeleport, mapToTeleport, ThingPlaceMode.Near);

            try
            {
                this.TryTakeFirstJob(pawnToTeleport);
            }
            catch { };
            try
            {
                this.LoadArea(pawnToTeleport);
            }
            catch { }

            Log.Message("Pawn: " + pawnToTeleport + " teleported to " + this.GetMapInfo(mapToTeleport));
            if (jump)
            {
                Current.Game.CurrentMap = mapToTeleport;
                CameraJumper.TryJumpAndSelect(pawnToTeleport);
            }
            if (draft)
            {
                pawnToTeleport.drafter.Drafted = true;
            }
            if (firstTime)
            {
                FloodFillerFog.DebugRefogMap(mapToTeleport);
                foreach (var cell in mapToTeleport.AllCells)
                {
                    FloodFillerFog.FloodUnfog(cell, mapToTeleport);
                }
            }

            FloodFillerFog.FloodUnfog(pawnToTeleport.Position, mapToTeleport);
            AccessTools.Method(typeof(FogGrid), "FloodUnfogAdjacent").Invoke(mapToTeleport.fogGrid, new object[]
            { pawnToTeleport.PositionHeld });
        }

        public override void StartedNewGame()
        {
            base.StartedNewGame();
        }

        public override void LoadedGame()
        {
            base.LoadedGame();
            foreach (var test in this.ZLevelsTracker)
            {
                Log.Message("Test: " + test);
                foreach (var d in test.Value.ZLevels)
                {
                    Log.Message("d: " + d);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look<Pawn, ActiveArea>(ref this.activeAreas, "activeAreas", LookMode.Reference,
                LookMode.Deep, ref this.PawnKeys, ref this.ActiveAreasValues);
            Scribe_Collections.Look<Pawn, JobTracker>(ref this.jobTracker, "jobTracker", LookMode.Reference,
                LookMode.Deep, ref this.PawnKeys, ref this.JobTrackerValues);
            Scribe_Collections.Look<int, ZLevelData>(ref this.ZLevelsTracker, "ZLevelsTracker",
                LookMode.Value, LookMode.Deep, ref this.Z_LevelsKeys, ref this.ZLevelsTrackerValues);
        }

        public Dictionary<Pawn, ActiveArea> activeAreas;
        public List<Pawn> PawnKeys = new List<Pawn>();
        public List<ActiveArea> ActiveAreasValues = new List<ActiveArea>();

        public Dictionary<Pawn, JobTracker> jobTracker;
        public List<JobTracker> JobTrackerValues = new List<JobTracker>();

        public List<int> Z_LevelsKeys = new List<int>();
        public Dictionary<int, ZLevelData> ZLevelsTracker = new Dictionary<int, ZLevelData>();
        public List<ZLevelData> ZLevelsTrackerValues = new List<ZLevelData>();
    }
}

