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
    public static class AnimalPatches
    {
        [HarmonyPatch(typeof(WildAnimalSpawner), "SpawnRandomWildAnimalAt")]
        public class AnimalPatch2
        {
            [HarmonyPrefix]
            private static bool SpawnAnimalPrefix(WildAnimalSpawner __instance, ref IntVec3 loc, 
                ref bool __result, Map ___map)
            {
                if (ZLevelsMod.settings.allowZLevelsInfestation)
                {
                    try
                    {
                        bool result = false;
                        var comp = ZUtils.GetMapComponentZLevel(___map);
                        var ZTracker = ZUtils.ZTracker;
                        if (___map.Parent is MapParent_ZLevel && comp != null
                            && ZTracker.GetUpperLevel(___map.Tile, ___map) != null &&
                            !ZUtils.GetMapComponentZLevel(ZTracker.GetUpperLevel(___map.Tile, ___map))
                                .hasCavesBelow.GetValueOrDefault(false))
                        {
                            result = false;
                        }
                        else
                        {
                            PawnKindDef pawnKindDef = (from a in ___map.Biome.AllWildAnimals
                                                       where ___map.mapTemperature.SeasonAcceptableFor(a.race)
                                                       select a)
                                                       .RandomElementByWeight((PawnKindDef def) =>
                                                       ___map.Biome.CommonalityOfAnimal(def) / def.wildGroupSize.Average);
                            if (pawnKindDef == null)
                            {
                                Log.Error("No spawnable animals right now.");
                                result = false;
                            }
                            else
                            {
                                ZLogger.Message("Spawning animal: " + pawnKindDef + " in biome: " + ___map.Biome);
                                int randomInRange = pawnKindDef.wildGroupSize.RandomInRange;
                                int radius = Mathf.CeilToInt(Mathf.Sqrt((float)pawnKindDef.wildGroupSize.max));
                                if (___map.Parent is MapParent_ZLevel && !loc.Walkable(___map))
                                {
                                    loc = CellFinderLoose.RandomCellWith((IntVec3 sq) => sq.Walkable(___map), ___map);
                                }
                                for (int i = 0; i < randomInRange; i++)
                                {
                                    IntVec3 loc2 = CellFinder.RandomClosewalkCellNear(loc, ___map, radius, null);
                                    GenSpawn.Spawn(PawnGenerator.GeneratePawn(pawnKindDef, null), loc2, ___map, WipeMode.Vanish);
                                }
                                result = true;
                            }
                        }
                        __result = result;
                    }
                    catch (Exception ex)
                    {
                        Log.Error("[Z-Levels] SpawnAnimalPrefix patch produced an error. That should not happen and will break things. Send a Hugslib log to the Z-Levels developers. Error message: " + ex, true);
                    }
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(IncidentWorker_Infestation), "TryExecuteWorker")]
        internal class Patch_Infestation_TryExecuteWorker
        {
            [HarmonyPrefix]
            private static bool PreFix(ref bool __result, IncidentParms parms)
            {
                if (ZLevelsMod.settings.allowZLevelsInfestation)
                {
                    try
                    {
                        Map map = (Map)parms.target;
                        var comp = ZUtils.GetMapComponentZLevel(map);
                        if (comp.hasCavesBelow.HasValue && comp.hasCavesBelow.Value)
                        {
                            var foods = map.listerThings.AllThings.Where(x => !(x is Plant) && !(x is Pawn)
                                    && x.GetStatValue(StatDefOf.Nutrition, true) > 0.1f);
                            if (foods != null && foods.Any())
                            {
                                List<PawnKindDef> infestators = new List<PawnKindDef>
                            {
                                ZLevelsDefOf.ZL_UndegroundBiome.AllWildAnimals.RandomElement()
                            };
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
                            ZLogger.Message("The map has no caves below to generate infestation");
                        }
                        __result = false;
                    }
                    catch (Exception ex)
                    {
                        Log.Error("[Z-Levels] Patch_Infestation_TryExecuteWorker patch produced an error. That should not happen and will break things. Send a Hugslib log to the Z-Levels developers. Error message: " + ex, true);
                    }
                    return false;
                }
                return true;
            }
        }
    }
}

