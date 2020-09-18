using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using static RimWorld.ColonistBar;

namespace ZLevels
{

    [HarmonyPatch(typeof(ColonistBarDrawLocsFinder), "CalculateColonistsInGroup")]
    public static class CalculateColonistsInGroupPatches
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            List<Entry> list = new List<Entry>();
            try
            {
                list = Find.ColonistBar.cachedEntries.OrderBy(x => x.map != null ? ZUtils.ZTracker.GetZIndexFor(x.map) : 9999).ToList();
            }
            catch { }
            finally 
            {
                Find.ColonistBar.cachedEntries = list;
            }
        }
    }

    [HarmonyPatch(typeof(ColonistBar), "ColonistBarOnGUI")]
    public static class ColonistBarOnGUIPatch
    {
        public static Texture2D AbandonButtonTex = ContentFinder<Texture2D>.Get("UI/Buttons/Abandon", true);
        [HarmonyPrefix]
        public static void Prefix(ColonistBar __instance, ref List<ColonistBar.Entry> ___cachedEntries, 
            ref List<Vector2> ___cachedDrawLocs)
        {
            try
            {
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
                                        var comp = ZUtils.GetMapComponentZLevel(map);
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
                                            var comp2 = ZUtils.GetMapComponentZLevel(map2);
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
                                        var comp = ZUtils.GetMapComponentZLevel(map);
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
                                            var comp2 = ZUtils.GetMapComponentZLevel(map2);
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

        private static bool ShowGroupFrames(List<Entry> entries)
        {
            int num = -1;
            for (int i = 0; i < entries.Count; i++)
            {
                num = Mathf.Max(num, entries[i].group);
            }
            return num >= 1;
        }
    }
}