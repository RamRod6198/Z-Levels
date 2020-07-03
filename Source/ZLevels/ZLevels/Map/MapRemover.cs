using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace ZLevels
{
	[HarmonyPatch(typeof(ColonistBar), "CheckRecacheEntries")]
	public static class ColonistBarPatch
	{
		public static Texture2D AbandonButtonTex = ContentFinder<Texture2D>.Get("UI/Buttons/Abandon", true);

		[HarmonyPostfix]
		public static void Listener(ColonistBar __instance, ref List<ColonistBar.Entry> 
			___cachedEntries, ref ColonistBarDrawLocsFinder ___drawLocsFinder, ref List<Vector2> 
			___cachedDrawLocs, ref float ___cachedScale)
		{
			try
			{
				for (int i = 0; i < ___cachedDrawLocs.Count; i++)
				{
					if (___cachedEntries[i].pawn == null && ___cachedEntries[i].map.Parent is MapParent_ZLevel)
					{
						//ZLogger.Message("Rect: " + ___cachedDrawLocs[i].x + " - " + ___cachedDrawLocs[i].y + " - " 
						//	+ __instance.Size.x + " - " + __instance.Size.y);
						Rect rect = new Rect(___cachedDrawLocs[i].x + (__instance.Size.x / 2f),
							___cachedDrawLocs[i].y + (__instance.Size.y / 2f), __instance.Size.x / 2, __instance.Size.y / 2);
						Matrix4x4 matrix = GUI.matrix;
						Color color2 = GUI.color;
						GUI.DrawTexture(rect, AbandonButtonTex);
						GUI.color = color2;
						GUI.matrix = matrix;
						if (Mouse.IsOver(rect))
						{
							if (Input.GetMouseButtonDown(0) && ___cachedEntries[i].map != null)
							{
								Map map = ___cachedEntries[i].map;
								Find.WindowStack.Add(new Dialog_MessageBox("ZAbandonConfirmation".Translate(), "Yes".Translate(), delegate ()
								{
									var comp = map.GetComponent<MapComponentZLevel>();
									var pathToWrite = Path.Combine(Path.Combine(GenFilePaths.ConfigFolderPath,
										"SavedMaps"), map.Tile + " - " + comp.Z_LevelIndex + ".xml");
									if (map.listerThings.AllThings.Count > 0)
									{
										BlueprintUtility.SaveEverything(pathToWrite, map, "SavedMap");
										ZLogger.Message("Removing map: " + map);
									}
									var parent = map.Parent as MapParent_ZLevel;
									var ZTracker = ZUtils.ZTracker;
									parent.Abandon();
									ZTracker.ZLevelsTracker[map.Tile].ZLevels.Remove(comp.Z_LevelIndex);

									foreach (var map2 in Find.Maps)
									{
										var comp2 = map2.GetComponent<MapComponentZLevel>();
										if (ZTracker.ZLevelsTracker[map2.Tile] != null)
										{
											foreach (var d in ZTracker.ZLevelsTracker[map2.Tile].ZLevels)
											{
												ZLogger.Message(map2 + ": " + d.Key + " - " + d.Value);
											}
										}
									}
								}, "No".Translate(), null, null, false, null, null));
							}
							else if (Input.GetMouseButtonDown(1) && ___cachedEntries[i].map != null)
							{
								Map map = ___cachedEntries[i].map;
								Find.WindowStack.Add(new Dialog_MessageBox("ZAbandonPermanentlyConfirmation".Translate(), "Yes".Translate(), delegate ()
								{
									var comp = map.GetComponent<MapComponentZLevel>();
									try
									{
										var pathToDelete = Path.Combine(Path.Combine(GenFilePaths.ConfigFolderPath,
										"SavedMaps"), map.Tile + " - " + comp.Z_LevelIndex + ".xml");
										var file = new FileInfo(pathToDelete);
										file.Delete();
									}
									catch
									{

									}

									var parent = map.Parent as MapParent_ZLevel;
									var ZTracker = ZUtils.ZTracker;
									parent.Abandon();
									ZTracker.ZLevelsTracker[map.Tile].ZLevels.Remove(comp.Z_LevelIndex);

									foreach (var map2 in Find.Maps)
									{
										var comp2 = map2.GetComponent<MapComponentZLevel>();
										if (ZTracker.ZLevelsTracker[map2.Tile] != null)
										{
											foreach (var d in ZTracker.ZLevelsTracker[map2.Tile].ZLevels)
											{
												ZLogger.Message(map2 + ": " + d.Key + " - " + d.Value);
											}
										}
									}
								}, "No".Translate(), null, null, false, null, null));
							}
						}
					}
				}
			}
			catch (Exception ex)
			{

			}
		}
	}
}

