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

    public class Building_Stairs : Building
    {
        public Region StairRegion;
        public Building_Stairs GetMatchingStair()
        {
            if (this is Building_StairsUp)
            {
                return (Building_Stairs)Position.GetThingList(ZUtils.ZTracker.GetUpperLevel(Map.Tile, Map))
                    .FirstOrDefault(x => x is Building_StairsDown && x.Position == Position);
            }
            else
            {
                return (Building_Stairs)Position.GetThingList(ZUtils.ZTracker.GetLowerLevel(Map.Tile, Map))
                    .FirstOrDefault(x => x is Building_StairsUp && x.Position == Position);
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            RegionLink regionLink = new RegionLink();
            regionLink.RegionA = map.regionGrid.GetValidRegionAt(Position);
            Building_Stairs match = GetMatchingStair();

            StairRegion = Region.MakeNewUnfilled(Position, map);
            StairRegion.extentsLimit.minX = StairRegion.extentsLimit.maxX = Position.x;
            StairRegion.extentsLimit.minZ = StairRegion.extentsLimit.maxZ = Position.z;
            StairRegion.type = RegionType.Portal;
            if (match != null)
            {
                regionLink.RegionB = match.Map.regionGrid.GetValidRegionAt(Position);
            }
            StairRegion.links.Add(regionLink);
            map.regionGrid.SetRegionAt(Position, StairRegion);
        }
    }


    public class Building_StairsUp : Building_Stairs, IAttackTarget
    {
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            var ZTracker = ZUtils.ZTracker;
            if (ZTracker.totalStairsUp == null) ZTracker.totalStairsUp = new HashSet<Building_Stairs>();
            ZTracker.totalStairsUp.Add(this);

            if (!ZTracker.stairsUp.ContainsKey(Map))
            {
                ZTracker.stairsUp[Map] = new List<Building_Stairs>();
            }
            if (!ZTracker.stairsUp[Map].Contains(this))
            {
                ZTracker.stairsUp[Map].Add(this);
            }
            ZLogger.Message("Spawning " + this);
            if (!respawningAfterLoad)
            {
                if (Position.GetTerrain(Map) == ZLevelsDefOf.ZL_OutsideTerrain)
                {
                    Map.terrainGrid.SetTerrain(Position, ZLevelsDefOf.ZL_OutsideTerrainTwo);
                }
                Map mapUpper = ZTracker.GetUpperLevel(Map.Tile, Map);
                if (mapUpper != null && mapUpper != Map)
                {
                    if (Position.GetThingList(mapUpper).Count(x => x.def == ZLevelsDefOf.ZL_StairsDown) == 0)
                    {
                        mapUpper.terrainGrid.SetTerrain(Position, ZLevelsDefOf.ZL_OutsideTerrainTwo);
                        var stairsToSpawn = ThingMaker.MakeThing(ZLevelsDefOf.ZL_StairsDown, Stuff);
                        GenPlace.TryPlaceThing(stairsToSpawn, Position, mapUpper, ThingPlaceMode.Direct);
                        stairsToSpawn.SetFaction(Faction);
                    }
                }
                else if (mapUpper == Map)
                {
                    Log.Error("There was a mismatch of ZLevels indices. This is a serious error, report it to the mod developers");
                    foreach (var map2 in ZTracker.GetAllMaps(Map.Tile))
                    {
                        ZLogger.Message("Index: " + ZTracker.GetMapInfo(map2));
                    }
                }
            }
        }

        public new Building_StairsDown GetMatchingStair
        {
            get
            {
                Map lowerMap = ZUtils.ZTracker.GetLowerLevel(Map.Tile, Map);
                if (lowerMap != null)
                {
                    return (Building_StairsDown)Position.GetThingList(lowerMap).FirstOrDefault(x => x is Building_StairsDown);
                }

                return null;
            }
        }
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            var ZTracker = ZUtils.ZTracker;
            if (ZTracker.stairsUp[Map].Contains(this))
            {
                ZTracker.stairsUp[Map].Remove(this);
            }
            if (Position.GetTerrain(Map) == ZLevelsDefOf.ZL_OutsideTerrainTwo)
            {
                Map.terrainGrid.SetTerrain(Position, ZLevelsDefOf.ZL_OutsideTerrain);
            }
            base.Destroy(mode);
        }

        public bool syncDamage = true;

        public override void PostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostApplyDamage(dinfo, totalDamageDealt);
            Map upperLevel = ZUtils.ZTracker.GetUpperLevel(Map.Tile, Map);
            if (giveDamage && upperLevel != null && upperLevel.listerThings.ThingsOfDef(ZLevelsDefOf.ZL_StairsDown)
                .Where(x => x.Position == Position).FirstOrDefault() is Building_StairsDown stairsDown)
            {
                var stairsDown = this.GetMatchingStair;
                if (stairsDown != null)
                {
                    Log.Message(stairsDown + ".HitPoints -= " + (int)totalDamageDealt, true);
                    stairsDown.syncDamage = false;
                    stairsDown.TakeDamage(new DamageInfo(dinfo.Def, dinfo.Amount));
                    stairsDown.syncDamage = true;
                }
            }
            base.PostApplyDamage(dinfo, totalDamageDealt);
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
            Scribe_Values.Look<string>(ref pathToPreset, "pathToPreset");
            Scribe_Collections.Look<string>(ref visitedPawns, "visitedPawns");
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

