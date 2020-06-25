using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
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
    public static class PowerPatches
    {
        [HarmonyPatch(typeof(PowerNetManager))]
        [HarmonyPatch("PowerNetsTick")]
        public class PowerNetsTick_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix()
            {
                try
                {
                    var connectedPowerNets = Current.Game.GetComponent<ConnectedPowerNets>();
                    foreach (var powerNet in connectedPowerNets.powerNets)
                    {
                        powerNet.Value.RemoveAll(x => x == null);
                        if (powerNet.Value.Count > 0)
                        {
                            Dictionary<CompPowerZTransmitter, float> compPowers = new Dictionary<CompPowerZTransmitter, float>();
                            foreach (var comp in powerNet.Value)
                            {
                                if (comp.PowerNet != null)
                                {
                                    compPowers[comp] = (comp.PowerNet.CurrentEnergyGainRate()
                                        / CompPower.WattsToWattDaysPerTick) - comp.powerOutputInt;
                                }
                            }
                            var newValue = compPowers.Sum(x => x.Value) / compPowers.Count;
                            foreach (var comp in powerNet.Value)
                            {
                                if (compPowers.ContainsKey(comp))
                                {
                                    comp.powerOutputInt = newValue - compPowers[comp];
                                }
                                //Log.Message(pawn + " - powerNet.Value: " + powerNet.Value.Count + " - newValue: " + newValue, true);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("[Z-Levels] PowerNetsTick_Patch patch produced an error. That should not happen and will break things. Send a Hugslib log to the Z-Levels developers. Error message: " + ex, true);
                }
                return true;
            }
        }
    }
}

