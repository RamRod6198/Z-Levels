using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
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

        //public override void GameComponentTick()
        //{
        //    base.GameComponentTick();
        //    if (Find.TickManager.TicksGame % 600 == 0)
        //    {
        //        foreach (var data in this.ZLevelsTracker)
        //        {
        //            foreach (var mapData in data.Value.ZLevels.Values)
        //            {
        //                foreach (var pawn in mapData.mapPawns.AllPawns)
        //                {
        //                    foreach (var mapData2 in data.Value.ZLevels.Values)
        //                    {
        //                        mapData2.dynamicDrawManager.RegisterDrawable(pawn);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        public void Select(object obj, bool playSound = true, bool forceDesignatorDeselect = true)
        {
            if (obj == null)
            {
                Log.Error("Cannot select null.", false);
                return;
            }
            Thing thing = obj as Thing;
            if (thing == null && !(obj is Zone))
            {
                Log.Error("Tried to select " + obj + " which is neither a Thing nor a Zone.", false);
                return;
            }
            if (thing != null && thing.Destroyed)
            {
                Log.Error("Cannot select destroyed thing.", false);
                return;
            }
            Pawn pawn = obj as Pawn;
            if (pawn != null && pawn.IsWorldPawn())
            {
                Log.Error("Cannot select world pawns.", false);
                return;
            }
            if (forceDesignatorDeselect)
            {
                Find.DesignatorManager.Deselect();
            }
            if (Find.Selector.SelectedZone != null && !(obj is Zone))
            {
                Find.Selector.ClearSelection();
            }
            if (obj is Zone && Find.Selector.SelectedZone == null)
            {
                Find.Selector.ClearSelection();
            }
            Map map = (thing != null) ? thing.Map : ((Zone)obj).Map;

            List<object> selected = Traverse.Create(Find.Selector).Field("selected").GetValue<List<object>>();

            for (int i = selected.Count - 1; i >= 0; i--)
            {
                Thing thing2 = selected[i] as Thing;
                if (((thing2 != null) ? thing2.Map : ((Zone)selected[i]).Map) != map)
                {
                    //Find.Selector.Deselect(selected[i]);
                }
            }
            if (selected.Count >= 200)
            {
                return;
            }
            if (!Find.Selector.IsSelected(obj))
            {
                selected.Add(obj);
                SelectionDrawer.Notify_Selected(obj);
            }
            Traverse.Create(Find.Selector).Field("selected").SetValue(selected);
        }
        public void CheckHotkeys()
        {
            bool keyDownEvent = ZLevelsDefOf.ZL_switchToUpperMap.KeyDownEvent;
            if (keyDownEvent)
            {
                Map mapToSwitch = this.GetUpperLevel(Find.CurrentMap.Tile, Find.CurrentMap);
                if (mapToSwitch != null)
                {
                    //var selectedObjects = Find.Selector.SelectedObjects.ListFullCopy();

                    var rememberedCamera = Current.Game.CurrentMap.rememberedCameraPos;
                    Current.Game.CurrentMap = mapToSwitch;
                    Find.CameraDriver.SetRootPosAndSize(rememberedCamera.rootPos, rememberedCamera.rootSize);

                    //foreach (var select in selectedObjects)
                    //{
                    //    this.Select(select);
                    //}
                }
                Event.current.Use();
            }
            bool keyDownEvent2 = ZLevelsDefOf.ZL_switchToLowerMap.KeyDownEvent;
            if (keyDownEvent2)
            {
                Map mapToSwitch = this.GetLowerLevel(Find.CurrentMap.Tile, Find.CurrentMap);
                if (mapToSwitch != null)
                {
                    //var selectedObjects = Find.Selector.SelectedObjects.ListFullCopy();

                    var rememberedCamera = Current.Game.CurrentMap.rememberedCameraPos;
                    Current.Game.CurrentMap = mapToSwitch;
                    Find.CameraDriver.SetRootPosAndSize(rememberedCamera.rootPos, rememberedCamera.rootSize);

                    //foreach (var select in selectedObjects)
                    //{
                    //    this.Select(select);
                    //}
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
                this.activeAreas[pawn] = new ActiveArea
                {
                    activeAreas = new Dictionary<Map, Area>()
                    {
                        {pawn.Map, pawn.playerSettings.AreaRestriction}
                    }
                };
            }

            //foreach (var test in this.activeAreas)
            //{
            //    ZLogger.Message("Pawn: " + test.Key);
            //    foreach (var d in test.Value.activeAreas)
            //    {
            //        ZLogger.Message("ActiveAreas: " + d);
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
            //ZLogger.Message("Index to get: " + this.GetZIndexFor(map));
            if (this.ZLevelsTracker != null && this.ZLevelsTracker.ContainsKey(tile)
                && this.ZLevelsTracker[tile].ZLevels.ContainsKey(this.GetZIndexFor(map) + 1))
            {
                //foreach (var d in this.ZLevelsTracker[tile].ZLevels)
                //{
                //    ZLogger.Message("Data: " + d.Key + " - " + d.Value);
                //}
                //ZLogger.Message("Getting: " + this.ZLevelsTracker[tile].ZLevels[this.GetZIndexFor(map) + 1]);

                //ZLogger.Message("Z_Levels contains key, getting map:" + Z_Levels[Z_LevelIndex + 1]);
                return this.ZLevelsTracker[tile].ZLevels[this.GetZIndexFor(map) + 1];
            }
            return null;
        }
        public Map GetLowerLevel(int tile, Map map)
        {
            //ZLogger.Message("Current map index: " + Z_LevelIndex);
            //ZLogger.Message("Trying to get index:" + (Z_LevelIndex - 1));
            if (this.ZLevelsTracker != null && this.ZLevelsTracker.ContainsKey(tile)
                && this.ZLevelsTracker[tile].ZLevels.ContainsKey(this.GetZIndexFor(map) - 1))
            {
                //ZLogger.Message("Z_Levels contains key, getting map:" + Z_Levels[Z_LevelIndex + 1]);
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

        public List<Map> GetAllMapsInClosestOrder(Map pawnMap)
        {
            List<Map> maps = new List<Map>();
            try
            {
                foreach (var map in this.ZLevelsTracker[pawnMap.Tile].ZLevels.Values.OrderBy(x =>
                (int)Mathf.Abs(this.GetZIndexFor(x) - this.GetZIndexFor(pawnMap))))
                {
                    maps.Add(map);
                }
                return maps;
            }
            catch
            {
                Log.Error("GetAllMaps returned null on " + pawnMap);
                return null;
            }
        }

        public int GetZIndexFor(Map map)
        {
            try
            {
                this.ZLevelsFixer(map.Tile);
                var comp = map.GetComponent<MapComponentZLevel>();
                return comp.Z_LevelIndex;
            }
            catch (Exception ex)
            {
                Log.Error("[Z-Levels] GetZIndexFor produced an error. " +
                    "That should not happen and will break things. " +
                    "Send a Hugslib log to the Z-Levels developers. Error message: " + ex, true);
                return -99999;
            }
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
                ZLogger.Message("Registering " + this.GetMapInfo(map) + " for index: " + index);
                return true;
            }
            else
            {
                this.ZLevelsTracker[map.Tile] = new ZLevelData
                {
                    ZLevels = new Dictionary<int, Map>
                    {
                        [index] = map
                    }
                };
                ZLogger.Message("Registering " + this.GetMapInfo(map) + " for index: " + index);
                return true;
            }
            return false;
        }

        //public override void GameComponentTick()
        //{
        //    base.GameComponentTick();
        //    if (Find.TickManager.TicksGame % 200 == 0)
        //    {
        //        foreach (var s in this.stairsDown)
        //        {
        //            foreach (var t in s.Value)
        //            {
        //            }
        //
        //        }
        //        foreach (var s in this.stairsUp)
        //        {
        //            foreach (var t in s.Value)
        //            {
        //            }
        //        }
        //        ZLogger.Message("========================");
        //    }
        //}

        public List<Job> HaulThingToDest(Pawn pawn, Thing thing, Map dest, ref IntVec3 lastStairsPosition, ref bool fail, int count = -1)
        {
            Log.Message(" - HaulThingToDest - List<Job> tempJobs = new List<Job>(); - 1", true);
            List<Job> tempJobs = new List<Job>();
            try
            {
                Log.Message(" - HaulThingToDest - if (this.GetZIndexFor(thing.Map) > this.GetZIndexFor(dest)) - 2", true);
                if (this.GetZIndexFor(thing.Map) > this.GetZIndexFor(dest))
                {
                    Log.Message(" - HaulThingToDest - foreach (var map in this.ZLevelsTracker[pawn.Map.Tile].ZLevels.Values.OrderByDescending(x => this.GetZIndexFor(x))) - 3", true);
                    foreach (var map in this.ZLevelsTracker[pawn.Map.Tile].ZLevels.Values.OrderByDescending(x => this.GetZIndexFor(x)))
                    {
                        Log.Message(" - HaulThingToDest - if (this.GetZIndexFor(thing.Map) >= this.GetZIndexFor(map) && - 4", true);
                        if (this.GetZIndexFor(thing.Map) >= this.GetZIndexFor(map) &&
                            this.GetZIndexFor(map) >= this.GetZIndexFor(dest))
                        {
                            Log.Message(" - HaulThingToDest - if (map != dest) - 5", true);
                            if (map != dest)
                            {
                                //var stairs = map.listerThings.AllThings.Where(x => x is Building_StairsDown && x.Spawned).ToList();
                                Log.Message(" - HaulThingToDest - var stairs = this.stairsDown[map]; - 7", true);
                                var stairs = this.stairsDown[map];
                                Log.Message(" - HaulThingToDest - if (stairs?.Count() > 0) - 8", true);
                                if (stairs?.Count() > 0)
                                {
                                    //var selectedStairs = GenClosest.ClosestThing_Global(lastStairsPosition, stairs, 99999f);
                                    Log.Message(" - HaulThingToDest - var selectedStairs = stairs.MinBy(x => IntVec3Utility.DistanceTo(pawn.Position, x.Position)); - 10", true);
                                    var selectedStairs = stairs.MinBy(x => IntVec3Utility.DistanceTo(pawn.Position, x.Position));
                                    Log.Message(" - HaulThingToDest - if (selectedStairs != null) - 11", true);
                                    if (selectedStairs != null)
                                    {
                                        Log.Message(" - HaulThingToDest - lastStairsPosition = selectedStairs.Position; - 12", true);
                                        lastStairsPosition = selectedStairs.Position;
                                        Log.Message(" - HaulThingToDest - Job gotoStairs = null; - 13", true);
                                        Job gotoStairs = null;
                                        Log.Message(" - HaulThingToDest - if (thing.Map == map) - 14", true);
                                        if (thing.Map == map)
                                        {
                                            ZLogger.Message("Build tree (HaulThingToDest): " + pawn + " - Hauling " + thing + " to "
                                                + selectedStairs + " in " + this.GetMapInfo(map));
                                            gotoStairs = JobMaker.MakeJob(ZLevelsDefOf.ZL_HaulThingToStairs,
                                                selectedStairs, thing);
                                            Log.Message(" - HaulThingToDest - if (count != -1) - 17", true);
                                            if (count != -1)
                                            {
                                                Log.Message(" - HaulThingToDest - gotoStairs.count = count; - 18", true);
                                                gotoStairs.count = count;
                                            }
                                        }
                                        else
                                        {
                                            ZLogger.Message("Build tree (HaulThingToDest): " + pawn + " - Finding and using " + selectedStairs
                                                + " in " + this.GetMapInfo(map));
                                            Log.Message(" - HaulThingToDest - gotoStairs = JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToStairs, selectedStairs); - 20", true);
                                            gotoStairs = JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToStairs, selectedStairs);
                                        }
                                        tempJobs.Add(gotoStairs);
                                    }
                                    else
                                    {
                                        Log.Message(" - HaulThingToDest - fail = true; - 22", true);
                                        fail = true;
                                    }
                                }
                                else
                                {
                                    Log.Message(" - HaulThingToDest - fail = true; - 23", true);
                                    fail = true;
                                }
                            }
                        }
                    }
                }
                else if (this.GetZIndexFor(thing.Map) < this.GetZIndexFor(dest))
                {
                    Log.Message(" - HaulThingToDest - foreach (var map in this.ZLevelsTracker[pawn.Map.Tile].ZLevels.Values.OrderBy(x => this.GetZIndexFor(x))) - 25", true);
                    foreach (var map in this.ZLevelsTracker[pawn.Map.Tile].ZLevels.Values.OrderBy(x => this.GetZIndexFor(x)))
                    {
                        Log.Message(" - HaulThingToDest - if (this.GetZIndexFor(thing.Map) <= this.GetZIndexFor(map) && - 26", true);
                        if (this.GetZIndexFor(thing.Map) <= this.GetZIndexFor(map) &&
                            this.GetZIndexFor(map) <= this.GetZIndexFor(dest))
                        {
                            Log.Message(" - HaulThingToDest - if (map != dest) - 27", true);
                            if (map != dest)
                            {
                                //var stairs = map.listerThings.AllThings.Where(x => x is Building_StairsUp && x.Spawned).ToList();
                                Log.Message(" - HaulThingToDest - var stairs = this.stairsUp[map]; - 29", true);
                                var stairs = this.stairsUp[map];
                                Log.Message(" - HaulThingToDest - if (stairs?.Count() > 0) - 30", true);
                                if (stairs?.Count() > 0)
                                {
                                    //var selectedStairs = GenClosest.ClosestThing_Global(lastStairsPosition, stairs, 99999f);
                                    Log.Message(" - HaulThingToDest - var selectedStairs = stairs.MinBy(x => IntVec3Utility.DistanceTo(pawn.Position, x.Position)); - 32", true);
                                    var selectedStairs = stairs.MinBy(x => IntVec3Utility.DistanceTo(pawn.Position, x.Position));
                                    Log.Message(" - HaulThingToDest - if (selectedStairs != null) - 33", true);
                                    if (selectedStairs != null)
                                    {
                                        Log.Message(" - HaulThingToDest - lastStairsPosition = selectedStairs.Position; - 34", true);
                                        lastStairsPosition = selectedStairs.Position;
                                        Log.Message(" - HaulThingToDest - Job gotoStairs = null; - 35", true);
                                        Job gotoStairs = null;
                                        Log.Message(" - HaulThingToDest - if (thing.Map == map) - 36", true);
                                        if (thing.Map == map)
                                        {
                                            ZLogger.Message("Build tree (HaulThingToDest): " + pawn + " - Hauling " + thing + " to "
                                                + selectedStairs + " in " + this.GetMapInfo(map));
                                            gotoStairs = JobMaker.MakeJob(ZLevelsDefOf.ZL_HaulThingToStairs,
                                                selectedStairs, thing);
                                            Log.Message(" - HaulThingToDest - if (count != -1) - 39", true);
                                            if (count != -1)
                                            {
                                                Log.Message(" - HaulThingToDest - gotoStairs.count = count; - 40", true);
                                                gotoStairs.count = count;
                                            }
                                        }
                                        else
                                        {
                                            ZLogger.Message("Build tree (HaulThingToDest): " + pawn + " - Finding and using " + selectedStairs
                                                + " in " + this.GetMapInfo(map));
                                            Log.Message(" - HaulThingToDest - gotoStairs = JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToStairs, selectedStairs); - 42", true);
                                            gotoStairs = JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToStairs, selectedStairs);
                                        }
                                        tempJobs.Add(gotoStairs);
                                    }
                                    else
                                    {
                                        Log.Message(" - HaulThingToDest - fail = true; - 44", true);
                                        fail = true;
                                    }
                                }
                                else
                                {
                                    Log.Message(" - HaulThingToDest - fail = true; - 45", true);
                                    fail = true;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Z-Levels: Job builder in HaulThingToDest produced an error. This error will lead to glitches to the game." +
                    " Report this to the Z-Levels devs and attach the save game with an error if you can.");
                Log.Message(" - HaulThingToDest - }; - 47", true);
            };
            Log.Message(" - HaulThingToDest - return tempJobs; - 48", true);
            return tempJobs;
        }

        public Job HaulThingToPlace(Pawn pawn, Thing thing, IntVec3 positionToHaul, int count = -1)
        {
            Job job = JobMaker.MakeJob(ZLevelsDefOf.ZL_HaulToCell, thing, positionToHaul);
            if (count == -1)
            {
                job.count = Mathf.Min(thing.stackCount, (int)(pawn.GetStatValue(StatDefOf.CarryingCapacity, true)
                    / thing.def.VolumePerUnit));
                if (job.count < 0)
                {
                    job.count = thing.stackCount;
                }
                job.count = thing.stackCount;
            }
            else
            {
                job.count = count;
            }
            return job;
        }
        public List<Job> GoToMap(Pawn pawn, Map dest, ref IntVec3 lastStairsPosition, ref bool fail)
        {
            List<Job> tempJobs = new List<Job>();
            if (pawn.Map != dest)
            {
                if (this.GetZIndexFor(pawn.Map) > this.GetZIndexFor(dest))
                {
                    foreach (var map in this.ZLevelsTracker[pawn.Map.Tile].ZLevels.Values.OrderByDescending(x => this.GetZIndexFor(x)))
                    {
                        if (this.GetZIndexFor(pawn.Map) >= this.GetZIndexFor(map) &&
                            this.GetZIndexFor(map) >= this.GetZIndexFor(dest))
                        {
                            if (map != dest)
                            {
                                //var stairs = map.listerThings.AllThings.Where(x => x is Building_StairsDown && x.Spawned).ToList();
                                var stairs = this.stairsDown[map];
                                if (stairs?.Count() > 0)
                                {
                                    //var selectedStairs = GenClosest.ClosestThing_Global(lastStairsPosition, stairs, 99999f);
                                    var selectedStairs = stairs.MinBy(x => IntVec3Utility.DistanceTo(pawn.Position, x.Position));
                                    if (selectedStairs != null)
                                    {
                                        ZLogger.Message("Build tree (GoToMap): " + pawn + " - Finding and using " + selectedStairs
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
                    foreach (var map in this.ZLevelsTracker[pawn.Map.Tile].ZLevels.Values.OrderBy(x => this.GetZIndexFor(x)))
                    {
                        if (this.GetZIndexFor(pawn.Map) <= this.GetZIndexFor(map) &&
                            this.GetZIndexFor(map) <= this.GetZIndexFor(dest))
                        {
                            if (map != dest)
                            {
                                //var stairs = map.listerThings.AllThings.Where(x => x is Building_StairsUp && x.Spawned).ToList();
                                var stairs = this.stairsUp[map];
                                if (stairs?.Count() > 0)
                                {
                                    //var selectedStairs = GenClosest.ClosestThing_Global(lastStairsPosition, stairs, 99999f);
                                    var selectedStairs = stairs.MinBy(x => IntVec3Utility.DistanceTo(pawn.Position, x.Position));
                                    if (selectedStairs != null)
                                    {
                                        ZLogger.Message("Build tree (GoToMap): " + pawn + " - Finding and using " + selectedStairs
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

            if (jobToDo.def == JobDefOf.HaulToCell)
            {
                ZLogger.Message("Job method 1");
                tempJobs.Add(JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToThingMap, null, jobToDo.targetA.Thing));
                ZLogger.Message(pawn + " haul " + jobToDo.targetA.Thing + " to " + dest);
                tempJobs.AddRange(this.HaulThingToDest(pawn, jobToDo.targetA.Thing, dest, ref lastStairsPosition, ref fail));
                tempJobs.Add(jobToDo);
            }
            else if (jobToDo.def == JobDefOf.HaulToContainer)
            {
                tempJobs.Add(JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToThingMap, null, jobToDo.targetA.Thing));
                ZLogger.Message(pawn + " haul " + jobToDo.targetA.Thing + " to " + dest);
                tempJobs.AddRange(this.HaulThingToDest(pawn, jobToDo.targetA.Thing, jobToDo.targetB.Thing.Map, ref lastStairsPosition, ref fail));
                tempJobs.Add(jobToDo);
            }
            else if (jobToDo.def == JobDefOf.Rescue || jobToDo.def == JobDefOf.Capture)
            {
                ZLogger.Message("Job method 1.5: " + jobToDo.targetA.Thing);
                tempJobs.Add(JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToThingMap, null, jobToDo.targetA.Thing));
                tempJobs.AddRange(this.HaulThingToDest(pawn, jobToDo.targetA.Thing, jobToDo.targetB.Thing.Map, ref lastStairsPosition, ref fail));
                tempJobs.Add(jobToDo);
            }
            else if (jobToDo.def == JobDefOf.Refuel)
            {
                ZLogger.Message("Job method 1.7: " + jobToDo.targetB.Thing);
                tempJobs.Add(JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToThingMap, null, jobToDo.targetB.Thing));
                tempJobs.AddRange(this.HaulThingToDest(pawn, jobToDo.targetB.Thing, jobToDo.targetA.Thing.Map, ref lastStairsPosition, ref fail));
                tempJobs.Add(JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToThingMap, null, jobToDo.targetA.Thing));
                tempJobs.Add(jobToDo);
            }
            else if (jobToDo?.targetQueueB?.Count > 0)
            {
                ZLogger.Message("Job method 2");
                if (jobToDo.targetQueueB?.Count == jobToDo.countQueue?.Count)
                {
                    for (int i = 0; i < jobToDo.targetQueueB.Count; i++)
                    {
                        var t = jobToDo.targetQueueB[i];
                        tempJobs.Add(JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToThingMap, null, t.Thing));
                        tempJobs.AddRange(this.HaulThingToDest(pawn, t.Thing, dest, ref lastStairsPosition, ref fail, jobToDo.countQueue[i]));
                        if (pawn.Map != jobToDo.targetA.Thing.Map || pawn.Map != t.Thing.Map)
                        {
                            tempJobs.Add(this.HaulThingToPlace(pawn, t.Thing, jobToDo.targetA.Cell, jobToDo.countQueue[i]));
                        }
                    }
                }
                else
                {
                    foreach (var t in jobToDo.targetQueueB)
                    {
                        tempJobs.Add(JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToThingMap, null, t.Thing));
                        tempJobs.AddRange(this.HaulThingToDest(pawn, t.Thing, dest, ref lastStairsPosition, ref fail));
                        if (pawn.Map != jobToDo.targetA.Thing.Map || pawn.Map != t.Thing.Map)
                        {
                            tempJobs.Add(this.HaulThingToPlace(pawn, t.Thing, jobToDo.targetA.Cell));
                        }
                    }
                }

                foreach (var t in jobToDo.countQueue)
                {
                    ZLogger.Message("Count: " + t);
                }
                tempJobs.Add(jobToDo);
            }
            else if (jobToDo.targetA != null && jobToDo.targetA.Thing?.Map != null)
            {
                ZLogger.Message("Job method 3: " + jobToDo.targetA.Thing);
                tempJobs.Add(JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToThingMap, null, jobToDo.targetA.Thing));
                tempJobs.Add(jobToDo);
            }
            else if (dest != null)
            {
                ZLogger.Message("Job method 4");
                tempJobs.AddRange(this.GoToMap(pawn, dest, ref lastStairsPosition, ref fail));
                tempJobs.Add(jobToDo);
            }

            if (fail != true && tempJobs.Count > 0)
            {
                this.jobTracker[pawn].activeJobs = tempJobs;
                this.jobTracker[pawn].mainJob = jobToDo;
            }
            else
            {
                ZLogger.Message("FAIL!!!");
                ZLogger.Message("fail: " + fail);
                ZLogger.Message("tempJobs.Count: " + tempJobs.Count);
            }

        }

        public void ResetJobs(Pawn pawn)
        {
            if (this.jobTracker == null)
            {
                ZLogger.Message("Creating new jobTracker");
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
                ZLogger.Message("Resetting jobTracker for " + pawn);
                this.jobTracker[pawn] = new JobTracker
                {
                    activeJobs = new List<Job>()
                };
            }
        }

        public bool TryTakeFirstJob(Pawn pawn, bool forced = false)
        {
            Job job = null;
            ZLogger.Message(pawn + " - START TryTakeFirstJob");
            try
            {
                Log.Message(" - TryTakeFirstJob - if (this.jobTracker.ContainsKey(pawn) && this.jobTracker[pawn].activeJobs?.Count() > 0) - 2", true);
                if (this.jobTracker.ContainsKey(pawn) && this.jobTracker[pawn].activeJobs?.Count() > 0)
                {
                    Log.Message("1 this.jobTracker[pawn].activeJobs: " + this.jobTracker[pawn].activeJobs.Count);
                    Log.Message(" - TryTakeFirstJob - Job job = this.jobTracker[pawn].activeJobs[0]; - 4", true);
                    job = this.jobTracker[pawn].activeJobs[0];
                    Log.Message(" - TryTakeFirstJob - if (job?.def != null) - 5", true);
                    if (job?.def != null)
                    {
                        Log.Message("2 this.jobTracker[pawn].activeJobs: " + this.jobTracker[pawn].activeJobs.Count);

                        ZLogger.Message(pawn + " CarriedThing " + pawn?.carryTracker?.CarriedThing);
                        Log.Message(" - TryTakeFirstJob - if (pawn?.carryTracker?.CarriedThing != null) - 8", true);
                        if (pawn?.carryTracker?.CarriedThing != null)
                        {
                            try
                            {
                                ZLogger.Message("--------------------------");
                                for (int i = this.jobTracker[pawn].mainJob.targetQueueB.Count - 1; i >= 0; i--)
                                {
                                    Log.Message(" - TryTakeFirstJob - var target = this.jobTracker[pawn].mainJob.targetQueueB[i]; - 10", true);
                                    var target = this.jobTracker[pawn].mainJob.targetQueueB[i];

                                    ZLogger.Message("0 BEFORE job.targetQueueB: " + target.Thing);
                                    ZLogger.Message("0 BEFORE job.targetQueueB.Map: " + target.Thing.Map);
                                    ZLogger.Message("0 BEFORE job.targetQueueB.stackCount: " + target.Thing.stackCount);
                                    ZLogger.Message("0 BEFORE job.targetQueueB.countQueue: " + this.jobTracker[pawn].mainJob.countQueue[i]);

                                }
                            }
                            catch { }

                            try
                            {
                                ZLogger.Message("--------------------------");
                                for (int i = this.jobTracker[pawn].mainJob.targetQueueB.Count - 1; i >= 0; i--)
                                {
                                    Log.Message(" - TryTakeFirstJob - var target = this.jobTracker[pawn].mainJob.targetQueueB[i]; - 16", true);
                                    var target = this.jobTracker[pawn].mainJob.targetQueueB[i];

                                    ZLogger.Message("15 job.targetQueueB: " + target.Thing);
                                    ZLogger.Message("15 job.targetQueueB.Map: " + target.Thing.Map);
                                    ZLogger.Message("15 job.targetQueueB.stackCount: " + target.Thing.stackCount);
                                    ZLogger.Message("15 job.targetQueueB.countQueue: " + this.jobTracker[pawn].mainJob.countQueue[i]);
                                }
                            }
                            catch { }

                            Thing savedThing = pawn.carryTracker.CarriedThing;
                            ZLogger.Message(pawn + " trying to drop " + pawn?.carryTracker?.CarriedThing + " for " + job);
                            Log.Message(" - TryTakeFirstJob - Thing newThing; - 23", true);
                            Thing newThing;
                            Log.Message(" - TryTakeFirstJob - pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Direct, out newThing); - 24", true);
                            pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Direct, out newThing);
                            //ZLogger.Pause("Try drop 1");
                            Log.Message(" - TryTakeFirstJob - if (job.def == ZLevelsDefOf.ZL_HaulToCell) - 26", true);
                            if (job.def == ZLevelsDefOf.ZL_HaulToCell)
                            {
                                Log.Message(" - TryTakeFirstJob - if (job.targetA.Thing != newThing) - 27", true);
                                if (job.targetA.Thing != newThing)
                                {
                                    Log.Message(" - TryTakeFirstJob - job.targetA = new LocalTargetInfo(newThing); - 28", true);
                                    job.targetA = new LocalTargetInfo(newThing);
                                }
                            }

                            ZLogger.Message(pawn + " dropping " + newThing + " for " + job);

                            ZLogger.Message("0 Analyzing: " + pawn);
                            ZLogger.Message("0 Saved thing: " + savedThing);
                            ZLogger.Message("0 Main job: " + job);
                            ZLogger.Message("0 job.targetA.Thing: " + job.targetA.Thing + " - " + job.targetA.Thing?.Map);
                            ZLogger.Message("0 job.targetB.Thing: " + job.targetB.Thing + " - " + job.targetB.Thing?.Map);
                            try
                            {
                                Log.Message(" - TryTakeFirstJob - if (job.targetA.Thing == savedThing && savedThing != newThing) - 35", true);
                                if (job.targetA.Thing == savedThing && savedThing != newThing)
                                {
                                    ZLogger.Message(newThing + " 0 job.targetA is not same: " + job.targetA.Thing);
                                    Log.Message(" - TryTakeFirstJob - job.targetA = new LocalTargetInfo(newThing); - 37", true);
                                    job.targetA = new LocalTargetInfo(newThing);
                                }

                                Log.Message(" - TryTakeFirstJob - if (job.targetB.Thing == savedThing && savedThing != newThing) - 38", true);
                                if (job.targetB.Thing == savedThing && savedThing != newThing)
                                {
                                    ZLogger.Message(newThing + " 0 job.targetB is not same: " + job.targetB.Thing);
                                    Log.Message(" - TryTakeFirstJob - job.targetB = new LocalTargetInfo(newThing); - 40", true);
                                    job.targetB = new LocalTargetInfo(newThing);
                                }
                            }
                            catch { };
                            try
                            {
                                for (int i = job.targetQueueA.Count - 1; i >= 0; i--)
                                {
                                    Log.Message(" - TryTakeFirstJob - var target = job.targetQueueA[i]; - 42", true);
                                    var target = job.targetQueueA[i];
                                    ZLogger.Message("0 job.targetQueueA: " + target.Thing + " - " + target.Thing.Map);
                                    Log.Message(" - TryTakeFirstJob - if (target.Thing == savedThing && savedThing != newThing) - 44", true);
                                    if (target.Thing == savedThing && savedThing != newThing)
                                    {
                                        ZLogger.Message(newThing + " 0 job.targetQueueA is not same: " + target.Thing);
                                        Log.Message(" - TryTakeFirstJob - job.targetQueueA[i] = new LocalTargetInfo(newThing); - 46", true);
                                        job.targetQueueA[i] = new LocalTargetInfo(newThing);
                                    }
                                }
                            }
                            catch { }
                            try
                            {
                                ZLogger.Message("--------------------------");
                                for (int i = job.targetQueueB.Count - 1; i >= 0; i--)
                                {
                                    Log.Message(" - TryTakeFirstJob - var target = job.targetQueueB[i]; - 48", true);
                                    var target = job.targetQueueB[i];

                                    ZLogger.Message("17 job.targetQueueB: " + target.Thing);
                                    ZLogger.Message("17 job.targetQueueB.Map: " + target.Thing.Map);
                                    ZLogger.Message("17 job.targetQueueB.stackCount: " + target.Thing.stackCount);
                                    ZLogger.Message("17 job.targetQueueB.countQueue: " + job.countQueue);

                                    Log.Message(" - TryTakeFirstJob - if (target.Thing == savedThing && savedThing != newThing) - 53", true);
                                    if (target.Thing == savedThing && savedThing != newThing)
                                    {
                                        ZLogger.Message(newThing + " 0 job.targetQueueB is not same: " + target.Thing);
                                        Log.Message(" - TryTakeFirstJob - job.targetQueueB[i] = new LocalTargetInfo(newThing); - 55", true);
                                        job.targetQueueB[i] = new LocalTargetInfo(newThing);
                                        Log.Message(" - TryTakeFirstJob - job.countQueue[i] = newThing.stackCount; - 56", true);
                                        job.countQueue[i] = newThing.stackCount;

                                    }
                                }
                            }
                            catch { }
                            Log.Message(" - TryTakeFirstJob - if (job != this.jobTracker[pawn].mainJob) - 57", true);
                            if (job != this.jobTracker[pawn].mainJob)
                            {
                                try
                                {
                                    Log.Message(" - TryTakeFirstJob - if (this.jobTracker[pawn].mainJob.targetA.Thing == savedThing && savedThing != newThing) - 58", true);
                                    if (this.jobTracker[pawn].mainJob.targetA.Thing == savedThing && savedThing != newThing)
                                    {
                                        ZLogger.Message(newThing + " 0 this.jobTracker[pawn].mainJob.targetA is not same: " + this.jobTracker[pawn].mainJob.targetA.Thing);
                                        Log.Message(" - TryTakeFirstJob - this.jobTracker[pawn].mainJob.targetA = new LocalTargetInfo(newThing); - 60", true);
                                        this.jobTracker[pawn].mainJob.targetA = new LocalTargetInfo(newThing);
                                    }

                                    Log.Message(" - TryTakeFirstJob - if (this.jobTracker[pawn].mainJob.targetB.Thing == savedThing && savedThing != newThing) - 61", true);
                                    if (this.jobTracker[pawn].mainJob.targetB.Thing == savedThing && savedThing != newThing)
                                    {
                                        ZLogger.Message(newThing + " 0 this.jobTracker[pawn].mainJob.targetB is not same: " + this.jobTracker[pawn].mainJob.targetB.Thing);
                                        Log.Message(" - TryTakeFirstJob - this.jobTracker[pawn].mainJob.targetB = new LocalTargetInfo(newThing); - 63", true);
                                        this.jobTracker[pawn].mainJob.targetB = new LocalTargetInfo(newThing);
                                    }
                                }
                                catch { }
                                try
                                {
                                    for (int i = this.jobTracker[pawn].mainJob.targetQueueA.Count - 1; i >= 0; i--)
                                    {
                                        Log.Message(" - TryTakeFirstJob - var target = this.jobTracker[pawn].mainJob.targetQueueA[i]; - 64", true);
                                        var target = this.jobTracker[pawn].mainJob.targetQueueA[i];
                                        ZLogger.Message("0 this.jobTracker[pawn].mainJob.targetQueueA: " + target.Thing + " - " + target.Thing.Map);
                                        Log.Message(" - TryTakeFirstJob - if (target.Thing == savedThing && savedThing != newThing) - 66", true);
                                        if (target.Thing == savedThing && savedThing != newThing)
                                        {
                                            ZLogger.Message(newThing + " 0 this.jobTracker[pawn].mainJob.targetQueueA is not same: " + target.Thing);
                                            Log.Message(" - TryTakeFirstJob - this.jobTracker[pawn].mainJob.targetQueueA[i] = new LocalTargetInfo(newThing); - 68", true);
                                            this.jobTracker[pawn].mainJob.targetQueueA[i] = new LocalTargetInfo(newThing);
                                        }
                                    }
                                }
                                catch { }
                                try
                                {
                                    for (int i = this.jobTracker[pawn].mainJob.targetQueueB.Count - 1; i >= 0; i--)
                                    {
                                        Log.Message(" - TryTakeFirstJob - var target = this.jobTracker[pawn].mainJob.targetQueueB[i]; - 69", true);
                                        var target = this.jobTracker[pawn].mainJob.targetQueueB[i];
                                        ZLogger.Message("0 this.jobTracker[pawn].mainJob.targetQueueB: " + target.Thing);
                                        Log.Message(" - TryTakeFirstJob - if (target.Thing == savedThing && savedThing != newThing) - 71", true);
                                        if (target.Thing == savedThing && savedThing != newThing)
                                        {
                                            ZLogger.Message(newThing + " 0 this.jobTracker[pawn].mainJob.targetQueueB is not same: " + target.Thing);
                                            Log.Message(" - TryTakeFirstJob - this.jobTracker[pawn].mainJob.targetQueueB[i] = new LocalTargetInfo(newThing); - 73", true);
                                            this.jobTracker[pawn].mainJob.targetQueueB[i] = new LocalTargetInfo(newThing);
                                            Log.Message(" - TryTakeFirstJob - this.jobTracker[pawn].mainJob.countQueue[i] = newThing.stackCount; - 74", true);
                                            this.jobTracker[pawn].mainJob.countQueue[i] = newThing.stackCount;
                                        }
                                    }
                                }
                                catch { }
                            }
                            var mainJob = this.jobTracker[pawn].mainJob;
                            ZLogger.Message("Main job stats: ");
                            ZLogger.Message("Job: " + mainJob);
                            ZLogger.Message("Job.targetQueueB: " + mainJob.targetQueueB);
                            ZLogger.Message("Job.countQueue: " + mainJob.countQueue);
                            Log.Message("3 this.jobTracker[pawn].activeJobs: " + this.jobTracker[pawn].activeJobs.Count);

                            Log.Message(" - TryTakeFirstJob - if (mainJob.countQueue != null && mainJob.countQueue?.Count == mainJob.targetQueueB?.Count) - 81", true);
                            if (mainJob.countQueue != null && mainJob.countQueue?.Count == mainJob.targetQueueB?.Count)
                            {
                                Log.Message(" - TryTakeFirstJob - bool repeat = true; - 82", true);
                                bool repeat = true;
                                Log.Message(" - TryTakeFirstJob - int num = 0; - 83", true);
                                int num = 0;
                                try
                                {
                                    ZLogger.Message("--------------------------");
                                    for (int i = this.jobTracker[pawn].mainJob.targetQueueB.Count - 1; i >= 0; i--)
                                    {
                                        Log.Message(" - TryTakeFirstJob - var target = this.jobTracker[pawn].mainJob.targetQueueB[i]; - 85", true);
                                        var target = this.jobTracker[pawn].mainJob.targetQueueB[i];

                                        ZLogger.Message("BEFORE job.targetQueueB: " + target.Thing);
                                        ZLogger.Message("BEFORE job.targetQueueB.Map: " + target.Thing.Map);
                                        ZLogger.Message("BEFORE job.targetQueueB.stackCount: " + target.Thing.stackCount);
                                        ZLogger.Message("BEFORE job.targetQueueB.countQueue: " + job.countQueue);

                                    }
                                }
                                catch { }

                                while (repeat && num < 100)
                                {
                                    Log.Message(" - TryTakeFirstJob - Dictionary<Thing, HashSet<int>> duplicates = new Dictionary<Thing, HashSet<int>>(); - 90", true);
                                    Dictionary<Thing, HashSet<int>> duplicates = new Dictionary<Thing, HashSet<int>>();
                                    for (int i = mainJob.targetQueueB.Count - 1; i >= 0; i--)
                                    {
                                        for (int j = mainJob.targetQueueB.Count - 1; j >= 0; j--)
                                        {
                                            Log.Message(" - TryTakeFirstJob - if (i != j && mainJob.targetQueueB[i] == mainJob.targetQueueB[j]) - 91", true);
                                            if (i != j && mainJob.targetQueueB[i] == mainJob.targetQueueB[j])
                                            {
                                                Log.Message(" - TryTakeFirstJob - if (!duplicates.ContainsKey(mainJob.targetQueueB[i].Thing)) - 92", true);
                                                if (!duplicates.ContainsKey(mainJob.targetQueueB[i].Thing))
                                                {
                                                    duplicates[mainJob.targetQueueB[i].Thing] = new HashSet<int>
                                                        {
                                                            i
                                                        };
                                                    ZLogger.Message("Adding " + mainJob.targetQueueB[i].Thing + " to duplicates with an index: " + i);
                                                }
                                                else
                                                {
                                                    Log.Message(" - TryTakeFirstJob - duplicates[mainJob.targetQueueB[i].Thing].Add(i); - 95", true);
                                                    duplicates[mainJob.targetQueueB[i].Thing].Add(i);
                                                    ZLogger.Message("Adding " + mainJob.targetQueueB[i].Thing + " to duplicates with an index: " + i);
                                                }
                                            }
                                        }
                                    }

                                    Log.Message(" - TryTakeFirstJob - if (duplicates.Count > 0) - 97", true);
                                    if (duplicates.Count > 0)
                                    {
                                        Log.Message(" - TryTakeFirstJob - foreach (var i in duplicates.First().Value.OrderBy(x => x).Skip(1).OrderByDescending(x => x)) - 98", true);
                                        foreach (var i in duplicates.First().Value.OrderBy(x => x).Skip(1).OrderByDescending(x => x))
                                        {
                                            ZLogger.Message("Removing item at " + i + " - " + mainJob.targetQueueB);
                                            Log.Message(" - TryTakeFirstJob - mainJob.targetQueueB.RemoveAt(i); - 100", true);
                                            mainJob.targetQueueB.RemoveAt(i);
                                            Log.Message(" - TryTakeFirstJob - mainJob.countQueue.RemoveAt(i); - 101", true);
                                            mainJob.countQueue.RemoveAt(i);
                                        }
                                    }
                                    Log.Message(" - TryTakeFirstJob - if (duplicates.Count > 1) - 102", true);
                                    if (duplicates.Count > 1)
                                    {
                                        Log.Message(" - TryTakeFirstJob - repeat = true; - 103", true);
                                        repeat = true;
                                    }
                                    else
                                    {
                                        Log.Message(" - TryTakeFirstJob - repeat = false; - 104", true);
                                        repeat = false;
                                    }
                                    num++;
                                    ZLogger.Message(num + " - checking duplicates");
                                }

                                try
                                {
                                    ZLogger.Message("--------------------------");
                                    for (int i = this.jobTracker[pawn].mainJob.targetQueueB.Count - 1; i >= 0; i--)
                                    {
                                        Log.Message(" - TryTakeFirstJob - var target = this.jobTracker[pawn].mainJob.targetQueueB[i]; - 108", true);
                                        var target = this.jobTracker[pawn].mainJob.targetQueueB[i];

                                        ZLogger.Message("AFTER job.targetQueueB: " + target.Thing);
                                        ZLogger.Message("AFTER job.targetQueueB.Map: " + target.Thing.Map);
                                        ZLogger.Message("AFTER job.targetQueueB.stackCount: " + target.Thing.stackCount);
                                        ZLogger.Message("AFTER job.targetQueueB.countQueue: " + job.countQueue);

                                    }
                                }
                                catch { }

                            }

                            try
                            {
                                ZLogger.Message("--------------------------");
                                for (int i = this.jobTracker[pawn].mainJob.targetQueueB.Count - 1; i >= 0; i--)
                                {
                                    Log.Message(" - TryTakeFirstJob - var target = this.jobTracker[pawn].mainJob.targetQueueB[i]; - 114", true);
                                    var target = this.jobTracker[pawn].mainJob.targetQueueB[i];

                                    ZLogger.Message("25 job.targetQueueB: " + target.Thing);
                                    ZLogger.Message("25 job.targetQueueB.Map: " + target.Thing.Map);
                                    ZLogger.Message("25 job.targetQueueB.stackCount: " + target.Thing.stackCount);
                                    ZLogger.Message("25 job.targetQueueB.countQueue: " + this.jobTracker[pawn].mainJob.countQueue[i]);

                                }
                            }
                            catch { }
                        }
                        Log.Message("4 this.jobTracker[pawn].activeJobs: " + this.jobTracker[pawn].activeJobs.Count);

                        ZLogger.Message(pawn + " TryMakePreToilReservations job " + job + " in " + this.GetMapInfo(pawn.Map));
                        Log.Message(" - TryTakeFirstJob - if (job.TryMakePreToilReservations(pawn, false)) - 121", true);
                        if (job.TryMakePreToilReservations(pawn, false))
                        {
                            ZLogger.Message(pawn + " taking job " + job + " in " + this.GetMapInfo(pawn.Map));
                            try
                            {
                                ZLogger.Message("--------------------------");
                                for (int i = this.jobTracker[pawn].mainJob.targetQueueB.Count - 1; i >= 0; i--)
                                {
                                    Log.Message(" - TryTakeFirstJob - var target = this.jobTracker[pawn].mainJob.targetQueueB[i]; - 124", true);
                                    var target = this.jobTracker[pawn].mainJob.targetQueueB[i];
                                    ZLogger.Message("30 job.targetQueueB: " + target.Thing);
                                    ZLogger.Message("30 job.targetQueueB.Map: " + target.Thing.Map);
                                    ZLogger.Message("30 job.targetQueueB.stackCount: " + target.Thing.stackCount);
                                    ZLogger.Message("30 job.targetQueueB.countQueue: " + this.jobTracker[pawn].mainJob.countQueue[i]);

                                }
                            }
                            catch { }
                            Log.Message(" - TryTakeFirstJob - if (forced) - 129", true);
                            if (forced)
                            {
                                Log.Message(" - TryTakeFirstJob - pawn.jobs.TryTakeOrderedJob(job); - 130", true);
                                pawn.jobs.TryTakeOrderedJob(job);
                            }
                            else
                            {
                                Log.Message(" - TryTakeFirstJob - pawn.jobs.jobQueue.EnqueueFirst(job); - 131", true);
                                pawn.jobs.jobQueue.EnqueueLast(job);
                            }
                            ZLogger.Message(pawn + " taking " + job + " from TryTakeFirstJob");
                            Log.Message(" - TryTakeFirstJob - this.jobTracker[pawn].activeJobs.RemoveAt(0); - 133", true);
                            this.jobTracker[pawn].activeJobs.RemoveAt(0);
                        }
                        else
                        {
                            ZLogger.Message(pawn + " in " + this.GetMapInfo(pawn.Map) + " failed " + job);
                            ZLogger.Message("job.targetA.Thing.Map: " + job.targetA.Thing?.Map);
                            ZLogger.Message("job.targetB.Thing.Map: " + job.targetB.Thing?.Map);
                            ZLogger.Message("Active jobs: " + this.jobTracker[pawn].activeJobs.Count);
                            Log.Message(" - TryTakeFirstJob - foreach (var d in this.jobTracker[pawn].activeJobs) - 138", true);
                            foreach (var d in this.jobTracker[pawn].activeJobs)
                            {
                                ZLogger.Message("Active jobs: " + d + " - " + pawn);
                            }
                            ZLogger.Message("Main job: " + this.jobTracker[pawn].mainJob + " - " + pawn);
                            ZLogger.Pause("Fail in TryMakePreToilReservations in method TryTakeFirstJob, job: " + job);
                            this.ResetJobs(pawn);
                        }
                    }
                }
                else
                {
                    ZLogger.Message("Resetting jobs for " + pawn);
                    Log.Message(" - TryTakeFirstJob - this.ResetJobs(pawn); - 143", true);
                    this.ResetJobs(pawn);
                }

                //try
                //{
                //    foreach (var d in this.jobTracker[pawn].activeJobs)
                //    {
                //        ZLogger.Message("Active jobs 2: " + d + " - " + pawn);
                //    }
                //    foreach (var t in pawn.jobs.jobQueue)
                //    {
                //        ZLogger.Message("Active jobQueue 2: " + pawn + " - " + t.job);
                //    }
                //    ZLogger.Message("========================");
                //}
                //catch { }
                ZLogger.Message(pawn + " - END TryTakeFirstJob");
                Log.Message(" - TryTakeFirstJob - return true; - 150", true);
                return true;
            }
            catch (Exception ex)
            {
                ZLogger.Error("Fail in TryTakeFirstJob: " + ex);
                ZLogger.Pause("Error in TryTakeFirstJob, job: " + job);
            }
            ZLogger.Message(pawn + " - END TryTakeFirstJob 2");
            Log.Message(" - TryTakeFirstJob - return false; - 154", true);
            return false;
        }

        public void SimpleTeleportThing(Thing thingToTeleport, IntVec3 cellToTeleport, Map mapToTeleport, bool firstTime = false, int dealDamage = 0)
        {
            if (mapToTeleport.thingGrid.ThingsListAt(cellToTeleport).Any())
            {
                for (int i = mapToTeleport.thingGrid.ThingsListAt(cellToTeleport).Count - 1; i >= 0; i--)
                {
                    Thing thing = mapToTeleport.thingGrid.ThingsListAt(cellToTeleport)[i];
                    if (thing is Building)
                    {
                        if (thing.Spawned)
                        {
                            thing.DeSpawn(DestroyMode.Refund);
                        }
                    }
                }
            }

            bool jump = false;
            bool draft = false;
            if (thingToTeleport is Pawn pawnToTeleport)
            {
                if (Find.Selector.SelectedObjects.Contains(pawnToTeleport))
                {
                    jump = true;
                }
                if (pawnToTeleport.Drafted)
                {
                    draft = true;
                }

                try
                {
                    this.SaveArea(pawnToTeleport);
                }
                catch { }
            }

            RegionListersUpdater.DeregisterInRegions(thingToTeleport, thingToTeleport.Map);
            thingToTeleport.Map?.spawnedThings.Remove(thingToTeleport);
            thingToTeleport.Map?.listerThings.Remove(thingToTeleport);
            thingToTeleport.Map?.thingGrid.Deregister(thingToTeleport);
            thingToTeleport.Map?.coverGrid.DeRegister(thingToTeleport);
            thingToTeleport.Map?.tooltipGiverList.Notify_ThingDespawned(thingToTeleport);
            thingToTeleport.Map?.attackTargetsCache.Notify_ThingDespawned(thingToTeleport);
            thingToTeleport.Map?.physicalInteractionReservationManager.ReleaseAllForTarget(thingToTeleport);
            StealAIDebugDrawer.Notify_ThingChanged(thingToTeleport);
            thingToTeleport.Map?.dynamicDrawManager.DeRegisterDrawable(thingToTeleport);
            if (thingToTeleport is Pawn pawn1)
            {
                thingToTeleport.Map?.mapPawns.DeRegisterPawn(pawn1);
            }

            Traverse.Create(thingToTeleport).Field("mapIndexOrState")
                .SetValue((sbyte)Find.Maps.IndexOf(mapToTeleport));

            RegionListersUpdater.RegisterInRegions(thingToTeleport, mapToTeleport);
            mapToTeleport.spawnedThings.TryAdd(thingToTeleport);
            mapToTeleport.listerThings.Add(thingToTeleport);
            mapToTeleport.thingGrid.Register(thingToTeleport);
            mapToTeleport.coverGrid.Register(thingToTeleport);
            mapToTeleport.tooltipGiverList.Notify_ThingSpawned(thingToTeleport);
            mapToTeleport.attackTargetsCache.Notify_ThingSpawned(thingToTeleport);
            StealAIDebugDrawer.Notify_ThingChanged(thingToTeleport);
            mapToTeleport.dynamicDrawManager.RegisterDrawable(thingToTeleport);
            if (thingToTeleport is Pawn pawn2)
            {
                mapToTeleport.mapPawns.RegisterPawn(pawn2);
            }

            if (thingToTeleport is Pawn pawnToTeleport2)
            {
                try
                {
                    this.LoadArea(pawnToTeleport2);
                }
                catch { }

                if (jump)
                {
                    Current.Game.CurrentMap = mapToTeleport;
                    CameraJumper.TryJumpAndSelect(pawnToTeleport2);
                }
                if (draft)
                {
                    try
                    {
                        pawnToTeleport2.drafter.Drafted = true;
                    }
                    catch { }
                }
            }
            if (dealDamage > 0)
            {
                if (thingToTeleport.def.useHitPoints)
                {
                    thingToTeleport.HitPoints -= dealDamage;
                }
                else if (thingToTeleport is Pawn pawn)
                {
                    pawn.TakeDamage(new DamageInfo(DamageDefOf.Blunt, dealDamage));
                }
            }

            ZLogger.Message("Thing: " + thingToTeleport + " teleported to " + this.GetMapInfo(mapToTeleport) + " - " + cellToTeleport);
            if (firstTime)
            {
                ZLogger.Message("Map: " + mapToTeleport);
                try
                {
                    FloodFillerFog.DebugRefogMap(mapToTeleport);
                    foreach (var cell in mapToTeleport.AllCells)
                    {
                        FloodFillerFog.FloodUnfog(cell, mapToTeleport);
                    }
                }
                catch { };
            }

            FloodFillerFog.FloodUnfog(thingToTeleport.Position, mapToTeleport);

            AccessTools.Method(typeof(FogGrid), "FloodUnfogAdjacent").Invoke(mapToTeleport.fogGrid, new object[]
            { thingToTeleport.PositionHeld });
        }

        public void ZLevelsFixer(int tile)
        {
            if (this.ZLevelsTracker[tile]?.ZLevels[0]?.listerThings == null)
            {
                var map = Find.WorldObjects.MapParents.Where(x => x.Tile == tile
                && x.HasMap && x.Map.IsPlayerHome).FirstOrDefault().Map;
                this.ZLevelsTracker[tile].ZLevels[0] = map;
            }
        }

        public void TeleportPawn(Pawn pawnToTeleport, IntVec3 cellToTeleport, Map mapToTeleport, bool firstTime = false, bool spawnStairsBelow = false, bool spawnStairsUpper = false)
        {
            //ZLogger.Message("Trying to teleport to " + mapToTeleport);
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

            if (spawnStairsUpper)
            {
                if (this.GetZIndexFor(pawnToTeleport.Map) < this.GetZIndexFor(mapToTeleport))
                {
                    var stairs = this.GetLowerLevel(mapToTeleport.Tile, mapToTeleport)?.thingGrid?
                        .ThingsListAt(cellToTeleport)?.Where(x => x is Building_StairsUp)?.FirstOrDefault();
                    if (stairs != null && stairs.Stuff != null)
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
                    ZLogger.Message("Stairs: " + stairs);
                    if (stairs != null)
                    {
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
            }

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

            RegionListersUpdater.DeregisterInRegions(pawnToTeleport, pawnToTeleport.Map);
            pawnToTeleport.Map?.spawnedThings.Remove(pawnToTeleport);
            pawnToTeleport.Map?.listerThings.Remove(pawnToTeleport);
            pawnToTeleport.Map?.thingGrid.Deregister(pawnToTeleport);
            pawnToTeleport.Map?.coverGrid.DeRegister(pawnToTeleport);
            pawnToTeleport.Map?.tooltipGiverList.Notify_ThingDespawned(pawnToTeleport);
            pawnToTeleport.Map?.attackTargetsCache.Notify_ThingDespawned(pawnToTeleport);
            pawnToTeleport.Map?.physicalInteractionReservationManager.ReleaseAllForTarget(pawnToTeleport);
            StealAIDebugDrawer.Notify_ThingChanged(pawnToTeleport);
            pawnToTeleport.Map?.dynamicDrawManager.DeRegisterDrawable(pawnToTeleport);
            pawnToTeleport.Map?.mapPawns.DeRegisterPawn(pawnToTeleport);

            Traverse.Create(pawnToTeleport).Field("mapIndexOrState")
                .SetValue((sbyte)Find.Maps.IndexOf(mapToTeleport));

            RegionListersUpdater.RegisterInRegions(pawnToTeleport, mapToTeleport);
            mapToTeleport.spawnedThings.TryAdd(pawnToTeleport);
            mapToTeleport.listerThings.Add(pawnToTeleport);
            mapToTeleport.thingGrid.Register(pawnToTeleport);
            mapToTeleport.coverGrid.Register(pawnToTeleport);
            mapToTeleport.tooltipGiverList.Notify_ThingSpawned(pawnToTeleport);
            mapToTeleport.attackTargetsCache.Notify_ThingSpawned(pawnToTeleport);
            StealAIDebugDrawer.Notify_ThingChanged(pawnToTeleport);
            mapToTeleport.dynamicDrawManager.RegisterDrawable(pawnToTeleport);
            mapToTeleport.mapPawns.RegisterPawn(pawnToTeleport);

            //try
            //{
            //    this.TryTakeFirstJob(pawnToTeleport);
            //}
            //catch { };

            try
            {
                this.LoadArea(pawnToTeleport);
            }
            catch { }

            ZLogger.Message("Pawn: " + pawnToTeleport + " teleported to " + this.GetMapInfo(mapToTeleport));
            if (jump)
            {
                Current.Game.CurrentMap = mapToTeleport;
                CameraJumper.TryJumpAndSelect(pawnToTeleport);
            }
            if (draft)
            {
                try
                {
                    pawnToTeleport.drafter.Drafted = true;
                }
                catch { }
            }
            if (firstTime)
            {
                try
                {
                    ZLogger.Message("Map: " + mapToTeleport);
                    FloodFillerFog.DebugRefogMap(mapToTeleport);
                    foreach (var cell in mapToTeleport.AllCells)
                    {
                        FloodFillerFog.FloodUnfog(cell, mapToTeleport);
                    }
                }
                catch { };
            }

            FloodFillerFog.FloodUnfog(pawnToTeleport.Position, mapToTeleport);
            AccessTools.Method(typeof(FogGrid), "FloodUnfogAdjacent").Invoke(mapToTeleport.fogGrid, new object[]
            { pawnToTeleport.PositionHeld });

            try
            {
                ZLogger.Message("--------------------------");
                for (int i = this.jobTracker[pawnToTeleport].mainJob.targetQueueB.Count - 1; i >= 0; i--)
                {
                    var target = this.jobTracker[pawnToTeleport].mainJob.targetQueueB[i];

                    ZLogger.Message("TeleportPawn job.targetQueueB: " + target.Thing);
                    ZLogger.Message("TeleportPawn job.targetQueueB.Map: " + target.Thing.Map);
                    ZLogger.Message("TeleportPawn job.targetQueueB.stackCount: " + target.Thing.stackCount);
                    ZLogger.Message("TeleportPawn job.targetQueueB.countQueue: " + this.jobTracker[pawnToTeleport].mainJob.countQueue[i]);
                }
            }
            catch { }
        }

        public Map CreateLowerLevel(Map origin, IntVec3 playerStartSpot)
        {
            var comp = origin.GetComponent<MapComponentZLevel>();
            var mapParent = (MapParent_ZLevel)WorldObjectMaker.MakeWorldObject(ZLevelsDefOf.ZL_Underground);

            mapParent.Tile = origin.Tile;
            mapParent.PlayerStartSpot = playerStartSpot;
            mapParent.TotalInfestations = comp.TotalInfestations;
            mapParent.hasCaves = comp.hasCavesBelow.GetValueOrDefault(false);
            Find.WorldObjects.Add(mapParent);

            string seedString = Find.World.info.seedString;
            Find.World.info.seedString = new System.Random().Next(0, 2147483646).ToString();
            Map newMap = null;
            mapParent.finishedGeneration = false;
            mapParent.Z_LevelIndex = comp.Z_LevelIndex - 1;
            try
            {
                var pathToLoad = Path.Combine(Path.Combine(GenFilePaths.ConfigFolderPath,
                    "SavedMaps"), origin.Tile + " - " + (comp.Z_LevelIndex - 1) + ".xml");
                FileInfo fileInfo = new FileInfo(pathToLoad);
                if (fileInfo.Exists)
                {
                    ZLogger.Message("Loading from " + pathToLoad);
                    newMap = MapGenerator.GenerateMap(origin.Size, mapParent,
                        ZLevelsDefOf.ZL_EmptyMap, mapParent.ExtraGenStepDefs, null);
                    BlueprintUtility.LoadEverything(newMap, pathToLoad);
                }
                else
                {
                    newMap = MapGenerator.GenerateMap(origin.Size, mapParent, mapParent.MapGeneratorDef, mapParent.ExtraGenStepDefs, null);
                }
            }
            catch
            {
                newMap = MapGenerator.GenerateMap(origin.Size, mapParent, mapParent.MapGeneratorDef, mapParent.ExtraGenStepDefs, null);
            }
            mapParent.finishedGeneration = true;

            Find.World.info.seedString = seedString;
            try
            {
                if (this.TryRegisterMap(newMap, comp.Z_LevelIndex - 1))
                {
                    var newComp = newMap.GetComponent<MapComponentZLevel>();
                    newComp.Z_LevelIndex = comp.Z_LevelIndex - 1;
                }
                GameCondition_NoSunlight gameCondition_NoSunlight =
                    (GameCondition_NoSunlight)GameConditionMaker.MakeCondition(ZLevelsDefOf.ZL_UndergroundCondition, -1);
                gameCondition_NoSunlight.Permanent = true;
                newMap.gameConditionManager.RegisterCondition(gameCondition_NoSunlight);
            }
            catch
            {

            }
            return newMap;
        }

        public Map CreateUpperLevel(Map origin, IntVec3 playerStartSpot)
        {
            var mapParent = (MapParent_ZLevel)WorldObjectMaker.MakeWorldObject(ZLevelsDefOf.ZL_Upper);

            var comp = origin.GetComponent<MapComponentZLevel>();

            mapParent.Tile = origin.Tile;
            mapParent.PlayerStartSpot = playerStartSpot;
            mapParent.hasCaves = false;
            Find.WorldObjects.Add(mapParent);

            string seedString = Find.World.info.seedString;
            Find.World.info.seedString = new System.Random().Next(0, 2147483646).ToString();
            Map newMap = null;
            mapParent.finishedGeneration = false;
            mapParent.Z_LevelIndex = comp.Z_LevelIndex + 1;
            try
            {
                var pathToLoad = Path.Combine(Path.Combine(GenFilePaths.ConfigFolderPath,
                    "SavedMaps"), origin.Tile + " - " + (comp.Z_LevelIndex + 1) + ".xml");
                FileInfo fileInfo = new FileInfo(pathToLoad);
                if (fileInfo.Exists)
                {
                    ZLogger.Message("Loading from " + pathToLoad);
                    newMap = MapGenerator.GenerateMap(origin.Size, mapParent, ZLevelsDefOf.ZL_EmptyMap
                        , mapParent.ExtraGenStepDefs, null);
                    BlueprintUtility.LoadEverything(newMap, pathToLoad);
                }
                else
                {
                    newMap = MapGenerator.GenerateMap(origin.Size, mapParent, mapParent.MapGeneratorDef,
                        mapParent.ExtraGenStepDefs, null);
                }
            }
            catch
            {
                newMap = MapGenerator.GenerateMap(origin.Size, mapParent, mapParent.MapGeneratorDef,
                    mapParent.ExtraGenStepDefs, null);
            };
            mapParent.finishedGeneration = true;
            Find.World.info.seedString = seedString;
            try
            {
                if (this.TryRegisterMap(newMap, comp.Z_LevelIndex + 1))
                {
                    var newComp = newMap.GetComponent<MapComponentZLevel>();
                    newComp.Z_LevelIndex = comp.Z_LevelIndex + 1;
                    AdjustMapGeneration(newMap);
                }
            }
            catch
            {

            }
            try
            {
                newMap.terrainGrid.SetTerrain(playerStartSpot, ZLevelsDefOf.ZL_OutsideTerrainTwo);
            }
            catch { }

            foreach (var intVec in newMap.AllCells)
            {
                newMap.mapDrawer.MapMeshDirty(intVec, MapMeshFlag.Terrain);
                newMap.mapDrawer.MapMeshDirty(intVec, MapMeshFlag.Things);
                newMap.mapDrawer.MapMeshDirty(intVec, MapMeshFlag.FogOfWar);
            }
            return newMap;
        }

        private bool SurroundedWithRock(IntVec3 c, Map map)
        {
            int num = 0;
            foreach (var cell in GenAdj.CellsAdjacentCardinal(c, Rot4.South, new IntVec2(1, 1)))
            {
                if (GenGrid.InBounds(cell, map) && cell.GetFirstBuilding(map) is Mineable)
                {
                    num++;
                    if (num >= 2)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public void AdjustMapGeneration(Map map)
        {
            try
            {
                Map mapBelow = this.GetLowerLevel(map.Tile, map);
                RockNoises.Init(map);
                foreach (IntVec3 allCell in map.AllCells)
                {
                    TerrainDef terrainDef = null;
                    if (mapBelow.roofGrid.RoofAt(allCell) != null && !mapBelow.roofGrid.RoofAt(allCell).isNatural)
                    {
                        terrainDef = ZLevelsDefOf.ZL_RoofTerrain;
                    }
                    else if (mapBelow.roofGrid.RoofAt(allCell) != null
                        && mapBelow.roofGrid.RoofAt(allCell).isNatural)
                    {
                        if (allCell.GetEdifice(mapBelow) is Mineable rock && rock.def.building.isNaturalRock
                            && !rock.def.building.isResourceRock)
                        {
                            terrainDef = TerrainDef.Named(rock.def.defName + "_Rough");
                            if (terrainDef == null)
                            {
                                terrainDef = TerrainDef.Named("Granite_Rough");
                            }
                        }
                        else
                        {
                            terrainDef = TerrainDef.Named("Granite_Rough");
                        }
                    }
                    if (allCell.GetEdifice(mapBelow) is Mineable rock2 && rock2.Spawned && !rock2.Destroyed
                        && mapBelow.roofGrid.RoofAt(allCell) != null
                        && (mapBelow.roofGrid.RoofAt(allCell) == RoofDefOf.RoofRockThick
                        || mapBelow.roofGrid.RoofAt(allCell) == RoofDefOf.RoofRockThin))
                    {
                        try
                        {
                            try
                            {
                                if (terrainDef == ZLevelsDefOf.ZL_RoofTerrain)
                                {
                                    terrainDef = rock2.def.building.naturalTerrain;
                                }
                            }
                            catch { };
                            GenSpawn.Spawn(rock2.def, allCell, map);
                            if (SurroundedWithRock(allCell, map))
                            {
                                map.roofGrid.SetRoof(allCell, allCell.GetRoof(mapBelow));
                            }
                            else
                            {
                                map.roofGrid.SetRoof(allCell, null);
                            }
                        }
                        catch { };

                    }
                    if (terrainDef != null)
                    {
                        map.terrainGrid.SetTerrain(allCell, terrainDef);
                    }
                }
                GenStep_ScatterLumpsMineableUnderground genStep_ScatterLumpsMineable = new GenStep_ScatterLumpsMineableUnderground
                {
                    maxValue = float.MaxValue
                };
                float num3 = 15f;
                genStep_ScatterLumpsMineable.countPer10kCellsRange = new FloatRange(num3, num3);
                genStep_ScatterLumpsMineable.Generate(map, default(GenStepParams));
            }
            catch { };
        }

        public override void StartedNewGame()
        {
            base.StartedNewGame();
        }

        public override void LoadedGame()
        {
            base.LoadedGame();
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

        public Dictionary<Map, List<Thing>> stairsUp = new Dictionary<Map, List<Thing>>();
        public Dictionary<Map, List<Thing>> stairsDown = new Dictionary<Map, List<Thing>>();

    }
}