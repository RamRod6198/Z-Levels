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
            if (this.ticksToExplode > 0 && base.Spawned)
            {
                base.Map.overlayDrawer.DrawOverlay(this, OverlayTypes.BurningWick);
            }
        }

        private CompPowerZTransmitter baseComp;
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            baseComp = this.GetComp<CompPowerZTransmitter>();
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

        public ConnectedPowerNets connectedPowerNets => ZUtils.ZTracker.connectedPowerNets;

        private Sustainer wickSustainer;

        CompPowerZTransmitter upperPowerComp = null;

        CompPowerZTransmitter lowerPowerComp = null;
    }
}
