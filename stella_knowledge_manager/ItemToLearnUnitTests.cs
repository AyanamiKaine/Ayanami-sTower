using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;


namespace stella_knowledge_manager
{
    public class ItemToLearnUnitTests
    {
        [Fact]
        public void EaseFactorIncreaseWhenGoodRatingIsChoosen()
        {
            FileToLearn item = new(new Guid { }, "Hello World", "", "", 2.5, 2);
            item.NextReviewDate = AT.SRS.SpacedRepetitionScheduler.CalculateNextReviewDate(item, AT.SRS.RecallEvaluation.GOOD);

            Assert.True(item.NextReviewDate > DateTime.Now);
        }
    }
}
