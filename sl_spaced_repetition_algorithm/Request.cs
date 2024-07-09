using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceRepetitionAlgorithm
{
    public class Request
    {
        public RecallEvaluation RecallEvaluation { get; set; } = RecallEvaluation.AGAIN;
        public FileToLearn FileToLearn { get; set; }
    }
}
