using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stella_knowledge_manager
{
    /// <summary>
    /// A simple pretty print interface for an object to implement, so an object returns a string that nicely represents it self.
    /// </summary>
    public interface IPrettyPrint
    {
        public string PrettyPrint();
    }
}
