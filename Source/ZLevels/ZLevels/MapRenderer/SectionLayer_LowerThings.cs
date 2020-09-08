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
			var ZTracker = ZUtils.ZTracker;
			int curLevel = ZTracker.GetZIndexFor(base.Map);
			if (curLevel > 0) 
			{
				base.ClearSubMeshes(MeshParts.All);
				foreach (var map in ZTracker.GetAllMaps(base.Map.Tile)
					.OrderByDescending(x => ZTracker.GetZIndexFor(x)))
				{
					int baseLevel = ZTracker.GetZIndexFor(map);
					if (curLevel > baseLevel && baseLevel >= 0)
					{
						foreach (IntVec3 intVec in this.section.CellRect)
						{
							IntVec3 position2 = intVec + new IntVec3(0, 0, -1);
							if (intVec.GetTerrain(base.Map) == ZLevelsDefOf.ZL_OutsideTerrain ||
								position2.InBounds(base.Map) && intVec.GetTerrain(base.Map) != ZLevelsDefOf.ZL_OutsideTerrain
								&& position2.GetTerrain(base.Map) == ZLevelsDefOf.ZL_OutsideTerrain)
							{
								List<Thing> list = map.thingGrid.ThingsListAt(intVec);
								int count = list.Count;
								for (int i = 0; i < count; i++)
								{
									Thing thing = list[i];
									if //((thing.def.seeThroughFog || 
									   //	!map.fogGrid.fogGrid[CellIndicesUtility.CellToIndex
									   //	(thing.Position, map.Size.x)]) && 
										
										(thing.def.drawerType != DrawerType.None
										&& (thing.def.drawerType != DrawerType.RealtimeOnly 
										|| !this.requireAddToMapMesh)
										&& (thing.def.hideAtSnowDepth >= 1f 
										|| map.snowGrid.GetDepth(thing.Position)
										<= thing.def.hideAtSnowDepth) && thing.Position.x == intVec.x
										&& thing.Position.z == intVec.z)
									{
										this.TakePrintFrom(thing, curLevel, baseLevel);
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
			DrawPos_Patch.ChangeDrawPos = true;
			TrueCenter_Patch.ChangeDrawPos = true;
			//DrawPos_Patch.levelOffset = (curLevel - baseLevel) / 2f;
			//TrueCenter_Patch.levelOffset = (curLevel - baseLevel) / 2f;

			try
			{
				t.Graphic.Print(this, t);

				//if (t is Mineable || t.def.defName.ToLower().Contains("wall"))
				//{
				//}
				//else if (t.Graphic is Graphic_Mote)
				//{
				//
				//}
				//else if (t.Graphic is Graphic_LinkedCornerFiller
				//	|| t.Graphic is Graphic_RandomRotated
				//	 || t.Graphic is Graphic_Linked)
				//{
				//	t.Graphic.Print(this, t);
				//}
				//else
				//{
				//	Vector2 drawSize = t.Graphic.drawSize;
				//	drawSize.x *= 1f - (((float)(curLevel) - (float)baseLevel) / 5f);
				//	drawSize.y *= 1f - (((float)(curLevel) - (float)baseLevel) / 5f);
				//	var newGraphic = t.Graphic.GetCopy(drawSize);
				//	newGraphic.Print(this, t);
				//}
			}
			catch (Exception ex)
			{

			}
			DrawPos_Patch.ChangeDrawPos = false;
			TrueCenter_Patch.ChangeDrawPos = false;
		}

		protected bool requireAddToMapMesh;
	}
}

