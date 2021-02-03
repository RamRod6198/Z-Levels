using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ZLevels
{
    [HarmonyPatch(typeof(World), "WorldPostTick")]
    public class WorldPostTick_Patch
    {
        private static void Postfix()
        {
            ZUtils.ZTracker.connectedPowerNets.Tick();
        }
    }

    [HarmonyPatch(typeof(PowerNet), "CurrentStoredEnergy")]
    public class CurrentStoredEnergy_Patch
    {
        private static bool Prefix(PowerNet __instance, ref float __result)
        {
            if (ZUtils.ZTracker.connectedPowerNets.connectedPowerNetsDict.TryGetValue(__instance, out ConnectedPowerNet connectedPowerNet))
            {
                __result = connectedPowerNet.TotalStoredEnergy;
                return false;
            }
            return true;
        }
    }
    public class ConnectedPowerNet
    {
        public ConnectedPowerNet(CompPowerZTransmitter comp)
        {
            connectedTransmitters = new List<CompPowerZTransmitter>();
            AddTransmitter(comp);
        }

        public void AddTransmitter(CompPowerZTransmitter transmitter)
        {
            connectedTransmitters.Add(transmitter);
        }

        public void RemoveTransmitter(CompPowerZTransmitter transmitter)
        {
            connectedTransmitters.Remove(transmitter);
        }

        public void Tick()
        {
            var totalEnergy = TotalEnergy;
            var powerNets = new Dictionary<PowerNet, List<CompPowerZTransmitter>>();
            foreach (var transmitter in connectedTransmitters)
            {
                if (powerNets.ContainsKey(transmitter.PowerNet))
                {
                    powerNets[transmitter.PowerNet].Add(transmitter);
                }
                else
                {
                    powerNets[transmitter.PowerNet] = new List<CompPowerZTransmitter> { transmitter };
                }
            }
            foreach (var powerNet in powerNets)
            {
                foreach (var comp in powerNet.Value)
                {
                    comp.powerOutputInt = 0;
                }
                var localEnergy = powerNet.Key.CurrentEnergyGainRate() / CompPower.WattsToWattDaysPerTick;
                var diffEnergy = totalEnergy - localEnergy;
                foreach (var comp in powerNet.Value)
                {
                    comp.powerOutputInt = diffEnergy / powerNet.Value.Count();
                }
            }
        }

        public float CurrentStoredEnergy(PowerNet powerNet)
        {
            float num = 0f;
            for (int i = 0; i < powerNet.batteryComps.Count; i++)
            {
                num += powerNet.batteryComps[i].StoredEnergy;
            }
            return num;
        }

        private List<CompPowerZTransmitter> connectedTransmitters;
        public List<CompPowerZTransmitter> ConnectedTransmitters => connectedTransmitters;
        public HashSet<PowerNet> ConnectedPowerNets => ConnectedTransmitters.Select(x => x.PowerNet).ToHashSet();
        public float TotalEnergy => ConnectedPowerNets.Sum(x => x.CurrentEnergyGainRate() / CompPower.WattsToWattDaysPerTick);
        public float TotalStoredEnergy => ConnectedPowerNets.Sum(x => CurrentStoredEnergy(x));
    }
    public class ConnectedPowerNets
    {
        public ConnectedPowerNets()
        {
            powerNets = new Dictionary<int, ConnectedPowerNet>();
            connectedPowerNetsDict = new Dictionary<PowerNet, ConnectedPowerNet>();
        }
        public void RegisterTransmitter(CompPowerZTransmitter comp)
        {
            Log.Message("Registering: " + comp);
            var lowerMap = ZUtils.ZTracker.GetLowerLevel(comp.parent.Tile, comp.parent.Map);
            var upperMap = ZUtils.ZTracker.GetUpperLevel(comp.parent.Tile, comp.parent.Map);
            foreach (var powerNet in powerNets)
            {
                if (!powerNet.Value.ConnectedTransmitters.Contains(comp))
                {
                    if (powerNet.Value.ConnectedTransmitters.FirstOrDefault()?.PowerNet == comp.PowerNet)
                    {
                        AddTransmitter(powerNet.Value, comp);
                        return;
                    }
                    if (lowerMap != null)
                    {
                        var lowerComps = powerNet.Value.ConnectedTransmitters.Where(x => x.parent.Map == lowerMap);
                        foreach (var lowerComp in lowerComps)
                        {
                            if (lowerComp.parent.Position.DistanceTo(comp.parent.Position) < 3)
                            {
                                AddTransmitter(powerNet.Value, comp);
                                return;
                            }
                        }
                    }
                    if (upperMap != null)
                    {
                        var upperComps = powerNet.Value.ConnectedTransmitters.Where(x => x.parent.Map == upperMap);
                        foreach (var upperComp in upperComps)
                        {
                            if (upperComp.parent.Position.DistanceTo(comp.parent.Position) < 3)
                            {
                                AddTransmitter(powerNet.Value, comp);
                                return;
                            }
                        }
                    }
                }
                else
                {
                    return;
                }
            }
            var connectedPowerNet = new ConnectedPowerNet(comp);
            AddTransmitter(connectedPowerNet, comp);
            powerNets.Add(powerNets.Count + 1, connectedPowerNet);
        }

        public void AddTransmitter(ConnectedPowerNet connectedPowerNet, CompPowerZTransmitter compPowerZTransmitter)
        {
            connectedPowerNet.AddTransmitter(compPowerZTransmitter);
            connectedPowerNetsDict[compPowerZTransmitter.PowerNet] = connectedPowerNet;
        }
        public void ChangePowerNet(CompPowerZTransmitter compPowerZTransmitter)
        {
            var connectedPowerNet = powerNets.Where(x => x.Value.ConnectedPowerNets.Contains(compPowerZTransmitter.PowerNet)).FirstOrDefault();
            if (connectedPowerNet.Value != null)
            {
                connectedPowerNetsDict[compPowerZTransmitter.PowerNet] = connectedPowerNet.Value;
            }
        }
        public void DeregisterTransmitter(CompPowerZTransmitter comp)
        {
            Log.Message("Deregistering: " + comp);
            foreach (var powerNet in powerNets)
            {
                powerNet.Value.RemoveTransmitter(comp);
            }
        }
        public void Tick()
        {
            foreach (var powerNet in powerNets)
            {
                powerNet.Value.Tick();
            }
        }

        public Dictionary<int, ConnectedPowerNet> powerNets = new Dictionary<int, ConnectedPowerNet>();
        public Dictionary<PowerNet, ConnectedPowerNet> connectedPowerNetsDict = new Dictionary<PowerNet, ConnectedPowerNet>();
    }
}

