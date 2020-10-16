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

        public ConnectedPowerNets connectedPowerNets;

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            if (Find.TickManager.TicksGame % 60 == 0)
            {
                foreach (var tile in this.ZLevelsTracker)
                {
                    //foreach (var map in this.GetAllMaps(tile.Key))
                    //{
                    //    ZLogger.Message(this.GetMapInfo(map) + " - " + map.Biome);
                    //}
                    this.ZLevelsFixer(tile.Key);
                }
            }
        }

        public override void GameComponentOnGUI()
        {
            base.GameComponentOnGUI();
            this.CheckHotkeys();
        }

        public void PreInit()
        {
            connectedPowerNets = Current.Game.GetComponent<ConnectedPowerNets>();
        }

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

            List<object> selected = Find.Selector.selected;

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
            Find.Selector.selected = selected;
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
            try
            {
                if (pawn.playerSettings != null && pawn.playerSettings.AreaRestriction != null)
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
                }

            }
            catch (Exception ex)
            {
                Log.Error("Exception of saving pawn area for Z-Levels: " + ex);
            }
        }

        public void LoadArea(Pawn pawn)
        {
            try
            {
                if (this.activeAreas == null) this.activeAreas = new Dictionary<Pawn, ActiveArea>();
                if (this.activeAreas.ContainsKey(pawn) && this.activeAreas[pawn].activeAreas != null && this.activeAreas[pawn].activeAreas.ContainsKey(pawn.Map))
                {
                    if (pawn.playerSettings == null) pawn.playerSettings = new Pawn_PlayerSettings(pawn);
                    pawn.playerSettings.AreaRestriction = this.activeAreas[pawn].activeAreas[pawn.Map];
                }
                else
                {
                    if (pawn.playerSettings == null) pawn.playerSettings = new Pawn_PlayerSettings(pawn);
                    pawn.playerSettings.AreaRestriction = null;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Exception of loading pawn area for Z-Levels: " + ex);
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
            bool deleteTile = false;
            if (this.ZLevelsTracker.ContainsKey(tile))
            {

                foreach (var map in this.ZLevelsTracker[tile].ZLevels.Values)
                {
                    if (map == null && this.ZLevelsTracker[tile].ZLevels.Values.Where(x => x != null).Any())
                    {
                        Log.Error("ZLevels contains null map, this should never happen");
                        foreach (var mapData in this.ZLevelsTracker[tile].ZLevels)
                        {
                            ZLogger.Message("Tile: " + tile + " - map index: " + mapData.Key + " - map: " + mapData.Value);
                        }
                    }
                    else
                    {
                        if (map == null && this.ZLevelsTracker[tile].ZLevels.Values.Where(x => x != null).Count() == 0)
                        {
                            deleteTile = true;
                        }
                    }
                    if (map != null)
                    {
                        maps.Add(map);
                    }
                }
            }
            if (deleteTile)
            {
                ZLogger.Pause("Removing " + tile + " from Z-Levels");
                this.ZLevelsTracker.Remove(tile);
            }
            return maps;
        }

        public List<Map> GetAllMapsInClosestOrder(Map pawnMap)
        {
            List<Map> maps = new List<Map>();
            if (this.ZLevelsTracker.ContainsKey(pawnMap.Tile))
            {
                foreach (var map in this.ZLevelsTracker[pawnMap.Tile].ZLevels.Values.OrderBy(x =>
                    (int)Mathf.Abs(this.GetZIndexFor(x) - this.GetZIndexFor(pawnMap))))
                {
                    //ZLogger.Message("Yielding " + this.GetMapInfo(map));
                    maps.Add(map);
                }
            }
            else
            {
                ZLogger.Pause(pawnMap + " has no registered maps");
            }
            return maps;
        }

        public int GetZIndexFor(Map map)
        {
            int index;
            if (this.mapIndex != null && this.mapIndex.TryGetValue(map, out index))
            {
                //ZLogger.Message("1 return: " + index + " for " + map, true);
                return index;
            }
            else
            {
                var comp = ZUtils.GetMapComponentZLevel(map);
                if (this.mapIndex == null)
                {
                    this.mapIndex = new Dictionary<Map, int>();
                }
                this.mapIndex[map] = comp.Z_LevelIndex;
                //ZLogger.Message("2 return: " + comp.Z_LevelIndex + " for " + map, true);
                return comp.Z_LevelIndex;
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
            ZLogger.Message(" - TryRegisterMap - if (this.ZLevelsTracker == null) - 1", true);
            if (this.ZLevelsTracker == null) this.ZLevelsTracker = new Dictionary<int, ZLevelData>();

            ZLogger.Message(" - TryRegisterMap - if (this.mapIndex == null) - 4", true);
            if (this.mapIndex == null)
            {
                ZLogger.Message(" - TryRegisterMap - this.mapIndex = new Dictionary<Map, int>(); - 5", true);
                this.mapIndex = new Dictionary<Map, int>();
            }
            ZLogger.Message(" - TryRegisterMap - if (this.ZLevelsTracker.ContainsKey(map.Tile)) - 6", true);
            if (this.ZLevelsTracker.ContainsKey(map.Tile))
            {
                ZLogger.Message(" - TryRegisterMap - if (this.ZLevelsTracker[map.Tile].ZLevels == null) - 7", true);
                if (this.ZLevelsTracker[map.Tile].ZLevels == null) this.ZLevelsTracker[map.Tile].ZLevels = new Dictionary<int, Map>();
                    
                ZLogger.Message(" - TryRegisterMap - this.ZLevelsTracker[map.Tile].ZLevels[index] = map; - 9", true);
                this.ZLevelsTracker[map.Tile].ZLevels[index] = map;

                ZLogger.Message(" - TryRegisterMap - if (!this.mapIndex.ContainsKey(map)) - 10", true);
                if (!this.mapIndex.ContainsKey(map))
                {
                    ZLogger.Message(" - TryRegisterMap - this.mapIndex[map] = index; - 11", true);
                    this.mapIndex[map] = index;
                }

                ZLogger.Message("1 Registering " + this.GetMapInfo(map) + " for index: " + index);
                ZLogger.Message(" - TryRegisterMap - return true; - 13", true);
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

                ZLogger.Message("this.ZLevelsTracker.ContainsKey(map.Tile): " + this.ZLevelsTracker.ContainsKey(map.Tile) + " - " + map.Tile + " - " + map, true);
                if (!this.mapIndex.ContainsKey(map))
                {
                    this.mapIndex[map] = index;
                }
                return true;
            }
            return false;
        }
        public string ShowJobData(Job job, Pawn pawn, Map dest)
        {
            string str = "-------------------------\n";
            try
            {
                str += pawn + " - " + job + "\n";
                str += "Pawn map: " + pawn.Map + "\n";
                str += "ZTracker dest map: " + this.jobTracker[pawn].targetDest + "\n";
                str += "Dest map: " + dest + "\n";
                str += "Job.workGiverDef: " + job.workGiverDef + "\n";
                str += "Job.jobGiver: " + job.jobGiver + "\n";
                str += "Job.count: " + job.count + "\n";
                str += "Job.targetA: " + job.targetA + "\n";
                str += "Job.targetA: " + job.targetA.Thing?.Map + "\n";
                str += "Job.targetB: " + job.targetB + "\n";
                str += "Job.targetB: " + job.targetB.Thing?.Map + "\n";
                str += "Job.targetC: " + job.targetC + "\n";
                str += "Job.targetC: " + job.targetC.Thing?.Map + "\n";
                str += "Job.targetQueueA: " + job.targetQueueA + "\n";
                str += "Job.haulOpportunisticDuplicates: " + job.haulOpportunisticDuplicates + "\n";
                try
                {
                    foreach (var t in job.targetQueueA)
                    {
                        str += "targetQueueA: " + t + "\n";
                        str += "targetQueueA.Map: " + t.Thing?.Map + "\n";
                    }
                }
                catch { }
                str += "Job.targetQueueB: " + job.targetQueueB + "\n";
                try
                {
                    foreach (var t in job.targetQueueB)
                    {
                        str += "targetQueueB: " + t + "\n";
                        str += "targetQueueB.Map: " + t.Thing?.Map + "\n";
                    }
                }
                catch { }
                str += "Job.countQueue: " + job.countQueue + "\n";

                try
                {
                    foreach (var t in job.countQueue)
                    {
                        str += "countQueue: " + t + "\n";
                    }
                }
                catch { }
                str += "------------------------\n";
            }
            catch { };
            return str;
        }

        public void ReserveTargets(Pawn pawn, Job job)
        {
            ZLogger.Message(pawn + " is starting reserving things for " + job, debugLevel: DebugLevel.Jobs);
            if (!this.jobTracker.ContainsKey(pawn)) this.jobTracker[pawn] = new JobTracker();
            if (this.jobTracker[pawn].reservedThings == null) this.jobTracker[pawn].reservedThings = new List<LocalTargetInfo>();
            if (job.targetA != null)
            {
                ZLogger.Message(pawn + " reserving " + job.targetA, true, debugLevel: DebugLevel.Jobs);
                this.jobTracker[pawn].reservedThings.Add(job.targetA);
            }
            if (job.targetB != null)
            {
                ZLogger.Message(pawn + " reserving " + job.targetB, true, debugLevel: DebugLevel.Jobs);
                this.jobTracker[pawn].reservedThings.Add(job.targetB);
            }
            if (job.targetC != null)
            {
                ZLogger.Message(pawn + " reserving " + job.targetC, true, debugLevel: DebugLevel.Jobs);
                this.jobTracker[pawn].reservedThings.Add(job.targetC);
            }
            try
            {
                foreach (var t in job.targetQueueA)
                {
                    ZLogger.Message(pawn + " reserving " + t, true, debugLevel: DebugLevel.Jobs);
                    this.jobTracker[pawn].reservedThings.Add(t);
                }
            }
            catch { }
            try
            {
                foreach (var t in job.targetQueueB)
                {
                    ZLogger.Message(pawn + " reserving " + t, true, debugLevel: DebugLevel.Jobs);
                    this.jobTracker[pawn].reservedThings.Add(t);
                }
            }
            catch { }
        }
        public void BuildJobListFor(Pawn pawn, Map dest, Job jobToDo)
        {
            this.ResetJobs(pawn);
            this.ReserveTargets(pawn, jobToDo);
            List<Job> tempJobs = new List<Job>();
            string log = "";
            if (jobToDo.def == JobDefOf.HaulToCell)
            {
                ZLogger.Message("Job method 1");
                log += "Job method 1\n";
                ZLogger.Message(pawn + " haul " + jobToDo.targetA.Thing + " to " + dest);
                this.jobTracker[pawn].targetDest = new TargetInfo(jobToDo.targetB.Cell, dest);
                Job job = JobMaker.MakeJob(ZLevelsDefOf.ZL_HaulThingToDest, jobToDo.targetA.Thing);
                job.haulOpportunisticDuplicates = false;
                job.count = jobToDo.count;
                tempJobs.Add(job);
            }
            else if (jobToDo.def == JobDefOf.HaulToContainer)
            {
                ZLogger.Message("Job method 1.1");
                log += "Job method 1.1\n";
                ZLogger.Message(pawn + " haul " + jobToDo.targetA.Thing + " to " + dest);
                this.jobTracker[pawn].targetDest = new TargetInfo(jobToDo.targetB.Thing);
                Job job = JobMaker.MakeJob(ZLevelsDefOf.ZL_HaulThingToDest, jobToDo.targetA.Thing);
                job.count = jobToDo.count;
                tempJobs.Add(job);
            }
            else if (jobToDo.def == JobDefOf.Rescue || jobToDo.def == JobDefOf.Capture || jobToDo.def == JobDefOf.Arrest)
            {
                ZLogger.Message("Job method 1.5: " + jobToDo.targetA.Thing);
                log += "Job method 1.5: " + jobToDo.targetA.Thing + "\n";
                this.jobTracker[pawn].targetDest = new TargetInfo(jobToDo.targetB.Thing);
                tempJobs.Add(JobMaker.MakeJob(ZLevelsDefOf.ZL_HaulThingToDest, jobToDo.targetA.Thing));
            }
            else if (jobToDo.def == JobDefOf.Refuel)
            {
                ZLogger.Message("Job method 1.7: " + jobToDo.count);
                log += "Job method 1.7: " + jobToDo.count + "\n";
                this.jobTracker[pawn].targetDest = new TargetInfo(jobToDo.targetA.Thing);
                Job job = JobMaker.MakeJob(ZLevelsDefOf.ZL_HaulThingToDest, jobToDo.targetB.Thing);
                job.count = jobToDo.count;
                tempJobs.Add(job);
            }
            else if (jobToDo.def == JobDefOf.Harvest)
            {
                if (jobToDo.targetA.Thing != null)
                {
                    ZLogger.Message("Job method 1.75");
                    log += "Job method 1.75\n";
                    this.jobTracker[pawn].targetDest = new TargetInfo(jobToDo.targetA.Thing);
                    tempJobs.Add(JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToMap));
                }
                else
                {
                    var target = jobToDo.targetQueueA.Where(x => x.HasThing && x.Thing.Map != null).FirstOrDefault();
                    if (target != null && target.Thing?.Map != null)
                    {
                        ZLogger.Message("Job method 1.76");
                        log += "Job method 1.76\n";
                        this.jobTracker[pawn].targetDest = new TargetInfo(target.Thing);
                        tempJobs.Add(JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToMap));
                    }
                }
            }
            else if (jobToDo.def == JobDefOf.Clean)
            {
                var target = jobToDo.targetQueueA.Where(x => x.HasThing && x.Thing.Map != null).FirstOrDefault();
                if (target != null && target.Thing?.Map != null)
                {
                    ZLogger.Message("Job method 1.79");
                    log += "Job method 1.79\n";
                    this.jobTracker[pawn].targetDest = new TargetInfo(target.Thing);
                    tempJobs.Add(JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToMap));
                }
            }
            else if (jobToDo?.targetQueueA?.Count > 0
                && jobToDo.targetQueueA.Where(x => x.HasThing && x.Thing.Map != null).Any())
            {
                ZLogger.Message("Job method 1.8");
                log += "Job method 1.8\n";
                if (jobToDo.targetQueueA?.Count > 1)
                {
                    if (jobToDo.targetQueueA?.Count == jobToDo.countQueue?.Count)
                    {
                        for (int i = 0; i < jobToDo.targetQueueA.Count; i++)
                        {
                            var t = jobToDo.targetQueueA[i];
                            if (t.HasThing && t.Thing.Map != null)
                            {
                                ZLogger.Message("DEST: " + this.GetMapInfo(dest));
                                Job job;
                                if (t.Thing is Filth)
                                {
                                    this.jobTracker[pawn].targetDest = new TargetInfo(t.Thing);
                                    job = JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToMap);
                                }
                                else
                                {
                                    if (jobToDo.targetA.Cell.IsValid)
                                    {
                                        this.jobTracker[pawn].targetDest = new TargetInfo(jobToDo.targetA.Cell, dest);
                                        job = JobMaker.MakeJob(ZLevelsDefOf.ZL_HaulToCell, t.Thing, jobToDo.targetA.Cell);
                                    }
                                    else
                                    {
                                        this.jobTracker[pawn].targetDest = new TargetInfo(pawn.Position, dest);
                                        job = JobMaker.MakeJob(ZLevelsDefOf.ZL_HaulThingToDest, t.Thing);
                                    }
                                }
                                job.count = jobToDo.countQueue[i];
                                tempJobs.Add(job);
                            }
                        }
                    }
                    else
                    {
                        foreach (var t in jobToDo.targetQueueA)
                        {
                            if (t.HasThing && t.Thing.Map != null)
                            {
                                ZLogger.Message("DEST:2 " + this.GetMapInfo(dest));
                                Job job;
                                if (t.Thing is Filth)
                                {
                                    this.jobTracker[pawn].targetDest = new TargetInfo(t.Thing);
                                    job = JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToMap);
                                }
                                else
                                {
                                    if (jobToDo.targetA.Cell.IsValid)
                                    {
                                        this.jobTracker[pawn].targetDest = new TargetInfo(jobToDo.targetA.Cell, dest);
                                        job = JobMaker.MakeJob(ZLevelsDefOf.ZL_HaulToCell, t.Thing, jobToDo.targetA.Cell);
                                    }
                                    else
                                    {
                                        this.jobTracker[pawn].targetDest = new TargetInfo(pawn.Position, dest);
                                        job = JobMaker.MakeJob(ZLevelsDefOf.ZL_HaulThingToDest, t.Thing);
                                    }
                                }
                                tempJobs.Add(job);
                            }
                        }
                    }
                }
                else
                {
                    if (jobToDo.targetQueueA?.Count == jobToDo.countQueue?.Count)
                    {
                        for (int i = 0; i < jobToDo.targetQueueA.Count; i++)
                        {
                            var t = jobToDo.targetQueueA[i];
                            if (t.HasThing && t.Thing.Map != null)
                            {
                                ZLogger.Message("DEST: " + this.GetMapInfo(dest));
                                Job job;
                                if (t.Thing is Filth)
                                {
                                    this.jobTracker[pawn].targetDest = new TargetInfo(t.Thing);
                                    job = JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToMap);
                                }
                                else
                                {
                                    if (jobToDo.targetA.Cell.IsValid)
                                    {
                                        this.jobTracker[pawn].targetDest = new TargetInfo(jobToDo.targetA.Cell, dest);
                                        job = JobMaker.MakeJob(ZLevelsDefOf.ZL_HaulToCell, t.Thing, jobToDo.targetA.Cell);
                                    }
                                    else
                                    {
                                        this.jobTracker[pawn].targetDest = new TargetInfo(pawn.Position, dest);
                                        job = JobMaker.MakeJob(ZLevelsDefOf.ZL_HaulThingToDest, t.Thing);
                                    }
                                }
                                job.count = jobToDo.countQueue[i];
                                tempJobs.Add(job);
                            }

                        }
                    }
                    else
                    {
                        foreach (var t in jobToDo.targetQueueA)
                        {
                            if (t.HasThing && t.Thing.Map != null)
                            {
                                ZLogger.Message("DEST:2 " + this.GetMapInfo(dest));
                                Job job;
                                if (t.Thing is Filth)
                                {
                                    this.jobTracker[pawn].targetDest = new TargetInfo(t.Thing);
                                    job = JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToMap);
                                }
                                else
                                {
                                    if (jobToDo.targetA.Cell.IsValid)
                                    {
                                        this.jobTracker[pawn].targetDest = new TargetInfo(jobToDo.targetA.Cell, dest);
                                        job = JobMaker.MakeJob(ZLevelsDefOf.ZL_HaulToCell, t.Thing, jobToDo.targetA.Cell);
                                    }
                                    else
                                    {
                                        this.jobTracker[pawn].targetDest = new TargetInfo(pawn.Position, dest);
                                        job = JobMaker.MakeJob(ZLevelsDefOf.ZL_HaulThingToDest, t.Thing);
                                    }
                                }
                                tempJobs.Add(job);
                            }
                        }
                    }
                }
            }
            else if (jobToDo?.targetQueueB?.Count > 0
                && jobToDo.targetQueueB.Where(x => x.HasThing && x.Thing.Map != null).Any())
            {
                ZLogger.Message("Job method 2");
                log += "Job method 2\n";
                if (jobToDo.targetQueueB?.Count > 1)
                {
                    if (jobToDo.targetQueueB?.Count == jobToDo.countQueue?.Count)
                    {
                        for (int i = 0; i < jobToDo.targetQueueB.Count; i++)
                        {
                            var t = jobToDo.targetQueueB[i];
                            if (t.HasThing && t.Thing.Map != null)
                            {
                                ZLogger.Message("DEST: " + this.GetMapInfo(dest));
                                Job job;
                                if (t.Thing is Filth)
                                {
                                    this.jobTracker[pawn].targetDest = new TargetInfo(t.Thing);
                                    job = JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToMap);
                                }
                                else
                                {
                                    if (jobToDo.targetB.Cell.IsValid)
                                    {
                                        this.jobTracker[pawn].targetDest = new TargetInfo(jobToDo.targetB.Cell, dest);
                                        job = JobMaker.MakeJob(ZLevelsDefOf.ZL_HaulToCell, t.Thing, jobToDo.targetB.Cell);
                                    }
                                    else
                                    {
                                        this.jobTracker[pawn].targetDest = new TargetInfo(pawn.Position, dest);
                                        job = JobMaker.MakeJob(ZLevelsDefOf.ZL_HaulThingToDest, t.Thing);
                                    }
                                }
                                job.count = jobToDo.countQueue[i];
                                tempJobs.Add(job);
                            }
                        }
                    }
                    else
                    {
                        foreach (var t in jobToDo.targetQueueB)
                        {
                            if (t.HasThing && t.Thing.Map != null)
                            {
                                ZLogger.Message("DEST:2 " + this.GetMapInfo(dest));
                                Job job;
                                if (t.Thing is Filth)
                                {
                                    this.jobTracker[pawn].targetDest = new TargetInfo(t.Thing);
                                    job = JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToMap);
                                }
                                else
                                {
                                    if (jobToDo.targetB.Cell.IsValid)
                                    {
                                        this.jobTracker[pawn].targetDest = new TargetInfo(jobToDo.targetB.Cell, dest);
                                        job = JobMaker.MakeJob(ZLevelsDefOf.ZL_HaulToCell, t.Thing, jobToDo.targetB.Cell);
                                    }
                                    else
                                    {
                                        this.jobTracker[pawn].targetDest = new TargetInfo(pawn.Position, dest);
                                        job = JobMaker.MakeJob(ZLevelsDefOf.ZL_HaulThingToDest, t.Thing);
                                    }
                                }
                                tempJobs.Add(job);
                            }
                        }
                    }
                }
                else
                {
                    if (jobToDo.targetQueueB?.Count == jobToDo.countQueue?.Count)
                    {
                        for (int i = 0; i < jobToDo.targetQueueB.Count; i++)
                        {
                            var t = jobToDo.targetQueueB[i];
                            if (t.HasThing && t.Thing.Map != null)
                            {
                                ZLogger.Message("DEST: " + this.GetMapInfo(dest));
                                Job job;
                                if (t.Thing is Filth)
                                {
                                    this.jobTracker[pawn].targetDest = new TargetInfo(t.Thing);
                                    job = JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToMap);
                                }
                                else
                                {
                                    if (jobToDo.targetB.Cell.IsValid)
                                    {
                                        this.jobTracker[pawn].targetDest = new TargetInfo(jobToDo.targetB.Cell, dest);
                                        job = JobMaker.MakeJob(ZLevelsDefOf.ZL_HaulToCell, t.Thing, jobToDo.targetB.Cell);
                                    }
                                    else
                                    {
                                        this.jobTracker[pawn].targetDest = new TargetInfo(pawn.Position, dest);
                                        job = JobMaker.MakeJob(ZLevelsDefOf.ZL_HaulThingToDest, t.Thing);
                                    }
                                }
                                job.count = jobToDo.countQueue[i];
                                tempJobs.Add(job);
                            }
                        }
                    }
                    else
                    {
                        foreach (var t in jobToDo.targetQueueB)
                        {
                            if (t.HasThing && t.Thing.Map != null)
                            {
                                ZLogger.Message("DEST:2 " + this.GetMapInfo(dest));
                                Job job;
                                if (t.Thing is Filth)
                                {
                                    this.jobTracker[pawn].targetDest = new TargetInfo(t.Thing);
                                    job = JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToMap);
                                }
                                else
                                {
                                    if (jobToDo.targetB.Cell.IsValid)
                                    {
                                        this.jobTracker[pawn].targetDest = new TargetInfo(jobToDo.targetB.Cell, dest);
                                        job = JobMaker.MakeJob(ZLevelsDefOf.ZL_HaulToCell, t.Thing, jobToDo.targetB.Cell);
                                    }
                                    else
                                    {
                                        this.jobTracker[pawn].targetDest = new TargetInfo(pawn.Position, dest);
                                        job = JobMaker.MakeJob(ZLevelsDefOf.ZL_HaulThingToDest, t.Thing);
                                    }
                                }
                                tempJobs.Add(job);
                            }
                        }
                    }
                }
            }
            else if (jobToDo.targetA != null && jobToDo.targetA.Thing?.Map != null)
            {
                ZLogger.Message("Job method 3: " + jobToDo.targetA.Thing);
                log += "Job method 3: " + jobToDo.targetA.Thing + "\n";
                this.jobTracker[pawn].targetDest = new TargetInfo(jobToDo.targetA.Thing);
                tempJobs.Add(JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToMap));
            }
            else if (jobToDo.targetB != null && jobToDo.targetB.Thing?.Map != null)
            {
                ZLogger.Message("Job method 4: " + jobToDo.targetB.Thing);
                log += "Job method 4: " + jobToDo.targetB.Thing + "\n";
                this.jobTracker[pawn].targetDest = new TargetInfo(jobToDo.targetB.Thing);
                tempJobs.Add(JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToMap));
            }
            else if (dest != null)
            {
                ZLogger.Message("Job method 5");
                log += "Job method 5\n";
                this.jobTracker[pawn].targetDest = new TargetInfo(pawn.Position, dest);
                tempJobs.Add(JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToMap));
            }
            tempJobs.Add(jobToDo);
            this.jobTracker[pawn].activeJobs = tempJobs;
            this.jobTracker[pawn].mainJob = jobToDo;
            foreach (var j in tempJobs)
            {
                log += "Added job: " + j + "\n";
            }
            ZLogger.Message(this.ShowJobData(jobToDo, pawn, dest) + log);
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
                if (this.jobTracker[pawn].activeJobs?.Any() ?? false)
                {
                    this.jobTracker[pawn].activeJobs.Clear();
                }
                if (this.jobTracker[pawn].activeJobs == null)
                {
                    this.jobTracker[pawn].activeJobs = new List<Job>();
                }
                this.jobTracker[pawn].targetDest = null;
                this.jobTracker[pawn].mainJob = null;
                this.jobTracker[pawn].forceGoToDestMap = false;
                this.jobTracker[pawn].failIfTargetMapIsNotDest = false;
                this.jobTracker[pawn].target = null;
                //this.jobTracker[pawn].ignoreGiversInFirstTime = null;
                this.jobTracker[pawn].oldMap = null;
                this.jobTracker[pawn].reservedThings = null;
                this.jobTracker[pawn].searchingJobsNow = false;
                ZLogger.Message("Resetting job data");
            }
            else
            {
                ZLogger.Message("Resetting jobTracker for " + pawn);
                this.jobTracker[pawn] = new JobTracker
                {
                    activeJobs = new List<Job>()
                };
            }
            pawn.jobs.EndCurrentJob(JobCondition.Errored);
        }

        public bool TryTakeFirstJob(Pawn pawn, bool forced = false)
        {
            Job job = null;
            ZLogger.Message(pawn + " - START TryTakeFirstJob");
            try
            {
                if (this.jobTracker.ContainsKey(pawn) && this.jobTracker[pawn].activeJobs != null && this.jobTracker[pawn].activeJobs.Any())
                {
                    job = this.jobTracker[pawn].activeJobs[0];
                    if (job?.def != null)
                    {
                        ZLogger.Message(pawn + " taking first job " + job);
                        if (pawn?.carryTracker?.CarriedThing != null)
                        {
                            ZLogger.Message(pawn + " CarriedThing " + pawn?.carryTracker?.CarriedThing);
                            try
                            {
                                ZLogger.Message("--------------------------");
                                for (int i = this.jobTracker[pawn].mainJob.targetQueueB.Count - 1; i >= 0; i--)
                                {
                                    var target = this.jobTracker[pawn].mainJob.targetQueueB[i];
                                    ZLogger.Message("0 BEFORE job.targetQueueB: " + target.Thing);
                                    ZLogger.Message("0 BEFORE job.targetQueueB.Map: " + target.Thing.Map);
                                    ZLogger.Message("0 BEFORE job.targetQueueB.stackCount: " + target.Thing.stackCount);
                                    ZLogger.Message("0 BEFORE job.targetQueueB.countQueue: " + this.jobTracker[pawn].mainJob.countQueue[i]);
                                }
                            }
                            catch { }

                            Thing savedThing = pawn.carryTracker.CarriedThing;
                            ZLogger.Message(pawn + " trying to drop " + pawn?.carryTracker?.CarriedThing + " for " + job);
                            Thing newThing;
                            pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Direct, out newThing);
                            //ZLogger.Pause("Try drop 1");

                            if (job.def == ZLevelsDefOf.ZL_HaulToCell)
                            {
                                if (job.targetA.Thing != newThing && newThing != null)
                                {
                                    job.targetA = new LocalTargetInfo(newThing);
                                }
                            }
                            ZLogger.Message(pawn + " dropping " + newThing + " for " + job);
                            try
                            {
                                if (newThing != null && job.targetA.Thing == savedThing && savedThing != newThing)
                                {
                                    ZLogger.Message(newThing + " 0 job.targetA is not same: " + job.targetA.Thing);
                                    job.targetA = new LocalTargetInfo(newThing);
                                }

                                if (newThing != null && job.targetB.Thing == savedThing && savedThing != newThing)
                                {
                                    ZLogger.Message(newThing + " 0 job.targetB is not same: " + job.targetB.Thing);
                                    job.targetB = new LocalTargetInfo(newThing);
                                }
                            }
                            catch { };
                            try
                            {
                                for (int i = job.targetQueueA.Count - 1; i >= 0; i--)
                                {
                                    var target = job.targetQueueA[i];
                                    ZLogger.Message("0 job.targetQueueA: " + target.Thing + " - " + target.Thing.Map);
                                    if (newThing != null && target.Thing == savedThing && savedThing != newThing)
                                    {
                                        ZLogger.Message(newThing + " 0 job.targetQueueA is not same: " + target.Thing);
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
                                    var target = job.targetQueueB[i];
                                    ZLogger.Message("17 job.targetQueueB: " + target.Thing);
                                    ZLogger.Message("17 job.targetQueueB.Map: " + target.Thing.Map);
                                    ZLogger.Message("17 job.targetQueueB.stackCount: " + target.Thing.stackCount);
                                    ZLogger.Message("17 job.targetQueueB.countQueue: " + job.countQueue);
                                    if (newThing != null && target.Thing == savedThing && savedThing != newThing)
                                    {
                                        ZLogger.Message(newThing + " 0 job.targetQueueB is not same: " + target.Thing);
                                        job.targetQueueB[i] = new LocalTargetInfo(newThing);
                                        job.countQueue[i] = newThing.stackCount;

                                    }
                                }
                            }
                            catch { }
                            if (job != this.jobTracker[pawn].mainJob)
                            {
                                try
                                {
                                    if (newThing != null && this.jobTracker[pawn].mainJob.targetA.Thing == savedThing && savedThing != newThing)
                                    {
                                        ZLogger.Message(newThing + " 0 this.jobTracker[pawn].mainJob.targetA is not same: " + this.jobTracker[pawn].mainJob.targetA.Thing);
                                        this.jobTracker[pawn].mainJob.targetA = new LocalTargetInfo(newThing);
                                    }

                                    if (newThing != null && this.jobTracker[pawn].mainJob.targetB.Thing == savedThing && savedThing != newThing)
                                    {
                                        ZLogger.Message(newThing + " 0 this.jobTracker[pawn].mainJob.targetB is not same: " + this.jobTracker[pawn].mainJob.targetB.Thing);
                                        this.jobTracker[pawn].mainJob.targetB = new LocalTargetInfo(newThing);
                                    }
                                }
                                catch { }
                                try
                                {
                                    for (int i = this.jobTracker[pawn].mainJob.targetQueueA.Count - 1; i >= 0; i--)
                                    {
                                        var target = this.jobTracker[pawn].mainJob.targetQueueA[i];
                                        ZLogger.Message("0 this.jobTracker[pawn].mainJob.targetQueueA: " + target.Thing + " - " + target.Thing.Map);
                                        if (newThing != null && target.Thing == savedThing && savedThing != newThing)
                                        {
                                            ZLogger.Message(newThing + " 0 this.jobTracker[pawn].mainJob.targetQueueA is not same: " + target.Thing);
                                            this.jobTracker[pawn].mainJob.targetQueueA[i] = new LocalTargetInfo(newThing);
                                        }
                                    }
                                }
                                catch { }
                                try
                                {
                                    for (int i = this.jobTracker[pawn].mainJob.targetQueueB.Count - 1; i >= 0; i--)
                                    {
                                        var target = this.jobTracker[pawn].mainJob.targetQueueB[i];
                                        ZLogger.Message("0 this.jobTracker[pawn].mainJob.targetQueueB: " + target.Thing);
                                        if (newThing != null && target.Thing == savedThing && savedThing != newThing)
                                        {
                                            ZLogger.Message(newThing + " 0 this.jobTracker[pawn].mainJob.targetQueueB is not same: " + target.Thing);
                                            this.jobTracker[pawn].mainJob.targetQueueB[i] = new LocalTargetInfo(newThing);
                                            this.jobTracker[pawn].mainJob.countQueue[i] = newThing.stackCount;
                                        }
                                    }
                                }
                                catch { }
                            }
                            var mainJob = this.jobTracker[pawn].mainJob;

                            if (mainJob.countQueue != null && mainJob.countQueue?.Count == mainJob.targetQueueB?.Count)
                            {
                                bool repeat = true;
                                int num = 0;
                                try
                                {
                                    ZLogger.Message("--------------------------");
                                    for (int i = this.jobTracker[pawn].mainJob.targetQueueB.Count - 1; i >= 0; i--)
                                    {
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
                                    Dictionary<Thing, HashSet<int>> duplicates = new Dictionary<Thing, HashSet<int>>();
                                    for (int i = mainJob.targetQueueB.Count - 1; i >= 0; i--)
                                    {
                                        for (int j = mainJob.targetQueueB.Count - 1; j >= 0; j--)
                                        {
                                            if (i != j && mainJob.targetQueueB[i] == mainJob.targetQueueB[j])
                                            {
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
                                                    duplicates[mainJob.targetQueueB[i].Thing].Add(i);
                                                    ZLogger.Message("Adding " + mainJob.targetQueueB[i].Thing + " to duplicates with an index: " + i);
                                                }
                                            }
                                        }
                                    }

                                    if (duplicates.Count > 0)
                                    {
                                        foreach (var i in duplicates.First().Value.OrderBy(x => x).Skip(1).OrderByDescending(x => x))
                                        {
                                            ZLogger.Message("Removing item at " + i + " - " + mainJob.targetQueueB);
                                            mainJob.targetQueueB.RemoveAt(i);
                                            mainJob.countQueue.RemoveAt(i);
                                        }
                                    }
                                    if (duplicates.Count > 1)
                                    {
                                        repeat = true;
                                    }
                                    else
                                    {
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
                                        var target = this.jobTracker[pawn].mainJob.targetQueueB[i];

                                        ZLogger.Message("AFTER job.targetQueueB: " + target.Thing);
                                        ZLogger.Message("AFTER job.targetQueueB.Map: " + target.Thing.Map);
                                        ZLogger.Message("AFTER job.targetQueueB.stackCount: " + target.Thing.stackCount);
                                        ZLogger.Message("AFTER job.targetQueueB.countQueue: " + job.countQueue);

                                    }
                                }
                                catch { }
                            }
                        }
                        ZLogger.Message(pawn + " TryMakePreToilReservations job " + job + " in " + this.GetMapInfo(pawn.Map));
                        if (pawn.CurJob != null)
                        {
                            pawn.ClearReservationsForJob(pawn.CurJob);
                        }
                        if (job == this.jobTracker[pawn].mainJob && this.jobTracker[pawn].forceGoToDestMap && pawn.Map != this.jobTracker[pawn].targetDest.Map)
                        {
                            ZLogger.Message(pawn + " force taking go to map " + job + " in " + this.GetMapInfo(pawn.Map));
                            pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToMap));
                        }
                        else if (job.TryMakePreToilReservations(pawn, ZLogger.DebugEnabled))
                        {
                            ZLogger.Message(pawn + " taking job " + job + " in " + this.GetMapInfo(pawn.Map));
                            if (forced)
                            {
                                pawn.jobs.TryTakeOrderedJob(job);
                            }
                            else
                            {
                                pawn.jobs.jobQueue.EnqueueLast(job);
                            }
                            ZLogger.Message(pawn + " taking " + job + " from TryTakeFirstJob");
                            this.jobTracker[pawn].activeJobs.RemoveAt(0);
                            ZLogger.Message("Clearing ignored workgivers");
                            //this.jobTracker[pawn].ignoreGiversInFirstTime?.Clear();
                        }
                        else
                        {
                            ZLogger.Pause("Fail in TryMakePreToilReservations in method TryTakeFirstJob, job: " + job + ", map: " + this.GetMapInfo(pawn.Map));
                            this.ResetJobs(pawn);
                            //ZLogger.Message("Adding " + job.workGiverDef + " to ignored workgivers");
                            //if (this.jobTracker[pawn].ignoreGiversInFirstTime == null)
                            //{
                            //    this.jobTracker[pawn].ignoreGiversInFirstTime = new HashSet<WorkGiverDef>();
                            //}
                            //this.jobTracker[pawn].ignoreGiversInFirstTime.Add(job.workGiverDef);
                        }
                    }
                }
                else
                {
                    ZLogger.Message("Resetting jobs for " + pawn);
                    this.ResetJobs(pawn);
                }
                return true;
            }
            catch (Exception ex)
            {
                ZLogger.Error("Fail in TryTakeFirstJob: " + ex);
                ZLogger.Pause("Error in TryTakeFirstJob, job: " + job);
            }
            return false;
        }

        public void ZLevelsFixer(int tile)
        {
            try
            {
                if (this.ZLevelsTracker.ContainsKey(tile) && this.ZLevelsTracker[tile].ZLevels != null
                    && this.ZLevelsTracker[tile].ZLevels[0] != null
                    && this.ZLevelsTracker[tile].ZLevels[0].listerThings == null)
                {
                    var map = Find.WorldObjects.MapParents.Where(x => x.Tile == tile
                    && x.HasMap && x.Map != null && x.Map.IsPlayerHome).FirstOrDefault().Map;
                    this.ZLevelsTracker[tile].ZLevels[0] = map;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error in ZLevelsFixer: " + ex);
            }
        }

        public void SpawnStairsUpper(Pawn pawnToTeleport, IntVec3 cellToTeleport, Map mapToTeleport)
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

        public void SpawnStairsBelow(Pawn pawn, IntVec3 cell, Map map)
        {
            if (this.GetZIndexFor(pawn.Map) > this.GetZIndexFor(map))
            {
                var stairs = pawn.Map.thingGrid.ThingsListAt(cell)?
                    .Where(x => x is Building_StairsDown)?.FirstOrDefault();
                ZLogger.Message("Stairs: " + stairs);
                if (stairs != null)
                {
                    if (stairs.Stuff != null)
                    {
                        var thingToMake = ZLevelsDefOf.ZL_StairsUp;
                        if (cell.GetThingList(map).Where(x => x.def == thingToMake).Count() == 0)
                        {
                            var newStairs = ThingMaker.MakeThing(thingToMake, stairs.Stuff);
                            newStairs.SetFaction(stairs.Faction);
                            GenPlace.TryPlaceThing(newStairs, cell, map, ThingPlaceMode.Direct);
                        }
                    }
                    else if (stairs.def.defName == ZLevelsDefOf.ZL_NaturalHole.defName)
                    {
                        foreach (var thing in pawn.Map.listerThings.AllThings)
                        {
                            if (thing is Building_StairsDown naturalHole && naturalHole.def.defName == ZLevelsDefOf.ZL_NaturalHole.defName)
                            {
                                var infestatorsPlace = IntVec3.Invalid;
                                Thing infestator = null;
                                if (naturalHole?.infestationData?.infestators != null)
                                {
                                    Predicate<Thing> validator = delegate (Thing t)
                                    {
                                        return naturalHole.infestationData.infestators.Contains(((Pawn)t).kindDef);
                                    };
                                    infestator = GenClosest.ClosestThing_Global(naturalHole.Position,
                                        map.mapPawns.AllPawns, 99999f, validator);
                                }
                                if (infestator != null)
                                {
                                    infestatorsPlace = infestator.Position;
                                    var tunnel = map.pathFinder.FindPath
                                        (naturalHole.Position, infestator, TraverseParms.For
                                        (TraverseMode.PassAllDestroyableThings, Danger.Deadly),
                                        PathEndMode.OnCell);
                                    if (tunnel?.NodesReversed != null && tunnel.NodesReversed.Count > 0)
                                    {
                                        foreach (var tile in tunnel.NodesReversed)
                                        {
                                            var building = tile.GetFirstBuilding(map);
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

        public void DestroyMineableBelow(Map map, IntVec3 cell)
        {
            if (map.thingGrid.ThingsListAt(cell).Any())
            {
                for (int i = map.thingGrid.ThingsListAt(cell).Count - 1; i >= 0; i--)
                {
                    Thing thing = map.thingGrid.ThingsListAt(cell)[i];
                    if (thing is Mineable)
                    {
                        if (thing.Spawned)
                        {
                            thing.DeSpawn(DestroyMode.WillReplace);
                        }
                    }
                }
            }
        }

        public void MoveThingToAnotherMap(Thing thingToTeleport, Map mapToTeleport)
        {
            //ZLogger.Message("CHECK: trying to teleport " + thingToTeleport + " from " + thingToTeleport.Map + " to " + mapToTeleport, true);
            thingToTeleport.DeSpawn(DestroyMode.Vanish);
            GenSpawn.Spawn(thingToTeleport, thingToTeleport.positionInt, mapToTeleport, WipeMode.Vanish);
        }

        public void MovePawnToAnotherMap(Pawn pawnToTeleport, Map mapToTeleport)
        {
            ZLogger.Message("CHECK: trying to teleport " + pawnToTeleport + " from " + pawnToTeleport.Map + " to " + mapToTeleport, true);
            if (pawnToTeleport.Map == mapToTeleport) return;
            if (mapToTeleport.spawnedThings.Contains(pawnToTeleport)) return;
            if (mapToTeleport.listerThings.Contains(pawnToTeleport)) return;
            if (pawnToTeleport is Pawn pawn && mapToTeleport.mapPawns.AllPawns.Contains(pawn)) return;
            
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
            pawnToTeleport.Map?.mapPawns.DeRegisterPawn((Pawn)pawnToTeleport);
            
            pawnToTeleport.mapIndexOrState = (sbyte)Find.Maps.IndexOf(mapToTeleport);
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
        }


        public bool ShouldCameraJump(Thing thing)
        {
            if (Find.Selector.SelectedObjects.Contains(thing))
                return true;
            return false;
        }

        public void DoCameraJumpToThing(Thing thing)
        {
            var rememberedCamera = Current.Game.CurrentMap.rememberedCameraPos;
            Current.Game.CurrentMap = thing.Map;
            Find.CameraDriver.SetRootPosAndSize(rememberedCamera.rootPos, rememberedCamera.rootSize);
            CameraJumper.TryJumpAndSelect(thing);
        }

        public bool ShouldRemainDrafted(Thing thing)
        {
            if (thing is Pawn pawn && pawn.Drafted)
                return true;
            return false;
        }

        public void TeleportThing(Thing thingToTeleport, IntVec3 cellToTeleport, Map mapToTeleport, bool firstTime = false, int dealDamage = 0)
        {
            bool jump = this.ShouldCameraJump(thingToTeleport);
            bool draft = this.ShouldRemainDrafted(thingToTeleport);

            if (thingToTeleport is Pawn pawnToTeleport)
            {
                this.SaveArea(pawnToTeleport);
            }

            MoveThingToAnotherMap(thingToTeleport, mapToTeleport);
            if (jump)
            {
                this.DoCameraJumpToThing(thingToTeleport);
            }
            if (thingToTeleport is Pawn pawnToTeleport2)
            {
                this.LoadArea(pawnToTeleport2);
                if (draft && pawnToTeleport2.drafter != null)
                {
                    pawnToTeleport2.drafter.Drafted = true;
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
            mapToTeleport.fogGrid.FloodUnfogAdjacent(thingToTeleport.PositionHeld);
        }

        public void TeleportPawn(Pawn pawnToTeleport, IntVec3 cellToTeleport, Map mapToTeleport, bool firstTime = false, bool spawnStairsBelow = false, bool spawnStairsUpper = false)
        {
            bool jump = this.ShouldCameraJump(pawnToTeleport);
            bool draft = this.ShouldRemainDrafted(pawnToTeleport);

            DestroyMineableBelow(mapToTeleport, cellToTeleport);

            if (spawnStairsUpper)
            {
                this.SpawnStairsUpper(pawnToTeleport, cellToTeleport, mapToTeleport);
            }
            if (spawnStairsBelow)
            {
                this.SpawnStairsBelow(pawnToTeleport, cellToTeleport, mapToTeleport);
            }

            this.SaveArea(pawnToTeleport);

            this.MovePawnToAnotherMap(pawnToTeleport, mapToTeleport);

            this.LoadArea(pawnToTeleport);

            ZLogger.Message("Pawn: " + pawnToTeleport + " teleported to " + this.GetMapInfo(mapToTeleport));

            if (jump)
            {
                this.DoCameraJumpToThing(pawnToTeleport);
            }

            if (draft && pawnToTeleport.drafter != null)
            {
                pawnToTeleport.drafter.Drafted = true;
            }

            if (firstTime)
            {
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

            FloodFillerFog.FloodUnfog(pawnToTeleport.Position, mapToTeleport);
            mapToTeleport.fogGrid.FloodUnfogAdjacent(pawnToTeleport.PositionHeld);
            this.ReCheckStairs();
        }

        public Map CreateLowerLevel(Map origin, IntVec3 playerStartSpot)
        {
            var comp = ZUtils.GetMapComponentZLevel(origin);
            var mapParent = (MapParent_ZLevel)WorldObjectMaker.MakeWorldObject(ZLevelsDefOf.ZL_Underground);

            mapParent.Tile = origin.Tile;
            mapParent.PlayerStartSpot = playerStartSpot;
            mapParent.TotalInfestations = comp.TotalInfestations;
            mapParent.hasCaves = comp.hasCavesBelow.GetValueOrDefault(false);
            Find.WorldObjects.Add(mapParent);

            string seedString = Find.World.info.seedString;
            Find.World.info.seedString = new System.Random().Next(0, 2147483646).ToString();
            Map newMap = null;
            mapParent.Z_LevelIndex = comp.Z_LevelIndex - 1;
            mapParent.IsUnderground = true;
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
            ZUtils.ZTracker.mapIndex[newMap] = mapParent.Z_LevelIndex;

            Find.World.info.seedString = seedString;
            try
            {
                if (this.TryRegisterMap(newMap, comp.Z_LevelIndex - 1))
                {
                    var newComp = ZUtils.GetMapComponentZLevel(newMap);
                    newComp.Z_LevelIndex = comp.Z_LevelIndex - 1;
                    ZUtils.ZTracker.mapIndex[newMap] = newComp.Z_LevelIndex;
                }
                GameCondition_NoSunlight gameCondition_NoSunlight =
                    (GameCondition_NoSunlight)GameConditionMaker.MakeCondition(ZLevelsDefOf.ZL_UndergroundCondition, -1);
                gameCondition_NoSunlight.Permanent = true;
                newMap.gameConditionManager.RegisterCondition(gameCondition_NoSunlight);
                AdjustLowerMapGeneration(newMap);

            }
            catch
            {

            }
            return newMap;
        }


        public Map CreateUpperLevel(Map origin, IntVec3 playerStartSpot)
        {
            var mapParent = (MapParent_ZLevel)WorldObjectMaker.MakeWorldObject(ZLevelsDefOf.ZL_Upper);

            var comp = ZUtils.GetMapComponentZLevel(origin);

            mapParent.Tile = origin.Tile;
            mapParent.PlayerStartSpot = playerStartSpot;
            mapParent.hasCaves = false;
            Find.WorldObjects.Add(mapParent);

            string seedString = Find.World.info.seedString;
            Find.World.info.seedString = new System.Random().Next(0, 2147483646).ToString();
            Map newMap = null;
            mapParent.Z_LevelIndex = comp.Z_LevelIndex + 1;
            mapParent.IsUpperLevel = true;
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
            ZUtils.ZTracker.mapIndex[newMap] = mapParent.Z_LevelIndex;
            Find.World.info.seedString = seedString;
            try
            {
                if (this.TryRegisterMap(newMap, comp.Z_LevelIndex + 1))
                {
                    var newComp = ZUtils.GetMapComponentZLevel(newMap);
                    newComp.Z_LevelIndex = comp.Z_LevelIndex + 1;
                    ZUtils.ZTracker.mapIndex[newMap] = newComp.Z_LevelIndex;
                    AdjustUpperMapGeneration(newMap);
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

        public void AdjustLowerMapGeneration(Map map)
        {
            try
            {
                Map mapUpper = this.GetUpperLevel(map.Tile, map);
                foreach (var geyser in mapUpper.listerThings.AllThings.Where(x => x is Building_SteamGeyser))
                {
                    var newGeyser = ThingMaker.MakeThing(ThingDefOf.SteamGeyser);
                    GenSpawn.Spawn(newGeyser, geyser.Position, map, WipeMode.Vanish);
                    foreach (var pos in GenAdj.OccupiedRect(newGeyser))
                    {
                        pos.GetEdifice(map).Destroy(DestroyMode.Vanish);
                    }
                }
            }
            catch { };
        }
        public void AdjustUpperMapGeneration(Map map)
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

        public void ReCheckStairs()
        {
            try
            {
                foreach (var tile in this.ZLevelsTracker)
                {
                    foreach (var map in this.GetAllMaps(tile.Key))
                    {
                        this.stairsDown[map] = this.totalStairsDown.Where(x => x.Map == map).ToList();
                        this.stairsUp[map] = this.totalStairsUp.Where(x => x.Map == map).ToList();
                        if (this.stairsDown[map].Count == 0 && this.GetLowerLevel(tile.Key, map) != null)
                        {
                            this.stairsDown[map] = map.listerThings.AllThings.Where(x => x is Building_StairsDown).Cast<Building_Stairs>().ToList();
                            this.totalStairsDown.AddRange(this.stairsDown[map]);
                        }
                        if (this.stairsUp[map].Count == 0 && this.GetUpperLevel(tile.Key, map) != null)
                        {
                            this.stairsUp[map] = map.listerThings.AllThings.Where(x => x is Building_StairsUp).Cast<Building_Stairs>().ToList();
                            this.totalStairsUp.AddRange(this.stairsUp[map]);
                        }
                        if (this.stairsDown.ContainsKey(map))
                        {
                            for (int i = this.stairsDown[map].Count - 1; i >= 0; i--)
                            {
                                if (!this.stairsDown[map][i].Position.Walkable(map))
                                {
                                    ZLogger.Message(this.stairsDown[map][i] + " not walkable, removing it");
                                    this.stairsDown[map].RemoveAt(i);
                                }
                            }
                        }
                        if (this.stairsUp.ContainsKey(map))
                        {
                            for (int i = this.stairsUp[map].Count - 1; i >= 0; i--)
                            {

                                if (!this.stairsUp[map][i].Position.Walkable(map))
                                {
                                    ZLogger.Message(this.stairsUp[map][i] + " not walkable, removing it");
                                    this.stairsUp[map].RemoveAt(i);
                                }
                            }
                        }
                    }
                    foreach (var map in this.GetAllMaps(tile.Key))
                    {
                        if (this.stairsDown.ContainsKey(map))
                        {
                            for (int i = this.stairsDown[map].Count - 1; i >= 0; i--)
                            {
                                Map lowerMap = this.GetLowerLevel(tile.Key, map);
                                if (lowerMap != null && this.stairsUp?[lowerMap]?.Where(x => x.Position
                                    == this.stairsDown[map][i].Position).Count() == 0)
                                {
                                    ZLogger.Message(this.stairsDown[map][i] + " - has no stairs upper, removing it");
                                    this.stairsDown[map].RemoveAt(i);
                                }
                            }
                        }
                        if (this.stairsUp.ContainsKey(map))
                        {
                            for (int i = this.stairsUp[map].Count - 1; i >= 0; i--)
                            {
                                Map upperMap = this.GetUpperLevel(tile.Key, map);
                                if (upperMap != null && this.stairsDown?[upperMap]?.Where(x => x.Position
                                    == this.stairsUp[map][i].Position).Count() == 0)
                                {
                                    ZLogger.Message(this.stairsUp[map][i] + " - has no stairs below, removing it");
                                    this.stairsUp[map].RemoveAt(i);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error in ZLevels in ReCheckStairs: " + ex);
            }
        }

        public override void StartedNewGame()
        {
            base.StartedNewGame();
            ZUtils.ResetZTracker();
            foreach (var map in Find.Maps)
            {
                if (map.IsPlayerHome)
                {
                    TryRegisterMap(map, 0);
                }
            }
            this.PreInit();
        }
        public override void LoadedGame()
        {
            base.LoadedGame();
            this.ReCheckStairs();
            ZUtils.ResetZTracker();
            this.PreInit();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look<Pawn, ActiveArea>(ref this.activeAreas, "activeAreas", LookMode.Reference,
                LookMode.Deep, ref this.ActiveAreasKeys, ref this.ActiveAreasValues);
            Scribe_Collections.Look<Pawn, JobTracker>(ref this.jobTracker, "jobTracker", LookMode.Reference,
                LookMode.Deep, ref this.JobTrackerKeys, ref this.JobTrackerValues);
            Scribe_Collections.Look<Map, int>(ref this.mapIndex, "mapIndex", LookMode.Reference,
                LookMode.Value, ref this.mapKeys, ref this.mapValues);
            Scribe_Collections.Look<int, ZLevelData>(ref this.ZLevelsTracker, "ZLevelsTracker",
                LookMode.Value, LookMode.Deep, ref this.Z_LevelsKeys, ref this.ZLevelsTrackerValues);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                try
                {
                    ZLogger.Message("START");
                    if (this.mapIndex != null && this.mapIndex.Count > 0)
                    {
                        if (this.ZLevelsTracker == null)
                        {
                            this.ZLevelsTracker = new Dictionary<int, ZLevelData>();
                            foreach (var d in this.mapIndex)
                            {
                                if (d.Key != null)
                                {
                                    ZLogger.Message("1 Registering map: " + d.Key + " - " + d.Value);
                                    this.TryRegisterMap(d.Key, d.Value);
                                }
                            }
                        }
                        else
                        {
                            foreach (var d in this.mapIndex)
                            {
                                if (d.Key != null)
                                {
                                    ZLogger.Message("2 Registering map: " + d.Key + " - " + d.Value);
                                    this.TryRegisterMap(d.Key, d.Value);
                                }

                            }
                        }
                    }
                    ZLogger.Message("END");
                }
                catch (Exception ex)
                {
                    Log.Error("Z-Levels produced save reloading error: " + ex);
                }
            }
        }

        public Dictionary<Pawn, ActiveArea> activeAreas;
        private List<Pawn> ActiveAreasKeys = new List<Pawn>();
        private List<ActiveArea> ActiveAreasValues = new List<ActiveArea>();

        public Dictionary<Pawn, JobTracker> jobTracker;
        private List<Pawn> JobTrackerKeys = new List<Pawn>();
        private List<JobTracker> JobTrackerValues = new List<JobTracker>();

        public Dictionary<int, ZLevelData> ZLevelsTracker = new Dictionary<int, ZLevelData>();
        private List<int> Z_LevelsKeys = new List<int>();
        private List<ZLevelData> ZLevelsTrackerValues = new List<ZLevelData>();

        public Dictionary<Map, List<Building_Stairs>> stairsUp = new Dictionary<Map, List<Building_Stairs>>();
        public Dictionary<Map, List<Building_Stairs>> stairsDown = new Dictionary<Map, List<Building_Stairs>>();

        public HashSet<Building_Stairs> totalStairsDown = new HashSet<Building_Stairs>();
        public HashSet<Building_Stairs> totalStairsUp = new HashSet<Building_Stairs>();

        public Dictionary<Map, int> mapIndex;
        private List<Map> mapKeys = new List<Map>();
        private List<int> mapValues = new List<int>();
    }
}