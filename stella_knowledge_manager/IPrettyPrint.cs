using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stella_knowledge_manager
{
    /// <summary>
    /// A simple pretty print interface for an object to implement, so it looks nicely displayed in the terminal.
    /// </summary>
    public interface IPrettyPrint
    {
        public void PrettyPrint();
    }
}
