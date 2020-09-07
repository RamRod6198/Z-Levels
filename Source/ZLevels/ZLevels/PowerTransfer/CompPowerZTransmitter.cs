using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ZLevels
{
    public class CompPowerZTransmitter : CompPowerPlant
    {
        public override float DesiredPowerOutput
        {
            get
            {
                return 0;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
        }

        public new CompProperties_PowerZTransmitter Props
        {
            get
            {
                return (CompProperties_PowerZTransmitter)this.props;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
        }

        public override void CompTick()
        {
            base.CompTick();
        }

        //public override string CompInspectStringExtra()
        //{
        //    CompProperties_PowerZTransmitter props = this.Props;
        //    string text = "PowerBatteryStored".Translate() + ": " + this.storedEnergy.ToString("F0") + " / " + props.storedEnergyMax.ToString("F0") + " Wd";
        //    text += "\n" + "PowerBatteryEfficiency".Translate() + ": " + 
        //        (props.efficiency * 100f).ToString("F0") + "%";
        //    if (this.storedEnergy > 0f)
        //    {
        //        text += "\n" + "SelfDischarging".Translate() + ": " + 5f.ToString("F0") + " W";
        //    }
        //    return text + "\n" + base.CompInspectStringExtra();
        //}

        public bool mainComp = true;
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            IEnumerator<Gizmo> enumerator = null;
            yield break;
            yield break;
        }

        public Thing transmitter;
    }
}

