using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ZLevels
{
    [StaticConstructorOnStartup]
    public static class JobLogging
    {
        static JobLogging()
        {
            foreach (var type in typeof(JobDriver).AllSubclassesNonAbstract())
            {
                var method = AccessTools.Method(type, "TryMakePreToilReservations");
                if (method != null)
                {
                    try
                    {
                        ZLevelsMod.harmony.Patch(method, new HarmonyMethod(AccessTools.Method(typeof(JobLogging), nameof(TryMakePreToilReservationsLog))));
                    }
                    catch { };
                }
            }
        }

        public static void TryMakePreToilReservationsLog(JobDriver __instance)
        {
            ZLogger.Message(__instance.pawn + " is doing TryMakePreToilReservations, job: " + __instance.job + " - jobdriver: " + __instance);
        }
    }

    [HarmonyPatch(typeof(Log), nameof(Log.Error), new Type[] { typeof(string) })]
    static class Log_Error_Patch
    {
        public static bool Prefix(string text)
        {
            if (text != null)
            {
                if (ZLogger.DebugEnabled && Current.gameInt?.tickManager != null)
                {
                    Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
                }
            }
            else
            {
                return false;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(Log))]
    [HarmonyPatch(nameof(Log.Notify_MessageReceivedThreadedInternal))]
    static class Notify_MessageReceivedThreadedInternal_Patch
    {
        public static bool Prefix()
        {
            if (ZLogger.DebugEnabled)
            {
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Map))]
    [HarmonyPatch(nameof(Map.ToString))]
    static class Map_ToString
    {
        public static void Postfix(Map __instance, ref string __result)
        {
            if (ZLogger.DebugEnabled)
            {
                __result = "(" + __result + " - Level " + ZUtils.ZTracker.GetZIndexFor(__instance) + ")";
            }
        }
    }

    [HarmonyPatch(typeof(WildAnimalSpawner))]
    [HarmonyPatch(nameof(WildAnimalSpawner.WildAnimalSpawnerTick))]
    static class WildAnimalSpawner_WildAnimalSpawnerTick
    {
        public static bool Prefix()
        {
            if (ZLogger.DebugEnabled)
            {
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Pawn))]
    [HarmonyPatch(nameof(Pawn.SpawnSetup))]
    static class Pawn_SpawnSetup
    {
        public static bool Prefix(Pawn __instance)
        {
            if (ZLogger.DebugEnabled)
            {
                if (__instance.RaceProps.Animal)
                {
                    Log.Message("Spawning: " + __instance);
                    return false;
                }
            }
            return true;
        }
    }            
}

