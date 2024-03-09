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
        public void EaseFactorIncrease()
        {
            FileToLearn item = new("0", "Hello World", "", "", 2.5, 2);
            item.NextReviewDate = SpacedRepetitionScheduler.CalculateNextReviewDate(item, "g");

            Assert.True(item.NextReviewDate > DateTime.Now);
        }
    }
}
