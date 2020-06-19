//using System;
//using System.Reflection;
//using HarmonyLib;
//using UnityEngine;
//using Verse;
//
//namespace ZLevels
//{
//	[HarmonyPatch(typeof(SectionLayer), "FinalizeMesh", null)]
//	public static class GenerateSpaceSubMesh
//	{
//		[HarmonyPostfix]
//		public static bool GenerateMesh(SectionLayer __instance)
//		{
//			if (__instance.GetType().Name != "SectionLayer_Terrain")
//			{
//				return true;
//			}
//			Section section = __instance.GetType().GetField("section", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance) as Section;
//			var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
//			var lowerMap = ZTracker.GetLowerLevel(section.map.Tile, section.map);
//
//			if (lowerMap != null && ZTracker.GetZIndexFor(section.map) > 0)
//			{
//				foreach (IntVec3 intVec in section.CellRect.Cells)
//				{
//					if (section.map.terrainGrid.TerrainAt(intVec) == ZLevelsDefOf.ZL_OutsideTerrain)
//					{
//						var material = lowerMap.terrainGrid.TerrainAt(intVec).DrawMatSingle;
//
//						//Graphics.DrawMesh(MeshMakerPlanes.NewPlaneMesh(1f), intVec.ToVector3(), Quaternion.identity, material, 0);
//
//						Printer_Mesh.PrintMesh(__instance, intVec.ToVector3(),
//							MeshMakerPlanes.NewPlaneMesh(1f), material);
//						ZLogger.Message("Printing lower terrain at " + intVec);
//					}
//					else
//					{
//						ZLogger.Message(ZTracker.GetMapInfo(section.map) + " - " 
//							+ section.map.terrainGrid.TerrainAt(intVec) + " at " + intVec + " is not outside terrain");
//					}
//				}
//			}
//			return true;
//		}
//	}
//}
