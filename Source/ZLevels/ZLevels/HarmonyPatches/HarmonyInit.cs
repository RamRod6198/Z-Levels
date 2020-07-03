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
            public static bool Prefix(string text, ref bool ignoreStopLoggingLimit)
            {
                // somehow the game periodically gives this error message when pawns haul between maps
                // and I really don’t know where the source is and how to fix it. If you know how, then tell me
                // This error doesnt affect the hauling itself, maybe this error occurs after the completion of the hauling job
                if (text.Contains("System.Exception: StartCarryThing got availableStackSpace 0 for haulTarg")
                    || text.Contains("overwriting slot group square") // not really an error, this is what z-level needs to look for things for hauling
                    || text.Contains("clearing group grid square") // same
                    || text.Contains("threw exception while executing toil's finish action (0), jobDriver=RimWorld.JobDriver_LayDown")
                    || text.Contains("threw exception while executing toil's finish action (1), jobDriver=RimWorld.JobDriver_LayDown")
                    )
                {
                    //ZLogger.Message("The error: " + text);
                    return false;
                }

                //try
                //{
                //    Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
                //}
                //catch { };
                //ignoreStopLoggingLimit = true;
                return true;
            }
        }

        //[HarmonyPatch(typeof(PawnRenderer), "DrawEquipment")]
        //public static class AvP_PawnRenderer_DrawEquipment_Cloak_Patch
        //{
        //    public static bool Prefix(PawnRenderer __instance)
        //    {
        //        return false;
        //    }
        //}

        [HarmonyPatch(typeof(ColonistBarColonistDrawer), "DrawIcons")]
        public static class DrawIcons_Patch
        {
            public static bool Prefix(Rect rect, Pawn colonist)
            {
                try
                {
                    if (colonist.CurJob != null && colonist.jobs.curDriver.asleep)
                    {

                    }
                }
                catch
                {
                    colonist.jobs.EndCurrentJob(JobCondition.Errored);
                }

                //Log.Message("colonist.CurJob != null && colonist.jobs.curDriver.asleep: "
                //    + (colonist.CurJob != null && colonist.jobs.curDriver != null).ToString());
                //
                //Log.Message("colonist.CurJob != null && colonist.jobs.curDriver.asleep: " 
                //    + (colonist.CurJob != null && colonist.jobs.curDriver.asleep).ToString());
                //
                //
                //Log.Message("colonist.InAggroMentalState: " + colonist.InAggroMentalState);
                //Log.Message("colonist.InMentalState: " + colonist.InMentalState);
                //Log.Message("colonist.InBed() &&  && colonist.CurrentBed().Medical: " 
                //    + (colonist.InBed() && colonist.CurrentBed().Medical).ToString());
                //
                //
                //Log.Message("colonist.mindState.IsIdle: " + colonist.mindState.IsIdle);
                //Log.Message("colonist.IsBurning(): " + colonist.IsBurning());
                //Log.Message("colonist.Inspired: " + colonist.Inspired);

                return true;
            }
        }
    }
}

