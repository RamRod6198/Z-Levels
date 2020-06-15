using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Noise;
using Verse.Sound;

namespace ZLevels
{
    [StaticConstructorOnStartup]
    public class Building_PowerTransmitter : Building
    {
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.ticksToExplode, "ticksToExplode", 0, false);
            Scribe_Values.Look<bool>(ref this.active, "active", false, false);
        }
        public override void Draw()
        {
            base.Draw();
            CompPowerBattery comp = base.GetComp<CompPowerBattery>();
            if (this.ticksToExplode > 0 && base.Spawned)
            {
                base.Map.overlayDrawer.DrawOverlay(this, OverlayTypes.BurningWick);
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            base.Destroy(mode);
            if (this.upperPowerComp != null)
            {
                this.upperPowerComp.PowerNet.powerComps.Remove(this.upperPowerComp);
            }
            if (this.lowerPowerComp != null)
            {
                this.lowerPowerComp.PowerNet.powerComps.Remove(this.lowerPowerComp);
            }
        }

        public float GetPowerNetCurrentGain(PowerNet net)
        {
            float num = 0f;
            for (int i = 0; i < net.powerComps.Count; i++)
            {
                if (net.powerComps[i].PowerOn)
                {
                    Log.Message("net.powerComps[i]: " + net.powerComps[i] + " - " + net.powerComps[i].parent, true);
                    num += net.powerComps[i].EnergyOutputPerTick;
                }
            }
            return num;
        }
        public override void Tick()
        {
            base.Tick();
            var baseComp = this.GetComp<CompPowerZTransmitter>();
            if (baseComp.PowerNet.transmitters.Where(x => x is CompPowerZTransmitter).Count() > 1
                && this.lowerPowerComp == null && this.upperPowerComp == null)
            {
                baseComp.PowerNet.transmitters.Remove(baseComp);
                baseComp.PowerNet.powerComps.Remove(baseComp);
                active = false;
            }
            else if (baseComp != null)
            {
                if (this.active == false) this.active = true;
                Log.Message(this + ZTracker.GetMapInfo(this.Map) + " works");
                var upperMap = ZTracker.GetUpperLevel(this.Map.Tile, this.Map);
                var lowerMap = ZTracker.GetLowerLevel(this.Map.Tile, this.Map);

                if (upperMap != null && this.upperPowerComp == null)
                {
                    foreach (var pos in GenRadial.RadialCellsAround(this.Position, 5f, true))
                    {
                        foreach (var t in pos.GetThingList(upperMap))
                        {
                            if (t.TryGetComp<CompPowerTransmitter>() != null 
                                && t.TryGetComp<CompPowerTransmitter>().PowerNet.powerComps
                                .Where(x => x is CompPowerZTransmitter comp 
                                && comp.parent is Building_PowerTransmitter transmitter 
                                && transmitter.lowerPowerComp != null)
                                .Count() == 0)
                                {
                                upperTransmitter = t;
                                var upperComp = upperTransmitter.TryGetComp<CompPowerTransmitter>();
                                upperPowerComp = (CompPowerZTransmitter)upperComp.PowerNet.powerComps
                                    .Where(x => x is CompPowerZTransmitter).FirstOrDefault();
                                if (upperPowerComp == null)
                                {
                                    upperPowerComp = new CompPowerZTransmitter();
                                    upperPowerComp.parent = this;
                                    upperPowerComp.Initialize(new CompProperties_PowerZTransmitter());
                                    upperPowerComp.powerOutputInt = 0;
                                    upperPowerComp.PowerOn = true;
                                    upperPowerComp.mainComp = false;
                                    upperComp.PowerNet.powerComps.Add(upperPowerComp);
                                }
                                break;
                            }
                        }
                    }
                }
                if (lowerMap != null && this.lowerPowerComp == null)
                {
                    foreach (var pos in GenRadial.RadialCellsAround(this.Position, 5f, true))
                    {
                        foreach (var t in pos.GetThingList(lowerMap))
                        {
                            if (t.TryGetComp<CompPowerTransmitter>() != null 
                                && t.TryGetComp<CompPowerTransmitter>().PowerNet.powerComps
                                .Where(x => x is CompPowerZTransmitter comp
                                && comp.parent is Building_PowerTransmitter transmitter
                                && transmitter.upperPowerComp != null)
                                .Count() == 0)
                            {
                                lowerTransmitter = t;
                                var lowerComp = lowerTransmitter.TryGetComp<CompPowerTransmitter>();
                                lowerPowerComp = (CompPowerZTransmitter)lowerComp.PowerNet
                                    .powerComps.Where(x => x is CompPowerZTransmitter).FirstOrDefault();
                                if (lowerPowerComp == null)
                                {
                                    lowerPowerComp = new CompPowerZTransmitter();
                                    lowerPowerComp.parent = this;
                                    lowerPowerComp.Initialize(new CompProperties_PowerZTransmitter());
                                    lowerPowerComp.powerOutputInt = 0;
                                    lowerPowerComp.PowerOn = true;
                                    lowerPowerComp.mainComp = false;
                                    lowerComp.PowerNet.powerComps.Add(lowerPowerComp);
                                }
                                break;
                            }
                        }
                    }
                }

                Dictionary<CompPowerZTransmitter, float> compPowers = new Dictionary<CompPowerZTransmitter, float>();
                float origBase = (baseComp.PowerNet.CurrentEnergyGainRate() / CompPower.WattsToWattDaysPerTick)
                    - baseComp.powerOutputInt;
                Log.Message(this + " has upperTransmitter: " + (upperTransmitter != null).ToString());
                Log.Message(this + " has lowerTransmitter: " + (lowerTransmitter != null).ToString());
                if (upperTransmitter != null && upperTransmitter.Spawned)
                {
                    var upperComp = upperTransmitter.TryGetComp<CompPowerTransmitter>();
                    if (!upperComp.PowerNet.powerComps.Contains(upperPowerComp))
                    {
                        upperComp.PowerNet.powerComps.Add(upperPowerComp);
                    }
                    compPowers[upperPowerComp] = (upperComp.PowerNet.CurrentEnergyGainRate()
                        / CompPower.WattsToWattDaysPerTick) - upperPowerComp.powerOutputInt;
                    Log.Message(this + " upperPowerComp: " + upperPowerComp + " - " + compPowers[upperPowerComp]);
                }

                if (lowerTransmitter != null && lowerTransmitter.Spawned)
                {
                    var lowerComp = lowerTransmitter.TryGetComp<CompPowerTransmitter>();
                    if (!lowerComp.PowerNet.powerComps.Contains(lowerPowerComp))
                    {
                        lowerComp.PowerNet.powerComps.Add(lowerPowerComp);
                    }
                    compPowers[lowerPowerComp] = (lowerComp.PowerNet.CurrentEnergyGainRate()
                                / CompPower.WattsToWattDaysPerTick) - lowerPowerComp.powerOutputInt;
                    Log.Message(this + " lowerPowerComp: " + lowerPowerComp + " - " + compPowers[lowerPowerComp]);

                }
                if (compPowers.Count > 0)
                {
                    var newValue = (origBase + compPowers.Sum(x => x.Value)) / (compPowers.Count + 1);
                    Log.Message(this + " newValue: " + newValue, true);
                    baseComp.powerOutputInt = newValue - origBase;
                    Log.Message(this + " baseComp.powerOutputInt: " + baseComp.powerOutputInt);
                    if (upperPowerComp != null && compPowers.ContainsKey(upperPowerComp))
                    {
                        upperPowerComp.powerOutputInt = newValue - compPowers[upperPowerComp];
                    }
                    if (lowerPowerComp != null && compPowers.ContainsKey(lowerPowerComp))
                    {
                        lowerPowerComp.powerOutputInt = newValue - compPowers[lowerPowerComp];
                    }
                }
            }
            if (this.ticksToExplode > 0)
            {
                if (this.wickSustainer == null)
                {
                    this.StartWickSustainer();
                }
                else
                {
                    this.wickSustainer.Maintain();
                }
                this.ticksToExplode--;
                if (this.ticksToExplode == 0)
                {
                    IntVec3 randomCell = this.OccupiedRect().RandomCell;
                    float radius = Rand.Range(0.5f, 1f) * 3f;
                    GenExplosion.DoExplosion(randomCell, base.Map, radius, DamageDefOf.Flame, null, -1, -1f, null, null, null, null, null, 0f, 1, false, null, 0f, 1, 0f, false, null, null);
                }
            }
            Log.Message("-----------------");
        }
        public override void PostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostApplyDamage(dinfo, totalDamageDealt);
            if (!base.Destroyed && this.ticksToExplode == 0 && dinfo.Def == DamageDefOf.Flame && Rand.Value < 0.05f && base.GetComp<CompPowerBattery>().StoredEnergy > 500f)
            {
                this.ticksToExplode = Rand.Range(70, 150);
                this.StartWickSustainer();
            }
        }

        private void StartWickSustainer()
        {
            SoundInfo info = SoundInfo.InMap(this, MaintenanceType.PerTick);
            this.wickSustainer = SoundDefOf.HissSmall.TrySpawnSustainer(info);
        }

        public override string GetInspectString()
        {
            var baseComp = this.GetComp<CompPowerZTransmitter>();
            if (baseComp.PowerNet == null)
            {
                return "PowerNotConnected".Translate();
            }
            string value = (baseComp.PowerNet.CurrentEnergyGainRate() / CompPower.WattsToWattDaysPerTick).ToString("F0");
            string value2 = baseComp.PowerNet.CurrentStoredEnergy().ToString("F0");
            return "PowerConnectedRateStored".Translate(value, value2);
        }

        public bool active; 

        private int ticksToExplode;

        public ZLevelsManager ZTracker = Current.Game.GetComponent<ZLevelsManager>();

        private Sustainer wickSustainer;

        Thing upperTransmitter;

        Thing lowerTransmitter;

        CompPowerZTransmitter upperPowerComp = null;

        CompPowerZTransmitter lowerPowerComp = null;

    }
}