using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ZLevels
{
    class ZLevelsSettings : ModSettings
    {
        public bool DebugEnabled = true;

        public override void ExposeData()
        {
            Scribe_Values.Look<bool>(ref DebugEnabled, "DebugEnabled", false);
            base.ExposeData();
        }

        // Draw the actual settings window that shows up after selecting Z-Levels in the list
        public void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.CheckboxLabeled("debugEnabledExplanation".Translate(), ref DebugEnabled);
            listingStandard.End();
        }
    }
}
