using System;
using System.Linq;
using RimWorld;
using Verse;

namespace ZLevels
{
	public class GenStep_AnimalsUnderground : GenStep
	{
		public override int SeedPart => 1298760307;

		public override void Generate(Map map, GenStepParams parms)
		{
			int num = 0;
			var mapParent = map.Parent as MapParent_ZLevel;
			if (mapParent.hasCaves)
			{
				int infestationCount = 0;
				if (mapParent.TotalInfestations != null && mapParent.TotalInfestations.Count > 0)
				{
					infestationCount = mapParent.TotalInfestations.Count;
				}
				var index = 0;
				while (true)
				{
					if (!map.wildAnimalSpawner.AnimalEcosystemFull)
					{
						num++;
						if (num >= 10000)
						{
							break;
						}
						IntVec3 loc = IntVec3.Invalid;
						if (infestationCount > 0 && index < infestationCount && CellFinder.TryFindRandomCellNear
							(mapParent.TotalInfestations[index].infestationPlace, map, 100,
							(IntVec3 c) => c.Walkable(map), out loc))
						{
							ZLogger.Message("Found loc: " + loc
								+ " origin place: " + mapParent.TotalInfestations[index].infestationPlace);
							index++;
						}
						else
						{
							loc = RCellFinder.RandomAnimalSpawnCell_MapGen(map);
						}
						if (!map.wildAnimalSpawner.SpawnRandomWildAnimalAt(loc))
						{
							ZLogger.Message("Spawning in loc: " + loc);
							return;
						}
						ZLogger.Message("Spawning in loc: " + loc);
						continue;
					}
					return;
				}
				Log.Error("Too many iterations.");
			}
		}
	}
}

