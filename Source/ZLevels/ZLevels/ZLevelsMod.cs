using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ZLevels
{
    class ZLevelsMod : Mod
    {
        public static ZLevelsSettings settings;
        public ZLevelsMod(ModContentPack pack) : base(pack)
        {
            settings = GetSettings<ZLevelsSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            settings.DoSettingsWindowContents(inRect);
        }

        // Return the name of the mod in the settings tab in game
        public override string SettingsCategory()
        {
            return "Z-Levels";
        }
    }
}
