using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace ZLevels
{
    public class Building_StairsDown : Building
    {
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                Map mapBelow = ZTracker.GetLowerLevel(this.Map.Tile, this.Map);
                if (mapBelow != null && this.def.defName == "FC_StairsDown")
                {
                    for (int i = mapBelow.thingGrid.ThingsListAt(this.Position).Count - 1; i >= 0; i--)
                    {
                        Thing thing = mapBelow.thingGrid.ThingsListAt(this.Position)[i];
                        if (thing is Mineable)
                        {
                            if (thing.Spawned)
                            {
                                thing.DeSpawn(DestroyMode.WillReplace);
                            }
                        }
                    }
                    if (this.Position.GetThingList(mapBelow).Where(x => x.def == ZLevelsDefOf.ZL_StairsUp).Count() == 0)
                    {
                        var stairsToSpawn = ThingMaker.MakeThing(ZLevelsDefOf.ZL_StairsUp, this.Stuff);
                        GenPlace.TryPlaceThing(stairsToSpawn, this.Position, mapBelow, ThingPlaceMode.Direct);
                        stairsToSpawn.SetFaction(this.Faction);
                    }
                    FloodFillerFog.FloodUnfog(this.Position, mapBelow);
                    AccessTools.Method(typeof(FogGrid), "FloodUnfogAdjacent").Invoke(mapBelow.fogGrid, new object[]
                    { this.Position });
                }
            }
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            var text = "GoDown".Translate();
            foreach (var opt in base.GetFloatMenuOptions(selPawn))
            {
                if (opt.Label != text)
                {
                    yield return opt;
                }
            }
            var opt2 = new FloatMenuOption(text, () =>
            {
                Log.Message("Test");
                Job job = JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToStairs, this);
                selPawn.jobs.StartJob(job, JobCondition.InterruptForced);
            }, MenuOptionPriority.Default, null, this);
            yield return opt2;
        }

        public override void Tick()
        {
            base.Tick();
            if (Find.TickManager.TicksGame % 60 == 0)
            {
                foreach (var dir in GenRadial.RadialCellsAround(this.Position, 20, true))
                {
                    foreach (var t in dir.GetThingList(this.Map))
                    {
                        if (t is Pawn pawn &&
                            pawn.HostileTo(this.Faction) && !pawn.mindState.MeleeThreatStillThreat
                            && GenSight.LineOfSight(this.Position, pawn.Position, this.Map))
                        {
                            if (this.visitedPawns == null) this.visitedPawns = new HashSet<string>();
                            var ZTracker = Current.Game.GetComponent<ZLevelsManager>();

                            if (!this.visitedPawns.Contains(pawn.ThingID))
                            {
                                Job goToStairs = JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToStairs, this);
                                pawn.jobs.jobQueue.EnqueueFirst(goToStairs);
                                this.visitedPawns.Add(pawn.ThingID);
                            }
                            else if (ZTracker.GetLowerLevel(this.Map.Tile, this.Map) != null && ZTracker.GetLowerLevel(this.Map.Tile, this.Map).mapPawns.AllPawnsSpawned.Where(x => pawn.HostileTo(x)).Any())
                            {
                                Job goToStairs = JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToStairs, this);
                                pawn.jobs.jobQueue.EnqueueFirst(goToStairs);
                            }
                            else if (ZTracker.GetZIndexFor(this.Map) != 0)
                            {
                                Job goToStairs = JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToStairs, this);
                                pawn.jobs.jobQueue.EnqueueFirst(goToStairs);
                            }
                        }
                    }
                }
            }
        }

        public Map Create(Map origin)
        {
            var comp = origin.GetComponent<MapComponentZLevel>();
            var mapParent = (MapParent_ZLevel)WorldObjectMaker.MakeWorldObject(ZLevelsDefOf.ZL_Underground);

            mapParent.Tile = origin.Tile;
            mapParent.PlayerStartSpot = this.Position;
            mapParent.TotalInfestations = comp.TotalInfestations;
            mapParent.hasCaves = comp.hasCavesBelow.GetValueOrDefault(false);
            Find.WorldObjects.Add(mapParent);

            string seedString = Find.World.info.seedString;
            Find.World.info.seedString = new System.Random().Next(0, 2147483646).ToString();

            var pathToLoad = Path.Combine(Path.Combine(GenFilePaths.ConfigFolderPath,
                    "SavedMaps"), origin.Tile + " - " + (comp.Z_LevelIndex - 1) + ".xml");
            FileInfo fileInfo = new FileInfo(pathToLoad);
            Map newMap = null;
            if (fileInfo.Exists)
            {
                Log.Message("Loading from " + pathToLoad);
                newMap = MapGenerator.GenerateMap(origin.Size, mapParent, 
                    ZLevelsDefOf.ZL_EmptyMap, mapParent.ExtraGenStepDefs, null);
                BlueprintUtility.LoadEverything(newMap, pathToLoad);
            }
            else
            {
                newMap = MapGenerator.GenerateMap(origin.Size, mapParent, mapParent.MapGeneratorDef, mapParent.ExtraGenStepDefs, null);
            }

            Find.World.info.seedString = seedString;
            var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
            if (ZTracker.TryRegisterMap(newMap, comp.Z_LevelIndex - 1))
            {
                var newComp = newMap.GetComponent<MapComponentZLevel>();
                newComp.Z_LevelIndex = comp.Z_LevelIndex - 1;
            }
            GameCondition_NoSunlight gameCondition_NoSunlight = 
                (GameCondition_NoSunlight)GameConditionMaker.MakeCondition(ZLevelsDefOf.ZL_UndergroundCondition, -1);
            gameCondition_NoSunlight.Permanent = true;
            newMap.gameConditionManager.RegisterCondition(gameCondition_NoSunlight);
            return newMap;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.shouldSpawnStairsBelow, "shouldSpawnStairsBelow", true);
            Scribe_Values.Look<bool>(ref this.visited, "visited", false);
            Scribe_Values.Look<string>(ref this.pathToPreset, "pathToPreset", null);
            Scribe_Deep.Look<InfestationData>(ref this.infestationData, "infestationData", null);
            Scribe_Collections.Look<string>(ref this.visitedPawns, "visitedPawns");
        }

        public HashSet<String> visitedPawns = new HashSet<string>();
        public string pathToPreset = "";
        public bool shouldSpawnStairsBelow = true;
        public InfestationData infestationData;
        public bool visited = false;
    }
}

