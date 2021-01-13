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
            private static void Prefix()
            {
                var connectedPowerNets = ZUtils.ZTracker.connectedPowerNets;
                //if (Find.TickManager.TicksGame % 60 == 0)
                //{
                //    int? keyToRemove = null;
                //    foreach (var powerNet in connectedPowerNets.powerNets)
                //    {
                //        var zTransmitters1 = powerNet.Value;
                //        foreach (var powerNet2 in connectedPowerNets.powerNets)
                //        {
                //            var zTransmitters2 = powerNet2.Value;
                //            foreach (var c1 in zTransmitters1)
                //            {
                //                foreach (var c2 in zTransmitters2)
                //                {
                //                    if (powerNet.Key != powerNet2.Key && c1 == c2)
                //                    {
                //                        if (zTransmitters1.Count > zTransmitters2.Count)
                //                        {
                //                            keyToRemove = powerNet2.Key;
                //                        }
                //                        else if (zTransmitters1.Count < zTransmitters2.Count)
                //                        {
                //                            keyToRemove = powerNet.Key;
                //                        }
                //                        else
                //                        {
                //                            keyToRemove = powerNet2.Key;
                //                        }
                //                    }
                //                }
                //            }
                //        }
                //    }
                //    if (keyToRemove.HasValue)
                //    {
                //        connectedPowerNets.powerNets.Remove(keyToRemove.Value);
                //    }
                //}

                foreach (var powerNet in connectedPowerNets.powerNets)
                {
                    foreach (var test in powerNet.Value)
                    {
                        if (powerNet.Value.Count != 2)
                        {
                            Log.Message("Isn't connected: " + test + " - " + test.parent.Map + " - " + powerNet.Value.Count);
                        }
                    }
                    var zTransmitters = powerNet.Value;
                    zTransmitters.RemoveAll(x => x == null || x.PowerNet == null);
                    if (zTransmitters.Count > 1)
                    {
                        Dictionary<CompPowerZTransmitter, float> compPowers = new Dictionary<CompPowerZTransmitter, float>();
                        foreach (var comp in zTransmitters)
                        {
                            comp.powerOutputInt = 0;
                        }
                        foreach (var comp in zTransmitters)
                        {
                            var newPowerNet = comp.PowerNet.transmitters.FirstOrDefault()?.PowerNet;
                            if (newPowerNet != null && comp.PowerNet != newPowerNet)
                            {
                                foreach (var comp2 in zTransmitters)
                                {
                                    if (comp2.PowerNet.Map == newPowerNet.Map && comp2.PowerNet != newPowerNet)
                                    {
                                        comp2.PowerNet.DeregisterConnector(comp2);
                                        comp2.transNet = newPowerNet;
                                        newPowerNet.RegisterConnector(comp2);
                                    }
                                }
                            }
                            if (comp.PowerNet != null)
                            {
                                compPowers[comp] = comp.PowerNet.CurrentEnergyGainRate() / CompPower.WattsToWattDaysPerTick;
                            }
                            else
                            {
                                Log.Message(comp + " is discarded!");
                            }
                        }
                        var newValue = compPowers.Sum(x => x.Value) / compPowers.Count;
                        foreach (var comp in zTransmitters)
                        {
                            if (compPowers.TryGetValue(comp, out float value))
                            {
                                if (!comp.PowerNet.powerComps.Contains(comp))
                                {
                                    comp.PowerNet.powerComps.RemoveAll(x => x is CompPowerZTransmitter trans && trans.PowerNet != comp.PowerNet);
                                    comp.PowerNet.powerComps.Add(comp);
                                }
                                comp.powerOutputInt = newValue - value;
                            }
                        }
                    }
                }
            }
        }
    }
}

