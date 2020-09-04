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
		public static Dictionary<Pawn, PawnRendererScaled> cachedRenderers = new Dictionary<Pawn, PawnRendererScaled>();

		[HarmonyPostfix]
		public static void DynamicDrawManagerPostfix(DynamicDrawManager __instance, Map ___map, ref bool ___drawingNow)
		{
			var ZTracker = ZUtils.ZTracker;
			int curLevel = ZTracker.GetZIndexFor(___map);
			if (curLevel != 0)
            {
				foreach (var map2 in ZTracker.GetAllMaps(___map.Tile).OrderBy(x => ZTracker.GetZIndexFor(x)))
				{
					int baseLevel = ZTracker.GetZIndexFor(map2);
					if (curLevel > baseLevel && baseLevel >= 0)
					{
						if (!DebugViewSettings.drawThingsDynamic)
						{
							return;
						}
						___drawingNow = true;
						bool[] fogGrid = map2.fogGrid.fogGrid;
						CellRect cellRect = Find.CameraDriver.CurrentViewRect;
						cellRect.ClipInsideMap(map2);
						cellRect = cellRect.ExpandedBy(1);
						CellIndices cellIndices = map2.cellIndices;
						foreach (Thing thing in map2.dynamicDrawManager.drawThings)
						{
							IntVec3 position = thing.Position;
							if (position.GetTerrain(___map) == ZLevelsDefOf.ZL_OutsideTerrain)
							{
								if ((cellRect.Contains(position) || thing.def.drawOffscreen)
									//&& (!fogGrid[cellIndices.CellToIndex(position)]
									//|| thing.def.seeThroughFog) 
									&& (thing.def.hideAtSnowDepth >= 1f
									|| map2.snowGrid.GetDepth(position) <= thing.def.hideAtSnowDepth))
								{
									try
									{
										if (thing.Graphic is Graphic_Mote)
										{

										}
										else if (thing.Graphic is Graphic_LinkedCornerFiller
											|| thing.Graphic is Graphic_RandomRotated
											 || thing.Graphic is Graphic_Linked)
										{
											thing.Draw();
										}
										else if (thing is Pawn pawn)
										{
											if (cachedRenderers.ContainsKey(pawn))
											{
												cachedRenderers[pawn].RenderPawnAt(thing.DrawPos, curLevel, baseLevel);
											}
											else
											{
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
												newRenderer.RenderPawnAt(thing.DrawPos, curLevel, baseLevel);
												cachedRenderers[pawn] = newRenderer;
											}
										}
										else if (thing.def.projectile == null)
										{
											Vector2 drawSize = thing.Graphic.drawSize;
											drawSize.x *= 1f - (((float)(curLevel) - (float)baseLevel) / 5f);
											drawSize.y *= 1f - (((float)(curLevel) - (float)baseLevel) / 5f);
											var newGraphic = thing.Graphic.GetCopy(drawSize);
											newGraphic.Draw(thing.DrawPos, thing.Rotation, thing);
										}
										else
										{
											thing.Draw();
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
						___drawingNow = false;
					}
				}
			}
		}
	}
}

