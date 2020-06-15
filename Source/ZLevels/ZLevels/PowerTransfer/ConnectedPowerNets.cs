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
    public class ConnectedPowerNets : GameComponent
    {
        public ConnectedPowerNets()
        {

        }

        public ConnectedPowerNets(Game game)
        {

        }
        public override void GameComponentTick()
        {
            base.GameComponentTick();
            foreach (var powerNet in this.powerNets)
            {
                Dictionary<CompPowerZTransmitter, float> compPowers = new Dictionary<CompPowerZTransmitter, float>();
                foreach (var comp in powerNet.Value)
                {
                    compPowers[comp] = (comp.PowerNet.CurrentEnergyGainRate()
                        / CompPower.WattsToWattDaysPerTick) - comp.powerOutputInt;
                    //Log.Message(powerNet.Key + " - " + comp.PowerNet.Map + " - transmitter: " + comp.transmitter, true);
                    if (compPowers.Count > 0)
                    {
                        var newValue = compPowers.Sum(x => x.Value) / compPowers.Count;
                        //Log.Message(this + " newValue: " + newValue, true);
                        foreach (var comp2 in compPowers)
                        {
                            comp2.Key.powerOutputInt = newValue - comp2.Value;
                        }
                    }
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look<int, List<CompPowerZTransmitter>>(ref this.powerNets, "powerNets", 
                LookMode.Value, LookMode.Deep, ref this.PowerNetIds, ref this.CompValues);
        }

        public Dictionary<int, List<CompPowerZTransmitter>> powerNets = new Dictionary<int, List<CompPowerZTransmitter>>();

        public List<List<CompPowerZTransmitter>> CompValues = new List<List<CompPowerZTransmitter>>();

        public List<int> PowerNetIds = new List<int>();

    }
}
