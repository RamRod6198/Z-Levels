using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ZLevels
{
    public class CompPowerTransmitter : CompPower
    {
        public float AmountCanAccept
        {
            get
            {
                if (this.parent.IsBrokenDown())
                {
                    return 0f;
                }
                CompProperties_PowerTransmitter props = this.Props;
                return (props.storedEnergyMax - this.storedEnergy) / props.efficiency;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
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

        public new CompProperties_PowerTransmitter Props
        {
            get
            {
                return (CompProperties_PowerTransmitter)this.props;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<float>(ref this.storedEnergy, "storedPower", 0f, false);
            CompProperties_PowerTransmitter props = this.Props;
            if (this.storedEnergy > props.storedEnergyMax)
            {
                this.storedEnergy = props.storedEnergyMax;
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            this.DrawPower(Mathf.Min(5f * CompPower.WattsToWattDaysPerTick, this.storedEnergy));
        }

        public void AddEnergy(float amount)
        {
            Log.Message("AddPower: " + amount);

            if (amount < 0f)
            {
                Log.Error("Cannot add negative energy " + amount, false);
                return;
            }
            if (amount > this.AmountCanAccept)
            {
                amount = this.AmountCanAccept;
            }
            amount *= this.Props.efficiency;
            this.storedEnergy += amount;
        }

        public void DrawPower(float amount)
        {
            Log.Message("DrawPower: " + amount);
            this.storedEnergy -= amount;
            if (this.storedEnergy < 0f)
            {
                Log.Error("Drawing power we don't have from " + this.parent, false);
                this.storedEnergy = 0f;
            }
        }

        public void SetStoredEnergyPct(float pct)
        {
            pct = Mathf.Clamp01(pct);
            this.storedEnergy = this.Props.storedEnergyMax * pct;
        }

        public override void ReceiveCompSignal(string signal)
        {
            if (signal == "Breakdown")
            {
                this.DrawPower(this.StoredEnergy);
            }
        }

        public override string CompInspectStringExtra()
        {
            CompProperties_PowerTransmitter props = this.Props;
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
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            IEnumerator<Gizmo> enumerator = null;
            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEBUG: Fill",
                    action = delegate ()
                    {
                        this.SetStoredEnergyPct(1f);
                    }
                };
                yield return new Command_Action
                {
                    defaultLabel = "DEBUG: Empty",
                    action = delegate ()
                    {
                        this.SetStoredEnergyPct(0f);
                    }
                };
            }
            yield break;
            yield break;
        }

        private float storedEnergy;

        private const float SelfDischargingWatts = 5f;
    }
}
