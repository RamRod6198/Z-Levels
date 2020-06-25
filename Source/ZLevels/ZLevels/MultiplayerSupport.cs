using System;
using System.Reflection;
using HarmonyLib;
using Multiplayer.API;
using Verse;

namespace ZLevels
{
	[StaticConstructorOnStartup]
	internal static class MultiplayerSupport
	{
		static MultiplayerSupport()
		{
			if (!MP.enabled)
			{
				return;
			}
			MP.RegisterSyncMethod(typeof(Building_StairsDown), "GiveJob", null);
			MP.RegisterSyncMethod(typeof(Building_StairsUp), "GiveJob", null);
			var method = AccessTools.Method(typeof(MapComponentZLevel), "MapComponentTick", null, null);
			MultiplayerSupport.harmony.Patch(method, new HarmonyMethod(typeof(MultiplayerSupport), 
				"FixRNGPre", null), new HarmonyMethod(typeof(MultiplayerSupport), "FixRNGPos", null), null, null);

		}

		private static void FixRNGPre()
		{
			Rand.PushState(Find.TickManager.TicksAbs);
		}

		private static void FixRNGPos()
		{
			Rand.PopState();
		}

		private static Harmony harmony = new Harmony("rimworld.zlevels.multiplayersupport");
	}
}

