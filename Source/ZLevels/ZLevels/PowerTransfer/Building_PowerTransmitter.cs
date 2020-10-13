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
                    num += net.powerComps[i].EnergyOutputPerTick;
                }
            }
            return num;
        }
        public override void Tick()
        {
            base.Tick();
            var baseComp = this.GetComp<CompPowerZTransmitter>();
            if (baseComp.PowerNet != null)
            {
                if (baseComp.PowerNet.powerComps.Where(x => x is CompPowerZTransmitter).Count() > 1
                    && this.lowerPowerComp == null && this.upperPowerComp == null)
                {
                    //ZLogger.Message("1 Removing " + baseComp + " - " + baseComp.GetHashCode(), true);
                    baseComp.PowerNet.powerComps.Remove(baseComp);
                }
                else if (baseComp != null && baseComp.PowerNet.powerComps.Contains(baseComp))
                {
                    if (!baseComp.PowerNet.powerComps.Contains(baseComp))
                    {
                        //ZLogger.Message("1 Adding " + baseComp + " - " + baseComp.GetHashCode(), true);
                        baseComp.PowerNet.powerComps.Add(baseComp);
                    }
                    var powerComps = new List<CompPowerZTransmitter>();
                    if (connectedPowerNets.powerNets == null)
                    {
                        //ZLogger.Message("2 Adding " + baseComp + " - " + baseComp.GetHashCode(), true);
                        powerComps.Add(baseComp);
                        connectedPowerNets.powerNets = new Dictionary<int, List<CompPowerZTransmitter>>
                        {
                            { 0, powerComps }
                        };
                    }
                    else if (connectedPowerNets.powerNets.Count == 0)
                    {
                        //ZLogger.Message("3 Adding " + baseComp + " - " + baseComp.GetHashCode(), true);
                        powerComps.Add(baseComp);
                        connectedPowerNets.powerNets.Add(0, powerComps);
                    }
                    else if (connectedPowerNets.powerNets.Values.Where(x => x.Where(y => y.PowerNet == baseComp.PowerNet
                        && y.PowerNet.powerComps.Exists(c => c is CompPowerZTransmitter)).Any()).Count() == 0)
                    {
                        //ZLogger.Message("4 Adding " + baseComp + " - " + baseComp.GetHashCode(), true);
                        powerComps.Add(baseComp);
                        int maxKey = connectedPowerNets.powerNets.Max(x => x.Key);
                        connectedPowerNets.powerNets.Add(maxKey + 1, powerComps);
                    }
                    else
                    {
                        powerComps = connectedPowerNets.powerNets.Values.Where(x => x
                            .Where(y => y.PowerNet == baseComp.PowerNet).Count() == 1).ToList().First();
                    }

                    //ZLogger.Message(this + ZTracker.GetMapInfo(this.Map) + " works", true);
                    var upperMap = ZTracker.GetUpperLevel(this.Map.Tile, this.Map);
                    var lowerMap = ZTracker.GetLowerLevel(this.Map.Tile, this.Map);

                    //if (Find.TickManager.TicksGame % 60 == 0)
                    //{
                    //    ZLogger.Message(this + " in " + ZTracker.GetMapInfo(this.Map) + " - this.upperTransmitter: " + this.upperTransmitter);
                    //    ZLogger.Message(this + " in " + ZTracker.GetMapInfo(this.Map) + " - this.upperTransmitter.Spawned: " + this.upperTransmitter?.Spawned);
                    //    ZLogger.Message(this + " in " + ZTracker.GetMapInfo(this.Map) + " - this.upperPowerComp: " + this.upperPowerComp);
                    //    ZLogger.Message(this + " in " + ZTracker.GetMapInfo(this.Map) + " - this.lowerTransmitter: " + this.lowerTransmitter);
                    //    ZLogger.Message(this + " in " + ZTracker.GetMapInfo(this.Map) + " - this.lowerTransmitter.Spawned: " + this.lowerTransmitter?.Spawned);
                    //    ZLogger.Message(this + " in " + ZTracker.GetMapInfo(this.Map) + " - this.lowerPowerComp: " + this.lowerPowerComp);
                    //}

                    if (this.upperPowerComp != null && (upperTransmitter == null || !upperTransmitter.Spawned))
                    {
                        this.upperPowerComp = null;
                        upperTransmitter = null;
                    }
                    if (this.lowerPowerComp != null && (lowerTransmitter == null || !lowerTransmitter.Spawned))
                    {
                        this.lowerPowerComp = null;
                        lowerTransmitter = null;
                    }
                    if (upperMap != null && this.upperPowerComp == null && Find.TickManager.TicksGame % 200 == 0)
                    {
                        foreach (var pos in GenRadial.RadialCellsAround(this.Position, 3f, true))
                        {
                            foreach (var t in pos.GetThingList(upperMap))
                            {
                                //ZLogger.Message("Power candidate: " + t);
                                if (t.TryGetComp<CompPowerTransmitter>() != null)
                                {
                                    upperTransmitter = t;
                                    var upperComp = upperTransmitter.TryGetComp<CompPowerTransmitter>();
                                    upperPowerComp = (CompPowerZTransmitter)upperComp.PowerNet.powerComps
                                        .Where(x => x is CompPowerZTransmitter).FirstOrDefault();
                                    if (upperPowerComp == null)
                                    {
                                        upperPowerComp = new CompPowerZTransmitter
                                        {
                                            parent = this,
                                            powerOutputInt = 0,
                                            //PowerOn = true,
                                            transNet = upperComp.transNet
                                        };
                                        upperPowerComp.Initialize(new CompProperties_PowerZTransmitter());
                                        //ZLogger.Message("5 Adding " + upperPowerComp + " - " + upperPowerComp.GetHashCode(), true);
                                        upperComp.PowerNet.powerComps.Add(upperPowerComp);
                                    }
                                    if (upperPowerComp != null && !powerComps.Contains(upperPowerComp))
                                    {
                                        //ZLogger.Message("6 Adding " + upperPowerComp + " - " + upperPowerComp.GetHashCode(), true);
                                        powerComps.Add(upperPowerComp);
                                    }
                                    break;
                                }
                            }
                        }
                    }
                    if (lowerMap != null && this.lowerPowerComp == null && Find.TickManager.TicksGame % 200 == 0)
                    {
                        foreach (var pos in GenRadial.RadialCellsAround(this.Position, 3f, true))
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
                                        lowerPowerComp = new CompPowerZTransmitter
                                        {
                                            parent = this,
                                            powerOutputInt = 0,
                                            transNet = lowerComp.transNet,
                                            //PowerOn = true
                                        };
                                        lowerPowerComp.Initialize(new CompProperties_PowerZTransmitter());
                                        //ZLogger.Message("7 Adding " + lowerPowerComp + " - " + lowerPowerComp.GetHashCode(), true);
                                        lowerComp.PowerNet.powerComps.Add(lowerPowerComp);
                                    }
                                    if (lowerPowerComp != null && !powerComps.Contains(lowerPowerComp))
                                    {
                                        //ZLogger.Message(this + " Lower add " + lowerPowerComp);
                                        //ZLogger.Message("8 Adding " + lowerPowerComp + " - " + lowerPowerComp.GetHashCode(), true);
                                        powerComps.Add(lowerPowerComp);
                                    }
                                    break;
                                }
                            }
                        }
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
            //ZLogger.Message(pawn + " - -----------------", true);
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
