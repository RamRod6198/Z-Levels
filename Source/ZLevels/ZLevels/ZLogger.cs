using System;
using Verse;

namespace ZLevels
{
	public static class ZLogger
	{
		public static void Message(string message)
		{
			Log.Message(ZLogger.Prefix + message, true);
		}

		public static void Warning(string message)
		{
			Log.Warning(ZLogger.Prefix + message, true);
		}

		public static void Warning(string message, Exception e)
		{
			Log.Warning(ZLogger.Prefix + message + "\n" + ((e != null) ? e.ToString() : null), true);
		}

		public static void Error(string message)
		{
			Log.Error(ZLogger.Prefix + message, false);
		}

		public static void Error(string message, Exception e)
		{
			Log.Error(ZLogger.Prefix + message + "\n" + ((e != null) ? e.ToString() : null), true);
		}

		private static readonly bool DebugEnabled = true;

		private static readonly string Prefix = "[Z-Levels] ";
	}
}