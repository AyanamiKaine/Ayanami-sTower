using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StellaLang;

/// <summary>
/// Parses s-expressions in Reverse Polish Notation (RPN) format and compiles them to VM bytecode.
/// In RPN s-expressions, operations come after their operands: (2 3 +) instead of (+ 2 3).
/// This matches the VM's stack-based execution model where operations pop from top of stack.
/// 
/// Examples:
/// - (2 3 +)           → PUSH 2, PUSH 3, ADD
/// - (5 (2 3 +) *)     → PUSH 5, PUSH 2, PUSH 3, ADD, MUL
/// - (x @)             → PUSH x, FETCH (assuming x is a variable address)
/// - (42 y !)          → PUSH 42, PUSH y, STORE
/// - (1 (10 PRINT) (20 PRINT) IF) → condition-based execution
/// </summary>
public static class SExpressionParser
{
    /// <summary>
    /// Represents a token in the s-expression.
    /// </summary>
    private abstract record Token
    {
        /// <summary>Opening parenthesis.</summary>
        public sealed record LeftParen : Token;

        /// <summary>Closing parenthesis.</summary>
        public sealed record RightParen : Token;

        /// <summary>An atom (word, number, or symbol).</summary>
        public sealed record Atom(string Value) : Token;
    }

    /// <summary>
    /// Represents a parsed s-expression element.
    /// </summary>
    private abstract record SExpr
    {
        /// <summary>An atomic value (number, word, or symbol).</summary>
        public sealed record AtomExpr(string Value) : SExpr;

        /// <summary>A list of s-expressions.</summary>
        public sealed record ListExpr(List<SExpr> Elements) : SExpr;
    }

    /// <summary>
    /// Tokenizes an s-expression string into a sequence of tokens.
    /// </summary>
    /// <param name="input">The s-expression string to tokenize.</param>
    /// <returns>A list of tokens.</returns>
    private static List<Token> Tokenize(string input)
    {
        var tokens = new List<Token>();
        var current = new StringBuilder();

        void FlushAtom()
        {
            if (current.Length > 0)
            {
                tokens.Add(new Token.Atom(current.ToString()));
                current.Clear();
            }
        }

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            switch (c)
            {
                case '(':
                    FlushAtom();
                    tokens.Add(new Token.LeftParen());
                    break;

                case ')':
                    FlushAtom();
                    tokens.Add(new Token.RightParen());
                    break;

                case ' ':
                case '\t':
                case '\n':
                case '\r':
                    FlushAtom();
                    break;

                default:
                    current.Append(c);
                    break;
            }
        }

        FlushAtom();
        return tokens;
    }

    /// <summary>
    /// Parses a sequence of tokens into an s-expression tree.
    /// </summary>
    /// <param name="tokens">The tokens to parse.</param>
    /// <returns>The parsed s-expression.</returns>
    private static SExpr Parse(List<Token> tokens)
    {
        if (tokens.Count == 0)
            throw new InvalidOperationException("Empty input - s-expressions must start with '('.");

        int index = 0;

        SExpr ParseOne()
        {
            if (index >= tokens.Count)
                throw new InvalidOperationException("Unexpected end of input while parsing s-expression.");

            var token = tokens[index++];

            return token switch
            {
                Token.LeftParen => ParseList(),
                Token.Atom atom => new SExpr.AtomExpr(atom.Value),
                Token.RightParen => throw new InvalidOperationException("Unexpected closing parenthesis."),
                _ => throw new InvalidOperationException($"Unknown token type: {token}")
            };
        }

        SExpr ParseList()
        {
            var elements = new List<SExpr>();

            while (index < tokens.Count)
            {
                var token = tokens[index];

                if (token is Token.RightParen)
                {
                    index++; // consume the closing paren
                    return new SExpr.ListExpr(elements);
                }

                elements.Add(ParseOne());
            }

            throw new InvalidOperationException("Unclosed parenthesis in s-expression.");
        }

        var result = ParseOne();

        // Ensure we consumed all tokens
        if (index < tokens.Count)
            throw new InvalidOperationException("Unexpected tokens after s-expression.");

        return result;
    }

    /// <summary>
    /// Compiles an s-expression to a ProgramBuilder.
    /// In RPN s-expressions, each element is evaluated left-to-right, with operations popping operands from the stack.
    /// </summary>
    /// <param name="expr">The s-expression to compile.</param>
    /// <param name="builder">The ProgramBuilder to add instructions to.</param>
    private static void CompileToBuilder(SExpr expr, ProgramBuilder builder)
    {
        switch (expr)
        {
            case SExpr.AtomExpr atom:
                CompileAtom(atom.Value, builder);
                break;

            case SExpr.ListExpr list:
                // In RPN, we evaluate each element in order
                // Each element either pushes a value or consumes values and pushes a result
                foreach (var element in list.Elements)
                {
                    CompileToBuilder(element, builder);
                }
                break;
        }
    }

    /// <summary>
    /// Maps common mathematical symbols to VM word names.
    /// </summary>
    private static readonly Dictionary<string, string> SymbolToWord = new()
    {
        ["+"] = "ADD",
        ["-"] = "SUB",
        ["*"] = "MUL",
        ["/"] = "DIV"
    };

    /// <summary>
    /// Compiles an atomic value (number, word, or symbol) to the ProgramBuilder.
    /// </summary>
    private static void CompileAtom(string value, ProgramBuilder builder)
    {
        // Try to parse as a number first
        if (long.TryParse(value, out long intValue))
        {
            builder.Push(intValue);
            return;
        }

        if (double.TryParse(value, out double floatValue))
        {
            // For now, we'll convert floats to integers since the VM primarily uses integers
            // A more sophisticated implementation would support float literals
            builder.Push((long)floatValue);
            return;
        }

        // Map symbols to word names
        if (SymbolToWord.TryGetValue(value, out var wordName))
        {
            builder.Word(wordName);
            return;
        }

        // Otherwise, treat it as a word name
        builder.Word(value);
    }

    /// <summary>
    /// Parses and compiles an s-expression string to a ProgramBuilder.
    /// </summary>
    /// <param name="input">The s-expression string in RPN format.</param>
    /// <returns>A ProgramBuilder containing the compiled program.</returns>
    public static ProgramBuilder Compile(string input)
    {
        var tokens = Tokenize(input);
        var expr = Parse(tokens);
        var builder = new ProgramBuilder();
        CompileToBuilder(expr, builder);
        return builder;
    }

    /// <summary>
    /// Parses and executes an s-expression string on the provided VM.
    /// </summary>
    /// <param name="input">The s-expression string in RPN format.</param>
    /// <param name="vm">The VM to execute on.</param>
    public static void Execute(string input, VMActor vm)
    {
        var builder = Compile(input);
        builder.Execute(vm);
    }
}
