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
using ZLevels.Properties;

namespace ZLevels
{
    public class Building_Stairs : Building
    {

        //public class DestroyedEventArgs : EventArgs
        //{
        //    public IntVec3 Location;
        //    public Map Map;
        //}


        public virtual Building_Stairs GetMatchingStair()
        {
            return null;
        }

        //public delegate void DestroyedEvent(object sender, DestroyedEventArgs args);

        //public event DestroyedEvent OnDestroyed;

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            //ZLogger.Message($"{this.GetType()} is being destroyed, invoking handler {OnDestroyed != null} ");
            //OnDestroyed?.Invoke(this, new DestroyedEventArgs() { Map = Map, Location = Position });
            ZPathfinder.Instance.getDijkstraGraphForTile(Map.Tile).RemoveNodeAt(Map, Position);
            base.Destroy(mode);
        }

        public bool syncDamage = true;


        public override void PostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            if (syncDamage)
            {
                Building_Stairs matchingStair = GetMatchingStair();
                if (matchingStair != null)
                {
                    ZLogger.Message(matchingStair + ".HitPoints -= " + (int)totalDamageDealt, true);
                    matchingStair.syncDamage = false;
                    matchingStair.TakeDamage(new DamageInfo(dinfo.Def, dinfo.Amount));
                    matchingStair.syncDamage = true;
                }
            }
            base.PostApplyDamage(dinfo, totalDamageDealt);
        }

        //public override void SpawnSetup(Map map, bool respawningAfterLoad)
        //{
        //    base.SpawnSetup(map, respawningAfterLoad);
        //    RegionLink regionLink = new RegionLink();
        //    regionLink.RegionA = map.regionGrid.GetValidRegionAt(Position);
        //    Building_Stairs match = GetMatchingStair();

        //    StairRegion = Region.MakeNewUnfilled(Position, map);
        //    StairRegion.extentsLimit.minX = StairRegion.extentsLimit.maxX = Position.x;
        //    StairRegion.extentsLimit.minZ = StairRegion.extentsLimit.maxZ = Position.z;
        //    StairRegion.type = RegionType.Portal;
        //    if (match != null)
        //    {
        //        regionLink.RegionB = match.Map.regionGrid.GetValidRegionAt(Position);
        //    }
        //    StairRegion.links.Add(regionLink);
        //    map.regionGrid.SetRegionAt(Position, StairRegion);
        //}
    }

    public class Building_StairsUp : Building_Stairs, IAttackTarget
    {
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            var ZTracker = ZUtils.ZTracker;
            if (ZTracker.totalStairsUp == null) ZTracker.totalStairsUp = new HashSet<Building_Stairs>();
            ZTracker.totalStairsUp.Add(this);

            if (!ZTracker.stairsUp.ContainsKey(this.Map))
            {
                ZTracker.stairsUp[this.Map] = new List<Building_Stairs>();
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
                    if (Position.GetThingList(mapUpper).Count(x => x.def == ZLevelsDefOf.ZL_StairsDown) != 0) return;
                    mapUpper.terrainGrid.SetTerrain(this.Position, ZLevelsDefOf.ZL_OutsideTerrainTwo);
                    var stairsToSpawn = ThingMaker.MakeThing(ZLevelsDefOf.ZL_StairsDown, this.Stuff);
                    GenPlace.TryPlaceThing(stairsToSpawn, this.Position, mapUpper, ThingPlaceMode.Direct);
                    stairsToSpawn.SetFaction(this.Faction);
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
        }

        public override Building_Stairs GetMatchingStair()
        {
            Map lowerMap = ZUtils.ZTracker.GetLowerLevel(this.Map.Tile, this.Map);
            if (lowerMap != null)
            {
                return (Building_StairsDown)Position.GetThingList(lowerMap).FirstOrFallback(x => x is Building_StairsDown);
            }
            return null;
        }
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            var ZTracker = ZUtils.ZTracker;
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
            var opt2 = new FloatMenuOption(text, () =>
            {
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

