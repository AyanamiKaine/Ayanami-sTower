using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HackAssemblerV2
{
    public class Assembler
    {
        private Code _code;
        private Tokenizer _tokenizer;
        private List<Token> _tokens;

        // Start storing variables at index 16 of memory
        // For each storage of a new variable, we'd increment this value
        // This way we can store it in the next adjacent memory address
        // Set memory address of variable, memIndex++;
        public int memIndex = 16;

        private Dictionary<string, int> _symbolTable = new()
            {
                { "R0", 0 },
                { "R1", 1 },
                { "R2", 2 },
                { "R3", 3 },
                { "R4", 4 },
                { "R5", 5 },
                { "R6", 6 },
                { "R7", 7 },
                { "R8", 8 },
                { "R9", 9 },
                { "R10", 10 },
                { "R11", 11 },
                { "R12", 12 },
                { "R13", 13 },
                { "R14", 14 },
                { "R15", 15 },
                { "SP", 0 },
                { "LCL", 1},
                { "ARG", 2 },
                { "THIS", 3 },
                { "THAT", 4 },
                { "SCREEN", 16384 },
                { "KBD", 24576 }
            };

        public Assembler(string input)
        {
            _code = new Code();
            _tokenizer = new Tokenizer(input);
        }

        public void ResolveLabels()
        {
            _tokens = _tokenizer.ScanTokens();

            //Handling Labels
            foreach (Token token in _tokens)
            {
                if (token.Type == TokenType.LABEL)
                {
                    string label = token.Value.Trim('(');
                    label = label.Trim(')');

                    if(!_symbolTable.ContainsKey(label))
                    {
                        _symbolTable.Add(label, token.LineNumber);
                    }
                }
            }

            foreach (var token in _tokens)
            {
                if (token.Type == TokenType.SYMBOL)
                {
                    string symbol = token.Value.Trim('@');
                    if (!_symbolTable.ContainsKey(symbol))
                    {
                        symbol = token.Value.Trim('@');

                        int SymbolToNumber;
                        bool success = int.TryParse(symbol, out SymbolToNumber);

                        if (success == true)
                        {
                            _symbolTable.Add(symbol, SymbolToNumber);
                        } 
                        else
                        {
                            //Here we encounter and add variables to the symbol table
                            //we start at 16 and increment it by one for each variable 
                            //we see and add to the symbol table
                            _symbolTable.Add(symbol, memIndex);
                            memIndex++;
                        }
                    }
                }
            }
        }

        private void Parse()
        {
            ResolveLabels();
        }

        public List<string> Assemble()
        {
            List<string> output = [];

            ResolveLabels();

            /*
             * For every token that has the same line number handle it as part of one instruction 16 bit-binary output
             */
            for (int i = 0; i < _tokens.Count-1; i++)
            {
                Token currentToken = _tokens[i];
                Token nextToken = _tokens[i+1];

                string DestBinary = "";
                string CompBinary = "";
                string JumpBinary = "";


                switch (currentToken.Type)
                {
                    case TokenType.SYMBOL:
                        output.Add("0");

                        if (_symbolTable.ContainsKey(currentToken.Value.Trim('@')))
                        {
                            output[currentToken.LineNumber] += IntTo15BitNumber(_symbolTable[currentToken.Value.Trim('@')]);
                        }
                        break;
                    case TokenType.DEST:
                        output.Add("111");
                        DestBinary = _code.Dest(_tokens[i].Value);
                        CompBinary = _code.Comp(_tokens[i + 1].Value);
                        JumpBinary = _code.Jump(_tokens[i + 2].Value);
                        output[currentToken.LineNumber] += CompBinary + DestBinary + JumpBinary;
                        break;
                }
            }


            return output;
        }

        private string IntTo15BitNumber(int decimalNumber)
        {
            return Convert.ToString(decimalNumber, 2).PadLeft(15, '0');
        }
    }
}
