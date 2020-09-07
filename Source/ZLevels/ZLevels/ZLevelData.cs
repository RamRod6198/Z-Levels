using System.Collections.Generic;
using Verse;

namespace ZLevels
{
	public class ZLevelData : IExposable
	{
        public void ExposeData()
		{
			Scribe_Collections.Look(ref ZLevels, "ZLevels", LookMode.Value, LookMode.Reference,
				ref ZLevelsKeys, ref ZLevelsValues);
		}

		public Dictionary<int, Map> ZLevels;
		public List<int> ZLevelsKeys = new List<int>();
		public List<Map> ZLevelsValues = new List<Map>(); 
	}
}

