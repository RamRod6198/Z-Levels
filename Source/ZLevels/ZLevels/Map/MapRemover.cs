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
	public class ColonistBarPatch
	{
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
						//Log.Message("Rect: " + ___cachedDrawLocs[i].x + " - " + ___cachedDrawLocs[i].y + " - " 
						//	+ __instance.Size.x + " - " + __instance.Size.y);
						Rect rect = new Rect(___cachedDrawLocs[i].x, ___cachedDrawLocs[i].y, 
							__instance.Size.x / 2, __instance.Size.y / 2);
						Texture2D AbandonButtonTex = ContentFinder<Texture2D>.Get("UI/Buttons/Abandon", true);
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
								var comp = map.GetComponent<MapComponentZLevel>();
								var pathToWrite = Path.Combine(Path.Combine(GenFilePaths.ConfigFolderPath,
									"SavedMaps"), map.Tile + " - " + comp.Z_LevelIndex + ".xml");
								if (map.listerThings.AllThings.Count > 0)
								{
									BlueprintUtility.SaveEverything(pathToWrite, map, "SavedMap");
									Log.Message("Removing map: " + map);
								}
								var parent = map.Parent as MapParent_ZLevel;
								var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
								parent.Abandon();
								ZTracker.ZLevelsTracker[map.Tile].ZLevels.Remove(comp.Z_LevelIndex);
								
								foreach (var map2 in Find.Maps)
								{
									var comp2 = map2.GetComponent<MapComponentZLevel>();
									if (ZTracker.ZLevelsTracker[map2.Tile] != null)
									{
										foreach (var d in ZTracker.ZLevelsTracker[map2.Tile].ZLevels)
										{
											Log.Message(map2 + ": " + d.Key + " - " + d.Value);
										}
									}
								}
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

