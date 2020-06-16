using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using Verse;

namespace ZLevels
{
    public static class BlueprintUtility
    {
        public static void SaveEverything(string path, Map map, string elementName)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(Path.GetDirectoryName(path));
            if (!directoryInfo.Exists)
            {
                directoryInfo.Create();
            }
            List<Pawn> pawns = new List<Pawn>();
            List<Building> buildings = new List<Building>();
            List<Thing> things = new List<Thing>();
            List<Filth> filths = new List<Filth>();
            List<Plant> plants = new List<Plant>();
            Dictionary<IntVec3, TerrainDef> terrains = new Dictionary<IntVec3, TerrainDef>();
            Dictionary<IntVec3, RoofDef> roofs = new Dictionary<IntVec3, RoofDef>();
            foreach (var thing in map.listerThings.AllThings)
            {
                if (thing is Gas || thing is Mote) continue;
                if (thing is Pawn pawn)
                {
                    pawns.Add(pawn);
                }
                else if (thing is Filth filth)
                {
                    filths.Add(filth);
                }
                else if (thing is Plant plant)
                {
                    plants.Add(plant);
                }
                else if (thing is Building building)
                {
                    buildings.Add(building);
                }
                else
                {
                    things.Add(thing);
                }
            }

            foreach (IntVec3 intVec in map.AllCells)
            {
                var terrain = intVec.GetTerrain(map);
                if (terrain != null)
                {
                    terrains[intVec] = terrain;
                }

                var roof = intVec.GetRoof(map);
                if (roof != null)
                {
                    roofs[intVec] = roof;
                }
            }

            Scribe.saver.InitSaving(path, elementName);
            Scribe_Collections.Look<Pawn>(ref pawns, "Pawns", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<Building>(ref buildings, "Buildings", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<Filth>(ref filths, "Filths", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<Thing>(ref things, "Things", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<Plant>(ref plants, "Plants", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<IntVec3, TerrainDef>(ref terrains, "Terrains",
                LookMode.Value, LookMode.Def, ref terrainKeys, ref terrainValues);
            Scribe_Collections.Look<IntVec3, RoofDef>(ref roofs, "Roofs",
                LookMode.Value, LookMode.Def, ref roofsKeys, ref roofsValues);
            Scribe.saver.FinalizeSaving();
        }

        public static void LoadEverything(Map map, string path)
        {
            List<Pawn> pawns = new List<Pawn>();
            List<Building> buildings = new List<Building>();
            List<Filth> filths = new List<Filth>();
            List<Thing> things = new List<Thing>();
            List<Plant> plants = new List<Plant>();
            Dictionary<IntVec3, TerrainDef> terrains = new Dictionary<IntVec3, TerrainDef>();
            Dictionary<IntVec3, RoofDef> roofs = new Dictionary<IntVec3, RoofDef>();

            Scribe.loader.InitLoading(path);

            Scribe_Collections.Look<Pawn>(ref pawns, "Pawns", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<Building>(ref buildings, "Buildings", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<Filth>(ref filths, "Filths", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<Thing>(ref things, "Things", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<Plant>(ref plants, "Plants", LookMode.Deep, new object[0]);

            Scribe_Collections.Look<IntVec3, TerrainDef>(ref terrains, "Terrains",
                LookMode.Value, LookMode.Def, ref terrainKeys, ref terrainValues);
            Scribe_Collections.Look<IntVec3, RoofDef>(ref roofs, "Roofs",
                LookMode.Value, LookMode.Def, ref roofsKeys, ref roofsValues);

            Scribe.loader.FinalizeLoading();

            if (terrains != null && terrains.Count > 0)
            {
                foreach (var terrain in terrains)
                {
                    try
                    {
                        if (GenGrid.InBounds(terrain.Key, map))
                        {
                            map.terrainGrid.SetTerrain(terrain.Key, terrain.Value);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error in map generating, cant spawn " + terrain.Key + " - " + ex);
                    }
                }
            }

            if (pawns != null && pawns.Count > 0)
            {
                foreach (var pawn in pawns)
                {
                    try
                    {
                        if (GenGrid.InBounds(pawn.Position, map))
                        {
                            GenSpawn.Spawn(pawn, pawn.Position, map, WipeMode.Vanish);
                            pawn.SetFaction(pawn.Faction);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error in map generating, cant spawn " + pawn + " - " + ex);
                    }
                }
            }

            if (buildings != null && buildings.Count > 0)
            {
                foreach (var building in buildings)
                {
                    try
                    {
                        if (GenGrid.InBounds(building.Position, map))
                        {
                            GenSpawn.Spawn(building, building.Position, map, building.Rotation, WipeMode.Vanish);
                            if (building.def.CanHaveFaction)
                            {
                                building.SetFaction(building.Faction);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error in map generating, cant spawn " + building + " - " + ex);
                    }
                }
            }

            if (filths != null && filths.Count > 0)
            {
                foreach (var filth in filths)
                {
                    GenSpawn.Spawn(filth, filth.Position, map, WipeMode.Vanish);
                }
            }

            if (plants != null && plants.Count > 0)
            {
                foreach (var plant in plants)
                {
                    GenSpawn.Spawn(plant, plant.Position, map, WipeMode.Vanish);
                }
            }

            if (things != null && things.Count > 0)
            {
                foreach (var thing in things)
                {
                    GenSpawn.Spawn(thing, thing.Position, map, WipeMode.Vanish);
                }
            }

            if (roofs != null && roofs.Count > 0)
            {
                foreach (var roof in roofs)
                {
                    try
                    {
                        if (GenGrid.InBounds(roof.Key, map))
                        {
                            map.roofGrid.SetRoof(roof.Key, roof.Value);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error in map generating, cant spawn " + roof.Key + " - " + ex);
                    }
                }
            }
        }

        public static List<IntVec3> terrainKeys = new List<IntVec3>();
        public static List<IntVec3> roofsKeys = new List<IntVec3>();
        public static List<TerrainDef> terrainValues = new List<TerrainDef>();
        public static List<RoofDef> roofsValues = new List<RoofDef>();
    }
}

