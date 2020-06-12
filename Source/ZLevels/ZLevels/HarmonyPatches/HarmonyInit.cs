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
    internal static class HarmonyInit
    {
        static HarmonyInit()
        {
            new Harmony("ZLevels.Mod").PatchAll();
        }

        [HarmonyPatch(typeof(Log))]
        [HarmonyPatch(nameof(Log.Error))]
        static class Log_Error_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(string text, bool ignoreStopLoggingLimit = false)
            {
                // somehow the game periodically gives this error message when pawns haul between maps
                // and I really don’t know where the source is and how to fix it. If you know how, then tell me
                // This error doesnt affect the hauling itself, maybe this error occurs after the completion of the hauling job
                if (text.Contains("System.Exception: StartCarryThing got availableStackSpace 0 for haulTarg")
                    || text.Contains("overwriting slot group square") // not really an error, this is what z-level needs to look for things for hauling
                    || text.Contains("clearing group grid square") // same
                    || text.Contains("threw exception while executing toil's finish action (0), jobDriver=RimWorld.JobDriver_LayDown")
                    )
                {
                    //ZLogger.Message("The error: " + text);
                    return false;
                }
                return true;
            }
        }
    }
}

