using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace ZLevels
{
	public class SectionLayer_LowerFogOfWar : SectionLayer
	{
		public override bool Visible
		{
			get
			{
				return DebugViewSettings.drawFog;
			}
		}

		public SectionLayer_LowerFogOfWar(Section section) : base(section)
		{
			this.relevantChangeTypes = MapMeshFlag.FogOfWar;
		}

		public override void Regenerate()
		{
			var ZTracker = ZUtils.ZTracker;
			int curLevel = base.Map.ZIndex;
			bool[] fogGrid = base.Map.fogGrid.fogGrid;
			if (curLevel > 0)
			{
				foreach (var map in ZTracker.GetAllMaps(base.Map.Tile)
					.OrderByDescending(x => x.ZIndex))
				{
					int baseLevel = map.ZIndex;
					CellIndices cellIndices = map.cellIndices;

					if (curLevel > baseLevel && baseLevel >= 0)
					{
						CellRect cellRect = this.section.CellRect;

						foreach (IntVec3 intVec in this.section.CellRect)
						{
							if (intVec.Fogged(map) && !intVec.Fogged(base.Map))
							{
								fogGrid[cellIndices.CellToIndex(intVec)] = true;
								base.Map.mapDrawer.MapMeshDirty(intVec, MapMeshFlag.FogOfWar);
							}
						}
					}
				}
			}

		}

		private bool[] vertsCovered = new bool[9];

		private const byte FogBrightness = 35;
	}
}

