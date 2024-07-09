using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spaced_Repetition_Database
{
    internal class Response
    {
        public string               Status { get; set; }
        public List<FileToLearn>    Data   { get; set; }
    }
}
