using System;
using System.Diagnostics;
using Verse;

namespace ZLevels
{
	public static class ZLogger
	{
		public static DebugLevel curDebugLevel = DebugLevel.All;

		//[Conditional("Debug")]
		public static void Pause(string reason)
		{
			Log.Error("Pausing, reason: " + reason);
			Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
		}
		//[Conditional("Debug")]
		public static void Message(string message, DebugLevel debugLevel = DebugLevel.All)
		{
			if (DebugEnabled && curDebugLevel != DebugLevel.None)
            {
				if (debugLevel == curDebugLevel || curDebugLevel == DebugLevel.All)
                {
					Log.Message(ZLogger.Prefix + message);
                }
            }
		}

		public static void Warning(string message)
		{
			if (DebugEnabled)
				Log.Warning(ZLogger.Prefix + message);
		}

		public static void Warning(string message, Exception e)
		{
			if (DebugEnabled)
				Log.Warning(ZLogger.Prefix + message + "\n" + (e?.ToString()));
		}

		public static void Error(string message)
		{
			Log.Error(ZLogger.Prefix + message);
		}

		public static void Error(string message, Exception e)
		{
			Log.Error(ZLogger.Prefix + message + "\n" + (e?.ToString()));
		}

		public static void ErrorOnce(string message)
		{
			Log.Error(ZLogger.Prefix + message);
		}

		public static bool DebugEnabled => ZLevelsMod.settings.DebugEnabled;

		private static readonly string Prefix = "[Z-Levels] ";
	}
}