using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ZLevels
{
    public class CompPowerTransmitterTest : CompPower
    {
        public float AmountCanAccept
        {
            get
            {
                if (this.parent.IsBrokenDown())
                {
                    return 0f;
                }
                CompProperties_PowerTransmitterTest props = this.Props;
                return (props.storedEnergyMax - this.storedEnergy) / props.efficiency;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            Log.Message("TEST");
        }
        public float StoredEnergy
        {
            get
            {
                return this.storedEnergy;
            }
        }

        public float StoredEnergyPct
        {
            get
            {
                return this.storedEnergy / this.Props.storedEnergyMax;
            }
        }

        public new CompProperties_PowerTransmitterTest Props
        {
            get
            {
                return (CompProperties_PowerTransmitterTest)this.props;
            }
        }

        public override void PostExposeData()
        {
            Log.Message("CompPowerTransmitter : CompPower - PostExposeData - base.PostExposeData(); - 1", true);
            base.PostExposeData();
            Log.Message("CompPowerTransmitter : CompPower - PostExposeData - Scribe_Values.Look<float>(ref this.storedEnergy, \"storedPower\", 0f, false); - 2", true);
            Scribe_Values.Look<float>(ref this.storedEnergy, "storedPower", 0f, false);
            Log.Message("CompPowerTransmitter : CompPower - PostExposeData - CompProperties_PowerTransmitter props = this.Props; - 3", true);
            CompProperties_PowerTransmitterTest props = this.Props;
            Log.Message("CompPowerTransmitter : CompPower - PostExposeData - if (this.storedEnergy > props.storedEnergyMax) - 4", true);
            if (this.storedEnergy > props.storedEnergyMax)
            {
                Log.Message("CompPowerTransmitter : CompPower - PostExposeData - this.storedEnergy = props.storedEnergyMax; - 5", true);
                this.storedEnergy = props.storedEnergyMax;
            }
        }

        public override void CompTick()
        {
            Log.Message("Test");
            base.CompTick();
            this.DrawPower(Mathf.Min(5f * CompPower.WattsToWattDaysPerTick, this.storedEnergy));
        }

        public void AddEnergy(float amount)
        {
            Log.Message("CompPowerTransmitter : CompPower - AddEnergy - if (amount < 0f) - 8", true);
            if (amount < 0f)
            {
                Log.Message("CompPowerTransmitter : CompPower - AddEnergy - Log.Error(\"Cannot add negative energy \" + amount, false); - 9", true);
                Log.Error("Cannot add negative energy " + amount, false);
                Log.Message("CompPowerTransmitter : CompPower - AddEnergy - return; - 10", true);
                return;
            }
            Log.Message("CompPowerTransmitter : CompPower - AddEnergy - if (amount > this.AmountCanAccept) - 11", true);
            if (amount > this.AmountCanAccept)
            {
                Log.Message("CompPowerTransmitter : CompPower - AddEnergy - amount = this.AmountCanAccept; - 12", true);
                amount = this.AmountCanAccept;
            }
            amount *= this.Props.efficiency;
            Log.Message("CompPowerTransmitter : CompPower - AddEnergy - this.storedEnergy += amount; - 14", true);
            this.storedEnergy += amount;
        }

        public void DrawPower(float amount)
        {
            Log.Message("CompPowerTransmitter : CompPower - DrawPower - this.storedEnergy -= amount; - 15", true);
            this.storedEnergy -= amount;
            Log.Message("CompPowerTransmitter : CompPower - DrawPower - if (this.storedEnergy < 0f) - 16", true);
            if (this.storedEnergy < 0f)
            {
                Log.Message("CompPowerTransmitter : CompPower - DrawPower - Log.Error(\"Drawing power we don't have from \" + this.parent, false); - 17", true);
                Log.Error("Drawing power we don't have from " + this.parent, false);
                Log.Message("CompPowerTransmitter : CompPower - DrawPower - this.storedEnergy = 0f; - 18", true);
                this.storedEnergy = 0f;
            }
        }

        public void SetStoredEnergyPct(float pct)
        {
            Log.Message("CompPowerTransmitter : CompPower - SetStoredEnergyPct - pct = Mathf.Clamp01(pct); - 19", true);
            pct = Mathf.Clamp01(pct);
            Log.Message("CompPowerTransmitter : CompPower - SetStoredEnergyPct - this.storedEnergy = this.Props.storedEnergyMax * pct; - 20", true);
            this.storedEnergy = this.Props.storedEnergyMax * pct;
        }

        public override void ReceiveCompSignal(string signal)
        {
            Log.Message("CompPowerTransmitter : CompPower - ReceiveCompSignal - if (signal == \"Breakdown\") - 21", true);
            if (signal == "Breakdown")
            {
                Log.Message("CompPowerTransmitter : CompPower - ReceiveCompSignal - this.DrawPower(this.StoredEnergy); - 22", true);
                this.DrawPower(this.StoredEnergy);
            }
        }

        public override string CompInspectStringExtra()
        {
            CompProperties_PowerTransmitterTest props = this.Props;
            string text = "PowerBatteryStored".Translate() + ": " + this.storedEnergy.ToString("F0") + " / " + props.storedEnergyMax.ToString("F0") + " Wd";
            text += "\n" + "PowerBatteryEfficiency".Translate() + ": " + (props.efficiency * 100f).ToString("F0") + "%";
            if (this.storedEnergy > 0f)
            {
                text += "\n" + "SelfDischarging".Translate() + ": " + 5f.ToString("F0") + " W";
            }
            return text + "\n" + base.CompInspectStringExtra();
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            Log.Message("CompPowerTransmitter : CompPower - CompGetGizmosExtra - foreach (Gizmo gizmo in base.CompGetGizmosExtra()) - 26", true);
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            IEnumerator<Gizmo> enumerator = null;
            Log.Message("CompPowerTransmitter : CompPower - CompGetGizmosExtra - if (Prefs.DevMode) - 29", true);
            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEBUG: Fill",
                    action = delegate ()
                    {
                        Log.Message("CompPowerTransmitter : CompPower - CompGetGizmosExtra - this.SetStoredEnergyPct(1f); - 30", true);
                        this.SetStoredEnergyPct(1f);
                    }
                };
                yield return new Command_Action
                {
                    defaultLabel = "DEBUG: Empty",
                    action = delegate ()
                    {
                        Log.Message("CompPowerTransmitter : CompPower - CompGetGizmosExtra - this.SetStoredEnergyPct(0f); - 32", true);
                        this.SetStoredEnergyPct(0f);
                    }
                };
            }
            yield break;
            Log.Message("CompPowerTransmitter : CompPower - CompGetGizmosExtra - yield break; - 35", true);
            yield break;
        }

        private float storedEnergy;

        private const float SelfDischargingWatts = 5f;
    }
}