using System;
using RimWorld;
using UnityEngine;
using Verse;
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

            //if (upperTransmitter != null && upperTransmitter.Spawned 
            //    && lowerTransmitter != null && lowerTransmitter.Spawned)
            //{
            //    Log.Message("Stored energy: " + this.GetComp<CompPowerTransmitterTest>().StoredEnergy);
            //}
            //else if (upperTransmitter != null && upperTransmitter.Spawned
            //    && (lowerTransmitter == null || !lowerTransmitter.Spawned))
            //{
            //    Log.Message("Stored energy: " + this.GetComp<CompPowerTransmitterTest>().StoredEnergy);
            //}
            //else if ((upperTransmitter == null || !upperTransmitter.Spawned)
            //    && lowerTransmitter != null && lowerTransmitter.Spawned)
            //{
            //    Log.Message("Stored energy: " + this.GetComp<CompPowerTransmitterTest>().StoredEnergy);
            //}

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

        private static readonly Vector2 BarSize = new Vector2(1.3f, 0.4f);

        private const float MinEnergyToExplode = 500f;

        private const float EnergyToLoseWhenExplode = 400f;

        private const float ExplodeChancePerDamage = 0.05f;

        private static readonly Material BatteryBarFilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.9f, 0.85f, 0.2f), false);

        private static readonly Material BatteryBarUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f), false);
    }
}
