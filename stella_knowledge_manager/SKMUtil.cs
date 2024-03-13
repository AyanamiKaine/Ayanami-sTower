using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stella_knowledge_manager
{
    public static class SKMUtil
    {
        public static ISRSItem GetItemById(SKM skm, Guid id)
        {
            return skm.PriorityList.SingleOrDefault((item) => item.Id == id);
        }

        public static ISRSItem GetItemByName(SKM skm, string name)
        {
            return skm.PriorityList.SingleOrDefault((item) => item.Name == name);
        }
    }
}
