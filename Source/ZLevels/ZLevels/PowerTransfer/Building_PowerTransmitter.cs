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
            if (baseComp.transmitter == null)
            {
                baseComp.transmitter = this;
            }
            var powerComps = new List<CompPowerZTransmitter>();
            if (connectedPowerNets.powerNets == null)
            {
                //Log.Message(this + " Add1 ", true);
                powerComps.Add(baseComp);
                connectedPowerNets.powerNets = new Dictionary<int, List<CompPowerZTransmitter>>();
                connectedPowerNets.powerNets.Add(0, powerComps);
            }
            else if (connectedPowerNets.powerNets.Count == 0)
            {
                //Log.Message(this + " Add2 ", true);
                powerComps.Add(baseComp);
                connectedPowerNets.powerNets.Add(0, powerComps);
            }
            else if (connectedPowerNets.powerNets.Values.Where(x => x
                .Where(y => y.PowerNet == baseComp.PowerNet).Count() > 0).Count() == 0)
            {
                //Log.Message(this + " Add3 ", true);
                powerComps.Add(baseComp);
                int maxKey = connectedPowerNets.powerNets.Max(x => x.Key);
                connectedPowerNets.powerNets.Add(maxKey + 1, powerComps);
            }
            else
            {
                powerComps = connectedPowerNets.powerNets.Values.Where(x => x
                    .Where(y => y.PowerNet == baseComp.PowerNet).Count() == 1).ToList().First();
            }

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
                //Log.Message(this + ZTracker.GetMapInfo(this.Map) + " works", true);
                var upperMap = ZTracker.GetUpperLevel(this.Map.Tile, this.Map);
                var lowerMap = ZTracker.GetLowerLevel(this.Map.Tile, this.Map);
            
                if (upperMap != null && this.upperPowerComp == null)
                {
                    foreach (var pos in GenRadial.RadialCellsAround(this.Position, 5f, true))
                    {
                        foreach (var t in pos.GetThingList(upperMap))
                        {
                            if (t.TryGetComp<CompPowerTransmitter>() != null)
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
                                    upperPowerComp.powerOutputInt = 1;
                                    upperPowerComp.transmitter = upperTransmitter;
                                    upperPowerComp.transNet = upperComp.transNet;
                                    upperPowerComp.PowerOn = true;
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
                            if (t.TryGetComp<CompPowerTransmitter>() != null)
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
                                    lowerPowerComp.powerOutputInt = 1;
                                    lowerPowerComp.transmitter = lowerTransmitter;
                                    lowerPowerComp.transNet = lowerComp.transNet;
                                    lowerPowerComp.PowerOn = true;
                                    lowerComp.PowerNet.powerComps.Add(lowerPowerComp);
                                }
                                break;
                            }
                        }
                    }
                }

                //if (upperPowerComp != null)
                //{
                //    if (!powerComps.Contains(upperPowerComp))
                //    {
                //        powerComps.Add(upperPowerComp);
                //    }
                //    //Log.Message(this + " - upperPowerComp: " + upperPowerComp + " - " + upperPowerComp.parent, true);
                //}
                //if (lowerPowerComp != null)
                //{
                //    if (!powerComps.Contains(lowerPowerComp))
                //    {
                //        powerComps.Add(lowerPowerComp);
                //    }
                //    //Log.Message(this + " - lowerPowerComp: " + lowerPowerComp + " - " + lowerPowerComp.parent, true);
                //}
                //
                //if (upperTransmitter != null)
                ////Log.Message(this + " - upperTransmitter: " + upperTransmitter, true);
                //if (lowerTransmitter != null)
                ////Log.Message(this + " - lowerTransmitter: " + lowerTransmitter, true);
                //
                //foreach (var powerNet in connectedPowerNets.powerNets)
                //{
                //    foreach (var comp in powerNet.Value)
                //    {
                //        //Log.Message(powerNet.Key + " - " + ZTracker.GetMapInfo(comp.PowerNet.Map) + " - transmitter: " + comp.transmitter, true);
                //    }
                //}


                if (upperTransmitter != null && upperTransmitter.Spawned
                    && lowerTransmitter != null && lowerTransmitter.Spawned)
                {
                    var upperComp = upperTransmitter.TryGetComp<CompPowerTransmitter>();
                    var lowerComp = lowerTransmitter.TryGetComp<CompPowerTransmitter>();
                
                    var upperPowerComp = (CompPowerZTransmitter)upperComp.PowerNet.powerComps.Where(x => x is CompPowerZTransmitter).FirstOrDefault();
                    if (upperPowerComp == null)
                    {
                        upperPowerComp = new CompPowerZTransmitter();
                        upperPowerComp.parent = this;
                        upperPowerComp.Initialize(new CompProperties_PowerZTransmitter());
                        upperPowerComp.powerOutputInt = 0;
                        upperPowerComp.PowerOn = true;
                        upperComp.PowerNet.powerComps.Add(upperPowerComp);
                    }
                
                    var lowerPowerComp = (CompPowerZTransmitter)lowerComp.PowerNet.powerComps.Where(x => x is CompPowerZTransmitter).FirstOrDefault();
                    if (lowerPowerComp == null)
                    {
                        lowerPowerComp = new CompPowerZTransmitter();
                        lowerPowerComp.parent = this;
                        lowerPowerComp.Initialize(new CompProperties_PowerZTransmitter());
                        lowerPowerComp.powerOutputInt = 0;
                        lowerPowerComp.PowerOn = true;
                        lowerComp.PowerNet.powerComps.Add(lowerPowerComp);
                    }
                
                    var origBase = (baseComp.PowerNet.CurrentEnergyGainRate() / CompPower.WattsToWattDaysPerTick)
                        - baseComp.powerOutputInt;
                    
                    var origUpper = (upperComp.PowerNet.CurrentEnergyGainRate() / CompPower.WattsToWattDaysPerTick)
                        - upperPowerComp.powerOutputInt;
                    
                    var origLower = (lowerComp.PowerNet.CurrentEnergyGainRate() / CompPower.WattsToWattDaysPerTick)
                        - lowerPowerComp.powerOutputInt;
                    
                    var newValue = (origBase + origUpper + origLower) / 3;
                    baseComp.powerOutputInt = newValue - origBase;
                    upperPowerComp.powerOutputInt = newValue - origUpper;
                    lowerPowerComp.powerOutputInt = newValue - origLower;
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
            Log.Message("-----------------", true);
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

        public ConnectedPowerNets connectedPowerNets = Current.Game.GetComponent<ConnectedPowerNets>();

        public ZLevelsManager ZTracker = Current.Game.GetComponent<ZLevelsManager>();

        private Sustainer wickSustainer;

        Thing upperTransmitter;

        Thing lowerTransmitter;

        CompPowerZTransmitter upperPowerComp = null;

        CompPowerZTransmitter lowerPowerComp = null;

    }
}