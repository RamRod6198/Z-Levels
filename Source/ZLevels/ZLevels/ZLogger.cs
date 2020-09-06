using System;
using Verse;

namespace ZLevels
{
	public static class ZLogger
	{
		//[Conditional("Debug")]
		public static void Message(string message, bool temp = true)
		{
			if (DebugEnabled)

				Log.Message(Prefix + message, true);
		}

		public static void Warning(string message)
		{
//			if (DebugEnabled)

				Log.Warning(Prefix + message, true);
		}

		public static void Warning(string message, Exception e)
		{
			if (DebugEnabled)

				Log.Warning(Prefix + message + "\n" + (e), true);
		}

		public static void Error(string message)
		{
			if (DebugEnabled)

				Log.Error(Prefix + message, true);
		}

		public static void Error(string message, Exception e)
		{
			if (DebugEnabled)

				Log.Error(Prefix + message + "\n" + (e), true);
		}

		public static void ErrorOnce(string message, bool test)
		{
			if (DebugEnabled)
			Log.Error(Prefix + message, true);
		}

		public static void Pause(string reason)
		{
			if (DebugEnabled)
			{
				Log.Error("Pausing, reason: " + reason, true);
				//Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
			}
		}

		public static bool DebugEnabled => ZLevels.settings.DebugEnabled;

		private static readonly string Prefix = "[Z-Levels] ";
	}
}

