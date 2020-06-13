using System;
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
                //Log.Message(this + "2 Stored energy: " + this.GetComp<CompPowerZTransmitter>().PowerNet.CurrentEnergyGainRate());
                //Log.Message(this + "2 Stored energy: " + this.GetComp<CompPowerZTransmitter>().PowerNet.CurrentStoredEnergy());
                //Log.Message(upperTransmitter + "2 Stored energy: " + upperTransmitter.TryGetComp<CompPowerTransmitter>().PowerNet.CurrentEnergyGainRate());
                //Log.Message(upperTransmitter + "2 Stored energy: " + upperTransmitter.TryGetComp<CompPowerTransmitter>().PowerNet.CurrentStoredEnergy());
                var comp = upperTransmitter.TryGetComp<CompPowerTransmitter>();
                var powerComp = (CompPowerZTransmitter)comp.PowerNet.powerComps.Where(x => x is CompPowerZTransmitter test && test.test == "VovaLoh").FirstOrDefault();
                if (powerComp == null)
                {
                    powerComp = new CompPowerZTransmitter();
                    powerComp.parent = this;
                    powerComp.Initialize(new CompProperties_PowerZTransmitter
                    {
                        storedEnergyMax = 1000f,
                        efficiency = 0.7f
                    });
                    powerComp.powerOutputInt = 99800;
                    powerComp.PowerOn = true;
                    powerComp.test = "VovaLoh";
                    comp.PowerNet.powerComps.Add(powerComp);
                }
                else
                {
                    Log.Message("TEst;;" + powerComp);
                    powerComp.powerOutputInt = 1234;
                    powerComp.PowerOn = true;
                }
            }
            else if ((upperTransmitter == null || !upperTransmitter.Spawned)
                && lowerTransmitter != null && lowerTransmitter.Spawned)
            {
                Log.Message("3 Stored energy: " + this.GetComp<CompPowerZTransmitter>().PowerNet.CurrentEnergyGainRate());
            }

            if (this.ticksToExplode > 0)
            {
                Log.Message(" - Test - if (this.wickSustainer == null) - 33", true);
                if (this.wickSustainer == null)
                {
                    Log.Message(" - Test - this.StartWickSustainer(); - 34", true);
                    this.StartWickSustainer();
                }
                else
                {
                    Log.Message(" - Test - this.wickSustainer.Maintain(); - 35", true);
                    this.wickSustainer.Maintain();
                }
                this.ticksToExplode--;
                Log.Message(" - Test - if (this.ticksToExplode == 0) - 37", true);
                if (this.ticksToExplode == 0)
                {
                    Log.Message(" - Test - IntVec3 randomCell = this.OccupiedRect().RandomCell; - 38", true);
                    IntVec3 randomCell = this.OccupiedRect().RandomCell;
                    Log.Message(" - Test - float radius = Rand.Range(0.5f, 1f) * 3f; - 39", true);
                    float radius = Rand.Range(0.5f, 1f) * 3f;
                    Log.Message(" - Test - GenExplosion.DoExplosion(randomCell, base.Map, radius, DamageDefOf.Flame, null, -1, -1f, null, null, null, null, null, 0f, 1, false, null, 0f, 1, 0f, false, null, null); - 40", true);
                    GenExplosion.DoExplosion(randomCell, base.Map, radius, DamageDefOf.Flame, null, -1, -1f, null, null, null, null, null, 0f, 1, false, null, 0f, 1, 0f, false, null, null);
                    Log.Message(" - Test - base.GetComp<CompPowerBattery>().DrawPower(400f); - 41", true);
                    base.GetComp<CompPowerBattery>().DrawPower(400f);
                }
            }
        }

        public override void PostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            Log.Message("Building_PowerTransmitter : Building - PostApplyDamage - base.PostApplyDamage(dinfo, totalDamageDealt); - 46", true);
            base.PostApplyDamage(dinfo, totalDamageDealt);
            Log.Message("Building_PowerTransmitter : Building - PostApplyDamage - if (!base.Destroyed && this.ticksToExplode == 0 && dinfo.Def == DamageDefOf.Flame && Rand.Value < 0.05f && base.GetComp<CompPowerBattery>().StoredEnergy > 500f) - 47", true);
            if (!base.Destroyed && this.ticksToExplode == 0 && dinfo.Def == DamageDefOf.Flame && Rand.Value < 0.05f && base.GetComp<CompPowerBattery>().StoredEnergy > 500f)
            {
                Log.Message("Building_PowerTransmitter : Building - PostApplyDamage - this.ticksToExplode = Rand.Range(70, 150); - 48", true);
                this.ticksToExplode = Rand.Range(70, 150);
                Log.Message("Building_PowerTransmitter : Building - PostApplyDamage - this.StartWickSustainer(); - 49", true);
                this.StartWickSustainer();
            }
        }

        private void StartWickSustainer()
        {
            SoundInfo info = SoundInfo.InMap(this, MaintenanceType.PerTick);
            Log.Message("Building_PowerTransmitter : Building - StartWickSustainer - this.wickSustainer = SoundDefOf.HissSmall.TrySpawnSustainer(info); - 51", true);
            this.wickSustainer = SoundDefOf.HissSmall.TrySpawnSustainer(info);
        }

        private int ticksToExplode;

        public ZLevelsManager ZTracker = Current.Game.GetComponent<ZLevelsManager>();

        private Sustainer wickSustainer;

        Thing upperTransmitter;

        Thing lowerTransmitter;

        private static readonly Vector2 BarSize = new Vector2(1.3f, 0.4f);

        private const float MinEnergyToExplode = 500f;

        private const float EnergyToLoseWhenExplode = 400f;

        private const float ExplodeChancePerDamage = 0.05f;

        private static readonly Material BatteryBarFilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.9f, 0.85f, 0.2f), false);

        private static readonly Material BatteryBarUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f), false);
    }
}