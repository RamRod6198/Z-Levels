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
	[HarmonyPatch(typeof(GenView), "ShouldSpawnMotesAt", new Type[] { typeof(IntVec3), typeof(Map)})]
	public static class ShouldSpawnMotesAt_Patch
	{
		private static CellRect viewRect;
		public static bool Prefix(ref bool __result, IntVec3 loc, Map map)
		{
			if (map != null)
            {
				if (map.Tile != Find.CurrentMap?.Tile)
				{
					__result = false;
					return false;
				}
				if (!loc.InBounds(map))
				{
					__result = false;
					return false;
				}
				viewRect = Find.CameraDriver.CurrentViewRect;
				viewRect = viewRect.ExpandedBy(5);
				__result = viewRect.Contains(loc);
				return false;
			}
			return true;
		}
	}

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

	[HarmonyPatch(typeof(Mote), "DrawPos", MethodType.Getter)]
	public static class Mote_DrawPos_Patch
	{
		public static void Postfix(Mote __instance, ref Vector3 __result)
		{
			if (DrawPos_Patch.ChangeDrawPos)
			{
				__result.z += DrawPos_Patch.zLevelOffset;
				__result.y += DrawPos_Patch.yLevelOffset;
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
		private static Dictionary<Thing, Dictionary<int, Graphic>> cachedGraphics = new Dictionary<Thing, Dictionary<int, Graphic>>();

		[TweakValue("0ZLevels", 0, 20)] public static float turretDrawZOffset = 1.9f;
		[TweakValue("0ZLevels", 0, 20)] public static float globalZOffset = 3.178f;
		[TweakValue("0ZLevels", 0, 20)] public static float pawnYOffset = 4.1f;
		[TweakValue("0ZLevels", 0, 20)] public static float moteZOffset = 1.614f;
		[TweakValue("0ZLevels", 0, 20)] public static float pawnZOffset = 1.614f;
		[TweakValue("0ZLevels", 0, 20)] public static float thingDrawScale = 10f;
		[TweakValue("0ZLevels", 0, 20)] public static float pawnDrawScale = 10f;

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
										if (graphicType == typeof(Graphic_LinkedCornerFiller) || graphicType == typeof(Graphic_Linked))
										{
											thing.Draw();
										}
										else if (thing is Pawn pawn)
										{
											DrawPos_Patch.ChangeDrawPos = false;
											var newDrawPos = thing.DrawPos;
											newDrawPos.z += (baseLevel - curLevel) / pawnZOffset;
											newDrawPos.y -= baseLevel - curLevel;
											newDrawPos.y -= pawnYOffset;
											// TODO Transpile PawnRenderer
										}
										else if (thing is Corpse corpse)
										{
											DrawPos_Patch.ChangeDrawPos = false;
											var newDrawPos = thing.DrawPos;
											newDrawPos.z += (baseLevel - curLevel) / pawnZOffset;
											newDrawPos.y -= baseLevel - curLevel;
											newDrawPos.y -= 4f;
											// TODO Transpile PawnRenderer
										}
										else if (thing.def.projectile == null && !thing.def.IsDoor)
										{
											if (!cachedGraphics.TryGetValue(thing, out var graphics))
											{
												graphics = new Dictionary<int, Graphic>();
												cachedGraphics[thing] = graphics;
											}
											if (!graphics.TryGetValue(curLevel, out var graphic) || true)
											{
												Vector2 drawSize = thing.Graphic.drawSize;
												drawSize.x *= 1f - ((curLevel - baseLevel) / thingDrawScale);
												drawSize.y *= 1f - ((curLevel - baseLevel) / thingDrawScale);

												if (thing.Graphic is Graphic_RandomRotated graphicRandomRotated)
												{
													graphic = graphicRandomRotated.subGraphic.GetCopy(drawSize, graphicRandomRotated.Shader);
													graphic.data = graphicRandomRotated.subGraphic.data;
												}
												else
                                                {
													graphic = thing.Graphic.GetCopy(drawSize, thing.Graphic.Shader);
													graphic.data = thing.Graphic.data;
                                                }
												graphic.data.drawSize = drawSize;
												graphics[curLevel] = graphic;
											}

											if (thing.def.mote != null)
                                            {
												DrawPos_Patch.zLevelOffset = (baseLevel - curLevel) / moteZOffset;
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
										}));
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

