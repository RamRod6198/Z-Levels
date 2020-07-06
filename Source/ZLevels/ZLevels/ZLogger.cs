using System;
using Verse;

namespace ZLevels
{
	public static class ZLogger
	{
		public static void Message(string message, bool temp = true)
		{
			if (ZLogger.DebugEnabled)

				Log.Message(ZLogger.Prefix + message, true);
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

		public static void Pause(string reason)
		{
			if (ZLogger.DebugEnabled)
			{
				Log.Error("Pausing, reason: " + reason, true);
				Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
			}
		}

		public static readonly bool DebugEnabled = true;

		private static readonly string Prefix = "[Z-Levels] ";
	}
}

