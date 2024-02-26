using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HackAssemblerV2
{
    public enum TokenType
    {
        DEST,
        JUMP,
        COMP,
        LITERAL,
        SYMBOL,
        LABEL,

        EOF,
    }

    public class Token(TokenType type, string value, object literal, int lineNumber)
    {
        public TokenType Type = type;
        public string Value = value;
        public object Literal = literal;
        public int LineNumber = lineNumber;
    }

    public class Tokenizer (string source)
    {
        private string _source = source;
        private List<Token> _tokens = [];
        private int _start = 0;
        private int _current = 0;
        private int _line = 0;


        public List<Token> ScanTokens()
        {
            while (!IsAtEnd())
            {
                _start = _current;
                ScanToken();
            }

            _tokens.Add(new Token(TokenType.EOF, "", null, _line));
            return _tokens;
        }


        public void ScanToken()
        {
            char c = Advance();
            switch (c)
            {                
                //Handling Comments, skipping over them
                case '/':
                    if (Match('/'))
                    {
                        while (Peek() != '\n' && !IsAtEnd()) Advance();
                        _line--; // A Comment does not increase the linenumber
                    }
                    break;

                case ' ':
                case '\r':
                case '\t':
                    //Ignore whitespace.
                    break;

                // Finding a lable does not increase the linenumber
                case '(':
                    Label();
                    _line--;
                    break;

                case '\n':
                    _line++;
                    break;
                
                default:
                    if (IsSymbol(c))
                    {
                        Symbol();
                        break;
                    }
                    if (IsCInstruction(c))
                    {
                        CInstruction();
                        break;
                    }
                    //throw new Exception("Unexpected Character");
                    break;
            }
        }

        private bool IsVariable(char c)
        {
            return IsAlpha(c);
        }

        private void CInstruction()
        {
            Dest();
            Comp();
            Jump();
        }

        private bool IsAInstruction(char c)
        {
            return IsSymbol(c);
        }

        private bool IsCInstruction(char c)
        {
            // A C instruction always starts with a possible destination
            if (c == '0')
            {
                return true;
            }
            else if (c == 'M')
            {
                return true;
            }
            else if (c == ('D'))
            {
                return true;
            }
            else if (c == ('A'))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void Label()
        {
            while (')' !=Peek()) Advance();

            if(')' == Peek())
            {
                Advance();
            }

            string text = _source.Substring(_start, _current - _start);
            AddToken(TokenType.LABEL, text);
        }

        private bool IsLabel(char c)
        {
            return c == '(';
        }

        private void Jump()
        {

            while (IsJump(Peek()))
            {
                Advance();
            }

            string text = _source.Substring(_start, _current - _start);
            AddToken(TokenType.JUMP, text);

            _start = _current;
        }

        private bool IsJump(char c)
        {
            switch (c)
            {
                case ';':
                    Advance();
                    _start = _current;
                    return true;
                case 'J':
                    return true;
                case 'G':
                    return true;
                case 'T':
                    return true;
                case 'E':
                    return true;
                case 'M':
                    return true;
                case 'P':
                    return true;
                case 'L':
                    return true;
                case 'N':
                    return true;
                case 'Q':
                    return true;



                default: 
                    return false;
            }
        }

        private void Comp()
        {
            while(IsComp(Peek()))
            {
                if(_start != 0)
                {
                    Advance();
                }
            }


            string text = _source.Substring(_start, _current - _start);
            AddToken(TokenType.COMP, text);

            _start = _current;
        }

        private bool IsComp(char c)
        {
            switch (c)
            {
                case '=':
                    Advance();
                    _start = _current;
                    return true;
                case '0':
                    return true;
                case '1':
                    return true;
                case '-':
                    return true;
                case '+':
                    return true;
                case 'D':
                    return true;
                case 'A':
                    return true;
                case 'M':
                    return true;
                case '&':
                    return true;
                case '|':
                    return true;
                case '!':
                    return true;

                default
                    : return false;
            }
        }

        private void Dest()
        {
            while (IsDest(Peek()))
            {
                Advance();
            }

            string text = "";

            if (Peek() == '=')
            {
                text = _source.Substring(_start, _current - _start);
            }

            AddToken(TokenType.DEST, text, text);
        }

        private bool IsDest(char c)
        {
            switch(c)
            {
                case '0':
                    if (Peek() == '=') 
                    {
                        return true;
                    }
                    else 
                    {
                        return false;
                    };
                case 'M':
                    return true;
                case 'D':
                    return true;
                case 'A':
                    return true;

                default: 
                    return false;
            }
        }

        private void Symbol()
        {
            while (IsAlphaNumeric(Peek()) && Peek() != '\n' && Peek() != '\0')
            {
                Advance();
            }
     
            string text = _source.Substring(_start, _current - _start);
            AddToken(TokenType.SYMBOL, text, text);
        }

        private bool IsSymbol(char c)
        {
            return c == '@';
        }

        private bool IsAlphaNumeric(char c)
        {
            return IsAlpha(c) || IsDigit(c) || c == '_' || c == '.' || c == '$' || c == ':' ;
        }

        private bool IsAlpha(char c)
        {
            return (c >= 'a' && c <= 'z') ||
                   (c >= 'A' && c <= 'Z') ||
                    c == '_';
        }

        private bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        private char Advance()
        {
            _current++;
            return _source[_current - 1];
        }
        
        private bool IsAtEnd()
        {
            return _current >= _source.Length;
        }

        private void AddToken(TokenType type)
        {
            AddToken(type, null);
        }

        private void AddToken(TokenType type, System.Object literal)
        {
            string text = _source.Substring(_start, _current - _start);
            _tokens.Add(new Token(type, text, literal, _line));
        }

        private void AddToken(TokenType type, string text, System.Object literal)
        {
            _tokens.Add(new Token(type, text, literal, _line));
        }

        private bool Match(char expected)
        {
            if (IsAtEnd()) return false;
            if (_source[_current] != expected) return false;

            _current++;
            return true;
        }

        private char Peek()
        {
            if (IsAtEnd()) return '\0';
            return _source[_current];
        }


    }
}
