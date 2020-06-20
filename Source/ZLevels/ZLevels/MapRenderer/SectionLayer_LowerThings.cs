using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ZLevels
{
	public class SectionLayer_LowerThings : SectionLayer
	{
		public SectionLayer_LowerThings(Section section) : base(section)
		{
			this.relevantChangeTypes = MapMeshFlag.Things;
			this.requireAddToMapMesh = true;
		}

		public override void DrawLayer()
		{
			if (!DebugViewSettings.drawThingsPrinted)
			{
				return;
			}
			base.DrawLayer();
		}

		public override void Regenerate()
		{
			var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
			if (ZTracker.GetZIndexFor(base.Map) > 0) 
			{
				base.ClearSubMeshes(MeshParts.All);
				foreach (var map in ZTracker.GetAllMaps(base.Map.Tile)
					.OrderBy(x => ZTracker.GetZIndexFor(x)))
				{
					if (map != base.Map && ZTracker.GetZIndexFor(map) >= 0)
					{
						foreach (IntVec3 intVec in this.section.CellRect)
						{
							if (base.Map.terrainGrid.TerrainAt(intVec) == ZLevelsDefOf.ZL_OutsideTerrain)
							{
								List<Thing> list = map.thingGrid.ThingsListAt(intVec);
								int count = list.Count;
								for (int i = 0; i < count; i++)
								{
									Thing thing = list[i];
									if ((thing.def.seeThroughFog || !map.fogGrid.fogGrid[CellIndicesUtility.CellToIndex
										(thing.Position, map.Size.x)]) && thing.def.drawerType != DrawerType.None
										&& (thing.def.drawerType != DrawerType.RealtimeOnly || !this.requireAddToMapMesh)
										&& (thing.def.hideAtSnowDepth >= 1f || map.snowGrid.GetDepth(thing.Position)
										<= thing.def.hideAtSnowDepth) && thing.Position.x == intVec.x
										&& thing.Position.z == intVec.z)
									{
										this.TakePrintFrom(thing, ZTracker.GetZIndexFor(base.Map), ZTracker.GetZIndexFor(map));
									}
								}
							}
						}
					}
				}
				base.FinalizeMesh(MeshParts.All);
			}

		}

		protected float AngleFromRot(Rot4 rot, Graphic graphic)
		{
			if (graphic.ShouldDrawRotated)
			{
				float num = rot.AsAngle;
				num += graphic.DrawRotatedExtraAngleOffset;
				if ((rot == Rot4.West && graphic.WestFlipped) || (rot == Rot4.East && graphic.EastFlipped))
				{
					num += 180f;
				}
				return num;
			}
			return 0f;
		}

		protected void TakePrintFrom(Thing t, int curLevel, int baseLevel)
		{
			try
			{
				if (t is Mineable || t.def.defName.ToLower().Contains("wall"))
				{
					t.Graphic.Print(this, t);
				}
				else
				{
					Vector2 drawSize = t.Graphic.drawSize;

					drawSize.x *= 1f - (((float)(curLevel) - (float)baseLevel) / 5f);
					drawSize.y *= 1f - (((float)(curLevel) - (float)baseLevel) / 5f);
					var newGraphic = t.Graphic.GetCopy(drawSize);
					newGraphic.Print(this, t);
				}
			}
			catch (Exception ex)
			{

			}
		}
		protected bool requireAddToMapMesh;
	}
}

