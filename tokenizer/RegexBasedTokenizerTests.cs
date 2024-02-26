using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Tokenizer;
using Xunit;

namespace Tokenizer
{
    public class RegexBasedTokenizerTests
    {

        [Fact]
        public void TokenizeSExpression1()
        {
            string inputString = "(defun hello_world () print \"Hello World\")";
            List<string> tokens = RegexBasedTokenizer.TokenizeInput(inputString);

            Assert.Equal("(",tokens[0]);
        }

        [Fact]
        public void TokenizeSExpression2()
        {
            string inputString = "(defun hello_world () print \"Hello World\"\n)";
            List<string> tokens = RegexBasedTokenizer.TokenizeInput(inputString);

            Assert.Equal("(", tokens[0]);
        }
    }
}
