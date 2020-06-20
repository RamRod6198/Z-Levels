using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace ZLevels
{
	[HarmonyPatch(typeof(DynamicDrawManager), "DrawDynamicThings")]
	public static class GenerateGraphics
	{
		[HarmonyPostfix]
		public static void DynamicDrawManagerPostfix(DynamicDrawManager __instance)
		{
			Map map = Traverse.Create(__instance).Field("map").GetValue<Map>();
			var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
			foreach (var map2 in ZTracker.GetAllMaps(map.Tile)
					.OrderBy(x => ZTracker.GetZIndexFor(x)))
			{
				if (map != map2 && ZTracker.GetZIndexFor(map2) >= 0)
				{
					HashSet<Thing> drawThings = Traverse.Create(map2.dynamicDrawManager).Field("drawThings").GetValue<HashSet<Thing>>();
					int curLevel = ZTracker.GetZIndexFor(map);
					int baseLevel = ZTracker.GetZIndexFor(map2);

					if (!DebugViewSettings.drawThingsDynamic)
					{
						return;
					}
					Traverse.Create(__instance).Field("drawingNow").SetValue(true);
					try
					{
						bool[] fogGrid = map2.fogGrid.fogGrid;
						CellRect cellRect = Find.CameraDriver.CurrentViewRect;
						cellRect.ClipInsideMap(map2);
						cellRect = cellRect.ExpandedBy(1);
						CellIndices cellIndices = map2.cellIndices;
						foreach (Thing thing in drawThings)
						{
							IntVec3 position = thing.Position;
							if ((cellRect.Contains(position) || thing.def.drawOffscreen) 
								&& (!fogGrid[cellIndices.CellToIndex(position)] 
								|| thing.def.seeThroughFog) && (thing.def.hideAtSnowDepth >= 1f 
								|| map2.snowGrid.GetDepth(position) <= thing.def.hideAtSnowDepth))
							{
								try
								{
									if (thing.Graphic is Graphic_Mote)
									{

									}
									else if (thing.Graphic is Graphic_LinkedCornerFiller 
										|| thing.Graphic is Graphic_RandomRotated)
									{
										thing.Draw();
									}
									else if (thing is Pawn pawn)
									{
										Vector2 drawSize = thing.Graphic.drawSize;
										////drawSize.x *= 1f - (((float)(curLevel) - (float)baseLevel) / 10f);
										////drawSize.y *= 1f - (((float)(curLevel) - (float)baseLevel) / 10f);
										//

										drawSize.x = 0.3f;
										drawSize.y = 0.3f;
										

										var newRenderer = new PawnRendererScaled(pawn);
										pawn.Drawer.renderer.graphics.ResolveAllGraphics();
										newRenderer.graphics.nakedGraphic = pawn.Drawer.renderer.graphics.nakedGraphic;
										newRenderer.graphics.headGraphic = pawn.Drawer.renderer.graphics.headGraphic;
										newRenderer.graphics.hairGraphic = pawn.Drawer.renderer.graphics.hairGraphic;
										newRenderer.graphics.rottingGraphic = pawn.Drawer.renderer.graphics.rottingGraphic;
										newRenderer.graphics.dessicatedGraphic = pawn.Drawer.renderer.graphics.dessicatedGraphic;
										newRenderer.graphics.apparelGraphics = pawn.Drawer.renderer.graphics.apparelGraphics;
										newRenderer.graphics.packGraphic = pawn.Drawer.renderer.graphics.packGraphic;
										newRenderer.graphics.flasher = pawn.Drawer.renderer.graphics.flasher;
										newRenderer.RenderPawnAt(thing.DrawPos);


										//if (nakedGraphic != null)
										//{
										//	nakedGraphic.Draw(thing.DrawPos, thing.Rotation, thing);
										//}

										//var hairGraphic = pawn.Drawer.renderer.graphics.hairGraphic.GetCopy(drawSize);
										//if (hairGraphic != null)
										//{
										//	hairGraphic.Draw(thing.DrawPos, thing.Rotation, thing);
										//}
										//
										//var headGraphic = pawn.Drawer.renderer.graphics.headGraphic.GetCopy(drawSize);
										//if (headGraphic != null)
										//{
										//	headGraphic.Draw(thing.DrawPos, thing.Rotation, thing);
										//}
										//foreach (var apparel in pawn.Drawer.renderer.graphics.apparelGraphics)
										//{
										//	var apparelGraphic = apparel.graphic.GetCopy(drawSize);
										//	if (apparelGraphic != null)
										//	{
										//		apparelGraphic.Draw(thing.DrawPos, thing.Rotation, thing);
										//	}
										//}
									}
									else
									{
										Vector2 drawSize = thing.Graphic.drawSize;
										drawSize.x *= 1f - (((float)(curLevel) - (float)baseLevel) / 10f);
										drawSize.y *= 1f - (((float)(curLevel) - (float)baseLevel) / 10f);
										var newGraphic = thing.Graphic.GetCopy(drawSize);
										newGraphic.Draw(thing.DrawPos, thing.Rotation, thing);
									}
								}
								catch (Exception ex)
								{
									Log.Error(string.Concat(new object[]
									{
										"Exception drawing ",
										thing,
										": ",
										ex.ToString()
									}), false);
								}
							}
						}
					}
					catch (Exception arg)
					{
						Log.Error("Exception drawing dynamic things: " + arg, false);
					}
					Traverse.Create(__instance).Field("drawingNow").SetValue(false);
				}
			}
		}
    }
}
