using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logger
{
    public class Request
    {
        public string   FilePath    { get; set; } = "./logs/log.txt";
        public string   Message     { get; set; } = "";
        public DateTime Time        { get; set; } = DateTime.Now;
    }
}
