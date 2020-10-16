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

		public Material LinkedDrawMatFrom(Graphic_LinkedCornerFiller graphic, Thing parent, IntVec3 cell)
		{
			int num = 0;
			int num2 = 1;
			for (int i = 0; i < 4; i++)
			{
				IntVec3 c = cell + GenAdj.CardinalDirections[i];
				if (graphic.ShouldLinkWith(c, parent))
				{
					num += num2;
				}
				num2 *= 2;
			}
			LinkDirections linkSet = (LinkDirections)num;
			return MaterialAtlasPool.SubMaterialFromAtlas(graphic.subGraphic.MatSingleFor(parent), linkSet);
		}
		public void BasePrint(Graphic_LinkedCornerFiller graphic, SectionLayer layer, Thing thing)
		{
			Material mat = LinkedDrawMatFrom(graphic, thing, thing.Position);
			Printer_Plane.PrintPlane(layer, thing.TrueCenter(), new Vector2(1f, 1f), mat);
		}
		public void Print(Graphic_LinkedCornerFiller graphic, SectionLayer layer, Thing thing, int curLevel, int baseLevel)
		{
			BasePrint(graphic, layer, thing);
			IntVec3 position = thing.Position;
			for (int i = 0; i < 4; i++)
			{
				IntVec3 c = thing.Position + GenAdj.DiagonalDirectionsAround[i];
				if (!graphic.ShouldLinkWith(c, thing) || (i == 0 && (!graphic.ShouldLinkWith(position + IntVec3.West, thing) || !graphic.ShouldLinkWith(position + IntVec3.South, thing)))
					|| (i == 1 && (!graphic.ShouldLinkWith(position + IntVec3.West, thing) || !graphic.ShouldLinkWith(position + IntVec3.North, thing))) || (i == 2 
					&& (!graphic.ShouldLinkWith(position + IntVec3.East, thing) || !graphic.ShouldLinkWith(position + IntVec3.North, thing))) || (i == 3 
					&& (!graphic.ShouldLinkWith(position + IntVec3.East, thing) || !graphic.ShouldLinkWith(position + IntVec3.South, thing))))
				{
					continue;
				}
				DrawPos_Patch.ChangeDrawPos = false;
				Vector3 center = thing.DrawPos + GenAdj.DiagonalDirectionsAround[i].ToVector3().normalized * Graphic_LinkedCornerFiller.CoverOffsetDist + Altitudes.AltIncVect 
					+ new Vector3(0f, 0f, 0.09f);
				center.z -= (curLevel - baseLevel) / 2f;
				center.y -= curLevel - baseLevel;
				DrawPos_Patch.ChangeDrawPos = true;
				Vector2 size = new Vector2(0.5f, 0.5f);
				if (!c.InBounds(thing.Map))
				{
					if (c.x == -1)
					{
						center.x -= 1f;
						size.x *= 5f;
					}
					if (c.z == -1)
					{
						center.z -= 1f;
						size.y *= 5f;
					}
					if (c.x == thing.Map.Size.x)
					{
						center.x += 1f;
						size.x *= 5f;
					}
					if (c.z == thing.Map.Size.z)
					{
						center.z += 1f;
						size.y *= 5f;
					}
				}
				Printer_Plane.PrintPlane(layer, center, size, LinkedDrawMatFrom(graphic, thing, thing.Position), 0f, flipUv: false, Graphic_LinkedCornerFiller.CornerFillUVs);
			}
		}


		public void Print(Blight blight, SectionLayer layer, Graphic newGraphic)
		{
			Plant plant = blight.Plant;
			if (plant != null)
			{
				PlantUtility.SetWindExposureColors(Blight.workingColors, plant);
			}
			else
			{
				Blight.workingColors[0].a = (Blight.workingColors[1].a = (Blight.workingColors[2].a = (Blight.workingColors[3].a = 0)));
			}
			float num = Blight.SizeRange.LerpThroughRange(blight.severity);
			if (plant != null)
			{
				float a = newGraphic.drawSize.x * plant.def.plant.visualSizeRange.LerpThroughRange(plant.Growth);
				num *= Mathf.Min(a, 1f);
			}
			num = Mathf.Clamp(num, 0.5f, 0.9f);
			Printer_Plane.PrintPlane(layer, blight.TrueCenter(), blight.def.graphic.drawSize * num, newGraphic.MatAt(blight.Rotation, blight), 0f, flipUv: false, null, 
				Blight.workingColors, 0.1f);
		}

		public void Print(Plant plant, SectionLayer layer, Graphic newGraphic, int curLevel, int baseLevel)
		{
			Vector3 a = plant.TrueCenter();
			Rand.PushState();
			Rand.Seed = plant.Position.GetHashCode();
			int num = Mathf.CeilToInt(plant.growthInt * (float)plant.def.plant.maxMeshCount);
			if (num < 1)
			{
				num = 1;
			}
			float num2 = plant.def.plant.visualSizeRange.LerpThroughRange(plant.growthInt);
			float num3 = plant.def.graphicData.drawSize.x * num2;

			Vector3 center = Vector3.zero;
			int num4 = 0;
			int[] positionIndices = PlantPosIndices.GetPositionIndices(plant);
			bool flag = false;
			foreach (int num5 in positionIndices)
			{
				if (plant.def.plant.maxMeshCount == 1)
				{
					center = a + Gen.RandomHorizontalVector(0.05f);
					float num6 = plant.Position.z;
					if (center.z - num2 / 2f < num6)
					{
						center.z = num6 + num2 / 2f;
						flag = true;
					}
				}
				else
				{
					int num7 = 1;
					switch (plant.def.plant.maxMeshCount)
					{
						case 1:
							num7 = 1;
							break;
						case 4:
							num7 = 2;
							break;
						case 9:
							num7 = 3;
							break;
						case 16:
							num7 = 4;
							break;
						case 25:
							num7 = 5;
							break;
						default:
							Log.Error(string.Concat(plant.def, " must have plant.MaxMeshCount that is a perfect square."));
							break;
					}
					float num8 = 1f / (float)num7;
					center = plant.Position.ToVector3();
					center.y = plant.def.Altitude;
					center.x += 0.5f * num8;
					center.z += 0.5f * num8;
					int num9 = num5 / num7;
					int num10 = num5 % num7;
					center.x += (float)num9 * num8;
					center.z += (float)num10 * num8;
					float max = num8 * 0.3f;
					center += Gen.RandomHorizontalVector(max);
				}
				bool @bool = Rand.Bool;
				Material matSingle = newGraphic.MatSingle;
				PlantUtility.SetWindExposureColors(Plant.workingColors, plant);
				center.z -= (curLevel - baseLevel) / 1.5f;
				center.y -= curLevel - baseLevel;
				num3 *= 1f - (((float)(curLevel) - (float)baseLevel) / 5f);
				Printer_Plane.PrintPlane(size: new Vector2(num3, num3), layer: layer, center: center, mat: matSingle, rot: 0f, flipUv: @bool, uvs: null,
					colors: Plant.workingColors, topVerticesAltitudeBias: 0.1f, uvzPayload: plant.HashOffset() % 1024);
				num4++;
				if (num4 >= num)
				{
					break;
				}
			}
			if (plant.def.graphicData.shadowData != null)
			{
				Vector3 center2 = a + plant.def.graphicData.shadowData.offset * num2;
				if (flag)
				{
					center2.z = plant.Position.ToVector3Shifted().z + plant.def.graphicData.shadowData.offset.z;
				}
				center2.y -= 3f / 70f;
				center2.z -= (curLevel - baseLevel) / 2f;
				center2.y -= curLevel - baseLevel;
				Vector3 volume = plant.def.graphicData.shadowData.volume * num2;
				Printer_Shadow.PrintShadow(layer, center2, volume, Rot4.North);
			}
			Rand.PopState();
		}

		public void BasePrint(Thing thing, SectionLayer layer, Graphic newGraphic)
        {
			newGraphic.Print(layer, thing);
		}
		public void Print(ThingWithComps thingWithComps, SectionLayer layer, Graphic newGraphic)
		{
			BasePrint(thingWithComps, layer, newGraphic);
			if (thingWithComps.comps != null)
			{
				for (int i = 0; i < thingWithComps.comps.Count; i++)
				{
					thingWithComps.comps[i].PostPrintOnto(layer);
				}
			}
		}
		protected void TakePrintFrom(Thing t, int curLevel, int baseLevel)
		{
			DrawPos_Patch.ChangeDrawPos = true;
			DrawPos_Patch.zLevelOffset = -(curLevel - baseLevel) / 2f;
			DrawPos_Patch.yLevelOffset = baseLevel - curLevel;
			try
			{
				if (t is Mineable || t.def.defName.ToLower().Contains("wall"))
				{
					if (t.Graphic is Graphic_LinkedCornerFiller linkedCornerFiller)
				    {
						Print(linkedCornerFiller, this, t, curLevel, baseLevel);
					}
					else
					{
						t.Print(this);
					}
				}
				
				else if (t.Graphic is Graphic_Mote)
				{

				}
				else if (t.Graphic is Graphic_LinkedCornerFiller
					|| t.Graphic is Graphic_RandomRotated
					 || t.Graphic is Graphic_Linked)
				{
					t.Print(this);
				}
				else
				{
					DrawPos_Patch.yLevelOffset -= 1;
					Vector2 drawSize = t.Graphic.drawSize;
					drawSize.x *= 1f - (((float)(curLevel) - (float)baseLevel) / 5f);
					drawSize.y *= 1f - (((float)(curLevel) - (float)baseLevel) / 5f);
					var newGraphic = t.Graphic.GetCopy(drawSize);
					if (t is Blight blight)
                    {
						Print(blight, this, newGraphic);
                    }
					else if (t is Plant plant)
                    {
						Print(plant, this, newGraphic, curLevel, baseLevel);
					}
					else
                    {
						if (t is ThingWithComps thingWithComps)
                        {
							Print(thingWithComps, this, newGraphic);
                        }
						else
                        {
							BasePrint(t, this, newGraphic);
                        }
                    }
				}
			}
			catch (Exception ex)
			{

			}
			DrawPos_Patch.ChangeDrawPos = false;
		}
		protected bool requireAddToMapMesh;
	}
}

