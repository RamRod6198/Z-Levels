using HarmonyLib;
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
        public static Harmony harmony;
        public static ZLevelsSettings settings;
        public ZLevelsMod(ModContentPack pack) : base(pack)
        {
            settings = GetSettings<ZLevelsSettings>();
            harmony = new Harmony("ZLevels.Mod");
            harmony.PatchAll();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            settings.DoSettingsWindowContents(inRect);
        }
        public override string SettingsCategory()
        {
            return this.Content.Name;
        }
    }
}
