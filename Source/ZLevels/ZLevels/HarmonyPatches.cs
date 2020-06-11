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

namespace ZLevels
{
    [StaticConstructorOnStartup]
    public static class JobManagerPatches
    {
        public static bool manualDespawn = false;


        [HarmonyPatch(typeof(WildAnimalSpawner), "SpawnRandomWildAnimalAt")]
        public class AnimalPatch2
        {
            [HarmonyPrefix]
            private static bool AnimalRemovalPrefix(WildAnimalSpawner __instance, ref IntVec3 loc, ref bool __result)
            {
                Map map = Traverse.Create(__instance).Field("map").GetValue<Map>();
                bool result = false;
                var comp = map.GetComponent<MapComponentZLevel>();
                var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                if (map.Parent is MapParent_ZLevel && comp != null 
                    && ZTracker.GetUpperLevel(map.Tile, map) != null &&
                    !ZTracker.GetUpperLevel(map.Tile, map).GetComponent<MapComponentZLevel>()
                    .hasCavesBelow.GetValueOrDefault(false))
                {
                    result = false;
                }
                else
                {
                    PawnKindDef pawnKindDef = (from a in map.Biome.AllWildAnimals
                                               where map.mapTemperature.SeasonAcceptableFor(a.race) select a)
                                               .RandomElementByWeight((PawnKindDef def) =>
                                               map.Biome.CommonalityOfAnimal(def) / def.wildGroupSize.Average);
                    if (pawnKindDef == null)
                    {
                        Log.Error("No spawnable animals right now.", false);
                        result = false;
                    }
                    else
                    {
                        int randomInRange = pawnKindDef.wildGroupSize.RandomInRange;
                        int radius = Mathf.CeilToInt(Mathf.Sqrt((float)pawnKindDef.wildGroupSize.max));
                        if (map.Parent is MapParent_ZLevel && !loc.Walkable(map))
                        {
                            loc = CellFinderLoose.RandomCellWith((IntVec3 sq) => sq.Walkable(map), map);
                        }
                        for (int i = 0; i < randomInRange; i++)
                        {
                            IntVec3 loc2 = CellFinder.RandomClosewalkCellNear(loc, map, radius, null);
                            GenSpawn.Spawn(PawnGenerator.GeneratePawn(pawnKindDef, null), loc2, map, WipeMode.Vanish);
                        }
                        result = true;
                    }
                }
                __result = result;
                return false;
            }
        }

        [HarmonyPatch(typeof(IncidentWorker_Infestation), "TryExecuteWorker")]
        internal class Patch_Infestation_TryExecuteWorker
        {
            [HarmonyPrefix]
            private static bool PreFix(ref bool __result, IncidentParms parms)
            {
                Map map = (Map)parms.target;
                var comp = map.GetComponent<MapComponentZLevel>();
                if (comp.hasCavesBelow.HasValue && comp.hasCavesBelow.Value)
                {
                    var foods = map.listerThings.AllThings.Where(x => !(x is Plant) && !(x is Pawn)
                            && x.GetStatValue(StatDefOf.Nutrition, true) > 0.1f);
                    if (foods != null && foods.Count() > 0)
                    {
                        List<PawnKindDef> infestators = new List<PawnKindDef>();
                        infestators.Add(ZLevelsDefOf.ZL_UndegroundBiome.AllWildAnimals.RandomElement());
                        var infestationPlace = foods.RandomElement().Position;
                        var infestationData = new InfestationData(infestators, parms.points, infestationPlace);
                        if (comp.ActiveInfestations == null)
                        {
                            comp.ActiveInfestations = new List<InfestationData>();
                        }
                        comp.ActiveInfestations.Add(infestationData);
                        if (comp.TotalInfestations == null)
                        {
                            comp.TotalInfestations = new List<InfestationData>();
                        }
                        comp.TotalInfestations.Add(infestationData);
                        var naturalHole = (Building_StairsDown)ThingMaker.MakeThing(ZLevelsDefOf.ZL_NaturalHole);
                        naturalHole.infestationData = infestationData;
                        GenSpawn.Spawn(naturalHole, infestationPlace, map, WipeMode.Vanish);
                        Find.LetterStack.ReceiveLetter("ZLevelInfestation"
                            .Translate(infestators.RandomElement().race.label), "ZLevelInfestationDesc".Translate(),
                            LetterDefOf.ThreatBig, naturalHole);
                    }
                }
                else
                {
                    Log.Message("The map has no caves below to generate infestation");
                }
                __result = false;
                return false;
            }
        }

        //static JobManagerPatches()
        //{
        //    MethodInfo method = typeof(JobManagerPatches).GetMethod("LogScanner");
        //    MethodInfo method2 = typeof(JobManagerPatches).GetMethod("LogScanner2");
        //    foreach (Type type in GenTypes.AllSubclasses(typeof(WorkGiver_Scanner)))
        //    {
        //        MethodInfo getMethod = type.GetMethod("PotentialWorkThingsGlobal");
        //        try
        //        {
        //            new Harmony("test.test.tst").Patch(getMethod, null, new HarmonyMethod(method), null, null);
        //            Log.Message("Patching: " + type);
        //        }
        //        catch { }
        //
        //        MethodInfo getMethod2 = type.GetProperty("PotentialWorkThingRequest").GetGetMethod(false);
        //        try
        //        {
        //            new Harmony("test.test.tst").Patch(getMethod2, null, new HarmonyMethod(method2), null, null);
        //            Log.Message("Patching: " + type);
        //        }
        //        catch { }
        //    }
        //
        //}
        //
        //public static void LogScanner(IEnumerable<Thing> __result, Pawn pawn, WorkGiver_Scanner __instance)
        //{
        //    if (__result != null)
        //    {
        //        Log.Message(pawn + " - " + __instance.def + " - count: " + __result.Count());
        //    }
        //    else
        //    {
        //        Log.Message(pawn + " - " + __instance.def + " - null count");
        //    }
        //}
        //
        //public static void LogScanner2(ThingRequest __result, WorkGiver_Scanner __instance)
        //{
        //    Log.Message(__instance.def + " - __result: " + __result);
        //    //throw new Exception("TEST");
        //}
        //
        //
        //[HarmonyPatch(typeof(JobQueue), "EnqueueFirst")]
        //internal static class Patch_JobQueue
        //{
        //    private static void Postfix(Job j, JobTag? tag = null)
        //    {
        //        Log.Message("Switching to " + j, true);
        //    }
        //}

        //[HarmonyPatch(typeof(JobQueue), "EnqueueLast")]
        //internal static class Patch_JobQueue2
        //{
        //    private static void Postfix(Job j, JobTag? tag = null)
        //    {
        //        Log.Message("Switching to " + j, true);
        //    }
        //}
        //
        //[HarmonyPatch(typeof(JobGiver_Work), "GiverTryGiveJobPrioritized")]
        //internal static class Patch_JobGiver_Work
        //{
        //    private static void Postfix(Pawn pawn, WorkGiver giver, IntVec3 cell)
        //    {
        //        Log.Message("Switching to " + pawn, true);
        //    }
        //}

        [HarmonyPatch(typeof(Pawn), "VerifyReservations")]
        internal static class Patch_VerifyReservations
        {
            private static bool Prefix(Pawn __instance)
            {
                try
                {
                    if (__instance.jobs == null)
                    {
                        return false;
                    }
                    if (__instance.CurJob != null || __instance.jobs.jobQueue.Count > 0 || __instance.jobs.startingNewJob)
                    {
                        return false;
                    }
                    bool flag = false;
                    List<Map> maps = Find.Maps;
                    for (int i = 0; i < maps.Count; i++)
                    {
                        LocalTargetInfo obj = maps[i].reservationManager.FirstReservationFor(__instance);
                        if (obj.IsValid)
                        {
                            flag = true;
                        }
                        LocalTargetInfo obj2 = maps[i].physicalInteractionReservationManager.FirstReservationFor(__instance);
                        if (obj2.IsValid)
                        {
                            Log.ErrorOnce(string.Format("Physical interaction reservation manager failed to clean up properly; {0} still reserving {1}", __instance.ToStringSafe<Pawn>(), obj2.ToStringSafe<LocalTargetInfo>()), 19586765 ^ __instance.thingIDNumber, false);
                            flag = true;
                        }
                        IAttackTarget attackTarget = maps[i].attackTargetReservationManager.FirstReservationFor(__instance);
                        if (attackTarget != null)
                        {
                            Log.ErrorOnce(string.Format("Attack target reservation manager failed to clean up properly; {0} still reserving {1}", __instance.ToStringSafe<Pawn>(), attackTarget.ToStringSafe<IAttackTarget>()), 100495878 ^ __instance.thingIDNumber, false);
                            flag = true;
                        }
                        IntVec3 obj3 = maps[i].pawnDestinationReservationManager.FirstObsoleteReservationFor(__instance);
                        if (obj3.IsValid)
                        {
                            flag = true;
                        }
                    }
                    if (flag)
                    {
                        __instance.ClearAllReservations(true);
                    }
                }
                catch
                {

                }
                return false;
            }
        }

        [HarmonyPatch(typeof(WeatherDecider), "ChooseNextWeather")]
        internal static class Patch_ChooseNextWeather
        {
            private static void Postfix(WeatherDecider __instance, WeatherDef __result)
            {
                try
                {
                    Map map = Traverse.Create(__instance).Field("map").GetValue<Map>();
                    var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                    Log.Message("Weather decider: " + __result + " - " + ZTracker.GetMapInfo(map));
                    if (ZTracker.GetZIndexFor(map) == 0)
                    {
                        foreach (var map2 in ZTracker.GetAllMaps(map.Tile))
                        {
                            if (ZTracker.GetZIndexFor(map2) > 0)
                            {
                                Log.Message("1 - " + ZTracker.GetMapInfo(map2) + " transitioting to " + __result);
                                map2.weatherManager.TransitionTo(__result);
                                map2.weatherManager.curWeatherAge = map.weatherManager.curWeatherAge;
                            }
                        }
                    }
                    else if (ZTracker.GetZIndexFor(map) > 0)
                    {
                        __result = map.weatherManager.curWeather;
                        Log.Message("2 - " + ZTracker.GetMapInfo(map) + " transitioting to " + __result);
        
                        map.weatherManager.TransitionTo(__result);
                    }
                    else if (ZTracker.GetZIndexFor(map) < 0)
                    {
                        __result = WeatherDefOf.Clear;
                        Log.Message("3 - " + ZTracker.GetMapInfo(map) + " transitioting to " + __result);
                        map.weatherManager.TransitionTo(__result);
                    }
                    Log.Message("Changed weather for " + ZTracker.GetMapInfo(map) + " - " + __result);
                }
                catch { };
            }
        }
        
        [HarmonyPatch(typeof(WeatherManager), "TransitionTo")]
        internal static class Patch_TransitionTo
        {
            private static void Postfix(WeatherManager __instance, WeatherDef newWeather)
            {
                try
                {
                    Map map = Traverse.Create(__instance).Field("map").GetValue<Map>();
                    var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                    Log.Message("2 Weather decider: " + newWeather + " - " + ZTracker.GetMapInfo(map));
                    if (ZTracker.GetZIndexFor(map) == 0)
                    {
                        foreach (var map2 in ZTracker.GetAllMaps(map.Tile))
                        {
                            if (ZTracker.GetZIndexFor(map2) > 0)
                            {
                                map2.weatherManager.lastWeather = __instance.curWeather;
                                map2.weatherManager.curWeather = newWeather;
                                map2.weatherManager.curWeatherAge = map.weatherManager.curWeatherAge;
                                Log.Message("1.2 - " + ZTracker.GetMapInfo(map2) + " transitioting to " + newWeather);
                            }
                        }
                    }
                    else if (ZTracker.GetZIndexFor(map) > 0)
                    {
                        Map playerMap = ZTracker.GetMapByIndex(map.Tile, 0);
                        __instance.lastWeather = playerMap.weatherManager.lastWeather;
                        __instance.curWeather = playerMap.weatherManager.curWeather;
                        __instance.curWeatherAge = playerMap.weatherManager.curWeatherAge;
                        Log.Message("2.2 - " + ZTracker.GetMapInfo(map) + " transitioting to " + map.weatherManager.curWeather);
                    }
                    else if (ZTracker.GetZIndexFor(map) < 0)
                    {
                        __instance.lastWeather = __instance.curWeather;
                        __instance.curWeather = WeatherDefOf.Clear;
                        __instance.curWeatherAge = 0;
                        Log.Message("3.2 - " + ZTracker.GetMapInfo(map) + " transitioting to " + WeatherDefOf.Clear);
                    }
                }
                catch { };
            }
        }

        [HarmonyPatch(typeof(WeatherDecider), "StartInitialWeather")]
        internal static class Patch_WeatherManager
        {
            private static bool Prefix(WeatherDecider __instance)
            {
                try
                {
                    Map map = Traverse.Create(__instance).Field("map").GetValue<Map>();
                    var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                    if (ZTracker.GetZIndexFor(map) < 0)
                    {
                        int curWeatherDuration = Traverse.Create(__instance).Field("curWeatherDuration").GetValue<int>();
                        map.weatherManager.curWeather = null;
                        WeatherDef weatherDef = WeatherDefOf.Clear;
                        WeatherDef lastWeather = WeatherDefOf.Clear;
                        map.weatherManager.curWeather = weatherDef;
                        map.weatherManager.lastWeather = lastWeather;
                        curWeatherDuration = weatherDef.durationRange.RandomInRange;
                        map.weatherManager.curWeatherAge = Rand.Range(0, curWeatherDuration);
                        return false;
                    }
                }
                catch { };
                return true;
            }
        }

        [HarmonyPatch(typeof(FloatMenuOption), "Disabled", MethodType.Getter)]
        internal static class Patch_FloatDisabled
        {
            private static bool Prefix(FloatMenuOption __instance, bool __result)
            {
                if (__instance.Label == "GoDown".Translate() || __instance.Label == "GoUP".Translate())
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(FloatMenuOption), "Chosen")]
        internal static class Patch_FloatMenuOption
        {
            private static void Postfix(FloatMenuOption __instance, bool colonistOrdering, FloatMenu floatMenu)
            {
                if (__instance.Label == "GoDown".Translate() || __instance.Label == "GoUP".Translate())
                {
                    if (Find.Selector.SelectedObjects.Where(x => x is Pawn).Count() > 1)
                    {
                        Thing thing;
                        foreach (var pawn in Find.Selector.SelectedObjects.Where(x => x is Pawn))
                        {
                            if (__instance.Label == "GoDown".Translate())
                            {
                                thing = GenClosest.ClosestThing_Global_Reachable(UI.MouseMapPosition().ToIntVec3()
                                    , ((Pawn)pawn).Map, ((Pawn)pawn).Map.listerThings.AllThings
                                    .Where(x => x is Building_StairsDown), PathEndMode.OnCell,
                                    TraverseParms.For(TraverseMode.ByPawn, Danger.Deadly, false), 9999f);
                                Job job = JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToStairs, thing);
                                ((Pawn)pawn).jobs.StartJob(job, JobCondition.InterruptForced);
                            }
                            else
                            {
                                thing = GenClosest.ClosestThing_Global_Reachable(UI.MouseMapPosition().ToIntVec3()
                                    , ((Pawn)pawn).Map, ((Pawn)pawn).Map.listerThings.AllThings
                                    .Where(x => x is Building_StairsUp), PathEndMode.OnCell,
                                    TraverseParms.For(TraverseMode.ByPawn, Danger.Deadly, false), 9999f);
                                Job job = JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToStairs, thing);
                                ((Pawn)pawn).jobs.StartJob(job, JobCondition.InterruptForced);
                            }
                        }
                        Log.Message("Choosen");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(RoofGrid), "SetRoof")]
        internal static class Patch_SetRoof
        {
            private static void Postfix(RoofGrid __instance, ref IntVec3 c, ref RoofDef def)
            {
                try
                {
                    if (def != null && !def.isNatural)
                    {
                        var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                        Map map = Traverse.Create(__instance).Field("map").GetValue<Map>();
                        var upperMap = ZTracker.GetUpperLevel(map.Tile, map);
                        if (upperMap != null && upperMap.terrainGrid.TerrainAt(c) != ZLevelsDefOf.ZL_RoofTerrain)
                        {
                            upperMap.terrainGrid.SetTerrain(c, ZLevelsDefOf.ZL_RoofTerrain);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Error in Patch_SetRoof: " + ex);
                };
            }
        }

        [HarmonyPatch(typeof(TerrainGrid), "SetTerrain")]
        internal static class Patch_SetTerrain
        {
            private static void Postfix(TerrainGrid __instance, ref IntVec3 c, ref TerrainDef newTerr)
            {
                try
                {
                    if (newTerr != null && newTerr.Removable)
                    {
                        var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                        Map map = Traverse.Create(__instance).Field("map").GetValue<Map>();
                        var lowerMap = ZTracker.GetLowerLevel(map.Tile, map);
                        if (lowerMap != null && lowerMap.roofGrid.RoofAt(c) != RoofDefOf.RoofConstructed)
                        {
                            lowerMap.roofGrid.SetRoof(c, RoofDefOf.RoofConstructed);
                        }
                    }
                    else if (newTerr == null)
                    {
                        var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                        Map map = Traverse.Create(__instance).Field("map").GetValue<Map>();
                        if (ZTracker.GetZIndexFor(map) > 0)
                        {
                            foreach (var t in c.GetThingList(map))
                            {
                                if (t is Pawn pawn)
                                {
                                    pawn.Kill(null);
                                }
                                else
                                {
                                    t.Destroy(DestroyMode.Refund);
                                }
                            }                    
                            map.terrainGrid.SetTerrain(c, ZLevelsDefOf.ZL_OutsideTerrain);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Error in Patch_SetTerrain: " + ex);
                };
            }
        }

        [HarmonyPatch(typeof(Pawn_PathFollower), "CostToMoveIntoCell", new Type[]
        {
            typeof(Pawn),
            typeof(IntVec3)
        })]
        public static class CostToMoveIntoCell_Patch
        {
            public static void Prefix(Pawn_PathFollower __instance, Pawn pawn, IntVec3 c, ref int __result)
            {
                if (c.GetTerrain(pawn.Map) == ZLevelsDefOf.ZL_OutsideTerrain)
                {
                    pawn.pather.StopDead();
                }
                __result = 100000;
            }
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
                    )
                {
                    //Log.Message("The error: " + text);
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
        public class AddHumanlikeOrders_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
            {
                Pawn pawn2 = GridsUtility.GetThingList(IntVec3.FromVector3(clickPos), pawn.Map)
                    .FirstOrDefault((Thing x) => x is Pawn) as Pawn;
                var mapComp = pawn.Map.GetComponent<MapComponentZLevel>();
                var ZTracker = Current.Game.GetComponent<ZLevelsManager>();

                if (pawn2 != null && ZTracker.ZLevelsTracker[pawn.Map.Tile].ZLevels.Count > 1)
                {
                    TaggedString toCheck = "Rescue".Translate(pawn2.LabelCap, pawn2);
                    FloatMenuOption floatMenuOption = opts.FirstOrDefault((FloatMenuOption x) => x.Label.Contains
                    (toCheck));
                    if (floatMenuOption != null)
                    {
                        opts.Remove(floatMenuOption);
                        opts.Add(AddHumanlikeOrders_Patch.addRescueOption(pawn, pawn2));
                    }
                    TaggedString toCheck2 = "Capture".Translate(pawn2.LabelCap, pawn2);
                    FloatMenuOption floatMenuOption2 = opts.FirstOrDefault((FloatMenuOption x) => x.Label.Contains
                    (toCheck2));
                    if (floatMenuOption2 != null)
                    {
                        opts.Remove(floatMenuOption2);
                        opts.Add(AddHumanlikeOrders_Patch.addCaptureOption(pawn, pawn2));
                    }
                }
            }

            public static FloatMenuOption addCaptureOption(Pawn pawn, Pawn victim)
            {
                var floatOption = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Capture".Translate
                    (victim.LabelCap, victim), delegate ()
                    {
                        Building_Bed building_Bed = RestUtility.FindBedFor(victim, pawn, true, false, false);
                        if (building_Bed == null)
                        {
                            building_Bed = RestUtility.FindBedFor(victim, pawn, true, false, true);
                        }
                        if (building_Bed == null)
                        {
                            var mapComp = pawn.Map.GetComponent<MapComponentZLevel>();
                            var oldMap = pawn.Map;
                            var oldPosition1 = pawn.Position;
                            var oldPosition2 = victim.Position;
                            var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                            foreach (var otherMap in ZTracker.GetAllMaps(pawn.Map.Tile))
                            {
                                if (oldMap != otherMap)
                                {
                                    var mapComp2 = otherMap.GetComponent<MapComponentZLevel>();
                                    var stairs = new List<Thing>();
                                    if (mapComp2.Z_LevelIndex >= 0)
                                    {

                                        Map lowerMap = ZTracker.GetLowerLevel(otherMap.Tile, otherMap);
                                        if (lowerMap != null)
                                        {
                                            stairs = lowerMap.listerThings.AllThings.Where(x => x is Building_StairsUp).ToList();
                                        }
                                    }
                                    else
                                    {
                                        Map upperMap = ZTracker.GetUpperLevel(otherMap.Tile, otherMap);
                                        if (upperMap != null)
                                        {
                                            stairs = upperMap.listerThings.AllThings.Where(x => x is Building_StairsDown).ToList();
                                        }
                                    }
                                    if (stairs != null && stairs.Count() > 0)
                                    {
                                        var selectedStairs = GenClosest.ClosestThing_Global(pawn.Position, stairs, 99999f);
                                        var position = selectedStairs.Position;
                                        bool select = false;
                                        if (Find.Selector.SelectedObjects.Contains(pawn)) select = true;
                                        JobManagerPatches.manualDespawn = true;
                                        pawn.DeSpawn();
                                        JobManagerPatches.manualDespawn = false;
                                        GenPlace.TryPlaceThing(pawn, position, otherMap, ThingPlaceMode.Direct);

                                        JobManagerPatches.manualDespawn = true;
                                        victim.DeSpawn();
                                        JobManagerPatches.manualDespawn = false;
                                        GenPlace.TryPlaceThing(victim, position, otherMap, ThingPlaceMode.Direct);
                                        building_Bed = RestUtility.FindBedFor(victim, pawn, true, false, false);
                                        if (building_Bed == null)
                                        {
                                            building_Bed = RestUtility.FindBedFor(victim, pawn, true, false, true);
                                        }

                                        JobManagerPatches.manualDespawn = true;
                                        pawn.DeSpawn();
                                        JobManagerPatches.manualDespawn = false;
                                        GenPlace.TryPlaceThing(pawn, oldPosition1, oldMap, ThingPlaceMode.Direct);
                                        if (select) Find.Selector.Select(pawn);

                                        JobManagerPatches.manualDespawn = true;
                                        victim.DeSpawn();
                                        JobManagerPatches.manualDespawn = false;
                                        GenPlace.TryPlaceThing(victim, oldPosition2, oldMap, ThingPlaceMode.Direct);

                                        if (building_Bed != null)
                                        {
                                            Log.Message("Found bed: " + building_Bed);
                                            Job captureJob = JobMaker.MakeJob(JobDefOf.Capture, victim, building_Bed);
                                            captureJob.count = 1;
                                            ZTracker.BuildJobListFor(pawn, oldMap, oldMap, captureJob, null);
                                            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Rescuing, KnowledgeAmount.Total);
                                            return;
                                        }
                                    }
                                }
                            }
                            if (building_Bed == null)
                            {
                                Messages.Message("CannotCapture".Translate() + ": " + "NoPrisonerBed".Translate(), victim, MessageTypeDefOf.RejectInput, false);
                                return;
                            }
                            Job job = JobMaker.MakeJob(JobDefOf.Capture, victim, building_Bed);
                            job.count = 1;
                            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Capturing, KnowledgeAmount.Total);
                            if (victim.Faction != null && victim.Faction != Faction.OfPlayer && !victim.Faction.def.hidden && !victim.Faction.HostileTo(Faction.OfPlayer) && !victim.IsPrisonerOfColony)
                            {
                                Messages.Message("MessageCapturingWillAngerFaction".Translate(victim.Named("PAWN"))
                                    .AdjustedFor(victim, "PAWN", true), victim, MessageTypeDefOf.CautionInput, false);
                            }
                        }
                    }, MenuOptionPriority.RescueOrCapture, null, victim, 0f, null, null), pawn, victim, "ReservedBy");
                return floatOption;
            }

            public static FloatMenuOption addRescueOption(Pawn pawn, Pawn victim)
            {
                var floatOption = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Rescue".Translate
                    (victim.LabelCap, victim), delegate ()
                    {
                        Building_Bed building_Bed = RestUtility.FindBedFor(victim, pawn, false, false, false);
                        if (building_Bed == null)
                        {
                            building_Bed = RestUtility.FindBedFor(victim, pawn, false, false, true);
                        }
                        if (building_Bed == null)
                        {
                            var mapComp = pawn.Map.GetComponent<MapComponentZLevel>();
                            var oldMap = pawn.Map;
                            var oldPosition1 = pawn.Position;
                            var oldPosition2 = victim.Position;
                            var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                            foreach (var otherMap in ZTracker.GetAllMaps(pawn.Map.Tile))
                            {
                                if (oldMap != otherMap)
                                {
                                    var mapComp2 = otherMap.GetComponent<MapComponentZLevel>();
                                    var stairs = new List<Thing>();
                                    if (mapComp2.Z_LevelIndex >= 0)
                                    {
                                        Map lowerMap = ZTracker.GetLowerLevel(otherMap.Tile, otherMap);
                                        if (lowerMap != null)
                                        {
                                            stairs = lowerMap.listerThings.AllThings.Where(x => x is Building_StairsUp).ToList();
                                        }
                                    }
                                    else
                                    {
                                        Map upperMap = ZTracker.GetUpperLevel(otherMap.Tile, otherMap);
                                        if (upperMap != null)
                                        {
                                            stairs = upperMap.listerThings.AllThings.Where(x => x is Building_StairsDown).ToList();
                                        }
                                    }
                                    if (stairs != null && stairs.Count() > 0)
                                    {
                                        var selectedStairs = GenClosest.ClosestThing_Global(pawn.Position, stairs, 99999f);
                                        var position = selectedStairs.Position;
                                        bool select = false;
                                        if (Find.Selector.SelectedObjects.Contains(pawn)) select = true;
                                        JobManagerPatches.manualDespawn = true;
                                        pawn.DeSpawn();
                                        JobManagerPatches.manualDespawn = false;
                                        GenPlace.TryPlaceThing(pawn, position, otherMap, ThingPlaceMode.Direct);

                                        JobManagerPatches.manualDespawn = true;
                                        victim.DeSpawn();
                                        JobManagerPatches.manualDespawn = false;
                                        GenPlace.TryPlaceThing(victim, position, otherMap, ThingPlaceMode.Direct);
                                        building_Bed = RestUtility.FindBedFor(victim, pawn, false, false, false);
                                        if (building_Bed == null)
                                        {
                                            building_Bed = RestUtility.FindBedFor(victim, pawn, false, false, true);
                                        }

                                        JobManagerPatches.manualDespawn = true;
                                        pawn.DeSpawn();
                                        JobManagerPatches.manualDespawn = false;
                                        GenPlace.TryPlaceThing(pawn, oldPosition1, oldMap, ThingPlaceMode.Direct);
                                        if (select) Find.Selector.Select(pawn);

                                        JobManagerPatches.manualDespawn = true;
                                        victim.DeSpawn();
                                        JobManagerPatches.manualDespawn = false;
                                        GenPlace.TryPlaceThing(victim, oldPosition2, oldMap, ThingPlaceMode.Direct);

                                        if (building_Bed != null)
                                        {
                                            Log.Message("Found bed: " + building_Bed);
                                            Job rescureJob = JobMaker.MakeJob(JobDefOf.Rescue, victim, building_Bed);
                                            rescureJob.count = 1;
                                            ZTracker.BuildJobListFor(pawn, oldMap, oldMap, rescureJob, null);
                                            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Rescuing, KnowledgeAmount.Total);
                                            return;
                                        }
                                    }
                                }
                            }
                            string t5;
                            if (victim.RaceProps.Animal)
                            {
                                t5 = "NoAnimalBed".Translate();
                            }
                            else
                            {
                                t5 = "NoNonPrisonerBed".Translate();
                            }
                            Messages.Message("CannotRescue".Translate() + ": " + t5, victim, MessageTypeDefOf.RejectInput, false);
                            return;
                        }
                        Job job = JobMaker.MakeJob(JobDefOf.Rescue, victim, building_Bed);
                        job.count = 1;
                        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                        PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Rescuing, KnowledgeAmount.Total);
                    }, MenuOptionPriority.RescueOrCapture, null, victim, 0f, null, null), pawn, victim, "ReservedBy");
                return floatOption;
            }
        }

        [HarmonyPatch(typeof(Pawn_JobTracker))]
        [HarmonyPatch(nameof(Pawn_JobTracker.StopAll))]
        static class Pawn_JobTracker_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix()
            {
                if (JobManagerPatches.manualDespawn == true)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(Pawn_PathFollower))]
        [HarmonyPatch(nameof(Pawn_PathFollower.StopDead))]
        static class Pawn_PathFollower_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix()
            {
                if (JobManagerPatches.manualDespawn == true)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(Pawn))]
        [HarmonyPatch(nameof(Pawn.ClearAllReservations))]
        static class Pawn_ClearAllReservations_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix()
            {
                if (JobManagerPatches.manualDespawn == true)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(Map), "Biome", MethodType.Getter)]
        public class GetBiomePatch
        {
            [HarmonyPostfix]
            private static void MapBiomePostfix(Map __instance, ref BiomeDef __result)
            {
                if (__instance.ParentHolder is MapParent_ZLevel)
                {
                    var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                    if (ZTracker.GetZIndexFor(__instance) < 0)
                    {
                        __result = ZLevelsDefOf.ZL_UndegroundBiome;
                    }
                    else if (ZTracker.GetZIndexFor(__instance) > 0)
                    {
                        __result = ZLevelsDefOf.ZL_UpperBiome;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ExitMapGrid))]
        [HarmonyPatch("MapUsesExitGrid", MethodType.Getter)]
        public class ExitCells_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(ExitMapGrid __instance, ref bool __result)
            {
                Map map = (Map)typeof(ExitMapGrid).GetField("map", BindingFlags.Instance | BindingFlags.Static
                    | BindingFlags.Public | BindingFlags.NonPublic).GetValue(__instance);
                if (map != null)
                {
                    if (map.ParentHolder is MapParent_ZLevel)
                    {
                        __result = false;
                        return false;
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(JobGiver_Work), "TryIssueJobPackage")]
        public class JobManagerPatch
        {
            [HarmonyPostfix]
            private static void TryIssueJobPackagePostfix(JobGiver_Work __instance, ref ThinkResult __result, Pawn pawn, JobIssueParams jobParams)
            {
                try
                {
                    var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                    if (__result.Job == null && ZTracker?.ZLevelsTracker[pawn.Map.Tile]?.ZLevels?.Count > 1)
                    {
                        if (ZTracker.jobTracker == null)
                        {
                            ZTracker.jobTracker = new Dictionary<Pawn, JobTracker>();
                        }
                        if (ZTracker.jobTracker.ContainsKey(pawn))
                        {
                            if (Find.TickManager.TicksGame - ZTracker.jobTracker[pawn].lastTick < 200)
                            // minimal job interval check per pawn is 200 ticks
                            {
                                return;
                            }
                            try
                            {
                                if (ZTracker.jobTracker[pawn]?.activeJobs?.Count() > 0)
                                {
                                    if (!pawn.jobs.jobQueue.Contains(ZTracker.jobTracker[pawn].activeJobs[0]))
                                    {
                                        Log.Message("Queue: " + ZTracker.jobTracker[pawn].activeJobs[0]);
                                        pawn.jobs.jobQueue.EnqueueLast(ZTracker.jobTracker[pawn].activeJobs[0]);
                                        return;
                                    }
                                    else if (pawn.jobs.jobQueue[0].job.targetA.Thing.Map != pawn.Map)
                                    {
                                        ZTracker.ResetJobTrackerFor(pawn);
                                    }
                                    else
                                    {
                                        return;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Message("Error2: " + ex);
                            };
                        }
                        else
                        {
                            ZTracker.jobTracker[pawn] = new JobTracker();
                        }
                        if (Find.TickManager.TicksGame - ZTracker.jobTracker[pawn].lastTick < 200)
                        // minimal job interval check per pawn is 200 ticks
                        {
                            return;
                        }
                        ThinkResult result;
                        var oldMap = pawn.Map;
                        var oldPosition = pawn.Position;
                        //Log.Message("======================================");
                        foreach (var otherMap in ZTracker.ZLevelsTracker[pawn.Map.Tile].ZLevels.Values)
                        {
                            if (Find.TickManager.TicksGame - ZTracker.jobTracker[pawn].lastTick < 200)
                            // minimal job interval check per pawn is 200 ticks
                            {
                                return;
                            }
                            var stairs = new List<Thing>();

                            Log.Message("Searching job for " + pawn + " in " + ZTracker.GetMapInfo(otherMap)
                                + " for " + ZTracker.GetMapInfo(oldMap));

                            if (ZTracker.GetZIndexFor(otherMap) > ZTracker.GetZIndexFor(oldMap))
                            {
                                Map lowerMap = ZTracker.GetLowerLevel(otherMap.Tile, otherMap);
                                if (lowerMap != null)
                                {
                                    Log.Message("Searching stairs up in " + ZTracker.GetMapInfo(otherMap));
                                    stairs = lowerMap.listerThings.AllThings.Where(x => x is Building_StairsUp).ToList();
                                }
                                else
                                {
                                    Log.Message("Lower map is null in " + ZTracker.GetMapInfo(otherMap));
                                }
                            }
                            else if (ZTracker.GetZIndexFor(otherMap) < ZTracker.GetZIndexFor(oldMap))
                            {
                                Map upperMap = ZTracker.GetUpperLevel(otherMap.Tile, otherMap);
                                if (upperMap != null)
                                {
                                    Log.Message("Searching stairs down in " + ZTracker.GetMapInfo(otherMap));
                                    stairs = upperMap.listerThings.AllThings.Where(x => x is Building_StairsDown).ToList();
                                }
                                else
                                {
                                    Log.Message("Upper map is null in " + ZTracker.GetMapInfo(otherMap));
                                }
                            }
                            if (Find.TickManager.TicksGame - ZTracker.jobTracker[pawn].lastTick < 200)
                            // minimal job interval check per pawn is 200 ticks
                            {
                                return;
                            }
                            bool goToOtherMap = false;
                            bool select = false;

                            if (stairs != null && stairs.Count() > 0)
                            {
                                var selectedStairs = GenClosest.ClosestThing_Global(pawn.Position, stairs, 99999f);
                                var position = selectedStairs.Position;

                                if (Find.Selector.SelectedObjects.Contains(pawn)) select = true;
                                JobManagerPatches.manualDespawn = true;
                                pawn.DeSpawn();
                                JobManagerPatches.manualDespawn = false;
                                GenPlace.TryPlaceThing(pawn, position, otherMap, ThingPlaceMode.Direct);
                                goToOtherMap = true;
                            }

                            //try
                            //{
                            //    Log.Message("102 Find.TickManager.TicksGame: " + Find.TickManager.TicksGame);
                            //    Log.Message("102 ZTracker.jobTracker[pawn].lastTick: " + ZTracker.jobTracker[pawn].lastTick);
                            //    Log.Message("102 Result: " + (Find.TickManager.TicksGame - ZTracker.jobTracker[pawn].lastTick));
                            //}
                            //catch { };
                            Map dest = null;
                            result = TryIssueJobPackage(pawn, jobParams, __instance, __instance.emergency, ref dest);
                            if (result.Job != null)
                            {
                                Log.Message("TryIssueJobPackage: " + pawn + " - map: " + ZTracker.GetMapInfo(pawn.Map)
                                    + " - " + pawn.Position + " result " + result.Job);

                                if (goToOtherMap)
                                {
                                    JobManagerPatches.manualDespawn = true;
                                    pawn.DeSpawn();
                                    JobManagerPatches.manualDespawn = false;
                                    GenPlace.TryPlaceThing(pawn, oldPosition, oldMap, ThingPlaceMode.Direct);
                                    if (select) Find.Selector.Select(pawn);
                                }

                                if (dest != null)
                                {
                                    ZTracker.BuildJobListFor(pawn, oldMap, dest, result.Job, null);
                                }
                                else
                                {
                                    ZTracker.BuildJobListFor(pawn, oldMap, otherMap, result.Job, null);
                                }
                                break;
                            }
                            else if (goToOtherMap)
                            {
                                JobManagerPatches.manualDespawn = true;
                                pawn.DeSpawn();
                                JobManagerPatches.manualDespawn = false;
                                GenPlace.TryPlaceThing(pawn, oldPosition, oldMap, ThingPlaceMode.Direct);
                                if (select) Find.Selector.Select(pawn);
                            }
                        
                        }
                    }
                    try
                    {
                        ZTracker.jobTracker[pawn].lastTick = Find.TickManager.TicksGame;
                    }
                    catch { }
                }
                catch (Exception ex)
                {
                    Log.Error("Some kind of error occurred in Z-Levels JobManager: " + ex);
                }
            }

            public static bool TryFindBestBetterStoreCellForValidator(Thing t, Pawn carrier, Map mapToSearch,
                StoragePriority currentPriority, Faction faction, out IntVec3 foundCell, bool needAccurateResult = true)
            {
                Map temp = null;
                return TryFindBestBetterStoreCellFor(t, carrier, mapToSearch, currentPriority, faction, out foundCell, ref temp);
            }

            public static bool TryFindBestBetterStoreCellFor(Thing t, Pawn carrier, Map mapToSearch, StoragePriority currentPriority, Faction faction, out IntVec3 foundCell, ref Map dest, bool needAccurateResult = true)
            {
                Zone_Stockpile zoneDest = null;
                bool result = true;
                //Log.Message(t + " priority: " + currentPriority + " - checking on " + carrier + " _ " + mapToSearch +
                //    " --------------------------------------- start", true);

                var allZones = new Dictionary<Zone_Stockpile, Map>();
                var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                var allMaps = ZTracker.GetAllMaps(carrier.Map.Tile);
                foreach (Map map in allMaps)
                {
                    foreach (var zone in map.zoneManager.AllZones.Where(x => x is Zone_Stockpile))
                    {
                        allZones[(Zone_Stockpile)zone] = map;
                    }
                }

                List<SlotGroup> allGroupsListInPriorityOrder = new List<SlotGroup>();
                foreach (var zone in allZones)
                {
                    allGroupsListInPriorityOrder.Add(zone.Key.GetSlotGroup());
                }

                if (allGroupsListInPriorityOrder.Count == 0)
                {
                    foundCell = IntVec3.Invalid;
                    result = false;
                }
                StoragePriority storagePriority = currentPriority;
                float num = 2.14748365E+09f;
                IntVec3 invalid = IntVec3.Invalid;
                int count = allGroupsListInPriorityOrder.Count;
                for (int i = 0; i < count; i++)
                {
                    SlotGroup slotGroup = allGroupsListInPriorityOrder[i];
                    //Log.Message("Checking: " + carrier + " - " + carrier.Map + " - " + slotGroup.parent + " - " + slotGroup.parent.Map);
                    StoragePriority priority = slotGroup.Settings.Priority;
                    if (priority < storagePriority || priority <= currentPriority)
                    {
                        break;
                    }
                    zoneDest = slotGroup.parent as Zone_Stockpile;
                    JobManagerPatch.TryFindBestBetterStoreCellForWorker
                        (t, carrier, mapToSearch, faction, slotGroup, needAccurateResult,
                        ref invalid, ref num, ref storagePriority, slotGroup.parent.Map);
                }

                if (!invalid.IsValid)
                {
                    foundCell = IntVec3.Invalid;
                    result = false;
                }
                else
                {
                    dest = zoneDest.Map;
                    try
                    {
                        Log.Message("RESULT: " + carrier + " - " + zoneDest + " - " + ZTracker.GetMapInfo(zoneDest.Map) + " accepts " + t, true);

                    }
                    catch (Exception ex)
                    {
                        Log.Message("Cant find result: " + ex);
                    }
                }
                foundCell = invalid;

                return result;
            }

            private static void TryFindBestBetterStoreCellForWorker(Thing t, Pawn carrier, Map map, Faction faction, SlotGroup slotGroup, bool needAccurateResult, ref IntVec3 closestSlot, ref float closestDistSquared, ref StoragePriority foundPriority, Map oldMap)
            {
                if (slotGroup == null)
                {
                    return;
                }
                if (!slotGroup.parent.Accepts(t))
                {
                    return;
                }
                if (slotGroup.HeldThings.Contains(t))
                {
                    //Log.Message(t + " already in stockpile " + slotGroup.parent);
                    return;
                }
                //Log.Message(t + " map " + t.Map + " - " + slotGroup.parent + " map " + slotGroup.parent.Map, true);
                //foreach (var test in slotGroup.HeldThings)
                //{
                //    Log.Message(test + " in " + slotGroup.parent, true);
                //}

                IntVec3 a = t.SpawnedOrAnyParentSpawned ? t.PositionHeld : carrier.PositionHeld;
                List<IntVec3> cellsList = slotGroup.CellsList;
                int count = cellsList.Count;
                int num;
                if (needAccurateResult)
                {
                    num = Mathf.FloorToInt((float)count * Rand.Range(0.005f, 0.018f));
                }
                else
                {
                    num = 0;
                }
                for (int i = 0; i < count; i++)
                {
                    IntVec3 intVec = cellsList[i];
                    float num2 = (float)(a - intVec).LengthHorizontalSquared;
                    if (num2 <= closestDistSquared && IsGoodStoreCell(intVec, map, t, carrier, faction, oldMap))
                    {
                        closestSlot = intVec;
                        closestDistSquared = num2;
                        foundPriority = slotGroup.Settings.Priority;
                        if (i >= num)
                        {
                            break;
                        }
                    }
                }
            }

            public static bool IsGoodStoreCell(IntVec3 c, Map map, Thing t, Pawn carrier, Faction faction, Map oldMap)
            {
                if (carrier != null && c.IsForbidden(carrier))
                {
                    return false;
                }
                if (!NoStorageBlockersIn(c, oldMap, t))
                {
                    return false;
                }
                if (carrier != null)
                {
                    if (!carrier.CanReserveNew(c))
                    {
                        return false;
                    }
                }
                else if (faction != null && map.reservationManager.IsReservedByAnyoneOf(c, faction))
                {
                    return false;
                }
                if (c.ContainsStaticFire(map))
                {
                    return false;
                }
                List<Thing> thingList = c.GetThingList(map);
                for (int i = 0; i < thingList.Count; i++)
                {
                    if (thingList[i] is IConstructible && GenConstruct.BlocksConstruction(thingList[i], t))
                    {
                        return false;
                    }
                }

                return true;// carrier == null || carrier.Map.reachability.CanReach(t.SpawnedOrAnyParentSpawned ? t.PositionHeld : carrier.PositionHeld, c, PathEndMode.ClosestTouch, TraverseParms.For(carrier, Danger.Deadly, TraverseMode.ByPawn, false));
            }

            private static bool NoStorageBlockersIn(IntVec3 c, Map map, Thing thing)
            {
                List<Thing> list = map.thingGrid.ThingsListAt(c);
                for (int i = 0; i < list.Count; i++)
                {
                    Thing thing2 = list[i];
                    if (thing2.def.EverStorable(false))
                    {
                        if (!thing2.CanStackWith(thing))
                        {
                            return false;
                        }
                        if (thing2.stackCount >= thing.def.stackLimit)
                        {
                            return false;
                        }
                    }
                    if (thing2.def.entityDefToBuild != null && thing2.def.entityDefToBuild.passability != Traversability.Standable)
                    {
                        return false;
                    }
                    if (thing2.def.surfaceType == SurfaceType.None && thing2.def.passability != Traversability.Standable)
                    {
                        return false;
                    }
                }
                return true;
            }

            public static Job HasJobOnThing(WorkGiver_DoBill scanner, Pawn pawn, Thing thing, bool forced = false)
            {
                IBillGiver billGiver = thing as IBillGiver;
                if (billGiver == null || !scanner.ThingIsUsableBillGiver(thing) || !billGiver.BillStack.AnyShouldDoNow || !billGiver.UsableForBillsAfterFueling() || !pawn.CanReserve(thing, 1, -1, null, forced) || thing.IsBurning() || thing.IsForbidden(pawn))
                {
                    return null;
                }
                CompRefuelable compRefuelable = thing.TryGetComp<CompRefuelable>();
                if (compRefuelable == null || compRefuelable.HasFuel)
                {
                    billGiver.BillStack.RemoveIncompletableBills();
                    var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                    var origMap = thing.Map;
                    var origMap2 = pawn.Map;
                    bool select = false;
                    foreach (var map in ZTracker.GetAllMaps(pawn.Map.Tile))
                    {
                        Traverse.Create(thing).Field("mapIndexOrState")
                            .SetValue((sbyte)Find.Maps.IndexOf(map));

                        Traverse.Create(pawn).Field("mapIndexOrState")
                            .SetValue((sbyte)Find.Maps.IndexOf(map));

                        Job job = Traverse.Create(scanner).Method("StartOrResumeBillJob", new object[]
                        {
                            pawn, billGiver
                        }).GetValue<Job>();
                        if (job != null)
                        {
                            Traverse.Create(thing).Field("mapIndexOrState")
                                .SetValue((sbyte)Find.Maps.IndexOf(origMap));

                            Traverse.Create(pawn).Field("mapIndexOrState")
                                .SetValue((sbyte)Find.Maps.IndexOf(origMap2));

                            return job;
                        }
                    }
                    Traverse.Create(thing).Field("mapIndexOrState")
                        .SetValue((sbyte)Find.Maps.IndexOf(origMap));
                    Traverse.Create(pawn).Field("mapIndexOrState")
                        .SetValue((sbyte)Find.Maps.IndexOf(origMap2));
                    return null;
                }
                if (!RefuelWorkGiverUtility.CanRefuel(pawn, thing, forced))
                {
                    return null;
                }
                return RefuelWorkGiverUtility.RefuelJob(pawn, thing, forced, null, null);
            }
            public static ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams, JobGiver_Work instance, bool emergency, ref Map dest)
            {
                if (emergency && pawn.mindState.priorityWork.IsPrioritized)
                {
                    List<WorkGiverDef> workGiversByPriority = pawn.mindState.priorityWork.WorkGiver.workType.workGiversByPriority;
                    for (int i = 0; i < workGiversByPriority.Count; i++)
                    {
                        WorkGiver worker = workGiversByPriority[i].Worker;
                        if (WorkGiversRelated(pawn.mindState.priorityWork.WorkGiver, worker.def))
                        {
                            Job job = GiverTryGiveJobPrioritized(pawn, worker, pawn.mindState.priorityWork.Cell);
                            if (job != null)
                            {
                                job.playerForced = true;
                                return new ThinkResult(job, instance, workGiversByPriority[i].tagToGive);
                            }
                        }
                    }
                    pawn.mindState.priorityWork.Clear();
                }
                List<WorkGiver> list = pawn.workSettings.WorkGiversInOrderNormal;
                int num = -999;
                TargetInfo bestTargetOfLastPriority = TargetInfo.Invalid;
                WorkGiver_Scanner scannerWhoProvidedTarget = null;
                WorkGiver_Scanner scanner;
                IntVec3 pawnPosition;
                float closestDistSquared;
                float bestPriority;
                bool prioritized;
                bool allowUnreachable;
                Danger maxPathDanger;
                var ZTracker = Current.Game.GetComponent<ZLevelsManager>();
                for (int j = 0; j < list.Count; j++)
                {

                    WorkGiver workGiver = list[j];
                    if (workGiver.def.priorityInType != num && bestTargetOfLastPriority.IsValid)
                    {
                        break;
                    }
                    if (!PawnCanUseWorkGiver(pawn, workGiver))
                    {
                        continue;
                    }

                    try
                    {

                        Job job2 = workGiver.NonScanJob(pawn);
                        if (job2 != null)
                        {
                            return new ThinkResult(job2, instance, list[j].def.tagToGive);
                        }

                        scanner = (workGiver as WorkGiver_Scanner);
                        if (scanner != null)
                        {

                            if (scanner.def.scanThings)
                            {
                                Predicate<Thing> validator = (Thing t) => !t.IsForbidden(pawn) &&
                                scanner.HasJobOnThing(pawn, t);

                                Predicate<Thing> billValidator = (Thing t) => !t.IsForbidden(pawn) &&
                                JobManagerPatch.HasJobOnThing((WorkGiver_DoBill)scanner, pawn, t) != null;

                                IntVec3 foundCell;
                                Predicate<Thing> haulingValidator = (Thing t) => !t.IsForbidden(pawn)
                                && pawn?.carryTracker?.AvailableStackSpace(t?.def) > 0
                                && pawn?.carryTracker?.MaxStackSpaceEver(t?.def) > 0
                                && pawn.CanReserve(t, 1, -1, null, false)
                                && JobManagerPatch.TryFindBestBetterStoreCellForValidator(t,
                                pawn, t.Map, StoreUtility.CurrentStoragePriorityOf(t), pawn.Faction,
                                out foundCell, true);

                                IEnumerable<Thing> enumerable;

                                if (scanner is WorkGiver_HaulGeneral)
                                {
                                    var allZones = new Dictionary<Zone_Stockpile, Map>();
                                    var allMaps = ZTracker.GetAllMaps(pawn.Map.Tile);
                                    foreach (Map map in allMaps)
                                    {
                                        foreach (var zone in map.zoneManager.AllZones.Where(x => x is Zone_Stockpile))
                                        {
                                            allZones[(Zone_Stockpile)zone] = map;
                                        }
                                    }
                                    
                                    List<Zone_Stockpile> copiedZones = new List<Zone_Stockpile>();
                                    
                                    foreach (Map map in allMaps)
                                    {
                                        foreach (var zone in allZones)
                                        {
                                            if (zone.Key.Map != pawn.Map)
                                            {
                                                var newZone = new Zone_Stockpile();
                                                newZone.settings = zone.Key.settings;
                                                foreach (IntVec3 intVec in zone.Key.cells)
                                                {
                                                    var newPosition = (intVec - zone.Key.Position) + pawn.Position;
                                                    newZone.cells.Add(newPosition);
                                                }
                                                newZone.zoneManager = pawn.Map.zoneManager;
                                                pawn.Map.zoneManager.RegisterZone(newZone);
                                                //Log.Message("Adding " + newZone + " to " + ZTracker.GetMapInfo(pawn.Map));
                                                copiedZones.Add(newZone);
                                            }
                                        }
                                    }
                                                                        
                                    foreach (var t in pawn.Map.listerThings.AllThings.Where(x => x.def.EverHaulable))
                                    {
                                        pawn.Map.listerHaulables.RecalcAllInCell(t.Position);
                                        pawn.Map.listerMergeables.RecalcAllInCell(t.Position);
                                    }

                                    enumerable = pawn.Map.listerHaulables.ThingsPotentiallyNeedingHauling();

                                    foreach (var zone in copiedZones)
                                    {
                                        pawn.Map.zoneManager.DeregisterZone(zone);
                                    }
                                    //Log.Message("--------------------", true);

                                }
                                else
                                {
                                    enumerable = scanner.PotentialWorkThingsGlobal(pawn);
                                }
                                Thing thing = null;
                                if (scanner.Prioritized)
                                {
                                    IEnumerable<Thing> enumerable2 = enumerable;
                                    if (enumerable2 == null)
                                    {
                                        enumerable2 = pawn.Map.listerThings.ThingsMatching(scanner.PotentialWorkThingRequest);
                                    }
                                    thing = ((!scanner.AllowUnreachable) ? GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, enumerable2, scanner.PathEndMode, TraverseParms.For(pawn, scanner.MaxPathDanger(pawn)), 9999f, validator, (Thing x) => scanner.GetPriority(pawn, x)) : GenClosest.ClosestThing_Global(pawn.Position, enumerable2, 99999f, validator, (Thing x) => scanner.GetPriority(pawn, x)));
                                }
                                else if (scanner.AllowUnreachable)
                                {
                                    IEnumerable<Thing> enumerable3 = enumerable;
                                    if (enumerable3 == null)
                                    {
                                        enumerable3 = pawn.Map.listerThings.ThingsMatching(scanner.PotentialWorkThingRequest);
                                    }
                                    thing = GenClosest.ClosestThing_Global(pawn.Position, enumerable3, 99999f, validator);
                                }
                                else
                                {
                                    if (scanner is WorkGiver_HaulGeneral)
                                    {
                                        try
                                        {
                                            Log.Message("Try get thing from enumerable: " + enumerable.Count(), true);
                                            thing = GenClosest.ClosestThingReachable
                                                (pawn.Position, pawn.Map, scanner.PotentialWorkThingRequest,
                                                scanner.PathEndMode, TraverseParms.For(pawn, scanner.MaxPathDanger(pawn)),
                                                9999f, null, enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch,
                                                enumerable != null);
                                            if (thing != null)
                                            {
                                                Log.Message("Get thing: " + thing + " in " + thing.Map + " for " + pawn + " in " + ZTracker.GetMapInfo(pawn.Map), true);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Message(ex.ToString());
                                        };
                                    }

                                    else if (scanner is WorkGiver_DoBill)
                                    {
                                        try
                                        {
                                            //thing = GenClosest.ClosestThingReachable
                                            //    (pawn.Position, pawn.Map, scanner.PotentialWorkThingRequest,
                                            //    scanner.PathEndMode, TraverseParms.For(pawn,
                                            //    scanner.MaxPathDanger(pawn)), 9999f, validator,
                                            //    enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch,
                                            //    enumerable != null);
                                            thing = GenClosest.ClosestThingReachable
                                                (pawn.Position, pawn.Map, scanner.PotentialWorkThingRequest,
                                                scanner.PathEndMode, TraverseParms.For(pawn,
                                                scanner.MaxPathDanger(pawn)), 9999f, billValidator,
                                                enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch,
                                                enumerable != null);
                                            //Log.Message("Get thing: " + thing + " in " + pawn.Map);
                                        }
                                        catch { };

                                    }

                                    else
                                    {
                                        thing = GenClosest.ClosestThingReachable
                                            (pawn.Position, pawn.Map, scanner.PotentialWorkThingRequest,
                                            scanner.PathEndMode, TraverseParms.For(pawn,
                                            scanner.MaxPathDanger(pawn)), 9999f, validator,
                                            enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch,
                                            enumerable != null);
                                    }
                                    if (thing != null)
                                    {
                                        Log.Message(pawn + " - " + ZTracker.GetMapInfo(pawn.Map) + " - " + scanner + " Selected thing: " + thing);
                                    }
                                }
                                if (thing != null)
                                {
                                    bestTargetOfLastPriority = thing;
                                    scannerWhoProvidedTarget = scanner;
                                }
                            }
                            if (scanner.def.scanCells)
                            {
                                pawnPosition = pawn.Position;
                                closestDistSquared = 99999f;
                                bestPriority = float.MinValue;
                                prioritized = scanner.Prioritized;
                                allowUnreachable = scanner.AllowUnreachable;
                                maxPathDanger = scanner.MaxPathDanger(pawn);
                                IEnumerable<IntVec3> enumerable4 = scanner.PotentialWorkCellsGlobal(pawn);
                                IList<IntVec3> list2;
                                if ((list2 = (enumerable4 as IList<IntVec3>)) != null)
                                {
                                    for (int k = 0; k < list2.Count; k++)
                                    {
                                        ProcessCell(list2[k]);
                                    }
                                }
                                else
                                {
                                    foreach (IntVec3 item in enumerable4)
                                    {
                                        ProcessCell(item);
                                    }
                                }
                            }
                        }

                        void ProcessCell(IntVec3 c)
                        {
                            bool flag = false;
                            float num2 = (c - pawnPosition).LengthHorizontalSquared;
                            float num3 = 0f;
                            if (prioritized)
                            {
                                if (!c.IsForbidden(pawn) && scanner.HasJobOnCell(pawn, c))
                                {
                                    if (!allowUnreachable && !pawn.CanReach(c, scanner.PathEndMode, maxPathDanger))
                                    {
                                        return;
                                    }
                                    num3 = scanner.GetPriority(pawn, c);
                                    if (num3 > bestPriority || (num3 == bestPriority && num2 < closestDistSquared))
                                    {
                                        flag = true;
                                    }
                                }
                            }
                            else if (num2 < closestDistSquared && !c.IsForbidden(pawn) && scanner.HasJobOnCell(pawn, c))
                            {
                                if (!allowUnreachable && !pawn.CanReach(c, scanner.PathEndMode, maxPathDanger))
                                {
                                    return;
                                }
                                flag = true;
                            }
                            if (flag)
                            {
                                bestTargetOfLastPriority = new TargetInfo(c, pawn.Map);
                                scannerWhoProvidedTarget = scanner;
                                closestDistSquared = num2;
                                bestPriority = num3;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(string.Concat(pawn, " threw exception in WorkGiver ", workGiver.def.defName, ": ", ex.ToString()));
                    }
                    finally
                    {

                    }
                    if (bestTargetOfLastPriority.IsValid)
                    {
                        Job job3 = null;
                        if (scannerWhoProvidedTarget is WorkGiver_HaulGeneral)
                        {
                            IntVec3 foundCell;
                            if (JobManagerPatch.TryFindBestBetterStoreCellFor(bestTargetOfLastPriority.Thing,
                                pawn, bestTargetOfLastPriority.Thing.Map, StoreUtility.CurrentStoragePriorityOf
                                (bestTargetOfLastPriority.Thing), pawn.Faction, out foundCell, ref dest))
                            {
                                job3 = JobMaker.MakeJob(JobDefOf.HaulToCell, bestTargetOfLastPriority.Thing, foundCell);
                                job3.count = Mathf.Min(bestTargetOfLastPriority.Thing.stackCount,
                                    (int)(pawn.GetStatValue(StatDefOf.CarryingCapacity, true)
                                    / bestTargetOfLastPriority.Thing.def.VolumePerUnit));
                                if (job3.count < 0)
                                {
                                    job3.count = bestTargetOfLastPriority.Thing.stackCount;
                                }
                                Log.Message(pawn + " tasked to haul " + bestTargetOfLastPriority.Thing + 
                                    " to " + foundCell + " to dest " + ZTracker.GetMapInfo(dest), true);
                            }
                            else
                            {
                                Log.Message("Cant haul " + bestTargetOfLastPriority.Thing + " in " + ZTracker.GetMapInfo(bestTargetOfLastPriority.Thing.Map), true);
                            }
                        }
                        else if (scannerWhoProvidedTarget is WorkGiver_DoBill)
                        {
                            job3 = null;
                            if (!bestTargetOfLastPriority.HasThing)
                            {
                                job3 = scannerWhoProvidedTarget.JobOnCell(pawn, bestTargetOfLastPriority.Cell);
                            }
                            else
                            {
                                var origMap = bestTargetOfLastPriority.Thing.Map;
                                var origMap2 = pawn.Map;
                                foreach (var tempMap in ZTracker.GetAllMaps(pawn.Map.Tile))
                                {
                                    Traverse.Create(bestTargetOfLastPriority.Thing).Field("mapIndexOrState")
                                        .SetValue((sbyte)Find.Maps.IndexOf(tempMap));
                                    Traverse.Create(pawn).Field("mapIndexOrState")
                                        .SetValue((sbyte)Find.Maps.IndexOf(tempMap));
                                    Log.Message("JobOnThing: " + bestTargetOfLastPriority.Thing + " searching in "
                                        + bestTargetOfLastPriority.Thing.Map);
                                    job3 = scannerWhoProvidedTarget.JobOnThing(pawn, bestTargetOfLastPriority.Thing);
                                    if (job3 != null)
                                    {
                                        foreach (var t in job3.targetQueueB)
                                        {
                                            Log.Message(ZTracker.GetMapInfo(tempMap) + " - Job2: " + job3 + " target: " + t);
                                        }
                                        break;
                                    }
                                    else
                                    {
                                        Log.Message("No job in " + ZTracker.GetMapInfo(tempMap) + " for " + bestTargetOfLastPriority.Thing
                                            + " - " + bestTargetOfLastPriority.Thing.Map);
                                    }
                                }
                                Traverse.Create(bestTargetOfLastPriority.Thing).Field("mapIndexOrState")
                                    .SetValue((sbyte)Find.Maps.IndexOf(origMap));

                                Traverse.Create(pawn).Field("mapIndexOrState")
                                    .SetValue((sbyte)Find.Maps.IndexOf(origMap2));
                            }
                        }
                        else
                        {
                            job3 = (!bestTargetOfLastPriority.HasThing) ?
                                scannerWhoProvidedTarget.JobOnCell(pawn, bestTargetOfLastPriority.Cell)
                                : scannerWhoProvidedTarget.JobOnThing(pawn, bestTargetOfLastPriority.Thing);
                        }

                        if (job3 != null)
                        {
                            job3.workGiverDef = scannerWhoProvidedTarget.def;
                            return new ThinkResult(job3, instance, list[j].def.tagToGive);
                        }
                        //Log.ErrorOnce(string.Concat(scannerWhoProvidedTarget, " provided target ", bestTargetOfLastPriority, " but yielded no actual job for pawn ", pawn, ". The CanGiveJob and JobOnX methods may not be synchronized."), 6112651);
                    }
                    num = workGiver.def.priorityInType;
                }
                return ThinkResult.NoJob;
            }

            private static bool PawnCanUseWorkGiver(Pawn pawn, WorkGiver giver)
            {
                if (!giver.def.nonColonistsCanDo && !pawn.IsColonist)
                {
                    return false;
                }
                if (pawn.WorkTagIsDisabled(giver.def.workTags))
                {
                    return false;
                }
                if (giver.ShouldSkip(pawn))
                {
                    return false;
                }
                if (giver.MissingRequiredCapacity(pawn) != null)
                {
                    return false;
                }
                return true;
            }

            private static bool WorkGiversRelated(WorkGiverDef current, WorkGiverDef next)
            {
                if (next == WorkGiverDefOf.Repair)
                {
                    return current == WorkGiverDefOf.Repair;
                }
                return true;
            }

            private static Job GiverTryGiveJobPrioritized(Pawn pawn, WorkGiver giver, IntVec3 cell)
            {
                if (!PawnCanUseWorkGiver(pawn, giver))
                {
                    return null;
                }
                try
                {
                    Job job = giver.NonScanJob(pawn);
                    if (job != null)
                    {
                        return job;
                    }
                    WorkGiver_Scanner scanner = giver as WorkGiver_Scanner;
                    if (scanner != null)
                    {
                        if (giver.def.scanThings)
                        {
                            Predicate<Thing> predicate = (Thing t) => !t.IsForbidden(pawn) &&
                            scanner.HasJobOnThing(pawn, t);
                            List<Thing> thingList = cell.GetThingList(pawn.Map);
                            for (int i = 0; i < thingList.Count; i++)
                            {
                                Thing thing = thingList[i];
                                if (scanner.PotentialWorkThingRequest.Accepts(thing) && predicate(thing))
                                {
                                    Job job2 = scanner.JobOnThing(pawn, thing);
                                    if (job2 != null)
                                    {
                                        job2.workGiverDef = giver.def;
                                    }
                                    return job2;
                                }
                            }
                        }
                        if (giver.def.scanCells && !cell.IsForbidden(pawn) && scanner.HasJobOnCell(pawn, cell))
                        {
                            Job job3 = scanner.JobOnCell(pawn, cell);
                            if (job3 != null)
                            {
                                job3.workGiverDef = giver.def;
                            }
                            return job3;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(string.Concat(pawn, " threw exception in GiverTryGiveJobTargeted on WorkGiver ", giver.def.defName, ": ", ex.ToString()));
                }
                return null;
            }
        }
    }
}

