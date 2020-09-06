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
        Building_Stairs GetMatchingStair()
        {
            if (this is Building_StairsUp)
            {
                return (Building_Stairs)Position.GetThingList(ZUtils.ZTracker.GetUpperLevel(Map.Tile, Map))
                    .FirstOrDefault(x => x is Building_StairsDown);
            }
            else
            {
                return (Building_Stairs)Position.GetThingList(ZUtils.ZTracker.GetLowerLevel(Map.Tile, Map))
                    .FirstOrDefault(x => x is Building_StairsUp);
            }
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
        }

        public Building_StairsDown GetMatchingStair
        {
            get
            {
                Map lowerMap = ZUtils.ZTracker.GetLowerLevel(this.Map.Tile, this.Map);
                if (lowerMap != null)
                {
                    return (Building_StairsDown)this.Position.GetThingList(lowerMap).FirstOrDefault(x => x is Building_StairsDown);
                }
                else
                {
                    return null;
                }
            }
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

        public bool syncDamage = true;

        public override void PostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            Map upperLevel = ZUtils.ZTracker.GetUpperLevel(this.Map.Tile, this.Map);
            if (syncDamage)
            {
                var stairsDown = this.GetMatchingStair;
                if (stairsDown != null)
                {
                    ZLogger.Message(stairsDown + ".HitPoints -= " + (int)totalDamageDealt, true);
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

