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
                // Good recall
                interval = (int)Math.Round(interval * item.EaseFactor, 0);
                item.EaseFactor += 0.1; // Adjust ease
            }
            else if (recallEvaluation == RecallEvaluation.BAD)
            {
                // Bad recall
                interval = 1; // Reset to initial interval
                item.EaseFactor -= 0.2; // Decrease ease (make harder)

                // Ensure ease factor doesn't go below a certain value (e.g., 1.3)
                item.EaseFactor = Math.Max(1.3, item.EaseFactor);
            }
            else
            {
                // Repeat - don't change the interval in this case
                interval = 0;
            }

            return DateTime.Now.AddDays(interval);
        }
    }
}
