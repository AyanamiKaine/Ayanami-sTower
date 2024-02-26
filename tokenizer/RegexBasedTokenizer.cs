using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

/*

This shows why you maybe should not use regex expressions to handle tokenization.
- The Regex logic is hard to understand, and most importantly its incredible hard to modify it

Regex based on Make-A-Lisp Process
- https://github.com/kanaka/mal/blob/master/process/guide.md#step0

Regex Expression explained
- [\s,]*: Matches any number of whitespaces or commas. This is not captured so it will be ignored and not tokenized.

- ~@: Captures the special two-characters ~@ (tokenized).

- [\[\]{}()'`~^@]: Captures any special single character, one of []{}()'`~^@ (tokenized).

- "(?:\\.|[^\\"])*"?: Starts capturing at a double-quote and stops at the Next double-quote unless it was preceded by a 
    backslash in which case it includes it until the Next double-quote (tokenized). It will also match unbalanced strings 
    (no ending double-quote) which should be reported as an error.

- ;.*: Captures any sequence of characters starting with ; (tokenized).

- [^\s\[\]{}('"`,;)]*: Captures a sequence of zero or more non special characters (e.g. symbols, numbers, "true", "false", 
  and "nil") and is sort of the inverse of the one above that captures special characters (tokenized).
 */


namespace Tokenizer
{
    public class RegexBasedTokenizer
    {
        private static readonly Regex TokenRegex = new Regex(
            @"[\s,]*(~@|[\[\]{}()'`~^@]|""(?:\\.|[^\\""])*""?|;.*|[^\s\[\]{}('"",;)]*)",
            RegexOptions.Compiled); // Compile the regex for efficiency

        public static List<string> TokenizeInput(string input)
        {
            List<string> tokens = new List<string>();

            MatchCollection matches = TokenRegex.Matches(input);
            foreach (Match match in matches)
            {
                // We only care about captures within the parenthesis starting at char 6
                if (match.Groups.Count > 1)
                {
                    string token = match.Groups[1].Value;

                    // Check for unbalanced double-quotes 
                    if (token.StartsWith("\"") && !token.EndsWith("\""))
                    {
                        throw new FormatException("Unbalanced double-quotes in token");
                    }

                    tokens.Add(token);
                }
            }

            return tokens;
        }
    }

    public class Reader
    {
        private List<string> _tokens;
        private int _position;

        public Reader(List<string> tokens)
        {
            this._tokens = tokens;
            this._position = 0;
        }

        public string Next()
        {
            if (_position >= _tokens.Count)
            {
                throw new IndexOutOfRangeException("End of _tokens reached");
            }
            return _tokens[_position++];
        }

        public string Peek()
        {
            if (_position >= _tokens.Count)
            {
                throw new IndexOutOfRangeException("End of _tokens reached");
            }
            return _tokens[_position];
        }
    }
}
