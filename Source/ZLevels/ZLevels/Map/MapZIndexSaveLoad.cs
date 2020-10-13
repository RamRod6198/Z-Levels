using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace ZLevels
{

    [HarmonyPatch(typeof(Map), "ExposeData")]
    internal static class Patch_ExposeData
    {
        private static void Postfix(Map __instance)
        {
            __instance.ZIndex = __instance.GetComponent<MapComponentZLevel>().Z_LevelIndex;
        }
    }
}
