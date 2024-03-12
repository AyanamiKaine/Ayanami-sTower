using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace stella_knowledge_manager
{
    /// <summary>
    /// An Spaced Repetition Item is an item that defined various fields related to SRS and other needed 
    /// functionality like priority, Id, Name, NumberOfTimeSeen, Description, PathToFile.
    /// </summary>
    public interface ISRSItem : ISRS , IPrettyPrint
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string PathToFile { get; set; }
        public string Description { get; set; }
        public double Priority { get; set; }
    }
}
