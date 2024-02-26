using Tokenizer;
using Xunit;

namespace Tokenizer
{
    public class BasicTokenizerTests
    {
        [Fact]
        public void Tokenize_Two_Lines1()
        {
            BasicTokenizer tokenizer = new("Tim 12 \n Karl 90");

            List<BasicToken> listOfTokens =  tokenizer.TokenizeSource();

            List<BasicToken> expectedTokens= new List<BasicToken>()
            {
                new(BasicTokenType.Identifier, "Tim", "Tim"),
                new(BasicTokenType.Number, "12", 12),
                new(BasicTokenType.Identifier, "Karl", "Karl"),
                new(BasicTokenType.Number, "90", 90),
            };

            Assert.True(expectedTokens[3].Equals(listOfTokens[3]));
        }

        [Fact]
        public void Tokenize_Two_Lines2()
        {
            BasicTokenizer tokenizer = new("Tim 12 \n Karl 90");

            List<BasicToken> listOfTokens = tokenizer.TokenizeSource();

            List<BasicToken> expectedTokens = new List<BasicToken>()
            {
                new(BasicTokenType.Identifier, "Tim", "Tim"),
                new(BasicTokenType.Number, "12", 12),
                new(BasicTokenType.Identifier, "Karl", "Karl"),
                new(BasicTokenType.Number, "90", 90),
            };

            Assert.Equal(expectedTokens.Count, listOfTokens.Count);
        }

        [Fact]
        public void Tokenize_equalToken()
        {
            BasicTokenizer tokenizer = new("Tim 12 \n Karl 90 \n == ");

            List<BasicToken> listOfTokens = tokenizer.TokenizeSource();

            List<BasicToken> expectedTokens = new List<BasicToken>()
            {
                new(BasicTokenType.Identifier, "Tim", "Tim"),
                new(BasicTokenType.Number, "12", 12),
                new(BasicTokenType.Identifier, "Karl", "Karl"),
                new(BasicTokenType.Number, "90", 90),
                new(BasicTokenType.Equal, "==", "=="),
            };

            Assert.Equal(expectedTokens[4], listOfTokens[4]);
        }

        [Fact]
        public void IgnoringComments()
        {
            BasicTokenizer tokenizer = new("Tim 12 \n Karl 90 //Hello World \n == ");

            List<BasicToken> listOfTokens = tokenizer.TokenizeSource();

            List<BasicToken> expectedTokens = new List<BasicToken>()
            {
                new(BasicTokenType.Identifier, "Tim", "Tim"),
                new(BasicTokenType.Number, "12", 12),
                new(BasicTokenType.Identifier, "Karl", "Karl"),
                new(BasicTokenType.Number, "90", 90),
                new(BasicTokenType.Equal, "==", "=="),
            };

            Assert.Equal(expectedTokens[4], listOfTokens[4]);
        }
    }
}