using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using stella_knowledge_manager;

namespace LoadFilesToLearn
{
    internal class Response
    {
        public string               Status { get; set; }
        public List<FileToLearn>    Data   { get; set; }
    }
}
