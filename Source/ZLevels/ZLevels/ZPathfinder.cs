using System;
using System.Collections.Generic;
using System.ComponentModel;
using Verse;
using Verse.AI;

namespace ZLevels.Properties
{
    /// <summary>
    /// So first we'll go check to see if a path exists from stairs a to b on all stairs within every map.
    /// Along the way, we'll mark all the no path pairs to come back to.
    /// Once we have all the valid connections, we'll use those to patch in valid paths.  Once we have this data structure,
    /// we'll know all the pathfinders needed to go from a to b.
    /// Second, going to go through the pathfinders in order and get the pawn paths for each.
    /// Finally, we'll return a list of pawn paths for the pawn to use, unless I can stitch them together somehow
    /// 
    /// </summary>
    public class ZPathfinder
    {
        private static Lazy<ZPathfinder> _instance = new Lazy<ZPathfinder>();
        public static ZPathfinder Instance => _instance.Value;
        public static readonly TraverseParms StairParms = TraverseParms.For(TraverseMode.ByPawn, Danger.Deadly, false);
        private HashSet<PathFinder> _pathFinders;
        public static void ResetPathfinder()
        {

            _instance = new Lazy<ZPathfinder>();
            
        }

        public bool AddMap(Map map)
        {
            bool ret = _pathFinders.Add(map.pathFinder);



            return ret;
        }

    }

 

}
