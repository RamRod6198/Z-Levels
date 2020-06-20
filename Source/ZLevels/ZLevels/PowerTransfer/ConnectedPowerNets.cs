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
        }

        public override void ExposeData()
        {
            base.ExposeData();
            //Scribe_Collections.Look<int, List<CompPowerZTransmitter>>(ref this.powerNets, "powerNets", 
            //    LookMode.Value, LookMode.Deep, ref this.PowerNetIds, ref this.CompValues);
            Scribe_Collections.Look<int, PowerData>(ref this.powerNets2, "powerNets2",
                LookMode.Value, LookMode.Deep, ref this.PowerNetIds, ref this.CompValues2);
        }

        public List<PowerData> powerDatas = new List<PowerData>();
        public Dictionary<int, List<CompPowerZTransmitter>> powerNets = new Dictionary<int, List<CompPowerZTransmitter>>();
        public Dictionary<int, PowerData> powerNets2 = new Dictionary<int, PowerData>();

        public List<List<CompPowerZTransmitter>> CompValues = new List<List<CompPowerZTransmitter>>();
        public List<PowerData> CompValues2 = new List<PowerData>();

        public List<int> PowerNetIds = new List<int>();

    }
}

