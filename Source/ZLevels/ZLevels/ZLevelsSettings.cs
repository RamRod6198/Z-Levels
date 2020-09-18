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
        public bool DebugEnabled = false;
        public bool allowZLevelsInfestation = true;
        public override void ExposeData()
        {
            Scribe_Values.Look<bool>(ref DebugEnabled, "DebugEnabled", false);
            Scribe_Values.Look<bool>(ref allowZLevelsInfestation, "allowZLevelsInfestation", true);
            base.ExposeData();
        }

        // Draw the actual settings window that shows up after selecting Z-Levels in the list
        public void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.CheckboxLabeled("debugEnabledExplanation".Translate(), ref DebugEnabled);
            listingStandard.CheckboxLabeled("allowZLevelsInfestation".Translate(), ref allowZLevelsInfestation);
            listingStandard.End();
        }
    }
}
