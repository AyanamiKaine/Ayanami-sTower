/*
This source file is heavily commented!
This is done to increase understanding at the cost
of duplicated knowledge
*/

/*

Here we show how a tokenizer can be written for: 
1. Tokens with arbitrary character lengths
2. How to handle comments
3. How to handle tokens with 2 character lengths
4. How to handle whitespaces
5. How to handle newlines


Its based on an implicit deterministic finite-state machine even though this is an ad-hoc scanner/tokenizer/lexer
To understand the difference between an implicit and explicit deterministic finite-state machine
see DFATokenizer.cs
*/

namespace Tokenizer
{


    /* BNF GRAMMAR
    <person> ::= <name> <age>
    <name>   ::= <letter>+
    <age>    ::= <digit>+
    <equal> ::= '=='
    <letter> ::= 'a' | 'b' | 'c' | ... | 'z' | 'A' | 'B' | 'C' | ... | 'Z'
    <digit>  ::= '0' | '1' | '2' | ... | '9'
     */

    /// <summary>
    /// Tokens are Terminals in BNF Grammar. (A terminal in BNF is the smallest unit that can not further be divided)
    ///
    /// Here a collection of letters is an identifier (name) and a collection of numbers is the age
    /// 
    /// It helps to define the token type has an enum
    /// decoupling its string representation from its TokenType
    /// </summary>
    public enum BasicTokenType
    {
        Identifier, // To show tokens of arbitrary character size
        Number,     // To show tokens of arbitrary character size
        Equal,      // To show tokens of character size 2
    }

    public class BasicToken
    {
        public BasicToken(BasicTokenType type, string value, object literalValue)
        {
            Type = type;
            Value = value;
            LiteralValue = literalValue;
        }

        public BasicTokenType Type;
        
        /// <summary>
        /// string representation of the value, "Tim" would be string name = "Time";
        /// </summary>
        public string Value;
        
        /// <summary>
        /// If value would be "102" its literal value would be equivalent to int LiteralValue = 102
        /// Why holding a literal value?, it helps converting to the right type otherwise this must be done
        /// by the parser when creating the Abstract Syntax Tree. Usually this must be done anyway.
        /// So we are doing it at the beginning.
        /// If wished to reduce the complexity of the tokenizer this step can be done in parsing
        /// </summary>
        public object LiteralValue;


        // This makes it easier to compare tokens to each other, we do this so we can test it better
        // By default classes are reference objects, not value objects. They are only equal if they reference the same object
        // Here we overwrite this default behavior to say if all fields are equal the object is equal.
        public override bool Equals(object obj)
        {
            // 1. Check for null and ensure same type
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            // 2. Cast to BasicToken
            BasicToken other = (BasicToken)obj;

            // 3. Compare Type, Value, and LiteralValue (with null checks)
            return Type == other.Type &&
                   Value == other.Value &&
                   (LiteralValue == null && other.LiteralValue == null ||
                    LiteralValue != null && LiteralValue.Equals(other.LiteralValue));
        }

        public override int GetHashCode()
        {
            // A simple but often reasonable approach:
            return Type.GetHashCode() ^ (Value?.GetHashCode() ?? 0) ^ (LiteralValue?.GetHashCode() ?? 0);
        }
    }

    /// <summary>
    /// A tokenizer/lexer/scanner has a sequence of characters as inputs and returns a sequence of
    /// tokens( a token is the smallest meaningful collection of characters)
    ///
    /// It does not validate the grammar!
    /// Our grammar expects the syntax of NAME AGE not AGE AGE AGE or NAME NAME or ==
    /// It only validates tokens, i.e Tim is an identifier token and valid, Tim23 is not a valid token as an identifier
    /// can only consist of letters not letter and/or digits
    /// </summary>
    public class BasicTokenizer
    {
        // A Tokenizer always has input in form of strings, this could a collection of strings
        // Or a simple big one where lines are separated with \n in the string;
        private String _source;
        private List<BasicToken> _tokens = new ();
        
        /// <summary>
        /// Start position in the line
        /// </summary>
        private int _start = 0;
        /// <summary>
        /// Current position in the line
        /// </summary>
        private int _current = 0;

        /// <summary>
        /// We track the current line number as it can be used for error handling, to say error at Tokenizer at line x
        /// </summary>
        private int _line = 1;
        
        public BasicTokenizer(String source)
        {
            _source = source;
        }

        public List<BasicToken> TokenizeSource()
        {
            while (!IsAtEnd())
            {
                // We do this so we set _start to the start position of the newline, should be done after a \n is found as
                // we then return to our while loop 
                _start = _current;
                TokenizeLine();
            }

            // We may add an eof token to our token list to show that the entire source got tokenized,
            // I personally don't like it as the last token always shows the eof implicitly, less code is 
            // usually a better choice to reduce complexity, especially if it does not reduce expressiveness
            return _tokens;
        }

        /// <summary>
        /// Is at checks if our current source input is empty and no more character can be tokenized
        /// </summary>
        /// <returns></returns>
        private bool IsAtEnd()
        {
            return _current >= _source.Length;
        }

        private void TokenizeLine()
        {
            char c = AdvanceOneCharacter();

            switch (c)
            {
                // Here we handle any one or two long characters, anything longer "could" be handled in the switch statements
                // itself, but doing so would make it ugly and harder to read, anything longer or complex than 2 characters
                // should be a separate function.
                case ' ':
                case '\r':
                case '\t':
                    //Ignore whitespace.
                    break;
                case '\n':
                    _line++;
                    break;

                // This switch case handles one line comments that start with //, everything past it
                // should be ignored as we see it as part of the comment, we advance until we meet a \n
                case '/':
                    if (Match('/'))
                    {
                        while (Peek() != '\n' && !IsAtEnd()) AdvanceOneCharacter();
                    }
                    break;

                // Here we handle tokens that are two characters log
                case '=':
                    if (Match('='))
                    {   
                        var value = _source.Substring(_start, _current - _start);
                        AddTokenHelperFunction(BasicTokenType.Equal, value);
                    }
                    break;

                default:
                    //Here we handle anything that is longer than one character
                    if (IsLetter(c))
                    {
                        Letter();
                        break;
                    }
                    
                    if (IsDigit(c))
                    {
                        Digit();
                        break;
                    }
                    //If a character that we don't handle falls through throw an exception
                    Error(c);
                    break;
            }
        }

        private char AdvanceOneCharacter()
        {
            _current++;
            return _source[_current - 1];
        }

        private bool IsLetter(char c)
        {
            // or we could write char.IsLetter(c); we dont do this as it recognizes Unicode as valid letters
            // this is not valid in our grammar only aA - zZ is allowed
            return c is >= 'a' and <= 'z' or >= 'A' and <= 'Z';
        }

        /// <summary>
        /// Letter should append the tokenlist with a Token
        /// </summary>
        private void Letter()
        {
            while (IsLetter(Peek()))
            {
                AdvanceOneCharacter();
            }

            var value = _source.Substring(_start, _current - _start);
            AddTokenHelperFunction(BasicTokenType.Identifier, value);
        }

        private bool IsDigit(char c)
        {
            // or we could write char.IsDigit(c); 
            // same reason we don't do this as in IsLetter()
            return c is >= '0' and <= '9';
        }

        /// <summary>
        /// Digit should append the tokenlist with a Token
        /// </summary>
        private void Digit()
        {
            while (IsDigit(Peek()))
            {
                AdvanceOneCharacter();
            }

            var value = _source.Substring(_start, _current - _start);

            if (int.TryParse(value, out var literalValue))
            {
                AddTokenHelperFunction(BasicTokenType.Number, value, literalValue);
            }
        }

        /// <summary>
        /// AddToken is a helper function to add tokens to our token list
        /// (The need for helper functions for a datastructures or collection of data often shows the need for a
        /// deeper abstraction like creating a TokenList class)
        /// </summary>
        private void AddTokenHelperFunction(BasicTokenType type, string value, object literalValue)
        {
            BasicToken token = new BasicToken(type, value, literalValue);
            _tokens.Add(token);
        }
        private void AddTokenHelperFunction(BasicTokenType type, string value)
        {
            BasicToken token = new BasicToken(type, value, value);
            _tokens.Add(token);
        }

        private char Peek()
        {
            if (IsAtEnd()) return '\0';
            return _source[_current];
        }

        /// <summary>
        /// Match checks if the Next character is the one we expect, this is often used for 2 character tokens
        /// we then advance the current character so start to current character are the indexes in _source
        /// for the 2 character token
        /// </summary>
        /// <param name="expectedChar"></param>
        /// <returns></returns>
        private bool Match(char expectedChar)
        {
            if (IsAtEnd()) 
                return false;

            if (_source[_current] != expectedChar) 
                return false;

            _current++;
            return true;
        }

        /// <summary>
        /// For good error handling you should not throw an error and stop tokenizing
        /// Why? because we get much more from parsing if we show more errors, instead of fixing one and only then
        /// it shows that on the next line there is another error.
        ///
        /// Errors should not be discarded but we should try to recover and bundle the errors,
        /// this could mean inserting the right token so an AST might be correct.
        ///
        /// This does not mean we should compile/interpret the program but it ensures that
        /// we go as far as we can to compilation/interpretation to get the most knowledge and
        /// error reporting we can get.
        /// </summary>
        /// <param name="currentChar"></param>
        /// <exception cref="Exception"></exception>
        private void Error(char currentChar)
        {
            throw new Exception($"Error at line:{_line}, couldn't handle token creation with the current character of: {currentChar}");
        }
    }
}
