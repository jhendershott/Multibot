using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace multicorp_bot.Controllers
{
    public class WorkOrderController
    {
        MultiBotDb MultiBotDb;
        public WorkOrderController()
        {
            MultiBotDb = new MultiBotDb();
        }

        public double GetExpModifier(string modName)
        {
            return MultiBotDb.WorkOrderTypes.Where(x => x.Name == modName).FirstOrDefault().XpModifier;
        }
    }
}
