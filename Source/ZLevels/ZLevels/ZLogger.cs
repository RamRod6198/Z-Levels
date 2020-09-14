using System;
using System.Diagnostics;
using Verse;

namespace ZLevels
{
	public static class ZLogger
	{
		public static DebugLevel curDebugLevel = DebugLevel.All;

		//[Conditional("Debug")]
		public static void Message(string message, bool temp = true, DebugLevel debugLevel = DebugLevel.All)
		{
			if (ZLogger.DebugEnabled && curDebugLevel != DebugLevel.None)
            {
				if (debugLevel == curDebugLevel || curDebugLevel == DebugLevel.All)
                {
					Log.Message(ZLogger.Prefix + message, true);
                }
            }
		}

		public static void Warning(string message)
		{
			if (ZLogger.DebugEnabled)

				Log.Warning(ZLogger.Prefix + message, true);
		}

		public static void Warning(string message, Exception e)
		{
			if (ZLogger.DebugEnabled)

				Log.Warning(ZLogger.Prefix + message + "\n" + (e?.ToString()), true);
		}

		public static void Error(string message)
		{
			if (ZLogger.DebugEnabled)

				Log.Error(ZLogger.Prefix + message, true);
		}

		public static void Error(string message, Exception e)
		{
			if (ZLogger.DebugEnabled)

				Log.Error(ZLogger.Prefix + message + "\n" + (e?.ToString()), true);
		}

		public static void ErrorOnce(string message, bool test)
		{
			if (ZLogger.DebugEnabled)
			Log.Error(ZLogger.Prefix + message, true);
		}

		//[Conditional("Debug")]
		public static void Pause(string reason)
		{
			if (ZLogger.DebugEnabled)
			{
				Log.Error("Pausing, reason: " + reason, true);
			}
		}

		public static bool DebugEnabled => ZLevelsMod.settings.DebugEnabled;

		private static readonly string Prefix = "[Z-Levels] ";
	}
}