using System;
using System.Reflection;
using HarmonyLib;
using Multiplayer.API;
using Verse;

namespace ZLevels
{
	[StaticConstructorOnStartup]
	internal static class ZUtils
	{
		public static ZLevelsManager ZTracker
		{
			get
			{
				if (zTracker == null)
				{
					zTracker = Current.Game.GetComponent<ZLevelsManager>();
					return zTracker;
				}
				return zTracker;
			}
		}

		public static void ResetZTracker()
		{
			zTracker = null;
		}

		private static ZLevelsManager zTracker;
	}
}

