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

		public static float zLevelOffset = 0f;
		public static float yLevelOffset = 0f;
		public static void Postfix(ref Vector3 __result)
		{
			if (ChangeDrawPos)
			{
				__result.z += zLevelOffset;
				__result.y += yLevelOffset;
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
		public static void Postfix(ref Vector3 __result)
        {
			if (DrawPos_Patch.ChangeDrawPos)
			{
				__result.z += DrawPos_Patch.zLevelOffset;
				__result.y += DrawPos_Patch.yLevelOffset;
			}
		}
    }


	[HarmonyPatch(typeof(DynamicDrawManager), "DrawDynamicThings")]
	public static class DrawDynamicThings
	{
		private static Dictionary<Pawn, PawnRendererScaled> cachedPawnRenderers = new Dictionary<Pawn, PawnRendererScaled>();
		private static Dictionary<Pawn, PawnRendererScaled> cachedCorpseRenderers = new Dictionary<Pawn, PawnRendererScaled>();
		private static Dictionary<Thing, Dictionary<int, Graphic>> cachedGraphics = new Dictionary<Thing, Dictionary<int, Graphic>>();

		[TweakValue("0ZLevels", 0, 20)] public static float turretDrawZOffset = 1.9f;
		[TweakValue("0ZLevels", 0, 20)] public static float globalZOffset = 3.95f;
		public static void Postfix(DynamicDrawManager __instance, Map ___map, ref bool ___drawingNow)
		{
			var ZTracker = ZUtils.ZTracker;
			int curLevel = ZTracker.GetZIndexFor(___map);
			if (curLevel > 0)
            {
				foreach (var map2 in ZTracker.GetAllMaps(___map.Tile))//.OrderBy(x => ZTracker.GetZIndexFor(x)))
				{
					int baseLevel = ZTracker.GetZIndexFor(map2);
					if (curLevel > baseLevel && baseLevel >= 0)
					{
						___drawingNow = true;
						bool[] fogGrid = map2.fogGrid.fogGrid;
						CellRect cellRect = Find.CameraDriver.CurrentViewRect;
						cellRect.ClipInsideMap(map2);
						cellRect = cellRect.ExpandedBy(1);
						CellIndices cellIndices = map2.cellIndices;
						foreach (Thing thing in map2.dynamicDrawManager.drawThings)
						{

							if (thing.def.drawerType != DrawerType.None && thing.def.drawerType != DrawerType.RealtimeOnly)
                            {
								if (thing is Building_TurretGun turretGun)
								{
									var turretDrawPos = turretGun.DrawPos;
									turretDrawPos.y += Math.Abs(baseLevel - curLevel);
									turretDrawPos.z += (baseLevel - curLevel) / turretDrawZOffset;
									Vector3 b = new Vector3(turretGun.def.building.turretTopOffset.x, 0f, turretGun.def.building.turretTopOffset.y).RotatedBy(turretGun.top.CurRotation);
									float turretTopDrawSize = turretGun.top.parentTurret.def.building.turretTopDrawSize
										* (1f - (((float)(curLevel) - (float)baseLevel) / 5f));
									Matrix4x4 matrix = default(Matrix4x4);
									matrix.SetTRS(turretDrawPos + Altitudes.AltIncVect + b, (turretGun.top.CurRotation + (float)TurretTop.ArtworkRotation).ToQuat(), new Vector3(turretTopDrawSize, 1f, turretTopDrawSize));
									Graphics.DrawMesh(MeshPool.plane10, matrix, turretGun.def.building.turretTopMat, 0);
								}
								continue;
							}
							IntVec3 position = thing.Position;
							IntVec3 position2 = position + new IntVec3(0, 0, -1);
							var positionTerrain = position.GetTerrain(___map);
							if (positionTerrain == ZLevelsDefOf.ZL_OutsideTerrain || position2.InBounds(___map) && positionTerrain != ZLevelsDefOf.ZL_OutsideTerrain && position2.GetTerrain(___map) == ZLevelsDefOf.ZL_OutsideTerrain)
							{
								if ((cellRect.Contains(position) || thing.def.drawOffscreen)
									//&& (!fogGrid[cellIndices.CellToIndex(position)]
									//|| thing.def.seeThroughFog) 
									&& (thing.def.hideAtSnowDepth >= 1f
									|| map2.snowGrid.GetDepth(position) <= thing.def.hideAtSnowDepth))
								{
									DrawPos_Patch.ChangeDrawPos = true;
									DrawPos_Patch.zLevelOffset = (baseLevel - curLevel) / globalZOffset;
									DrawPos_Patch.yLevelOffset = baseLevel - curLevel;
									try
									{
										var graphicType = thing.Graphic.GetType();
										if (graphicType == typeof(Graphic_Mote))
										{
											
										}
										else if (graphicType == typeof(Graphic_LinkedCornerFiller) || graphicType == typeof(Graphic_RandomRotated) || graphicType == typeof(Graphic_Linked))
										{
											thing.Draw();
										}
										else if (thing is Pawn pawn)
										{
											DrawPos_Patch.ChangeDrawPos = false;
											var newDrawPos = thing.DrawPos;
											newDrawPos.z += (baseLevel - curLevel) / 2f;
											newDrawPos.y -= baseLevel - curLevel;
											newDrawPos.y -= 4.1f;
											if (cachedPawnRenderers.TryGetValue(pawn, out var pawnRenderer))
											{
												pawnRenderer.RenderPawnAt(newDrawPos, curLevel, baseLevel);
											}
											else
											{
												var newRenderer = new PawnRendererScaled(pawn, pawn.Drawer.renderer.wiggler);
												pawn.Drawer.renderer.graphics.ResolveAllGraphics();
												newRenderer.graphics.nakedGraphic = pawn.Drawer.renderer.graphics.nakedGraphic;
												newRenderer.graphics.headGraphic = pawn.Drawer.renderer.graphics.headGraphic;
												newRenderer.graphics.hairGraphic = pawn.Drawer.renderer.graphics.hairGraphic;
												newRenderer.graphics.rottingGraphic = pawn.Drawer.renderer.graphics.rottingGraphic;
												newRenderer.graphics.dessicatedGraphic = pawn.Drawer.renderer.graphics.dessicatedGraphic;
												newRenderer.graphics.apparelGraphics = pawn.Drawer.renderer.graphics.apparelGraphics;
												newRenderer.graphics.packGraphic = pawn.Drawer.renderer.graphics.packGraphic;
												newRenderer.graphics.flasher = pawn.Drawer.renderer.graphics.flasher;
												newRenderer.RenderPawnAt(newDrawPos, curLevel, baseLevel);
												cachedPawnRenderers[pawn] = newRenderer;
											}
										}
										else if (thing is Corpse corpse)
										{
											DrawPos_Patch.ChangeDrawPos = false;
											var newDrawPos = thing.DrawPos;
											newDrawPos.z += (baseLevel - curLevel) / 2f;
											newDrawPos.y -= baseLevel - curLevel;
											newDrawPos.y -= 4f;
											if (cachedCorpseRenderers.TryGetValue(corpse.InnerPawn, out var pawnRenderer))
											{
												pawnRenderer.RenderPawnAt(newDrawPos, curLevel, baseLevel);
											}
											else
											{
												var newRenderer = new PawnRendererScaled(corpse.InnerPawn, corpse.InnerPawn.Drawer.renderer.wiggler);
												corpse.InnerPawn.Drawer.renderer.graphics.ResolveAllGraphics();
												newRenderer.graphics.nakedGraphic = corpse.InnerPawn.Drawer.renderer.graphics.nakedGraphic;
												newRenderer.graphics.headGraphic = corpse.InnerPawn.Drawer.renderer.graphics.headGraphic;
												newRenderer.graphics.hairGraphic = corpse.InnerPawn.Drawer.renderer.graphics.hairGraphic;
												newRenderer.graphics.rottingGraphic = corpse.InnerPawn.Drawer.renderer.graphics.rottingGraphic;
												newRenderer.graphics.dessicatedGraphic = corpse.InnerPawn.Drawer.renderer.graphics.dessicatedGraphic;
												newRenderer.graphics.apparelGraphics = corpse.InnerPawn.Drawer.renderer.graphics.apparelGraphics;
												newRenderer.graphics.packGraphic = corpse.InnerPawn.Drawer.renderer.graphics.packGraphic;
												newRenderer.graphics.flasher = corpse.InnerPawn.Drawer.renderer.graphics.flasher;
												newRenderer.RenderPawnAt(newDrawPos, curLevel, baseLevel);
												cachedCorpseRenderers[corpse.InnerPawn] = newRenderer;
											}
										}
										else if (thing.def.projectile == null && !thing.def.IsDoor)
										{
											if (!cachedGraphics.TryGetValue(thing, out var graphics))
											{
												graphics = new Dictionary<int, Graphic>();
												cachedGraphics[thing] = graphics;
											}
											if (!graphics.TryGetValue(curLevel, out var graphic))
											{
												Vector2 drawSize = thing.Graphic.drawSize;
												drawSize.x *= 1f - (((float)(curLevel) - (float)baseLevel) / 5f);
												drawSize.y *= 1f - (((float)(curLevel) - (float)baseLevel) / 5f);
												graphic = thing.Graphic.GetCopy(drawSize);
												graphics[curLevel] = graphic;
											}
											graphic.Draw(thing.DrawPos, thing.Rotation, thing);
										}
										else
										{
											if (thing is Building_Door door)
                                            {
												DrawPos_Patch.ChangeDrawPos = false;
												DrawDoor(door, baseLevel, curLevel);
											}
											else
                                            {
												thing.Draw();
                                            }
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
									DrawPos_Patch.ChangeDrawPos = false;
								}
							}
						}
						___drawingNow = false;
					}
				}
			}
		}

		public static void DrawDoor(Building_Door door, int baseLevel, int curLevel)
		{
			door.Rotation = Building_Door.DoorRotationAt(door.Position, door.Map);
			float num = Mathf.Clamp01((float)door.ticksSinceOpen / (float)door.TicksToOpenNow);
			float d = 0f + 0.45f * num;
			for (int i = 0; i < 2; i++)
			{
				Vector3 vector = default(Vector3);
				Mesh mesh;
				if (i == 0)
				{
					vector = new Vector3(0f, 0f, -1f);
					mesh = MeshPool.plane10;
				}
				else
				{
					vector = new Vector3(0f, 0f, 1f);
					mesh = MeshPool.plane10Flip;
				}
				Rot4 rotation = door.Rotation;
				rotation.Rotate(RotationDirection.Clockwise);
				vector = rotation.AsQuat * vector;
				Vector3 drawPos = door.DrawPos;
				drawPos.y = AltitudeLayer.DoorMoveable.AltitudeFor();
				drawPos += vector * d;
				drawPos.z += (baseLevel - curLevel) / 2f;
				drawPos.y -= (baseLevel - curLevel) / 2f + 10f;
				Graphics.DrawMesh(mesh, drawPos, door.Rotation.AsQuat, door.Graphic.MatAt(door.Rotation), 0);
			}
			door.Comps_PostDraw();
		}
	}
}

