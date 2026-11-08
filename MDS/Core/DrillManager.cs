using System.Collections.Generic;
using System.Linq;

//Currently unfinished. The DrillManager is intended to handle the processing and execution of commands that are persistent over time.

namespace MDS.Core
{
    public class DrillManager
    {
        private static List<Drill> activeDrills = new List<Drill>();

        public static void RegisterDrill(Drill drill)
        {
            activeDrills.Add(drill);
        }

        public static void Update()
        {
            foreach (Drill drill in activeDrills.ToList())
            {
                if (drill.CheckCondition())
                {
                    drill.ExecuteNextStep();
                }
            }
        }
    }

    public abstract class Drill
    {
        public abstract bool CheckCondition();
        public abstract void ExecuteNextStep();
    }
}
