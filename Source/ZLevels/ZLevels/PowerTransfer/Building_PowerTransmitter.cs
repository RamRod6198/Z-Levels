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
            Log.Message("Building_PowerTransmitter : Building - ExposeData - base.ExposeData(); - 1", true);
            base.ExposeData();
            Log.Message("Building_PowerTransmitter : Building - ExposeData - Scribe_Values.Look<int>(ref this.ticksToExplode, \"ticksToExplode\", 0, false); - 2", true);
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

        public override void Tick()
        {
            base.Tick();
            var upperMap = ZTracker.GetUpperLevel(this.Map.Tile, this.Map);
            var lowerMap = ZTracker.GetLowerLevel(this.Map.Tile, this.Map);
            if (upperMap != null && (this.upperTransmitter == null || !this.upperTransmitter.Spawned))
            {
                foreach (var pos in GenRadial.RadialCellsAround(this.Position, 5f, true))
                {
                    foreach (var t in pos.GetThingList(upperMap))
                    {
                        if (t.TryGetComp<CompPowerTransmitter>() != null)
                        {
                            upperTransmitter = t;
                            break;
                        }
                    }
                }
            }
            if (lowerMap != null && (this.lowerTransmitter == null || !this.lowerTransmitter.Spawned))
            {
                foreach (var pos in GenRadial.RadialCellsAround(this.Position, 5f, true))
                {
                    foreach (var t in pos.GetThingList(lowerMap))
                    {
                        if (t.TryGetComp<CompPowerTransmitter>() != null)
                        {
                            lowerTransmitter = t;
                            break;
                        }
                    }
                }
            }

            if (upperTransmitter != null && upperTransmitter.Spawned
                && lowerTransmitter != null && lowerTransmitter.Spawned)
            {
                Log.Message("1 Stored energy: " + this.GetComp<CompPowerZTransmitter>().PowerNet.CurrentEnergyGainRate());
            }
            else if (upperTransmitter != null && upperTransmitter.Spawned
                && (lowerTransmitter == null || !lowerTransmitter.Spawned))
            {
                var comp = upperTransmitter.TryGetComp<CompPowerTransmitter>();
                var baseComp = this.GetComp<CompPowerZTransmitter>();

                var powerComp = (CompPowerZTransmitter)comp.PowerNet.powerComps.Where(x => x is CompPowerZTransmitter).FirstOrDefault();
                if (powerComp == null)
                {
                    powerComp = new CompPowerZTransmitter();
                    powerComp.parent = this;
                    powerComp.Initialize(new CompProperties_PowerZTransmitter
                    {
                        storedEnergyMax = 1000f,
                        efficiency = 0.7f
                    });
                    powerComp.powerOutputInt = 0;
                    powerComp.PowerOn = true;
                    comp.PowerNet.powerComps.Add(powerComp);
                }

                var orig1 = (baseComp.PowerNet.CurrentEnergyGainRate() / CompPower.WattsToWattDaysPerTick)
                    - baseComp.powerOutputInt;
                var orig2 = (comp.PowerNet.CurrentEnergyGainRate() / CompPower.WattsToWattDaysPerTick)
                    - powerComp.powerOutputInt;

                var newValue = (orig1 + orig2) / 2;
                baseComp.powerOutputInt = newValue - orig1;
                powerComp.powerOutputInt = newValue - orig2;
            }
            else if ((upperTransmitter == null || !upperTransmitter.Spawned)
                && lowerTransmitter != null && lowerTransmitter.Spawned)
            {
                var comp = lowerTransmitter.TryGetComp<CompPowerTransmitter>();
                var baseComp = this.GetComp<CompPowerZTransmitter>();

                var powerComp = (CompPowerZTransmitter)comp.PowerNet.powerComps.Where(x => x is CompPowerZTransmitter).FirstOrDefault();
                if (powerComp == null)
                {
                    powerComp = new CompPowerZTransmitter();
                    powerComp.parent = this;
                    powerComp.Initialize(new CompProperties_PowerZTransmitter
                    {
                        storedEnergyMax = 1000f,
                        efficiency = 0.7f
                    });
                    powerComp.powerOutputInt = 0;
                    powerComp.PowerOn = true;
                    comp.PowerNet.powerComps.Add(powerComp);
                }

                var orig1 = (baseComp.PowerNet.CurrentEnergyGainRate() / CompPower.WattsToWattDaysPerTick)
                    - baseComp.powerOutputInt;
                var orig2 = (comp.PowerNet.CurrentEnergyGainRate() / CompPower.WattsToWattDaysPerTick)
                    - powerComp.powerOutputInt;

                var newValue = (orig1 + orig2) / 2;
                baseComp.powerOutputInt = newValue - orig1;
                powerComp.powerOutputInt = newValue - orig2;
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
                    base.GetComp<CompPowerBattery>().DrawPower(400f);
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

        private int ticksToExplode;

        public ZLevelsManager ZTracker = Current.Game.GetComponent<ZLevelsManager>();

        private Sustainer wickSustainer;

        Thing upperTransmitter;

        Thing lowerTransmitter;

	}
}