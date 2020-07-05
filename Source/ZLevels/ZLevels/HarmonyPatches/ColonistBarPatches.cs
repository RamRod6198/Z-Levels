using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace ZLevels
{
    [HarmonyPatch(typeof(ColonistBar), "CheckRecacheEntries")]
    public static class ColonistBarPatches
    {
        public static bool entriesDirty = false;

        [HarmonyPrefix]
        public static void Prefix(ColonistBar __instance, bool ___entriesDirty)
        {
            if (___entriesDirty) 
            {
                entriesDirty = true;
            }
        }
    }

    [HarmonyPatch(typeof(ColonistBar), "ColonistBarOnGUI")]
    public static class ColonistBarOnGUIPatch
    {
        public static Texture2D AbandonButtonTex = ContentFinder<Texture2D>.Get("UI/Buttons/Abandon", true);
        [HarmonyPostfix]
        public static void Postfix(ColonistBar __instance, ref List<ColonistBar.Entry> ___cachedEntries, ref ColonistBarDrawLocsFinder ___drawLocsFinder, ref List<Vector2> ___cachedDrawLocs, ref float ___cachedScale)
        {
            try
            {
                if (ColonistBarPatches.entriesDirty)
                {
                    ColonistBarPatches.entriesDirty = false;
                    ___cachedEntries = ___cachedEntries.OrderBy(x => ZUtils.ZTracker.GetZIndexFor(x.map)).ToList();
                }
                if (___cachedDrawLocs.Count == ___cachedEntries.Count)
                {
                    for (int i = 0; i < ___cachedDrawLocs.Count; i++)
                    {
                        if (___cachedEntries[i].pawn == null && ___cachedEntries[i].map.Parent is MapParent_ZLevel)
                        {
                            //ZLogger.Message("Rect: " + ___cachedDrawLocs[i].x + " - " + ___cachedDrawLocs[i].y + " - "
                            //      + __instance.Size.x + " - " + __instance.Size.y);
                            Rect rect = new Rect(___cachedDrawLocs[i].x + (__instance.Size.x / 1.25f),
                                    ___cachedDrawLocs[i].y + (__instance.Size.y / 1.25f),
                                    __instance.Size.x / 3, __instance.Size.y / 3);
                            GUI.DrawTexture(rect, AbandonButtonTex);
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
            }
            catch { };
        }
    }
}