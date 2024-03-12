using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stella_knowledge_manager
{
    /// <summary>
    /// This interface defined what an item as to implemnt for fields to be used in Spaced Repetition Algorithms
    /// </summary>
    public interface ISRS
    {
        public abstract double EaseFactor { get; set; }
        public DateTime NextReviewDate { get; set; }
        public int NumberOfTimeSeen { get; set; } 
    }
}
