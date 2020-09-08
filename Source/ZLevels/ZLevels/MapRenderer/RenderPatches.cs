using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ZLevels
{
	[HarmonyPatch(typeof(Thing), "DrawPos", MethodType.Getter)]
	public static class DrawPos_Patch
	{
		public static bool ChangeDrawPos = false;

		public static float levelOffset = 0f;
		public static void Postfix(ref Vector3 __result)
		{
			if (ChangeDrawPos)
			{
				__result.z -= levelOffset;
				__result.y -= levelOffset;
			}
		}
	}

	[HarmonyPatch(typeof(GenThing), "TrueCenter")]
	[HarmonyPatch(new Type[]
		{
			typeof(IntVec3),
			typeof(Rot4),
			typeof(IntVec2),
			typeof(float)
		}, new ArgumentType[]
		{
			ArgumentType.Normal,
			ArgumentType.Normal,
			ArgumentType.Normal,
			ArgumentType.Normal
		})]
	public static class TrueCenter_Patch
    {
		public static bool ChangeDrawPos = false;

		public static float levelOffset = 0f;
		public static void Postfix(ref Vector3 __result)
        {
			if (ChangeDrawPos)
			{
				__result.z -= levelOffset;
				__result.y -= levelOffset;
			}
		}
    }


	//[HarmonyPatch(typeof(DynamicDrawManager), "DrawDynamicThings")]
	//public static class GenerateGraphics
	//{
	//	public static Dictionary<Pawn, PawnRendererScaled> cachedPawnRenderers = new Dictionary<Pawn, PawnRendererScaled>();
	//	public static Dictionary<Pawn, PawnRendererScaled> cachedCorpseRenderers = new Dictionary<Pawn, PawnRendererScaled>();
	//
	//
	//	[HarmonyPostfix]
	//	public static void DynamicDrawManagerPostfix(DynamicDrawManager __instance, Map ___map, ref bool ___drawingNow)
	//	{
	//		var ZTracker = ZUtils.ZTracker;
	//		int curLevel = ZTracker.GetZIndexFor(___map);
	//		if (curLevel != 0)
    //        {
	//			foreach (var map2 in ZTracker.GetAllMaps(___map.Tile).OrderBy(x => ZTracker.GetZIndexFor(x)))
	//			{
	//				int baseLevel = ZTracker.GetZIndexFor(map2);
	//				if (curLevel > baseLevel && baseLevel >= 0)
	//				{
	//					if (!DebugViewSettings.drawThingsDynamic)
	//					{
	//						return;
	//					}
	//					___drawingNow = true;
	//					bool[] fogGrid = map2.fogGrid.fogGrid;
	//					CellRect cellRect = Find.CameraDriver.CurrentViewRect;
	//					cellRect.ClipInsideMap(map2);
	//					cellRect = cellRect.ExpandedBy(1);
	//					CellIndices cellIndices = map2.cellIndices;
	//					foreach (Thing thing in map2.dynamicDrawManager.drawThings)
	//					{
	//						IntVec3 position = thing.Position;
	//						IntVec3 position2 = position + new IntVec3(0, 0, -1);
	//						if (position.GetTerrain(___map) == ZLevelsDefOf.ZL_OutsideTerrain ||
	//							position2.InBounds(___map) && position.GetTerrain(___map) != ZLevelsDefOf.ZL_OutsideTerrain 
	//							&& position2.GetTerrain(___map) == ZLevelsDefOf.ZL_OutsideTerrain)
	//						{
	//							if ((cellRect.Contains(position) || thing.def.drawOffscreen)
	//								//&& (!fogGrid[cellIndices.CellToIndex(position)]
	//								//|| thing.def.seeThroughFog) 
	//								&& (thing.def.hideAtSnowDepth >= 1f
	//								|| map2.snowGrid.GetDepth(position) <= thing.def.hideAtSnowDepth))
	//							{
	//								DrawPos_Patch.ChangeDrawPos = true;
	//								TrueCenter_Patch.ChangeDrawPos = true;
	//								DrawPos_Patch.levelOffset = (baseLevel - curLevel) / 2f;
	//								TrueCenter_Patch.levelOffset = (baseLevel - curLevel) / 2f;
	//								Log.Message(thing.Map + " - levelOffset: " + DrawPos_Patch.levelOffset, true);
	//								Log.Message(thing.Map + " - levelOffset: " + TrueCenter_Patch.levelOffset, true);
	//								try
	//								{
	//									if (thing.Graphic is Graphic_Mote)
	//									{
	//
	//									}
	//									else if (thing.Graphic is Graphic_LinkedCornerFiller
	//										|| thing.Graphic is Graphic_RandomRotated
	//										 || thing.Graphic is Graphic_Linked)
	//									{
	//										thing.Draw();
	//									}
	//									else if (thing is Pawn pawn)
	//									{
	//										if (cachedPawnRenderers.ContainsKey(pawn))
	//										{
	//											cachedPawnRenderers[pawn].RenderPawnAt(thing.DrawPos, curLevel, baseLevel);
	//										}
	//										else
	//										{
	//											var newRenderer = new PawnRendererScaled(pawn, pawn.Drawer.renderer.wiggler);
	//											pawn.Drawer.renderer.graphics.ResolveAllGraphics();
	//											newRenderer.graphics.nakedGraphic = pawn.Drawer.renderer.graphics.nakedGraphic;
	//											newRenderer.graphics.headGraphic = pawn.Drawer.renderer.graphics.headGraphic;
	//											newRenderer.graphics.hairGraphic = pawn.Drawer.renderer.graphics.hairGraphic;
	//											newRenderer.graphics.rottingGraphic = pawn.Drawer.renderer.graphics.rottingGraphic;
	//											newRenderer.graphics.dessicatedGraphic = pawn.Drawer.renderer.graphics.dessicatedGraphic;
	//											newRenderer.graphics.apparelGraphics = pawn.Drawer.renderer.graphics.apparelGraphics;
	//											newRenderer.graphics.packGraphic = pawn.Drawer.renderer.graphics.packGraphic;
	//											newRenderer.graphics.flasher = pawn.Drawer.renderer.graphics.flasher;
	//											newRenderer.RenderPawnAt(thing.DrawPos, curLevel, baseLevel);
	//											cachedPawnRenderers[pawn] = newRenderer;
	//										}
	//									}
	//									else if (thing is Corpse corpse)
	//									{
	//										if (cachedCorpseRenderers.ContainsKey(corpse.InnerPawn))
	//										{
	//											cachedCorpseRenderers[corpse.InnerPawn].RenderPawnAt(thing.DrawPos, curLevel, baseLevel);
	//										}
	//										else
	//										{
	//											var newRenderer = new PawnRendererScaled(corpse.InnerPawn, corpse.InnerPawn.Drawer.renderer.wiggler);
	//											corpse.InnerPawn.Drawer.renderer.graphics.ResolveAllGraphics();
	//											newRenderer.graphics.nakedGraphic = corpse.InnerPawn.Drawer.renderer.graphics.nakedGraphic;
	//											newRenderer.graphics.headGraphic = corpse.InnerPawn.Drawer.renderer.graphics.headGraphic;
	//											newRenderer.graphics.hairGraphic = corpse.InnerPawn.Drawer.renderer.graphics.hairGraphic;
	//											newRenderer.graphics.rottingGraphic = corpse.InnerPawn.Drawer.renderer.graphics.rottingGraphic;
	//											newRenderer.graphics.dessicatedGraphic = corpse.InnerPawn.Drawer.renderer.graphics.dessicatedGraphic;
	//											newRenderer.graphics.apparelGraphics = corpse.InnerPawn.Drawer.renderer.graphics.apparelGraphics;
	//											newRenderer.graphics.packGraphic = corpse.InnerPawn.Drawer.renderer.graphics.packGraphic;
	//											newRenderer.graphics.flasher = corpse.InnerPawn.Drawer.renderer.graphics.flasher;
	//											newRenderer.RenderPawnAt(thing.DrawPos, curLevel, baseLevel);
	//											cachedCorpseRenderers[corpse.InnerPawn] = newRenderer;
	//										}
	//									}
	//									else if (thing.def.projectile == null)
	//									{
	//										Vector2 drawSize = thing.Graphic.drawSize;
	//										drawSize.x *= 1f - (((float)(curLevel) - (float)baseLevel) / 5f);
	//										drawSize.y *= 1f - (((float)(curLevel) - (float)baseLevel) / 5f);
	//										var newGraphic = thing.Graphic.GetCopy(drawSize);
	//										newGraphic.Draw(thing.DrawPos, thing.Rotation, thing);
	//									}
	//									else
	//									{
	//										thing.Draw();
	//									}
	//								}
	//								catch (Exception ex)
	//								{
	//									Log.Error(string.Concat(new object[]
	//									{
	//										"Exception drawing ",
	//										thing,
	//										": ",
	//										ex.ToString()
	//									}), false);
	//								}
	//								DrawPos_Patch.ChangeDrawPos = false;
	//								TrueCenter_Patch.ChangeDrawPos = false;
	//							}
	//						}
	//					}
	//					___drawingNow = false;
	//				}
	//			}
	//		}
	//	}
	//}
}

