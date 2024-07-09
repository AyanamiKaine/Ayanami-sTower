using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Spaced_Repetition_Database
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    internal enum Command
    {
        CREATE,
        RETRIVE_ALL_ITEMS,
        RETRIVE_ITEM_WITH_HIGHEST_PRIORITY,
        RETRIEVE_ALL_DUE_ITEMS,
        UPDATE,
        DELETE
    }
    internal class Request
    {
        public Command  Command { get; set; }           = Command.RETRIVE_ITEM_WITH_HIGHEST_PRIORITY;
        public FileToLearn FileToLearn { get; set; }

        public Guid Id { get; set; }
    }
}
