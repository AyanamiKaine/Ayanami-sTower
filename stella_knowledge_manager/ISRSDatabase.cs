using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stella_knowledge_manager
{
    public interface ISRSDatabase
    {
        public void AddItem(ISRSItem item);
        public ISRSItem GetItem(Guid id);
        public void UpdateItem(Guid id, ISRSItem updatedItem);
        public void DeleteItem(Guid id);
    }
}
