using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace ZLevels
{
	public class GenStep_RocksFromGridUnderground : GenStep
	{
		private class RoofThreshold
		{
			public RoofDef roofDef;

			public float minGridVal;
		}

		private float maxMineableValue = float.MaxValue;

		private const int MinRoofedCellsPerGroup = 20;

		public override int SeedPart => 1182952823;

		public static ThingDef RockDefAt(IntVec3 c)
		{
			ThingDef thingDef = null;
			float num = -999999f;
			for (int i = 0; i < RockNoises.rockNoises.Count; i++)
			{
				float value = RockNoises.rockNoises[i].noise.GetValue(c);
				if (value > num)
				{
					thingDef = RockNoises.rockNoises[i].rockDef;
					num = value;
				}
			}
			if (thingDef == null)
			{
				ZLogger.ErrorOnce("Did not get rock def to generate at " + c, true);
				thingDef = ThingDefOf.Sandstone;
			}
			return thingDef;
		}

		public override void Generate(Map map, GenStepParams parms)
		{
			map.regionAndRoomUpdater.Enabled = false;
			float num = 0.7f;
			List<RoofThreshold> list = new List<RoofThreshold>();
			RoofThreshold roofThreshold = new RoofThreshold
			{
				roofDef = RoofDefOf.RoofRockThick,
				minGridVal = num * 1.14f
			};
			list.Add(roofThreshold);
			RoofThreshold roofThreshold2 = new RoofThreshold
			{
				roofDef = RoofDefOf.RoofRockThin,
				minGridVal = num * 1.04f
			};
			list.Add(roofThreshold2);
			MapGenFloatGrid elevation = MapGenerator.Elevation;
			MapGenFloatGrid caves = MapGenerator.Caves;
			foreach (IntVec3 allCell in map.AllCells)
			{
				if (caves[allCell] <= 0f)
				{
					GenSpawn.Spawn(RockDefAt(allCell), allCell, map);
				}
				map.roofGrid.SetRoof(allCell, RoofDefOf.RoofRockThick);
			}
			BoolGrid visited = new BoolGrid(map);
			List<IntVec3> toRemove = new List<IntVec3>();
			foreach (IntVec3 allCell2 in map.AllCells)
			{
				if (!visited[allCell2] && IsNaturalRoofAt(allCell2, map))
				{
					toRemove.Clear();
					map.floodFiller.FloodFill(allCell2, (IntVec3 x) => IsNaturalRoofAt(x, map), delegate(IntVec3 x)
					{
						visited[x] = true;
						toRemove.Add(x);
					});
					if (toRemove.Count < 20)
					{
						for (int j = 0; j < toRemove.Count; j++)
						{
							map.roofGrid.SetRoof(toRemove[j], null);
						}
					}
				}
			}
			GenStep_ScatterLumpsMineableUnderground genStep_ScatterLumpsMineable = new GenStep_ScatterLumpsMineableUnderground
			{
				maxValue = maxMineableValue
			};
			float num3 = 16f;
			genStep_ScatterLumpsMineable.countPer10kCellsRange = new FloatRange(num3, num3);
			genStep_ScatterLumpsMineable.Generate(map, parms);
			map.regionAndRoomUpdater.Enabled = true;
		}

		private bool IsNaturalRoofAt(IntVec3 c, Map map)
		{
			if (c.Roofed(map))
			{
				return c.GetRoof(map).isNatural;
			}
			return false;
		}
	}
}

