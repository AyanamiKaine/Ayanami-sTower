using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LoadFilesToLearn
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Command
    {
        GET
    }

    internal class Request
    {
        public Command Command { get; set; } = Command.GET; 
    }
}
