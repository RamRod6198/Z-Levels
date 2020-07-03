using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HarmonyLib;
using Multiplayer.API;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace ZLevels
{
    public class Building_StairsDown : Building, IAttackTarget
    {
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
            if (!ZTracker.stairsDown.ContainsKey(this.Map))
            {
                ZTracker.stairsDown[this.Map] = new List<Thing>();
            }
            if (!ZTracker.stairsDown[this.Map].Contains(this))
            {
                ZTracker.stairsDown[this.Map].Add(this);
            }
            ZLogger.Message("Spawning " + this);
            if (!respawningAfterLoad)
            {
                if (this.Position.GetTerrain(this.Map) == ZLevelsDefOf.ZL_OutsideTerrain)
                {
                    this.Map.terrainGrid.SetTerrain(this.Position, ZLevelsDefOf.ZL_OutsideTerrainTwo);
                }
                Map mapBelow = ZTracker.GetLowerLevel(this.Map.Tile, this.Map);
                if (mapBelow != null && mapBelow != this.Map && this.def.defName == "FC_StairsDown")
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
                        mapBelow.terrainGrid.SetTerrain(this.Position, ZLevelsDefOf.ZL_OutsideTerrainTwo);
                        GenPlace.TryPlaceThing(stairsToSpawn, this.Position, mapBelow, ThingPlaceMode.Direct);
                        stairsToSpawn.SetFaction(this.Faction);
                    }
                    FloodFillerFog.FloodUnfog(this.Position, mapBelow);
                    AccessTools.Method(typeof(FogGrid), "FloodUnfogAdjacent").Invoke(mapBelow.fogGrid, new object[]
                    { this.Position });
                }
                else if (mapBelow == this.Map)
                {
                    Log.Error("There was a mismatch of ZLevels indices. This is a serious error, report it to the mod developers");
                    foreach (var map2 in ZTracker.GetAllMaps(this.Map.Tile))
                    {
                        ZLogger.Message("Index: " + ZTracker.GetMapInfo(map2));
                    }
                }
            }
            ZTracker.totalStairsDown.Add(this);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
            if (ZTracker.stairsDown[this.Map].Contains(this))
            {
                ZTracker.stairsDown[this.Map].Remove(this);
            }
            if (this.Position.GetTerrain(this.Map) == ZLevelsDefOf.ZL_OutsideTerrainTwo)
            {
                this.Map.terrainGrid.SetTerrain(this.Position, ZLevelsDefOf.ZL_OutsideTerrain);
            }
            base.Destroy(mode);
        }

        public void GiveJob(Pawn pawn, Thing stairs)
        {
            Job job = JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToStairs, stairs);
            pawn.jobs.StartJob(job, JobCondition.InterruptForced);
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
                GiveJob(selPawn, this);
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

        Thing IAttackTarget.Thing
        {
            get
            {
                return this;
            }
        }
        public LocalTargetInfo TargetCurrentlyAimingAt
        {
            get
            {
                return LocalTargetInfo.Invalid;
            }
        }

        public float TargetPriorityFactor
        {
            get
            {
                return 0f;
            }
        }

        public bool ThreatDisabled(IAttackTargetSearcher disabledFor)
        {
            return true;
        }
    }
}

