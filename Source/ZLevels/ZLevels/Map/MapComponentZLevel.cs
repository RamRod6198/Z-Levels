using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ZLevels
{
    public class MapComponentZLevel : MapComponent
    {
        public MapComponentZLevel(Map map) : base(map)
        {

        }

        public int Z_LevelIndex = 0;
        public override void MapGenerated()
        {
            base.MapGenerated();
            if (Rand.Chance(0.5f) && (Find.WorldGrid[map.Tile].hilliness == Hilliness.SmallHills
                        || Find.WorldGrid[map.Tile].hilliness == Hilliness.LargeHills
                        || Find.WorldGrid[map.Tile].hilliness == Hilliness.Mountainous
                        || Find.WorldGrid[map.Tile].hilliness == Hilliness.Impassable))
            {
                ZLogger.Message("The map has caves below now");
                this.hasCavesBelow = true;
            }
        }
        public override void MapComponentUpdate()
        {
            base.MapComponentUpdate();
            //if (this.DoGeneration && path.Length > 0)
            //{
            //    SettlementGeneration.DoSettlementGeneration(this.map, this.path, this.map.ParentFaction, false);
            //    this.DoGeneration = false;
            //}
            if (this.ReFog)
            {
                ZLogger.Message("Refog" + this.map);
                FloodFillerFog.DebugRefogMap(this.map);
                this.ReFog = false;
            }
        }

        //public void DoForcedGeneration(bool disableFog)
        //{
        //    SettlementGeneration.DoSettlementGeneration(this.map, this.path, this.map.ParentFaction, disableFog);
        //    this.DoGeneration = false;
        //}

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            if (!this.hasCavesBelow.HasValue)
            {
                if (Rand.Chance(0.5f) && (Find.WorldGrid[map.Tile].hilliness == Hilliness.SmallHills
                    || Find.WorldGrid[map.Tile].hilliness == Hilliness.LargeHills
                    || Find.WorldGrid[map.Tile].hilliness == Hilliness.Mountainous
                    || Find.WorldGrid[map.Tile].hilliness == Hilliness.Impassable))
                {
                    ZLogger.Message("The map has caves below now");
                    this.hasCavesBelow = true;
                }
            }

            var ZTracker = ZUtils.ZTracker;
            if (ZTracker.ZLevelsTracker == null)
            {
                ZLogger.Message("1 Resetting ZLevelsTracker");
                ZTracker.ZLevelsTracker = new Dictionary<int, ZLevelData>();
            }

            foreach (var tile in ZTracker.ZLevelsTracker)
            {
                foreach (var zData in ZTracker.ZLevelsTracker[tile.Key].ZLevels)
                {
                    ZLogger.Message("2 Tile: " + tile.Key + " - Map: " + ZTracker.GetMapInfo(zData.Value));
                    ZLogger.Message("Map null: " + (zData.Value == null).ToString());
                    ZLogger.Message("Map.Pawns null: " + (zData.Value.mapPawns == null).ToString());
                    ZLogger.Message("2 Map.Pawns null: " + (this.map.mapPawns == null).ToString());
                }
            }

            if (!ZTracker.ZLevelsTracker.ContainsKey(this.map.Tile) && ZTracker.TryRegisterMap(this.map, 0))
            {
                this.Z_LevelIndex = 0;
            }
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (Find.TickManager.TicksGame % Rand.RangeInclusive(60, 100) == 0)
            {
                if (this.ActiveInfestations != null && this.ActiveInfestations.Count > 0)
                {
                    foreach (var infestation in this.ActiveInfestations)
                    {
                        foreach (var pawnKind in infestation.infestators)
                        {
                            if (infestation.infestationParms > 0f)
                            {
                                var pawn = PawnGenerator.GeneratePawn(pawnKind, null);
                                infestation.infestationParms -= pawnKind.combatPower;
                                GenSpawn.Spawn(pawn, infestation.infestationPlace, map);
                            }
                        }
                    }
                    this.ActiveInfestations.RemoveAll(x => x.infestationParms <= 0f);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look<InfestationData>(ref this.ActiveInfestations, "InfestationDatas", LookMode.Deep, null);
            Scribe_Collections.Look<InfestationData>(ref this.TotalInfestations, "TotalInfestations", LookMode.Deep, null);
            Scribe_Values.Look<int>(ref this.Z_LevelIndex, "Z_LevelIndex", 0);

            Scribe_Values.Look<bool>(ref this.DoGeneration, "DoGeneration", false);
            Scribe_Values.Look<bool?>(ref this.hasCavesBelow, "hasCavesBelow", null);
            Scribe_Values.Look<string>(ref this.path, "path", "");
        }

        public bool DoGeneration = false;

        public bool ReFog = false;
        public string path = "";

        public bool? hasCavesBelow;

        public List<InfestationData> ActiveInfestations;

        public List<InfestationData> TotalInfestations;

    }
}

