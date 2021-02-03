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
        PowerNet curPowerNet; 
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
            if (this?.PowerNet != null)
            {
                if (this.PowerOn)
                {
                    if (curPowerNet != this.PowerNet)
                    {
                        curPowerNet = this.PowerNet;
                        ZUtils.ZTracker.connectedPowerNets.RegisterTransmitter(this);
                    }
                }
                else
                {
                    ZUtils.ZTracker.connectedPowerNets.DeregisterTransmitter(this);
                }
            }
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
        }
    }
}

