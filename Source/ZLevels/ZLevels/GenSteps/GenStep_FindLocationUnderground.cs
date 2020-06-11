using System;
using Verse;

namespace ZLevels
{
	public class GenStep_FindLocationUnderground : GenStep
	{
		public override int SeedPart
		{
			get
			{
				return 820815231;
			}
		}

		public override void Generate(Map map, GenStepParams parms)
		{
			DeepProfiler.Start("RebuildAllRegions");
			map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
			DeepProfiler.End();
			MapGenerator.PlayerStartSpot = ((MapParent_ZLevel)map.Parent).PlayerStartSpot;
		}
	}
}

