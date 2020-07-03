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
    public class Building_StairsUp : Building, IAttackTarget
    {
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
            if (!ZTracker.stairsUp.ContainsKey(this.Map))
            {
                ZTracker.stairsUp[this.Map] = new List<Thing>();
            }
            if (!ZTracker.stairsUp[this.Map].Contains(this))
            {
                ZTracker.stairsUp[this.Map].Add(this);
            }
            ZLogger.Message("Spawning " + this);

            if (!respawningAfterLoad)
            {
                if (this.Position.GetTerrain(this.Map) == ZLevelsDefOf.ZL_OutsideTerrain)
                {
                    this.Map.terrainGrid.SetTerrain(this.Position, ZLevelsDefOf.ZL_OutsideTerrainTwo);
                }
                Map mapUpper = ZTracker.GetUpperLevel(this.Map.Tile, this.Map);
                if (mapUpper != null && mapUpper != this.Map)
                {
                    if (this.Position.GetThingList(mapUpper).Where(x => x.def == ZLevelsDefOf.ZL_StairsDown).Count() == 0)
                    {
                        mapUpper.terrainGrid.SetTerrain(this.Position, ZLevelsDefOf.ZL_OutsideTerrainTwo);
                        var stairsToSpawn = ThingMaker.MakeThing(ZLevelsDefOf.ZL_StairsDown, this.Stuff);
                        GenPlace.TryPlaceThing(stairsToSpawn, this.Position, mapUpper, ThingPlaceMode.Direct);
                        stairsToSpawn.SetFaction(this.Faction);
                    }
                }
                else if (mapUpper == this.Map)
                {
                    Log.Error("There was a mismatch of ZLevels indices. This is a serious error, report it to the mod developers");
                    foreach (var map2 in ZTracker.GetAllMaps(this.Map.Tile))
                    {
                        ZLogger.Message("Index: " + ZTracker.GetMapInfo(map2));
                    }
                }
            }
            ZTracker.totalStairsUp.Add(this);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
            if (ZTracker.stairsUp[this.Map].Contains(this))
            {
                ZTracker.stairsUp[this.Map].Remove(this);
            }
            if (this.Position.GetTerrain(this.Map) == ZLevelsDefOf.ZL_OutsideTerrainTwo)
            {
                this.Map.terrainGrid.SetTerrain(this.Position, ZLevelsDefOf.ZL_OutsideTerrain);
            }
            base.Destroy(mode);
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
                            var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                            
                            if (this.visitedPawns == null) this.visitedPawns = new HashSet<string>();
                            if (!this.visitedPawns.Contains(pawn.ThingID))
                            {
                                Job goToStairs = JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToStairs, this);
                                pawn.jobs.jobQueue.EnqueueFirst(goToStairs);
                                this.visitedPawns.Add(pawn.ThingID);
                            }
                            else if (ZTracker.GetLowerLevel(this.Map.Tile, this.Map) != null &&
                                ZTracker.GetLowerLevel(this.Map.Tile, this.Map).mapPawns.AllPawnsSpawned.Where(x => pawn.HostileTo(x)).Any())
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

        public void GiveJob(Pawn pawn, Thing stairs)
        {
            Job job = JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToStairs, stairs);
            pawn.jobs.StartJob(job, JobCondition.InterruptForced);
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            var text = "GoUP".Translate();
            foreach (var opt in base.GetFloatMenuOptions(selPawn))
            {
                if (opt.Label != text)
                {
                    yield return opt;
                }
            }
            var opt2 = new FloatMenuOption(text, () => {
                GiveJob(selPawn, this);
                }, MenuOptionPriority.Default, null, this);
            yield return opt2;

        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref shouldSpawnStairsUpper, "shouldSpawnStairsUpper");
            Scribe_Values.Look<string>(ref this.pathToPreset, "pathToPreset");
            Scribe_Collections.Look<string>(ref this.visitedPawns, "visitedPawns");
        }

        public HashSet<String> visitedPawns = new HashSet<string>();

        public string pathToPreset = "";
        public bool shouldSpawnStairsUpper = true;

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

