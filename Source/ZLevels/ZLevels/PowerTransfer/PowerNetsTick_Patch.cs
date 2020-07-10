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
            private static void Prefix()
            {
                try
                {
                    var connectedPowerNets = Current.Game.GetComponent<ConnectedPowerNets>();
                    int? keyToRemove = null;
                    if (Find.TickManager.TicksGame % 60 == 0)
                    {
                        foreach (var powerNet in connectedPowerNets.powerNets)
                        {
                            foreach (var powerNet2 in connectedPowerNets.powerNets)
                            {
                                foreach (var c1 in powerNet.Value)
                                {
                                    foreach (var c2 in powerNet2.Value)
                                    {
                                        if (powerNet.Key != powerNet2.Key && c1 == c2)
                                        {
                                            //Log.Message("PowerNetsTick_Patch: " + c1 + " == " + c2);
                                            if (powerNet.Value.Count > powerNet2.Value.Count)
                                            {
                                                keyToRemove = powerNet2.Key;
                                            }
                                            else if (powerNet.Value.Count < powerNet2.Value.Count)
                                            {
                                                keyToRemove = powerNet.Key;
                                            }
                                            else
                                            {
                                                keyToRemove = powerNet2.Key;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (keyToRemove.HasValue)
                    {
                        connectedPowerNets.powerNets.Remove(keyToRemove.Value);
                    }

                    foreach (var powerNet in connectedPowerNets.powerNets)
                    {
                        powerNet.Value.RemoveAll(x => x == null);
                        if (powerNet.Value.Count > 1)
                        {
                            Dictionary<CompPowerZTransmitter, float> compPowers = new Dictionary<CompPowerZTransmitter, float>();
                            foreach (var comp in powerNet.Value)
                            {
                                comp.powerOutputInt = 0;
                                //Log.Message("1 Powernet has: " + comp + " - " + comp.PowerNet.Map + " - " + comp.GetHashCode() + " - " + comp.PowerNet.GetHashCode(), true);
                            }

                            foreach (var comp in powerNet.Value)
                            {
                                var newPowerNet = comp.PowerNet.transmitters.FirstOrDefault()?.PowerNet;

                                if (newPowerNet != null && comp.PowerNet != newPowerNet)
                                {
                                    foreach (var comp2 in powerNet.Value)
                                    {
                                        if (comp2.PowerNet.Map == newPowerNet.Map
                                            && comp2.PowerNet != newPowerNet)
                                        {
                                            //Log.Message("Replacing " + comp2.GetHashCode() + " - " + comp2.PowerNet.GetHashCode() 
                                            //    + " with " + newPowerNet.GetHashCode() + " in " + comp2.PowerNet.Map, true);
                                            comp2.PowerNet.DeregisterConnector(comp2);
                                            comp2.transNet = newPowerNet;
                                            newPowerNet.RegisterConnector(comp2);
                                        }
                                    }
                                }

                                if (comp.PowerNet != null)
                                {
                                    compPowers[comp] = comp.PowerNet.CurrentEnergyGainRate()
                                        / CompPower.WattsToWattDaysPerTick;
                                }
                            }
                            var newValue = compPowers.Sum(x => x.Value) / compPowers.Count;
                            //Log.Message("------------------", true);
                            //foreach (var comp in powerNet.Value)
                            //{
                            //    Log.Message("2 Powernet has: " + comp + " - " + comp.PowerNet.Map + " - " + comp.GetHashCode() + " - " + comp.PowerNet.GetHashCode(), true);
                            //}
                            //Log.Message("------------------", true);
                            foreach (var comp in powerNet.Value)
                            {
                                if (compPowers.ContainsKey(comp))
                                {
                                    if (!comp.PowerNet.powerComps.Contains(comp))
                                    {
                                        comp.PowerNet.powerComps.RemoveAll(x => x is CompPowerZTransmitter trans && trans.PowerNet != comp.PowerNet);
                                        //Log.Message("Adding " + comp + " - " + comp.GetHashCode() + " to " + comp.PowerNet.GetHashCode(), true);
                                        comp.PowerNet.powerComps.Add(comp);
                                    }
                                    //Log.Message("comp.powerNet energy: " + comp.PowerNet.Map + " - " + comp.PowerNet.CurrentEnergyGainRate()
                                    //        / CompPower.WattsToWattDaysPerTick + " - " + comp.GetHashCode() + " - "
                                    //        + comp.PowerNet.GetHashCode(), true);
                                    //foreach (var p in comp.PowerNet.powerComps)
                                    //{
                                    //    Log.Message("powerComp: " + p + " - " + p.powerOutputInt + " - " + p.GetHashCode() + " - " + p.PowerNet.GetHashCode(), true);
                                    //}
                                    
                                    comp.powerOutputInt = newValue - compPowers[comp];
                                    //Log.Message("comp.powerOutputInt: " + comp.powerOutputInt + " - " 
                                    //    + comp.PowerNet.Map + " - " + comp.PowerNet.CurrentEnergyGainRate() 
                                    //    / CompPower.WattsToWattDaysPerTick + " - " + comp.GetHashCode() + " - "
                                    //    + comp.PowerNet.GetHashCode(), true);
                                }
                            }
                            //Log.Message("powerNet.Value: " + powerNet.Value.Count + " - newValue: " + newValue, true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("[Z-Levels] PowerNetsTick_Patch patch produced an error. That should not happen and will break things. Send a Hugslib log to the Z-Levels developers. Error message: " + ex, true);
                }
            }
        }
    }
}

