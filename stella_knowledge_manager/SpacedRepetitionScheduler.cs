using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stella_knowledge_manager
{
    public enum RecallEvaluation
    {
        GOOD,
        BAD,
        AGAIN,
    }

    /// <summary>
    /// Should probably use some form of dependency inversion so we can use various spaced repetition algorithms
    /// </summary>
    public class SpacedRepetitionScheduler
    {
        public static DateTime CalculateNextReviewDate(ISRS item, RecallEvaluation recallEvaluation)
        {
            int interval = 1; // Initial interval

            if (recallEvaluation == RecallEvaluation.GOOD)
            {
                if(item.NumberOfTimeSeen < 4)
                {
                    return DateTime.Now.AddMinutes(5);
                }

                // Good recall
                interval = (int)Math.Round(interval * item.EaseFactor, 0);
                item.EaseFactor += 0.1; // Adjust ease
            }
            else if (recallEvaluation == RecallEvaluation.BAD)
            {
                if (item.NumberOfTimeSeen < 10 && item.EaseFactor < 2.5)
                {
                    return DateTime.Now.AddMinutes(5);
                }
                // Bad recall
                interval = 1; // Reset to initial interval
                item.EaseFactor -= 0.4; // Decrease ease (make harder)

                // Ensure ease factor doesn't go below a certain value (e.g., 1.3)
                item.EaseFactor = Math.Max(1.3, item.EaseFactor);
            }
            else
            {
                // Repeat - See the items in 5 minutes again
                return DateTime.Now.AddSeconds(10);
            }

            return DateTime.Now.AddDays(interval);
        }
    }
}
